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

            var form = new RenderForm("SharpDX - MiniTri Direct3D 11 Sample");

            form.IsFullscreen = true;

            // SwapChain description
            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(
                    form.ClientSize.Width,
                    form.ClientSize.Height,
                    new Rational(60, 1), 
                    Format.R8G8B8A8_UNorm
                ),
                IsWindowed = true,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput,
            };

            // Create Device and SwapChain
            Device device;
            SwapChain swapChain;
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out device, out swapChain);
            var context = device.ImmediateContext;

            Shaders.BasicFlat();

            // Ignore all windows events
            var factory = swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            // New RenderTargetView from the backbuffer
            var backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            var renderView = new RenderTargetView(device, backBuffer);

            //var shaderProgram = init.Add(Shaders.BasicFlat());
            var shaderProgram2 = init.Add(Shaders.BasicTextured());
            var sampler = init.Add(new TextureSampler());
            var texture = init.Add(RenderTexture.FromBitmap((Bitmap)Bitmap.FromFile("icon.png")));
            var blendState = init.Add(new BlendStateKeeper());

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
                new VertTex2D(new Vector2(0.5f, -0.5f), new Vector2(1, 0), new Color4(1.0f)),
                new VertTex2D(new Vector2(-0.5f, -0.5f), new Vector2(0, 0), new Color4(1.0f)),
                new VertTex2D(new Vector2(-0.5f, 0.5f), new Vector2(0, 1), new Color4(1.0f)),
            };

            var indicesQuad = new[] { 0, 1, 2, 0, 2, 3 };

            var shapeBuffer = init.Add(new ShapeBuffer<VertTex2D>(100, PrimitiveTopology.TriangleList, ShapePresets.Quads));

            // Instantiate Vertex buiffer from vertex data
            var vertices = init.Add(new ModelBuffer<VertTex2D>(vertsQuadTex, indicesQuad));

            init.Init(device);

            // Prepare All the stages
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            vertices.Bind(context);
            shaderProgram2.Bind(context);
            context.OutputMerger.SetTargets(renderView);

            context.PixelShader.SetSampler(0, sampler);
            context.PixelShader.SetShaderResource(0, texture);

            context.OutputMerger.BlendState = blendState;

            // Main loop
            RenderLoop.Run(form, () =>
            {
                //form.WindowState = System.Windows.Forms.FormWindowState.Maximized;
                context.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
                context.ClearRenderTargetView(renderView, Color.Black);
                //context.Draw(3, 0);
                //context.DrawIndexed(6, 0, 0);
                shapeBuffer.UseContext(context);
                foreach (var v in vertsQuadTex2) shapeBuffer.Push(v);
                shapeBuffer.Flush();
                swapChain.Present(0, PresentFlags.None);
            });

            // Release all resources
            vertices.Dispose();
            renderView.Dispose();
            backBuffer.Dispose();
            context.ClearState();
            context.Flush();
            device.Dispose();
            context.Dispose();
            swapChain.Dispose();
            factory.Dispose();
            init.Dispose();
        }
    }
}
