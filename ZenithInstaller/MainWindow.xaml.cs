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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    Stream data;
                    if(Environment.Is64BitOperatingSystem) data = ZenithUpdates.DownloadAssetData(ZenithUpdates.DataAssetName64);
                    else data = ZenithUpdates.DownloadAssetData(ZenithUpdates.DataAssetName32);
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
