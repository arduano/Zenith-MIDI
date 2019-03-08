using BMEngine;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        List<IPluginRender> RenderPlugins = new List<IPluginRender>();

        CurrentRendererPointer renderer = new CurrentRendererPointer();

        public MainWindow()
        {
            InitializeComponent();
            settings = new RenderSettings();
            InitialiseSettingsValues();
            creditText.Text = "Video was rendered by Zenith\n>> https://arduano.github.io/Zenith-MIDI/start";
        }

        void InitialiseSettingsValues()
        {
            maxBufferSize.Value = settings.maxTrackBufferSize;
            viewWidth.Value = settings.width;
            viewHeight.Value = settings.height;
            viewFps.Value = settings.fps;
            vsyncEnabled.IsChecked = settings.vsync;
            tempoSlider.Value = Math.Log(settings.tempoMultiplier, 2);
            //fontSizePicker.Value = settings.fontSize;
            //showNoteCount.IsChecked = settings.showNoteCount;
            //showNoteScreenCount.IsChecked = settings.showNotesRendered;
            //var fonts = Fonts.GetFontFamilies("C:\\Windows\\Fonts");
            //fontPicker.Items.Clear();
            //foreach (var f in fonts)
            //{
            //    try
            //    {
            //        foreach (var k in f.FamilyNames.Keys.Where((a) => a.IetfLanguageTag.Contains("en")))
            //        {
            //            var item = new ComboBoxItem() { Content = f.FamilyNames[k] };
            //            fontPicker.Items.Add(item);
            //            if (settings.font == f.FamilyNames[k]) fontPicker.SelectedItem = item;
            //        }
            //    }
            //    catch { }
            //}
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
            long time = 0;
            int nc = -1;
            long maxRam = 0;
            long avgRam = 0;
            long ramSample = 0;
            Stopwatch timewatch = new Stopwatch();
            timewatch.Start();
            try
            {
                while ((midifile.ParseUpTo(time += (long)(win.tempoFrameStep * 10)) || nc != 0) && settings.running)
                {
                    SpinWait.SpinUntil(() => midifile.currentSyncTime < win.midiTime + win.lastDeltaTimeOnScreen + (long)(win.tempoFrameStep * 10) || !settings.running);
                    if (!settings.running) break;
                    Note n;
                    long cutoffTime = (long)win.midiTime;
                    bool manualDelete = false;
                    bool receivedInfo = false;
                    while (!receivedInfo)
                        try
                        {
                            manualDelete = renderer.renderer.ManualNoteDelete;
                            receivedInfo = true;
                        }
                        catch
                        { }
                    lock (midifile.globalDisplayNotes)
                    {
                        var i = midifile.globalDisplayNotes.Iterate();
                        if (manualDelete)
                            while (i.MoveNext(out n))
                            {
                                if (n.delete)
                                    i.Remove();
                                else nc++;
                            }
                        else
                            while (i.MoveNext(out n))
                            {
                                if (n.hasEnded && n.end < cutoffTime)
                                    i.Remove();
                                if (n.start > cutoffTime) break;
                            }
                    }
                    try
                    {
                        Console.WriteLine(
                            Math.Round((double)time / midifile.maxTrackTime * 10000) / 100 +
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while opeining render window. Please try again.\n\n" + ex.Message + "\n" + ex.StackTrace);
                settings.running = false;
            }
            winthread.GetAwaiter().GetResult();
            settings.running = false;
            midifile.Reset();
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
            if (id == -1)
            {
                renderer.renderer = null;
                return;
            }
            pluginsList.SelectedIndex = id;
            renderer.renderer = RenderPlugins[id];
            previewImage.Source = renderer.renderer.PreviewImage;
            pluginDescription.Text = renderer.renderer.Description;

            var c = renderer.renderer.SettingsControl;
            if (c == null) return;
            if (c.Parent != null)
                (c.Parent as Panel).Children.Clear();
            pluginsSettings.Children.Add(c);
            c.Margin = new Thickness(0);
            c.VerticalAlignment = VerticalAlignment.Top;
            c.HorizontalAlignment = HorizontalAlignment.Left;
        }

        private void BrowseMidiButton_Click(object sender, RoutedEventArgs e)
        {
            var open = new OpenFileDialog();
            open.Filter = "Midi files (*.mid)|*.mid";
            if ((bool)open.ShowDialog())
            {
                midiPath.Text = open.FileName;
                midipath = open.FileName;
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(midipath))
            {
                MessageBox.Show("Midi file doesn't exist");
                return;
            }
#if !DEBUG_LOAD
            try
            {
#endif
            settings.maxTrackBufferSize = (int)maxBufferSize.Value;

            if (midifile != null) midifile.Dispose();
            midifile = null;
            GC.Collect();
            GC.WaitForFullGCComplete();
            midifile = new MidiFile(midipath, settings);
            Resources["midiLoaded"] = true;
#if !DEBUG_LOAD
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
#endif
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

            settings.running = true;
            settings.width = (int)viewWidth.Value;
            settings.height = (int)viewHeight.Value;
            settings.fps = (int)viewFps.Value;
            settings.ffRender = false;
            settings.renderSecondsDelay = 0;
            renderThread = Task.Factory.StartNew(RunRenderWindow);
            Resources["notPreviewing"] = false;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
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

            settings.running = true;
            settings.width = (int)viewWidth.Value;
            settings.height = (int)viewHeight.Value;
            settings.fps = (int)viewFps.Value;
            settings.ffRender = true;
            settings.ffPath = videoPath.Text;
            settings.renderSecondsDelay = (double)secondsDelay.Value;

            settings.paused = false;
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

        private void Paused_Checked(object sender, RoutedEventArgs e)
        {
            settings.paused = (bool)previewPaused.IsChecked;
        }

        private void VsyncEnabled_Checked(object sender, RoutedEventArgs e)
        {
            settings.vsync = (bool)vsyncEnabled.IsChecked;
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
            settings.forceReRender = (bool)forceReRender.IsChecked;
        }

        private void TempoSlider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                previewPaused.IsChecked = !settings.paused;
                settings.paused = (bool)previewPaused.IsChecked;
            }
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                previewPaused.IsChecked = !settings.paused;
                settings.paused = (bool)previewPaused.IsChecked;
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

        //private void ShowNoteScreenCount_Checked(object sender, RoutedEventArgs e)
        //{
        //    settings.showNotesRendered = (bool)showNoteScreenCount.IsChecked;
        //}

        //private void ShowNoteCount_Checked(object sender, RoutedEventArgs e)
        //{
        //    settings.showNoteCount = (bool)showNoteCount.IsChecked;
        //}

        //private void FontSizePicker_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        //{
        //    try
        //    {
        //        settings.fontSize = (int)fontSizePicker.Value;
        //    }
        //    catch { }
        //}

        //private void FontPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    settings.font = (string)((ComboBoxItem)fontPicker.SelectedItem).Content;
        //}
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
