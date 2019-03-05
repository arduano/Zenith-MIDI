using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Black_Midi_Render
{
    class Note
    {
        public long start;
        public long end;
        public bool hasEnded;
        public byte channel;
        public byte note;
        public byte vel;
        public MidiTrack track;
    }

    class Tempo
    {
        public long pos;
        public int tempo;
    }

    class ColorChange
    {
        public long pos;
        public Color4 col1;
        public Color4 col2;
        public byte channel;
        public MidiTrack track;
    }
}
