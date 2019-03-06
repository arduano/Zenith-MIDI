using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiUtils
{
    public delegate byte[] ProcessMidiEvent(byte[] dtimeb, int dtime, byte[] data, long time);

    public class TrackFilter
    {
        public ProcessMidiEvent MidiEventFilter = null;

        public TrackFilter()
        {

        }

        byte[] ReadVariableLen(IByteReader reader, out int val)
        {
            byte[] b = new byte[5];
            byte c;
            int len = 0;
            val = 0;
            for (int i = 0; i < 4; i++)
            {
                c = reader.Read();
                if (c > 0x7F)
                {
                    b[len] = c;
                    val = (val << 7) | (c & 0x7F);
                }
                else
                {
                    b[len] = c;
                    val = val << 7 | c;
                len++;
                    break;
                }
                len++;
            }
            return b.Take(len).ToArray();
        }

        public byte[] MakeVariableLen(int i)
        {
            var b = new byte[5];
            int len = 0;
            while (true)
            {
                byte v = (byte)(i & 0x7F);
                i = i >> 7;
                if (i != 0)
                {
                    v = (byte)(v | 0x80);
                    b[len++] = v;
                }
                else
                {
                    b[len++] = v;
                    break;
                }
            }
            return b.Take(len).ToArray();
        }

        public byte[] FilterTrack(IByteReader reader)
        {
            List<byte> track = new List<byte>();
            long trackTime = 0;
            bool hasEndEvent = false;
            byte prevCommand = 0;
            while (true)
            {
                try
                {
                    int dtime = 0;
                    var dtimeb = ReadVariableLen(reader, out dtime);
                    trackTime += dtime;
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
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                            command,
                            reader.Read(),
                            reader.Read()
                        }, trackTime));
                        //byte channel = (byte)(command & 0b00001111);
                        //byte note = reader.Read();
                        //byte vel = reader.Read();
                        //Note on
                    }
                    else if (comm == 0b10000000)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                            command,
                            reader.Read(),
                            reader.Read()
                        }, trackTime));
                        //int channel = command & 0b00001111;
                        //byte note = reader.Read();
                        //byte vel = reader.Read();
                        //Note off
                    }
                    else if (comm == 0b10100000)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                            command,
                            reader.Read(),
                            reader.Read()
                        }, trackTime));
                        //int channel = command & 0b00001111;
                        //byte note = reader.Read();
                        //byte vel = reader.Read();
                    }
                    else if (comm == 0b10100000)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                            command,
                            reader.Read(),
                            reader.Read()
                        }, trackTime));
                        //int channel = command & 0b00001111;
                        //byte number = reader.Read();
                        //byte value = reader.Read();
                    }
                    else if (comm == 0b11000000)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                            command,
                            reader.Read()
                        }, trackTime));
                        //int channel = command & 0b00001111;
                        //byte program = reader.Read();
                    }
                    else if (comm == 0b11010000)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                            command,
                            reader.Read()
                        }, trackTime));
                        //int channel = command & 0b00001111;
                        //byte pressure = reader.Read();
                    }
                    else if (comm == 0b11100000)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                            command,
                            reader.Read(),
                            reader.Read()
                        }, trackTime));
                        //int channel = command & 0b00001111;
                        //byte l = reader.Read();
                        //byte m = reader.Read();
                    }
                    else if (comm == 0b10110000)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                            command,
                            reader.Read(),
                            reader.Read()
                        }, trackTime));
                        //int channel = command & 0b00001111;
                        //byte cc = reader.Read();
                        //byte vv = reader.Read();
                    }
                    else if (command == 0b11110000)
                    {
                        List<byte> b = new List<byte> { command };
                        byte c = command;
                        while (c != 0b11110111) {
                            c = reader.Read();
                            b.Add(c);
                        }
                        track.AddRange(MidiEventFilter(dtimeb, dtime, b.ToArray(), trackTime));
                    }
                    else if (command == 0b11110100 || command == 0b11110001 || command == 0b11110101 || command == 0b11111001 || command == 0b11111101)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] { command }, trackTime));
                        //printf("Undefined\n");
                    }
                    else if (command == 0b11110010)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                            command,
                            reader.Read(),
                            reader.Read()
                        }, trackTime));
                        //int channel = command & 0b00001111;
                        //byte ll = reader.Read();
                        //byte mm = reader.Read();

                    }
                    else if (command == 0b11110011)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                            command,
                            reader.Read()
                        }, trackTime));
                        //byte ss = reader.Read();
                    }
                    else if (command == 0b11110110)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] { command }, trackTime));
                    }
                    else if (command == 0b11110111)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] { command }, trackTime));
                    }
                    else if (command == 0b11111000)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] { command }, trackTime));
                    }
                    else if (command == 0b11111010)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] { command }, trackTime));
                    }
                    else if (command == 0b11111100)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] { command }, trackTime));
                    }
                    else if (command == 0b11111110)
                    {
                        track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] { command }, trackTime));
                    }
                    else if (command == 0xFF)
                    {
                        byte command2 = reader.Read();
                        if (command2 == 0x00)
                        {
                            if (reader.Read() != 2)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] { command, command2, 2 }, trackTime));
                        }
                        else if (command2 >= 0x01 && command2 <= 0x0A)
                        {
                            int size;
                            var len = ReadVariableLen(reader, out size);
                            byte[] b = new byte[size];
                            for (int i = 0; i < size; i++)
                            {
                                b[i] = reader.Read();
                            }
                            track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {command, command2 }.Concat(len.Concat(b)).ToArray(), trackTime));
                        }
                        else if (command2 == 0x20)
                        {
                            byte command3 = reader.Read();
                            if (command3 != 1)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                                command,
                                command2,
                                command3,
                                reader.Read()
                            }, trackTime));
                        }
                        else if (command2 == 0x21)
                        {
                            byte command3 = reader.Read();
                            if (command3 != 1)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                                command,
                                command2,
                                command3,
                                reader.Read()
                            }, trackTime));
                        }
                        else if (command2 == 0x2F)
                        {
                            byte command3 = reader.Read();
                            if (command3 != 0)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                                command,
                                command2,
                                command3
                            }, trackTime));
                            hasEndEvent = true;
                            break;
                        }
                        else if (command2 == 0x51)
                        {
                            byte command3 = reader.Read();
                            if (command3 != 3)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                                command,
                                command2,
                                command3,
                                reader.Read(),
                                reader.Read(),
                                reader.Read()
                            }, trackTime));
                        }
                        else if (command2 == 0x54)
                        {
                            byte command3 = reader.Read();
                            if (command3 != 5)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                                command,
                                command2,
                                command3,
                                reader.Read(),
                                reader.Read(),
                                reader.Read(),
                                reader.Read()
                            }, trackTime));
                        }
                        else if (command2 == 0x58)
                        {
                            byte command3 = reader.Read();
                            if (command3 != 4)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                                command,
                                command2,
                                command3,
                                reader.Read(),
                                reader.Read(),
                                reader.Read(),
                                reader.Read()
                            }, trackTime));
                        }
                        else if (command2 == 0x59)
                        {
                            byte command3 = reader.Read();
                            if (command3 != 2)
                            {
                                throw new Exception("Corrupt Track");
                            }
                            track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] {
                                command,
                                command2,
                                command3,
                                reader.Read(),
                                reader.Read()
                            }, trackTime));
                        }
                        else if (command2 == 0x7F)
                        {
                            int size;
                            var len = ReadVariableLen(reader, out size);
                            byte[] b = new byte[size];
                            for (int i = 0; i < size; i++)
                            {
                                b[i] = reader.Read();
                            }
                            track.AddRange(MidiEventFilter(dtimeb, dtime, new byte[] { command, command2 }.Concat(len.Concat(b)).ToArray(), trackTime));
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
                catch
                {
                    break;
                }
            }
            if (!hasEndEvent) track.AddRange(new byte[] { 0, 0xFF, 0x2F, 0x00 });
            return track.ToArray();
        }
    }
}
