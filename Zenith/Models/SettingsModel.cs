using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.Models
{
    public class SettingsModel : SaveableModel
    {
        public struct SavedHistoricalFile
        {
            public string Path { get; }
            public string Name { get; }

            public SavedHistoricalFile(string path) : this()
            {
                Path = path;
                Name = System.IO.Path.GetFileName(path);
            }
        }

        public Dictionary<string, string> BrowseFilesPathsCache { get; set; } = new Dictionary<string, string>();
        public SavedHistoricalFile[] HistoricalMidiFiles { get; set; } = new SavedHistoricalFile[0];

        public SettingsModel() : base("test.json")
        {

        }

        public string GetCachedFilePath(string key)
        {
            lock (l)
            {
                if (!BrowseFilesPathsCache.ContainsKey(key)) return null;
                return BrowseFilesPathsCache[key];
            }
        }

        public void SaveCachedFilePath(string key, string path)
        {
            lock (l)
            {
                if (BrowseFilesPathsCache.ContainsKey(key))
                {
                    BrowseFilesPathsCache[key] = path;
                }
                else
                {
                    BrowseFilesPathsCache.Add(key, path);
                }
            }
            SaveSettings();
        }

        public string SaveFileDialog(string key, string filter)
        {
            var save = new SaveFileDialog();
            save.OverwritePrompt = true;
            save.Filter = filter;
            save.InitialDirectory = GetCachedFilePath(key) ?? save.InitialDirectory;
            if ((bool)save.ShowDialog())
            {
                SaveCachedFilePath(key, Path.GetDirectoryName(save.FileName));
                return save.FileName;
            }
            return null;
        }

        public string OpenFileDialog(string key, string filter)
        {
            var save = new OpenFileDialog();
            save.Filter = filter;
            save.InitialDirectory = GetCachedFilePath(key) ?? save.InitialDirectory;
            if ((bool)save.ShowDialog())
            {
                SaveCachedFilePath(key, Path.GetDirectoryName(save.FileName));
                return save.FileName;
            }
            return null;
        }

        public void AddHistoricalMidiFile (string file)
        {
            lock (l)
            {
                var filtered = HistoricalMidiFiles.Where(f => f.Path != file);
                HistoricalMidiFiles =
                    new[] { new SavedHistoricalFile(file) }
                    .Concat(filtered)
                    .Take(40)
                    .ToArray();
            }
            SaveSettings();
        }
    }
}
