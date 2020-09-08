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
using ZenithEngine.DXHelper.D2D;
using OpenTK.Graphics;

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

        CompositeRenderSurface composite;
        Compositor compositor;
        ShaderProgram plainShader;

        InterlopRenderTarget2D target2d;
        SolidColorBrushKeeper brush;
        TextFormatKeeper textFormat;

        public Render()
        {
            compositor = init.Add(new Compositor());
            plainShader = init.Add(Shaders.BasicTextured());
        }

        public override void Init(DeviceGroup device, MidiPlayback midi, RenderStatus status)
        {
            init.Replace(ref composite, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight));
            init.Replace(ref target2d, new InterlopRenderTarget2D(composite));
            init.Replace(ref brush, new SolidColorBrushKeeper(target2d, new Color4(255, 255, 255, 255)));
            init.Replace(ref textFormat, new TextFormatKeeper("Gabriola", 96));

            base.Init(device, midi, status);
        }

        public override void RenderFrame(DeviceContext context, IRenderSurface renderSurface)
        {
            var rect = new RawRectangleF(0, 0, composite.Width, composite.Height);
            using (target2d.BeginDraw())
            {
                textFormat.TextAlignment = DirectWrite.TextAlignment.Center;
                textFormat.ParagraphAlignment = DirectWrite.ParagraphAlignment.Center;
                target2d.RenderTarget.DrawText("test stuff", textFormat, rect, brush);
                var layout = textFormat.GetLayout("test stuff");
                Console.WriteLine(layout.DetermineMinWidth());
            }

            compositor.Composite(context, composite, plainShader, renderSurface);
        }
    }
}
