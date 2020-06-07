using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine
{
    public class RenderStatus : INotifyPropertyChanged
    {
        public int FPS { get; set; } = 60;

        public int OutputWidth { get; } = 1920;
        public int OutputHeight { get;  } = 1080;
        public int RenderWidth { get => OutputWidth * SSAA; }
        public int RenderHeight { get => OutputHeight * SSAA; }
        public int SSAA { get; } = 1;

        public bool Running { get; set; } = true;

        public bool PreviewAudioEnabled { get; set; } = true;

        public bool RealtimePlayback { get; set; } = false;

        public RenderStatus(int outputWidth, int outputHeight)
        {
            OutputWidth = outputWidth;
            OutputHeight = outputHeight;
        }

        public RenderStatus(int outputWidth, int outputHeight, int ssaa) : this(outputWidth, outputHeight)
        {
            SSAA = ssaa;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
