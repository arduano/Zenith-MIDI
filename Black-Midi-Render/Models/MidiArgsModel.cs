using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using ZenithEngine.MIDI.Disk;

namespace Zenith.Models
{
    public enum MidiLoadStatus
    {
        Unloaded,
        Loading,
        Loaded,
        Cancelling,
    }

    public class MidiArgsModel : INotifyPropertyChanged
    {
        public LoadedMidiArgsModel Loaded { get; set; }
        public MidiLoadStatus LoadStatus { get; set; } = MidiLoadStatus.Unloaded;

        public async Task LoadMidi(string filename)
        {
            await Err.Handle(async () =>
            {
                if (LoadStatus != MidiLoadStatus.Unloaded) throw new UIException("Can't load a midi when another is already loaded");
                LoadStatus = MidiLoadStatus.Loading;
                await Task.Run(() =>
                {
                    Loaded = new LoadedMidiArgsModel(new DiskMidiFile(filename), filename);
                    LoadStatus = MidiLoadStatus.Loaded;
                });
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
