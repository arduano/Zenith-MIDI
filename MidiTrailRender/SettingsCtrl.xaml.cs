using Newtonsoft.Json;
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

namespace MIDITrailRender
{
    /// <summary>
    /// Interaction logic for SettingsCtrl.xaml
    /// </summary>
    public partial class SettingsCtrl : UserControl
    {
        Settings settings;
        public AuraSelect auraselect;

        public event Action PaletteChanged
        {
            add { paletteList.PaletteChanged += value; }
            remove { paletteList.PaletteChanged -= value; }
        }

        public void SetValues()
        {
            firstNote.Value = settings.firstNote;
            lastNote.Value = settings.lastNote - 1;
            noteDownSpeed.Value = (decimal)settings.noteDownSpeed;
            noteUpSpeed.Value = (decimal)settings.noteUpSpeed;
            boxNotes.IsChecked = settings.boxNotes;
            useVel.IsChecked = settings.useVel;
            notesChangeSize.IsChecked = settings.notesChangeSize;
            notesChangeTint.IsChecked = settings.notesChangeTint;
            sameWidthNotes.IsChecked = settings.sameWidthNotes;
            lightShade.IsChecked = settings.lightShade;
            tiltKeys.IsChecked = settings.tiltKeys;
            showKeyboard.IsChecked = settings.showKeyboard;
            noteDeltaScreenTime.Value = settings.deltaTimeOnScreen;
            camOffsetX.Value = (decimal)settings.viewOffset;
            camOffsetY.Value = (decimal)settings.viewHeight;
            camOffsetZ.Value = (decimal)settings.viewPan;
            FOVSlider.Value = settings.FOV / Math.PI * 180;
            viewAngSlider.Value = settings.camAng / Math.PI * 180;
            viewTurnSlider.Value = settings.camRot / Math.PI * 180;
            viewTurnSlider.Value = settings.camRot / Math.PI * 180;
            renderDistSlider.Value = settings.viewdist;
            renderDistBackSlider.Value = settings.viewback;
            paletteList.SelectImage(settings.palette);
            auraselect.LoadSettings();
        }

        ProfileManager profiles = new ProfileManager("Plugins/MIDITrailRender.json");
        public SettingsCtrl(Settings settings) : base()
        {
            InitializeComponent();
            noteDeltaScreenTime.nudToSlider = v => Math.Log(v, 2);
            noteDeltaScreenTime.sliderToNud = v => Math.Pow(2, v);
            this.settings = settings;
            paletteList.SetPath("Plugins\\Assets\\Palettes");
            LoadSettings(true);
            auraselect = new AuraSelect(settings);
            auraSubControlGrid.Children.Add(auraselect);
            auraselect.Margin = new Thickness(0);
            auraselect.HorizontalAlignment = HorizontalAlignment.Stretch;
            auraselect.VerticalAlignment = VerticalAlignment.Stretch;
            auraselect.Width = double.NaN;
            auraselect.Height = double.NaN;
            SetValues();
            ReloadProfiles();
        }

        private void Nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            try
            {
                if (sender == firstNote) settings.firstNote = (int)firstNote.Value;
                if (sender == lastNote) settings.lastNote = (int)lastNote.Value + 1;
                if (sender == noteDownSpeed) settings.noteDownSpeed = (double)noteDownSpeed.Value;
                if (sender == noteUpSpeed) settings.noteUpSpeed = (double)noteUpSpeed.Value;
                if (sender == camOffsetX) settings.viewOffset = (double)camOffsetX.Value;
                if (sender == camOffsetY) settings.viewHeight = (double)camOffsetY.Value;
                if (sender == camOffsetZ) settings.viewPan = (double)camOffsetZ.Value;
            }
            catch (NullReferenceException) { }
            catch (InvalidOperationException) { }
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

