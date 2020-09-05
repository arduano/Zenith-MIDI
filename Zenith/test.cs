using System;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using ZenithEngine.DXHelper;
using ZenithEngine.DXHelper.Presets;
using System.Runtime.InteropServices;
using System.Drawing;
using Color = SharpDX.Color;

namespace Zenith
{
    class test
    {
        [StructLayoutAttribute(LayoutKind.Sequential)]
        struct Vert
        {
            [AssemblyElement("POSITION", Format.R32G32_Float)]
            public Vector2 Pos;

            [AssemblyElement("COLOR", Format.R32G32B32A32_Float)]
            public Vector4 Col;

            public Vert(Vector2 pos, Vector4 col)
            {
                Pos = pos;
                Col = col;
            }
        }

        public test()
        {
            var init = new Initiator();

            var form = new ManagedRenderWindow(1280, 720);
            form.Text = "test";

            // form.Fullscreen = true;

            // Create Device and SwapChain
            Device device = form.Device;
            var context = device.ImmediateContext;

            Shaders.BasicFlat();

            var shaderProgram = init.Add(Shaders.BasicFlat());
            var shaderProgram2 = init.Add(Shaders.BasicTextured());
            var sampler = init.Add(new TextureSampler());
            var texture = init.Add(RenderTexture.FromBitmap((Bitmap)Bitmap.FromFile("icon.png")));
            var blendState = init.Add(new BlendStateKeeper());
            var rasterizerState = init.Add(new RasterizerStateKeeper());
            var surface = init.Add(new CompositeRenderSurface(1920, 1080));
            var flatBuff = init.Add(new Flat2dShapeBuffer(16));
            var compositor = init.Add(new Compositor());

            var vertsTriangle = new[]
            {
                new Vert2D(new Vector2(0.0f, 0.5f), new Color4(1.0f, 0.0f, 0.0f, 1.0f)),
                new Vert2D(new Vector2(0.5f, -0.5f), new Color4(0.0f, 1.0f, 0.0f, 1.0f)),
                new Vert2D(new Vector2(-0.5f, -0.5f), new Color4(0.0f, 0.0f, 1.0f, 1.0f))
            };

            var vertsQuad = new[]
            {
                new Vert2D(new Vector2(0.5f, 0.5f), new Color4(1.0f, 0.0f, 0.0f, 1.0f)),
                new Vert2D(new Vector2(0.5f, -0.5f), new Color4(0.0f, 1.0f, 0.0f, 1.0f)),
                new Vert2D(new Vector2(-0.5f, -0.5f), new Color4(0.0f, 0.0f, 1.0f, 1.0f)),
                new Vert2D(new Vector2(-0.5f, 0.5f), new Color4(1.0f, 1.0f, 0.0f, 1.0f)),
            };

            var vertsQuadTex = new[]
            {
                new VertTex2D(new Vector2(0.5f, 0.5f), new Vector2(1, 1), new Color4(1.0f, 0.0f, 0.0f, 1.0f)),
                new VertTex2D(new Vector2(0.5f, -0.5f), new Vector2(1, 0), new Color4(0.0f, 1.0f, 0.0f, 1.0f)),
                new VertTex2D(new Vector2(-0.5f, -0.5f), new Vector2(0, 0), new Color4(0.0f, 0.0f, 1.0f, 1.0f)),
                new VertTex2D(new Vector2(-0.5f, 0.5f), new Vector2(0, 1), new Color4(1.0f, 1.0f, 0.0f, 1.0f)),
            };

            var vertsQuadTex2 = new[]
            {
                new VertTex2D(new Vector2(0.5f, 0.5f), new Vector2(1, 1), new Color4(1.0f)),
                new VertTex2D(new Vector2(0.5f, 0), new Vector2(1, 0), new Color4(1.0f)),
                new VertTex2D(new Vector2(0, 0), new Vector2(0, 0), new Color4(1.0f)),
                new VertTex2D(new Vector2(0, 0.5f), new Vector2(0, 1), new Color4(1.0f)),
            };

            var indicesQuad = new[] { 0, 1, 2, 0, 2, 3 };

            var shapeBuffer = init.Add(new ShapeBuffer<VertTex2D>(100, PrimitiveTopology.TriangleList, ShapePresets.Quads));

            // Instantiate Vertex buiffer from vertex data
            var vertices = init.Add(new ModelBuffer<VertTex2D>(vertsQuadTex2, indicesQuad));

            init.Init(device);

            // Prepare All the stages
            // vertices.Bind(context);

            // context.OutputMerger.BlendState = blendState;

            // Main loop
            RenderLoop.Run(form, () =>
            {
                // using (rasterizerState.ApplyTo(context))
                {
                    //shaderProgram2.Bind(context);
                    //shapeBuffer.UseContext(context);

                    ////context.PixelShader.SetSampler(0, sampler);
                    ////context.PixelShader.SetShaderResource(0, texture);
                    //surface.BindViewAndClear(context);
                    ////foreach (var v in vertsQuadTex2) shapeBuffer.Push(v);
                    ////shapeBuffer.Flush();
                    ////context.ClearRenderTargetView(form.RenderTarget);
                    ////context.PixelShader.SetSampler(0, sampler);
                    //compositor.Composite(context, surface, shaderProgram2, form);
                    //shaderProgram.Bind(context);
                    //flatBuff.UseContext(context);
                    //flatBuff.PushQuad(0.5f, 1, 1, 0.5f, new Color4(0, 1, 1, 1));
                    //flatBuff.Flush();
                    //form.Present(false);


                    //form.BindViewAndClear(context);
                    //shaderProgram.Bind(context);
                    //flatBuff.UseContext(context);
                    //flatBuff.PushQuad(0.5f, 1, 1, 0.5f, new Color4(0, 1, 1, 1));
                    //flatBuff.Flush();
                    //form.Present(false);
                }
            });

            // Release all resources
            vertices.Dispose();
            context.ClearState();
            context.Flush();
            device.Dispose();
            context.Dispose();
            init.Dispose();
        }
    }
}
