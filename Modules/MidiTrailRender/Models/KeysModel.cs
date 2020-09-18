using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIDITrailRender.Models
{
    public class KeysModel : INotifyPropertyChanged
    {
        public bool EnableWater { get; set; } = true;

        public FullColorModel UnpressedColor { get; set; } = new FullColorModel()
        {
            Diffuse = 1,
            Emit = 0,
            Specular = 1,
            Water = 0,
        };
        public FullColorModel PressedColor { get; set; } = new FullColorModel()
        {
            Diffuse = 0.5,
            Emit = 0.5,
            Specular = 0,
            Water = 0.5,
        };

        public bool WaterSecondaryColor { get; set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
