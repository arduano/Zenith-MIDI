using System;
using System.Collections.Generic;
using System.ComponentModel;
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


        public enum ResPreset
        {
            [Description("1280x720")]
            Res720p,
            [Description("1920x1080")]
            Res1080p,
            [Description("2560x1440")]
            Res1440p,
            [Description("3840x2160")]
            Res4k,
            [Description("5120x2880")]
            Res5k,
            [Description("7680x4320")]
            Res8k,
            [Description("15360x8640")]
            Res16k,
        }


        public GeneralTab()
        {
            InitializeComponent();

            speedSlider.NudToSlider = v => Math.Log(v, 2);
            speedSlider.SliderToNud = v => Math.Pow(2, v);

            foreach (var val in Enum.GetValues(typeof(ResPreset)).Cast<ResPreset>())
            {
                var item = new EnumComboBoxItem();
                item.EnumValue = val;
                item.Content = val.ToString().Replace("Res", "");
                resolutionPreset.Items.Add(item);
            }
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

        private void ResPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO (Khang): should be a helper function and merged with the one in ZenithEngine
            var val = (ResPreset)resolutionPreset.Selected;
            var descSplit = (val.GetType()
                  .GetField(val.ToString())
                  .GetCustomAttributes(typeof(DescriptionAttribute), false)
                  .FirstOrDefault() as DescriptionAttribute)
                  .Description.Split('x');
            if (descSplit.Length != 2)
                throw new Exception("Resolution preset description didn't specify 2 numbers");
            int tempWidth, tempHeight;
            if (!Int32.TryParse(descSplit[0], out tempWidth) || !Int32.TryParse(descSplit[1], out tempHeight))
                throw new Exception("Failed to parse resolution preset description");
            renderWidth.Value = tempWidth;
            renderHeight.Value = tempHeight;
        }
    }
}
