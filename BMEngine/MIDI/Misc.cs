using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.MIDI
{
    public abstract class PositionedEvent
    {
        protected PositionedEvent(long position)
        {
            Position = position;
        }

        public long Position { get; internal set; }
    }

    public class Note
    {
        public double start { get; internal set; }
        public double end { get; internal set; }
        public bool hasEnded { get; internal set; }
        public byte channel { get; internal set; }
        public byte key { get; internal set; }
        public byte vel { get; internal set; }
        public bool delete { get; internal set; } = false;
        public object meta { get; set; } = null;
        public int track { get; internal set; }
        public NoteColor color { get; internal set; }
    }

    public class NoteColor
    {
        public Color4 Left { get; set; }
        public Color4 Right { get; set; }
        internal bool isDefault { get; set; } = true;

        public void Alter(Color4 left, Color4 right)
        {
            if (!isDefault) return;
            Left = left;
            Right = right;
        }
    }

    public struct PlaybackEvent
    {
        public double time;
        public int val;
    }

    public class Tempo
    {
        public Tempo(long pos, int rawTempo)
        {
            this.pos = pos;
            this.rawTempo = rawTempo;
            this.realTempo = 60000000.0 / rawTempo;
        }

        public long pos { get; internal set; }
        public int rawTempo { get; internal set; }
        public double realTempo { get; internal set; }
    }

    public class ColorChange : PositionedEvent
    {
        public ColorChange(long pos, IMidiPlaybackTrack track, byte channel, Color4 col1, Color4 col2) : base(pos)
        {
            this.track = track;
            this.channel = channel;
            this.col1 = col1;
            this.col2 = col2;
        }

        public Color4 col1 { get; internal set; }
        public Color4 col2 { get; internal set; }
        public byte channel { get; internal set; }
        public IMidiPlaybackTrack track { get; internal set; }
    }

    public class TimeSignature : PositionedEvent
    {
        public TimeSignature(long pos, int numerator, int denominator) : base(pos)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        public int Numerator { get; internal set; }
        public int Denominator { get; internal set; }
    }
}
