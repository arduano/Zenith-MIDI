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
        public float Time;
        public FullColorData LeftColor;
        public FullColorData RightColor;
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 16)]
    public struct NoteShaderConstant
    {
        public Matrix Model;
        public Matrix View;
        public Vector3 ViewPos;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NoteInstance
    {
        [AssemblyElement("LEFT", Format.R32_Float)]
        public float Left;

        [AssemblyElement("RIGHT", Format.R32_Float)]
        public float Right;

        [AssemblyElement("START", Format.R32_Float)]
        public float Start;

        [AssemblyElement("END", Format.R32_Float)]
        public float End;

        [AssemblyElement("COLORLEFT", Format.R32G32B32A32_Float)]
        public Color4 ColorLeft;

        [AssemblyElement("COLORRIGHT", Format.R32G32B32A32_Float)]
        public Color4 ColorRight;

        [AssemblyElement("SCALE", Format.R32_Float)]
        public float Scale;

        [AssemblyElement("HEIGHT", Format.R32_Float)]
        public float Height;

        public NoteInstance(float left, float right, float start, float end, Color4 colorLeft, Color4 colorRight, float scale, float extraScale)
        {
            Left = left;
            Right = right;
            Start = start;
            End = end;
            ColorLeft = colorLeft;
            ColorRight = colorRight;
            Scale = scale;
            Height = extraScale;
        }
    }

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

    class NoteBufferParts : DeviceInitiable
    {
        public ShapeBuffer<NoteInstance> Body { get; set; }
        public ShapeBuffer<NoteInstance> Cap { get; set; }

        protected override void InitInternal()
        {
            Body.Init(Device);
            Cap.Init(Device);
        }

        protected override void DisposeInternal()
        {
            Body.Dispose();
            Cap.Dispose();
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
        public RenderObject(Matrix transform)
        {
            var a = Matrix.Identity * Matrix.Identity;
            Transform = transform;
            Position = transform.TranslationVector;
        }

        public Vector3 Position { get; }
        public Matrix Transform { get; }

        public abstract void Render(DeviceContext context);
    }

    public partial class Render : PureModule
    {
        #region Info
        public override string Name { get; } = "MIDITrail";
        public override string Description { get; } = "aaa";
        public override ImageSource PreviewImage { get; } = LoadPreviewBitmap(Properties.Resources.preview);
        public override string LanguageDictName { get; } = "miditrail";
        #endregion

        #region RenderObjects
        class KeyRenderObject : RenderObject
        {
            public int Key { get; }
            KeyboardState Keyboard { get; }
            public ModelBuffer<KeyVert> Model { get; set; }

            Render render;

            public KeyRenderObject(Render render, KeyboardState keyboard, ModelBuffer<KeyVert> model, Matrix transform, int key) : base(transform)
            {
                Key = key;
                Model = model;
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

                //keyShader.ConstData.LeftColor.Emit.Alpha = 0;
                //keyShader.ConstData.RightColor.Emit.Alpha = 0;

                keyShader.ConstData.LeftColor.Specular = new Color4(1, 1, 1, 1);
                keyShader.ConstData.RightColor.Specular = new Color4(1, 1, 1, 1);

                using (keyShader.UseOn(context))
                    Model.BindAndDraw(context);
            }
        }

        class NoteRenderObject : RenderObject
        {
            public int Key { get; }
            KeyboardState Keyboard { get; }
            NoteBufferParts Buffer { get; }
            IEnumerable<Note> Notes { get; }
            double NoteScale { get; }
            double FrontMax { get; }
            double MinLength { get; }

            Render render;

            public NoteRenderObject(
                Render render,
                KeyboardState keyboard, 
                NoteBufferParts buffer,
                Matrix transform,
                int key,
                IEnumerable<Note> notes,
                double noteScale,
                double frontMax,
                double minLength
            ) : base(transform)
            {
                Key = key;
                this.render = render;
                this.Keyboard = keyboard;
                Buffer = buffer;
                Notes = notes;
                NoteScale = noteScale;
                FrontMax = frontMax;
                MinLength = minLength;
            }

            public override void Render(DeviceContext context)
            {
                var noteShader = render.noteShader;
                noteShader.ConstData.Model = Transform;

                //keyShader.ConstData.LeftColor.Diffuse = Keyboard.Colors[Key].Left;
                //keyShader.ConstData.RightColor.Diffuse = Keyboard.Colors[Key].Right;

                //keyShader.ConstData.LeftColor.Emit.Alpha = 0;
                //keyShader.ConstData.RightColor.Emit.Alpha = 0;

                //keyShader.ConstData.LeftColor.Emit = Keyboard.Colors[Key].Left;
                //keyShader.ConstData.RightColor.Emit = Keyboard.Colors[Key].Right;

                //float glowStrength = Math.Max(render.keyPressPos[Key], 0);

                //keyShader.ConstData.LeftColor.Emit.Alpha = 2 * glowStrength;
                //keyShader.ConstData.RightColor.Emit.Alpha = 2 * glowStrength;

                ////keyShader.ConstData.LeftColor.Emit.Alpha = 0;
                ////keyShader.ConstData.RightColor.Emit.Alpha = 0;

                //keyShader.ConstData.LeftColor.Specular = new Color4(1, 1, 1, 1);
                //keyShader.ConstData.RightColor.Specular = new Color4(1, 1, 1, 1);

                var keyboard = Keyboard;
                var time = render.Midi.PlayerPosition;
                var minLength = (float)MinLength;

                void Loop(ShapeBuffer<NoteInstance> buffer)
                {
                    buffer.UseContext(context);
                    foreach (var n in Notes)
                    {
                        if (n.Start < time && (n.End > time || !n.HasEnded))
                        {
                            keyboard.PressKey(n.Key);
                            keyboard.BlendNote(n.Key, n.Color);
                        }

                        var notePos = keyboard.Notes[n.Key];
                        var keyPos = keyboard.Keys[n.Key];

                        var noteStart = (float)((n.Start - time) / NoteScale);
                        var noteEnd = (float)((n.End - time) / NoteScale);

                        var left = (float)notePos.Left - 0.5f;
                        var right = (float)notePos.Right - 0.5f;
                        var middle = (keyPos.Left + keyPos.Right) / 2;
                        left -= (float)middle - 0.5f;
                        right -= (float)middle - 0.5f;

                        if (!n.HasEnded || noteEnd > FrontMax) noteEnd = (float)FrontMax;

                        if (noteEnd - noteStart < minLength)
                        {
                            var noteMiddle = (noteStart + noteEnd) / 2;
                            noteStart = noteMiddle - minLength / 2;
                            noteEnd = noteMiddle + minLength / 2;
                        }

                        buffer.Push(new NoteInstance(
                            left,
                            right,
                            noteStart,
                            noteEnd,
                            n.Color.Left,
                            n.Color.Right,
                            (float)keyboard.BlackNoteWidth,
                            (float)keyboard.BlackNoteWidth
                        ));
                    }
                    buffer.Flush();
                }

                using (noteShader.UseOn(context))
                {
                    using (render.noDepthStencil.UseOn(context))
                        Loop(Buffer.Body);
                    using (render.depthStencil.UseOn(context))
                        Loop(Buffer.Cap);
                }
            }
        }
        #endregion

        UI settings = LoadUI(() => new UI());
        public override FrameworkElement SettingsControl => settings;

        public override double StartOffset => 0;

        protected override NoteColorPalettePick PalettePicker => settings.Palette;

        ShaderProgram<KeyShaderConstant> keyShader;
        ShaderProgram<NoteShaderConstant> noteShader;

        CompositeRenderSurface depthSurface;
        CompositeRenderSurface cutoffSurface;
        CompositeRenderSurface preFinalSurface;
        Compositor compositor;
        ShaderProgram plainShader;
        ShaderProgram colorspaceShader;
        ShaderProgram colorCutoffShader;

        BlendStateKeeper addBlendState;
        BlendStateKeeper pureBlendState;

        PingPongGlow pingPongGlow;

        ShaderProgram quadShader;
        ShaderProgram alphaAddFixShader;

        RasterizerStateKeeper rasterizer;

        DepthStencilStateKeeper depthStencil;
        DepthStencilStateKeeper noDepthStencil;

        KeyModelData keyModels;

        NoteBufferParts flatNoteBuffer;
        NoteBufferParts cubeNoteBuffer;
        NoteBufferParts roundedNoteBuffer;

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
            var obj = objLoader.Load(new BufferedStream(File.OpenRead("D:/keys.obj")));

            Dictionary<string, ModelBuffer<KeyVert>> keyModels = new Dictionary<string, ModelBuffer<KeyVert>>();
            Dictionary<string, ModelBuffer<NoteVert>> noteModelsBody = new Dictionary<string, ModelBuffer<NoteVert>>();
            Dictionary<string, ModelBuffer<NoteVert>> noteModelsCaps = new Dictionary<string, ModelBuffer<NoteVert>>();

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
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        var v = f[i];
                        var vert = obj.Vertices[v.VertexIndex - 1];
                        var norm = obj.Normals[v.NormalIndex - 1];
                        var vertItem = new BasicVert(
                            new Vector3(vert.X, vert.Y, vert.Z),
                            new Vector3(norm.X, norm.Y, norm.Z)
                        );
                        verts.Add(vertItem);
                        indices.Add(indices.Count);
                        if (isNote)
                        {
                            if (body)
                            {
                                bodyVerts.Add(vertItem);
                                bodyIndices.Add(bodyIndices.Count);
                            }
                            else
                            {
                                capVerts.Add(vertItem);
                                capIndices.Add(capIndices.Count);
                            }
                        }
                    }
                }

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

                    var noteBodyVerts = bodyVerts.Select(v => new NoteVert(v.Pos, v.Normal, (v.Pos.X - xMin) / xRange, normalizeCorner(v.Pos))).ToArray();
                    var noteCapVerts = capVerts.Select(v => new NoteVert(v.Pos, v.Normal, (v.Pos.X - xMin) / xRange, normalizeCorner(v.Pos))).ToArray();
                    var shapeBody = new ModelBuffer<NoteVert>(noteBodyVerts, bodyIndices.ToArray());
                    var shapeCap = new ModelBuffer<NoteVert>(noteCapVerts, capIndices.ToArray());
                    lock (noteModelsBody)
                        noteModelsBody.Add(group.Name, shapeBody);
                    lock (noteModelsCaps)
                        noteModelsCaps.Add(group.Name, shapeCap);
                }
                else
                {
                    var keyVerts = verts.Select(v => new KeyVert(v.Pos, v.Normal, (v.Pos.Z - zMin) / zRange)).ToArray();
                    var shape = new ModelBuffer<KeyVert>(keyVerts, indices.ToArray());
                    lock (keyModels)
                        keyModels.Add(group.Name, shape);
                }
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

            noteShader = init.Add(new ShaderProgram<NoteShaderConstant>(
                ReadEmbed("MIDITrailRender.Shaders.notes.fx"),
                typeof(NoteVert),
                typeof(NoteInstance),
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
            alphaAddFixShader = init.Add(Shaders.AlphaAddFix());
            compositor = init.Add(new Compositor());

            depthStencil = init.Add(new DepthStencilStateKeeper(DepthStencilPresets.Basic));
            noDepthStencil = init.Add(new DepthStencilStateKeeper(DepthStencilPresets.Always));

            rasterizer = init.Add(new RasterizerStateKeeper());
            rasterizer.Description.CullMode = CullMode.Front;

            addBlendState = init.Add(new BlendStateKeeper(BlendPreset.Add));
            pureBlendState = init.Add(new BlendStateKeeper(BlendPreset.PreserveColor));

            ShapeBuffer<NoteInstance> bufferFromModel(ModelBuffer<NoteVert> model)
            {
                return new ShapeBuffer<NoteInstance>(new InstancedBufferFlusher<NoteVert, NoteInstance>(1024 * 64, model));
            }

            flatNoteBuffer = new NoteBufferParts()
            {
                Body = init.Add(bufferFromModel(noteModelsBody["note-flat"])),
                Cap = null
            };

            cubeNoteBuffer = new NoteBufferParts()
            {
                Body = init.Add(bufferFromModel(noteModelsBody["note-cube"])),
                Cap = init.Add(bufferFromModel(noteModelsCaps["note-cube"])),
            };

            roundedNoteBuffer = new NoteBufferParts()
            {
                Body = init.Add(bufferFromModel(noteModelsBody["note-rounded"])),
                Cap = init.Add(bufferFromModel(noteModelsCaps["note-rounded"])),
            };

            settings.Palette.PaletteChanged += ReloadTrackColors;
        }

        public override void Init(Device device, MidiPlayback midi, RenderStatus status)
        {
            keyPressPos = new float[256];
            keyPressVel = new float[256];

            lastMidiTime = midi.PlayerPositionSeconds;

            init.Replace(ref depthSurface, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight, true));
            init.Replace(ref cutoffSurface, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight));
            init.Replace(ref preFinalSurface, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight));
            init.Replace(ref pingPongGlow, new PingPongGlow(status.RenderWidth, status.RenderHeight));

            base.Init(device, midi, status);
        }

        public override void RenderFrame(DeviceContext context, IRenderSurface renderSurface)
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
            float backNoteCutoff = 0.5f;
            float noteScale = 5000;

            var time = Midi.PlayerPosition;
            var frontMax = frontNoteCutoff;

            var view =
                Matrix.Translation(0, -0.2f, -0.7f) *
                Matrix.Scaling(1, 1, -1);
            var perspective = Matrix.PerspectiveFovLH((float)Math.PI / 3, Status.AspectRatio, 0.1f, 100f);
            var viewPos = view.TranslationVector;

            void sortObjectList(List<RenderObject> list)
            {
                list.Sort((a, b) =>
                {
                    return (b.Position - viewPos).Length().CompareTo((a.Position - viewPos).Length());
                });
            }

            var noteBuffer = roundedNoteBuffer.Body;

            noteShader.ConstData.View = view;
            noteShader.ConstData.ViewPos = keyShader.ConstData.View.TranslationVector;
            noteShader.ConstData.View *= perspective;
            noteShader.ConstData.Model = Matrix.Identity;

            using (depthSurface.UseViewAndClear(context))
            using (depthStencil.UseOn(context))
            using (rasterizer.UseOn(context))
            {
                keyShader.ConstData.View = view;
                keyShader.ConstData.ViewPos = keyShader.ConstData.View.TranslationVector;
                keyShader.ConstData.View *= perspective;
                keyShader.ConstData.Time = (float)Midi.PlayerPositionSeconds;

                //keyShader.ConstData.ViewNorm = keyShader.ConstData.View;
                //keyShader.ConstData.ModelNorm = keyShader.ConstData.Model;

                List<RenderObject> renderObjects = new List<RenderObject>();
                List<RenderObject> renderNotes = new List<RenderObject>();

                var keySet = sameWidth ? keyModels.SameWidth : keyModels.DifferentWidth;

                var iterators = Midi.IterateNotesKeyed(Midi.PlayerPosition - backNoteCutoff * noteScale, Midi.PlayerPosition + frontNoteCutoff * noteScale);
                for (int i = firstKey; i < lastKey; i++)
                {
                    var layoutKey = keyboard.Keys[i];

                    var width = layoutKey.Right - layoutKey.Left;
                    var middle = (layoutKey.Right + layoutKey.Left) / 2 - 0.5f;

                    var translation = Matrix.Translation((float)middle, 0, 0);

                    Matrix model = Matrix.Identity *
                    Matrix.RotationX((float)Math.PI / 2 * keyPressPos[i] * 0.03f) *
                    Matrix.Translation(0, 0, -1) *
                    Matrix.Scaling((float)keyboard.BlackKeyWidth * 2 * 0.865f) *
                    translation *
                    Matrix.RotationY((float)Math.PI / 2 * 0) *
                    Matrix.Translation(0, 0, 0);

                    var isFirst = i == firstKey;
                    var isLast = i == lastKey - 1;

                    var keyPart = isFirst ? keySet.Right : isLast ? keySet.Left : keySet.Normal;

                    var nro = new NoteRenderObject(
                        this,
                        keyboard,
                        roundedNoteBuffer,
                        translation,
                        0,
                        iterators[i],
                        noteScale,
                        frontMax,
                        keyboard.WhiteNoteWidth / 2
                    );
                    renderNotes.Add(nro);
                    renderObjects.Add(new KeyRenderObject(this, keyboard, keyPart.GetKey(i), model, i));
                }

                sortObjectList(renderNotes);
                if (sameWidth)
                {
                    foreach (var m in renderNotes)
                        m.Render(context);
                }
                else
                {
                    var notes = renderNotes.Cast<NoteRenderObject>();
                    foreach (var m in notes.Where(n => !keyboard.BlackKey[n.Key]))
                        m.Render(context);
                    foreach (var m in notes.Where(n => keyboard.BlackKey[n.Key]))
                        m.Render(context);
                }

                float timeScale = (float)(Midi.PlayerPositionSeconds - lastMidiTime) * 60;
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

                sortObjectList(renderObjects);
                foreach (var m in renderObjects)
                    m.Render(context);
            }

            bool useGlow = true;

            ITextureResource lastSurface;
            if (useGlow)
            {
                compositor.Composite(context, depthSurface, colorCutoffShader, cutoffSurface);
                pingPongGlow.GlowSigma = 20;
                pingPongGlow.ApplyOn(context, cutoffSurface, 3, 1);
                compositor.Composite(context, depthSurface, colorspaceShader, preFinalSurface);
                using (addBlendState.UseOn(context))
                    compositor.Composite(context, cutoffSurface, plainShader, preFinalSurface, false);
                lastSurface = preFinalSurface;
            }
            else
            {
                lastSurface = depthSurface;
            }

            using (pureBlendState.UseOn(context))
                compositor.Composite(context, lastSurface, colorspaceShader, renderSurface);

            lastMidiTime = Midi.PlayerPositionSeconds;
        }
    }
}
