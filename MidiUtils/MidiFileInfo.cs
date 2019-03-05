using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiUtils
{
    public class MidiFileInfo
    {
        public class MidiChunkPointer
        {
            public long Start { get; set; }
            public uint Length { get; set; }
        }

        public ushort Format { get; private set; }
        public ushort Division { get; private set; }
        public int TrackCount { get; private set; }

        public MidiChunkPointer[] Tracks { get; private set; }

        private MidiFileInfo() { }

        public static MidiFileInfo Parse(Stream reader)
        {
            var info = new MidiFileInfo();
            ParseHeaderChunk(reader, info);
            var tracks = new List<MidiChunkPointer>();
            while(reader.Position < reader.Length)
            {
                ParseTrackChunk(reader, tracks);
            }
            info.Tracks = tracks.ToArray();
            info.TrackCount = tracks.Count;
            return info;
        }

        static void AssertText(Stream reader, string text)
        {
            foreach (char c in text)
            {
                if (reader.ReadByte() != c)
                {
                    throw new Exception("Corrupt chunk headers");
                }
            }
        }

        static uint ReadInt32(Stream reader)
        {
            uint length = 0;
            for (int i = 0; i != 4; i++)
                length = (uint)((length << 8) | (byte)reader.ReadByte());
            return length;
        }

        static ushort ReadInt16(Stream reader)
        {
            ushort length = 0;
            for (int i = 0; i != 2; i++)
                length = (ushort)((length << 8) | (byte)reader.ReadByte());
            return length;
        }

        static void ParseHeaderChunk(Stream reader, MidiFileInfo info)
        {
            AssertText(reader, "MThd");
            uint length = (uint)ReadInt32(reader);
            if (length != 6) throw new Exception("Header chunk size isn't 6");
            info.Format = ReadInt16(reader);
            ReadInt16(reader);
            info.Division = ReadInt16(reader);
            if (info.Format == 2) throw new Exception("Midi type 2 not supported");
            if (info.Division < 0) throw new Exception("Division < 0 not supported");
        }

        static void ParseTrackChunk(Stream reader, List<MidiChunkPointer> tracks)
        {
            AssertText(reader, "MTrk");
            uint length = (uint)ReadInt32(reader);
            tracks.Add(new MidiChunkPointer() { Start = reader.Position, Length = length });
            reader.Position += length;
            Console.WriteLine("Track " + tracks.Count + ", Size " + length);
        }
    }
}
