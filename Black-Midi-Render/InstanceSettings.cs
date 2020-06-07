using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith
{
    class InstanceSettings : INotifyPropertyChanged
    {
        public bool TimeBased { get; set; } = false;

        public bool IgnoreColorEvents { get; set; } = false;

        public long LastBGChangeTime { get; set; } = -1;
        public string BGImage { get; set; } = null;

        public bool IncludeAudio { get; set; } = false;
        public string AudioInputPath { get; set; } = "";

        public bool UseBitrate { get; set; } = true;
        public bool CustomFFmpeg { get; set; } = false;
        public int Bitrate { get; set; } = 20000;
        public int RenderCRF { get; set; } = 17;
        public string RenderCRFPreset { get; set; } = "medium";
        public bool FFmpegDebug { get; set; } = false;
        public string FFmpegCustomArgs { get; set; } = "";

        public bool IsRendering { get; set; } = false;
        public string RenderOutput { get; set; } = "";
        public bool IsRenderingMask { get; set; } = false;
        public string RenderMaskOutput { get; set; } = "";



        public event PropertyChangedEventHandler PropertyChanged;
    }
}
