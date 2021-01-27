using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtfUnknown;

namespace ZenithEngine.MIDI.Disk
{
    public struct DiskTrackParseProgress
    {
        public DiskTrackParseProgress(DiskMidiTrack track, double progress)
        {
            Track = track;
            Progress = progress;
        }

        public DiskMidiTrack Track { get; }
        public double Progress { get; }
    }

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
        FastList<Scale> scaleEvents = new FastList<Scale>();
        FastList<TextData> lyricsEvents = new FastList<TextData>();
        public IEnumerable<Tempo> TempoEvents { get => tempoEvents; }
        public IEnumerable<TimeSignature> TimesigEvents { get => timesigEvents; }
        public IEnumerable<Scale> ScaleEvents { get => scaleEvents; }
        public IEnumerable<TextData> LyricsEvents { get => lyricsEvents; }

        public NoteColor[] TrackColors { get; } = new NoteColor[16];
        public NoteColor[] InitialTrackColors { get; } = new NoteColor[16];

        public long LastNoteTick { get; private set; }

        bool readDelta = false;
        FastList<Note>[] unendedNotes = null;

        BufferByteReader reader;

        public void ResetColors()
        {
            var col = new Color4(0.5f, 0.5f, 0.5f, 1);
            for (int i = 0; i < 16; i++)
            {
                var c = InitialTrackColors[i];
                if (c == null)
                {
                    TrackColors[i] = new NoteColor() { Left = col, Right = col, isDefault = true };
                }
                else
                {
                    TrackColors[i] = new NoteColor() { Left = c.Left, Right = c.Right, isDefault = true };
                }
            }
        }

        public void SetZeroColors()
        {
            for (int i = 0; i < 16; i++)
            {
                if (InitialTrackColors[i] != null)
                {
                    TrackColors[i].Set(InitialTrackColors[i].Left, InitialTrackColors[i].Right);
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
            SetZeroColors();
        }

        public static DiskMidiTrack NewParserTrack(
            int id,
            BufferByteReader reader,
            IProgress<DiskTrackParseProgress> progress,
            int callbackRate)
        {
            var track = new DiskMidiTrack(id, reader, TrackMode.Parse);
            track.InitialParse(progress, callbackRate);
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
            MidiPlayback playback,
            NoteColor[] initialTrackColors = null)
        {
            var track = new DiskMidiTrack(id, reader, TrackMode.Playback, initialTrackColors);
            track.MidiPlayback = playback;
            return track;
        }

        void InitialParse(IProgress<DiskTrackParseProgress> progress, int callbackRate)
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
                    progress?.Report(new DiskTrackParseProgress(this, reader.Location / (double)reader.Length));
                }
            }

            progress?.Report(new DiskTrackParseProgress(this, 1));
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
                        n.End = ParseTimeTicks;
                        n.HasEnded = true;
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
                {
                    time = ParseTimeSeconds * 1000;
                }

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
                            n.End = time;
                            n.HasEnded = true;
                            if (n.Vel > 10) sendEv();
                        }
                    }
                    else
                    {
                        if (vel > 10) sendEv();
                        Note n = new Note();
                        n.Start = time;
                        n.Key = note;
                        n.Color = TrackColors[channel];
                        n.Channel = channel;
                        n.Vel = vel;
                        n.Track = ID;
                        unendedNotes[note << 4 | channel].Add(n);

                        if (MidiPlayback.NotesKeysSeparated)
                        {
                            MidiPlayback.NotesKeyed[note].Add(n);
                        }
                        else
                        {
                            MidiPlayback.NotesSingle.Add(n);
                        }
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

                    if (command == 0x05)
                    {
                        byte[] rawlyrics = new byte[size];
                        for (int i = 0; i < size; i++)
                        {
                            rawlyrics[i] = reader.Read();
                        }
                        lyricsEvents.Add(new TextData(ParseTimeTicks, rawlyrics));
                    }
                    else if (command == 0x0A)
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
                                Color4 col1 = new Color4(data[4] / 255.0f, data[5] / 255.0f, data[6] / 255.0f, data[7] / 255.0f);
                                Color4 col2;
                                if (data.Length == 12)
                                    col2 = new Color4(data[8] / 255.0f, data[9] / 255.0f, data[10] / 255.0f, data[11] / 255.0f);
                                else col2 = col1;
                                if (loading)
                                {
                                    if (ParseTimeTicks == 0)
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
                                }
                                else
                                {
                                    if (data[2] < 0x10 || data[2] == 0x7F)
                                    {
                                        var c = new ColorChange(ParseTimeSeconds, this, data[2], col1, col2);
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
                    else if (command == 0x59)
                    {
                        if (size != 2)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        sbyte sf = Convert.ToSByte(reader.ReadFast());
                        bool mi = Convert.ToBoolean(reader.ReadFast());
                        if (loading)
                        {
                            scaleEvents.Add(new Scale(ParseTimeTicks, sf, mi));
                        }
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
