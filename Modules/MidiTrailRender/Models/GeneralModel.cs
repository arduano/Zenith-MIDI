using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIDITrailRender.Models
{
    public class GeneralModel : INotifyPropertyChanged
    {
        public int FirstKey { get; set; } = 0;
        public int LastKey { get; set; } = 128;
        public bool SameWidthNotes { get; set; } = true;

        public double NoteScale { get; set; } = 5000;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
