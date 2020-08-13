using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Zenith.Models
{
    public class OutputArgsModel : INotifyPropertyChanged
    {
        public string OutputLocation { get; set; } = "";

        public bool UseAudio { get; set; } = false;
        public string AudioLocation { get; set; } = "";
        public bool UseMaskOutput { get; set; } = false;
        public string MaskOutputLocation { get; set; } = "";

        public string OutputArgs

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
