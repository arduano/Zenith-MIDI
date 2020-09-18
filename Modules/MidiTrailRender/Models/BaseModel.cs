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
    public class BaseModel : INotifyPropertyChanged
    {
        public CameraModel Camera { get; set; } = new CameraModel();
        public GeneralModel General { get; set; } = new GeneralModel();
        public GlowModel Glow { get; set; } = new GlowModel();
        public KeysModel Keys { get; set; } = new KeysModel();
        public NotesModel Notes { get; set; } = new NotesModel();

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
