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
    public enum KeyboardRenderers
    {
        Legacy,
        New,
        Flat
    }

    public enum NoteRenderers
    {
        Shaded,
        Flat
    }

    public class RenderSettings : INotifyPropertyChanged
    {
        public int FPS { get; set; } = 60;

        public int PixelWidth { get; set; } = 1920;
        public int PixelHeight { get; set; } = 1080;
        public int SSAA { get; set; } = 1;

        public bool IsRendering { get; set; } = false;
        public string RenderOutput { get; set; } = "";
        public bool IsRenderingMask { get; set; } = false;
        public string RenderMaskOutput { get; set; } = "";
        public bool VSync { get; set; } = true;
        public double RenderStartDelay { get; set; } = 0;

        public double PreviewSpeed { get; set; } = 1;

        public bool IncludeAudio { get; set; } = false;
        public string AudioInputPath { get; set; } = "";

        public bool UseBitrate { get; set; } = true;
        public bool CustomFFmpeg { get; set; } = false;
        public int Bitrate { get; set; } = 20000;
        public int RenderCRF { get; set; } = 17;
        public string RenderCRFPreset { get; set; } = "medium";
        public bool FFmpegDebug { get; set; } = false;
        public string FFmpegCustomArgs { get; set; } = "";

        public bool Running { get; set; } = false;

        public bool PreviewAudioEnabled { get; set; } = true;

        public bool RealtimePlayback { get; set; } = true;

        public double CurrentFPS { get; set; } = 0;

        public bool TimeBased { get; set; } = false;

        public bool IgnoreColorEvents { get; set; } = false;

        public long LastBGChangeTime { get; set; } = -1;
        public string BGImage { get; set; } = null;


        public event Action PauseToggled;
        private bool paused = false;
        public bool Paused
        {
            get => paused;
            set
            {
                paused = value;
                PauseToggled();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
