using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace ZenithEngine
{
    public interface IPluginRender : IDisposable
    {
        string Name { get; }
        string Description { get; }
        bool Initialized { get; }
        ImageSource PreviewImage { get; }
        bool ManualNoteDelete { get; }

        NoteColor[][] NoteColors { set; }
        double Tempo { set; }
        
        string LanguageDictName { get; }

        double StartOffset { get; }
        long LastNoteCount { get; }
        Control SettingsControl { get; }

        void Init(MidiFile midi);
        void RenderFrame(FastList<Note> notes, double midiTime, int finalCompositeBuff);
        void ReloadTrackColors();
    }
}
