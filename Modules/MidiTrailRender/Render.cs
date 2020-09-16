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
using MIDITrailRender.Views;
using MIDITrailRender.Logic;
using MIDITrailRender.Models;

namespace MIDITrailRender
{
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

                keyShader.ConstData.LeftColor.Emit.Alpha = 20 * glowStrength;
                keyShader.ConstData.RightColor.Emit.Alpha = 20 * glowStrength;

                //keyShader.ConstData.LeftColor.Emit.Alpha = 0;
                //keyShader.ConstData.RightColor.Emit.Alpha = 0;

                keyShader.ConstData.LeftColor.Specular = new Color4(1, 1, 1, 1);
                keyShader.ConstData.RightColor.Specular = new Color4(1, 1, 1, 1);

                using (keyShader.UseOn(context))
                    Model.BindAndDraw(context);
            }
        }
        #endregion

        MainView settingsView = LoadUI(() => new MainView());
        public override FrameworkElement SettingsControl => settingsView;

        public override double StartOffset => 0;
        
        protected override NoteColorPalettePick PalettePicker => null;

        ShaderProgram<KeyShaderConstant> keyShader;

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

        DepthStencilStateKeeper depthStencil;

        RasterizerStateKeeper rasterizer;

        FullModelData allModels;

        NoteRenderer noteRenderer;

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

            allModels = init.Add(ModelLoader.LoadAllModels());
            noteRenderer = init.Add(new NoteRenderer(allModels));

            var resources = assembly.GetManifestResourceNames();

            keyShader = init.Add(new ShaderProgram<KeyShaderConstant>(
                ReadEmbed("MIDITrailRender.Shaders.keys.fx"),
                typeof(KeyVert),
                "4_0",
                "VS",
                "PS"
            ));

            plainShader = init.Add(Shaders.BasicTextured());
            colorspaceShader = init.Add(Shaders.Colorspace());
            colorCutoffShader = init.Add(Shaders.ColorCutoff());
            alphaAddFixShader = init.Add(Shaders.AlphaAddFix());
            compositor = init.Add(new Compositor());

            depthStencil = init.Add(new DepthStencilStateKeeper(DepthStencilPresets.Basic));

            rasterizer = init.Add(new RasterizerStateKeeper());
            rasterizer.Description.CullMode = CullMode.Front;

            addBlendState = init.Add(new BlendStateKeeper(BlendPreset.Add));
            pureBlendState = init.Add(new BlendStateKeeper(BlendPreset.PreserveColor));

            ShapeBuffer<NoteInstance> bufferFromModel(ModelBuffer<NoteVert> model)
            {
                return new ShapeBuffer<NoteInstance>(new InstancedBufferFlusher<NoteVert, NoteInstance>(1024 * 64, model));
            }

            //settings.Palette.PaletteChanged += ReloadTrackColors;
        }

        public override void Init(DeviceGroup device, MidiPlayback midi, RenderStatus status)
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
            var settings = settingsView.Data;
            var camera = settings.Camera;

            int firstKey = 0;//settings.keys.left;
            int lastKey = 128;//settings.keys.right;

            bool sameWidth = true;//settings.sameWidthNotes;

            var time = Midi.PlayerPosition;

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

            var view =
                Matrix.Translation((float)camera.CamX, (float)camera.CamY, (float)camera.CamZ) *
                Matrix.RotationY((float)(camera.CamRotY / 180 * Math.PI)) *
                Matrix.RotationX((float)(camera.CamRotX / 180 * Math.PI)) *
                Matrix.RotationZ((float)(camera.CamRotZ / 180 * Math.PI)) *
                Matrix.Scaling(1, 1, -1);
            var perspective = Matrix.PerspectiveFovLH((float)(camera.CamFOV / 180 * Math.PI), Status.AspectRatio, 0.1f, 100f);
            var viewPos = view.TranslationVector;

            var cameraData = new Camera(settings, Status);

            void sortObjectList(List<RenderObject> list)
            {
                list.Sort((a, b) =>
                {
                    return (b.Position - viewPos).Length().CompareTo((a.Position - viewPos).Length());
                });
            }

            var noteBuffer = allModels.Notes.Rounded.Body;

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

                var keySet = sameWidth ? allModels.Keys.SameWidth : allModels.Keys.DifferentWidth;

                var frontMax = frontNoteCutoff;
                var backMax = -backNoteCutoff;

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

                    renderObjects.Add(new KeyRenderObject(this, keyboard, keyPart.GetKey(i), model, i));
                }

                noteRenderer.RenderNotes(context, settings, Midi, cameraData, keyboard);

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
