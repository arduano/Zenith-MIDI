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
        public GlowPassModel Pass { get; set; } = new GlowPassModel();

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
