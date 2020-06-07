using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.MIDI.Disk
{
    class DiskMidiPlayback : MidiPlayback
    {
        public override double ParserPosition => TimeBased ? SecondsParsed : TicksParsed;
        public override double PlayerPosition => TimeBased ? TimeSeconds : TimeTicksFractional;
        public override double PlayerPositionSeconds => TimeSeconds;

        public double TimeSeconds { get; internal set; }
        public double TimeTicksFractional { get; internal set; }
        public long TimeTicks => (long)TimeTicksFractional;

        public long TicksParsed { get; internal set; }
        public double SecondsParsed { get; internal set; }


        DiskMidiFile midi;
        public override MidiFile Midi => midi;

        DiskMidiTrack[] tracks;
        public override IMidiPlaybackTrack[] Tracks => tracks;

        bool stopped = false;
        bool disposed = false;

        int remainingTracks = 0;

        long lastNoteCount = 0;
        public override long LastIterateNoteCount => lastNoteCount;

        public DiskMidiPlayback(DiskMidiFile file, DiskReadProvider reader, double startDelay) : base(file)
        {
            midi = file;

            tracks = new DiskMidiTrack[file.TrackCount];
            for (int i = 0; i < file.TrackCount; i++)
            {
                var trackReader = new BufferByteReader(reader, 10000000, midi.TrackPositions[i].start, midi.TrackPositions[i].length);
                tracks[i] = DiskMidiTrack.NewPlayerTrack(i, trackReader, this);
            }

            Notes = new FastList<Note>();
            ColorChanges = new FastList<ColorChange>();
            PlaybackEvents = new FastList<PlaybackEvent>();
            Tempo = midi.TempoEvents[0];
            TimeSignature = midi.TimeSignatureEvents[0];
            TimeSeconds = -startDelay;
        }

        public override bool ParseUpTo(double time)
        {
            for (; ParserPosition <= time && !stopped; TicksParsed++)
            {
                SecondsParsed += 1 * ParserTempoTickMultiplier;
                int ut = 0;
                for (int trk = 0; trk < Midi.TrackCount; trk++)
                {
                    var t = Tracks[trk];
                    if (!t.Ended)
                    {
                        ut++;
                        t.Step(TicksParsed);
                    }
                }
                remainingTracks = ut;
            }
            foreach (var t in Tracks)
            {
                if (!t.Ended) return true;
            }
            return false;
        }

        int tempoEventId = 0;
        int timesigEventId = 0;
        public override void AdvancePlaybackTo(double time)
        {
            var offset = time - TimeSeconds;
            if (offset < 0) return;

            var multiplier = ((double)Tempo.rawTempo / Midi.PPQ) / 1000000;
            if (true)
            {
                while (
                    tempoEventId < midi.TempoEvents.Length &&
                    TimeTicksFractional + offset / multiplier > midi.TempoEvents[tempoEventId].pos
                )
                {
                    Tempo = midi.TempoEvents[tempoEventId];
                    tempoEventId++;
                    var diff = Tempo.pos - TimeTicksFractional;
                    if (diff * multiplier > offset || diff < 0)
                    { }
                    TimeTicksFractional += diff;
                    offset -= diff * multiplier;
                    TimeSeconds += diff * multiplier;
                    multiplier = ((double)Tempo.rawTempo / Midi.PPQ) / 1000000;
                }
                TimeTicksFractional += offset / multiplier;
                TimeSeconds += offset;
            }
            else
            {
                // this part isn't enabled because it steps by ticks instead of seconds.
                // I made this function use seconds instead of ticks, but don't want to delete
                // this code.

                //while (
                //    tempoEventId < midi.TempoEvents.Length &&
                //    TimeTicksFractional + offset > midi.TempoEvents[tempoEventId].pos
                //)
                //{
                //    Tempo = midi.TempoEvents[tempoEventId];
                //    tempoEventId++;
                //    var diff = TimeTicksFractional - Tempo.pos;
                //    TimeTicksFractional += diff;
                //    offset -= diff;
                //    TimeSeconds += diff / multiplier;
                //    multiplier = ((double)Tempo.rawTempo / Midi.PPQ) / 1000000;
                //}
                //TimeTicksFractional += offset;
                //TimeSeconds += offset / multiplier;
            }

            while (timesigEventId != midi.TimeSignatureEvents.Length &&
                midi.TimeSignatureEvents[timesigEventId].Position < TimeTicksFractional)
            {
                TimeSignature = midi.TimeSignatureEvents[timesigEventId++];
            }

            if (SecondsParsed < TimeSeconds) ParseUpTo(PlayerPosition);

            return;
        }

        public override IEnumerable<Note> IterateNotes()
        {
            long nc = 0;
            foreach (var n in Notes)
            {
                if (stopped) break;
                nc++;
                yield return n;
            }
            if (remainingTracks == 0 && nc == 0) Ended = true;
            lastNoteCount = nc;
        }

        public override IEnumerable<Note> IterateNotes(double topCutoffOffset)
        {
            long nc = 0;
            var iter = Notes.Iterate();
            var cutoff = topCutoffOffset + PlayerPosition;
            for (Note n = null; iter.MoveNext(out n);)
            {
                if (stopped) break;
                if (n.end < PlayerPosition && n.hasEnded)
                {
                    iter.Remove();
                    continue;
                }
                if (n.start > cutoff) 
                    break;
                nc++;
                yield return n;
            }
            if (remainingTracks == 0 && nc == 0) Ended = true;
            lastNoteCount = nc;
        }

        public override IEnumerable<Note> IterateNotesCustomDelete()
        {
            long nc = 0;
            var iter = Notes.Iterate();
            for (Note n = null; iter.MoveNext(out n);)
            {
                if (stopped) break;
                if (n.delete)
                {
                    iter.Remove();
                    continue;
                }
                nc++;
                yield return n;
            }
            if (remainingTracks == 0 && nc == 0) Ended = true;
            lastNoteCount = nc;
        }

        public override void ForceStop()
        {
            stopped = true;
        }

        public override void Dispose()
        {
            if (disposed) return;
            ForceStop();
            disposed = true;

            Notes.Unlink();
            ColorChanges.Unlink();
            PlaybackEvents.Unlink();
            Notes = null;
            ColorChanges = null;
            PlaybackEvents = null;

            foreach (var t in tracks) t.Dispose();
        }
    }
}
