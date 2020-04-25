using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine
{
    struct TrackPos
    {
        public long start;
        public uint length;

        public TrackPos(long start, uint length)
        {
            this.start = start;
            this.length = length;
        }
    }

    public class MidiFile : IDisposable
    {
        Stream MidiFileReader;
        public ushort division;
        public int trackCount;
        public ushort format;

        public int zerothTempo = 500000;

        List<TrackPos> trackPositions = new List<TrackPos>();

        public MidiTrack[] tracks;

        public MidiInfo info;

        public long maxTrackTime;
        public long noteCount = 0;

        public long currentSyncTime = 0;
        public double currentFlexSyncTime = 0;

        public FastList<Note> globalDisplayNotes = new FastList<Note>();
        public FastList<Tempo> globalTempoEvents = new FastList<Tempo>();
        public FastList<ColorChange> globalColorEvents = new FastList<ColorChange>();
        public FastList<PlaybackEvent> globalPlaybackEvents = new FastList<PlaybackEvent>();

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
            tracks = new MidiTrack[trackCount];

            Console.WriteLine("Loading tracks into memory, biggest tracks first.");
            Console.WriteLine("Please expect this to start slow, especially on bigger midis.");
            info = new MidiInfo();
            LoadAndParseAll(true);
            Console.WriteLine("Loaded!");
            Console.WriteLine("Note count: " + noteCount);
            unendedTracks = trackCount;

            info.division = division;
            info.firstTempo = zerothTempo;
            info.noteCount = noteCount;
            info.tickLength = maxTrackTime;
            info.trackCount = trackCount;
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
            trackPositions.Add(new TrackPos(MidiFileReader.Position, length));
            MidiFileReader.Position += length;
            trackCount++;
            Console.WriteLine("Track " + trackCount + ", Size " + length);
        }


        public bool ParseUpTo(double targetTime)
        {
            lock (globalDisplayNotes)
            {
                if (settings.timeBasedNotes)
                    for (; currentFlexSyncTime <= targetTime && settings.running; currentSyncTime++)
                    {
                        currentFlexSyncTime += 1 / tempoTickMultiplier;
                        int ut = 0;
                        for (int trk = 0; trk < trackCount; trk++)
                        {
                            var t = tracks[trk];
                            if (!t.trackEnded)
                            {
                                ut++;
                                t.Step(currentSyncTime);
                            }
                        }
                        unendedTracks = ut;
                    }
                else
                    for (; currentSyncTime <= targetTime && settings.running; currentSyncTime++)
                    {
                        int ut = 0;
                        for (int trk = 0; trk < trackCount; trk++)
                        {
                            var t = tracks[trk];
                            if (!t.trackEnded)
                            {
                                ut++;
                                t.Step(currentSyncTime);
                            }
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
            long[] trackLengths = new long[tracks.Length];
            int p = 0;
            List<FastList<Tempo>> tempos = new List<FastList<Tempo>>();
            var diskReader = new DiskReadProvider(MidiFileReader);
            int[] trackOrder = new int[tracks.Length];
            for (int i = 0; i < trackOrder.Length; i++) trackOrder[i] = i;
            Array.Sort(trackPositions.Select(t => t.length).ToArray(), trackOrder);
            trackOrder = trackOrder.Reverse().ToArray();
            ParallelFor(0, tracks.Length, Environment.ProcessorCount * 3, (_i) =>
               {
                   int i = trackOrder[_i];
                   var reader = new BufferByteReader(diskReader, 10000000, trackPositions[i].start, trackPositions[i].length);
                   tracks[i] = new MidiTrack(i, reader, this, settings);
                   var t = tracks[i];
                   while (!t.trackEnded)
                   {
                       try
                       {
                           t.ParseNextEvent(true);
                       }
                       catch
                       {
                           break;
                       }
                   }
                   noteCount += t.noteCount;
                   trackLengths[i] = t.trackTime;
                   if (t.foundTimeSig != null)
                       info.timeSig = t.foundTimeSig;
                   if (t.zerothTempo != -1)
                   {
                       zerothTempo = t.zerothTempo;
                   }
                   lock (tempos)
                   {
                       tempos.Add(t.TempoEvents);
                       t.ResetAndResize(100000);
                       Console.WriteLine("Loaded track " + p++ + "/" + tracks.Length);
                   }
               });
            maxTrackTime = trackLengths.Max();
            Console.WriteLine("Processing Tempos");
            LinkedList<Tempo> Tempos = new LinkedList<Tempo>();
            var iters = tempos.Select(t => t.GetEnumerator()).ToArray();
            bool[] unended = new bool[iters.Length];
            for (int i = 0; i < iters.Length; i++) unended[i] = iters[i].MoveNext();
            while (true)
            {
                long smallest = 0;
                bool first = true;
                int id = 0;
                for (int i = 0; i < iters.Length; i++)
                {
                    if (!unended[i]) continue;
                    if (first)
                    {
                        smallest = iters[i].Current.pos;
                        id = i;
                        first = false;
                        continue;
                    }
                    if (iters[i].Current.pos < smallest)
                    {
                        smallest = iters[i].Current.pos;
                        id = i;
                    }
                }
                if (first)
                {
                    break;
                }
                Tempos.AddLast(iters[id].Current);
                unended[id] = iters[id].MoveNext();
            }

            double time = 0;
            long ticks = maxTrackTime;
            double multiplier = ((double)500000 / division) / 1000000;
            long lastt = 0;
            foreach (var t in Tempos)
            {
                var offset = t.pos - lastt;
                time += offset * multiplier;
                ticks -= offset;
                lastt = t.pos;
                multiplier = ((double)t.tempo / division) / 1000000;
            }

            time += ticks * multiplier;

            info.secondsLength = time;

            maxTrackTime = trackLengths.Max();
            unendedTracks = trackCount;
        }

        void ParallelFor(int from, int to, int threads, Action<int> func)
        {
            Dictionary<int, Task> tasks = new Dictionary<int, Task>();
            BlockingCollection<int> completed = new BlockingCollection<int>();

            void RunTask(int i)
            {
                var t = new Task(() => {
                    func(i);
                    completed.Add(i);
                });
                tasks.Add(i, t);
                t.Start();
            }

            void TryTake()
            {
                var t = completed.Take();
                tasks.Remove(t);
            }

            for (int i = from; i < to; i++)
            {
                RunTask(i);
                if (tasks.Count > threads) TryTake();
            }

            while (completed.Count > 0 || tasks.Count > 0) TryTake();
        }

        public void SetZeroColors()
        {
            foreach (var t in tracks) t.SetZeroColors();
        }

        public void Reset()
        {
            globalDisplayNotes.Unlink();
            globalTempoEvents.Unlink();
            globalColorEvents.Unlink();
            globalPlaybackEvents.Unlink();
            currentSyncTime = 0;
            currentFlexSyncTime = 0;
            unendedTracks = trackCount;
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
