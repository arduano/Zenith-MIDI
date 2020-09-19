using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;
using ZenithEngine;
using System.Collections.ObjectModel;
using ZenithEngine.Modules;
using System.IO;
using System.Windows;
using ZenithEngine.MIDI.Audio;
using ZenithEngine.DXHelper;

namespace Zenith.Models
{
    public class BaseModel : INotifyPropertyChanged
    {
        public RenderArgsModel RenderArgs { get; set; } = new RenderArgsModel();
        public OutputArgsModel OutputOptions { get; set; } = new OutputArgsModel();
        public MidiArgsModel Midi { get; set; } = new MidiArgsModel();
        public CacheModel Cache { get; set; } = CacheModel.Instance;
        public SettingsModel Settings { get; set; } = SettingsModel.Instance;
        public LanguagesModel Lang { get; set; } = LanguagesModel.Instance;

        public ObservableCollection<IModuleRender> RenderModules { get; } = new ObservableCollection<IModuleRender>();
        public IModuleRender SelectedModule { get; set; } = null;
        ModuleManager moduleManager { get; } = new ModuleManager();

        #region Module Loader
        public bool HasTriedLoadingModules { get; private set; } = false;
        public CancellableTask ModuleLoadTask { get; private set; }
        public bool IsLoadingModules => ModuleLoadTask != null;
        public bool AreModulesLoaded => !IsLoadingModules;

        public async Task LoadAllModules()
        {
            await Err.Handle(async () =>
            {
                HasTriedLoadingModules = true;
                if (IsLoadingModules) throw new UIException("Can't load modules while already loading");
                ModuleLoadTask = CancellableTask.Run(cancel =>
                {
                    RunModuleLoader(cancel).Wait();
                });
                try
                {
                    await ModuleLoadTask.Await();
                }
                finally
                {
                    ModuleLoadTask = null;
                }
            });
        }

        public void LoadAllModulesSync()
        {
            Err.Handle(() =>
            {
                HasTriedLoadingModules = true;
                try
                {
                    ModuleLoadTask?.Wait();
                }
                finally
                {
                    ModuleLoadTask = null;
                }
                RunModuleLoader().Wait();
            });
        }

        Task RunModuleLoader() => RunModuleLoader(new CancellationTokenSource().Token);
        async Task RunModuleLoader(CancellationToken cancel)
        {
            SelectedModule = null;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                RenderModules.Clear();
                moduleManager.ClearModule();
            });

            var files = Directory.GetFiles("Plugins");
            var dlls = files.Where((s) => s.EndsWith(".dll"));

