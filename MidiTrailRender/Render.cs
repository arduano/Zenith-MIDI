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

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct KeyVert
    {
        public static ShaderProgram GetBasicShader() =>
            ShaderProgram.Presets.Basic();

        Vector3 pos;
        Vector3 normal;
        float side;

        public KeyVert(Vector3 pos, Vector3 normal, float side)
        {
            this.pos = pos;
            this.normal = normal;
            this.side = side;
        }
    }

    class KeyModelDataEdge : IDisposable
    {
        public ModelBuffer<KeyVert>[] Models { get; set; }

        public ModelBuffer<KeyVert> GetKey(int key) => Models[key % 12];

        public KeyModelDataEdge(ModelBuffer<KeyVert>[] models)
        {
            Models = models;
        }

        public void Init()
        {
            foreach (var m in Models) m.Init();
        }

        public void Dispose()
        {
            foreach (var m in Models) m.Dispose();
        }
    }

    class KeyModelDataType : IDisposable
    {
        public KeyModelDataEdge Normal { get; set; }
        public KeyModelDataEdge Left { get; set; }
        public KeyModelDataEdge Right { get; set; }

        public void Init()
        {
            Normal.Init();
            Left.Init();
            Right.Init();
        }

        public void Dispose()
        {
            Normal.Dispose();
            Left.Dispose();
            Right.Dispose();
        }
    }

    class KeyModelData : IDisposable
    {
        public KeyModelDataType SameWidth { get; set; }
        public KeyModelDataType DifferentWidth { get; set; }

        public void Init()
        {
            SameWidth.Init();
            DifferentWidth.Init();
        }

        public void Dispose()
        {
            SameWidth.Dispose();
            DifferentWidth.Dispose();
        }
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

        public FrameworkElement SettingsControl => null;

        public double StartOffset => 0;

        RenderStatus renderStatus;
        MidiPlayback midi = null;

        ShaderProgram keyShader;

        BasicRenderSurface depthSurface;
        Compositor compositor;
        ShaderProgram compositeShader;

        ShaderProgram quadShader;
        BasicShapeBuffer quadBuffer;

        DisposeGroup disposer;

        KeyModelData keyModels;

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

            Dictionary<string, ModelBuffer<KeyVert>> models = new Dictionary<string, ModelBuffer<KeyVert>>();

            Parallel.ForEach(obj.Groups, group =>
            {
                if (group.Name.StartsWith("p-"))
                    return;

                var verts = new List<KeyVert>();
                var indices = new List<int>();
                foreach (var f in group.Faces)
                {
                    if (f.Count != 3) throw new Exception("Non triangle faces not supported");
                    for (int i = 0; i < 3; i++)
                    {
                        var v = f[i];
                        var vert = obj.Vertices[v.VertexIndex - 1];
                        var norm = obj.Normals[v.NormalIndex - 1];
                        verts.Add(new KeyVert(
                            new Vector3(vert.X, vert.Y, vert.Z),
                            new Vector3(norm.X, norm.Y, norm.Z),
                            1));
                        indices.Add(indices.Count);
                    }
                }

                var shape = new ModelBuffer<KeyVert>(verts.ToArray(), indices.ToArray(), new[] {
                    new InputAssemblyPart(3, VertexAttribPointerType.Float),
                    new InputAssemblyPart(3, VertexAttribPointerType.Float),
                    new InputAssemblyPart(1, VertexAttribPointerType.Float),
                });

                lock (models)
                {
                    models.Add(group.Name, shape);
                }
            });

            ModelBuffer<KeyVert>[] getArray(string category, string side)
            {
                var keys = new ModelBuffer<KeyVert>[12];
                for (int i = 0; i < 12; i++)
                {
                    if (KeyboardState.IsBlackKey(i))
                        keys[i] = models[$"black-{category}"];
                    else
                        keys[i] = models[$"white-{category}-{side}-{i}"];
                }
                return keys;
            }

            keyModels = new KeyModelData()
            {
                SameWidth = new KeyModelDataType()
                {
                    Left = new KeyModelDataEdge(getArray("sw", "left")),
                    Normal = new KeyModelDataEdge(getArray("sw", "both")),
                    Right = new KeyModelDataEdge(getArray("sw", "right")),
                },
                DifferentWidth = new KeyModelDataType()
                {
                    Left = new KeyModelDataEdge(getArray("dw", "left")),
                    Normal = new KeyModelDataEdge(getArray("dw", "both")),
                    Right = new KeyModelDataEdge(getArray("dw", "right")),
                }
            };

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
            disposer.Add(keyShader);
            disposer.Add(keyModels);
            keyModels.Init();

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
            using (new GLEnabler().Enable(EnableCap.Blend))
            {

                using (new GLEnabler().Enable(EnableCap.DepthTest).Enable(EnableCap.CullFace))
                {
                    GL.DepthFunc(DepthFunction.Less);

                    depthSurface.BindSurfaceAndClear();
                    GL.Clear(ClearBufferMask.DepthBufferBit);

                    int firstKey = 0;
                    int lastKey = 128;

                    var keyboard = new KeyboardState(firstKey, lastKey, new KeyboardParams()
                    {
                        BlackKey2setOffset = 0.15,
                        BlackKey3setOffset = 0.3,
                        BlackKeyScale = 0.583,
                        SameWidthNotes = true,
                    });

                    keyShader.Bind();

                    Matrix4 view = Matrix4.Identity *
                        Matrix4.CreateTranslation(0, 0, -0.1f) *
                        Matrix4.CreatePerspectiveFieldOfView(MathHelper.Pi / 2, renderStatus.AspectRatio, 0.01f, 400);
                    Matrix3 viewNorm = new Matrix3(view);

                    GL.UniformMatrix4(keyShader.Uniform("matView"), false, ref view);
                    GL.UniformMatrix3(keyShader.Uniform("matViewNorm"), false, ref viewNorm);

                    var keyType = keyModels.SameWidth;
                    var keyPart = keyType.Normal;

                    for (int i = 0; i < 128; i++)
                    {
                        var layoutKey = keyboard.Keys[i];

                        var width = layoutKey.Right - layoutKey.Left;
                        var middle = (layoutKey.Right + layoutKey.Left) / 2 - 0.5f;

                        Matrix4 model = Matrix4.Identity *
                            Matrix4.CreateScale((float)keyboard.BlackKeyWidth * 2) *
                            Matrix4.CreateTranslation((float)middle, 0, 0) *
                            Matrix4.CreateRotationX(MathHelper.Pi / 2 * 0.3f) *
                            Matrix4.CreateRotationY(MathHelper.Pi / 2 * (float)midi.PlayerPositionSeconds) *
                            Matrix4.CreateTranslation(0, 0, 0);
                        Matrix3 modelNorm = new Matrix3(model);

                        GL.UniformMatrix4(keyShader.Uniform("matModel"), false, ref model);
                        GL.UniformMatrix3(keyShader.Uniform("matModelNorm"), false, ref modelNorm);
                        keyPart.GetKey(i).DrawSingle();
                    }
                }

                compositor.Composite(depthSurface, compositeShader, renderSurface);
            }
        }

        public void ReloadTrackColors()
        {
            //throw new NotImplementedException();
        }
    }
}
