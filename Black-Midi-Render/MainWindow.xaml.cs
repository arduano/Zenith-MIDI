using ZenithEngine;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using ZenithShared;
using System.IO;
using System.IO.Compression;
using ZenithEngine.Modules;
using ZenithEngine.MIDI;
using ZenithEngine.MIDI.Disk;
using ZenithEngine.GLEngine;
using ZenithEngine.GLEngine.Types;
using System.Collections.ObjectModel;
using ZenithEngine.UI;
using OpenTK.Graphics;
using System.Globalization;
using ZenithEngine.MIDI.Audio;

namespace Zenith
{
    class CurrentRendererPointer
    {
        public Queue<IModuleRender> disposeQueue = new Queue<IModuleRender>();
        public IModuleRender renderer = null;
    }

    public enum UpdateProgress
    {
        NotDownloading,
        Downloading,
        Downloaded
    }

    public partial class MainWindow : Window
    {
        #region Chrome Window scary code
        private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }
            return (IntPtr)0;
        }

        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
            }
            Marshal.StructureToPtr(mmi, lParam, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
            public static readonly RECT Empty = new RECT();
            public int Width { get { return Math.Abs(right - left); } }
            public int Height { get { return bottom - top; } }
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
            public RECT(RECT rcSrc)
            {
                left = rcSrc.left;
                top = rcSrc.top;
                right = rcSrc.right;
                bottom = rcSrc.bottom;
            }
            public bool IsEmpty { get { return left >= right || top >= bottom; } }
            public override string ToString()
            {
                if (this == Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }
            public override bool Equals(object obj)
            {
                if (!(obj is Rect)) { return false; }
                return (this == (RECT)obj);
            }
            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode() => left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2) { return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom); }
            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2) { return !(rect1 == rect2); }
        }

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);
        #endregion

        #region Dependency Properties
        public bool MidiLoaded
        {
            get { return (bool)GetValue(MidiLoadedProperty); }
            set { SetValue(MidiLoadedProperty, value); }
        }

        public static readonly DependencyProperty MidiLoadedProperty =
            DependencyProperty.Register("MidiLoaded", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        public RenderPipeline ActivePipeline
        {
            get { return (RenderPipeline)GetValue(ActivePipelineProperty); }
            set { SetValue(ActivePipelineProperty, value); }
        }

        public static readonly DependencyProperty ActivePipelineProperty =
            DependencyProperty.Register("ActivePipeline", typeof(RenderPipeline), typeof(MainWindow), new PropertyMetadata(null));


        public bool Rendering
        {
            get { return (bool)GetValue(RenderingProperty); }
            set { SetValue(RenderingProperty, value); }
        }

        public static readonly DependencyProperty RenderingProperty =
            DependencyProperty.Register("Rendering", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        public bool RenderingOrPreviewing
        {
            get { return (bool)GetValue(RenderingOrPreviewingProperty); }
            set { SetValue(RenderingOrPreviewingProperty, value); }
        }

        public static readonly DependencyProperty RenderingOrPreviewingProperty =
            DependencyProperty.Register("RenderingOrPreviewing", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool CanLoadMidi
        {
            get { return (bool)GetValue(CanLoadMidiProperty); }
            set { SetValue(CanLoadMidiProperty, value); }
        }

        public static readonly DependencyProperty CanLoadMidiProperty =
            DependencyProperty.Register("CanLoadMidi", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool CanUnloadMidi
        {
            get { return (bool)GetValue(CanUnloadMidiProperty); }
            set { SetValue(CanUnloadMidiProperty, value); }
        }

        public static readonly DependencyProperty CanUnloadMidiProperty =
            DependencyProperty.Register("CanUnloadMidi", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool CanStart
        {
            get { return (bool)GetValue(CanStartProperty); }
            set { SetValue(CanStartProperty, value); }
        }

        public static readonly DependencyProperty CanStartProperty =
            DependencyProperty.Register("CanStart", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        public bool CanChangeResolution
        {
            get { return (bool)GetValue(CanChangeResolutionProperty); }
            set { SetValue(CanChangeResolutionProperty, value); }
        }

        public static readonly DependencyProperty CanChangeResolutionProperty =
            DependencyProperty.Register("CanChangeResolution", typeof(bool), typeof(MainWindow), new PropertyMetadata(true));

        void InitBindings()
        {
            new InplaceConverter<RenderPipeline, bool>(
                new BBinding(ActivePipelineProperty, this),
                (p) => p != null
            ).Set(this, RenderingOrPreviewingProperty);

            new InplaceConverter<bool, bool, bool>(
                new BBinding(RenderingOrPreviewingProperty, this),
                new BBinding(MidiLoadedProperty, this),
                (r, m) => !r && !m
            ).Set(this, CanLoadMidiProperty);

            new InplaceConverter<bool, bool, bool>(
                new BBinding(RenderingOrPreviewingProperty, this),
                new BBinding(MidiLoadedProperty, this),
                (r, m) => !r && m
            ).Set(this, CanUnloadMidiProperty);

            new InplaceConverter<bool, bool, bool>(
                new BBinding(RenderingOrPreviewingProperty, this),
                new BBinding(MidiLoadedProperty, this),
                (r, m) => !r && m
            ).Set(this, CanStartProperty);

            new InplaceConverter<RenderPipeline, bool>(
                new BBinding(ActivePipelineProperty, this),
                (p) => p != null && p.Rendering
            ).Set(this, RenderingProperty);

            new InplaceConverter<bool, bool>(
                new BBinding(RenderingOrPreviewingProperty, this),
                (r) => !r
            ).Set(this, CanChangeResolutionProperty);
        }

        #endregion


        MidiFile midifile = null;
        string midipath = "";

        FrameworkElement pluginControl = null;

        ObservableCollection<IModuleRender> RenderModules { get; } = new ObservableCollection<IModuleRender>();

        List<Dictionary<string, ResourceDictionary>> Languages = new List<Dictionary<string, ResourceDictionary>>();

        bool foundOmniMIDI = true;
        bool OmniMIDIDisabled = false;

        long lastBackgroundChangeTime = 0;

        string defaultPlugin = "Classic";

        ModuleManager ModuleRunner { get; } = new ModuleManager();
        InstanceSettings Instance { get; } = new InstanceSettings();
        InstallSettings InstallSettings { get; } = new InstallSettings();

        RenderStatus CurrentRenderStatus { get; set; } = null;

        void RunLanguageCheck()
        {
            string ver;
            try
            {
                ver = ZenithLanguages.GetLatestVersion();
            }
            catch { return; }
            if (ver != InstallSettings.LanguagesVersion)
            {
                try
                {
                    Console.WriteLine("Update found for language packs, downloading...");
                    var latest = ZenithLanguages.DownloadLatestVersion();
                    ZenithLanguages.UnpackFromStream(latest);
                    InstallSettings.LanguagesVersion = ver;
                    InstallSettings.SaveConfig();
                    Console.WriteLine("Updated language packs!");
                }
                catch { Console.WriteLine("Failed to update language packs"); }
            }
        }

        void RunUpdateCheck()
        {
            if (!InstallSettings.AutoUpdate) return;

            var requiredInstaller = ZenithUpdates.InstallerVer;
            if (InstallSettings.InstallerVer != requiredInstaller)
            {
                Console.WriteLine("Important update found for installer, updating...");
                ZenithUpdates.UpdateInstaller();
                InstallSettings.InstallerVer = requiredInstaller;
                InstallSettings.SaveConfig();
            }

            string ver;
            try
            {
                ver = ZenithUpdates.GetLatestVersion();
            }
            catch { return; }
            if (ver != InstallSettings.VersionName)
            {
                Console.WriteLine("Found Update! Current: " + InstallSettings.VersionName + " Latest: " + ver);
                try
                {
                    Dispatcher.InvokeAsync(() => windowTabs.UpdaterProgress = UpdateProgress.Downloading).Wait();
                    Stream data;
                    if (Environment.Is64BitOperatingSystem) data = ZenithUpdates.DownloadAssetData(ZenithUpdates.DataAssetName64);
                    else data = ZenithUpdates.DownloadAssetData(ZenithUpdates.DataAssetName32);
                    var dest = File.OpenWrite(ZenithUpdates.DefaultUpdatePackagePath);
                    data.CopyTo(dest);
                    data.Close();
                    dest.Close();
                    Dispatcher.InvokeAsync(() => windowTabs.UpdaterProgress = UpdateProgress.Downloaded).Wait();
                }
                catch (Exception e)
                {
                    Dispatcher.InvokeAsync(() => windowTabs.UpdaterProgress = UpdateProgress.NotDownloading).Wait();
                    MessageBox.Show("Couldn't download and save update package", "Update failed");
                }
            }
        }

        void CheckUpdateDownloaded()
        {
            if (InstallSettings.PreviousVersion != InstallSettings.VersionName)
            {
                if (File.Exists("settings.json")) File.Delete("settings.json");
                InstallSettings.PreviousVersion = InstallSettings.VersionName;
                InstallSettings.SaveConfig();
            }

            if (InstallSettings.AutoUpdate)
            {
                if (File.Exists(ZenithUpdates.DefaultUpdatePackagePath))
                {
                    try
                    {
                        using (var z = File.OpenRead(ZenithUpdates.DefaultUpdatePackagePath))
                        using (ZipArchive archive = new ZipArchive(z))
                        { }
                        Dispatcher.InvokeAsync(() => windowTabs.UpdaterProgress = UpdateProgress.Downloaded).Wait();
                        if (!ZenithUpdates.IsAnotherProcessRunning())
                        {
                            Process.Start(ZenithUpdates.InstallerPath, "update -Reopen");
                        }
                    }
                    catch (Exception) { File.Delete(ZenithUpdates.DefaultUpdatePackagePath); }
                }
            }
        }

        public void LoadMidi(string path)
        {
            if (midifile != null) UnloadMidi();

            if (!File.Exists(path))
            {
                MessageBox.Show("Midi file doesn't exist");
                return;
            }
            try
            {
                if (midifile != null) midifile.Dispose();
                midifile = null;
                GC.Collect();
                GC.WaitForFullGCComplete();
                midifile = new DiskMidiFile(path);
                MidiLoaded = true;
                browseMidiButton.Content = Path.GetFileName(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        public void UnloadMidi()
        {
            midifile.Dispose();
            midifile = null;
            GC.Collect();
            GC.WaitForFullGCComplete();
            MidiLoaded = false;
            browseMidiButton.SetResourceReference(Button.ContentProperty, "load");
        }

        public MainWindow()
        {
            InitBindings();

            CheckUpdateDownloaded();

            InitializeComponent();
            SetCustomFFMPEGOptions();

            pluginsList.ItemsSource = RenderModules;

            windowTabs.VersionName = InstallSettings.VersionName;

            SourceInitialized += (s, e) =>
            {
                IntPtr handle = (new WindowInteropHelper(this)).Handle;
                HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WindowProc));
            };

            tempoMultSlider.nudToSlider = v => Math.Log(v, 2);
            tempoMultSlider.sliderToNud = v => Math.Pow(2, v);

            bool dontUpdateLanguages = false;

            if (!File.Exists("Settings/settings.json"))
            {
                var sett = new JObject();
                sett.Add("defaultBackground", "");
                sett.Add("ignoreKDMAPI", "false");
                sett.Add("defaultPlugin", "Classic");
                sett.Add("ignoreLanguageUpdates", "false");
                File.WriteAllText("Settings/settings.json", JsonConvert.SerializeObject(sett));
            }

            {
                dynamic sett = JsonConvert.DeserializeObject(File.ReadAllText("Settings/settings.json"));
                if (sett.defaultBackground != "")
                {
                    try
                    {
                        bgImagePath.Text = sett.defaultBackground;
                    }
                    catch
                    {
                        if (bgImagePath.Text != "")
                            MessageBox.Show("Couldn't load default background image");
                    }
                }
                if ((bool)sett.ignoreKDMAPI) foundOmniMIDI = false;
                defaultPlugin = (string)sett.defaultPlugin;
                dontUpdateLanguages = (bool)sett.ignoreLanguageUpdates;
            }

            Task omnimidiLoader = null;
            Task languageLoader = null;
            if (!dontUpdateLanguages) Task.Run(RunLanguageCheck);
            Task updateLoader = Task.Run(RunUpdateCheck);
            if (foundOmniMIDI)
            {
                omnimidiLoader = Task.Run(() =>
                {
                    try
                    {
                        KDMAPIOutput.Init();
                        Console.WriteLine("Loaded KDMAPI!");
                    }
                    catch
                    {
                        Console.WriteLine("Failed to load KDMAPI, disabling");
                        foundOmniMIDI = false;
                    }
                });
            }
            if (!foundOmniMIDI)
            {
                disableKDMAPI.IsEnabled = false;
            }

            InitialiseSettingsValues();
            creditText.Text = "Video was rendered with Zenith\nhttps://arduano.github.io/Zenith-MIDI/start";

            if (languageLoader != null) languageLoader.Wait();

            var languagePacks = Directory.GetDirectories("Languages");
            foreach (var language in languagePacks)
            {
                var resources = Directory.GetFiles(language).Where((l) => l.EndsWith(".xaml")).ToList();
                if (resources.Count == 0) continue;

                Dictionary<string, ResourceDictionary> fullDict = new Dictionary<string, ResourceDictionary>();
                foreach (var r in resources)
                {
                    ResourceDictionary file = new ResourceDictionary();
                    file.Source = new Uri(Path.GetFullPath(r), UriKind.RelativeOrAbsolute);
                    var name = Path.GetFileNameWithoutExtension(r);
                    fullDict.Add(name, file);
                }
                if (!fullDict.ContainsKey("window")) continue;
                if (fullDict["window"].Contains("LanguageName") && fullDict["window"]["LanguageName"].GetType() == typeof(string))
                    Languages.Add(fullDict);
            }
            Languages.Sort(new Comparison<Dictionary<string, ResourceDictionary>>((d1, d2) =>
            {
                if ((string)d1["window"]["LanguageName"] == "English") return -1;
                if ((string)d2["window"]["LanguageName"] == "English") return 1;
                else return 0;
            }));
            foreach (var lang in Languages)
            {
                var item = new ComboBoxItem() { Content = lang["window"]["LanguageName"] };
                languageSelect.Items.Add(item);
            }
            languageSelect.SelectedIndex = 0;
            if (omnimidiLoader != null)
                omnimidiLoader.Wait();
        }

        void InitialiseSettingsValues()
        {
            ReloadPlugins();
        }

        Task renderThread = null;

        void ReloadPlugins()
        {
            previewImage.Source = null;
            pluginDescription.Text = "";

            RenderModules.Clear();
            ModuleRunner.ClearModules();

            RenderModules.Clear();
            var files = Directory.GetFiles("Plugins");
            var libfiles = Directory.GetFiles("lib").Select(p => Path.GetFileName(p));
            var dlls = files.Where((s) => s.EndsWith(".dll"));
            foreach (var d in dlls)
            {
                if (libfiles.Contains(Path.GetFileName(d)))
                {
                    File.Delete(d);
                    continue;
                }
                try
                {
                    RenderModules.Add(ModuleManager.LoadModule(d));
                }
#if DEBUG
                catch (ModuleLoadFailedException e)
#else
                catch (Exception e)
#endif
                {
                    MessageBox.Show(e.Message);
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
                    SelectModule(i);
                    return;
                }
                i++;
            }
            if (RenderModules.Count > 0) SelectModule(0);
        }

        public void SelectModule(string name)
        {
            int instant = -1;
            int close = -1;
            int far = -1;

            for(int i = 0; i < RenderModules.Count; i++)
            {
                var n = RenderModules[RenderModules.Count - i - 1].Name;
                if(n == name)
                    instant = i;
                if (n.ToLower() == name.ToLower())
                    close = i;
                if (n.ToLower().Replace(" ", "") == name.ToLower().Replace(" ", ""))
                    far = i;
            }
            if (instant != -1) SelectModule(instant);
            else if (close != -1) SelectModule(close);
            else if (far != -1) SelectModule(far);
            else MessageBox.Show($"Could not find module with name similar to {name}");
        }

        public void SelectModule(int id)
        {
            pluginControl = null;
            if (id == -1)
            {
                ModuleRunner.ClearModules();
                return;
            }
            pluginsList.SelectedIndex = id;
            ModuleRunner.UseModule(RenderModules[id]);
            var module = ModuleRunner.CurrentModule;
            previewImage.Source = module.PreviewImage;
            pluginDescription.Text = module.Description;

            var c = module.SettingsControl;
            if (c == null) return;
            pluginsSettings.Children.Clear();
            pluginsSettings.Children.Add(c);
            c.VerticalAlignment = VerticalAlignment.Stretch;
            c.HorizontalAlignment = HorizontalAlignment.Stretch;
            c.Width = double.NaN;
            c.Height = double.NaN;
            c.Margin = new Thickness(0);
            pluginControl = c;
            if (languageSelect.SelectedIndex != -1 && Languages[languageSelect.SelectedIndex].ContainsKey(module.LanguageDictName))
            {
                c.Resources.MergedDictionaries.Clear();
                c.Resources.MergedDictionaries.Add(Languages[0][module.LanguageDictName]);
                c.Resources.MergedDictionaries.Add(Languages[languageSelect.SelectedIndex][module.LanguageDictName]);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var open = new OpenFileDialog();
            open.Filter = "Midi files (*.mid)|*.mid";
            if ((bool)open.ShowDialog())
            {
                LoadMidi(open.FileName);
            }
            else return;

        }

        private void UnloadButton_Click(object sender, RoutedEventArgs e)
        {
            UnloadMidi();
        }

        void SetPipelineValues()
        {
            if (ActivePipeline == null) return;
            if (!ActivePipeline.Rendering)
            {
                ActivePipeline.Paused = previewPaused.IsChecked;
                ActivePipeline.PreviewSpeed = tempoMultSlider.Value;
                ActivePipeline.VSync = vsyncEnabled.IsChecked;
                ActivePipeline.Status.RealtimePlayback = realtimePlayback.IsChecked;
            }
            else
            {
                ActivePipeline.Paused = false;
                ActivePipeline.PreviewSpeed = 1;
                ActivePipeline.VSync = false;
                ActivePipeline.Status.RealtimePlayback = false;
            }
            ActivePipeline.Status.FPS = (int)viewFps.Value;
        }

        void SetCustomFFMPEGOptions()
        {
            if (!IsInitialized) return;
            if (customFFmpegArgs.IsChecked) return;
            if (crfOption.IsChecked)
            {
                ffmpegOptions.Text = $"-vf vflip -pix_fmt yuv420p -vcodec libx264 -crf {crfFactor.Value}";
            }
            if (bitrateOption.IsChecked)
            {
                ffmpegOptions.Text = $"-vf vflip -pix_fmt yuv420p -vcodec libx264 -b:v {bitrate.Value}";
            }
        }

        public void StartPipeline(bool render)
        {
            var timeBased = noteSizeStyle.SelectedIndex == 1;
            // var startOffset = midifile.StartTicksToSeconds(ModuleRunner.CurrentModule.StartOffset, timeBased) +
            //                (render ? (double)secondsDelay.Value : 0);
            var startOffset = 0;


            var playback = midifile.GetMidiPlayback(
                startOffset,
                timeBased
            );

            CurrentRenderStatus = new RenderStatus((int)viewWidth.Value, (int)viewHeight.Value, (int)SSAAFactor.Value);

            if (render)
            {
                var ffmpegArgs = ffmpegOptions.Text;
                if (includeAudio.IsChecked)
                {
                    ffmpegArgs = $"-itsoffset {startOffset.ToString().Replace(",", ".")} -i \"{audioPath.Text}\" -acodec aac {ffmpegArgs}";
                }
                var args = new OutputSettings(ffmpegArgs, videoPath.Text, alphaPath.Text != "" ? alphaPath.Text : null);
                ActivePipeline = new RenderPipeline(CurrentRenderStatus, playback, ModuleRunner, args);
            }
            else
            {
                ActivePipeline = new RenderPipeline(CurrentRenderStatus, playback, ModuleRunner);
            }

            SetPipelineValues();
            ActivePipeline.Start();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartPipeline(false);
        }

        private void StartRenderButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoPath.Text == "")
            {
                MessageBox.Show("Please specify a destination path");
                return;
            }

            if (File.Exists(videoPath.Text))
            {
                if (MessageBox.Show("Are you sure you want to override " + Path.GetFileName(videoPath.Text), "Override", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;
            }
            if (includeAlpha.IsChecked)
            {
                if (File.Exists(alphaPath.Text))
                {
                    if (MessageBox.Show("Are you sure you want to override " + Path.GetFileName(alphaPath.Text), "Override", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        return;
                }
                if (alphaPath.Text == "")
                {
                    MessageBox.Show("Please specify transparency mask destination path");
                    return;
                }
            }
            if (includeAudio.IsChecked)
            {
                if (audioPath.Text == "")
                {
                    MessageBox.Show("Please specify audio source path");
                    return;
                }
            }

            StartPipeline(true);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActivePipeline != null)
            {
                ActivePipeline.Dispose();
                ActivePipeline = null;
            }
        }

        private void BrowseVideoSaveButton_Click(object sender, RoutedEventArgs e)
        {
            var save = new SaveFileDialog();
            save.OverwritePrompt = true;
            save.Filter = "H.264 video (*.mp4)|*.mp4|All types|*.*";
            if ((bool)save.ShowDialog())
            {
                videoPath.Text = save.FileName;
            }
        }

        private void BrowseAudioButton_Click(object sender, RoutedEventArgs e)
        {
            var audio = new OpenFileDialog();
            audio.Filter = "Common audio files (*.mp3;*.wav;*.ogg;*.flac)|*.mp3;*.wav;*.ogg;*.flac";
            if ((bool)audio.ShowDialog())
            {
                audioPath.Text = audio.FileName;
            }
        }

        private void BrowseAlphaButton_Click(object sender, RoutedEventArgs e)
        {
            var save = new SaveFileDialog();
            save.OverwritePrompt = true;
            save.Filter = "H.264 video (*.mp4)|*.mp4";
            if ((bool)save.ShowDialog())
            {
                alphaPath.Text = save.FileName;
            }
        }

        private void Paused_Checked(object sender, RoutedEventArgs e)
        {
            SetPipelineValues();
        }

        private void VsyncEnabled_Checked(object sender, RoutedEventArgs e)
        {
            SetPipelineValues();
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Space)
            //{
            //    previewPaused.IsChecked = !settings.Paused;
            //    settings.Paused = (bool)previewPaused.IsChecked;
            //}
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadPlugins();
        }

        private void PluginsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectModule(pluginsList.SelectedIndex);
        }

        private void ResolutionPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string preset = (string)((ComboBoxItem)resolutionPreset.SelectedItem).Content;
            switch (preset)
            {
                case "720p":
                    viewWidth.Value = 1280;
                    viewHeight.Value = 720;
                    break;
                case "1080p":
                    viewWidth.Value = 1920;
                    viewHeight.Value = 1080;
                    break;
                case "1440p":
                    viewWidth.Value = 2560;
                    viewHeight.Value = 1440;
                    break;
                case "4k":
                    viewWidth.Value = 3840;
                    viewHeight.Value = 2160;
                    break;
                case "5k":
                    viewWidth.Value = 5120;
                    viewHeight.Value = 2880;
                    break;
                case "8k":
                    viewWidth.Value = 7680;
                    viewHeight.Value = 4320;
                    break;
                case "16k":
                    viewWidth.Value = 15360;
                    viewHeight.Value = 8640;
                    break;
                default:
                    break;
            }
        }

        private void LanguageSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var renderer = ModuleRunner.CurrentModule;
            if (renderer != null)
            {
                pluginControl.Resources.MergedDictionaries.Clear();
                pluginControl.Resources.MergedDictionaries.Add(Languages[0][renderer.LanguageDictName]);
                pluginControl.Resources.MergedDictionaries.Add(Languages[languageSelect.SelectedIndex][renderer.LanguageDictName]);
            }
            Resources.MergedDictionaries[0].MergedDictionaries.Clear();
            Resources.MergedDictionaries[0].MergedDictionaries.Add(Languages[0]["window"]);
            Resources.MergedDictionaries[0].MergedDictionaries.Add(Languages[languageSelect.SelectedIndex]["window"]);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (foundOmniMIDI)
                KDMAPIOutput.Terminate();
        }

        private void Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            //if (settings == null) return;
            //if (sender == realtimePlayback) settings.RealtimePlayback = (bool)realtimePlayback.IsChecked;
        }

        private void DisableKDMAPI_Click(object sender, RoutedEventArgs e)
        {
            //if (OmniMIDIDisabled)
            //{
            //    disableKDMAPI.Content = Resources["disableKDMAPI"];
            //    OmniMIDIDisabled = false;
            //    settings.PreviewAudioEnabled = true;
            //    try
            //    {
            //        Console.WriteLine("Loading KDMAPI...");
            //        KDMAPI.InitializeKDMAPIStream();
            //        Console.WriteLine("Loaded!");
            //    }
            //    catch { }
            //}
            //else
            //{
            //    disableKDMAPI.Content = Resources["enableKDMAPI"];
            //    OmniMIDIDisabled = true;
            //    settings.PreviewAudioEnabled = false;
            //    try
            //    {
            //        Console.WriteLine("Unloading KDMAPI");
            //        KDMAPI.TerminateKDMAPIStream();
            //    }
            //    catch { }
            //}
        }

        private void NoteSizeStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (settings == null) return;
            //if (noteSizeStyle.SelectedIndex == 0) settings.TimeBased = false;
            //if (noteSizeStyle.SelectedIndex == 1) settings.TimeBased = true;
        }

        private void IgnoreColorEvents_Checked(object sender, RoutedEventArgs e)
        {
            //if (settings == null) return;
            //settings.IgnoreColorEvents = (bool)ignoreColorEvents.IsChecked;
        }

        private void UseBGImage_Checked(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    if (useBGImage.IsChecked && bgImagePath.Text != "")
            //    {
            //        settings.BGImage = bgImagePath.Text;
            //    }
            //    else
            //    {
            //        settings.BGImage = null;
            //    }
            //    settings.LastBGChangeTime = DateTime.Now.Ticks;
            //}
            //catch { }
        }

        private void BrowseBG_Click(object sender, RoutedEventArgs e)
        {
            //var open = new OpenFileDialog();
            //open.Filter = "Image files |*.png;*.bmp;*.jpg;*.jpeg";
            //if ((bool)open.ShowDialog())
            //{
            //    bgImagePath.Text = open.FileName;
            //    try
            //    {
            //        settings.BGImage = bgImagePath.Text;
            //    }
            //    catch
            //    {
            //        settings.BGImage = null;
            //    }
            //    settings.LastBGChangeTime = DateTime.Now.Ticks;
            //}
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // if (e.Key == Key.Space) settings.Paused = !settings.Paused;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimiseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
            }
            catch { }
            WindowState = WindowState.Minimized;
        }

        private void tempoMultSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetPipelineValues();
        }

        private void updateDownloaded_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ZenithUpdates.KillAllProcesses();
            Process.Start(ZenithUpdates.InstallerPath, "update -Reopen");
            Close();
        }

        private void RealtimePreview_Checked(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            SetPipelineValues();
        }

        private void VsyncEnabled_Checked(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            SetPipelineValues();
        }

        private void viewFps_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            SetPipelineValues();
        }

        private void includeAlpha_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (alphaPath.Text == "" && videoPath.Text != "")
            {
                alphaPath.Text = Path.Combine(
                        Path.GetDirectoryName(videoPath.Text),
                        Path.GetFileNameWithoutExtension(videoPath.Text) +
                        ".mask" +
                        Path.GetExtension(videoPath.Text)
                    );
            }
        }

        private void outputTypeChanged(object sender, RoutedEventArgs e)
        {
            SetCustomFFMPEGOptions();
        }

        private void crfFactor_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            SetCustomFFMPEGOptions();
        }

        private void bitrate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            SetCustomFFMPEGOptions();
        }
    }

    public class CustomTabs : TabControl
    {
        public UpdateProgress UpdaterProgress
        {
            get { return (UpdateProgress)GetValue(UpdaterProgressProperty); }
            set { SetValue(UpdaterProgressProperty, value); }
        }

        public static readonly DependencyProperty UpdaterProgressProperty =
            DependencyProperty.Register("UpdaterProgress", typeof(UpdateProgress), typeof(CustomTabs), new PropertyMetadata(UpdateProgress.NotDownloading));


        public string VersionName
        {
            get { return (string)GetValue(VersionNameProperty); }
            set { SetValue(VersionNameProperty, value); }
        }

        public static readonly DependencyProperty VersionNameProperty =
            DependencyProperty.Register("VersionName", typeof(string), typeof(CustomTabs), new PropertyMetadata(""));
    }

    public class AndValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = true;
            for (int i = 0; i < values.Length; i++) b = b && (bool)values[i];

            return b;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OrValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = false;
            for (int i = 0; i < values.Length; i++) b = b || (bool)values[i];

            return b;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotValueConverter : IMultiValueConverter, IValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Convert(values[0], targetType, parameter, culture);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
