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
        bool first = true;

        public U ConstData;

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
            ConstData = data;
        }

        protected override void InitInternal()
        {
            base.InitInternal();
            var size = Utilities.SizeOf<U>();
            if (size % 16 != 0) size += 16 - (size % 16);
            dynamicConstantBuffer = new Buffer(Device, size, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            first = true;
        }

        protected override void DisposeInternal()
        {
            base.DisposeInternal();
            dynamicConstantBuffer.Dispose();
        }

        public void UpdateConstBuffer(DeviceContext context)
        {
            if (!lastData.Equals(ConstData) || first)
            {
                first = false;
                var dataBox = context.MapSubresource(dynamicConstantBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
                Utilities.Write(dataBox.DataPointer, ref ConstData);
                context.UnmapSubresource(dynamicConstantBuffer, 0);
                lastData = ConstData;
            }

            context.VertexShader.SetConstantBuffer(0, dynamicConstantBuffer);
            context.PixelShader.SetConstantBuffer(0, dynamicConstantBuffer);
            context.GeometryShader?.SetConstantBuffer(0, dynamicConstantBuffer);
        }

        public override IDisposable UseOn(DeviceContext context)
        {
            var applier = base.UseOn(context);

            UpdateConstBuffer(context);

            return applier;
        }
    }

    public class ShaderProgram : PureDeviceInitiable
    {
        struct ShaderKeep
        {
            public ShaderKeep(VertexShader vert, PixelShader pixel, GeometryShader geo, InputLayout input)
            {
                Vert = vert;
                Pixel = pixel;
                Geo = geo;
                Input = input;
            }

            public VertexShader Vert { get; }
            public PixelShader Pixel { get; }
            public GeometryShader Geo { get; }
            public InputLayout Input { get; }
        }

        public ShaderBytecode VertexShaderByteCode { get; private set; }
        public VertexShader VertexShader { get; private set; }
        public ShaderBytecode PixelShaderByteCode { get; private set; }
        public PixelShader PixelShader { get; private set; }
        public ShaderBytecode GeometryShaderByteCode { get; private set; }
        public GeometryShader GeometryShader { get; private set; }

        public InputLayout InputLayout { get; private set; }
        DisposeGroup dispose = new DisposeGroup();

        bool initedShader = false;

        string fragEntry;
        string vertEntry;
        string geoEntry;
        string version;
        string shader;

        InputElement[] layoutParts;

        List<string> basicPrepend = new List<string>();

        Dictionary<string, string> defines = new Dictionary<string, string>();

        public ShaderProgram(string shader, Type inputType, string version, string vertEntry, string fragEntry, string geoEntry = null)
            : this(shader, ShaderHelper.GetLayout(inputType), version, vertEntry, fragEntry, geoEntry)
        {
            basicPrepend.Add(ShaderHelper.BuildStructDefinition(inputType));
        }

        public ShaderProgram(string shader, string inputTypeName, Type inputType, Type instanceType, string version, string vertEntry, string fragEntry, string geoEntry = null)
            : this(shader, ShaderHelper.GetLayout(inputType).Concat(ShaderHelper.GetLayout(instanceType, 1, 1)).ToArray(), version, vertEntry, fragEntry, geoEntry)
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
            string definesPrepend = "";

            foreach (var k in defines)
            {
                definesPrepend += $"#define {k.Key} {k.Value}\n";
            }

            return String.Join("\n\n", basicPrepend) + "\n\n" + definesPrepend + "\n\n" + shader;
        }

        protected override void InitInternal()
        {
            InitShader();
        }

        protected override void DisposeInternal()
        {
            DisposeShader();
        }

        void InitShader()
        {
            if (initedShader) return;
            initedShader = true;

            dispose = new DisposeGroup();

            var code = GetPreparedCode();

            Console.WriteLine(string.Join("\n", code.Split('\n').Select((s, i) => $"{i}. {s}").ToArray()));

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

        void DisposeShader()
        {
            if (!initedShader) return;
            initedShader = false;

            dispose.Dispose();
        }

        public ShaderProgram SetDefine(string define) => SetDefine(define, "");
        public ShaderProgram SetDefine(string define, int value) => SetDefine(define, value.ToString());
        public ShaderProgram SetDefine(string define, float value) => SetDefine(define, value.ToString());
        public ShaderProgram SetDefine(string define, string value)
        {
            if (defines.ContainsKey(define))
            {
                if (defines[define] == value) return this;
                defines[define] = value;
            }
            else
            {
                defines.Add(define, value);
            }

            DisposeShader();

            return this;
        }

        public ShaderProgram RemoveDefine(string define)
        {
            if (!defines.ContainsKey(define)) return this;
            defines.Remove(define);
            DisposeShader();
            return this;
        }

        public virtual IDisposable UseOn(DeviceContext context)
        {
            InitShader();
            return new Applier<ShaderKeep>(
                new ShaderKeep(VertexShader, PixelShader, GeometryShader, InputLayout),
                () => new ShaderKeep(context.VertexShader.Get(), context.PixelShader.Get(), context.GeometryShader.Get(), context.InputAssembler.InputLayout),
                (val) =>
                {
                    context.InputAssembler.InputLayout = val.Input;
                    context.VertexShader.Set(val.Vert);
                    context.PixelShader.Set(val.Pixel); 
                    context.GeometryShader.Set(val.Geo);
                });
        }
    }
}
