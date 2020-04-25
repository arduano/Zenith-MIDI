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
using ZenithShared;

namespace Zenith_MIDI
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            BlockingCollection<int> test = new BlockingCollection<int>();
            BlockingCollection<int> test2 = new BlockingCollection<int>();
            Task.Run(() =>
            {
                foreach (var v in test.GetConsumingEnumerable()) test2.Add(v);
            });
            for(int i = 0; ; i++)
            {
                test.Add(i);
                var b = test2.Take();
                if (b % 10000 == 0) Console.WriteLine(b);
            }


#if !DEBUG
            try
            {
#endif
            Console.Title = "Zenith";
            Application app = new Application();
            app.Run(new MainWindow());
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
