using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Threading;

namespace Zenith.Models
{
    public abstract class LanguageItem
    {
        public ResourceDictionary LanguageResources { get; } = new ResourceDictionary();

        protected static string NameToken { get; } = "lang.name";

        protected void RecursiveAdd(JObject data, ResourceDictionary dict) =>
            RecursiveAdd("lang", data, dict);
        protected void RecursiveAdd(string root, JObject data, ResourceDictionary dict)
        {
            root = root + ".";
            foreach (var d in data)
            {
                var k = root + d.Key;
                if (d.Value.Type == JTokenType.String)
                {
                    if (!dict.Contains(k)) dict.Add(k, (string)d.Value);
                }
                if (d.Value.Type == JTokenType.Object)
                {
                    RecursiveAdd(k, (JObject)d.Value, dict);
                }
            }
        }
    }

    public class JsonLanguageItem : LanguageItem
    {
        public JsonLanguageItem(string json)
        {
            var dict = new ResourceDictionary();
            var obj = (JObject)JsonConvert.DeserializeObject(json);
            //RecursiveAdd(obj, dict);
            LanguageResources.MergedDictionaries.Add(dict);
        }
    }

    public class FolderLanguageItem : LanguageItem
    {
        public FolderLanguageItem(string folder, string code)
        {
            Code = code;
            Folder = folder;
            Reload();
        }

        FileSystemWatcher watcher;

        public bool Watching
        {
            get => watcher != null;
            set
            {
                if (!(Watching ^ value)) return;
                if (value)
                {
                    watcher = new FileSystemWatcher()
                    {
                        Path = Folder,
                        Filter = "*.json",
                    };

                    watcher.Changed += Watcher_Changed;
                    watcher.Created += Watcher_Changed;
                    watcher.Deleted += Watcher_Changed;
                    watcher.Renamed += Watcher_Changed;

                    watcher.EnableRaisingEvents = true;

                    Reload();
                }
                else
                {
                    watcher?.Dispose();
                    watcher = null;
                }
            }
        }

        void Reload()
        {
            var files = Directory.GetFiles(Folder, "*.json");

            var prevdict = LanguageResources.MergedDictionaries.FirstOrDefault();
            var dict = new ResourceDictionary();

            LanguageResources.MergedDictionaries.Clear();

            foreach (var f in files)
            {
                var text = FileUtil.TryReadFileSync(f);
                var data = (JObject)JsonConvert.DeserializeObject(text);
                RecursiveAdd(data, dict);
            }

            LanguageResources.MergedDictionaries.Add(dict);
            Name = LanguageResources.Contains(NameToken) ? (string)LanguageResources[NameToken] : Code;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (Application.Current.Dispatcher.Thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId)
            {
                Reload();
            }
            else
            {
                Application.Current.Dispatcher.InvokeAsync(Reload).Wait();
            }
        }

        public string Folder { get; }
        public string Name { get; private set; }
        public string Code { get; }
    }

    public class LanguagesModel : INotifyPropertyChanged
    {
        public static LanguagesModel Instance { get; } = new LanguagesModel();

        LanguageItem defaultLanguage;

        private LanguagesModel()
        {
            var languageDir = Path.Combine(FileUtil.BaseDirectory, "Languages");
            var languages = Directory.GetDirectories(languageDir);
            List<FolderLanguageItem> langs = new List<FolderLanguageItem>();
            foreach (var l in languages)
            {
                langs.Add(new FolderLanguageItem(l, l.Replace('\\', '/').Split('/').Last()));
            }

            Languages = langs.ToArray();

            PropertyChanged += LanguagesModel_PropertyChanged;

            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("Zenith.Languages.en.main.json"))
            using (var reader = new StreamReader(stream))
                defaultLanguage = new JsonLanguageItem(reader.ReadToEnd());

            MergedLanguages.MergedDictionaries.Add(defaultLanguage.LanguageResources);

            var english = langs.Where(l => l.Code == "en").FirstOrDefault();
            SelectedLanguage = english;

            SetListening();
        }

        private void LanguagesModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedLanguage))
            {
                while (MergedLanguages.MergedDictionaries.Count > 1)
                {
                    MergedLanguages.MergedDictionaries.RemoveAt(1);
                }

                if (SelectedLanguage != null)
                {
                    MergedLanguages.MergedDictionaries.Add(SelectedLanguage.LanguageResources);
                }
            }
            if (e.PropertyName == nameof(Listening))
            {
                SetListening();
            }
        }

        void SetListening()
        {
            foreach (var l in Languages)
            {
                l.Watching = Listening;
            }
        }

        public FolderLanguageItem[] Languages { get; private set; } = new FolderLanguageItem[0];
        public LanguageItem SelectedLanguage { get; set; }
        public ResourceDictionary MergedLanguages { get; } = new ResourceDictionary();

        public bool Listening { get; set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
