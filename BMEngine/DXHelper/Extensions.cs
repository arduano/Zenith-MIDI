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

        public static Matrix3x3 To3x3(this Matrix mat) =>
            new Matrix3x3(mat.M11, mat.M12, mat.M13, mat.M21, mat.M22, mat.M23, mat.M31, mat.M32, mat.M33);
    }
}
