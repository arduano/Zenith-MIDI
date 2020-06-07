using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.MIDI.Disk
{
    public delegate void DiskTrackParseProgress(DiskMidiTrack track, double progress);

    public class DiskMidiTrack : IMidiPlaybackTrack, IMidiTrack, IDisposable
    {
        enum TrackMode
        {
            Parse,
            Playback
        }

        TrackMode Mode { get; }
        internal bool IsParseMode => Mode == TrackMode.Parse;
        internal bool IsPlaybackMode => Mode == TrackMode.Playback;

        public int ID { get; }

        long lastStepTime = 0;

        public bool Ended { get; private set; } = false;
        public long NoteCount { get; private set; } = 0;
        public long ParseTimeTicks { get; private set; } = 0;
        public double ParseTimeSeconds { get; private set; } = 0;

        public long TickLength => ParseTimeTicks;

        public MidiPlayback MidiPlayback { get; private set; }

        FastList<Tempo> tempoEvents = new FastList<Tempo>();
        FastList<TimeSignature> timesigEvents = new FastList<TimeSignature>();
        public IEnumerable<Tempo> TempoEvents { get => tempoEvents; }
        public IEnumerable<TimeSignature> TimesigEvents { get => timesigEvents; }

        public NoteColor[] TrackColors { get; } = new NoteColor[16];
        public NoteColor[] InitialTrackColors { get; } = new NoteColor[16];

        public long LastNoteTick { get; private set; }

        bool readDelta = false;
        FastList<Note>[] unendedNotes = null;

        BufferByteReader reader;

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

        private DiskMidiTrack(int id, BufferByteReader reader, TrackMode mode, NoteColor[] initialTrackColors = null)
        {
            ResetColors();
            Mode = mode;
            this.reader = reader;
            ID = id;

            if (initialTrackColors == null)
            {
                InitialTrackColors = new NoteColor[16];
            }
            else
            {
                InitialTrackColors = initialTrackColors;
            }
        }

        public static DiskMidiTrack NewParserTrack(
            int id,
            BufferByteReader reader,
            DiskTrackParseProgress progressCallback,
            int callbackRate)
        {
            var track = new DiskMidiTrack(id, reader, TrackMode.Parse);
            track.InitialParse(progressCallback, callbackRate);
            reader.Dispose();
            return track;
        }

        public static DiskMidiTrack NewParserTrack(
            int id,
            BufferByteReader reader)
        {
            return NewParserTrack(id, reader, null, int.MaxValue);
        }

        public static DiskMidiTrack NewPlayerTrack(
            int id,
            BufferByteReader reader,
            MidiPlayback playback)
        {
            var track = new DiskMidiTrack(id, reader, TrackMode.Playback);
            track.MidiPlayback = playback;
            return track;
        }

        void InitialParse(DiskTrackParseProgress progressCallback, int callbackRate)
        {

            for (int i = 0; i < 16; i++) InitialTrackColors[i] = null;
            while (!Ended)
            {
                try
                {
                    ParseNextEvent();
                }
                catch
                {
                    break;
                }

                if (NoteCount % callbackRate == 0)
                {
                    progressCallback?.Invoke(this, reader.Location / (double)reader.Length);
                }
            }

            progressCallback?.Invoke(this, 1);
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
            ParseTimeSeconds += (time - lastStepTime) * MidiPlayback.ParserTempoTickMultiplier;
            lastStepTime = time;
            try
            {
                if (time >= ParseTimeTicks)
                {
                    if (readDelta)
                    {
                        long d = ParseTimeTicks;
                        do
                        {
                            ParseNextEvent();
                            if (Ended) return;
                            ParseTimeTicks += ReadVariableLen();
                            readDelta = true;
                        }
                        while (ParseTimeTicks == d);
                    }
                    else
                    {
                        if (Ended) return;
                        ParseTimeTicks += ReadVariableLen();
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
                        n.end = ParseTimeTicks;
                        n.hasEnded = true;
                    }
                    un.Unlink();
                }
            }
            Dispose();
            unendedNotes = null;
        }

        byte prevCommand = 0;
        public void ParseNextEvent()
        {
            bool loading = IsParseMode;

            try
            {
                if (!readDelta)
                {
                    ParseTimeTicks += ReadVariableLen();
                }
                readDelta = false;

                double time = ParseTimeTicks;
                if (MidiPlayback != null && MidiPlayback.TimeBased)
                    time = ParseTimeSeconds;

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

                    LastNoteTick = ParseTimeTicks;

                    if (loading)
                    {
                        if (comm == 0x90 && vel != 0)
                        {
                            NoteCount++;
                        }
                        return;
                    }

                    void sendEv()
                    {
                        if (MidiPlayback.PushPlaybackEvents)
                        {
                            MidiPlayback.PlaybackEvents.Add(new PlaybackEvent()
                            {
                                time = ParseTimeSeconds,
                                val = command | (note << 8) | (vel << 16)
                            });
                        }
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
                            if (n.vel > 10) sendEv();
                        }
                    }
                    else
                    {
                        if (vel > 10) sendEv();
                        Note n = new Note();
                        n.start = time;
                        n.key = note;
                        n.color = TrackColors[channel];
                        n.channel = channel;
                        n.vel = vel;
                        n.track = ID;
                        unendedNotes[note << 4 | channel].Add(n);
                        MidiPlayback.Notes.Add(n);
                    }
                }
                else if (comm == 0xA0)
                {
                    int channel = command & 0x0F;
                    byte note = reader.Read();
                    byte vel = reader.Read();

                    if (loading) return;

                    if (MidiPlayback.PushPlaybackEvents)
                    {
                        MidiPlayback.PlaybackEvents.Add(new PlaybackEvent()
                        {
                            time = ParseTimeSeconds,
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

                    if (MidiPlayback.PushPlaybackEvents)
                    {
                        MidiPlayback.PlaybackEvents.Add(new PlaybackEvent()
                        {
                            time = ParseTimeSeconds,
                            val = command | (cc << 8) | (vv << 16)
                        });
                    }
                }
                else if (comm == 0xC0)
                {
                    int channel = command & 0x0F;
                    byte program = reader.Read();

                    if (loading) return;

                    if (MidiPlayback.PushPlaybackEvents)
                    {
                        MidiPlayback.PlaybackEvents.Add(new PlaybackEvent()
                        {
                            time = ParseTimeSeconds,
                            val = command | (program << 8)
                        });
                    }
                }
                else if (comm == 0xD0)
                {
                    int channel = command & 0x0F;
                    byte pressure = reader.Read();

                    if (loading) return;

                    if (MidiPlayback.PushPlaybackEvents)
                    {
                        MidiPlayback.PlaybackEvents.Add(new PlaybackEvent()
                        {
                            time = ParseTimeSeconds,
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

                    if (MidiPlayback.PushPlaybackEvents)
                    {
                        MidiPlayback.PlaybackEvents.Add(new PlaybackEvent()
                        {
                            time = ParseTimeSeconds,
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
                                        var c = new ColorChange(ParseTimeTicks, this, data[2], col1, col2);
                                        MidiPlayback.ColorChanges.Add(c);
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
                            tempoEvents.Add(new Tempo(ParseTimeTicks, btempo));
                        }
                        else
                        {
                            MidiPlayback.ParserTempoTickMultiplier = ((double)btempo / MidiPlayback.Midi.PPQ) / 1000000;
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
                            timesigEvents.Add(new TimeSignature(ParseTimeTicks, nn, dd));
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

        bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            reader.Dispose();
        }
    }
}
