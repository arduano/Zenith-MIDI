using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiUtils
{
    public class MidiWriter
    {
        Stream writer;

        long chunkStart = 0;

        public MidiWriter(Stream writer)
        {
            this.writer = writer;
        }

        public void Write(string text)
        {
            for (int i = 0; i < text.Length; i++) writer.WriteByte((byte)text[i]);
        }

        public void Write(byte[] data)
        {
            writer.Write(data, 0, data.Length);
        }

        public void Write(ushort v)
        {
            for (int i = 1; i >= 0; i--) writer.WriteByte((byte)((v >> (i * 8)) & 0xFF));
        }

        public void Write(uint v)
        {
            for (int i = 3; i >= 0; i--) writer.WriteByte((byte)((v >> (i * 8)) & 0xFF));
        }

        public void Write(byte v)
        {
            writer.WriteByte(v);
        }

        public void WriteFormat(ushort s)
        {
            long pos = writer.Position;
            writer.Position = 8;
            Write((ushort)s);
            writer.Position = pos;
        }

        public void WriteNtrks(ushort s)
        {
            long pos = writer.Position;
            writer.Position = 10;
            Write((ushort)s);
            writer.Position = pos;
        }

        public void WriteDivision(ushort s)
        {
            long pos = writer.Position;
            writer.Position = 12;
            Write((ushort)s);
            writer.Position = pos;
        }

        public void Init()
        {
            writer.Position = 0;
            Write("MThd");
            Write((int)6);
            WriteFormat(1);
            WriteNtrks(0);
            WriteDivision(96);
        }

        public void InitTrack()
        {
            chunkStart = writer.Length;
            writer.Position = chunkStart;
            Write("MTrk");
            Write((int)0);
        }

        public void EndTrack()
        {
            uint len = (uint)(writer.Position - chunkStart) - 8;
            writer.Position = chunkStart + 4;
            Write(len);
            writer.Position = writer.Length;
        }

        public void Close()
        {
            writer.Flush();
            writer.Close();
        }
    }
}
