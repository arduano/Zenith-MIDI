using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace ZenithEngine.DXHelper
{
    public class ShaderProgram<U> : ShaderProgram
        where U : struct
    {
        U lastData;

        public U UniformData;

        Buffer dynamicConstantBuffer;

        public ShaderProgram(string shader, Type inputType, string version, string vertEntry, string fragEntry, string geoEntry = null)
            : base(shader, inputType, version, vertEntry, fragEntry, geoEntry) { }

        public ShaderProgram(string shader, string inputTypeName, Type inputType, Type instanceType, string version, string vertEntry, string fragEntry, string geoEntry = null)
            : base(shader, inputTypeName, inputType, instanceType, version, vertEntry, fragEntry, geoEntry) { }

        public ShaderProgram(string shader, Type inputType, Type instanceType, string version, string vertEntry, string fragEntry, string geoEntry = null)
            : base(shader, inputType, instanceType, version, vertEntry, fragEntry, geoEntry) { }

        public ShaderProgram(string shader, InputElement[] layoutParts, string version, string vertEntry, string fragEntry, string geoEntry = null)
         : base(shader, layoutParts, version, vertEntry, fragEntry, geoEntry) { }

        public void SetConstant(U data)
        {
            UniformData = data;
        }

        protected override void InitInternal()
        {
            base.InitInternal();
            dynamicConstantBuffer = new Buffer(Device, Utilities.SizeOf<U>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
        }

        protected override void DisposeInternal()
        {
            base.DisposeInternal();
            dynamicConstantBuffer.Dispose();
        }

        public override void Bind(DeviceContext context)
        {
            base.Bind(context);

            if (!lastData.Equals(UniformData))
            {
                var dataBox = context.MapSubresource(dynamicConstantBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
                Utilities.Write(dataBox.DataPointer, ref UniformData);
                context.UnmapSubresource(dynamicConstantBuffer, 0);
                lastData = UniformData;
            }

            context.VertexShader.SetConstantBuffer(0, dynamicConstantBuffer);
            context.PixelShader.SetConstantBuffer(0, dynamicConstantBuffer);
            context.GeometryShader?.SetConstantBuffer(0, dynamicConstantBuffer);
        }
    }

    public class ShaderProgram : DeviceInitiable
    {
        public ShaderBytecode VertexShaderByteCode { get; private set; }
        public VertexShader VertexShader { get; private set; }
        public ShaderBytecode PixelShaderByteCode { get; private set; }
        public PixelShader PixelShader { get; private set; }
        public ShaderBytecode GeometryShaderByteCode { get; private set; }
        public GeometryShader GeometryShader { get; private set; }

        public InputLayout InputLayout { get; private set; }

        protected DisposeGroup dispose = new DisposeGroup();

        string fragEntry;
        string vertEntry;
        string geoEntry;
        string version;
        string shader;

        InputElement[] layoutParts;

        List<string> basicPrepend = new List<string>();

        public ShaderProgram(string shader, Type inputType, string version, string vertEntry, string fragEntry, string geoEntry = null)
            : this(shader, ShaderHelper.GetLayout(inputType), version, vertEntry, fragEntry, geoEntry)
        {
            basicPrepend.Add(ShaderHelper.BuildStructDefinition(inputType));
        }

        public ShaderProgram(string shader, string inputTypeName, Type inputType, Type instanceType, string version, string vertEntry, string fragEntry, string geoEntry = null)
            : this(shader, ShaderHelper.GetLayout(inputType).Concat(ShaderHelper.GetLayout(instanceType)).ToArray(), version, vertEntry, fragEntry, geoEntry)
        {
            basicPrepend.Add(ShaderHelper.BuildStructDefinition(inputTypeName, inputType, instanceType));
        }

        public ShaderProgram(string shader, Type inputType, Type instanceType, string version, string vertEntry, string fragEntry, string geoEntry = null)
            : this(shader, inputType.Name, inputType, instanceType, version, vertEntry, fragEntry, geoEntry)
        { }

        public ShaderProgram(string shader, InputElement[] layoutParts, string version, string vertEntry, string fragEntry, string geoEntry = null)
        {
            this.shader = shader.Replace("\t", "    ");
            this.version = version;
            this.fragEntry = fragEntry;
            this.vertEntry = vertEntry;
            this.geoEntry = geoEntry;
            this.layoutParts = layoutParts;
        }

        public string GetPreparedCode()
        {
            return String.Join("\n\n", basicPrepend) + "\n\n" + shader;
        }

        protected override void InitInternal()
        {
            dispose = new DisposeGroup();

            var code = GetPreparedCode();

            Console.WriteLine(code);

            VertexShaderByteCode = dispose.Add(ShaderBytecode.Compile(code, vertEntry, "vs_" + version, ShaderFlags.None, EffectFlags.None));
            VertexShader = dispose.Add(new VertexShader(Device, VertexShaderByteCode));
            PixelShaderByteCode = dispose.Add(ShaderBytecode.Compile(code, fragEntry, "ps_" + version, ShaderFlags.None, EffectFlags.None));
            PixelShader = dispose.Add(new PixelShader(Device, PixelShaderByteCode));

            if (geoEntry != null)
            {
                GeometryShaderByteCode = dispose.Add(ShaderBytecode.Compile(code, geoEntry, "gs_" + version, ShaderFlags.None, EffectFlags.None));
                GeometryShader = dispose.Add(new GeometryShader(Device, GeometryShaderByteCode));
            }

            InputLayout = dispose.Add(new InputLayout(Device, VertexShaderByteCode, layoutParts));
        }

        protected override void DisposeInternal()
        {
            dispose.Dispose();
        }

        public virtual void Bind(DeviceContext context)
        {
            context.InputAssembler.InputLayout = InputLayout;
            context.VertexShader.Set(VertexShader);
            context.PixelShader.Set(PixelShader);
            context.GeometryShader.Set(GeometryShader);
        }
    }
}
