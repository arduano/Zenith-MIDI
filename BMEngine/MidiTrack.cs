using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine
{
    public class Note
    {
        public double start;
        public double end;
        public bool hasEnded;
        public byte channel;
        public byte key;
        public byte vel;
        public bool delete = false;
        public object meta = null;
        public int track;
        public NoteColor color;
    }

    public class NoteColor
    {
        public Color4 left;
        public Color4 right;
        public bool isDefault = true;
    }

    public struct PlaybackEvent
    {
        public double pos;
        public int val;
    }

    public class Tempo
    {
        public long pos;
        public int tempo;
    }

    public class ColorChange
    {
        public double pos;
        public Color4 col1;
        public Color4 col2;
        public byte channel;
        public MidiTrack track;
    }

    public class TimeSignature
    {
        public int numerator { get; internal set; }
        public int denominator { get; internal set; }
    }

    public class MidiTrack : IDisposable
    {
        public int trackID;

        public bool trackEnded = false;

        public long trackTime = 0;
        public long lastStepTime = 0;
        public double trackFlexTime = 0;
        public long noteCount = 0;
        public int zerothTempo = -1;

        byte channelPrefix = 0;

        MidiFile midi;

        public FastList<Note>[] UnendedNotes = null;
        public LinkedList<Tempo> Tempos = new LinkedList<Tempo>();

        FastList<Note> globalDisplayNotes;
        FastList<Tempo> globalTempoEvents;
        FastList<ColorChange> globalColorEvents;
        FastList<PlaybackEvent> globalPlaybackEvents;

        public FastList<Tempo> TempoEvents = new FastList<Tempo>();

        public NoteColor[] trkColors;
        public NoteColor[] zeroTickTrkColors;

        public TimeSignature foundTimeSig = null;

        bool readDelta = false;

        BufferByteReader reader;

        public void Reset()
        {
            if (UnendedNotes != null) foreach (var un in UnendedNotes) un.Unlink();
            reader.Reset();
            ResetColors();
            trackTime = 0;
            lastStepTime = 0;
            trackFlexTime = 0;
            trackEnded = false;
            readDelta = false;
            channelPrefix = 0;
            noteCount = 0;
            UnendedNotes = null;
        }

        public void ResetAndResize(int newSize)
        {
            reader.ResetAndResize(newSize);
            reader.Reset();
        }

        public void ResetColors()
        {
            trkColors = new NoteColor[16];
            for (int i = 0; i < 16; i++)
            {
                trkColors[i] = new NoteColor() { left = Color4.Gray, right = Color4.Gray, isDefault = true };
            }
        }

        public void SetZeroColors()
        {
            for (int i = 0; i < 16; i++)
            {
                if (zeroTickTrkColors[i] != null)
                {
                    trkColors[i].left = zeroTickTrkColors[i].left;
                    trkColors[i].right = zeroTickTrkColors[i].right;
                }
            }
        }

        RenderSettings settings;
        public MidiTrack(int id, BufferByteReader reader, MidiFile file, RenderSettings settings)
        {
            this.settings = settings;
            globalDisplayNotes = file.globalDisplayNotes;
            globalTempoEvents = file.globalTempoEvents;
            globalColorEvents = file.globalColorEvents;
            globalPlaybackEvents = file.globalPlaybackEvents;
            midi = file;
            this.reader = reader;
            trackID = id;
            ResetColors();

            zeroTickTrkColors = new NoteColor[16];
            for (int i = 0; i < 16; i++) zeroTickTrkColors[i] = null;
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
            timebase = settings.timeBasedNotes;
            trackFlexTime += (time - lastStepTime) / midi.tempoTickMultiplier;
            lastStepTime = time;
            try
            {
                if (time >= trackTime)
                {
                    if (readDelta)
                    {
                        long d = trackTime;
                        do
                        {
                            ParseNextEvent(false);
                            if (trackEnded) return;
                            trackTime += ReadVariableLen();
                            readDelta = true;
                        }
                        while (trackTime == d);
                    }
                    else
                    {
                        if (trackEnded) return;
                        trackTime += ReadVariableLen();
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
            trackEnded = true;
            if (UnendedNotes != null)
                foreach (var un in UnendedNotes)
                {
                    var iter = un.Iterate();
                    Note n;
                    while (iter.MoveNext(out n))
                    {
                        n.end = trackTime;
                        n.hasEnded = true;
                    }
                    un.Unlink();
                }
            UnendedNotes = null;
        }

        byte prevCommand = 0;
        bool timebase = false;
        public void ParseNextEvent(bool loading)
        {
            try
            {
                if (!readDelta)
                {
                    trackTime += ReadVariableLen();
                }
                readDelta = false;

                double time = trackTime;
                if (timebase)
                    time = trackFlexTime;

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
                            noteCount++;
                        }
                        return;
                    }

                    if (settings.playbackEnabled && vel > 10)
                    {
                        globalPlaybackEvents.Add(new PlaybackEvent()
                        {
                            pos = time,
                            val = command | (note << 8) | (vel << 16)
                        });
                    }

                    if (comm == 0x80 || vel == 0)
                    {
                        var l = UnendedNotes[note << 4 | channel];
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
                        n.color = trkColors[channel];
                        n.channel = channel;
                        n.vel = vel;
                        n.track = trackID;
                        if (UnendedNotes == null)
                        {
                            UnendedNotes = new FastList<Note>[256 * 16];
                            for (int i = 0; i < 256 * 16; i++)
                            {
                                UnendedNotes[i] = new FastList<Note>();
                            }
                        }
                        UnendedNotes[note << 4 | channel].Add(n);
                        globalDisplayNotes.Add(n);
                    }
                }
                else if (comm == 0xA0)
                {
                    int channel = command & 0x0F;
                    byte note = reader.Read();
                    byte vel = reader.Read();

                    if (loading) return;

                    if (settings.playbackEnabled)
                    {
                        globalPlaybackEvents.Add(new PlaybackEvent()
                        {
                            pos = time,
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

                    if (settings.playbackEnabled)
                    {
                        globalPlaybackEvents.Add(new PlaybackEvent()
                        {
                            pos = time,
                            val = command | (cc << 8) | (vv << 16)
                        });
                    }
                }
                else if (comm == 0xC0)
                {
                    int channel = command & 0x0F;
                    byte program = reader.Read();

                    if (loading) return;

                    if (settings.playbackEnabled)
                    {
                        globalPlaybackEvents.Add(new PlaybackEvent()
                        {
                            pos = time,
                            val = command | (program << 8)
                        });
                    }
                }
                else if (comm == 0xD0)
                {
                    int channel = command & 0x0F;
                    byte pressure = reader.Read();

                    if (loading) return;

                    if (settings.playbackEnabled)
                    {
                        globalPlaybackEvents.Add(new PlaybackEvent()
                        {
                            pos = time,
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

                    if (settings.playbackEnabled)
                    {
                        globalPlaybackEvents.Add(new PlaybackEvent()
                        {
                            pos = time,
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
                                        zeroTickTrkColors[data[2]] = new NoteColor() { left = col1, right = col2 };
                                    }
                                    else if (data[2] == 0x7F)
                                    {
                                        for (int i = 0; i < 16; i++)
                                            zeroTickTrkColors[i] = new NoteColor() { left = col1, right = col2 };
                                    }
                                }
                                else
                                {
                                    if (data[2] < 0x10 || data[2] == 0x7F)
                                    {
                                        var c = new ColorChange() { pos = time, col1 = col1, col2 = col2, channel = data[2], track = this };
                                        globalColorEvents.Add(c);
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
                            if (trackTime == 0)
                            {
                                zerothTempo = btempo;
                            }

                            Tempo t = new Tempo();
                            t.pos = trackTime;
                            t.tempo = btempo;
                            TempoEvents.Add(t);
                        }
                        else
                        {
                            if (!timebase)
                            {
                                Tempo t = new Tempo();
                                t.pos = trackTime;
                                t.tempo = btempo;

                                lock (globalTempoEvents)
                                {
                                    globalTempoEvents.Add(t);
                                }
                            }
                            midi.tempoTickMultiplier = ((double)midi.division / btempo) * 1000;
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

        public void Dispose()
        {
            reader.Dispose();
        }
    }
}
