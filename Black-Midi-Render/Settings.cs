using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Zenith_MIDI
{
    class Settings
    {
        public string PreviousVersion = "";
        public string VersionName = "2.0.10";
        public string LanguagesVersion = "";
        public bool AutoUpdate = false;
        public bool Installed = false;
        public string SelectedLanguage = "";
        public string InstallerVer = "";

        public static readonly string SettingsPath = "Settings\\meta.kvs";

        public Settings()
        {
            if (!File.Exists(SettingsPath))
            {
                if(!Directory.Exists(Path.GetDirectoryName(SettingsPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                SaveConfig();
            }
            else
            {
                var stream = new StreamReader(new GZipStream(File.Open(SettingsPath, FileMode.Open), CompressionMode.Decompress));
                var text = stream.ReadToEnd();
                var obj = (dynamic)JsonConvert.DeserializeObject(text);
                stream.Close();
                if (obj.version != null) VersionName = obj.version;
                if (obj.langsVersion != null) LanguagesVersion = obj.langsVersion;
                if (obj.selectedLang != null) SelectedLanguage = obj.selectedLang;
                if (obj.autoUpdate != null) AutoUpdate = obj.autoUpdate;
                if (obj.installed != null) Installed = obj.installed;
                if (obj.installerVer != null) InstallerVer = obj.installerVer;
            }
        }

        public void SaveConfig()
        {
            var jobj = new JObject();
            jobj.Add("version", VersionName);
            jobj.Add("langsVersion", LanguagesVersion);
            jobj.Add("selectedLang", SelectedLanguage);
            jobj.Add("autoUpdate", AutoUpdate);
            jobj.Add("installed", Installed);
            jobj.Add("installerVer", InstallerVer);

            var stream = new StreamWriter(new GZipStream(File.Open(SettingsPath, FileMode.Create), CompressionMode.Compress));
            stream.Write(JsonConvert.SerializeObject(jobj));
            stream.Close();
        }
    }
}
