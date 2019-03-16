using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidiUtils;

namespace BMEngine
{
    public class Note
    {
        public long start;
        public long end;
        public bool hasEnded;
        public byte channel;
        public byte note;
        public byte vel;
        public bool delete = false;
        public object meta = null;
        public MidiTrack track;
    }

    public class Tempo
    {
        public long pos;
        public int tempo;
    }

    public class ColorChange
    {
        public long pos;
        public Color4 col1;
        public Color4 col2;
        public byte channel;
        public MidiTrack track;
    }

    public class MidiTrack : IDisposable
    {
        public int trackID;

        public bool trackEnded = false;

        public long trackTime = 0;
        public long prevTrackTime = 0;
        public long noteCount = 0;
        public int zerothTempo = -1;

        byte channelPrefix = 0;

        public FastList<Note>[] UnendedNotes = null;
        public LinkedList<Tempo> Tempos = new LinkedList<Tempo>();

        FastList<Note> globalDisplayNotes;
        FastList<Tempo> globalTempoEvents;
        FastList<ColorChange> globalColorEvents;

        public Color4[] trkColor;

        bool readDelta = false;

        IByteReader reader;

        public void Reset()
        {
            if (UnendedNotes != null) foreach (var un in UnendedNotes) un.Unlink();
            reader.Reset();
            ResetColors();
            trackTime = 0;
            prevTrackTime = 0;
            trackEnded = false;
            readDelta = false;
            channelPrefix = 0;
            noteCount = 0;
            UnendedNotes = null;
        }

        public void ResetColors()
        {
            trkColor = new Color4[32];
            for (int i = 0; i < 16; i++)
            {
                trkColor[i * 2] = Color4.Gray;//Color4.FromHsv(new OpenTK.Vector4((trackID * 16 + i) * 1.36271f % 1, 1.0f, 1, 1f));
                trkColor[i * 2 + 1] = Color4.Gray;//Color4.FromHsv(new OpenTK.Vector4((trackID * 16 + i) * 1.36271f % 1, 1.0f, 1, 1f));
            }
        }

        RenderSettings settings;
        public MidiTrack(int id, IByteReader reader, MidiFile file, RenderSettings settings)
        {
            this.settings = settings;
            globalDisplayNotes = file.globalDisplayNotes;
            globalTempoEvents = file.globalTempoEvents;
            globalColorEvents = file.globalColorEvents;
            this.reader = reader;
            trackID = id;
            ResetColors();
        }

