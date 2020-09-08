using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace ZenithEngine.DXHelper.D2D
{
    public class SolidColorBrushKeeper : DeviceInitiable
    {
        public static implicit operator SolidColorBrush(SolidColorBrushKeeper keeper) => keeper.Brush;

        public SolidColorBrush Brush { get; private set; }
        InterlopRenderTarget2D renderTarget;

        public SolidColorBrushKeeper(InterlopRenderTarget2D renderTarget, Color4 color)
        {
            this.renderTarget = renderTarget;
            Color = color;
        }

        public Color4 Color { get; }

        protected override void InitInternal()
        {
            Brush = new SolidColorBrush(renderTarget, new RawColor4(Color.R, Color.G, Color.B, Color.A));
        }
    }
}