            var loaders = dlls.Select(dll =>
            {
                cancel.ThrowIfCancellationRequested();
                return Task.Run(() => ModuleManager.LoadModule(dll));
            });
            foreach (var mod in loaders)
            {
                cancel.ThrowIfCancellationRequested();
                try
                {
                    var module = await mod;
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        RenderModules.Add(module);
                    });
                }
                catch (ModuleLoadFailedException e)
                {
                    Err.Notify(e.Message);
                }
                catch (Exception e)
                {
                    Err.Notify(e.ToString());
                }
                cancel.ThrowIfCancellationRequested();
            }

            if (SelectedModule == null) SelectDefaultRenderer();
        }

        void SelectDefaultRenderer()
        {
            int i = 0;
            foreach (var p in RenderModules)
            {
                if (p.Name == "Classic")
                {
                    SelectedModule = p;
                    return;
                }
                i++;
            }
            if (RenderModules.Count > 0) SelectedModule = RenderModules[0];
            else SelectedModule = null;
        }
        #endregion

        #region Playback
        public RenderPipeline RenderPipeline { get; set; }
        public RenderStatus RenderStatus { get; set; }

        public RenderProgressModel RenderProgress { get; set; } = null;

        public CancellableTask RenderTask { get; private set; }

        public bool IsMidiLoaded { get; private set; } = false;

        public bool IsPlaying => RenderPipeline != null;
        public bool IsPlaybackLoading => RenderPipeline == null && RenderTask != null;
        public bool IsRendering => IsPlaying && RenderPipeline.Rendering;
        public bool IsNotPlaying => !IsPlaying;
        public bool IsNotRendering => !IsRendering;
        public bool CanStartPlaying => IsNotPlaying && IsMidiLoaded && SelectedModule != null;

        public bool Paused { get; set; } = false;
        public bool VsyncEnabled { get; set; } = false;
        public bool RealtimePlayback { get; set; } = true;
        public bool MuteAudio { get; set; } = false;
        public double PreviewSpeed { get; set; } = 1;

        void AssertRenderConstraints()
        {
            if (IsPlaying) throw new UIException("Can't start playback when already playing");
            if (Midi.LoadStatus != MidiLoadStatus.Loaded) throw new UIException("Can't start playback with no midi loaded");
            if (SelectedModule == null) throw new UIException("Can't play with no module selected");
        }

        public Task StartPreview() => StartPlayback(false);
        public Task StartRender() => StartPlayback(true);

        async Task StartPlayback(bool render)
        {
            await Err.Handle(async () =>
            {
                AssertRenderConstraints();
                RenderTask = CancellableTask.Run(cancel =>
                {
                    var startOffset = Midi.Loaded.MidiFile.StartTicksToSeconds(SelectedModule.StartOffset, RenderArgs.NoteSize == NoteSize.Time);
                    if (render) startOffset += OutputOptions.StartOffset;

                    OutputSettings output = null;
                    if (render)
                    {
                        var args = OutputOptions.ValidateAndGetRenderArgs(startOffset);
                        if (OutputOptions.UseMaskOutput)
                        {
                            output = new OutputSettings(args, OutputOptions.OutputLocation, OutputOptions.MaskOutputLocation);
                        }
                        else
                        {
                            output = new OutputSettings(args, OutputOptions.OutputLocation);
                        }
                    }

                    RenderStatus = new RenderStatus(RenderArgs.Width, RenderArgs.Height, RenderArgs.SSAA);
                    if (!render)
                    {
                        RenderStatus.RealtimePlayback = RealtimePlayback;
                        RenderStatus.PreviewAudioEnabled = !MuteAudio;
                    }
                    else
                    {
                        RenderStatus.RealtimePlayback = false;
                        RenderStatus.PreviewAudioEnabled = false;
                    }

                    var playback = Midi.Loaded.MidiFile.GetMidiPlayback(startOffset, RenderArgs.NoteSize == NoteSize.Time);


                    RenderPipeline = new RenderPipeline(RenderStatus, playback, moduleManager, output);
                    if (!render)
                    {
                        RenderPipeline.Paused = Paused;
                        RenderPipeline.VSync = VsyncEnabled;
                        RenderPipeline.PreviewSpeed = PreviewSpeed;
                    }
                    else
                    {
                        RenderPipeline.Paused = false;
                        RenderPipeline.VSync = false;
                        RenderPipeline.PreviewSpeed = 1;
                    }

                    RenderProgress = new RenderProgressModel(RenderPipeline);

                    var device = new DeviceGroup();
                    PreviewBase preview;
                    if (render)
                    {
                        var elpreview = new ElementPreview(device);
                        preview = elpreview;
                        RenderProgress.PreviewElement = elpreview.Element;
                    }
                    else
                    {
                        preview = new WindowPreview(device);
                    }
                    try
                    {
                        cancel.ThrowIfCancellationRequested();
                        RenderPipeline.Start(preview, cancel).Wait();
                    }
                    finally
                    {
                        RenderPipeline.Dispose();
                        device.Dispose();
                        RenderPipeline = null;
                        RenderStatus = null;
                        RenderProgress = null;
                    }
                });
                try
                {
                    await RenderTask.Await();
                }
                finally
                {
                    RenderTask = null;
                }
            });
        }

        public async Task StopPlayback()
        {
            await Err.Handle(async () =>
            {
                RenderTask?.Cancel();
                await RenderTask.Await();
            });
        }
        #endregion

        #region Audio
        public bool KdmapiConnected { get; private set; }
        public bool KdmapiNotDetected { get; private set; }
        public Task ConnectKdmapiTask { get; private set; }
        public Task DisconnectKdmapiTask { get; private set; }
        public bool LoadingKdmapi => ConnectKdmapiTask != null || DisconnectKdmapiTask != null;

        public async Task LoadKdmapi()
        {
            await Err.Handle(async () =>
            {
                if (KdmapiNotDetected) throw new UIException("Can't detect kdmapi, connection cancelled");
                if (KdmapiConnected || LoadingKdmapi) throw new UIException("Can't load kdmapi while it is already loaded");
                ConnectKdmapiTask = Task.Run(() =>
                {
                    try
                    {
                        KDMAPIOutput.Init();
                        KdmapiConnected = KDMAPIOutput.Initialized;
                    }
                    catch
                    {
                        KdmapiNotDetected = true;
                    }
                });
                try
                {
                    await ConnectKdmapiTask.Await();
                }
                finally
                {
                    ConnectKdmapiTask = null;
                }
            });
        }

        public async Task UnloadKdmapi()
        {
            await Err.Handle(async () =>
            {
                if (!KdmapiConnected || LoadingKdmapi) throw new UIException("Can't disconnect kdmapi while when it isn't connected");
                DisconnectKdmapiTask = Task.Run(() =>
                {
                    KDMAPIOutput.Terminate();
                    KdmapiConnected = KDMAPIOutput.Initialized;
                });
                try
                {
                    await DisconnectKdmapiTask.Await();
                }
                finally
                {
                    DisconnectKdmapiTask = null;
                }
            });
        }
        #endregion

        public BaseModel()
        {
            PropertyChanged += BaseModel_PropertyChanged;
            Midi.PropertyChanged += Midi_PropertyChanged;

            Lang.PropertyChanged += Lang_PropertyChanged;
            Settings.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.SelectedLanguage))
            {

            }
        }

        private void Lang_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Midi_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Midi.LoadStatus))
                IsMidiLoaded = Midi.LoadStatus == MidiLoadStatus.Loaded;
        }

        private void BaseModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedModule))
                moduleManager.UseModule(SelectedModule);

            if (IsPlaying)
            {
                if (!IsRendering)
                {
                    if (e.PropertyName == nameof(VsyncEnabled))
                        RenderPipeline.VSync = VsyncEnabled;
                    if (e.PropertyName == nameof(RealtimePlayback))
                        RenderStatus.RealtimePlayback = RealtimePlayback;
                    if (e.PropertyName == nameof(Paused))
                        RenderPipeline.Paused = Paused;
                    if (e.PropertyName == nameof(MuteAudio))
                        RenderStatus.PreviewAudioEnabled = !MuteAudio;
                    if (e.PropertyName == nameof(PreviewSpeed))
                        RenderPipeline.PreviewSpeed = PreviewSpeed;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
