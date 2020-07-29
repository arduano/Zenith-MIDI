using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Text;
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
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpCompress;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Tar;
using ZenithEngine.IO;
using Brushes = System.Windows.Media.Brushes;
using Path = System.IO.Path;

namespace TexturedRender
{
    /// <summary>
    /// Interaction logic for SettingsCtrl.xaml
    /// </summary>

    class PackLocation
    {
        public string filename;
        public PackType type;
    }

    public partial class SettingsCtrl : UserControl
    {
        List<PackLocation> resourcePacks = new List<PackLocation>();
        Settings settings;

        public event Action PaletteChanged
        {
            add { paletteList.PaletteChanged += value; }
            remove { paletteList.PaletteChanged -= value; }
        }

        string packPath = "Plugins\\Assets\\Textured\\Resources";

        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        bool inited = false;
        public SettingsCtrl(Settings settings)
        {
            this.settings = settings;
            InitializeComponent();
            noteDeltaScreenTime.nudToSlider = v => Math.Log(v, 2);
            noteDeltaScreenTime.sliderToNud = v => Math.Pow(2, v);
            inited = true;
            paletteList.SetPath("Plugins\\Assets\\Palettes", 1f);
            ReloadPacks();
            SetValues();
        }

        void WriteDefaultPack()
        {
            string dir = Path.Combine(packPath, "Default");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            Properties.Resources.keyBlack.Save(dir + "\\keyBlack.png");
            Properties.Resources.keyBlackPressed.Save(dir + "\\keyBlackPressed.png");
            Properties.Resources.keyWhite.Save(dir + "\\keyWhite.png");
            Properties.Resources.keyWhitePressed.Save(dir + "\\keyWhitePressed.png");
            Properties.Resources.note.Save(dir + "\\note.png");
            Properties.Resources.bar.Save(dir + "\\bar.png");
            Properties.Resources.noteEdge.Save(dir + "\\noteEdge.png");
            Properties.Resources.preview.Save(dir + "\\preview.png");
            File.WriteAllBytes(dir + "\\pack.json", Properties.Resources.pack);
        }

