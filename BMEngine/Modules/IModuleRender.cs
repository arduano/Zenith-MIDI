using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using ZenithEngine.GLEngine;
using ZenithEngine.MIDI;

namespace ZenithEngine.Modules
{
    public interface IModuleRender : IDisposable
    {
        string Name { get; }
        string Description { get; }
        bool Initialized { get; }
        ImageSource PreviewImage { get; }

        string LanguageDictName { get; }

        Control SettingsControl { get; }
        public double StartOffset { get; }

        void Init(MidiPlayback midi);
        void RenderFrame(RenderSurface finalCompositeBuff);
        void ReloadTrackColors();
    }
}
