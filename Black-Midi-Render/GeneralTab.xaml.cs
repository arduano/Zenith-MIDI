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

        private async void loadMidi_Click(object sender, RoutedEventArgs e)
        {
            await Data.Midi.LoadMidi("D:\\Midi\\tau2.5.9.mid");
        }

        private async void startPreview_Click(object sender, RoutedEventArgs e)
        {
            await Data.StartPreview();
        }

        private async void stopPlayback_Click(object sender, RoutedEventArgs e)
        {
            await Data.StopPlayback();
        }
    }

    public class DecimalIntConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (decimal)(int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)(decimal)value;
        }
    }
}