        long ReadVariableLen()
        {
            byte c;
            int val = 0;
            for (int i = 0; i < 4; i++)
            {
                c = reader.Read();
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
            try
            {
                if (time >= trackTime)
                {
                    if (readDelta)
                    {
                        long d = trackTime;
                        do
                        {
                            ParseNextEvent();
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
        public void ParseNextEvent(bool readOnly = false)
        {
            try
            {
                if (!readDelta)
                {
                    trackTime += ReadVariableLen();
                }
                readDelta = false;
                byte command = reader.Read();
                if (command < 0x80)
                {
                    reader.Pushback = command;
                    command = prevCommand;
                }
                prevCommand = command;
                byte comm = (byte)(command & 0b11110000);
                if (comm == 0b10010000)
                {
                    byte channel = (byte)(command & 0b00001111);
                    byte note = reader.Read();
                    byte vel = reader.Read();
                    if (vel == 0)
                    {
                        if (!readOnly)
                            try
                            {
                                Note n = UnendedNotes[note << 4 | channel].Pop();
                                n.end = trackTime;
                                n.hasEnded = true;
                            }
                            catch { }
                    }
                    else
                    {
                        Note n = new Note();
                        n.start = trackTime;
                        n.note = note;
                        n.track = this;
                        n.channel = channel;
                        n.vel = vel;
                        if (!readOnly)
                        {
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
                }
                else if (comm == 0b10000000)
                {
                    int channel = command & 0b00001111;
                    byte note = reader.Read();
                    byte vel = reader.Read();

                    if (!readOnly)
                        try
                        {
                            Note n = UnendedNotes[note << 4 | channel].Pop();
                            n.end = trackTime;
                            n.hasEnded = true;
                        }
                        catch { }
                }
                else if (comm == 0b10100000)
                {
                    int channel = command & 0b00001111;
                    byte note = reader.Read();
                    byte vel = reader.Read();
                }
                else if (comm == 0b11000000)
                {
                    int channel = command & 0b00001111;
                    byte program = reader.Read();
                }
                else if (comm == 0b11010000)
                {

                    int channel = command & 0b00001111;
                    byte pressure = reader.Read();
                }
                else if (comm == 0b11100000)
                {
                    int channel = command & 0b00001111;
                    byte l = reader.Read();
                    byte m = reader.Read();
                }
                else if (comm == 0b10110000)
                {
                    int channel = command & 0b00001111;
                    byte cc = reader.Read();
                    byte vv = reader.Read();
                }
                else if (command == 0b11110000)
                {
                    while (reader.Read() != 0b11110111) ;
                }
                else if (command == 0b11110100 || command == 0b11110001 || command == 0b11110101 || command == 0b11111001 || command == 0b11111101)
                {
                    //printf("Undefined\n");
                }
                else if (command == 0b11110010)
                {
                    int channel = command & 0b00001111;
                    byte ll = reader.Read();
                    byte mm = reader.Read();

                }
                else if (command == 0b11110011)
                {
                    byte ss = reader.Read();
                }
                else if (command == 0b11110110)
                {
                }
                else if (command == 0b11110111)
                {
                }
                else if (command == 0b11111000)
                {
                }
                else if (command == 0b11111010)
                {
                }
                else if (command == 0b11111100)
                {
                }
                else if (command == 0b11111110)
                {
                }
                else if (command == 0xFF)
                {
                    command = reader.Read();
                    if (command == 0x00)
                    {
                        if (reader.Read() != 2)
                        {
                            throw new Exception("Corrupt Track");
                        }
                    }
                    else if (command == 0x01)
                    {
                        int size = (int)ReadVariableLen();
                        char[] text = new char[size];
                        for (int i = 0; i < size; i++)
                        {
                            text[i] = (char)reader.Read();
                        }
                        string str = new string(text);
                    }
                    else if (command == 0x02)
                    {
                        int size = (int)ReadVariableLen();
                        char[] text = new char[size];
                        for (int i = 0; i < size; i++)
                        {
                            text[i] = (char)reader.Read();
                        }
                        string str = new string(text);
                    }
                    else if (command == 0x03)
                    {
                        int size = (int)ReadVariableLen();
                        char[] text = new char[size];
                        for (int i = 0; i < size; i++)
                        {
                            text[i] = (char)reader.Read();
                        }
                        string str = new string(text);
                    }
                    else if (command == 0x04)
                    {
                        int size = (int)ReadVariableLen();
                        char[] text = new char[size];
                        for (int i = 0; i < size; i++)
                        {
                            text[i] = (char)reader.Read();
                        }
                        string str = new string(text);
                    }
                    else if (command == 0x05)
                    {
                        int size = (int)ReadVariableLen();
                        char[] text = new char[size];
                        for (int i = 0; i < size; i++)
                        {
                            text[i] = (char)reader.Read();
                        }
                        string str = new string(text);
                    }
                    else if (command == 0x06)
                    {
                        int size = (int)ReadVariableLen();
                        char[] text = new char[size];
                        for (int i = 0; i < size; i++)
                        {
                            text[i] = (char)reader.Read();
                        }
                        string str = new string(text);
                    }
                    else if (command == 0x07)
                    {
                        int size = (int)ReadVariableLen();
                        char[] text = new char[size];
                        for (int i = 0; i < size; i++)
                        {
                            text[i] = (char)reader.Read();
                        }
                        string str = new string(text);
                    }
                    else if (command == 0x08)
                    {
                        int size = (int)ReadVariableLen();
                        char[] text = new char[size];
                        for (int i = 0; i < size; i++)
                        {
                            text[i] = (char)reader.Read();
                        }
                        string str = new string(text);
                    }
                    else if (command == 0x09)
                    {
                        int size = (int)ReadVariableLen();
                        char[] text = new char[size];
                        for (int i = 0; i < size; i++)
                        {
                            text[i] = (char)reader.Read();
                        }
                        string str = new string(text);
                    }
                    else if (command == 0x0A)
                    {
                        int size = (int)ReadVariableLen();
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
                                if (data[2] < 0x10 || data[2] == 0x7F)
                                {
                                    var c = new ColorChange() { pos = trackTime, col1 = col1, col2 = col2, channel = data[2], track = this };
                                    globalColorEvents.Add(c);
                                }
                            }
                        }
                    }
                    else if (command == 0x20)
                    {
                        command = reader.Read();
                        if (command != 1)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        channelPrefix = reader.Read();
                    }
                    else if (command == 0x21)
                    {
                        command = reader.Read();
                        if (command != 1)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        reader.Skip(1);
                        //TODO:  MIDI port
                    }
                    else if (command == 0x2F)
                    {
                        command = reader.Read();
                        if (command != 0)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        EndTrack();
                    }
                    else if (command == 0x51)
                    {
                        command = reader.Read();
                        if (command != 3)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        int btempo = 0;
                        for (int i = 0; i != 3; i++)
                            btempo = (int)((btempo << 8) | reader.Read());
                        Tempo t = new Tempo();
                        t.pos = trackTime;
                        t.tempo = btempo;

                        //if (trackID <= 1)
                        {
                            if (!readOnly)
                            {
                                lock (globalTempoEvents)
                                {
                                    globalTempoEvents.Add(t);
                                }
                            }
                        }
                    }
                    else if (command == 0x54)
                    {
                        command = reader.Read();
                        if (command != 5)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        reader.Skip(4);
                    }
                    else if (command == 0x58)
                    {
                        command = reader.Read();
                        if (command != 4)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        reader.Skip(4);
                    }
                    else if (command == 0x59)
                    {
                        command = reader.Read();
                        if (command != 2)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        reader.Skip(2);
                        //TODO: Key Signature
                    }
                    else if (command == 0x7F)
                    {
                        int size = (int)ReadVariableLen();
                        byte[] data = new byte[size];
                        for (int i = 0; i < size; i++)
                        {
                            data[i] = reader.Read();
                        }
                    }
                    else
                    {
                        throw new Exception("Corrupt Track");
                    }
                }
                else
                {
                    throw new Exception("Corrupt Track");
                }
            }
            catch (IndexOutOfRangeException)
            {
                EndTrack();
            }
            catch { }
        }

        public void ParseNextEventFast()
        {
            long _t = 0;
            try
            {
                _t = trackTime;
                trackTime += ReadVariableLen();
                prevTrackTime = _t;
                byte command = reader.Read();
                if (command < 0x80)
                {
                    reader.Pushback = command;
                    command = prevCommand;
                }
                prevCommand = command;
                byte comm = (byte)(command & 0b11110000);
                if (comm == 0b10010000)
                {
                    byte channel = (byte)(command & 0b00001111);
                    reader.Skip(2);
                    noteCount++;
                }
                else if (comm == 0b10000000)
                {
                    int channel = command & 0b00001111;
                    reader.Skip(2);
                }
                else if (comm == 0b10100000)
                {
                    int channel = command & 0b00001111;
                    reader.Skip(2);
                }
                else if (comm == 0b11000000)
                {
                    int channel = command & 0b00001111;
                    reader.Skip(1);
                }
                else if (comm == 0b11010000)
                {

                    int channel = command & 0b00001111;
                    reader.Skip(1);
                }
                else if (comm == 0b11100000)
                {
                    int channel = command & 0b00001111;
                    reader.Skip(2);
                }
                else if (comm == 0b10110000)
                {
                    int channel = command & 0b00001111;
                    reader.Skip(2);
                }
                else if (command == 0b11110000)
                {
                    while (reader.Read() != 0b11110111) ;
                }
                else if (command == 0b11110100 || command == 0b11110001 || command == 0b11110101 || command == 0b11111001 || command == 0b11111101)
                {
                    //printf("Undefined\n");
                }
                else if (command == 0b11110010)
                {
                    int channel = command & 0b00001111;
                    reader.Skip(2);

                }
                else if (command == 0b11110011)
                {
                    byte ss = reader.Read();
                }
                else if (command == 0b11110110)
                {
                }
                else if (command == 0b11110111)
                {
                }
                else if (command == 0b11111000)
                {
                }
                else if (command == 0b11111010)
                {
                }
                else if (command == 0b11111100)
                {
                }
                else if (command == 0b11111110)
                {
                }
                else if (command == 0xFF)
                {
                    command = reader.Read();
                    if (command == 0x00)
                    {
                        if (reader.Read() != 2)
                        {
                            throw new Exception("Corrupt Track");
                        }
                    }
                    else if (command >= 0x01 &&
                            command <= 0x0A)
                    {
                        int size = (int)ReadVariableLen();
                        reader.Skip(size);
                    }
                    else if (command == 0x20)
                    {
                        command = reader.Read();
                        if (command != 1)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        channelPrefix = reader.Read();
                    }
                    else if (command == 0x21)
                    {
                        command = reader.Read();
                        if (command != 1)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        reader.Skip(1);
                        //TODO:  MIDI port
                    }
                    else if (command == 0x2F)
                    {
                        command = reader.Read();
                        if (command != 0)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        EndTrack();
                    }
                    else if (command == 0x51)
                    {
                        command = reader.Read();
                        if (command != 3)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        int btempo = 0;
                        for (int i = 0; i != 3; i++)
                            btempo = (int)((btempo << 8) | reader.Read());
                        if (trackTime == 0)
                        {
                            zerothTempo = btempo;
                        }
                    }
                    else if (command == 0x54)
                    {
                        command = reader.Read();
                        if (command != 5)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        reader.Skip(4);
                    }
                    else if (command == 0x58)
                    {
                        command = reader.Read();
                        if (command != 4)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        reader.Skip(4);
                    }
                    else if (command == 0x59)
                    {
                        command = reader.Read();
                        if (command != 2)
                        {
                            throw new Exception("Corrupt Track");
                        }
                        reader.Skip(2);
                        //TODO: Key Signature
                    }
                    else if (command == 0x7F)
                    {
                        int size = (int)ReadVariableLen();
                        reader.Skip(size);
                    }
                    else
                    {
                        throw new Exception("Corrupt Track");
                    }
                }
                else
                {
                    throw new Exception("Corrupt Track");
                }
            }
            catch (IndexOutOfRangeException)
            {
                EndTrack();
            }
            catch { }
        }

        public void Dispose()
        {
            reader.Dispose();
        }
    }
}
