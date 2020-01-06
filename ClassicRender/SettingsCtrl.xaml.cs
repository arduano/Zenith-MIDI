using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using BMEngine;
using Newtonsoft.Json;

namespace ClassicRender
{
    /// <summary>
    /// Interaction logic for SettingsCtrl.xaml
    /// </summary>
    public partial class SettingsCtrl : UserControl
    {
        Settings settings;

        public event Action PaletteChanged
        {
            add { paletteList.PaletteChanged += value; }
            remove { paletteList.PaletteChanged -= value; }
        }

        public void SetValues()
        {
            firstNote.Value = settings.firstNote;
            lastNote.Value = settings.lastNote - 1;
            pianoHeight.Value = (int)(settings.pianoHeight * 100);
            noteDeltaScreenTime.Value = Math.Log(settings.deltaTimeOnScreen, 2);
            sameWidth.IsChecked = settings.sameWidthNotes;
            blackNotesAbove.IsChecked = settings.blackNotesAbove;
            paletteList.SelectImage(settings.palette);
        }

        public SettingsCtrl(Settings settings) : base()
        {
            InitializeComponent();
            this.settings = settings;
            paletteList.SetPath("Plugins\\Assets\\Palettes");
            LoadSettings(true);
            SetValues();
        }

        private void Nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (sender == firstNote) settings.firstNote = (int)firstNote.Value;
                if (sender == lastNote) settings.lastNote = (int)lastNote.Value + 1;
                if (sender == pianoHeight) settings.pianoHeight = (double)pianoHeight.Value / 100;
                if (sender == noteDeltaScreenTime) settings.deltaTimeOnScreen = (int)noteDeltaScreenTime.Value;
            }
            catch (NullReferenceException) { }
            catch (InvalidOperationException) { }
        }

        private void NoteDeltaScreenTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (screenTimeLock) return;
                screenTimeLock = true;
                settings.deltaTimeOnScreen = Math.Pow(2, noteDeltaScreenTime.Value);
                screenTime_nud.Value = (decimal)settings.deltaTimeOnScreen;
                screenTimeLock = false;
            }
            catch (NullReferenceException)
            {
                screenTimeLock = false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            settings.palette = paletteList.SelectedImage;
            try
            {
                string s = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText("Plugins/ClassicRender.json", s);
                Console.WriteLine("Saved settings to ClassicRender.json");
            }
            catch
            {
                Console.WriteLine("Could not save settings");
            }
        }

        void LoadSettings(bool startup = false)
        {

            try
            {
                string s = File.ReadAllText("Plugins/ClassicRender.json");
                var sett = JsonConvert.DeserializeObject<Settings>(s);
                injectSettings(sett);
                Console.WriteLine("Loaded settings from ClassicRender.json");
            }
            catch
            {
                if (!startup)
                    Console.WriteLine("Could not load saved plugin settings");
            }
        }

        private void BlackNotesAbove_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.blackNotesAbove = (bool)blackNotesAbove.IsChecked;
            }
            catch (NullReferenceException) { }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        void injectSettings(Settings sett)
        {
            var sourceProps = typeof(Settings).GetFields().ToList();
            var destProps = typeof(Settings).GetFields().ToList();

            foreach (var sourceProp in sourceProps)
            {
                if (destProps.Any(x => x.Name == sourceProp.Name))
                {
                    var p = destProps.First(x => x.Name == sourceProp.Name);
                    p.SetValue(settings, sourceProp.GetValue(sett));
                }
            }
            SetValues();
        }

        private void DefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            injectSettings(new Settings());
        }

        private void SameWidth_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.sameWidthNotes = (bool)sameWidth.IsChecked;
                blackNotesAbove.IsEnabled = !settings.sameWidthNotes;
            }
            catch (NullReferenceException) { }
        }

        bool screenTimeLock = false;
        private void ScreenTime_nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (screenTimeLock) return;
                screenTimeLock = true;
                noteDeltaScreenTime.Value = Math.Log((double)screenTime_nud.Value, 2);
                settings.deltaTimeOnScreen = (double)screenTime_nud.Value;
                screenTimeLock = false;
            }
            catch
            {
                screenTimeLock = false;
            }
        }
    }
}
