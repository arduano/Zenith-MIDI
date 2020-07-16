using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using ZenithEngine.DXHelper;
using ZenithEngine.DXHelper.Presets;
using System.Runtime.InteropServices;
using ZenithEngine.ModuleUtil;
using ZenithEngine.Modules;
using ZenithEngine.MIDI;
using ZenithEngine.ModuleUI;
using ObjLoader.Loader.Loaders;
using System.Reflection;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Mathematics;
using System.Windows.Media;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using Matrix = SharpDX.Matrix;

namespace MIDITrailRender
{
    struct IndexNorm
    {
        public int vertIndex;
        public int normIndex;
    }

    struct BasicVert
    {
        public Vector3 Pos;
        public Vector3 Normal;

        public BasicVert(Vector3 pos, Vector3 normal)
        {
            Pos = pos;
            Normal = normal;
        }
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct KeyVert
    {
        [AssemblyElement("POS", Format.R32G32B32_Float)]
        public Vector3 Pos;

        [AssemblyElement("NORM", Format.R32G32B32_Float)]
        public Vector3 Normal;

        [AssemblyElement("SIDE", Format.R32_Float)]
        public float Side;

        public KeyVert(Vector3 pos, Vector3 normal, float side)
        {
            Pos = pos;
            Normal = normal;
            Side = side;
        }
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NoteVert
    {
        [AssemblyElement("POS", Format.R32G32B32_Float)]
        public Vector3 Pos;

        [AssemblyElement("NORM", Format.R32G32B32_Float)]
        public Vector3 Normal;

        [AssemblyElement("SIDE", Format.R32_Float)]
        public float Side;

        [AssemblyElement("CORNER", Format.R32G32B32_Float)]
        public Vector3 Corner;

        public NoteVert(Vector3 pos, Vector3 normal, float side, Vector3 corner)
        {
            Pos = pos;
            Normal = normal;
            Side = side;
            Corner = corner;
        }
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 16)]
    public struct FullColorData
    {
        public Color4 Diffuse;
        public Color4 Emit;
        public Color4 Specular;
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 16)]
    public struct KeyShaderConstant
    {
        public Matrix Model;
        public Matrix View;
        public Vector3 ViewPos;
        public float PressDepth;
        public FullColorData LeftColor;
        public FullColorData RightColor;
    }

    //[StructLayoutAttribute(LayoutKind.Sequential)]
    //public struct NoteInstance
    //{
    //    [AssemblyPart(1, VertexAttribPointerType.Float)]
    //    public float Left;

    //    [AssemblyPart(1, VertexAttribPointerType.Float)]
    //    public float Right;

    //    [AssemblyPart(1, VertexAttribPointerType.Float)]
    //    public float Start;

    //    [AssemblyPart(1, VertexAttribPointerType.Float)]
    //    public float End;

    //    [AssemblyPart(4, VertexAttribPointerType.Float)]
    //    public Color4 ColorLeft;

    //    [AssemblyPart(4, VertexAttribPointerType.Float)]
    //    public Color4 ColorRight;

    //    [AssemblyPart(1, VertexAttribPointerType.Float)]
    //    public float Scale;

    //    [AssemblyPart(1, VertexAttribPointerType.Float)]
    //    public float ExtraScale;

    //    public NoteInstance(float left, float right, float start, float end, Color4 colorLeft, Color4 colorRight, float scale, float extraScale)
    //    {
    //        Left = left;
    //        Right = right;
    //        Start = start;
    //        End = end;
    //        ColorLeft = colorLeft;
    //        ColorRight = colorRight;
    //        Scale = scale;
    //        ExtraScale = extraScale;
    //    }
    //}

    class KeyModelDataEdge : DeviceInitiable
    {
        public ModelBuffer<KeyVert>[] Models { get; set; }

        public ModelBuffer<KeyVert> GetKey(int key) => Models[key % 12];

        public KeyModelDataEdge(ModelBuffer<KeyVert>[] models)
        {
            Models = models;
        }

        protected override void InitInternal()
        {
            foreach (var m in Models) m.Init(Device);
        }

