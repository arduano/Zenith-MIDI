using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtfUnknown;

namespace ZenithEngine.MIDI.Disk
{
    class DiskMidiPlayback : MidiPlayback
    {
        public override double ParserPosition => TimeBased ? SecondsParsed * 1000 : TicksParsed;
        public override double PlayerPosition => TimeBased ? TimeSeconds * 1000 : TimeTicksFractional;
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

        bool disposed = false;

        int remainingTracks = 0;

        object parseLock = new object();

        protected long lastNoteCount = 0;
        public override long LastIterateNoteCount => lastNoteCount;

        public DiskMidiPlayback(DiskMidiFile file, DiskReadProvider reader, double startDelay, bool timeBased, long? maxAllocation = null) : base(file, file.TempoEvents[0].rawTempo, timeBased)
        {
            midi = file;

            tracks = new DiskMidiTrack[file.TrackCount];

            var maxSize = 10000000;

            if (maxAllocation != null)
            {
                while(maxSize > 100000)
                {
                    var sum = file.TrackPositions
                        .Select(t => (long)t.length)
                        .Select(l => l <= maxSize ? l : maxSize * 2)
                        .Sum();
                    if (sum < maxAllocation) break;
                    maxSize -= 1000;
                }
            }

            for (int i = 0; i < file.TrackCount; i++)
            {
                var trackReader = new BufferByteReader(reader, maxSize, midi.TrackPositions[i].start, midi.TrackPositions[i].length);
                tracks[i] = DiskMidiTrack.NewPlayerTrack(i, trackReader, this, midi.Tracks[i].InitialTrackColors);
            }

            ColorChanges = new FastList<ColorChange>();
            PlaybackEvents = new FastList<PlaybackEvent>();
            Tempo = midi.TempoEvents[0];
            TimeSignature = midi.TimeSignatureEvents[0];
            TimeSeconds = -startDelay;
            TimeTicksFractional = -startDelay / ParserTempoTickMultiplier;
            Scale = midi.ScaleEvents[0];
        }

        public override bool ParseUpTo(double time)
        {
            lock (parseLock)
            {
                for (; ParserPosition <= time + 1 && !stopped; TicksParsed++)
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
        }

        int tempoEventId = 0;
        int timesigEventId = 0;
        int scaleEventId = 0;
        
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

            while (scaleEventId != midi.ScaleEvents.Length &&
                midi.ScaleEvents[scaleEventId].Position < TimeTicksFractional)
            {
                Scale = midi.ScaleEvents[scaleEventId++];
            }

            Encoding encoding = midi.LyricsEvents.Length == 0 ? Encoding.Unicode : CharsetDetector.DetectFromBytes(midi.LyricsEvents[1].RawText).Detected?.Encoding;
            for (int lyricsEventId = 0; lyricsEventId != midi.LyricsEvents.Length &&
                midi.LyricsEvents[lyricsEventId].Position < TimeTicksFractional; lyricsEventId++)
            {
                Lyrics = midi.LyricsEvents[lyricsEventId];
                Lyrics.Text = encoding.GetString(midi.LyricsEvents[lyricsEventId].RawText);
            }

            if (SecondsParsed < TimeSeconds) ParseUpTo(PlayerPosition);

            FlushColorEvents();
        }

        IEnumerable<Note> SingleNoteListFromSource(Func<IEnumerable<Note>> getNotes)
        {
            NotesKeysSeparated = false;
            var notes = getNotes();
            long nc = 0;
            foreach (var n in notes)
            {
                nc++;
                yield return n;
            }
            lastNoteCount = nc;
            CheckEnded();
        }

        public override IEnumerable<Note> IterateNotes() =>
            SingleNoteListFromSource(() => IterateNotesList(NotesSingle));

        public override IEnumerable<Note> IterateNotes(double bottomCutoffOffset, double topCutoffOffset) =>
            SingleNoteListFromSource(() => IterateNotesListWithCutoffs(NotesSingle, bottomCutoffOffset, topCutoffOffset));

        public override IEnumerable<Note> IterateNotesCustomDelete() =>
            SingleNoteListFromSource(() => IterateNotesListWithCustomDelete(NotesSingle));

        IEnumerable<Note>[] KeyedNoteListFromSource(Func<int, IEnumerable<Note>> getNotes)
        {
            NotesKeysSeparated = true;
            long nc = 0;
            return GenerateNotesListArrays(
                getNotes,
                n =>
                {
                    nc += n;
                    lastNoteCount = nc;
                    CheckEnded();
                });
        }

        public override IEnumerable<Note>[] IterateNotesKeyed() =>
            KeyedNoteListFromSource(key => IterateNotesList(NotesKeyed[key]));

        public override IEnumerable<Note>[] IterateNotesKeyed(double bottomCutoffOffset, double topCutoffOffset) =>
            KeyedNoteListFromSource(key => IterateNotesListWithCutoffs(NotesKeyed[key], bottomCutoffOffset, topCutoffOffset));

        public override IEnumerable<Note>[] IterateNotesCustomDeleteKeyed() =>
            KeyedNoteListFromSource(key => IterateNotesListWithCustomDelete(NotesKeyed[key]));

        void CheckEnded()
        {
            if (remainingTracks == 0 && lastNoteCount == 0) Ended = true;
        }

        public override void Dispose()
        {
            if (disposed) return;
            ForceStop();
            disposed = true;

            NotesKeyed = null;
            NotesSingle = null;
            ColorChanges.Unlink();
            PlaybackEvents.Unlink();
            ColorChanges = null;
            PlaybackEvents = null;

            foreach (var t in tracks) t.Dispose();
        }
    }
}
