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

        public MidiParseProgress LoaderStatus { get; private set; }

        public async Task LoadMidi(string filename)
        {
            await Err.Handle(async () =>
            {
                if (LoadStatus != MidiLoadStatus.Unloaded) throw new UIException("Can't load a midi when another is already loaded");
                LoadStatus = MidiLoadStatus.Loading;
                await Task.Run(() =>
                {
                    var reporter = new Progress<MidiParseProgress>(progress =>
                    {
                        LoaderStatus = progress;
                    });
                    var file = new DiskMidiFile(filename, reporter);
                    Loaded = new LoadedMidiArgsModel(file, filename);
                    LoadStatus = MidiLoadStatus.Loaded;
                });
            });
        }

        public void UnloadMidi()
        {
            Err.Handle(() =>
            {
                if (LoadStatus != MidiLoadStatus.Loaded) throw new UIException("Can't unload when no midi is loaded");
                Loaded.Dispose();
                Loaded = null;
                LoadStatus = MidiLoadStatus.Unloaded;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
