using OpenTK.Graphics;
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

        public int TrackCount => Midi.TrackCount;
        public FastList<Note> Notes { get; protected set; }
        public FastList<ColorChange> ColorChanges { get; protected set; }
        public FastList<PlaybackEvent> PlaybackEvents { get; protected set; }
        public Tempo Tempo { get; internal set; }
        public TimeSignature TimeSignature { get; internal set; }

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
        }

        public void CheckParseDistance(double parseDist)
        {
            ParseUpTo(PlayerPosition + parseDist);
        }

        public abstract bool ParseUpTo(double time);

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

        public void AdvancePlayback(double offset)
        {
            AdvancePlaybackTo(PlayerPositionSeconds + offset);
        }
        public abstract void AdvancePlaybackTo(double time);

        public abstract IEnumerable<Note> IterateNotes();
        public abstract IEnumerable<Note> IterateNotes(double topCutoffOffset);
        public abstract IEnumerable<Note> IterateNotesCustomDelete();


        public abstract void ForceStop();
        public abstract void Dispose();
    }
}
