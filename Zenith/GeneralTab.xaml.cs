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
        public bool SelectingHistoricalMidi
        {
            get { return (bool)GetValue(SelectingHistoricalMidiProperty); }
            set { SetValue(SelectingHistoricalMidiProperty, value); }
        }

        public static readonly DependencyProperty SelectingHistoricalMidiProperty =
            DependencyProperty.Register("SelectingHistoricalMidi", typeof(bool), typeof(GeneralTab), new PropertyMetadata(false));



        public GeneralTab()
        {
            InitializeComponent();

            speedSlider.NudToSlider = v => Math.Log(v, 2);
            speedSlider.SliderToNud = v => Math.Pow(2, v);
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
            SelectingHistoricalMidi = true;
            selectPreviousMidiList.SelectedIndex = -1;

            void selectPreviousMidiList_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                Load(Data.Cache.HistoricalMidiFiles[selectPreviousMidiList.SelectedIndex].Path);
            }

            void browseMidiButton_Click(object sender, RoutedEventArgs e)
            {
                var path = Data.Cache.OpenFileDialog("BrowseMidi", "Midi files (*.mid)|*.mid");
                if (path != null) Load(path);
            }

            void cancelSelectingMidiButton_Click(object sender, RoutedEventArgs _e)
            {
                Unbind();
                e.Loaded();
            }

            selectPreviousMidiList.SelectionChanged += selectPreviousMidiList_SelectionChanged;
            browseMidiButton.Click += browseMidiButton_Click;
            cancelSelectingMidiButton.Click += cancelSelectingMidiButton_Click;

            void Unbind()
            {
                selectPreviousMidiList.SelectionChanged -= selectPreviousMidiList_SelectionChanged;
                browseMidiButton.Click -= browseMidiButton_Click;
                cancelSelectingMidiButton.Click -= cancelSelectingMidiButton_Click;
                SelectingHistoricalMidi = false;
            }

            async void Load(string midi)
            {
                Unbind();
                Data.Cache.AddHistoricalMidiFile(midi);
                await Data.Midi.LoadMidi(midi);
                e.Loaded();
            }
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
