using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using ZenithEngine.MIDI;
using ZenithEngine.MIDI.Disk;
using System.IO;

namespace Zenith.Models
{
    public class LoadedMidiArgsModel : IDisposable
    {
        public MidiFile MidiFile { get; }

        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long NoteCount => MidiFile.NoteCount;

        public LoadedMidiArgsModel(MidiFile midi, string filepath)
        {
            MidiFile = midi;
            FilePath = filepath;
            FileName = Path.GetFileName(filepath);
        }

        public void Dispose()
        {
            MidiFile.Dispose();
        }
    }
}
