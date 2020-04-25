using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZenithEngine
{
    public class BufferByteReader
    {
        long pos;
        int bufferSize;
        int bufferPos;
        int maxBufferPos;
        long streamStart;
        long streamLen;
        DiskReadProvider stream;
        byte[] buffer;
        byte[] bufferNext;
        BlockingCollection<byte[]> readReturn = new BlockingCollection<byte[]>();
        bool sentRequest = false;

        public BufferByteReader(DiskReadProvider stream, int buffersize, long streamstart, long streamlen)
        {
            if (buffersize > streamlen) buffersize = (int)streamlen;
            this.bufferSize = buffersize;
            buffer = new byte[buffersize];
            bufferNext = new byte[buffersize];
            this.streamStart = streamstart;
            this.streamLen = streamlen;
            this.stream = stream;
            UpdateBuffer(pos, true);
        }

        void UpdateBuffer(long pos, bool first = false)
        {
            if (first)
            {
                if (sentRequest)
                {
                    readReturn.Take();
                }
                stream.Request(new ReadDiskRequest(pos + streamStart, bufferSize, bufferNext, readReturn));
                sentRequest = true;
            }
            bufferNext = readReturn.Take();
            sentRequest = false;
            Buffer.BlockCopy(bufferNext, 0, buffer, 0, bufferSize);
            stream.Request(new ReadDiskRequest(pos + streamStart + bufferSize, bufferSize, bufferNext, readReturn));
            sentRequest = true;
            maxBufferPos = (int)Math.Min(streamLen - pos + 1, bufferSize);
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
            byte b = buffer[bufferPos++];
            if (bufferPos < maxBufferPos) return b;
            else if (bufferPos >= bufferSize)
            {
                pos += bufferPos;
                bufferPos = 0;
                UpdateBuffer(pos);
                return b;
            }
            else throw new IndexOutOfRangeException();
        }

        public byte ReadFast()
        {
            byte b = buffer[bufferPos++];
            if (bufferPos < maxBufferPos) return b;
            else if (bufferPos >= bufferSize)
            {
                pos += bufferPos;
                bufferPos = 0;
                UpdateBuffer(pos);
                return b;
            }
            else throw new IndexOutOfRangeException();
        }

        public void Reset()
        {
            pos = 0;
            bufferPos = 0;
            UpdateBuffer(pos, true);
        }

        public void ResetAndResize(int buffersize)
        {
            if (buffersize > streamLen) buffersize = (int)streamLen;
            this.bufferSize = buffersize;
            buffer = new byte[buffersize];
            bufferNext = new byte[buffersize];
            Reset();
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
                bufferPos++;
                if (bufferPos < maxBufferPos) continue;
                if (bufferPos >= bufferSize)
                {
                    pos += bufferPos;
                    bufferPos = 0;
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

    public struct ReadDiskRequest
    {
        public long from;
        public int length;
        public byte[] buffer;
        public BlockingCollection<byte[]> output;

        public ReadDiskRequest(long from, int length, byte[] buffer, BlockingCollection<byte[]> output)
        {
            this.from = from;
            this.length = length;
            this.buffer = buffer;
            this.output = output;
        }
    }

    public class DiskReadProvider : IDisposable
    {
        BlockingCollection<ReadDiskRequest> requests = new BlockingCollection<ReadDiskRequest>();
        Stream reader;

        public DiskReadProvider(Stream reader)
        {
            this.reader = reader;
            Task.Run(() =>
            {
                foreach(var request in requests.GetConsumingEnumerable())
                {
                    if(reader.Position != request.from)
                    {
                        reader.Position = request.from;
                    }
                    reader.Read(request.buffer, 0, request.length);
                    request.output.Add(request.buffer);
                }
            });
        }

        public void Request(ReadDiskRequest req)
        {
            requests.Add(req);
        }

        public void Dispose()
        {
            requests.CompleteAdding();
            reader.Dispose();
        }
    }
}
