using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public static class RenderSurfaceExtensions
    {
        struct ViewTargetCache
        {
            public ViewTargetCache(ViewportF viewport, RenderTargetView view, DepthStencilView depthView)
            {
                Viewport = viewport;
                View = view;
                DepthView = depthView;
            }

            public ViewportF Viewport { get; }
            public RenderTargetView View { get; }
            public DepthStencilView DepthView { get; }
        }

        public static IDisposable UseView(this IRenderSurface surface, DeviceContext context, ViewportF viewport)
        {
            return new Applier<ViewTargetCache>(
                new ViewTargetCache(viewport, surface.RenderTarget, surface.RenderTargetDepth),
                () =>
                {
                    DepthStencilView stencil;
                    var target = context.OutputMerger.GetRenderTargets(1, out stencil)[0];
                    ViewportF viewport = new ViewportF();

                    // need to use try catch because sharpdx is being fucked
                    try
                    {
                        var viewports = context.Rasterizer.GetViewports<ViewportF>();
                        if (viewports.Length > 0) viewport = viewports[0];
                    }
                    catch (IndexOutOfRangeException) { }

                    return new ViewTargetCache(viewport, target, stencil);
                },
                val =>
                {
                    if (val.DepthView == null)
                    {
                        context.OutputMerger.SetRenderTargets(val.View);
                    }
                    else
                    {
                        context.OutputMerger.SetRenderTargets(val.DepthView, val.View);
                    }

                    context.Rasterizer.SetViewport(val.Viewport);
                }
            );
        }

        public static IDisposable UseView(this IRenderSurface surface, DeviceContext context)
        {
            return surface.UseView(context, new ViewportF(0, 0, surface.Width, surface.Height));
        }

        public static IDisposable UseViewAndClear(this IRenderSurface surface, DeviceContext context)
        {
            var view = surface.UseView(context);
            context.ClearRenderTargetView(surface.RenderTarget);
            if (surface.RenderTargetDepth != null)
            {
                context.ClearDepthStencilView(surface.RenderTargetDepth, DepthStencilClearFlags.Depth, 0, 0);
            }
            return view;
        }
    }
}