        protected override void DisposeInternal()
        {
            foreach (var m in Models) m.Dispose();
        }
    }

    class KeyModelDataType : DeviceInitiable
    {
        public KeyModelDataEdge Normal { get; set; }
        public KeyModelDataEdge Left { get; set; }
        public KeyModelDataEdge Right { get; set; }

        protected override void InitInternal()
        {
            Normal.Init(Device);
            Left.Init(Device);
            Right.Init(Device);
        }

        protected override void DisposeInternal()
        {
            Normal.Dispose();
            Left.Dispose();
            Right.Dispose();
        }
    }

    class KeyModelData : DeviceInitiable
    {
        public KeyModelDataType SameWidth { get; set; }
        public KeyModelDataType DifferentWidth { get; set; }

        protected override void InitInternal()
        {
            SameWidth.Init(Device);
            DifferentWidth.Init(Device);
        }

        protected override void DisposeInternal()
        {
            SameWidth.Dispose();
            DifferentWidth.Dispose();
        }
    }

    abstract class RenderObject
    {
        public RenderObject(ModelBuffer<KeyVert> model, Matrix transform)
        {
            var a = Matrix.Identity * Matrix.Identity;
            Model = model;
            Transform = transform;
            Position = transform.TranslationVector;
        }

        public ModelBuffer<KeyVert> Model { get; set; }
        public Vector3 Position { get; }
        public Matrix Transform { get; }

        public abstract void Render(DeviceContext context);
    }

    public class Render : IModuleRender
    {
        #region Info
        public string Name { get; } = "MIDITrail";
        public string Description { get; } = "aaa";
        public bool Initialized { get; private set; } = false;
        public ImageSource PreviewImage { get; } = ModuleUtils.BitmapToImageSource(Properties.Resources.preview);
        ImageSource IModuleRender.PreviewImage => PreviewImage;
        public string LanguageDictName { get; } = "miditrail";
        #endregion

        #region UI
        class UI : UIDockWithPalettes
        {
            public class Keys : UIDock
            {
                public Keys() : base(Dock.Left) { }

                [UIChild]
                public UINumber left = new UINumber()
                {
                    Label = new DynamicResourceExtension("firstNote"),
                    Min = 0,
                    Max = 255,
                    Value = 0,
                };

                [UIChild]
                public UINumber right = new UINumber()
                {
                    Label = new DynamicResourceExtension("lastNote"),
                    Min = 1,
                    Max = 256,
                    Value = 128,
                };
            }

            [UIChild]
            public Keys keys = new Keys();

            [UIChild]
            public UINumberSlider noteScreenTime = new UINumberSlider()
            {
                Label = new DynamicResourceExtension("noteScreenTime"),
                SliderMin = 2,
                SliderMax = 4096,
                Min = 0.1,
                Max = 1000000,
                DecimalPoints = 2,
                Step = 1,
                Value = 400,
            };

            [UIChild]
            public UINumberSlider kbHeight = new UINumberSlider()
            {
                Label = new DynamicResourceExtension("pianoHeight"),
                SliderMin = 0,
                SliderMax = 100,
                Min = 0,
                Max = 100,
                DecimalPoints = 2,
                Step = 1,
                Value = 16,
                SliderWidth = 200,
            };

            [UIChild]
            public UICheckbox sameWidthNotes = new UICheckbox()
            {
                Label = new DynamicResourceExtension("sameWidthNotes"),
                IsChecked = true,
            };
        }
        #endregion

        #region RenderObjects
        class KeyRenderObject : RenderObject
        {
            public int Key { get; }
            KeyboardState Keyboard { get; }

            Render render;

            public KeyRenderObject(Render render, KeyboardState keyboard, ModelBuffer<KeyVert> model, Matrix transform, int key) : base(model, transform)
            {
                Key = key;
                this.render = render;
                this.Keyboard = keyboard;
            }

