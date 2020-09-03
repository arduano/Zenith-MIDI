using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.Models
{
    public class SettingsModel : SaveableModel
    {
        public double Test { get; set; }

        public SettingsModel() : base("test.json")
        {

        }
    }
}
