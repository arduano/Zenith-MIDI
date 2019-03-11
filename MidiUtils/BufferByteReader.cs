using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MidiUtils
{
    public class BufferByteReader : IByteReader
    {
        long pos;
        int buffersize;
        int bufferpos;
        int maxbufferpos;
        long streamstart;
        long streamlen;
        Stream stream;
        byte[] buffer;

        public BufferByteReader(Stream stream, int buffersize, long streamstart, long streamlen)
        {
            if (buffersize > streamlen) buffersize = (int)streamlen;
            this.buffersize = buffersize;
            this.streamstart = streamstart;
            this.streamlen = streamlen;
            this.stream = stream;
            buffer = new byte[buffersize];
            UpdateBuffer();
        }

        void UpdateBuffer()
        {
            lock (stream)
            {
                stream.Position = pos + streamstart;
                stream.Read(buffer, 0, buffersize);
            }
            maxbufferpos = (int)Math.Min(streamlen - pos + 1, buffersize);
        }

        public long Location => pos;

        public int Pushback { get; set; } = -1;

        public byte Read()
        {
            if(Pushback != -1)
            {
                byte _b = (byte)Pushback;
                Pushback = -1;
                return _b;
            }
            byte b = buffer[bufferpos++];
            if (bufferpos < maxbufferpos) return b;
            else if (bufferpos >= buffersize)
            {
                pos += bufferpos;
                bufferpos = 0;
                UpdateBuffer();
                return b;
            }
            else throw new IndexOutOfRangeException();
        }

        public void Reset()
        {
            pos = 0;
            bufferpos = 0;
            UpdateBuffer();
        }

        public void Skip(int count)
        {
            bufferpos += count;
            if (bufferpos < maxbufferpos) return;
            else if (bufferpos >= buffersize)
            {
                pos += bufferpos;
                bufferpos = 0;
                UpdateBuffer();
            }
            else throw new IndexOutOfRangeException();
        }

        public void Dispose()
        {
            buffer = null;
        }
    }
}
