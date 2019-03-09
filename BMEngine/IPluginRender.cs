using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace BMEngine
{
    public interface IPluginRender : IDisposable
    {
        string Name { get; }
        string Description { get; }
        bool Initialized { get; }
        ImageSource PreviewImage { get; }
        bool ManualNoteDelete { get; }
        int NoteCollectorOffset { get; }

        double LastMidiTimePerTick { get; set; }

        double NoteScreenTime { get; }
        long LastNoteCount { get; }
        Control SettingsControl { get; }

        void Init();
        void RenderFrame(FastList<Note> notes, double midiTime, int finalCompositeBuff);
        void SetTrackColors(Color4[][] trakcs);
    }
}
