using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zenith.Models
{
    public static class FileUtil
    {
        public static string BaseDirectory => Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        public static async Task<string> TryReadFile(string path)
        {
            if (!Path.IsPathRooted(path)) path = Path.Combine(BaseDirectory, "Settings", path);
            for (int i = 0; i < 100; i++)
            {
                if (!File.Exists(path)) throw new FileNotFoundException();
                try
                {
                    return File.ReadAllText(path);
                }
                catch { await Task.Delay(100); }
            }
            throw new Exception("Unable to read settings file after 100 attempts");
        }

        public static string TryReadFileSync(string path)
        {
            if (!Path.IsPathRooted(path)) path = Path.Combine(BaseDirectory, "Settings", path);
            for (int i = 0; i < 100; i++)
            {
                if (!File.Exists(path)) throw new FileNotFoundException();
                try
                {
                    return File.ReadAllText(path);
                }
                catch { Thread.Sleep(100); }
            }
            throw new Exception("Unable to read settings file after 100 attempts");
        }
    }
}
