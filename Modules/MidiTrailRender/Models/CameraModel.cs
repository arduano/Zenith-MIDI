using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIDITrailRender.Models
{
    public class CameraModel : INotifyPropertyChanged
    {
        public double CamX { get; set; } = 0;
        public double CamY { get; set; } = -0.46;
        public double CamZ { get; set; } = -0.09;

        public double CamRotX { get; set; } = 58;
        public double CamRotY { get; set; } = 0;
        public double CamRotZ { get; set; } = 0;

        public bool UseOrthro { get; set; } = false;
        public double OrthroX { get; set; } = 0;
        public double OrthroY { get; set; } = 0;
        public double OrthroScaleX { get; set; } = 1;
        public double OrthroScaleY { get; set; } = 1;

        public double RenderDistForward { get; set; } = 1.2;
        public double RenderDistBack { get; set; } = 0.15;

        public double CamFOV { get; set; } = 75;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
