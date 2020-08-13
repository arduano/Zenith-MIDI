using OpenTK.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.MIDI.Disk
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

    public enum MidiParseStep
    {
        Discover,
        Parse,
    }

    public struct MidiParseProgress
    {
        public int Parsed { get; }
        public int Discovered { get; }
        public MidiParseStep Step { get; }

        public MidiParseProgress(int parsed, int discovered)
        {
            Step = MidiParseStep.Parse;
            Parsed = parsed;
            Discovered = discovered;
        }

        public MidiParseProgress(int discovered)
        {
            Step = MidiParseStep.Discover;
            Parsed = 0;
            Discovered = discovered;
        }
    }

    public class DiskMidiFile : MidiFile
    {
        Stream MidiFileReader;
        DiskReadProvider FileReadProvider;
        public ushort Format { get; private set; }

        public IMidiTrack[] Tracks { get; private set; }

        public Tempo[] TempoEvents { get; private set; }
        public TimeSignature[] TimeSignatureEvents { get; private set; }

        internal List<TrackPos> TrackPositions { get; } = new List<TrackPos>();

        public DiskMidiFile(string filename) : this(filename, null) { }
        public DiskMidiFile(string filename, IProgress<MidiParseProgress> progress)
        {
            MidiFileReader = new StreamReader(filename).BaseStream;
            ParseHeaderChunk();
            while (MidiFileReader.Position < MidiFileReader.Length)
            {
                ParseTrackChunk();
                progress.Report(new MidiParseProgress(TrackCount));
            }
            Tracks = new IMidiTrack[TrackCount];

            Console.WriteLine("Loading tracks into memory, biggest tracks first.");
            Console.WriteLine("Please expect this to start slow, especially on bigger midis.");
            LoadAndParseAll(progress);
            Console.WriteLine("Loaded!");
            Console.WriteLine("Note count: " + NoteCount);
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
            Format = ReadInt16();
            ReadInt16();
            PPQ = ReadInt16();
            if (Format == 2) throw new Exception("Midi type 2 not supported");
        }

        void ParseTrackChunk()
        {
            AssertText("MTrk");
            uint length = ReadInt32();
            TrackPositions.Add(new TrackPos(MidiFileReader.Position, length));
            MidiFileReader.Position += length;
            TrackCount++;
            Console.WriteLine("Track " + TrackCount + ", Size " + length);
        }

        void LoadAndParseAll(IProgress<MidiParseProgress> progress)
        {
            int p = 0;
            FileReadProvider = new DiskReadProvider(MidiFileReader);
            int[] trackOrder = new int[Tracks.Length];
            for (int i = 0; i < trackOrder.Length; i++) trackOrder[i] = i;
            Array.Sort(TrackPositions.Select(t => t.length).ToArray(), trackOrder);
            trackOrder = trackOrder.Reverse().ToArray();
            object l = new object();
            ParallelFor(0, Tracks.Length, Environment.ProcessorCount * 3, (_i) =>
            {
                int i = trackOrder[_i];
                var reader = new BufferByteReader(FileReadProvider, 10000000, TrackPositions[i].start, TrackPositions[i].length);
                var t = DiskMidiTrack.NewParserTrack(i, reader);
                NoteCount += t.NoteCount;
                Tracks[i] = t;
                t.Dispose();
                lock (l)
                {
                    Console.WriteLine("Loaded track " + p++ + "/" + Tracks.Length);
                    progress.Report(new MidiParseProgress(p, TrackCount));
                }
            });
            TickLength = Tracks.Select(t => t.LastNoteTick).Max();
            Console.WriteLine("Processing Tempos");

            var temposMerge = ZipMerger<Tempo>.MergeMany(Tracks.Select(t => t.TempoEvents).ToArray(), e => e.pos).ToArray();
            if (temposMerge.Length == 0 || temposMerge.First().pos > 0)
            {
                temposMerge = new[] { new Tempo(0, 500000) }.Concat(temposMerge).ToArray();
            }
            TempoEvents = temposMerge;

            var timesigMerge = ZipMerger<TimeSignature>.MergeMany(Tracks.Select(t => t.TimesigEvents).ToArray(), e => e.Position).ToArray();
            if (timesigMerge.Length == 0 || timesigMerge[0].Position > 0)
            {
                timesigMerge = new[] { new TimeSignature(0, 4, 4) }.Concat(timesigMerge).ToArray();
            }
            TimeSignatureEvents = timesigMerge;

            double time = 0;
            long ticks = TickLength;
            double multiplier = ((double)500000 / PPQ) / 1000000;
            long lastt = 0;
            foreach (var t in TempoEvents)
            {
                var offset = t.pos - lastt;
                if (offset > ticks) break;
                time += offset * multiplier;
                ticks -= offset;
                lastt = t.pos;
                multiplier = ((double)t.rawTempo / PPQ) / 1000000;
            }
            time += ticks * multiplier;

            SecondsLength = time;

            GC.Collect();
        }

        void ParallelFor(int from, int to, int threads, Action<int> func)
        {
            Dictionary<int, Task> tasks = new Dictionary<int, Task>();
            BlockingCollection<int> completed = new BlockingCollection<int>();

            void RunTask(int i)
            {
                var t = new Task(() =>
                {
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

        public override void Dispose()
        {
            foreach (var t in Tracks) t.Dispose();
            MidiFileReader.Dispose();
        }

        public override MidiPlayback GetMidiPlayback(double startOffset, bool timeBased)
        {
            return new DiskMidiPlayback(this, FileReadProvider, startOffset, timeBased);
        }

        public override MidiPlayback GetMidiPlayback(double startOffsetTicks, double startOffsetSeconds, bool timeBased)
        {
            double startOffset = startOffsetSeconds + StartTicksToSeconds(startOffsetTicks, timeBased);
            return GetMidiPlayback(startOffset, timeBased);
        }

        public override double StartTicksToSeconds(double startOffset, bool timeBased)
        {
            double multiplier = ((double)TempoEvents[0].rawTempo / PPQ) / 1000000;
            if (timeBased)
            {
                multiplier = 1.0 / 1000;
            }
            return startOffset * multiplier;
        }
    }
}
