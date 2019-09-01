using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BMEngine
{
    public class BufferByteReader
    {
        long pos;
        int buffersize;
        int bufferpos;
        int maxbufferpos;
        long streamstart;
        long streamlen;
        Stream stream;
        byte[] buffer;
        byte[] bufferNext;
        Task nextReader = null;

        public BufferByteReader(Stream stream, int buffersize, long streamstart, long streamlen)
        {
            if (buffersize > streamlen) buffersize = (int)streamlen;
            this.buffersize = buffersize;
            this.streamstart = streamstart;
            this.streamlen = streamlen;
            this.stream = stream;
            buffer = new byte[buffersize];
            bufferNext = new byte[buffersize];
            UpdateBuffer(pos, true);
        }

        void UpdateBuffer(long pos, bool first = false)
        {
            if (first)
            {
                nextReader = Task.Run(() =>
                {
                    lock (stream)
                    {
                        stream.Position = pos + streamstart;
                        stream.Read(bufferNext, 0, buffersize);
                    }
                });
            }
            nextReader.GetAwaiter().GetResult();
            Buffer.BlockCopy(bufferNext, 0, buffer, 0, buffersize);
            nextReader = Task.Run(() =>
            {
                lock (stream)
                {
                    stream.Position = pos + streamstart + buffersize;
                    stream.Read(bufferNext, 0, buffersize);
                }
            });
            nextReader.GetAwaiter().GetResult();
            //lock (stream)
            //{
            //    stream.Position = pos + streamstart;
            //    stream.Read(buffer, 0, buffersize);
            //}
            maxbufferpos = (int)Math.Min(streamlen - pos + 1, buffersize);
        }

        public long Location => pos;

        public int Pushback = -1;

        public byte Read()
        {
            if (Pushback != -1)
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
                UpdateBuffer(pos);
                return b;
            }
            else throw new IndexOutOfRangeException();
        }

        public byte ReadFast()
        {
            byte b = buffer[bufferpos++];
            if (bufferpos < maxbufferpos) return b;
            else if (bufferpos >= buffersize)
            {
                pos += bufferpos;
                bufferpos = 0;
                UpdateBuffer(pos);
                return b;
            }
            else throw new IndexOutOfRangeException();
        }

        public void Reset()
        {
            pos = 0;
            bufferpos = 0;
            UpdateBuffer(pos, true);
        }

        public void Skip(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if(Pushback != -1)
                {
                    Pushback = -1;
                    continue;
                }
                bufferpos++;
                if (bufferpos < maxbufferpos) continue;
                if (bufferpos >= buffersize)
                {
                    pos += bufferpos;
                    bufferpos = 0;
                    UpdateBuffer(pos);
                }
                else throw new IndexOutOfRangeException();
            }
        }

        public void Dispose()
        {
            buffer = null;
        }
    }
}
