using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMEngine
{
    public class MidiFile : IDisposable
    {
        Stream MidiFileReader;
        public ushort division;
        public int trackcount;
        public ushort format;

        public int zerothTempo = 500000;

        List<long> trackBeginnings = new List<long>();
        List<uint> trackLengths = new List<uint>();

        public MidiTrack[] tracks;

        public MidiInfo info;

        public long maxTrackTime;
        public long noteCount = 0;

        public long currentSyncTime = 0;

        public FastList<Note> globalDisplayNotes = new FastList<Note>();
        public FastList<Tempo> globalTempoEvents = new FastList<Tempo>();
        public FastList<ColorChange> globalColorEvents = new FastList<ColorChange>();
        public FastList<PlaybackEvent> globalPlaybackEvents = new FastList<PlaybackEvent>();

        public long lastTempoTick = 0;
        public double lastTempoTime = 0;
        public double tempoTickMultiplier = 0;

        public int unendedTracks = 0;

        RenderSettings settings;

        public MidiFile(string filename, RenderSettings settings)
        {
            this.settings = settings;
            MidiFileReader = new StreamReader(filename).BaseStream;
            ParseHeaderChunk();
            while (MidiFileReader.Position < MidiFileReader.Length)
            {
                ParseTrackChunk();
            }
            tracks = new MidiTrack[trackcount];

            Console.WriteLine("Loading tracks into memory");
            LoadAndParseAll(true);
            Console.WriteLine("Loaded!");
            Console.WriteLine("Note count: " + noteCount);
            unendedTracks = trackcount;
            info = new MidiInfo()
            {
                division = division,
                firstTempo = zerothTempo,
                noteCount = noteCount,
                tickLength = maxTrackTime,
                trackCount = trackcount
            };
            lastTempoTick = 0;
            lastTempoTime = 0;
            tempoTickMultiplier = (double)division / 500000 * 1000;
        }

        void AssertText(string text)
        {
            foreach (char c in text)
            {
                if (MidiFileReader.ReadByte() != c)
                {
                    throw new Exception("Corrupt chunk headers");
                }
            }
        }

        uint ReadInt32()
        {
            uint length = 0;
            for (int i = 0; i != 4; i++)
                length = (uint)((length << 8) | (byte)MidiFileReader.ReadByte());
            return length;
        }

        ushort ReadInt16()
        {
            ushort length = 0;
            for (int i = 0; i != 2; i++)
                length = (ushort)((length << 8) | (byte)MidiFileReader.ReadByte());
            return length;
        }

        void ParseHeaderChunk()
        {
            AssertText("MThd");
            uint length = ReadInt32();
            if (length != 6) throw new Exception("Header chunk size isn't 6");
            format = ReadInt16();
            ReadInt16();
            division = ReadInt16();
            if (format == 2) throw new Exception("Midi type 2 not supported");
            if (division < 0) throw new Exception("Division < 0 not supported");
        }

        void ParseTrackChunk()
        {
            AssertText("MTrk");
            uint length = ReadInt32();
            trackBeginnings.Add(MidiFileReader.Position);
            trackLengths.Add(length);
            MidiFileReader.Position += length;
            trackcount++;
            Console.WriteLine("Track " + trackcount + ", Size " + length);
        }


        public bool ParseUpTo(long targetTime)
        {
            if(settings.timeBasedNotes) targetTime = (long)((targetTime - lastTempoTime) * tempoTickMultiplier + lastTempoTick);
            lock (globalDisplayNotes)
            {
                for (; currentSyncTime <= targetTime && settings.running; currentSyncTime++)
                {
                    int ut = 0;
                    for (int trk = 0; trk < trackcount; trk++)
                    {
                        var t = tracks[trk];
                        if (!t.trackEnded) ut++;
                        t.Step(currentSyncTime);
                    }
                    unendedTracks = ut;
                }
                foreach (var t in tracks)
                {
                    if (!t.trackEnded) return true;
                }
                return false;
            }
        }

        public void LoadAndParseAll(bool useBufferStream = false)
        {
            long[] tracklens = new long[tracks.Length];
            int p = 0;
            Parallel.For(0, tracks.Length, (i) =>
            {
                var reader = new BufferByteReader(MidiFileReader, 1<<20, trackBeginnings[i], trackLengths[i]);
                tracks[i] = new MidiTrack(i, reader, this, settings);
                var t = tracks[i];
                while (!t.trackEnded)
                {
                    try
                    {
                        t.ParseNextEventFast();
                    }
                    catch
                    {
                        break;
                    }
                }
                noteCount += t.noteCount;
                tracklens[i] = t.trackTime;
                if (t.zerothTempo != -1)
                {
                    zerothTempo = t.zerothTempo;
                }
                if (useBufferStream)
                {
                    t.Dispose();
                    tracks[i] = new MidiTrack(i,
                        new BufferByteReader(MidiFileReader, settings.maxTrackBufferSize, trackBeginnings[i], trackLengths[i]),
                        this, settings);
                }
                else t.Reset();
                Console.WriteLine("Loaded track " + p++ + "/" + tracks.Length);
                GC.Collect();
            });
            maxTrackTime = tracklens.Max();
            unendedTracks = trackcount;
        }

        public void Reset()
        {
            globalDisplayNotes.Unlink();
            globalTempoEvents.Unlink();
            globalColorEvents.Unlink();
            globalPlaybackEvents.Unlink();
            currentSyncTime = 0;
            unendedTracks = trackcount;
            lastTempoTick = 0;
            lastTempoTime = 0;
            tempoTickMultiplier = (double)division / 500000 * 1000;
            foreach (var t in tracks) t.Reset();
        }

        public void Dispose()
        {
            foreach (var t in tracks) t.Dispose();
            MidiFileReader.Dispose();
        }
    }
}