        void ReloadPacks()
        {
            int lastSelected = pluginList.SelectedIndex;
            string lastSelectedName;
            if (lastSelected == -1)
            {
                lastSelectedName = "Default";
                lastSelected = 0;
            }
            else
            {
                lastSelectedName = (string)((ListBoxItem)pluginList.SelectedItem).Content;
            }

            string dir = packPath;

            resourcePacks.Clear();
            WriteDefaultPack();
            pluginList.Items.Clear();
            foreach (var p in Directory.GetDirectories(dir))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Folder });
            }
            foreach (var p in Directory.GetFiles(dir, "*.zip"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Zip });
            }
            foreach (var p in Directory.GetFiles(dir, "*.zrp"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Zrp });
            }
            foreach (var p in Directory.GetFiles(dir, "*.rar"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Rar });
            }
            foreach (var p in Directory.GetFiles(dir, "*.7z"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.SevenZip });
            }
            foreach (var p in Directory.GetFiles(dir, "*.tar"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Tar });
            }
            foreach (var p in Directory.GetFiles(dir, "*.tar.bz"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Tar });
            }
            foreach (var p in Directory.GetFiles(dir, "*.tar.gz"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Tar });
            }
            foreach (var p in Directory.GetFiles(dir, "*.tar.xz"))
            {
                resourcePacks.Add(new PackLocation() { filename = p, type = PackType.Tar });
            }

            resourcePacks.Sort((a, b) =>
            {
                return a.filename.CompareTo(b.filename);
            });

            foreach (var p in resourcePacks)
            {
                if (p.type == PackType.Folder)
                    pluginList.Items.Add(new ListBoxItem()
                    {
                        Content = p.filename.Split('\\').Last(),
                        Foreground = Brushes.White
                    });
                else
                    pluginList.Items.Add(new ListBoxItem()
                    {
                        Content = p.filename.Split('\\').Last(),
                        Foreground = Brushes.Green
                    });
            }

            if ((string)((ListBoxItem)pluginList.Items[lastSelected]).Content == lastSelectedName)
            {
                pluginList.SelectedIndex = lastSelected;
            }
            else
            {
                foreach (ListBoxItem p in pluginList.Items)
                {
                    if ((string)p.Content == lastSelectedName)
                    {
                        pluginList.SelectedItem = p;
                        break;
                    }
                }
            }
        }

        void UnloadPack(Pack r)
        {
            lock (r)
            {
                r.Preview?.Dispose();
                r.Preview = null;
            }
        }

        Pack LoadPack(string p, PackType type, Dictionary<string, string> switches = null, Dictionary<string, string[]> assertSwitches = null)
        {
            DirectoryFolder getFolder()
            {
                switch (type)
                {
                    case PackType.Folder: return DirectoryFolder.OpenFolder(p);
                    default: throw new NotImplementedException();
                }
            }

            return PackOpener.Load(p.Replace("\\", "/").Split('/').Last(), getFolder(), switches, assertSwitches);
        }

        T parseType<T>(Pack pack, dynamic o)
        {
            if (o == null) throw new RuntimeBinderException();
            string switchName = null;
            try
            {
                switchName = (string)((JObject)o).GetValue("_switch");
                if (switchName == null) throw new RuntimeBinderException();
            }
            catch
            {
                try
                {
                    return (T)o;
                }
                catch
                {
                    throw new Exception("value " + o.ToString() + " can't be converted to type " + typeof(T).ToString());
                }
            }

            if (!pack.SwitchValues.ContainsKey(switchName))
            {
                throw new Exception("switch name not found: " + switchName);
            }

            dynamic _o;
            try
            {
                _o = ((JObject)o).GetValue(pack.SwitchValues[switchName]);
            }
            catch
            {
                throw new Exception("value " + pack.SwitchValues[switchName] + " not found on a switch");
            }

            try
            {
                return parseType<T>(pack, _o);
            }
            catch
            {
                throw new Exception("value " + _o.ToString() + " can't be converted to type " + typeof(T).ToString());
            }
        }

        private void PluginList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadSelectedPack();
        }

        private void ReloadPackButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSelectedPack();
        }

        void LoadSelectedPack()
        {
            try
            {
                var p = resourcePacks[pluginList.SelectedIndex];
                if (settings.currPack != null) UnloadPack(settings.currPack);
                var pack = LoadPack(p.filename, p.type);
                if (!pack.Error)
                {
                    pluginDesc.Foreground = Brushes.White;
                    settings.currPack = pack;
                    settings.lastPackChangeTime = DateTime.Now.Ticks;
                }
                else
                {
                    pluginDesc.Foreground = Brushes.Red;
                    settings.currPack = null;
                    settings.lastPackChangeTime = DateTime.Now.Ticks;
                }
                if (pack.Preview == null)
                    previewImg.Source = null;
                else
                    previewImg.Source = BitmapToImageSource(pack.Preview);
                switchTab.Visibility = Visibility.Collapsed;
                switchPanel.Children.Clear();
                if (pack.SwitchChoices != null && pack.SwitchChoices.Count != 0)
                {
                    switchTab.Visibility = Visibility.Visible;
                    bool first = true;
                    foreach (var s in pack.SwitchOrder)
                    {
                        if (pack.SwitchChoices.ContainsKey(s))
                        {
                            var menu = new ComboBox();
                            menu.Tag = s;
                            foreach (var v in pack.SwitchChoices[s])
                            {
                                menu.Items.Add(new ComboBoxItem() { Content = v });
                            }
                            var dock = new DockPanel();
                            dock.HorizontalAlignment = HorizontalAlignment.Left;
                            dock.Children.Add(new Label() { Content = s });
                            dock.Children.Add(menu);
                            switchPanel.Children.Add(dock);
                            menu.SelectedIndex = 0;
                            pack.SwitchValues[s] = pack.SwitchChoices[s][0];
                            menu.SelectionChanged += Menu_SelectionChanged;
                        }
                        else
                        {
                            switchPanel.Children.Add(new Label() { Content = s, FontSize = 16, Margin = first ? new Thickness(0) : new Thickness(0, 10, 0, 0) });
                        }
                        first = false;
                    }
                }
                pluginDesc.Text = pack.Description;
            }
            catch { }
        }

        private void Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (settings.currPack != null)
            {
                var p = resourcePacks[pluginList.SelectedIndex];
                var choices = settings.currPack.SwitchChoices;
                var vals = settings.currPack.SwitchValues;

                var box = (ComboBox)sender;
                var tag = (string)box.Tag;
                vals[tag] = choices[tag][box.SelectedIndex];

                UnloadPack(settings.currPack);
                var pack = LoadPack(p.filename, p.type, vals, choices);
                if (!pack.Error)
                {
                    pluginDesc.Text = pack.Description;
                    pluginDesc.Foreground = Brushes.White;
                    settings.currPack = pack;
                    settings.lastPackChangeTime = DateTime.Now.Ticks;
                }
                else
                {
                    pluginDesc.Text = pack.Description;
                    pluginDesc.Foreground = Brushes.Red;
                    settings.currPack = null;
                    settings.lastPackChangeTime = DateTime.Now.Ticks;
                }
                if (pack.Preview == null)
                    previewImg.Source = null;
                else
                    previewImg.Source = BitmapToImageSource(pack.Preview);
            }
        }

        public void SetValues()
        {
            firstNote.Value = settings.firstNote;
            lastNote.Value = settings.lastNote - 1;
            noteDeltaScreenTime.Value = settings.deltaTimeOnScreen;
            blackNotesAbove.IsChecked = settings.blackNotesAbove;
            paletteList.SelectImage(settings.palette);
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadPacks();
        }

        bool screenTimeLock = false;

        private void BlackNotesAbove_Checked(object sender, RoutedEventArgs e)
        {
            if (!inited) return;
            try
            {
                settings.blackNotesAbove = (bool)blackNotesAbove.IsChecked;
            }
            catch (NullReferenceException) { }
        }

        private void NoteDeltaScreenTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!inited) return;
            try
            {
                if (screenTimeLock) return;
                screenTimeLock = true;
                settings.deltaTimeOnScreen = noteDeltaScreenTime.Value;
                screenTimeLock = false;
            }
            catch (NullReferenceException)
            {
                screenTimeLock = false;
            }
        }

        private void Nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (!inited) return;
            try
            {
                if (sender == firstNote) settings.firstNote = (int)firstNote.Value;
                if (sender == lastNote) settings.lastNote = (int)lastNote.Value + 1;
            }
            catch (NullReferenceException) { }
            catch (InvalidOperationException) { }
        }

        private void openFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!packPath.Contains(":\\") && !packPath.Contains(":/"))
                Process.Start("explorer.exe", System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), packPath));
            else
                Process.Start("explorer.exe", packPath);
        }
    }
}
