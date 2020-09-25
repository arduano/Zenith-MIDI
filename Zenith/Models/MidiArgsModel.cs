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

        public CancellableTask MidiLoadTask { get; private set; }

        public MidiParseProgress LoaderStatus { get; private set; }

        public async Task LoadMidi(string filename)
        {
            await Err.Handle(async () =>
            {
                if (LoadStatus != MidiLoadStatus.Unloaded) throw new UIException("Can't load a midi when another is already loaded");
                LoadStatus = MidiLoadStatus.Loading;
                MidiLoadTask = CancellableTask.Run(cancel =>
                {
                    var reporter = new Progress<MidiParseProgress>(progress =>
                    {
                        LoaderStatus = progress;
                    });
                    try
                    {
                        var file = new DiskMidiFile(filename, reporter, cancel, 8L * 1024 * 1024 * 1024);
                        Loaded = new LoadedMidiArgsModel(file, filename);
                        LoadStatus = MidiLoadStatus.Loaded;
                    }
                    catch (Exception e)
                    {
                        LoadStatus = MidiLoadStatus.Unloaded;
                        throw e;
                    }
                }); 
                try
                {
                    await MidiLoadTask.Await();
                }
                finally
                {
                    MidiLoadTask = null;
                }
            });
        }

        public async Task CancelMidiLoading()
        {
            await Err.Handle(async () =>
            {
                if (LoadStatus != MidiLoadStatus.Loading || MidiLoadTask == null) throw new UIException("Can't cancel loading when nothing is loading");
                LoadStatus = MidiLoadStatus.Cancelling;
                MidiLoadTask?.Cancel();
                try
                {
                    await MidiLoadTask.Await();
                }
                finally
                {
                    MidiLoadTask = null;
                    Loaded = null;
                }
            });
        }

        public void UnloadMidi()
        {
            Err.Handle(() =>
            {
                if (LoadStatus != MidiLoadStatus.Loaded) throw new UIException("Can't unload midi when no midi is loaded");
                Loaded.Dispose();
                Loaded = null;
                LoadStatus = MidiLoadStatus.Unloaded;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
