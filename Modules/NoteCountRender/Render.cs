using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ZenithEngine;
using ZenithEngine.DXHelper;
using ZenithEngine.MIDI;
using ZenithEngine.Modules;
using Direct2D1 = SharpDX.Direct2D1;
using DXGI = SharpDX.DXGI;
using DirectWrite = SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using ZenithEngine.DXHelper.Presets;

namespace NoteCountRender
{
    public class Render : PureModule
    {
        #region Info
        public override string Name { get; } = "Note Count";
        public override string Description { get; } = "blah blah blah";
        public override ImageSource PreviewImage { get; } = LoadPreviewBitmap(Properties.Resources.preview);
        public override string LanguageDictName { get; } = "notecounter";
        #endregion

        public override FrameworkElement SettingsControl => null;

        public override double StartOffset => 0;

        protected override NoteColorPalettePick PalettePicker => null;

        Direct2D1.Factory d2dFactory;
        DirectWrite.Factory dwFactory;

        CompositeRenderSurface preFinalSurface;
        Compositor compositor;
        ShaderProgram plainShader;

        Direct2D1.RenderTarget textRt = null;
        DirectWrite.TextFormat textFormat;
        Direct2D1.SolidColorBrush brush;
        RawRectangleF rect;

        public Render()
        {
            compositor = init.Add(new Compositor());
            plainShader = init.Add(Shaders.BasicTextured());
        }

        public override void Init(Device device, MidiPlayback midi, RenderStatus status)
        {
            init.Replace(ref preFinalSurface, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight));
            base.Init(device, midi, status);

            d2dFactory = new Direct2D1.Factory();
            dwFactory = new DirectWrite.Factory();

            var dxgiSurface = preFinalSurface.Texture.QueryInterface<DXGI.Surface>();
            var renderTargetProperties = new Direct2D1.RenderTargetProperties(new Direct2D1.PixelFormat(DXGI.Format.R32G32B32A32_Float, Direct2D1.AlphaMode.Premultiplied));
            textRt = new Direct2D1.RenderTarget(d2dFactory, dxgiSurface, renderTargetProperties);

            textFormat = new DirectWrite.TextFormat(dwFactory, "Gabriola", 96) { TextAlignment = DirectWrite.TextAlignment.Center, ParagraphAlignment = DirectWrite.ParagraphAlignment.Center };
            brush = new Direct2D1.SolidColorBrush(textRt, new RawColor4(1.0f, 1.0f, 1.0f, 1.0f));
            rect = new RawRectangleF(0, 0, preFinalSurface.Width, preFinalSurface.Height);
        }

        public override void RenderFrame(DeviceContext context, IRenderSurface renderSurface)
        {
            textRt.BeginDraw();
            rect = new RawRectangleF(0, 0, preFinalSurface.Width, preFinalSurface.Height);
            textRt.DrawText("test stuff", textFormat, rect, brush);
            DirectWrite.TextLayout layout = new DirectWrite.TextLayout(dwFactory, "test stuff", textFormat, float.MaxValue, float.MaxValue);
            Console.WriteLine(layout.DetermineMinWidth());
            textRt.EndDraw();

            compositor.Composite(context, preFinalSurface, plainShader, renderSurface);
        }
    }
}
