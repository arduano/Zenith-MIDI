using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexturedRender
{
    public class ViewSettingsModel : INotifyPropertyChanged
    {
        public int FirstKey { get; set; } = 0;
        public int LastKey { get; set; } = 127;

        public double NoteScreenTime { get; set; } = 1000;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
