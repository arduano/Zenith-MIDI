using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIDITrailRender.Models
{
    public enum NoteType
    {
        Round,
        Cube,
        Flat
    }

    public class NotesModel : INotifyPropertyChanged
    {
        public bool EnableWater { get; set; } = false;

        public FullColorModel UnpressedColor { get; set; } = new FullColorModel()
        {
            Diffuse = 1,
            Emit = 0,
            Specular = 1,
            Water = 0.35,
        };
        public FullColorModel PressedColor { get; set; } = new FullColorModel()
        {
            Diffuse = 0.5,
            Emit = 0.5,
            Specular = 0,
            Water = 0.35,
        };

        public bool WaterSecondaryColor { get; set; } = true;

        public double Angle { get; set; } = 0;
        public double Offset { get; set; } = 0;

        public NoteType NoteType { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
