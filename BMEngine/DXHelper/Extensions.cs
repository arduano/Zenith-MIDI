using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public static class Extensions
    {
        public static void ClearRenderTargetView(this DeviceContext ctx, RenderTargetView view)
        {
            ctx.ClearRenderTargetView(view, new Color4(0, 0, 0, 0));
        }

        public static void BindView(this IRenderSurface surface, DeviceContext context)
        {
            context.OutputMerger.SetTargets(surface.RenderTarget);
            context.Rasterizer.SetViewport(new Viewport(0, 0, surface.Width, surface.Height, 0.0f, 1.0f));
        }

        public static void BindViewAndClear(this IRenderSurface surface, DeviceContext context)
        {
            surface.BindView(context);
            context.ClearRenderTargetView(surface.RenderTarget, new Color4(0, 0, 0, 0));
        }

        public static IDisposable UseOnPS(this ITextureResource tex, DeviceContext ctx, int slot) =>
            new Applier<ShaderResourceView>(tex.TextureResource, () => ctx.PixelShader.GetShaderResources(slot, 1)[0], val => ctx.PixelShader.SetShaderResource(slot, val));
        public static IDisposable UseOnVS(this ITextureResource tex, DeviceContext ctx, int slot) =>
            new Applier<ShaderResourceView>(tex.TextureResource, () => ctx.VertexShader.GetShaderResources(slot, 1)[0], val => ctx.VertexShader.SetShaderResource(slot, val));
        public static IDisposable UseOnGS(this ITextureResource tex, DeviceContext ctx, int slot) =>
            new Applier<ShaderResourceView>(tex.TextureResource, () => ctx.GeometryShader.GetShaderResources(slot, 1)[0], val => ctx.GeometryShader.SetShaderResource(slot, val));

        public static IDisposable UseOnPS(this ITextureResource tex, DeviceContext ctx) =>
            tex.UseOnPS(ctx, 0);
        public static IDisposable UseOnVS(this ITextureResource tex, DeviceContext ctx) =>
            tex.UseOnVS(ctx, 0);
        public static IDisposable UseOnGS(this ITextureResource tex, DeviceContext ctx) =>
            tex.UseOnGS(ctx, 0);
    }
}
