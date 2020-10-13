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
    /// Interaction logic for RenderProgress.xaml
    /// </summary>
    public partial class RenderProgress : UserControl
    {
        public BaseModel Data
        {
            get
            {
                var data = DataContext as BaseModel;
                if (data == null) throw new Exception("Data context must be set correctly");
                return data;
            }
        }

        public RenderProgress()
        {
            InitializeComponent();
        }

        private async void stopPlayback_LoaderClick(object sender, ZenithEngine.UI.LoadableRoutedEventArgs e)
        {
            await Data.StopPlayback();
            e.Loaded();
        }
    }

    class DispTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double time = (double)value;
            var timespan = TimeSpan.FromSeconds(time);
            return timespan.ToString(@"hh\:mm\:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class ProgPrecentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double prog = (double)value;
            return Math.Round(prog * 100, 1) + "%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class NoteCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long count = (long)value;
            return count.ToString("#,##0");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class FPSConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double fps = (double)value;
            return fps.ToString("#,##0.##");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
