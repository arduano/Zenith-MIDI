using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.MIDI
{
    public interface IMidiTrack : IDisposable
    {
        long NoteCount { get; }
        long TickLength { get; }
        long LastNoteTick { get; }
        int ID { get; }
        IEnumerable<Tempo> TempoEvents { get; }
        IEnumerable<TimeSignature> TimesigEvents { get; }
        NoteColor[] InitialTrackColors { get; }
    }
}
