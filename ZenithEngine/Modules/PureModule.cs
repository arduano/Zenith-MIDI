using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ZenithEngine.DXHelper;
using ZenithEngine.MIDI;
using ZenithEngine.ModuleUI;

namespace ZenithEngine.Modules
{
    public abstract class PureModule : DeviceInitiable, IModuleRender
    {
        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract ImageSource PreviewImage { get; }

        public abstract string LanguageDictName { get; }

        public virtual FrameworkElement SettingsControl { get; }

        public abstract double StartOffset { get; }

        protected MidiPlayback Midi { get; private set; }
        protected RenderStatus Status { get; private set; }

        protected abstract NoteColorPalettePick PalettePicker { get; }

        public virtual void Init(Device device, MidiPlayback midi, RenderStatus status)
        {
            Init(device);
            Midi = midi;
            Status = status;

            init.Init(device);

            ReloadTrackColors();
        }

        public override void Dispose()
        {
            base.Dispose();
            Midi = null;
            Status = null;
        }

        public virtual void ReloadTrackColors()
        {
            if (PalettePicker == null || Midi == null) return;
            var cols = PalettePicker.GetColors(Midi.TrackCount);
            Midi.ApplyColors(cols);
        }

        public abstract void RenderFrame(DeviceContext context, IRenderSurface renderSurface);

        protected static T LoadUI<T>(Func<T> load)
        {
            T data = default;
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                data = load();
            }).Wait();
            return data;
        }

        protected static ImageSource LoadPreviewBitmap(Bitmap img)
        {
            return LoadUI(() => ModuleUtil.ModuleUtils.BitmapToImageSource(img));
        }
    }
}
