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
using ZenithShared;

namespace ZenithInstaller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public Exception exception = null;

        void ProgressCallbac(long dl, long total)
        {
            Dispatcher.Invoke(() =>
            {
                dlProgress.Visibility = Visibility.Visible;
                dlProgress.Content = (Math.Round(((double)dl / total * 1000.0)) / 10) + "% of " + (Math.Round(total / 100000.0) / 10) + "mb";
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    Stream data;
                    if(Environment.Is64BitOperatingSystem) data = ZenithUpdates.DownloadAssetDataProgressive(ZenithUpdates.DataAssetName64, ProgressCallbac);
                    else data = ZenithUpdates.DownloadAssetDataProgressive(ZenithUpdates.DataAssetName32, ProgressCallbac);
                    Dispatcher.Invoke(() =>
                    {
                        dlProgress.Visibility = Visibility.Collapsed;
                    });
                    Dispatcher.Invoke(() => progressText.Content = "Installing...");
                    ZenithUpdates.InstallFromStream(data);
                    data.Close();
                    Program.FinalizeInstall();
                    Dispatcher.Invoke(() => Close());
                }
                catch (Exception ex)
                {
                    exception = ex;
                    Dispatcher.Invoke(() => Close());
                }
            });
        }
    }
}
