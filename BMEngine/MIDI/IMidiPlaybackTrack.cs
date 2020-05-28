using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.MIDI
{
    public interface IMidiPlaybackTrack : IDisposable
    {
        int ID { get; }
        public bool Ended { get; }
        public long NoteCount { get; }
        public long ParseTimeTicks { get; }
        public double ParseTimeSeconds { get; }
        public NoteColor[] TrackColors { get; }

        MidiPlayback MidiPlayback { get; }

        void Step(long time);
    }
}
