using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Black_Midi_Render
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "Zenith";
            Application app = new Application();
            app.Run(new MainWindow());
        }
    }
}
