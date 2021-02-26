using OpenTK.Graphics.ES11;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZenithEngine;
using ZenithEngine.DXHelper;
using ZenithEngine.DXHelper.Presets;
using ZenithEngine.IO;
using ZenithEngine.Modules;
using ZenithEngine.ModuleUI;
using ZenithEngine.ModuleUtil;

namespace TexturedRender
{
    struct LoadedNoteLocations
    {
        public LoadedNoteLocations(int middle, int top, int bottom)
        {
            Middle = middle;
            Top = top;
            Bottom = bottom;
        }

        public int Middle { get; }
        public int Top { get; }
        public int Bottom { get; }
    }

    public class Render : PureModule
    {
        public override string Name => "Textured";

        public override string Description => "Plugin for loading and rendering custom resource packs, " +
            "with settings defined in a .json file";

        public override ImageSource PreviewImage { get; } = LoadPreviewBitmap(Properties.Resources.preview);

        SettingsCtrl settings = LoadUI(() => new SettingsCtrl());
        public override ISerializableContainer SettingsControl => settings;
        protected override NoteColorPalettePick PalettePicker => settings.Data.PalettePicker;
        public override double StartOffset => settings.Data.View.NoteScreenTime;

        ShapeBuffer<VertMultiTex2D> buffer;

        ShaderProgram darkenShader;
        ShaderProgram lightenShader;
        ShaderProgram hybridShader;

        TextureSampler sampler;

        ThreadedKeysLoop<VertMultiTex2D> multithread;

        LoadedPack loadedPack;

        public Render()
        {
            buffer = init.Add(new ShapeBuffer<VertMultiTex2D>(1024));
            darkenShader = init.Add(Shaders.MultiTexture(10, TextureShaderPreset.Darken));
            lightenShader = init.Add(Shaders.MultiTexture(10, TextureShaderPreset.Lighten));
            hybridShader = init.Add(Shaders.MultiTexture(10, TextureShaderPreset.Hybrid));

            sampler = init.Add(new TextureSampler(SamplerPresets.Wrap));
            sampler.Description.Filter = Filter.MinMagMipPoint;

            multithread = init.Add(new ThreadedKeysLoop<VertMultiTex2D>(1 << 12));

            PalettePicker.PaletteChanged += ReloadTrackColors;
        }

        (ShaderResourceView[], LoadedNoteLocations[]) GenerateNoteLocations(IEnumerable<NoteSchema> notes)
        {
            List<ShaderResourceView> noteResources = new List<ShaderResourceView>();
            List<LoadedNoteLocations> noteLocations = new List<LoadedNoteLocations>();
            int texid = 0;
            foreach (var n in notes)
            {
                int middle = texid++;
                noteResources.Add(n.middleTexture);
                int top = -1;
                int bottom = -1;
                if (n.useEndCaps)
                {
                    top = texid++;
                    noteResources.Add(n.topTexture);
                    bottom = texid++;
                    noteResources.Add(n.bottomTexture);
                }
                noteLocations.Add(new LoadedNoteLocations(middle, top, bottom));
            }
            return (noteResources.ToArray(), noteLocations.ToArray());
        }

        void CheckLoadedPack()
        {
            if (loadedPack != settings.Data.LoadedPack)
                init.Replace(ref loadedPack, settings.Data.LoadedPack);
        }

        protected override void InitInternal()
        {
            CheckLoadedPack();
            base.InitInternal();
        }

