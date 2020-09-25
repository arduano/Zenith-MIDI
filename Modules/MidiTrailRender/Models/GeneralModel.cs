using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine;

namespace MIDITrailRender.Models
{
    public class GeneralModel : INotifyPropertyChanged
    {
        public int FirstKey { get; set; } = 0;
        public int LastKey { get; set; } = 127;
        public bool SameWidthNotes { get; set; } = true;

        public double NoteScale { get; set; } = 5000;

        [JsonIgnore]
        public NoteColorPalettePick PalettePicker { get; set; } = new NoteColorPalettePick();

        public GeneralModel()
        {
            PalettePicker.SetPath("Plugins\\Assets\\Palettes");
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
