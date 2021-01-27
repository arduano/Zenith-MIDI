using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.MIDI
{
    public abstract class MidiPlayback : IDisposable
    {
        public bool TimeBased { get; }
        public bool PreviewMode { get; }

        public bool PushPlaybackEvents { get; set; } = false;

        protected bool stopped = false;

        bool notesKeysSeparated = true;
        public bool NotesKeysSeparated
        {
            get => notesKeysSeparated;
            set
            {
                if (notesKeysSeparated != value)
                {
                    notesKeysSeparated = value;
                    MoveNotesToType(notesKeysSeparated);
                }
            }
        }

        void MoveNotesToType(bool keysSeparated)
        {
            if (keysSeparated)
            {
                NotesKeyed = new FastList<Note>[256];
                for (int i = 0; i < NotesKeyed.Length; i++) NotesKeyed[i] = new FastList<Note>();
                if (NotesSingle != null)
                {
                    foreach (var n in NotesSingle)
                    {
                        NotesKeyed[n.Key].Add(n);
                    }
                    NotesSingle = null;
                }
            }
            else
            {
                NotesSingle = new FastList<Note>();
                if (NotesKeyed != null)
                {
                    var zip = ZipMerger<Note>.MergeMany(NotesKeyed, n => n.Start);
                    foreach (var n in zip) NotesSingle.Add(n);
                    NotesKeyed = null;
                }
            }
        }

        public int TrackCount => Midi.TrackCount;
        public FastList<Note> NotesSingle { get; protected set; }
        public FastList<Note>[] NotesKeyed { get; protected set; }
        public FastList<ColorChange> ColorChanges { get; protected set; }
        public FastList<PlaybackEvent> PlaybackEvents { get; protected set; }
        public Tempo Tempo { get; internal set; }
        public TimeSignature TimeSignature { get; internal set; }
        public Scale Scale { get; internal set; }
        public TextData Lyrics { get; internal set; }

        internal double ParserTempoTickMultiplier { get; set; } = 0;

        public abstract double ParserPosition { get; }
        public abstract double PlayerPosition { get; }
        public abstract double PlayerPositionSeconds { get; }

        public abstract MidiFile Midi { get; }

        public bool Ended { get; protected set; } = false;

        public abstract IMidiPlaybackTrack[] Tracks { get; }

        public abstract long LastIterateNoteCount { get; }

        public MidiPlayback(MidiFile midi, double initialTempo, bool timeBased)
        {
            TimeBased = timeBased;
            ParserTempoTickMultiplier = (initialTempo / midi.PPQ) / 1000000;
            MoveNotesToType(notesKeysSeparated);
        }

        public void CheckParseDistance(double parseDist)
        {
            ParseUpTo(PlayerPosition + parseDist);
        }

        public abstract bool ParseUpTo(double time);

        protected void FlushColorEvents()
        {
            while (!ColorChanges.ZeroLen && ColorChanges.First.Position <= PlayerPositionSeconds)
            {
                ColorChange change = ColorChanges.Pop();
                if (change.Channel == 0x7f)
                {
                    foreach (var c in change.Track.TrackColors) c.Set(change.Col1, change.Col2);
                }
                else
                {
                    change.Track.TrackColors[change.Channel].Set(change.Col1, change.Col2);
                }
            }
        }

        public int ColorCount => Midi.TrackCount * 32;
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
                if (track.Length != 32) throw new Exception("Color count doesnt match");
                for (int j = 0; j < 16; j++)
                {
                    Tracks[i].TrackColors[j].Alter(track[j * 2], track[j * 2 + 1]);
                }
            }
        }

        public void ClearNoteMeta()
        {
            void ClearStream(IEnumerable<Note> notes)
            {
                foreach (var n in notes) n.Meta = null;
            }

            if (NotesKeyed != null)
            {
                Parallel.ForEach(NotesKeyed, k => ClearStream(k));
            }
            if (NotesSingle != null)
            {
                ClearStream(NotesSingle);
            }
        }

        public void AdvancePlayback(double offset)
        {
            AdvancePlaybackTo(PlayerPositionSeconds + offset);
        }
        public abstract void AdvancePlaybackTo(double time);

        public abstract IEnumerable<Note> IterateNotes();
        public IEnumerable<Note> IterateNotes(double topCutoffOffset) => IterateNotes(PlayerPosition, topCutoffOffset);
        public abstract IEnumerable<Note> IterateNotes(double bottomCutoffOffset, double topCutoffOffset);
        public abstract IEnumerable<Note> IterateNotesCustomDelete();

        public abstract IEnumerable<Note>[] IterateNotesKeyed();
        public IEnumerable<Note>[] IterateNotesKeyed(double topCutoffOffset) => IterateNotesKeyed(PlayerPosition, topCutoffOffset);
        public abstract IEnumerable<Note>[] IterateNotesKeyed(double bottomCutoffOffset, double topCutoffOffset);
        public abstract IEnumerable<Note>[] IterateNotesCustomDeleteKeyed();

        protected IEnumerable<Note> IterateNotesListWithCutoffs(FastList<Note> notes, double bottomCutoffOffset, double topCutoffOffset)
        {
            ParseUpTo(topCutoffOffset);
            var iter = notes.Iterate();
            for (Note n = null; iter.MoveNext(out n);)
            {
                if (stopped) break;
                if (n.End < bottomCutoffOffset && n.HasEnded)
                {
                    iter.Remove();
                    continue;
                }
                if (n.Start > topCutoffOffset)
                    break;
                yield return n;
            }
        }

        protected IEnumerable<Note> IterateNotesListWithCustomDelete(FastList<Note> notes)
        {
            var iter = notes.Iterate();
            for (Note n = null; iter.MoveNext(out n);)
            {
                if (n.Delete)
                {
                    iter.Remove();
                    continue;
                }
                if (stopped) break;
                yield return n;
                if (n.Delete)
                {
                    iter.Remove();
                }
            }
        }

        protected IEnumerable<Note> IterateNotesList(FastList<Note> notes)
        {

            foreach (var n in notes)
            {
                if (stopped) break;
                yield return n;
            }
        }

        protected static IEnumerable<Note>[] GenerateNotesListArrays(Func<int, IEnumerable<Note>> listFromKey, Action<long> completedTrack)
        {
            object l = new object();
            int completed = 0;
            IEnumerable<Note> fromKey(int key)
            {
                long nc = 0;
                foreach (var n in listFromKey(key))
                {
                    yield return n;
                    nc++;
                }
                lock (l)
                {
                    completedTrack(nc);
                    completed++;
                }
            }

            IEnumerable<IEnumerable<Note>> allKeys()
            {
                for (int i = 0; i < 256; i++)
                {
                    yield return fromKey(i);
                }
            }

            return allKeys().ToArray();
        }

        public virtual void ForceStop()
        {
            stopped = true;
        }

        public abstract void Dispose();
    }
}