        public override void RenderFrame(DeviceContext context, IRenderSurface renderSurface)
        {
            buffer.UseContext(context);

            var settings = this.settings.Data;
            CheckLoadedPack();

            if (loadedPack == null || loadedPack.Pack == null) return;

            using (sampler.UseOnPS(context))
            {
                var pack = loadedPack.Pack;
                var view = settings.View;

                var keyboard = new KeyboardState(view.FirstKey, view.LastKey, new KeyboardParams()
                {
                    AdvancedBlackKeyOffsets = pack.advancedBlackKeyOffsets,
                    BlackKeyColor = Color4.Black,

                    SameWidthNotes = pack.sameWidthNotes,

                    BlackKey2setOffset = pack.blackKey2setOffset,
                    BlackKey3setOffset = pack.blackKey3setOffset,
                    BlackKeyScale = pack.blackKeyScale,

                    BlackNote2setOffset = pack.blackNote2setOffset,
                    BlackNote3setOffset = pack.blackNote3setOffset,
                    BlackNoteScale = pack.blackNoteScale,
                });

                float keyboardHeightFull = (float)(
                    pack.keyboardHeight / 100 /
                    (keyboard.LastNote - keyboard.FirstNote) *
                    128 / (1920.0 / 1080.0) * Status.AspectRatio
                );
                float keyboardHeight = keyboardHeightFull;
                float barHeight = keyboardHeightFull * (float)pack.barHeight / 100;
                if (pack.UseBar) keyboardHeight -= barHeight;

                double noteScreenTime = view.NoteScreenTime;

                double notePosFactor = 1 / noteScreenTime * (1 - keyboardHeightFull);

                double whiteKeyWidth = keyboard.WhiteKeyWidth;

                double topExtraTime = pack.notes
                    .Select(n => whiteKeyWidth / (n.topTexture?.AspectRatio ?? 9999))
                    .Max();
                topExtraTime /= notePosFactor;

                double bottomExtraTime = pack.notes
                    .Select(n => whiteKeyWidth / (n.bottomTexture?.AspectRatio ?? 9999))
                    .Max();
                bottomExtraTime /= notePosFactor;

                double midiTime = Midi.PlayerPosition;

                Midi.CheckParseDistance(noteScreenTime + topExtraTime);


                var noteTypesSorted = pack.notes
                    .OrderBy(n => n.keyType == NoteType.Both ? 1 : 0)
                    .ToArray();
                var (noteResources, noteLocations) = GenerateNoteLocations(noteTypesSorted);

                var shaderDispose = new DisposeGroup();
                var shader = dispose.Add(darkenShader.UseOn(context));
                var shaderType = NoteShaderType.Normal;

                void useShader(NoteShaderType type)
                {
                    if (shaderType == type) return;
                    ShaderProgram newShader = null;
                    if (type == NoteShaderType.Normal)
                        newShader = darkenShader;
                    if (type == NoteShaderType.Inverted)
                        newShader = lightenShader;
                    if (type == NoteShaderType.Hybrid)
                        newShader = hybridShader;
                    dispose.Remove(shader);
                    shader = dispose.Add(newShader.UseOn(context));
                    shaderType = type;
                }


                void pushQuad(Vector2 tl, Vector2 br, Vector2 uvtl, Vector2 uvbr, Color4 colTop, Color4 colBottom, int texid)
                {
                    buffer.Push(new VertMultiTex2D(new Vector2(tl.X, tl.Y), new Vector2(uvtl.X, uvtl.Y), colTop, texid));
                    buffer.Push(new VertMultiTex2D(new Vector2(br.X, tl.Y), new Vector2(uvbr.X, uvtl.Y), colTop, texid));
                    buffer.Push(new VertMultiTex2D(new Vector2(br.X, br.Y), new Vector2(uvbr.X, uvbr.Y), colBottom, texid));
                    buffer.Push(new VertMultiTex2D(new Vector2(tl.X, br.Y), new Vector2(uvtl.X, uvbr.Y), colBottom, texid));
                }

                double renderCutoff = midiTime + noteScreenTime;

                var noteStreams = Midi.IterateNotesKeyed(midiTime - bottomExtraTime, renderCutoff + topExtraTime);

                #region Notes
                useShader(pack.noteShader);
                context.PixelShader.SetShaderResources(0, noteResources);
                multithread.Render(context, keyboard.FirstNote, keyboard.LastNote, !pack.sameWidthNotes, (k, push) =>
                {
                    void pushNoteQuad(Vector2 tl, Vector2 br, Vector2 uvtl, Vector2 uvbr, Color4 colLeft, Color4 colRight, int texid)
                    {
                        push(new VertMultiTex2D(new Vector2(tl.X, tl.Y), new Vector2(uvtl.X, uvtl.Y), colLeft, texid));
                        push(new VertMultiTex2D(new Vector2(br.X, tl.Y), new Vector2(uvbr.X, uvtl.Y), colRight, texid));
                        push(new VertMultiTex2D(new Vector2(br.X, br.Y), new Vector2(uvbr.X, uvbr.Y), colRight, texid));
                        push(new VertMultiTex2D(new Vector2(tl.X, br.Y), new Vector2(uvtl.X, uvbr.Y), colLeft, texid));
                    }

                    var key = keyboard.Notes[k];
                    var stream = noteStreams[k];

                    float left = keyboard.Notes[k].Left;
                    float right = keyboard.Notes[k].Right;
                    float width = right - left;
                    float viewAspect = (float)Status.AspectRatio;
                    var minBottom = keyboardHeight - 0.1f;
                    var minTop = keyboardHeight - 0.1f;

                    foreach (var n in stream)
                    {
                        bool pressed = false;
                        if (n.Start < midiTime && (n.End > midiTime || !n.HasEnded))
                        {
                            keyboard.PressKey(k);
                            keyboard.BlendNote(k, n.Color);
                            pressed = true;
                        }

                        float top = (float)(1 - (renderCutoff - n.End) * notePosFactor);
                        float bottom = (float)(1 - (renderCutoff - n.Start) * notePosFactor);
                        float topCap = 0, bottomCap = 0;

                        if (!n.HasEnded) top = 1.1f;

                        double texSize = (top - bottom) / width / viewAspect;
                        int tex = 0;
                        foreach (var t in noteTypesSorted)
                        {
                            if (t.keyType != NoteType.Both)
                            {
                                if (key.IsBlack != (t.keyType == NoteType.Black))
                                {
                                    tex++;
                                    continue;
                                }
                            }
                            break;
                        }
                        if (tex >= noteLocations.Length)
                        { }
                        var texLocations = noteLocations[tex];
                        var noteOptions = noteTypesSorted[tex];

                        Color4 leftCol = n.Color.Left;
                        Color4 rightCol = n.Color.Right;

                        if (key.IsBlack)
                        {
                            Color4 darken = new Color4(0, 0, 0, 1f - noteOptions.darkenBlackNotes);

                            var oldLeftAlpha = leftCol.Alpha;
                            var oldRightAlpha = rightCol.Alpha;

                            leftCol = leftCol.BlendWith(darken);
                            rightCol = rightCol.BlendWith(darken);

                            leftCol.Alpha = oldLeftAlpha;
                            rightCol.Alpha = oldRightAlpha;
                        }

                        if (pressed)
                        {
                            Color4 lighten = new Color4(
                                noteOptions.highlightHitNotesColor[0] / 255f,
                                noteOptions.highlightHitNotesColor[1] / 255f,
                                noteOptions.highlightHitNotesColor[2] / 255f,
                                noteOptions.highlightHitNotes);
                            leftCol = leftCol.BlendWith(lighten);
                            rightCol = rightCol.BlendWith(lighten);
                        }

                        float topHeight;
                        float bottomHeight;
                        if (noteOptions.useEndCaps)
                        {
                            topHeight = width / (float)noteOptions.topTexture.AspectRatio * viewAspect;
                            bottomHeight = width / (float)noteOptions.bottomTexture.AspectRatio * viewAspect;
                            topCap = top + topHeight * (float)noteOptions.noteTopOversize;
                            bottomCap = bottom - bottomHeight * (float)noteOptions.noteBottomOversize;
                            if (n.HasEnded)
                                top -= topHeight * (1 - (float)noteOptions.noteTopOversize);
                            bottom += bottomHeight * (1 - (float)noteOptions.noteBottomOversize);
                            if (bottom > top)
                            {
                                float middle = (top + bottom) / 2;
                                top = middle;
                                bottom = middle;
                                if (!noteOptions.squeezeEndCaps)
                                {
                                    topCap = top + topHeight;
                                    bottomCap = bottom - bottomHeight;
                                }
                            }
                            texSize = (bottom - top) / width / viewAspect;

                            pushNoteQuad(
                                new Vector2(left, topCap),
                                new Vector2(right, top),
                                new Vector2(0, 0),
                                new Vector2(1, 1),
                                leftCol,
                                rightCol,
                                texLocations.Top
                            );
                            pushNoteQuad(
                                new Vector2(left, bottom),
                                new Vector2(right, bottomCap),
                                new Vector2(0, 0),
                                new Vector2(1, 1),
                                leftCol,
                                rightCol,
                                texLocations.Bottom
                            );
                        }
                        pushNoteQuad(
                            new Vector2(left, top),
                            new Vector2(right, bottom),
                            new Vector2(0, 0),
                            new Vector2(1, 1),
                            leftCol,
                            rightCol,
                            texLocations.Middle
                        );
                    }
                });
                #endregion

                #region Overlays
                void renderOverlays(bool above)
                {
                    float getLeft(int key)
                    {
                        int k = key % 12;
                        if (k < 0) k += 12;
                        int o = (key - k) / 12;

                        return keyboard.Keys[k].Left + (keyboard.Keys[12].Left - keyboard.Keys[0].Left) * o;
                    }

                    float getWidth(int key)
                    {
                        int k = key % 12;
                        if (k < 0) k += 12;
                        if (KeyboardState.IsBlackKey(k)) return keyboard.BlackKeyWidth;
                        else return keyboard.WhiteKeyWidth;
                    }

                    useShader(NoteShaderType.Normal);
                    foreach (var o in pack.overlays.Where(o => o.belowKeyboard != above))
                    {
                        double start = getLeft(o.firstKey);
                        double end = getLeft(o.lastKey) + getWidth(o.lastKey);
                        double height = Math.Abs(start - end) / o.texture.AspectRatio * Status.AspectRatio;

                        using (o.texture.UseOnPS(context))
                        {
                            pushQuad(
                                new Vector2((float)start, (float)height),
                                new Vector2((float)end, 0),
                                new Vector2(0, 0),
                                new Vector2(1, 1),
                                Color4.White,
                                Color4.White,
                                0
                            );
                            buffer.Flush();
                        }
                    }
                }
                #endregion

                renderOverlays(false);

                #region Keyboard
                ShaderResourceView[] keyResources;
                if (pack.UseSideKeys)
                {
                    keyResources = new ShaderResourceView[] {
                    pack.whiteKey,
                    pack.whiteKeyPressed,
                    pack.blackKey,
                    pack.blackKeyPressed,
                    pack.whiteKeyLeft,
                    pack.whiteKeyLeftPressed,
                    pack.whiteKeyRight,
                    pack.whiteKeyRightPressed,
                };
                }
                else
                {
                    keyResources = new ShaderResourceView[] {
                        pack.whiteKey,
                        pack.whiteKeyPressed,
                        pack.blackKey,
                        pack.blackKeyPressed
                    };
                }

                if (pack.UseBar)
                {
                    context.PixelShader.SetShaderResource(0, pack.bar);
                    pushQuad(
                        new Vector2(0, keyboardHeightFull),
                        new Vector2(1, keyboardHeight),
                        new Vector2(0, 0),
                        new Vector2(1, 1),
                        Color4.White,
                        Color4.White,
                        0
                    );
                    buffer.Flush();
                }


                context.PixelShader.SetShaderResources(0, keyResources);

                useShader(pack.whiteKeyShader);
                foreach (var k in keyboard.IterateWhiteKeys())
                {
                    var top = keyboardHeight;
                    if (k.Pressed) top += keyboardHeightFull * (float)pack.whiteKeyPressedOversize / 100;
                    else top += keyboardHeightFull * (float)pack.whiteKeyOversize / 100;
                    var uvleft = 0f;
                    var uvright = 1f;
                    if (pack.whiteKeysFullOctave)
                    {
                        var num = keyboard.KeyNumber[k.Key] % 7;
                        uvleft = num * 1.0f / 7;
                        uvright = (num + 1) * 1.0f / 7;
                    }

                    int texid = 0;
                    if (k.Pressed) texid++;

                    if (pack.UseSideKeys)
                    {
                        if (k.Key == keyboard.FirstKey) texid += 4;
                        if (k.Key == keyboard.LastKey - 1) texid += 6;
                    }

                    pushQuad(
                        new Vector2(k.Left, top),
                        new Vector2(k.Right, 0),
                        new Vector2(uvleft, 0),
                        new Vector2(uvright, 1),
                        k.Color.Left,
                        k.Color.Right,
                        texid
                    );
                }
                buffer.Flush();

                useShader(pack.blackKeyShader);
                foreach (var k in keyboard.IterateBlackKeys())
                {
                    var top = keyboardHeight;
                    var bottom = keyboardHeight * (float)pack.blackKeyHeight / 100;
                    if (k.Pressed) top += keyboardHeightFull * (float)pack.blackKeyPressedOversize / 100;
                    else top += keyboardHeightFull * (float)pack.blackKeyOversize / 100;
                    var uvleft = 0f;
                    var uvright = 1f;
                    if (pack.blackKeysFullOctave)
                    {
                        var num = keyboard.KeyNumber[k.Key] % 5;
                        uvleft = num * 1.0f / 5;
                        uvright = (num + 1) * 1.0f / 5;
                    }
                    Color4 leftCol = k.Color.Left;
                    Color4 rightCol = k.Color.Right;
                    if (!k.Pressed && pack.blackKeysWhiteShade)
                    {
                        leftCol = Color4.White;
                        rightCol = Color4.White;
                    }

                    int texid = 2;
                    if (k.Pressed) texid++;
                    pushQuad(
                        new Vector2(k.Left, top),
                        new Vector2(k.Right, bottom),
                        new Vector2(uvleft, 0),
                        new Vector2(uvright, 1),
                        leftCol,
                        rightCol,
                        texid
                    );
                }
                buffer.Flush();
                #endregion

                renderOverlays(true);
            }
        }
    }
}
