using OpenTK.Graphics;
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
        public ushort Division { get; private set; }
        public int TrackCount { get; private set; }
        public ushort Format { get; private set; }

        public MidiTrack[] Tracks { get; private set; }

        public Tempo[] TempoEvents { get; private set; }
        public TimeSignature[] TimeSignatureEvents { get; private set; }

        public long NoteCount { get; private set; } = 0;

        RenderSettings settings;

        List<TrackPos> trackPositions = new List<TrackPos>();

        internal double ParserTempoTickMultiplier { get; set; } = 0;

        public int RemainingTracks { get; private set; } = 0;


        public FastList<Note> Notes { get; private set; }
        public FastList<ColorChange> ColorChanges { get; private set; }
        public FastList<PlaybackEvent> PlaybackEvents { get; private set; }
        public Tempo Tempo { get; internal set; }
        public TimeSignature TimeSignature { get; internal set; }

        public double TimeSeconds { get; internal set; }
        public double TimeTicksFractional { get; internal set; }
        public long TimeTicks => (long)TimeTicksFractional;

        public long TicksParsed { get; internal set; }
        public double SecondsParsed { get; internal set; }

        public long TickLength { get; private set; }
        public double SecondsLength { get; private set; }

        public double ParserPosition => settings.TimeBased ? SecondsParsed : TicksParsed;
        public double PlayerPosition => settings.TimeBased ? TimeSeconds : TimeTicks;

        public MidiFile(string filename, RenderSettings settings)
        {
            this.settings = settings;
            MidiFileReader = new StreamReader(filename).BaseStream;
            ParseHeaderChunk();
            while (MidiFileReader.Position < MidiFileReader.Length)
            {
                ParseTrackChunk();
            }
            Tracks = new MidiTrack[TrackCount];

            Console.WriteLine("Loading tracks into memory, biggest tracks first.");
            Console.WriteLine("Please expect this to start slow, especially on bigger midis.");
            LoadAndParseAll(true);
            Console.WriteLine("Loaded!");
            Console.WriteLine("Note count: " + NoteCount);
            RemainingTracks = TrackCount;

            ParserTempoTickMultiplier = (double)Division / 500000 * 1000;
        }

        #region Loading
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
            Division = ReadInt16();
            if (Format == 2) throw new Exception("Midi type 2 not supported");
            if (Division < 0) throw new Exception("Division < 0 not supported");
        }

        void ParseTrackChunk()
        {
            AssertText("MTrk");
            uint length = ReadInt32();
            trackPositions.Add(new TrackPos(MidiFileReader.Position, length));
            MidiFileReader.Position += length;
            TrackCount++;
            Console.WriteLine("Track " + TrackCount + ", Size " + length);
        }

        public void LoadAndParseAll(bool useBufferStream = false)
        {
            int p = 0;
            var diskReader = new DiskReadProvider(MidiFileReader);
            int[] trackOrder = new int[Tracks.Length];
            for (int i = 0; i < trackOrder.Length; i++) trackOrder[i] = i;
            Array.Sort(trackPositions.Select(t => t.length).ToArray(), trackOrder);
            trackOrder = trackOrder.Reverse().ToArray();
            object l = new object();
            ParallelFor(0, Tracks.Length, Environment.ProcessorCount * 3, (_i) =>
               {
                   int i = trackOrder[_i];
                   var reader = new BufferByteReader(diskReader, 10000000, trackPositions[i].start, trackPositions[i].length);
                   var t = new MidiTrack(i, reader, this, settings);
                   t.InitialParse();
                   NoteCount += t.NoteCount;
                   Tracks[i] = t;
                   lock (l)
                   {
                       t.ResetAndResize(20000);
                       Console.WriteLine("Loaded track " + p++ + "/" + Tracks.Length);
                   }
               });
            TickLength = Tracks.Select(t => t.TickTime).Max();
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
            double multiplier = ((double)500000 / Division) / 1000000;
            long lastt = 0;
            foreach (var t in TempoEvents)
            {
                var offset = t.pos - lastt;
                time += offset * multiplier;
                ticks -= offset;
                lastt = t.pos;
                multiplier = ((double)t.rawTempo / Division) / 1000000;
            }
            time += ticks * multiplier;

            SecondsLength = time;

            RemainingTracks = TrackCount;

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

        void SetZeroColors()
        {
            foreach (var t in Tracks) t.SetZeroColors();
        }
        #endregion

        #region Playback

        #region External
        public void StartPlaybackParse(double startDelay)
        {
            EndPlaybackParse();
            Notes = new FastList<Note>();
            ColorChanges = new FastList<ColorChange>();
            PlaybackEvents = new FastList<PlaybackEvent>();
            Tempo = TempoEvents[0];
            TimeSignature = TimeSignatureEvents[0];
            TimeSeconds = -startDelay;
            SetZeroColors();
        }

        public bool ParseUpTo(double time)
        {
            for (; ParserPosition <= time && settings.Running; TicksParsed++)
            {
                TimeSeconds += 1 / ParserTempoTickMultiplier;
                int ut = 0;
                for (int trk = 0; trk < TrackCount; trk++)
                {
                    var t = Tracks[trk];
                    if (!t.Ended)
                    {
                        ut++;
                        t.Step(TicksParsed);
                    }
                }
                RemainingTracks = ut;
            }
            foreach (var t in Tracks)
            {
                if (!t.Ended) return true;
            }
            return false;
        }

        int tempoEventId = 0;
        int timesigEventId = 0;
        public void AdvancePlayback(double time)
        {
            if (SecondsParsed < time) ParseUpTo(time + 1);

            var offset = time - TimeSeconds;
            if (offset < 0) return;

            var multiplier = ((double)Tempo.rawTempo / Division) / 1000000;
            if (settings.TimeBased || true)
            {
                while (
                    tempoEventId < TempoEvents.Length &&
                    TimeTicksFractional + offset / multiplier > TempoEvents[tempoEventId].pos
                )
                {
                    Tempo = TempoEvents[tempoEventId];
                    tempoEventId++;
                    var diff = TimeTicksFractional - Tempo.pos;
                    TimeTicksFractional += diff;
                    offset -= diff * multiplier;
                    TimeSeconds += diff;
                    multiplier = ((double)Tempo.rawTempo / Division) / 1000000;
                }
                TimeTicksFractional += offset / multiplier;
                TimeSeconds += offset;
            }
            else
            {
                while (
                    tempoEventId < TempoEvents.Length &&
                    TimeTicksFractional + offset > TempoEvents[tempoEventId].pos
                )
                {
                    Tempo = TempoEvents[tempoEventId];
                    tempoEventId++;
                    var diff = TimeTicksFractional - Tempo.pos;
                    TimeTicksFractional += diff;
                    offset -= diff;
                    TimeSeconds += diff / multiplier;
                    multiplier = ((double)Tempo.rawTempo / Division) / 1000000;
                }
                TimeTicksFractional += offset;
                TimeSeconds += offset / multiplier;
            }

            while (timesigEventId != TimeSignatureEvents.Length &&
                TimeSignatureEvents[timesigEventId].Position < TimeTicksFractional)
            {
                TimeSignature = TimeSignatureEvents[timesigEventId++];
            }

            return;
        }

        public void EndPlaybackParse()
        {
            Notes = null;
            ColorChanges = null;
            PlaybackEvents = null;
            Reset();
        }
        #endregion

        #region Plugin Access

        public double RenderMidiTime => settings.TimeBased ? TimeSeconds * 1000 : TimeTicks;
        public void CheckParseDistance(double parseDist)
        {
            ParseUpTo(PlayerPosition + parseDist);
        }

        public int ColorCount => TrackCount * 32;
        public void ApplyColors(Color4[] colors)
        {
            if (colors.Length != ColorCount) throw new Exception("Color count doesnt match");

            for (int i = 0; i < Tracks.Length; i++)
            {
                for (int j = 0; j < Tracks[i].TrackColors.Length; j++)
                {
                    Tracks[i].TrackColors[j].Alter(colors[i * 32 + j * 2], colors[i * 32 + j * 2 + 1]);
                }
            }
        }
        public void ApplyColors(Color4[][] colors)
        {
            if (colors.Length != TrackCount) throw new Exception("Color count doesnt match");

            for (int i = 0; i < Tracks.Length; i++)
            {
                var track = colors[i];
                if(track.Length != 32) throw new Exception("Color count doesnt match");
                for (int j = 0; j < 16; j++)
                {
                    Tracks[i].TrackColors[j].Alter(track[j * 2], track[j * 2 + 1]);
                }
            }
        }

        #endregion

        #endregion

        public void Reset()
        {
            tempoEventId = 0;
            timesigEventId = 0;
            RemainingTracks = TrackCount;
            ParserTempoTickMultiplier = (double)Division / 500000 * 1000;
            TimeSeconds = 0;
            TimeTicksFractional = 0;
            SecondsParsed = 0;
            TicksParsed = 0;
            TimeTicksFractional = 0;
            foreach (var t in Tracks) t.Reset();
        }

        public void Dispose()
        {
            foreach (var t in Tracks) t.Dispose();
            MidiFileReader.Dispose();
        }
    }
}
