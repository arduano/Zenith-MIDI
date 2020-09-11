using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using DX.WPF;

namespace Zenith.Models
{
    public class RenderProgressModel : INotifyPropertyChanged
    {
        public double Progress => (Seconds - StartSeconds) / EndSeconds;

        public DXElement PreviewElement { get; set; }

        public double StartSeconds { get; }
        public double EndSeconds { get; }
        public double Seconds { get; private set; }

        public long NotesRendering { get; private set; }

        public RenderPipeline Pipeline { get; }

        public double FPS { get; private set; }
        public long LastFrameNumber { get; private set; }

        Stopwatch frameTimer = null;

        public RenderProgressModel(RenderPipeline pipeline)
        {
            this.Pipeline = pipeline;
            StartSeconds = pipeline.StartTime;
            EndSeconds = pipeline.EndTime;

            Pipeline.RenderProgress += Pipeline_RenderProgress;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Pipeline_RenderProgress(object sender, RenderProgressData e)
        {
            Seconds = Pipeline.Playback.PlayerPositionSeconds;
            NotesRendering = Pipeline.Playback.LastIterateNoteCount;

            if(frameTimer == null)
            {
                frameTimer = new Stopwatch();
                frameTimer.Start();
                FPS = 0;
            }
            else
            {
                var seconds = frameTimer.Elapsed.TotalSeconds;
                frameTimer.Reset();
                frameTimer.Start();
                var diff = e.RenderFrameNumber - LastFrameNumber;
                var smooth = 1;
                FPS = ((FPS * smooth) + diff) / (smooth + seconds);
            }

            LastFrameNumber = e.RenderFrameNumber;
        }
    }
}
