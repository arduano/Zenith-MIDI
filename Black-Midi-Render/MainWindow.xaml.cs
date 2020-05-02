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

namespace Zenith_MIDI
{
    class CurrentRendererPointer
    {
        public Queue<IPluginRender> disposeQueue = new Queue<IPluginRender>();
        public IPluginRender renderer = null;
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
            /// <summary>x coordinate of point.</summary>
            public int x;
            /// <summary>y coordinate of point.</summary>
            public int y;
            /// <summary>Construct a point of coordinates (x,y).</summary>
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

        RenderSettings settings;
        MidiFile midifile = null;
        string midipath = "";

        Control pluginControl = null;

        List<IPluginRender> RenderPlugins = new List<IPluginRender>();

        CurrentRendererPointer renderer = new CurrentRendererPointer();

        List<Dictionary<string, ResourceDictionary>> Languages = new List<Dictionary<string, ResourceDictionary>>();

        bool foundOmniMIDI = true;
        bool OmniMIDIDisabled = false;

        string defaultPlugin = "Classic";

        Settings metaSettings = new Settings();

        void RunLanguageCheck()
        {
            string ver;
            try
            {
                ver = ZenithLanguages.GetLatestVersion();
            }
            catch { return; }
            if (ver != metaSettings.LanguagesVersion)
            {
                try
                {
                    Console.WriteLine("Update found for language packs, downloading...");
                    var latest = ZenithLanguages.DownloadLatestVersion();
                    ZenithLanguages.UnpackFromStream(latest);
                    metaSettings.LanguagesVersion = ver;
                    metaSettings.SaveConfig();
                    Console.WriteLine("Updated language packs!");
                }
                catch { Console.WriteLine("Failed to update language packs"); }
            }
        }

