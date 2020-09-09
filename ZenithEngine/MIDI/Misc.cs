using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.MIDI
{
    public abstract class PositionedEvent
    {
        protected PositionedEvent(double position)
        {
            Position = position;
        }

        public double Position { get; internal set; }
    }

    public class Note
    {
        public double Start { get; internal set; }
        public double End { get; internal set; }
        public bool HasEnded { get; internal set; }
        public byte Channel { get; internal set; }
        public byte Key { get; internal set; }
        public byte Vel { get; internal set; }
        public bool Delete { get; set; } = false;
        public object Meta { get; set; } = null;
        public int Track { get; internal set; }
        public NoteColor Color { get; internal set; }
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

        public void Set(Color4 left, Color4 right)
        {
            isDefault = false;
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
        public ColorChange(double pos, IMidiPlaybackTrack track, byte channel, Color4 col1, Color4 col2) : base(pos)
        {
            this.Track = track;
            this.Channel = channel;
            this.Col1 = col1;
            this.Col2 = col2;
        }

        public Color4 Col1 { get; internal set; }
        public Color4 Col2 { get; internal set; }
        public byte Channel { get; internal set; }
        public IMidiPlaybackTrack Track { get; internal set; }
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
