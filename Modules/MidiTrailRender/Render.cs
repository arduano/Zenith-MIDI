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
    public partial class Render : PureModule
    {
        #region Info
        public override string Name { get; } = "MIDITrail";
        public override string Description { get; } = "aaa";
        public override ImageSource PreviewImage { get; } = LoadPreviewBitmap(Properties.Resources.preview);
        public override string LanguageDictName { get; } = "miditrail";
        #endregion


        MainView settingsView = LoadUI(() => new MainView());
        public override FrameworkElement SettingsControl => settingsView;

        public override double StartOffset => 0;
        
        protected override NoteColorPalettePick PalettePicker => settingsView.Data.General.PalettePicker;

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
        KeyboardPhysics keyboardPhysics;
        KeyboardHandler keyboardHandler;

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
            keyboardHandler = init.Add(new KeyboardHandler(allModels));

            var resources = assembly.GetManifestResourceNames();

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

            settingsView.Data.General.PalettePicker.PaletteChanged += ReloadTrackColors;
        }

        public override void Init(DeviceGroup device, MidiPlayback midi, RenderStatus status)
        {
            lastMidiTime = midi.PlayerPositionSeconds;

            init.Replace(ref depthSurface, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight, true));
            init.Replace(ref cutoffSurface, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight));
            init.Replace(ref preFinalSurface, new CompositeRenderSurface(status.RenderWidth, status.RenderHeight));
            init.Replace(ref pingPongGlow, new PingPongGlow(status.RenderWidth, status.RenderHeight));

            keyboardPhysics = new KeyboardPhysics();

            base.Init(device, midi, status);
        }

        public override void RenderFrame(DeviceContext context, IRenderSurface renderSurface)
        {
            var settings = settingsView.Data;
            var genSettings = settings.General;

            var keyboard = new KeyboardState(genSettings.FirstKey, genSettings.LastKey, new KeyboardParams()
            {
                BlackKey2setOffset = 0.15 * 2,
                BlackKey3setOffset = 0.3 * 2,
                BlackKeyScale = 0.583,
                SameWidthNotes = genSettings.SameWidthNotes,
            });

            var camera = new Camera(settings, Status, Midi.PlayerPositionSeconds);

            using (depthSurface.UseViewAndClear(context))
            using (depthStencil.UseOn(context))
            using (rasterizer.UseOn(context))
            {                
                noteRenderer.RenderNotes(context, settings, Midi, camera, keyboard);
                keyboardPhysics.UpdateFrom(keyboard, Midi.PlayerPositionSeconds);
                var keys = keyboardHandler.GetKeyObjects(settings, keyboard, keyboardPhysics);
                camera.RenderOrdered(keys, context);
            }

            bool useGlow = true;

            ITextureResource lastSurface;
            if (useGlow)
            {
                var glowConfig = settings.Glow;
                compositor.Composite(context, depthSurface, colorCutoffShader, cutoffSurface);
                pingPongGlow.GlowSigma = (float)glowConfig.GlowSigma;
                pingPongGlow.ApplyOn(context, cutoffSurface, (float)glowConfig.GlowStrength, (float)glowConfig.GlowBrightness);
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
