using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine
{
    public class MidiState : IDisposable
    {
        public FastList<Note> Notes { get; private set; }
        public FastList<ColorChange> ColorChanges { get; private set; }
        public Tempo Tempo { get; internal set; }
        public TimeSignature TimeSignature { get; internal set; }

        public double TimeSeconds { get; internal set; }
        public double TimeTicksFractional { get; internal set; }
        public long TimeTicks => (long)TimeTicksFractional;

        public MidiFile Midi { get; internal set; }

        internal MidiState(MidiFile midi)
        {
            Notes = new FastList<Note>();
            ColorChanges = new FastList<ColorChange>();
            Midi = midi;
            Tempo = Midi.temp
        }

        public void Dispose()
        {
            Notes.Unlink();
            Notes = null;
            TimeSignature = null;
            Midi = null;
        }
    }
}
