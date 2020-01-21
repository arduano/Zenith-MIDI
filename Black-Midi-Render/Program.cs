using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ZenithShared;

namespace Zenith_MIDI
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //var ver = Languages.GetLatestVersion();
            //var stream = Languages.DownloadLatestVersion();
            //Languages.UnpackFromStream(stream);

            new Settings();

            Console.Title = "Zenith";
            Application app = new Application();
            app.Run(new MainWindow());
        }
    }
}
