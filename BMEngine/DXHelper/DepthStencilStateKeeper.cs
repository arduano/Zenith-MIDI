using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;

namespace ZenithEngine.DXHelper
{
    //public class DepthStencilStateKeeper
    //{
    //    public static implicit operator DepthStencilState(DepthStencilStateKeeper keeper) => keeper.DepthStencilState;

    //    public DepthStencilState DepthStencilState { get; private set; }
    //    public DepthStencilStateDescription Description;

    //    public static DepthStencilStateDescription BasicStateDescription()
    //    {
    //        var desc = new DepthStencilStateDescription()
    //        {
    //            BackFace
    //        };

    //        return blendDesc;
    //    }

    //    public BlendStateKeeper() : this(BasicBlendDescription()) { }
    //    public BlendStateKeeper(BlendStateDescription desc)
    //    {
    //        Description = desc;
    //    }

    //    protected override void InitInternal()
    //    {
    //        BlendState = new BlendState(Device, Description);
    //    }

    //    protected override void DisposeInternal()
    //    {
    //        BlendState.Dispose();
    //    }

    //    public IDisposable UseOn(DeviceContext ctx) =>
    //        new Applier<BlendState>(this, () => ctx.OutputMerger.BlendState, val => ctx.OutputMerger.BlendState = val);
    //}
}