            public override void Render(DeviceContext context)
            {
                var keyShader = render.keyShader;
                keyShader.ConstData.Model = Transform;

                keyShader.ConstData.LeftColor.Diffuse = Keyboard.Colors[Key].Left;
                keyShader.ConstData.RightColor.Diffuse = Keyboard.Colors[Key].Right;

                keyShader.ConstData.LeftColor.Emit.Alpha = 0;
                keyShader.ConstData.RightColor.Emit.Alpha = 0;
                
                keyShader.ConstData.LeftColor.Emit = Keyboard.Colors[Key].Left;
                keyShader.ConstData.RightColor.Emit = Keyboard.Colors[Key].Right;

                float glowStrength = Math.Max(render.keyPressPos[Key], 0);

                keyShader.ConstData.LeftColor.Emit.Alpha = 2 * glowStrength;
                keyShader.ConstData.RightColor.Emit.Alpha = 2 * glowStrength;

                keyShader.ConstData.LeftColor.Specular = new Color4(1, 1, 1, 1);
                keyShader.ConstData.RightColor.Specular = new Color4(1, 1, 1, 1);

                using (keyShader.UseOn(context))
                    Model.BindAndDraw(context);
            }
        }
        #endregion

        UI settings = new UI();
        public FrameworkElement SettingsControl => settings;

        public double StartOffset => 0;


        RenderStatus renderStatus;
        MidiPlayback midi = null;

        ShaderProgram<KeyShaderConstant> keyShader;
        ShaderProgram noteShader;

        CompositeRenderSurface depthSurface;
        CompositeRenderSurface cutoffSurface;
        CompositeRenderSurface preFinalSurface;
        Compositor compositor;
        ShaderProgram plainShader;
        ShaderProgram colorspaceShader;
        ShaderProgram colorCutoffShader;

        BlendStateKeeper addBlendState;

        PingPongGlow pingPongGlow;

        ShaderProgram quadShader;

        Initiator init = new Initiator();

        KeyModelData keyModels;

        //InstancedModelBuffer<NoteVert, NoteInstance> flatNoteBuffer;
        //InstancedModelBuffer<NoteVert, NoteInstance> cubeNoteBuffer;
        //InstancedModelBuffer<NoteVert, NoteInstance> roundedNoteBuffer;

        float[] keyPressPos = new float[256];
        float[] keyPressVel = new float[256];

        double lastMidiTime = 0;

        public Render()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string ReadEmbed(string name)
            {
                using (var stream = assembly.GetManifestResourceStream(name))
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }

            var factory = new ObjLoaderFactory();
            var objLoader = factory.Create();
            var obj = objLoader.Load(new BufferedStream(File.OpenRead("E:/keys.obj")));

            Dictionary<string, ModelBuffer<KeyVert>> keyModels = new Dictionary<string, ModelBuffer<KeyVert>>();
            Dictionary<string, ModelBuffer<NoteVert>> noteModels = new Dictionary<string, ModelBuffer<NoteVert>>();

            Parallel.ForEach(obj.Groups, group =>
            {
                if (group.Name.StartsWith("p-"))
                    return;

                bool isNote = group.Name.StartsWith("note-");

                var verts = new List<BasicVert>();
                var indices = new List<int>();
                var bodyVerts = new List<BasicVert>();
                var bodyIndices = new List<int>();
                var capVerts = new List<BasicVert>();
                var capIndices = new List<int>();

                foreach (var f in group.Faces)
                {
                    if (f.Count != 3) throw new Exception("Non triangle faces not supported");
                    bool body = true;
                    if (isNote)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var v = f[i];
                            var norm = obj.Normals[v.NormalIndex - 1];
                            var dot = Vector3.Dot(new Vector3(norm.X, norm.Y, norm.Z), new Vector3(0, 0, 1));
                            if (Math.Abs(dot) > 0.1)
                            {
                                body = false;
                                break;
                            }
                        }
                        if (body) continue;
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        var v = f[i];
                        var vert = obj.Vertices[v.VertexIndex - 1];
                        var norm = obj.Normals[v.NormalIndex - 1];
                        verts.Add(new BasicVert(
                            new Vector3(vert.X, vert.Y, vert.Z),
                            new Vector3(norm.X, norm.Y, norm.Z)
                        ));
                        indices.Add(indices.Count);
                    }
                }

