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
        public double CamY { get; set; } = -0.598;
        public double CamZ { get; set; } = -0.209;

        public double CamRotX { get; set; } = 50;
        public double CamRotY { get; set; } = 0;
        public double CamRotZ { get; set; } = 0;

        public bool UseOrthro { get; set; } = false;
        public double OrthroX { get; set; } = 0;
        public double OrthroY { get; set; } = 0;
        public double OrthroScaleX { get; set; } = 1;
        public double OrthroScaleY { get; set; } = 1;

        public double RenderDistForward { get; set; } = 1.5;
        public double RenderDistBack { get; set; } = 0.15;

        public double CamFOV { get; set; } = 60;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
