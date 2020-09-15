using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ZenithEngine;
using ZenithEngine.DXHelper;
using ZenithEngine.Modules;
using ZenithShared;

namespace Zenith
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
            Console.Title = "Zenith";
            Application app = new Application();
            var window = new ZenithWindow();

            window.Loaded += (s, e) =>
            {
                //if(args.Length > 0)
                //{
                //    window.LoadMidi(args[0]);
                //}
                //if(args.Length > 1)
                //{
                //    window.SelectModule(args[1]);
                //    window.StartPipeline(false);
                //}
            };

            app.Run(window);
#if !DEBUG
            }
            catch (Exception e)
            {
                string msg = e.Message + "\n" + e.Data + "\n";
                msg += e.StackTrace;
                MessageBox.Show(msg, "Zenith has crashed!");
            }
#endif
        }
    }
}
