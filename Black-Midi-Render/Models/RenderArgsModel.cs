using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Zenith.Models
{
    public enum NoteSize
    {
        [Description("Tick Based")]
        Tick,
        [Description("Time Based")]
        Time,
    }

    public class RenderArgsModel : INotifyPropertyChanged
    {
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public int SSAA { get; set; } = 1;
        public int FPS { get; set; } = 60;

        public bool IgnoreColorEvents { get; set; } = false;
        public NoteSize NoteSize { get; set; } = NoteSize.Tick;

        public event PropertyChangedEventHandler PropertyChanged;

        public RenderArgsModel()
        {
            PropertyChanged += (s, e) =>
            {
                Console.WriteLine(e.PropertyName);
            };
        }
    }
}
