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

namespace Zenith.Models
{
    public class BaseModel : INotifyPropertyChanged
    {
        public RenderArgsModel RenderArgs { get; set; } = new RenderArgsModel();
        public MidiArgsModel Midi { get; set; } = new MidiArgsModel();

        public ObservableCollection<IModuleRender> RenderModules { get; } = new ObservableCollection<IModuleRender>();
        public IModuleRender SelectedModule { get; set; } = null;
        ModuleManager moduleManager { get; } = new ModuleManager();

        #region Module Loader
        public bool HasTriedLoadingModules { get; private set; } = false;
        public CancellableTask ModuleLoadTask { get; private set; }
        public bool IsLoadingModules => ModuleLoadTask != null;
        public bool AreModulesLoaded => !IsLoadingModules;

        public void LoadAllModules()
        {
            HasTriedLoadingModules = true;
            if (IsLoadingModules) throw new UIException("Can't load modules while already loading");
            ModuleLoadTask = CancellableTask.Run(async cancel =>
            {
                await RunModuleLoader(cancel);
            });
        }

        public void LoadAllModulesSync()
        {
            HasTriedLoadingModules = true;
            ModuleLoadTask?.Wait();
            RunModuleLoader().Wait();
        }

        Task RunModuleLoader() => RunModuleLoader(new CancellationTokenSource().Token);
        async Task RunModuleLoader(CancellationToken cancel)
        {
            //previewImage.Source = null;
            //pluginDescription.Text = "";
            SelectedModule = null;

            RenderModules.Clear();
            moduleManager.ClearModule();

            RenderModules.Clear();
            var files = Directory.GetFiles("Plugins");
            var dlls = files.Where((s) => s.EndsWith(".dll"));
            foreach (var d in dlls)
            {
                cancel.ThrowIfCancellationRequested();
                try
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        RenderModules.Add(ModuleManager.LoadModule(d));
                    });
                }
#if DEBUG
                catch (ModuleLoadFailedException e)
#else
                catch (Exception e)
#endif
                {
                    cancel.ThrowIfCancellationRequested();
                    Err.Notify(e.Message);
                }
            }

            SelectDefaultRenderer();
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

        public CancellableTask RenderTask { get; private set; }

        public bool IsPlaying => RenderPipeline != null;
        public bool IsRendering => IsPlaying && RenderPipeline.Rendering;
        public bool IsNotPlaying => !IsPlaying;
        public bool IsNotRendering => !IsNotRendering;

        public bool Paused { get; set; } = false;
        public bool VsyncEnabled { get; set; } = true;
        public bool RealtimePlayback { get; set; } = true;
        public bool UseAudioOutput { get; set; } = true;
        public double PreviewSpeed { get; set; } = 1;

        void AssertRenderConstraints()
        {
            if (IsPlaying) throw new UIException("Can't start playback when already playing");
            if (Midi.LoadStatus != MidiLoadStatus.Loaded) throw new UIException("Can't start playback with no midi loaded");
            if (SelectedModule == null) throw new UIException("Can't play with no module selected");
        }

        public async Task StartPreview()
        {
            await Err.Handle(async () =>
            {
                AssertRenderConstraints();
                RenderTask = CancellableTask.Run(async cancel =>
                {
                    RenderStatus = new RenderStatus(RenderArgs.Width, RenderArgs.Height, RenderArgs.SSAA)
                    {
                        RealtimePlayback = RealtimePlayback,
                        PreviewAudioEnabled = UseAudioOutput,
                    };
                    var playback = Midi.Loaded.MidiFile.GetMidiPlayback(0, RenderArgs.NoteSize == NoteSize.Time);
                    RenderPipeline = new RenderPipeline(RenderStatus, playback, moduleManager)
                    {
                        Paused = Paused,
                        VSync = VsyncEnabled,
                        PreviewSpeed = PreviewSpeed,
                    };
                    await RenderPipeline.Start(cancel);
                    RenderPipeline.Dispose();
                    RenderPipeline = null;
                    RenderStatus = null;
                });
                await RenderTask.Task;
            });
        }

        public async Task StopPlayback()
        {
            RenderTask.Cancel();
            await RenderTask.Task;
        }
        #endregion

        public BaseModel()
        {
            PropertyChanged += BaseModel_PropertyChanged;
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
                    if (e.PropertyName == nameof(UseAudioOutput))
                        RenderStatus.PreviewAudioEnabled = UseAudioOutput;
                    if (e.PropertyName == nameof(PreviewSpeed))
                        RenderPipeline.PreviewSpeed = PreviewSpeed;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
