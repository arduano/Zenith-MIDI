using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIDITrailRender.Models
{
    public class GlowModel : INotifyPropertyChanged
    {
        public bool UseGlow { get; set; } = true;
        public double GlowSigma { get; set; } = 20;
        public double GlowStrength { get; set; } = 3;
        public double GlowBrightness { get; set; } = 1;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
