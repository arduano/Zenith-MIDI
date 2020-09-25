using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using ZenithEngine.IO;
using ZenithEngine.ModuleUI;
using Path = System.IO.Path;

namespace TexturedRender
{
    public partial class SettingsCtrl : UserControl, ISerializableContainer
    {
        string packPath = "Plugins\\Assets\\Textured\\Resources";

        public SettingsModel Data { get; } = new SettingsModel();

        public SettingsCtrl()
        {
            DataContext = Data;

            Data.PropertyChanged += Data_PropertyChanged;

            InitializeComponent();

            //noteDeltaScreenTime.NudToSlider = v => Math.Log(v, 2);
            //noteDeltaScreenTime.SliderToNud = v => Math.Pow(2, v);
            Data.PalettePicker.SetPath("Plugins\\Assets\\Palettes", 1f);
            ReloadPacks();
        }

        private void Data_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(Data.SelectedPack))
                {
                    Data.LoadedPack?.Unload();
                    if (Data.SelectedPack != null)
                    {
                        var name = Data.SelectedPack.Filename;
                        Data.LoadedPack = new LoadedPack(name, Data.SelectedPack);
                    }
                    else
                    {
                        Data.LoadedPack = null;
                    }
                }
            }
            catch (Exception err)
            {

            }
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
            var lastSelection = Data.SelectedPack;

            string dir = packPath;

            WriteDefaultPack();

            var packs = DirectoryFolder.ListDirectories(dir)
                .OrderBy(p => p.Filename)
                .ToArray();

            var selected = packs.Where(p => p.Equals(lastSelection)).FirstOrDefault();

            if (selected == null)
            {
                selected = packs
                    .OrderBy(r => r.Filename == "Default" ? 0 : 1)
                    .ThenBy(r => r.Filename)
                    .FirstOrDefault();
            }

            if (Data.SelectedPack?.Equals(selected) != true)
            {
                Data.SelectedPack = selected;
            }
            Data.PackLocations = packs;
        }

        private void openFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!packPath.Contains(":\\") && !packPath.Contains(":/"))
                Process.Start("explorer.exe", System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), packPath));
            else
                Process.Start("explorer.exe", packPath);
        }

        public void Parse(JObject data)
        {
            Data.Parse(data);
        }

        public JObject Serialize()
        {
            return Data.Serialize();
        }

        private void reloadListButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadPacks();
        }

        private void reloadPackButton_Click(object sender, RoutedEventArgs e)
        {
            var s = Data.SelectedPack;
            Data.SelectedPack = null;
            Data.SelectedPack = s;
        }
    }
}
