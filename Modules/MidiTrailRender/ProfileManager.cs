using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace MIDITrailRender
{
    class ProfileManager
    {
        string jsonPath;
        Dictionary<string, Settings> settings = new Dictionary<string, Settings>();

        public string[] Profiles => settings.Keys.ToArray();

        public ProfileManager(string savePath)
        {
            jsonPath = savePath;
            Load();
        }

        void injectSettings(Settings insett, Settings outsett)
        {
            var sourceProps = typeof(Settings).GetFields().ToList();
            var destProps = typeof(Settings).GetFields().ToList();

            foreach (var sourceProp in sourceProps)
            {
                if (destProps.Any(x => x.Name == sourceProp.Name))
                {
                    var p = destProps.First(x => x.Name == sourceProp.Name);
                    p.SetValue(outsett, sourceProp.GetValue(insett));
                }
            }
        }

        public void Add(Settings sett, string name)
        {
            if (settings.ContainsKey(name))
            {
                MessageBox.Show("A profile with this name already exists");
                return;
            }
            var s = new Settings();
            injectSettings(sett, s);
            settings.Add(name, s);
            Save();
        }

        public void Save()
        {
            try
            {
                var s = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(jsonPath, s);
            }
            catch
            {
                MessageBox.Show("Could not save settings. Is the file open in another program?");
            }
        }

        public void Load()
        {
            if (!File.Exists(jsonPath))
            {
                settings = new Dictionary<string, Settings>();
            }
            else
            {
                try
                {
                    var json = File.ReadAllText(jsonPath);
                    settings = JsonConvert.DeserializeObject<Dictionary<string, Settings>>(json);
                }
                catch
                {
                    MessageBox.Show("Could not decode JSON settings file, loading defaults");
                    settings = new Dictionary<string, Settings> { { "Default", new Settings() } };
                }
            }
        }

        public void LoadProfile(string name, Settings dest)
        {
            if (!settings.ContainsKey(name))
            {
                MessageBox.Show("Could not poad profile");
                return;
            }
            injectSettings(settings[name], dest);
        }

        public void DeleteProfile(string name)
        {
            try
            {
                settings.Remove(name);
                Save();
            }
            catch { }
        }
    }
}
