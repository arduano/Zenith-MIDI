using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using ZenithEngine;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using ZenithEngine.GLEngine;
using System.Runtime.InteropServices;
using ZenithEngine.ModuleUtil;
using ZenithEngine.Modules;
using ZenithEngine.MIDI;
using ZenithEngine.ModuleUI;
using ObjLoader.Loader.Loaders;
using System.Reflection;

namespace MIDITrailRender
{
    struct IndexNorm
    {
        public int vertIndex;
        public int normIndex;
    }

    public class Render : IModuleRender
    {
        #region Info
        public string Name { get; } = "MIDITrail";
        public string Description { get; } = "aaa";
        public bool Initialized { get; private set; } = false;
        public ImageSource PreviewImage { get; } = ModuleUtils.BitmapToImageSource(Properties.Resources.preview);
        public string LanguageDictName { get; } = "miditrail";
        #endregion

        #region Shaders

        #endregion

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct Vert
        {
            public static ShaderProgram GetBasicShader() =>
                ShaderProgram.Presets.Basic();

            Vector3 pos;
            Color4 color;
            float side;

            public Vert(Vector3 pos, Color4 color, float side)
            {
                this.pos = pos;
                this.color = color;
                this.side = side;
            }
        }

        public FrameworkElement SettingsControl => null;

        public double StartOffset => 0;

        RenderStatus renderStatus;
        MidiPlayback midi = null;

        ShaderProgram keyShader;

        BasicRenderSurface depthSurface ;
        Compositor compositor;
        ShaderProgram compositeShader;

        ShaderProgram quadShader;
        BasicShapeBuffer quadBuffer;

        DisposeGroup disposer;

        ModelBuffer<Vert> shape;

        public Render()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string ReadEmbed(string name)
            {
                return new StreamReader(assembly.GetManifestResourceStream(name)).ReadToEnd();
            }

            var factory = new ObjLoaderFactory();
            var objLoader = factory.Create();
            var obj = objLoader.Load(new BufferedStream(File.OpenRead("E:/keys.obj")));

            var o = obj.Groups[0];

            var verts = new List<Vert>();
            var norms = new List<int>();
            foreach (var f in o.Faces)
            {
                if (f.Count != 3) throw new Exception("Non triangle faces not supported");
                for(int i = 0; i < 3; i++)
                {
                    var v = f[i];
                    var vert = obj.Vertices[v.VertexIndex];
                    var norm = obj.Normals[v.NormalIndex];
                    verts.Add(new Vert(new Vector3(vert.X / 5, vert.Y / 5, vert.Z / 5), Color4.White, 1));
                    norms.Add(norms.Count);
                }
            }

            shape = new ModelBuffer<Vert>(verts.ToArray(), norms.ToArray(), new[] { 
                new InputAssemblyPart(3, VertexAttribPointerType.Float, 0),
                new InputAssemblyPart(4, VertexAttribPointerType.Float, 3 * 4),
                new InputAssemblyPart(1, VertexAttribPointerType.Float, 7 * 4),
            });

            var resources = assembly.GetManifestResourceNames();
            keyShader = new ShaderProgram(
                ReadEmbed("MIDITrailRender.Shaders.keys.vert.glsl"),
                ReadEmbed("MIDITrailRender.Shaders.keys.frag.glsl")
            );
        }

        public void Init(MidiPlayback midi, RenderStatus status)
        {
            renderStatus = status;
            this.midi = midi;

            disposer = new DisposeGroup();
            disposer.Add(shape);
            shape.Init();

            depthSurface = disposer.Add(new BasicRenderSurface(status.RenderWidth, status.RenderHeight, true));
            compositeShader = disposer.Add(ShaderProgram.Presets.BasicTextured());
            compositor = disposer.Add(new Compositor());

            quadShader = disposer.Add(BasicShapeBuffer.GetBasicShader());
            quadBuffer = disposer.Add(new BasicShapeBuffer(100, ShapePresets.Quads));


            Initialized = true;
        }

        public void Dispose()
        {
            disposer.Dispose();
            Initialized = false;
        }

        public void RenderFrame(RenderSurface renderSurface)
        {
            depthSurface.BindSurfaceAndClear();

            Matrix4 view = Matrix4.Identity *
                Matrix4.CreateRotationX(MathHelper.Pi / 2 * 0.3f) *
                Matrix4.CreateRotationY(MathHelper.Pi / 2 * 0.3f) *
                Matrix4.CreateRotationZ(MathHelper.Pi / 2 * 0.3f) *
                Matrix4.CreateTranslation(0, -1, -1) *
            Matrix4.CreatePerspectiveFieldOfView(MathHelper.Pi / 2, renderStatus.AspectRatio, 0.01f, 400);
            //Matrix4 view = Matrix4.Identity;
            Matrix4 model = Matrix4.Identity;

            keyShader.Bind();
            GL.UniformMatrix4(keyShader.Uniform("view"), false, ref view);
            GL.UniformMatrix4(keyShader.Uniform("model"), false, ref model);

            shape.DrawSingle();

            compositor.Composite(depthSurface, compositeShader, renderSurface);
        }

        public void ReloadTrackColors()
        {
            //throw new NotImplementedException();
        }
    }
}