        void RunUpdateCheck()
        {
            if (!metaSettings.AutoUpdate) return;

            var requiredInstaller = ZenithUpdates.InstallerVer;
            if (metaSettings.InstallerVer != requiredInstaller)
            {
                Console.WriteLine("Important update found for installer, updating...");
                ZenithUpdates.UpdateInstaller();
                metaSettings.InstallerVer = requiredInstaller;
                metaSettings.SaveConfig();
            }

            string ver;
            try
            {
                ver = ZenithUpdates.GetLatestVersion();
            }
            catch { return; }
            if (ver != metaSettings.VersionName)
            {
                Console.WriteLine("Found Update! Current: " + metaSettings.VersionName + " Latest: " + ver);
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
            if(metaSettings.PreviousVersion != metaSettings.VersionName)
            {
                if (File.Exists("settings.json")) File.Delete("settings.json");
                metaSettings.PreviousVersion = metaSettings.VersionName;
                metaSettings.SaveConfig();
            }

            if (metaSettings.AutoUpdate)
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

        public MainWindow()
        {
            CheckUpdateDownloaded();

            InitializeComponent();

            windowTabs.VersionName = metaSettings.VersionName;

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
                        settings.BGImage = bgImagePath.Text;
                    }
                    catch
                    {
                        settings.BGImage = null;
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
                        KDMAPI.InitializeKDMAPIStream();
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
            settings = new RenderSettings();
            settings.PauseToggled += ToggledPause;
            InitialiseSettingsValues();
            creditText.Text = "Video was rendered with Zenith\nhttps://arduano.github.io/Zenith-MIDI/start";

            if(languageLoader != null) languageLoader.Wait();

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

        void ToggledPause()
        {
            Dispatcher.Invoke(() =>
            {
                if ((bool)previewPaused.IsChecked ^ settings.Paused)
                {
                    previewPaused.IsChecked = settings.Paused;
                }
            });

        }

        void InitialiseSettingsValues()
        {
            viewWidth.Value = settings.width;
            viewHeight.Value = settings.height;
            viewFps.Value = settings.fps;
            vsyncEnabled.IsChecked = settings.vsync;
            tempoMultSlider.Value = settings.tempoMultiplier;

            ReloadPlugins();
        }

        Task renderThread = null;
        RenderWindow win = null;
        void RunRenderWindow()
        {
            bool winStarted = false;
            Task winthread = new Task(() =>
            {
                win = new RenderWindow(renderer, midifile, settings);
                winStarted = true;
                win.Run();
            });
            winthread.Start();
            SpinWait.SpinUntil(() => winStarted);
            double time = 0;
            int nc = -1;
            long maxRam = 0;
            long avgRam = 0;
            long ramSample = 0;
            Stopwatch timewatch = new Stopwatch();
            timewatch.Start();
            IPluginRender render = null;
            double lastWinTime = double.NaN;
            bool tryToParse()
            {
                lock (midifile)
                {
                    return (midifile.ParseUpTo(
                        (win.midiTime + win.lastDeltaTimeOnScreen +
                        (win.tempoFrameStep * 20 * settings.tempoMultiplier * (win.lastMV > 1 ? win.lastMV : 1))))
                        || nc != 0) && settings.running;
                }
            }
            try
            {
                while (tryToParse())
                {
                    //SpinWait.SpinUntil(() => lastWinTime != win.midiTime || render != renderer.renderer || !settings.running);
                    if (!settings.running) break;
                    Note n;
                    double cutoffTime = win.midiTime;
                    bool manualDelete = false;
                    double noteCollectorOffset = 0;
                    bool receivedInfo = false;
                    while (!receivedInfo)
                        try
                        {
                            render = renderer.renderer;
                            receivedInfo = true;
                        }
                        catch
                        { }
                    manualDelete = render.ManualNoteDelete;
                    noteCollectorOffset = render.NoteCollectorOffset;
                    cutoffTime += noteCollectorOffset;
                    if (!settings.running) break;
                    lock (midifile.globalDisplayNotes)
                    {
                        var i = midifile.globalDisplayNotes.Iterate();
                        if (manualDelete)
                            while (i.MoveNext(out n))
                            {
                                if (n.delete)
                                    i.Remove();
                                else
                                    nc++;
                            }
                        else
                            while (i.MoveNext(out n))
                            {
                                if (n.hasEnded && n.end < cutoffTime)
                                    i.Remove();
                                if (n.start > cutoffTime) break;
                            }
                        GC.Collect();
                    }
                    try
                    {
                        double progress = win.midiTime / midifile.maxTrackTime;
                        if (settings.timeBasedNotes) progress = win.midiTime / 1000 / midifile.info.secondsLength;
                        Console.WriteLine(
                            new TimeSpan(0, 0, (int)(timewatch.ElapsedMilliseconds / 1000)) +
                            " " + Math.Round(progress * 10000) / 100 +
                            "%\tRender FPS: " + Math.Round(settings.liveFps) +
                            "\tNotes drawn: " + renderer.renderer.LastNoteCount
                            );
                    }
                    catch
                    {
                    }
                    long ram = Process.GetCurrentProcess().PrivateMemorySize64;
                    if (maxRam < ram) maxRam = ram;
                    avgRam = (long)((double)avgRam * ramSample + ram) / (ramSample + 1);
                    ramSample++;
                    lastWinTime = win.midiTime;
                    Stopwatch s = new Stopwatch();
                    s.Start();
                    SpinWait.SpinUntil(() =>
                    (
                        (s.ElapsedMilliseconds > 1000.0 / settings.fps * 30 && false) ||
                        (win.midiTime + win.lastDeltaTimeOnScreen +
                        (win.tempoFrameStep * 10 * settings.tempoMultiplier * (win.lastMV > 1 ? win.lastMV : 1))) > midifile.currentSyncTime ||
                        lastWinTime != win.midiTime || render != renderer.renderer || !settings.running
                    )
                    ); ;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while opeining render window. Please try again.\n\n" + ex.Message + "\n" + ex.StackTrace);
                settings.running = false;
            }
            winthread.GetAwaiter().GetResult();
            settings.running = false;
            Console.WriteLine("Reset midi file");
            midifile.Reset();
            win.Dispose();
            win = null;
            GC.Collect();
            GC.WaitForFullGCComplete();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(
                    "Finished render\nRAM usage (Private bytes)\nPeak: " + Math.Round((double)maxRam / 1000 / 1000 / 1000 * 100) / 100 +
                    "GB\nAvg: " + Math.Round((double)avgRam / 1000 / 1000 / 1000 * 100) / 100 +
                    "GB\nMinutes to render: " + Math.Round((double)timewatch.ElapsedMilliseconds / 1000 / 60 * 100) / 100);
            Console.ResetColor();
            Dispatcher.Invoke(() =>
            {
                Resources["notRendering"] = true;
                Resources["notPreviewing"] = true;
            });
        }

        void ReloadPlugins()
        {
            previewImage.Source = null;
            pluginDescription.Text = "";
            lock (renderer)
            {
                foreach (var p in RenderPlugins)
                {
                    if (p.Initialized) renderer.disposeQueue.Enqueue(p);
                }
                RenderPlugins.Clear();
                var files = Directory.GetFiles("Plugins");
                var dlls = files.Where((s) => s.EndsWith(".dll"));
                foreach (var d in dlls)
                {
                    try
                    {
                        var DLL = Assembly.UnsafeLoadFrom(System.IO.Path.GetFullPath(d));
                        bool hasClass = false;
                        var name = System.IO.Path.GetFileName(d);
                        try
                        {
                            foreach (Type type in DLL.GetExportedTypes())
                            {
                                if (type.Name == "Render")
                                {
                                    hasClass = true;
                                    var instance = (IPluginRender)Activator.CreateInstance(type, new object[] { settings });
                                    RenderPlugins.Add(instance);
                                    Console.WriteLine("Loaded " + name);
                                }
                            }
                            if (!hasClass)
                            {
                                MessageBox.Show("Could not load " + name + "\nDoesn't have render class");
                            }
                        }
                        catch (RuntimeBinderException)
                        {
                            MessageBox.Show("Could not load " + name + "\nA binding error occured");
                        }
                        catch (InvalidCastException)
                        {
                            MessageBox.Show("Could not load " + name + "\nThe Render class was not a compatible with the interface");
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("An error occured while binfing " + name + "\n" + e.Message);
                        }
                    }
                    catch { }
                }

                pluginsList.Items.Clear();
                for (int i = 0; i < RenderPlugins.Count; i++)
                {
                    pluginsList.Items.Add(new ListBoxItem() { Content = RenderPlugins[i].Name });
                }
                if (RenderPlugins.Count != 0)
                {
                    SelectRenderer(0);
                }
            }
        }

        void SelectRenderer(int id)
        {
            (pluginsSettings as Panel).Children.Clear();
            pluginControl = null;
            if (id == -1)
            {
                renderer.renderer = null;
                return;
            }
            pluginsList.SelectedIndex = id;
            lock (renderer)
            {
                renderer.renderer = RenderPlugins[id];
            }
            previewImage.Source = renderer.renderer.PreviewImage;
            pluginDescription.Text = renderer.renderer.Description;

            var c = renderer.renderer.SettingsControl;
            if (c == null) return;
            if (c.Parent != null)
                (c.Parent as Panel).Children.Clear();
            pluginsSettings.Children.Add(c);
            c.VerticalAlignment = VerticalAlignment.Stretch;
            c.HorizontalAlignment = HorizontalAlignment.Stretch;
            c.Width = double.NaN;
            c.Height = double.NaN;
            c.Margin = new Thickness(0);
            pluginControl = c;
            if (languageSelect.SelectedIndex != -1 && Languages[languageSelect.SelectedIndex].ContainsKey(renderer.renderer.LanguageDictName))
            {
                c.Resources.MergedDictionaries[0].MergedDictionaries.Clear();
                c.Resources.MergedDictionaries[0].MergedDictionaries.Add(Languages[0][renderer.renderer.LanguageDictName]);
                c.Resources.MergedDictionaries[0].MergedDictionaries.Add(Languages[languageSelect.SelectedIndex][renderer.renderer.LanguageDictName]);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var open = new OpenFileDialog();
            open.Filter = "Midi files (*.mid)|*.mid";
            if ((bool)open.ShowDialog())
            {
                midipath = open.FileName;
            }
            else return;

            if (!File.Exists(midipath))
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
                midifile = new MidiFile(midipath, settings);
                Resources["midiLoaded"] = true;
                browseMidiButton.Content = Path.GetFileName(midipath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private void UnloadButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Unloading midi");
            midifile.Dispose();
            midifile = null;
            GC.Collect();
            GC.WaitForFullGCComplete();
            Console.WriteLine("Unloaded");
            Resources["midiLoaded"] = false;
            browseMidiButton.SetResourceReference(Button.ContentProperty, "load");
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (renderer.renderer == null)
            {
                MessageBox.Show("No renderer is selected");
                return;
            }

            windowTabs.SelectedIndex = 4;

            settings.realtimePlayback = (bool)realtimePlayback.IsChecked;

            settings.running = true;
            settings.width = (int)viewWidth.Value * (int)SSAAFactor.Value;
            settings.height = (int)viewHeight.Value * (int)SSAAFactor.Value;
            settings.downscale = (int)SSAAFactor.Value;
            settings.fps = (int)viewFps.Value;
            settings.ffRender = false;
            settings.Paused = false;
            settings.renderSecondsDelay = 0;
            renderThread = Task.Factory.StartNew(RunRenderWindow, TaskCreationOptions.RunContinuationsAsynchronously | TaskCreationOptions.LongRunning);
            Resources["notPreviewing"] = false;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (settings.running == false)
            {
                Resources["notRendering"] = true;
                Resources["notPreviewing"] = true;
            }
            else
                settings.running = false;
        }

        private void StartRenderButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoPath.Text == "")
            {
                MessageBox.Show("Please specify a destination path");
                return;
            }

            if (renderer.renderer == null)
            {
                MessageBox.Show("No renderer is selected");
                return;
            }

            if (File.Exists(videoPath.Text))
            {
                if (MessageBox.Show("Are you sure you want to override " + Path.GetFileName(videoPath.Text), "Override", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;
            }
            if (File.Exists(alphaPath.Text))
            {
                if (MessageBox.Show("Are you sure you want to override " + Path.GetFileName(alphaPath.Text), "Override", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    return;
            }

            settings.realtimePlayback = false;

            settings.running = true;
            settings.width = (int)viewWidth.Value * (int)SSAAFactor.Value;
            settings.height = (int)viewHeight.Value * (int)SSAAFactor.Value;
            settings.downscale = (int)SSAAFactor.Value;
            settings.fps = (int)viewFps.Value;
            settings.ffRender = true;
            settings.ffPath = videoPath.Text;
            settings.renderSecondsDelay = (double)secondsDelay.Value;

            settings.Paused = false;
            previewPaused.IsChecked = false;
            settings.tempoMultiplier = 1;
            tempoMultSlider.Value = 1;

            settings.ffmpegDebug = (bool)ffdebug.IsChecked;

            settings.useBitrate = (bool)bitrateOption.IsChecked;
            settings.CustomFFmpeg = (bool)FFmpeg.IsChecked;
            if (settings.useBitrate) settings.bitrate = (int)bitrate.Value;
            else if (settings.CustomFFmpeg)
            {
                settings.ffoption = FFmpegOptions.Text;
            }
            else
            {
                settings.crf = (int)crfFactor.Value;
                settings.crfPreset = (string)((ComboBoxItem)crfPreset.SelectedItem).Content;
            }

            settings.includeAudio = (bool)includeAudio.IsChecked;
            settings.audioPath = audioPath.Text;
            settings.ffRenderMask = (bool)includeAlpha.IsChecked;
            settings.ffMaskPath = alphaPath.Text;
            renderThread = Task.Factory.StartNew(RunRenderWindow);
            Resources["notPreviewing"] = false;
            Resources["notRendering"] = false;
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
            settings.Paused = (bool)previewPaused.IsChecked;
        }

        private void VsyncEnabled_Checked(object sender, RoutedEventArgs e)
        {
            if (settings == null) return;
            settings.vsync = (bool)vsyncEnabled.IsChecked;
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                previewPaused.IsChecked = !settings.Paused;
                settings.Paused = (bool)previewPaused.IsChecked;
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadPlugins();
        }

        private void PluginsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var p in RenderPlugins)
            {
                if (p.Initialized) renderer.disposeQueue.Enqueue(p);
            }
            SelectRenderer(pluginsList.SelectedIndex);
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
            if (pluginControl != null)
                lock (renderer)
                {
                    ((UserControl)pluginControl).Resources.MergedDictionaries[0].MergedDictionaries.Clear();
                    ((UserControl)pluginControl).Resources.MergedDictionaries[0].MergedDictionaries.Add(Languages[0][renderer.renderer.LanguageDictName]);
                    ((UserControl)pluginControl).Resources.MergedDictionaries[0].MergedDictionaries.Add(Languages[languageSelect.SelectedIndex][renderer.renderer.LanguageDictName]);
                }
            Resources.MergedDictionaries[0].MergedDictionaries.Clear();
            Resources.MergedDictionaries[0].MergedDictionaries.Add(Languages[0]["window"]);
            Resources.MergedDictionaries[0].MergedDictionaries.Add(Languages[languageSelect.SelectedIndex]["window"]);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (foundOmniMIDI)
                KDMAPI.TerminateKDMAPIStream();
        }

        private void Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            if (settings == null) return;
            if (sender == realtimePlayback) settings.realtimePlayback = (bool)realtimePlayback.IsChecked;
        }

        private void DisableKDMAPI_Click(object sender, RoutedEventArgs e)
        {
            if (OmniMIDIDisabled)
            {
                disableKDMAPI.Content = Resources["disableKDMAPI"];
                OmniMIDIDisabled = false;
                settings.playbackEnabled = true;
                try
                {
                    Console.WriteLine("Loading KDMAPI...");
                    KDMAPI.InitializeKDMAPIStream();
                    Console.WriteLine("Loaded!");
                }
                catch { }
            }
            else
            {
                disableKDMAPI.Content = Resources["enableKDMAPI"];
                OmniMIDIDisabled = true;
                settings.playbackEnabled = false;
                try
                {
                    Console.WriteLine("Unloading KDMAPI");
                    KDMAPI.TerminateKDMAPIStream();
                }
                catch { }
            }
        }

        private void NoteSizeStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (settings == null) return;
            if (noteSizeStyle.SelectedIndex == 0) settings.timeBasedNotes = false;
            if (noteSizeStyle.SelectedIndex == 1) settings.timeBasedNotes = true;
        }

        private void IgnoreColorEvents_Checked(object sender, RoutedEventArgs e)
        {
            if (settings == null) return;
            settings.ignoreColorEvents = (bool)ignoreColorEvents.IsChecked;
        }

        private void UseBGImage_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (useBGImage.IsChecked && bgImagePath.Text != "")
                {
                    settings.BGImage = bgImagePath.Text;
                }
                else
                {
                    settings.BGImage = null;
                }
                settings.lastBGChangeTime = DateTime.Now.Ticks;
            }
            catch { }
        }

        private void BrowseBG_Click(object sender, RoutedEventArgs e)
        {
            var open = new OpenFileDialog();
            open.Filter = "Image files |*.png;*.bmp;*.jpg;*.jpeg";
            if ((bool)open.ShowDialog())
            {
                bgImagePath.Text = open.FileName;
                try
                {
                    settings.BGImage = bgImagePath.Text;
                }
                catch
                {
                    settings.BGImage = null;
                }
                settings.lastBGChangeTime = DateTime.Now.Ticks;
            }
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) settings.Paused = !settings.Paused;
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
            if (settings != null) settings.tempoMultiplier = tempoMultSlider.Value;
        }

        private void updateDownloaded_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ZenithUpdates.KillAllProcesses();
            Process.Start(ZenithUpdates.InstallerPath, "update -Reopen");
            Close();
        }

        private void StackPanel_DragOver(object sender, DragEventArgs e)
        {

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

    public class NotValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)values[0];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
