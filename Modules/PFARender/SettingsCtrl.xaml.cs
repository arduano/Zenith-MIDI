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

namespace PFARender
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
            noteDeltaScreenTime.Value = settings.deltaTimeOnScreen;
            borderWidth.Value = (decimal)settings.borderWidth;
            sameWidth.IsChecked = settings.sameWidthNotes;
            //topColorSelect.SelectedIndex = (int)settings.topColor;
            middleCSquare.IsChecked = settings.middleC;
            blackNotesAbove.IsChecked = settings.blackNotesAbove;
            paletteList.SelectImage(settings.palette);
        }

        public SettingsCtrl(Settings settings) : base()
        {
            InitializeComponent();
            noteDeltaScreenTime.nudToSlider = v => Math.Log(v, 2);
            noteDeltaScreenTime.sliderToNud = v => Math.Pow(2, v);
            this.settings = settings;
            paletteList.SetPath("Plugins\\Assets\\Palettes", 0.8f);
            SetValues();
        }

        private void Nud_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (settings == null) return;
            if (sender == firstNote) settings.firstNote = (int)firstNote.Value;
            if (sender == lastNote) settings.lastNote = (int)lastNote.Value + 1;
            if (sender == pianoHeight) settings.pianoHeight = (double)pianoHeight.Value / 100;
            if (sender == noteDeltaScreenTime) settings.deltaTimeOnScreen = (int)noteDeltaScreenTime.Value;
            if (sender == borderWidth) settings.borderWidth = (double)borderWidth.Value;
        }

        private void NoteDeltaScreenTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (settings == null) return;
            settings.deltaTimeOnScreen = noteDeltaScreenTime.Value;
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

        private void SameWidth_Checked(object sender, RoutedEventArgs e)
        {
            if (settings == null) return;
            settings.sameWidthNotes = (bool)sameWidth.IsChecked;
        }

        //private void TopColorSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    try
        //    {
        //        settings.topColor = (TopColor)topColorSelect.SelectedIndex;
        //    }
        //    catch (NullReferenceException) { }
        //}

        private void MiddleCSquare_Checked(object sender, RoutedEventArgs e)
        {
            if (settings == null) return;
            settings.middleC = (bool)middleCSquare.IsChecked;
        }

        private void BlackNotesAbove_Checked(object sender, RoutedEventArgs e)
        {
            if (settings == null) return;
            settings.blackNotesAbove = (bool)blackNotesAbove.IsChecked;
        }

        private void BarColorHex_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (settings == null) return;
            var hex = barColorHex.Text;
            if (hex.Length != 6) return;
            try
            {
                int col = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                settings.topBarR = ((col >> 16) & 0xFF) / 255.0f;
                settings.topBarG = ((col >> 8) & 0xFF) / 255.0f;
                settings.topBarB = ((col >> 0) & 0xFF) / 255.0f;
            }
            catch { return; }
        }
    }
}
