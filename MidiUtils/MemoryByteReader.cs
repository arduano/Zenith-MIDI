using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidiUtils;

namespace MidiUtils
{
    public class MemoryByteReader : IByteReader
    {
        byte[] bytes;
        long pos;
        public MemoryByteReader(byte[] data)
        {
            bytes = data;
        }

        public long Location => pos;

        public int Pushback { get; set; } = -1;

        public void Dispose()
        {
            bytes = null;
        }

        public byte Read()
        {
            if (Pushback != -1)
            {
                byte _b = (byte)Pushback;
                Pushback = -1;
                return _b;
            }
            return bytes[pos++];
        }


        public void Reset()
        {
            pos = 0;
        }

        public void Skip(int count)
        {
            pos += count;
        }
    }
}
