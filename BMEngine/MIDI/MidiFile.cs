using OpenTK.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.MIDI
{

    public abstract class MidiFile : IDisposable
    {
        public ushort PPQ { get; protected set; }
        public int TrackCount { get; protected set; }
        public long NoteCount { get; protected set; } = 0;

        public bool PushPlaybackEvents { get; set; } = false;

        public long TickLength { get; internal set; }
        public double SecondsLength { get; internal set; }

        public abstract MidiPlayback GetMidiPlayback(double startOffset);

        public abstract void Dispose();
    }
}