        private void BoxNotes_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.boxNotes = (bool)boxNotes.IsChecked;
            }
            catch { }
        }

        private void FOVSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                settings.FOV = (double)FOVSlider.Value / 180 * Math.PI;
            }
            catch { }
        }

        private void ViewAngSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                settings.camAng = (double)viewAngSlider.Value / 180 * Math.PI;
            }
            catch { }
        }

        private void ViewTurnSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                settings.camRot = (double)viewTurnSlider.Value / 180 * Math.PI;
            }
            catch { }
        }

        private void NoteDeltaScreenTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                settings.deltaTimeOnScreen = noteDeltaScreenTime.Value;
            }
            catch (NullReferenceException)
            {

            }
        }

        private void RenderDistSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                settings.viewdist = (double)renderDistSlider.Value;
            }
            catch { }
        }

        private void RenderDistBackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                settings.viewback = (double)renderDistBackSlider.Value;
            }
            catch { }
        }

        private void FarPreset_Click(object sender, RoutedEventArgs e)
        {
            camOffsetY.Value = 0.5M;
            camOffsetX.Value = 0.4M;
            camOffsetZ.Value = 0;
            FOVSlider.Value = 60;
            viewAngSlider.Value = 32.08;
            viewTurnSlider.Value = 0;
            renderDistSlider.Value = 14;
            renderDistBackSlider.Value = 0.2;
        }

        private void MediumPreset_Click(object sender, RoutedEventArgs e)
        {
            camOffsetY.Value = 0.52M;
            camOffsetX.Value = 0.37M;
            camOffsetZ.Value = 0;
            FOVSlider.Value = 60;
            viewAngSlider.Value = 34.98;
            viewTurnSlider.Value = 0;
            renderDistSlider.Value = 5.52;
            renderDistBackSlider.Value = 0.2;
        }

        private void ClosePreset_Click(object sender, RoutedEventArgs e)
        {
            camOffsetY.Value = 0.55M;
            camOffsetX.Value = 0.33M;
            camOffsetZ.Value = 0;
            FOVSlider.Value = 60;
            viewAngSlider.Value = 39.62;
            viewTurnSlider.Value = 0;
            renderDistSlider.Value = 3.06;
            renderDistBackSlider.Value = 0.2;
        }

        private void TopPreset_Click(object sender, RoutedEventArgs e)
        {
            camOffsetY.Value = 10M;
            camOffsetX.Value = -3.77M;
            camOffsetZ.Value = -1.53M;
            FOVSlider.Value = 26;
            viewAngSlider.Value = 90;
            viewTurnSlider.Value = -90;
            renderDistSlider.Value = 7.93;
            renderDistBackSlider.Value = 0.64;
        }

        private void PerspectivePreset_Click(object sender, RoutedEventArgs e)
        {
            camOffsetY.Value = 0.67M;
            camOffsetX.Value = 1.07M;
            camOffsetZ.Value = -0.32M;
            FOVSlider.Value = 60;
            viewAngSlider.Value = 33.24;
            viewTurnSlider.Value = -13.84;
            renderDistSlider.Value = 14;
            renderDistBackSlider.Value = 0.98;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            settings.palette = paletteList.SelectedImage;
            try
            {
                string s = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText("Plugins/MIDITrailRender.json", s);
                Console.WriteLine("Saved settings to MIDITrailRender.json");
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
                string s = File.ReadAllText("Plugins/MIDITrailRender.json");
                var sett = JsonConvert.DeserializeObject<Settings>(s);
                injectSettings(sett);
                Console.WriteLine("Loaded settings from MIDITrailRender.json");
            }
            catch
            {
                if (!startup)
                    Console.WriteLine("Could not load saved plugin settings");
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void DefaultsButton_Click(object sender, RoutedEventArgs e)
        {
            injectSettings(new Settings());
        }

        private void UseVel_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.useVel = (bool)useVel.IsChecked;
            }
            catch { }
        }

        private void CheckboxChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender == notesChangeSize) settings.notesChangeSize = (bool)notesChangeSize.IsChecked;
                if (sender == notesChangeTint) settings.notesChangeTint = (bool)notesChangeTint.IsChecked;
                if (sender == eatNotes) settings.eatNotes = (bool)eatNotes.IsChecked;
                if (sender == sameWidthNotes) settings.sameWidthNotes = (bool)sameWidthNotes.IsChecked;
                if (sender == lightShade) settings.lightShade = (bool)lightShade.IsChecked;
                if (sender == tiltKeys) settings.tiltKeys = (bool)tiltKeys.IsChecked;
                if (sender == showKeyboard) settings.showKeyboard = (bool)showKeyboard.IsChecked;
            }
            catch { }
        }

        private void NewProfile_Click(object sender, RoutedEventArgs e)
        {
            settings.palette = paletteList.SelectedImage;
            if (profileName.Text == "")
            {
                MessageBox.Show("Please write a name for the profile");
                return;
            }
            profiles.Add(settings, profileName.Text);
            ReloadProfiles();
            foreach (var i in profileSelect.Items)
            {
                if((string)((ComboBoxItem)i).Content == profileName.Text)
                {
                    profileSelect.SelectedItem = i;
                    break;
                }
            }
            SetValues();
        }

        private void ProfileSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                profiles.LoadProfile((string)((ComboBoxItem)profileSelect.SelectedItem).Content, settings);
                SetValues();
            }
            catch { }
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (profileSelect.SelectedItem == null) return;
            profiles.DeleteProfile((string)((ComboBoxItem)profileSelect.SelectedItem).Content);
            ReloadProfiles();
            SetValues();
        }

        void ReloadProfiles()
        {
            var ps = profiles.Profiles;
            profileSelect.Items.Clear();
            foreach (var p in ps)
            {
                profileSelect.Items.Add(new ComboBoxItem()
                {
                    Content = p
                });
            }
        }
    }
}
