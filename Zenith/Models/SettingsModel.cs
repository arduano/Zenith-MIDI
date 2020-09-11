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
        public string SelectedLanguage { get; set; }

        public static SettingsModel Instance { get; } = new SettingsModel();
        public SettingsModel() : base("settings.json")
        { }
    }
}
