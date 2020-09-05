using System;
using System.Collections.Generic;
using System.Globalization;
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
using Zenith.Models;
using ZenithEngine.UI;

namespace Zenith
{
    /// <summary>
    /// Interaction logic for GeneralTab.xaml
    /// </summary>
    public partial class GeneralTab : UserControl
    {
        public GeneralTab()
        {
            InitializeComponent();

            speedSlider.nudToSlider = v => Math.Log(v, 2);
            speedSlider.sliderToNud = v => Math.Pow(2, v);
            speedSlider.Minimum = Math.Log(0.01, 2);
            speedSlider.Maximum = Math.Log(50, 2);
        }

        public BaseModel Data
        {
            get
            {
                var data = DataContext as BaseModel;
                if (data == null) throw new Exception("Data context must be set correctly");
                return data;
            }
        }

        private async void loadMidi_Click(object sender, LoadableRoutedEventArgs e)
        {
            await Data.Midi.LoadMidi("D:\\Midi\\tau2.5.9.mid");
            //await Data.Midi.LoadMidi("D:\\Midi\\(black score) Last Brutal Sister Flandre S 110 Million Notes.mid");
            e.Loaded();
        }

        private async void cancelLoadButton_Click(object sender, LoadableRoutedEventArgs e)
        {
            await Data.Midi.CancelMidiLoading();
            e.Loaded();
        }

        private async void startPreview_Click(object sender, RoutedEventArgs e)
        {
            await Data.StartPreview();
        }

        private async void stopPlayback_Click(object sender, RoutedEventArgs e)
        {
            await Data.StopPlayback();
        }

        private void unloadMidi_Click(object sender, RoutedEventArgs e)
        {
            Data.Midi.UnloadMidi();
        }

        private async void reloadModules_LoaderClick(object sender, LoadableRoutedEventArgs e)
        {
            await Data.LoadAllModules();
            e.Loaded();
        }

        private async void kdmapiSwitch_LoaderClick(object sender, LoadableRoutedEventArgs e)
        {
            if (Data.KdmapiConnected) await Data.UnloadKdmapi();
            else await Data.LoadKdmapi();
            e.Loaded();
        }
    }
}
