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
using System.Runtime.InteropServices;

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

        string shader = @"
struct VS_IN
{
	float2 pos : POSITION;
	float4 col : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
};

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;
	
	output.pos = float4(input.pos, 0, 1);
	output.col = input.col;
	
	return output;
}

float4 PS( PS_IN input ) : SV_Target
{
	return input.col;
}

technique10 Render
{
	pass P0
	{
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
";

        public test()
        {
            var init = new Initiator();

            var form = new RenderForm("SharpDX - MiniTri Direct3D 11 Sample");

            ref RenderForm a = ref form;

            // SwapChain description
            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription =
                                   new ModeDescription(form.ClientSize.Width, form.ClientSize.Height,
                                                       new Rational(60, 1), Format.R8G8B8A8_UNorm),
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

            // Ignore all windows events
            var factory = swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            // New RenderTargetView from the backbuffer
            var backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            var renderView = new RenderTargetView(device, backBuffer);

            var shaderProgram = init.Add(new ShaderProgram<Vert>(shader, "4_0", "VS", "PS"));

            var vertsTriangle = new[]
            {
                new Vert(new Vector2(0.0f, 0.5f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f)),
                new Vert(new Vector2(0.5f, -0.5f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f)),
                new Vert(new Vector2(-0.5f, -0.5f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f))
            };

            var vertsQuad = new[]
            {
                new Vert(new Vector2(0.5f, 0.5f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f)),
                new Vert(new Vector2(0.5f, -0.5f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f)),
                new Vert(new Vector2(-0.5f, -0.5f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f)),
                new Vert(new Vector2(-0.5f, 0.5f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f)),
            };

            var indicesQuad = new[] { 0, 1, 2, 0, 2, 3 };

            var shapeBuffer = init.Add(new ShapeBuffer<Vert>(100, PrimitiveTopology.TriangleList, ShapePresets.Quads));

            // Instantiate Vertex buiffer from vertex data
            var vertices = init.Add(new ModelBuffer<Vert>(vertsQuad, indicesQuad));

            init.Init(device);

            // Prepare All the stages
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            vertices.Bind(context);
            context.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
            shaderProgram.Bind(context);
            context.OutputMerger.SetTargets(renderView);

            // Main loop
            RenderLoop.Run(form, () =>
            {
                context.ClearRenderTargetView(renderView, Color.Black);
                //context.Draw(3, 0);
                //context.DrawIndexed(6, 0, 0);
                shapeBuffer.UseContext(context);
                foreach (var v in vertsQuad) shapeBuffer.Push(v);
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