                try
                {
                    var zMin = verts.Select(v => v.Pos.Z).Min();
                    var zMax = verts.Select(v => v.Pos.Z).Max();
                    var zRange = zMax - zMin;
                    var zMiddle = (zMax + zMin) / 2;

                    var xMin = verts.Select(v => v.Pos.X).Min();
                    var xMax = verts.Select(v => v.Pos.X).Max();
                    var xRange = xMax - xMin;
                    var xMiddle = (xMax + xMin) / 2;

                    if (isNote)
                    {
                        Vector3 normalizeCorner(Vector3 pos)
                        {
                            return new Vector3(
                                    pos.X > 0 ? 1 : 0,
                                    pos.Y > -0.5 ? 1 : 0,
                                    pos.Z > 0 ? 1 : 0
                                );
                        }

                        var noteVerts = verts.Select(v => new NoteVert(v.Pos, v.Normal, (v.Pos.X - xMin) / xRange, normalizeCorner(v.Pos))).ToArray();
                        var shape = new ModelBuffer<NoteVert>(noteVerts, indices.ToArray());
                        lock (keyModels)
                            noteModels.Add(group.Name, shape);
                    }
                    else
                    {
                        var keyVerts = verts.Select(v => new KeyVert(v.Pos, v.Normal, (v.Pos.Z - zMin) / zRange)).ToArray();
                        var shape = new ModelBuffer<KeyVert>(keyVerts, indices.ToArray());
                        lock (keyModels)
                            keyModels.Add(group.Name, shape);
                    }
                }
                catch { }
            });

            ModelBuffer<KeyVert>[] getArray(string category, string side)
            {
                var keys = new ModelBuffer<KeyVert>[12];
                for (int i = 0; i < 12; i++)
                {
                    if (KeyboardState.IsBlackKey(i))
                        keys[i] = keyModels[$"black-{category}"];
                    else
                        keys[i] = keyModels[$"white-{category}-{side}-{i}"];
                }
                return keys;
            }

            this.keyModels = init.Add(new KeyModelData()
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
            });

            var resources = assembly.GetManifestResourceNames();

            keyShader = init.Add(new ShaderProgram<KeyShaderConstant>(
                ReadEmbed("MIDITrailRender.Shaders.keys.fx"),
                typeof(KeyVert),
                "4_0",
                "VS",
                "PS"
            ));


            //noteShader = new ShaderProgram(
            //    ReadEmbed("MIDITrailRender.Shaders.notes.hlsl"),
            //);

            plainShader = init.Add(Shaders.BasicTextured());
            colorspaceShader = init.Add(Shaders.Colorspace());
            colorCutoffShader = init.Add(Shaders.ColorCutoff());
            compositor = init.Add(new Compositor());

            addBlendState = init.Add(new BlendStateKeeper(BlendPreset.Add));

            //flatNoteBuffer = new InstancedModelBuffer<NoteVert, NoteInstance>(1024 * 64, noteModels["note-flat"]);
            //cubeNoteBuffer = new InstancedModelBuffer<NoteVert, NoteInstance>(1024 * 64, noteModels["note-cube"]);
            //roundedNoteBuffer = new InstancedModelBuffer<NoteVert, NoteInstance>(1024 * 64, noteModels["note-rounded"]);

            settings.Palette.PaletteChanged += ReloadTrackColors;
        }

        public void Init(Device device, MidiPlayback midi, RenderStatus status)
        {
            renderStatus = status;
            this.midi = midi;

            keyPressPos = new float[256];
            keyPressVel = new float[256];

            init.Init(device);

            init.Replace(ref depthSurface, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight, true));
            init.Replace(ref cutoffSurface, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight));
            init.Replace(ref preFinalSurface, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight));
            init.Replace(ref pingPongGlow, new PingPongGlow(status.RenderWidth, status.RenderHeight));

            //depthSurface = disposer.Add(new BasicRenderSurface(status.RenderWidth, status.RenderHeight, true));
            //compositeShader = disposer.Add(ShaderProgram.Presets.BasicTextured());
            //compositor = disposer.Add(new Compositor());

            //quadShader = disposer.Add(BasicShapeBuffer.GetBasicShader());
            //quadBuffer = disposer.Add(new BasicShapeBuffer(100, ShapePresets.Quads));

            //disposer.Add(flatNoteBuffer);
            //disposer.Add(cubeNoteBuffer);
            //disposer.Add(roundedNoteBuffer);

            ReloadTrackColors();

            lastMidiTime = midi.PlayerPositionSeconds;

            Initialized = true;
        }

        public void Dispose()
        {
            init.Dispose();
            Initialized = false;
        }

        public void RenderFrame(DeviceContext context, IRenderSurface renderSurface)
        {

            int firstKey = settings.keys.left;
            int lastKey = settings.keys.right;

            bool sameWidth = settings.sameWidthNotes;

            var keyboard = new KeyboardState(firstKey, lastKey, new KeyboardParams()
            {
                BlackKey2setOffset = 0.15 * 2,
                BlackKey3setOffset = 0.3 * 2,
                BlackKeyScale = 0.583,
                SameWidthNotes = sameWidth,
            });

            float frontNoteCutoff = 10;
            float backNoteCutoff = 0.3f;
            float noteScale = 5000;

            var time = midi.PlayerPosition;
            var frontMax = frontNoteCutoff;




            void renderNote(Note n)
            {
                if (n.Start < time && (n.End > time || !n.HasEnded))
                {
                    keyboard.PressKey(n.Key);
                    keyboard.BlendNote(n.Key, n.Color);
                }

                var key = keyboard.Notes[n.Key];

                var noteStart = (float)(n.Start - midi.PlayerPosition) / noteScale;
                var noteEnd = (float)(n.End - midi.PlayerPosition) / noteScale;

                if (!n.HasEnded || noteEnd > frontMax) noteEnd = frontMax;

                //if (useZFight)
                //{
                //    var stack = zNoteEnds[n.Key];
                //    while (stack.Count != 0 && stack.Peek() - 0.0001 < n.Start)
                //        stack.Pop();
                //    stack.Push(n.HasEnded ? n.End : double.PositiveInfinity);
                //    if (!sameWidth)
                //    {
                //        if (zNoteMaximums[n.Key] < stack.Count)
                //            zNoteMaximums[n.Key] = stack.Count;
                //    }
                //}

                //noteBuffer.PushInstance(new NoteInstance(
                //    (float)key.Left - 0.5f,
                //    (float)key.Right - 0.5f,
                //    noteStart,
                //    noteEnd,
                //    n.Color.Left,
                //    n.Color.Right,
                //    1,
                //    useZFight ? (zNoteEnds[n.Key].Count + zNoteExtra[n.Key]) * zFightStrength : 0
                //));
            }

            var iterator = midi.IterateNotes(midi.PlayerPosition - backNoteCutoff * noteScale, midi.PlayerPosition + frontNoteCutoff * noteScale);

            foreach (var n in iterator) renderNote(n);

            //if (sameWidth)
            //{
            //    foreach (var n in iterator) renderNote(n);
            //}
            //else
            //{
            //    foreach (var n in iterator)
            //    {
            //        if (!keyboard.BlackKey[n.Key]) renderNote(n);
            //    }

            //    for (int i = 0; i < 256; i++)
            //    {
            //        if (keyboard.BlackKey[i])
            //        {
            //            if (i != 0) zNoteExtra[i] = zNoteMaximums[i - 1];
            //            if (i != 255 && zNoteMaximums[i + 1] > zNoteExtra[i]) zNoteExtra[i] = zNoteMaximums[i + 1];
            //        }
            //    }

            //    foreach (var n in iterator)
            //    {
            //        if (keyboard.BlackKey[n.Key]) renderNote(n);
            //    }
            //}

            float timeScale = (float)(midi.PlayerPositionSeconds - lastMidiTime) * 60;
            for (int i = 0; i < 256; i++)
            {
                if (keyboard.Pressed[i])
                {
                    keyPressVel[i] += 0.1f * timeScale;
                }
                else
                {
                    keyPressVel[i] += -keyPressPos[i] / 1 * timeScale;
                }
                keyPressVel[i] *= (float)Math.Pow(0.5f, timeScale);
                keyPressPos[i] += keyPressVel[i] * timeScale;

                float maxPress = 1;
                if (keyPressPos[i] > maxPress)
                {
                    keyPressPos[i] = maxPress;
                    keyPressVel[i] = 0;
                }
            }

            keyShader.ConstData.View =
                Matrix.Translation(0, -0.2f, -0.7f) *
                Matrix.Scaling(1, 1, -1)
                ;
            keyShader.ConstData.ViewPos = keyShader.ConstData.View.TranslationVector;
            keyShader.ConstData.View *= Matrix.PerspectiveFovLH((float)Math.PI / 3, renderStatus.AspectRatio, 0.1f, 100f);

            //keyShader.ConstData.ViewNorm = keyShader.ConstData.View;
            //keyShader.ConstData.ModelNorm = keyShader.ConstData.Model;

            List<RenderObject> renderObjects = new List<RenderObject>();

            var keySet = sameWidth ? keyModels.SameWidth : keyModels.DifferentWidth;

            for (int i = firstKey; i < lastKey; i++)
            {
                var layoutKey = keyboard.Keys[i];

                var width = layoutKey.Right - layoutKey.Left;
                var middle = (layoutKey.Right + layoutKey.Left) / 2 - 0.5f;

                Matrix model = Matrix.Identity *
                Matrix.RotationX((float)Math.PI / 2 * keyPressPos[i] * 0.03f) *
                Matrix.Translation(0, 0, -1) *
                Matrix.Scaling((float)keyboard.BlackKeyWidth * 2 * 0.865f) *
                Matrix.Translation((float)middle, 0, 0) *
                Matrix.RotationY((float)Math.PI / 2 * 0) *
                Matrix.Translation(0, 0, 0);

                var isFirst = i == firstKey;
                var isLast = i == lastKey - 1;

                var keyPart = isFirst ? keySet.Right : isLast ? keySet.Left : keySet.Normal;

                renderObjects.Add(new KeyRenderObject(this, keyboard, keyPart.GetKey(i), model, i));
            }

            using (depthSurface.UseViewAndClear(context))
            {
                foreach (var m in renderObjects)
                {
                    m.Render(context);
                }
            }

            bool useGlow = true;

            ITextureResource lastSurface;
            if (useGlow)
            {
                compositor.Composite(context, depthSurface, colorCutoffShader, cutoffSurface);
                pingPongGlow.GlowSigma = 20;
                pingPongGlow.ApplyOn(context, cutoffSurface, 1, 1);
                compositor.Composite(context, depthSurface, colorspaceShader, preFinalSurface);
                using (addBlendState.UseOn(context))
                    compositor.Composite(context, cutoffSurface, plainShader, preFinalSurface, false);
                lastSurface = preFinalSurface;
            }
            else
            {
                lastSurface = depthSurface;
            }

            compositor.Composite(context, lastSurface, colorspaceShader, renderSurface);

            //using (new GLEnabler().Enable(EnableCap.Blend))
            //{

            //    using (new GLEnabler().Enable(EnableCap.DepthTest).Enable(EnableCap.CullFace))
            //    {
            //        GL.DepthFunc(DepthFunction.Less);

            //        depthSurface.BindSurfaceAndClear();
            //        GL.Clear(ClearBufferMask.DepthBufferBit);

            //        int firstKey = settings.keys.left;
            //        int lastKey = settings.keys.right;

            //        bool sameWidth = settings.sameWidthNotes;

            //        var keyboard = new KeyboardState(firstKey, lastKey, new KeyboardParams()
            //        {
            //            BlackKey2setOffset = 0.15 * 2,
            //            BlackKey3setOffset = 0.3 * 2,
            //            BlackKeyScale = 0.583,
            //            SameWidthNotes = sameWidth,
            //        });

            //        Matrix4 view = Matrix4.Identity *
            //            Matrix4.CreateTranslation(0, -0.1f, -0.3f);

            //        var viewPos = new Vector3(new Vector4(0, 0, 0, 1) * view);

            //        view *= Matrix4.CreatePerspectiveFieldOfView(MathHelper.Pi / 2, renderStatus.AspectRatio, 0.01f, 50);
            //        Matrix3 viewNorm = new Matrix3(view);

            //        void bindViewUniforms()
            //        {
            //            GL.UniformMatrix4(keyShader.Uniform("matView"), false, ref view);
            //            GL.UniformMatrix3(keyShader.Uniform("matViewNorm"), false, ref viewNorm);
            //            GL.Uniform3(keyShader.Uniform("viewPos"), viewPos);
            //        }

            //        void bindModelUniform(Matrix4 model)
            //        {
            //            var modelNorm = new Matrix3(model);
            //            GL.UniformMatrix4(keyShader.Uniform("matModel"), false, ref model);
            //            GL.UniformMatrix3(keyShader.Uniform("matModelNorm"), false, ref modelNorm);
            //        }

            //        keyShader.Bind();
            //        bindViewUniforms();

            //        noteShader.Bind();
            //        bindViewUniforms();

            //        GL.Uniform1(keyShader.Uniform("height"), keyboard.BlackKeyWidth);

            //        bindModelUniform(Matrix4.Identity);

            //        var keyType = keyModels.DifferentWidth;
            //        if (settings.sameWidthNotes) keyType = keyModels.SameWidth;
            //        var keyPart = keyType.Normal;

            //        List<RenderObject> renderObjects = new List<RenderObject>();

            //        var noteBuffer = roundedNoteBuffer;

            //        bool useZFight = true;
            //        float zFightStrength = 0.001f;

            //        Stack<double>[] zNoteEnds = null;
            //        int[] zNoteMaximums = null;
            //        int[] zNoteExtra = null;
            //        if (useZFight)
            //        {
            //            zNoteEnds = new Stack<double>[256];
            //            zNoteMaximums = new int[256];
            //            zNoteExtra = new int[256];
            //            for (int i = 0; i < 256; i++) zNoteEnds[i] = new Stack<double>();
            //        }

            //        void renderNote(Note n)
            //        {
            //            if (n.Start < time && (n.End > time || !n.HasEnded))
            //            {
            //                keyboard.PressKey(n.Key);
            //                keyboard.BlendNote(n.Key, n.Color);
            //            }

            //            var key = keyboard.Notes[n.Key];

            //            var noteStart = (float)(n.Start - midi.PlayerPosition) / noteScale;
            //            var noteEnd = (float)(n.End - midi.PlayerPosition) / noteScale;

            //            if (!n.HasEnded || noteEnd > frontMax) noteEnd = frontMax;

            //            if (useZFight)
            //            {
            //                var stack = zNoteEnds[n.Key];
            //                while (stack.Count != 0 && stack.Peek() - 0.0001 < n.Start)
            //                    stack.Pop();
            //                stack.Push(n.HasEnded ? n.End : double.PositiveInfinity);
            //                if (!sameWidth)
            //                {
            //                    if (zNoteMaximums[n.Key] < stack.Count)
            //                        zNoteMaximums[n.Key] = stack.Count;
            //                }
            //            }

            //            noteBuffer.PushInstance(new NoteInstance(
            //                (float)key.Left - 0.5f,
            //                (float)key.Right - 0.5f,
            //                noteStart,
            //                noteEnd,
            //                n.Color.Left,
            //                n.Color.Right,
            //                1,
            //                useZFight ? (zNoteEnds[n.Key].Count + zNoteExtra[n.Key]) * zFightStrength : 0
            //            ));
            //        }

            //        var iterator = midi.IterateNotes(midi.PlayerPosition - backNoteCutoff * noteScale, midi.PlayerPosition + frontNoteCutoff * noteScale);

            //        if (sameWidth)
            //        {
            //            foreach (var n in iterator) renderNote(n);
            //        }
            //        else
            //        {
            //            foreach (var n in iterator)
            //            {
            //                if(!keyboard.BlackKey[n.Key]) renderNote(n);
            //            }

            //            for (int i = 0; i < 256; i++) 
            //            {
            //                if (keyboard.BlackKey[i])
            //                {
            //                    if (i != 0) zNoteExtra[i] = zNoteMaximums[i - 1];
            //                    if (i != 255 && zNoteMaximums[i + 1] > zNoteExtra[i]) zNoteExtra[i] = zNoteMaximums[i + 1];
            //                }
            //            }

            //            foreach (var n in iterator)
            //            {
            //                if (keyboard.BlackKey[n.Key]) renderNote(n);
            //            }
            //        }

            //        foreach (var n in midi.IterateNotes(midi.PlayerPosition - backNoteCutoff * noteScale, midi.PlayerPosition + frontNoteCutoff * noteScale))
            //        {
            //        }

            //        noteBuffer.Flush();

            //        float timeScale = (float)(midi.PlayerPositionSeconds - lastMidiTime) * 60;

            //        for (int i = 0; i < 256; i++)
            //        {
            //            if (keyboard.Pressed[i])
            //            {
            //                keyPressVel[i] += 0.1f * timeScale;
            //            }
            //            else
            //            {
            //                keyPressVel[i] += -keyPressPos[i] / 1 * timeScale;
            //            }
            //            keyPressVel[i] *= (float)Math.Pow(0.5f, timeScale);
            //            keyPressPos[i] += keyPressVel[i] * timeScale;

            //            float maxPress = 1;
            //            if (keyPressPos[i] > maxPress)
            //            {
            //                keyPressPos[i] = maxPress;
            //                keyPressVel[i] = 0;
            //            }
            //        }

            //        for (int i = firstKey; i < lastKey; i++)
            //        {
            //            var layoutKey = keyboard.Keys[i];

            //            var width = layoutKey.Right - layoutKey.Left;
            //            var middle = (layoutKey.Right + layoutKey.Left) / 2 - 0.5f;

            //            Matrix4 model = Matrix4.Identity *
            //            Matrix4.CreateTranslation(0, 0, 1) *
            //            Matrix4.CreateRotationX(MathHelper.Pi / 2 * keyPressPos[i] * 0.03f) *
            //            Matrix4.CreateTranslation(0, 0, -1) *
            //            Matrix4.CreateScale((float)keyboard.BlackKeyWidth * 2 * 0.865f) *
            //            Matrix4.CreateTranslation((float)middle, 0, 0) *
            //            Matrix4.CreateRotationY(MathHelper.Pi / 2 * 0) *
            //            Matrix4.CreateTranslation(0, 0, 0);

            //            renderObjects.Add(new KeyRenderObject(keyPart.GetKey(i), model, i));
            //        }

            //        renderObjects.Sort((a, b) =>
            //        {
            //            return (b.Position - viewPos).Length.CompareTo((a.Position - viewPos).Length);
            //        });

            //        foreach (var obj in renderObjects)
            //        {
            //            if (obj is KeyRenderObject)
            //            {
            //                var key = obj as KeyRenderObject;
            //                keyShader.Bind();

            //                Vector4 col;

            //                if (keyboard.BlackKey[key.Key]) col = new Vector4(0, 0, 0, 1);
            //                else col = new Vector4(1, 1, 1, 1);

            //                GL.Uniform1(keyShader.Uniform("pressDepth"), keyPressPos[key.Key]);
            //                GL.Uniform4(keyShader.Uniform("mainColor"), col);
            //                GL.Uniform4(keyShader.Uniform("sideColor1"), keyboard.Colors[key.Key].Left);
            //                GL.Uniform4(keyShader.Uniform("sideColor2"), keyboard.Colors[key.Key].Right);
            //            }

            //            bindModelUniform(obj.Transform);

            //            obj.Model.DrawSingle();
            //        }
            //    }

            //    compositor.Composite(depthSurface, compositeShader, renderSurface);
            //}

            lastMidiTime = midi.PlayerPositionSeconds;
        }

        public void ReloadTrackColors()
        {
            var cols = settings.Palette.GetColors(midi.TrackCount);
            midi.ApplyColors(cols);
        }
    }
}
