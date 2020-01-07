using BMEngine;
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

namespace Black_Midi_Render
{
    class CurrentRendererPointer
    {
        public Queue<IPluginRender> disposeQueue = new Queue<IPluginRender>();
        public IPluginRender renderer = null;
    }

    public partial class MainWindow : Window
    {
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

        public MainWindow()
        {
            InitializeComponent();

            dynamic sett = JsonConvert.DeserializeObject(File.ReadAllText("settings.json"));
            if (sett.defaultBackground != "")
            {
                try
                {
                    bgImagePath.Text = sett.defaultBackground;
                    settings.BGImage = new Bitmap(bgImagePath.Text);
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
            JArray size = sett.settingsWindowSize;
            Width = (double)size[0];
            Height = (double)size[1];

            Task omnimidiLoader = null;
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
                useOmniMidi.IsChecked = false;
                useOmniMidi.IsEnabled = false;
                muteAudio.IsChecked = false;
                muteAudio.IsEnabled = false;
            }
            settings = new RenderSettings();
            settings.PauseToggled += ToggledPause;
            InitialiseSettingsValues();
            creditText.Text = "Video was rendered with Zenith\nhttps://arduano.github.io/Zenith-MIDI/start";

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
                omnimidiLoader.GetAwaiter().GetResult();
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
            tempoSlider.Value = Math.Log(settings.tempoMultiplier, 2);

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
                    return (midifile.ParseUpTo((win.midiTime + win.lastDeltaTimeOnScreen +
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
                            Math.Round(progress * 10000) / 100 +
                            "\tNotes drawn: " + renderer.renderer.LastNoteCount +
                            "\tRender FPS: " + Math.Round(settings.liveFps) + "        "
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
                        s.ElapsedMilliseconds > 1000.0 / settings.fps * 10 ||
                        win.midiTime + win.lastDeltaTimeOnScreen + (win.tempoFrameStep * 10 * settings.tempoMultiplier * win.lastMV) > midifile.currentSyncTime ||
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
            if (languageSelect.SelectedIndex != -1)
                c.Resources.MergedDictionaries.Add(Languages[languageSelect.SelectedIndex][RenderPlugins[id].LanguageDictName]);
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
            settings.playbackEnabled = (bool)useOmniMidi.IsChecked;
            settings.playSound = !(bool)muteAudio.IsChecked;

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
            settings.playbackEnabled = false;
            settings.playSound = true;

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
            tempoSlider.Value = 0;

            settings.ffmpegDebug = (bool)ffdebug.IsChecked;

            settings.useBitrate = (bool)bitrateOption.IsChecked;
            if (settings.useBitrate) settings.bitrate = (int)bitrate.Value;
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
            save.Filter = "H.264 video (*.mp4)|*.mp4";
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
            try
            {
                settings.vsync = (bool)vsyncEnabled.IsChecked;
            }
            catch { }
        }

        private void TempoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                settings.tempoMultiplier = Math.Pow(2, tempoSlider.Value);
                tempoValue.Content = Math.Round(settings.tempoMultiplier * 100) / 100;
            }
            catch (NullReferenceException)
            {

            }
        }

        private void ForceReRender_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.forceReRender = (bool)forceReRender.IsChecked;
            }
            catch { }
        }

        private void TempoSlider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (e.RightButton == MouseButtonState.Pressed)
            //{
            //    previewPaused.IsChecked = !settings.Paused;
            //    settings.Paused = (bool)previewPaused.IsChecked;
            //}
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
                    ((UserControl)pluginControl).Resources.MergedDictionaries[0].MergedDictionaries.Add(Languages[languageSelect.SelectedIndex][renderer.renderer.LanguageDictName]);
                }
            Resources.MergedDictionaries[0].MergedDictionaries.Clear();
            Resources.MergedDictionaries[0].MergedDictionaries.Add(Languages[languageSelect.SelectedIndex]["window"]);
        }

        private void RadioChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                RadioButton[] buttons = new RadioButton[] { bitrateOption, crfOption };
                foreach (var b in buttons) if (b != sender) b.IsChecked = false;
            }
            catch { }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (foundOmniMIDI)
                KDMAPI.TerminateKDMAPIStream();
        }

        private void Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender == useOmniMidi)
                {
                    settings.playbackEnabled = (bool)useOmniMidi.IsChecked;
                    if (!settings.playbackEnabled) midifile.globalPlaybackEvents.Unlink();
                }
                if (sender == muteAudio) settings.playSound = !(bool)muteAudio.IsChecked;
                if (sender == realtimePlayback) settings.realtimePlayback = (bool)realtimePlayback.IsChecked;
            }
            catch { }
        }

        private void DisableKDMAPI_Click(object sender, RoutedEventArgs e)
        {
            if (OmniMIDIDisabled)
            {
                disableKDMAPI.Content = Resources["disableKDMAPI"];
                OmniMIDIDisabled = false;
                useOmniMidi.IsEnabled = true;
                useOmniMidi.IsChecked = true;
                muteAudio.IsEnabled = true;
                muteAudio.IsChecked = false;
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
                useOmniMidi.IsChecked = false;
                useOmniMidi.IsEnabled = false;
                muteAudio.IsChecked = false;
                muteAudio.IsEnabled = false;
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
            try
            {
                if (noteSizeStyle.SelectedIndex == 0) settings.timeBasedNotes = false;
                if (noteSizeStyle.SelectedIndex == 1) settings.timeBasedNotes = true;
            }
            catch { }
        }

        private void IgnoreColorEvents_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.ignoreColorEvents = (bool)ignoreColorEvents.IsChecked;
            }
            catch { }
        }

        private void UseBGImage_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.lastyBGChangeTime = DateTime.Now.Ticks;
                if ((bool)useBGImage.IsChecked)
                {
                    try
                    {
                        settings.BGImage = new Bitmap(bgImagePath.Text);
                    }
                    catch
                    {
                        settings.BGImage = null;
                        if (bgImagePath.Text != "")
                            MessageBox.Show("Couldn't load image");
                    }
                }
                else
                {
                    settings.BGImage = null;
                }
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
                    settings.BGImage = new Bitmap(bgImagePath.Text);
                }
                catch
                {
                    settings.BGImage = null;
                    if (bgImagePath.Text != "")
                        MessageBox.Show("Couldn't load image");
                }
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
