using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine
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
        public ColorChange(long pos, MidiTrack track, byte channel, Color4 col1, Color4 col2) : base(pos)
        {
            this.track = track;
            this.channel = channel;
            this.col1 = col1;
            this.col2 = col2;
        }

        public Color4 col1 { get; internal set; }
        public Color4 col2 { get; internal set; }
        public byte channel { get; internal set; }
        public MidiTrack track { get; internal set; }
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

    public class MidiTrack : IDisposable
    {
        int trackID;
        long lastStepTime = 0;
        MidiFile midi;

        public bool Ended { get; private set; } = false;
        public long NoteCount { get; private set; } = 0;
        public long TickTime { get; private set; } = 0;

        FastList<Tempo> tempoEvents = new FastList<Tempo>();
        FastList<TimeSignature> timesigEvents = new FastList<TimeSignature>();
        public IEnumerable<Tempo> TempoEvents { get => tempoEvents; }
        public IEnumerable<TimeSignature> TimesigEvents { get => timesigEvents; }

        public NoteColor[] TrackColors { get; } = new NoteColor[16];
        NoteColor[] InitialTrackColors { get; } = new NoteColor[16];
        public double TrackSeconds { get; private set; } = 0;

        bool readDelta = false;
        FastList<Note>[] unendedNotes = null;

        BufferByteReader reader;

        public void Reset()
        {
            if (unendedNotes != null) foreach (var un in unendedNotes) un.Unlink();
            reader.Reset();
            ResetColors();
            TickTime = 0;
            lastStepTime = 0;
            TrackSeconds = 0;
            Ended = false;
            readDelta = false;
            NoteCount = 0;
            unendedNotes = null;
        }

        public void ResetAndResize(int newSize)
        {
            reader.ResetAndResize(newSize);
            reader.Reset();
        }

        public void ResetColors()
        {
            for (int i = 0; i < 16; i++)
            {
                TrackColors[i] = new NoteColor() { Left = Color4.Gray, Right = Color4.Gray, isDefault = true };
            }
        }

        public void SetZeroColors()
        {
            for (int i = 0; i < 16; i++)
            {
                if (InitialTrackColors[i] != null)
                {
                    TrackColors[i].Left = InitialTrackColors[i].Left;
                    TrackColors[i].Right = InitialTrackColors[i].Right;
                }
            }
        }

        RenderSettings settings;
        public MidiTrack(int id, BufferByteReader reader, MidiFile file, RenderSettings settings)
        {
            this.settings = settings;
            midi = file;
            this.reader = reader;
            trackID = id;

            InitialTrackColors = new NoteColor[16];
        }

        public void InitialParse()
        {
            ResetColors();
            for (int i = 0; i < 16; i++) InitialTrackColors[i] = null;
            while (!Ended)
            {
                try
                {
                    ParseNextEvent(true);
                }
                catch
                {
                    break;
                }
            }
        }

        long ReadVariableLen()
        {
            byte c;
            int val = 0;
            for (int i = 0; i < 4; i++)
            {
                c = reader.ReadFast();
                if (c > 0x7F)
                {
                    val = (val << 7) | (c & 0x7F);
                }
                else
                {
                    val = val << 7 | c;
                    return val;
                }
            }
            return val;
        }

        public void Step(long time)
        {
            timebase = settings.TimeBased;
            TrackSeconds += (time - lastStepTime) / midi.ParserTempoTickMultiplier;
            lastStepTime = time;
            try
            {
                if (time >= TickTime)
                {
                    if (readDelta)
                    {
                        long d = TickTime;
                        do
                        {
                            ParseNextEvent(false);
                            if (Ended) return;
                            TickTime += ReadVariableLen();
                            readDelta = true;
                        }
                        while (TickTime == d);
                    }
                    else
                    {
                        if (Ended) return;
                        TickTime += ReadVariableLen();
                        readDelta = true;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                EndTrack();
            }
        }

        void EndTrack()
        {
            Ended = true;
            if (unendedNotes != null)
            {
                foreach (var un in unendedNotes)
                {
                    var iter = un.Iterate();
                    Note n;
                    while (iter.MoveNext(out n))
                    {
                        n.end = TickTime;
                        n.hasEnded = true;
                    }
                    un.Unlink();
                }
            }
            unendedNotes = null;
        }

        byte prevCommand = 0;
        bool timebase = false;
        public void ParseNextEvent(bool loading)
        {
            try
            {
                if (!readDelta)
                {
                    TickTime += ReadVariableLen();
                }
                readDelta = false;

                double time = TickTime;
                if (timebase)
                    time = TrackSeconds;

                byte command = reader.ReadFast();
                if (command < 0x80)
                {
                    reader.Pushback = command;
                    command = prevCommand;
                }
                prevCommand = command;
                byte comm = (byte)(command & 0xF0);
                if (comm == 0x90 || comm == 0x80)
                {
                    byte channel = (byte)(command & 0x0F);
                    byte note = reader.Read();
                    byte vel = reader.ReadFast();

                    if (loading)
                    {
                        if (comm == 0x90 && vel != 0)
                        {
                            NoteCount++;
                        }
                        return;
                    }

                    if (settings.PreviewAudioEnabled && (comm == 0x80 || vel > 10))
                    {
                        midi.PlaybackEvents.Add(new PlaybackEvent()
                        {
                            time = TrackSeconds,
                            val = command | (note << 8) | (vel << 16)
                        });
                    }

                    if (unendedNotes == null)
                    {
                        unendedNotes = new FastList<Note>[256 * 16];
                        for (int i = 0; i < unendedNotes.Length; i++) unendedNotes[i] = new FastList<Note>();
                    }

                    if (comm == 0x80 || vel == 0)
                    {
                        var l = unendedNotes[note << 4 | channel];
                        if (!l.ZeroLen)
                        {
                            Note n = l.Pop();
                            n.end = time;
                            n.hasEnded = true;
                        }
                    }
                    else
                    {
                        Note n = new Note();
                        n.start = time;
                        n.key = note;
                        n.color = TrackColors[channel];
                        n.channel = channel;
                        n.vel = vel;
                        n.track = trackID;
                        unendedNotes[note << 4 | channel].Add(n);
                        midi.Notes.Add(n);
                    }
                }
                else if (comm == 0xA0)
                {
                    int channel = command & 0x0F;
                    byte note = reader.Read();
                    byte vel = reader.Read();

                    if (loading) return;

                    if (settings.PreviewAudioEnabled)
                    {
                        midi.PlaybackEvents.Add(new PlaybackEvent()
                        {
                            time = TrackSeconds,
                            val = command | (note << 8) | (vel << 16)
                        });
                    }
                }
                else if (comm == 0xB0)
                {
                    int channel = command & 0x0F;
                    byte cc = reader.Read();
                    byte vv = reader.Read();

                    if (loading) return;

                    if (settings.PreviewAudioEnabled)
                    {
                        midi.PlaybackEvents.Add(new PlaybackEvent()
                        {
                            time = TrackSeconds,
                            val = command | (cc << 8) | (vv << 16)
                        });
                    }
                }
                else if (comm == 0xC0)
                {
                    int channel = command & 0x0F;
                    byte program = reader.Read();

                    if (loading) return;

                    if (settings.PreviewAudioEnabled)
                    {
                        midi.PlaybackEvents.Add(new PlaybackEvent()
                        {
                            time = TrackSeconds,
                            val = command | (program << 8)
                        });
                    }
                }
                else if (comm == 0xD0)
                {
                    int channel = command & 0x0F;
                    byte pressure = reader.Read();

                    if (loading) return;

                    if (settings.PreviewAudioEnabled)
                    {
                        midi.PlaybackEvents.Add(new PlaybackEvent()
                        {
                            time = TrackSeconds,
                            val = command | (pressure << 8)
                        });
                    }
                }
                else if (comm == 0xE0)
                {
                    int channel = command & 0x0F;
                    byte l = reader.Read();
                    byte m = reader.Read();

                    if (loading) return;

                    if (settings.PreviewAudioEnabled)
                    {
                        midi.PlaybackEvents.Add(new PlaybackEvent()
                        {
                            time = TrackSeconds,
                            val = command | (l << 8) | (m << 16)
                        });
                    }
                }
                else if (command == 0xF0)
                {
                    while (reader.Read() != 0b11110111) ;
                }
                else if (command == 0b11110010)
                {
                    int channel = command & 0x0F;
                    byte ll = reader.Read();
                    byte mm = reader.Read();
                }
                else if (command == 0b11110011)
                {
                    byte ss = reader.Read();
                }
                else if (command == 0xFF)
                {
                    command = reader.Read();
                    int size = (int)ReadVariableLen();

                    if (command == 0x0A)
                    {
                        byte[] data = new byte[size];
                        for (int i = 0; i < size; i++)
                        {
                            data[i] = reader.Read();
                        }
                        if (data.Length == 8 || data.Length == 12)
                        {
                            if (data[0] == 0x00 &&
                                data[1] == 0x0F)
                            {
                                Color4 col1 = new Color4(data[4], data[5], data[6], data[7]);
                                Color4 col2;
                                if (data.Length == 12)
                                    col2 = new Color4(data[8], data[9], data[10], data[11]);
                                else col2 = col1;
                                if (loading)
                                {
                                    if (data[2] < 0x10)
                                    {
                                        InitialTrackColors[data[2]] = new NoteColor() { Left = col1, Right = col2 };
                                    }
                                    else if (data[2] == 0x7F)
                                    {
                                        for (int i = 0; i < 16; i++)
                                            InitialTrackColors[i] = new NoteColor() { Left = col1, Right = col2 };
                                    }
                                }
                                else
                                {
                                    if (data[2] < 0x10 || data[2] == 0x7F)
                                    {
                                        var c = new ColorChange(TickTime, this, data[2], col1, col2);
                                        midi.ColorChanges.Add(c);
                                    }
                                }
                            }
                        }
                    }
                    else if (command == 0x2F)
                    {
                        if (size != 0)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        EndTrack();
                    }
                    else if (command == 0x51)
                    {
                        if (size != 3)
                        {
                            throw new Exception("Corrupt Track");
                        }

                        int btempo = 0;
                        for (int i = 0; i != 3; i++)
                            btempo = (int)((btempo << 8) | reader.Read());

                        if (loading)
                        {
                            tempoEvents.Add(new Tempo(TickTime, btempo));
                        }
                        else
                        {
                            midi.ParserTempoTickMultiplier = ((double)midi.Division / btempo) * 1000;
                        }
                    }
                    else if (command == 0x58)
                    {
                        if (size != 4)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        int nn = reader.ReadFast();
                        int dd = reader.ReadFast();
                        if (loading)
                        {
                            dd = (int)Math.Pow(2, dd);
                            timesigEvents.Add(new TimeSignature(TickTime, nn, dd));
                        }
                        reader.Skip(2);
                    }
                    else
                    {
                        reader.Skip(size);
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                EndTrack();
            }
            catch
            { }
        }

        public void Dispose()
        {
            reader.Dispose();
        }
    }
}
