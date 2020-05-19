using ZenithEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.IO;

namespace TexturedRender
{
    public class Render : IPluginRender
    {
        public string Name => "Textured";

        public string Description => "Plugin for loading and rendering custom resource packs, " +
            "with settings defined in a .json file";

        public string LanguageDictName { get; } = "textured";

        #region Shaders
        string quadShaderVert = @"#version 330 core

layout(location=0) in vec2 in_position;
layout(location=1) in vec4 in_color;
layout(location=2) in vec2 in_uv;
layout(location=3) in float in_texid;

out vec4 v2f_color;
out vec2 uv;
out float texid;

void main()
{
    gl_Position = vec4(in_position.x * 2 - 1, in_position.y * 2 - 1, 1.0f, 1.0f);
    v2f_color = in_color;
    uv = in_uv;
    texid = in_texid;
}
";
        string quadShaderFrag = @"#version 330 core

in vec4 v2f_color;
in vec2 uv;
in float texid;

uniform sampler2D textureSampler1;
uniform sampler2D textureSampler2;
uniform sampler2D textureSampler3;
uniform sampler2D textureSampler4;
uniform sampler2D textureSampler5;
uniform sampler2D textureSampler6;
uniform sampler2D textureSampler7;
uniform sampler2D textureSampler8;
uniform sampler2D textureSampler9;
uniform sampler2D textureSampler10;
uniform sampler2D textureSampler11;
uniform sampler2D textureSampler12;

out vec4 out_color;

void main()
{
    vec4 col;
    if(texid < 0.5) col = texture2D( textureSampler1, uv );
    else if(texid < 1.5) col = texture2D( textureSampler2, uv );
    else if(texid < 2.5) col = texture2D( textureSampler3, uv );
    else if(texid < 3.5) col = texture2D( textureSampler4, uv );
    else if(texid < 4.5) col = texture2D( textureSampler5, uv );
    else if(texid < 5.5) col = texture2D( textureSampler6, uv );
    else if(texid < 6.5) col = texture2D( textureSampler7, uv );
    else if(texid < 7.5) col = texture2D( textureSampler8, uv );
    else if(texid < 8.5) col = texture2D( textureSampler9, uv );
    else if(texid < 9.5) col = texture2D( textureSampler10, uv );
    else if(texid < 10.5) col = texture2D( textureSampler11, uv );
    else if(texid < 11.5) col = texture2D( textureSampler12, uv );
    out_color = col * v2f_color;
}
";
        string invertQuadShaderFrag = @"#version 330 core

in vec4 v2f_color;
in vec2 uv;
in float texid;

uniform sampler2D textureSampler1;
uniform sampler2D textureSampler2;
uniform sampler2D textureSampler3;
uniform sampler2D textureSampler4;
uniform sampler2D textureSampler5;
uniform sampler2D textureSampler6;
uniform sampler2D textureSampler7;
uniform sampler2D textureSampler8;
uniform sampler2D textureSampler9;
uniform sampler2D textureSampler10;
uniform sampler2D textureSampler11;
uniform sampler2D textureSampler12;

out vec4 out_color;

void main()
{
    vec4 col;
    if(texid < 0.5) col = texture2D( textureSampler1, uv );
    else if(texid < 1.5) col = texture2D( textureSampler2, uv );
    else if(texid < 2.5) col = texture2D( textureSampler3, uv );
    else if(texid < 3.5) col = texture2D( textureSampler4, uv );
    else if(texid < 4.5) col = texture2D( textureSampler5, uv );
    else if(texid < 5.5) col = texture2D( textureSampler6, uv );
    else if(texid < 6.5) col = texture2D( textureSampler7, uv );
    else if(texid < 7.5) col = texture2D( textureSampler8, uv );
    else if(texid < 8.5) col = texture2D( textureSampler9, uv );
    else if(texid < 9.5) col = texture2D( textureSampler10, uv );
    else if(texid < 10.5) col = texture2D( textureSampler11, uv );
    else if(texid < 11.5) col = texture2D( textureSampler12, uv );
    col = 1 - col;
    col.w = 1 - col.w;
    vec4 col2 = 1 - v2f_color;
    col2.w = 1 - col2.w;
    out_color = 1 - col * col2;
    out_color.w = 1 - out_color.w;
}
";
        string evenQuadShaderFrag = @"#version 330 core

in vec4 v2f_color;
in vec2 uv;
in float texid;

uniform sampler2D textureSampler1;
uniform sampler2D textureSampler2;
uniform sampler2D textureSampler3;
uniform sampler2D textureSampler4;
uniform sampler2D textureSampler5;
uniform sampler2D textureSampler6;
uniform sampler2D textureSampler7;
uniform sampler2D textureSampler8;
uniform sampler2D textureSampler9;
uniform sampler2D textureSampler10;
uniform sampler2D textureSampler11;
uniform sampler2D textureSampler12;

out vec4 out_color;

void main()
{
    vec4 col;
    if(texid < 0.5) col = texture2D( textureSampler1, uv );
    else if(texid < 1.5) col = texture2D( textureSampler2, uv );
    else if(texid < 2.5) col = texture2D( textureSampler3, uv );
    else if(texid < 3.5) col = texture2D( textureSampler4, uv );
    else if(texid < 4.5) col = texture2D( textureSampler5, uv );
    else if(texid < 5.5) col = texture2D( textureSampler6, uv );
    else if(texid < 6.5) col = texture2D( textureSampler7, uv );
    else if(texid < 7.5) col = texture2D( textureSampler8, uv );
    else if(texid < 8.5) col = texture2D( textureSampler9, uv );
    else if(texid < 9.5) col = texture2D( textureSampler10, uv );
    else if(texid < 10.5) col = texture2D( textureSampler11, uv );
    else if(texid < 11.5) col = texture2D( textureSampler12, uv );
    col = col * 2;
    if(col.x > 1){
        out_color.x = 1 - (2 - col.x) * (1 - v2f_color.x);
    }
    else out_color.x = col.x * v2f_color.x;
    if(col.y > 1){
        out_color.y = 1 - (2 - col.y) * (1 - v2f_color.y);
    }
    else out_color.y = col.y * v2f_color.y;
    if(col.z > 1){
        out_color.z = 1 - (2 - col.z) * (1 - v2f_color.z);
    }
    else out_color.z = col.z * v2f_color.z;
    out_color.w = col.w * v2f_color.w;
}
";

        int MakeShader(string vert, string frag)
        {
            int _vertexObj = GL.CreateShader(ShaderType.VertexShader);
            int _fragObj = GL.CreateShader(ShaderType.FragmentShader);
            int statusCode;
            string info;

            GL.ShaderSource(_vertexObj, vert);
            GL.CompileShader(_vertexObj);
            info = GL.GetShaderInfoLog(_vertexObj);
            GL.GetShader(_vertexObj, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            GL.ShaderSource(_fragObj, frag);
            GL.CompileShader(_fragObj);
            info = GL.GetShaderInfoLog(_fragObj);
            GL.GetShader(_fragObj, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            int shader = GL.CreateProgram();
            GL.AttachShader(shader, _fragObj);
            GL.AttachShader(shader, _vertexObj);
            GL.LinkProgram(shader);
            return shader;
        }
        #endregion

        void loadImage(Bitmap image, int texID, bool loop, bool linear = false)
        {
            GL.BindTexture(TextureTarget.Texture2D, texID);
            BitmapData data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            if (linear)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            }
            else
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            }
            if (loop)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            }
            else
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            }

            image.UnlockBits(data);
        }

        public bool Initialized { get; set; }

        public System.Windows.Media.ImageSource PreviewImage { get; set; } = null;

        public bool ManualNoteDelete => false;

        public double NoteCollectorOffset => -maxBottomCapSize;

        public NoteColor[][] NoteColors { get; set; }

        public double Tempo { get; set; }

        public double NoteScreenTime => settings.deltaTimeOnScreen + maxTopCapSize;

        public long LastNoteCount { get; set; }

        public System.Windows.Controls.Control SettingsControl { get; set; } = null;

        int quadShader;
        int evenquadShader;
        int inverseQuadShader;

        int vertexBufferID;
        int colorBufferID;
        int uvBufferID;
        int texIDBufferID;

        int quadBufferLength = 2048 * 64;
        double[] quadVertexbuff;
        float[] quadColorbuff;
        double[] quadUVbuff;
        float[] quadTexIDbuff;
        int quadBufferPos = 0;

        RenderSettings renderSettings;
        Settings settings;

        int indexBufferId;
        uint[] indexes = new uint[2048 * 128 * 6];

        bool[] blackKeys = new bool[257];
        int[] keynum = new int[257];

        Pack currPack = null;
        public long lastPackChangeTime = 0;

        public void UnloadPack()
        {
            if (currPack == null) return;
            GL.DeleteTextures(4, new int[] {
                currPack.whiteKeyTexID, currPack.whiteKeyPressedTexID,
                currPack.blackKeyTexID, currPack.blackKeyPressedTexID
            });

            if (currPack.whiteKeyLeftTex != null)
                GL.DeleteTextures(2, new int[] {
                    currPack.whiteKeyLeftTexID, currPack.whiteKeyPressedLeftTexID
                });
            if (currPack.whiteKeyRightTex != null)
                GL.DeleteTextures(2, new int[] {
                    currPack.whiteKeyRightTexID, currPack.whiteKeyPressedRightTexID
                });

            if (currPack.useBar) GL.DeleteTexture(currPack.barTexID);
            foreach (var n in currPack.NoteTextures)
            {
                GL.DeleteTexture(n.noteMiddleTexID);
                if (n.useCaps)
                {
                    GL.DeleteTexture(n.noteBottomTexID);
                    GL.DeleteTexture(n.noteTopTexID);
                }
            }
            foreach (var o in currPack.OverlayTextures)
            {
                GL.DeleteTexture(o.texID);
            }
        }

        public void LoadPack()
        {
            if (currPack == null) return;
            if (currPack.disposed) return;
            lock (currPack)
            {
                currPack.whiteKeyTexID = GL.GenTexture();
                currPack.whiteKeyPressedTexID = GL.GenTexture();
                currPack.blackKeyTexID = GL.GenTexture();
                currPack.blackKeyPressedTexID = GL.GenTexture();
                if (currPack.useBar) currPack.barTexID = GL.GenTexture();

                loadImage(currPack.whiteKeyTex, currPack.whiteKeyTexID, false, currPack.linearScaling);
                loadImage(currPack.whiteKeyPressedTex, currPack.whiteKeyPressedTexID, false, currPack.linearScaling);
                loadImage(currPack.blackKeyTex, currPack.blackKeyTexID, false);
                loadImage(currPack.blackKeyPressedTex, currPack.blackKeyPressedTexID, false);

                if (currPack.whiteKeyLeftTex != null)
                {
                    currPack.whiteKeyLeftTexID = GL.GenTexture();
                    currPack.whiteKeyPressedLeftTexID = GL.GenTexture();
                    loadImage(currPack.whiteKeyLeftTex, currPack.whiteKeyLeftTexID, false, currPack.linearScaling);
                    loadImage(currPack.whiteKeyPressedLeftTex, currPack.whiteKeyPressedLeftTexID, false, currPack.linearScaling);
                }
                if (currPack.whiteKeyRightTex != null)
                {
                    currPack.whiteKeyRightTexID = GL.GenTexture();
                    currPack.whiteKeyPressedRightTexID = GL.GenTexture();
                    loadImage(currPack.whiteKeyRightTex, currPack.whiteKeyRightTexID, false, currPack.linearScaling);
                    loadImage(currPack.whiteKeyPressedRightTex, currPack.whiteKeyPressedRightTexID, false, currPack.linearScaling);
                }

                if (currPack.useBar) loadImage(currPack.barTex, currPack.barTexID, false);

                foreach (var n in currPack.NoteTextures)
                {
                    n.noteMiddleTexID = GL.GenTexture();
                    loadImage(n.noteMiddleTex, n.noteMiddleTexID, true);
                    if (n.useCaps)
                    {
                        n.noteTopTexID = GL.GenTexture();
                        loadImage(n.noteTopTex, n.noteTopTexID, false);
                        n.noteBottomTexID = GL.GenTexture();
                        loadImage(n.noteBottomTex, n.noteBottomTexID, false);
                    }
                }
                foreach (var o in currPack.OverlayTextures)
                {
                    o.texID = GL.GenTexture();
                    loadImage(o.tex, o.texID, false);
                }
            }
        }

        public void CheckPack()
        {
            if (settings.lastPackChangeTime != lastPackChangeTime)
            {
                UnloadPack();
                currPack = settings.currPack;
                lastPackChangeTime = settings.lastPackChangeTime;
                LoadPack();
            }
        }

        public void Dispose()
        {
            GL.DeleteBuffers(3, new int[] { vertexBufferID, colorBufferID, uvBufferID, indexBufferId, texIDBufferID });

            GL.DeleteProgram(quadShader);
            GL.DeleteProgram(inverseQuadShader);
            GL.DeleteProgram(evenquadShader);
            quadVertexbuff = null;
            quadColorbuff = null;
            quadUVbuff = null;
            Initialized = false;
            UnloadPack();
            Console.WriteLine("Disposed of TextureRender");
        }

        public Render(RenderSettings settings)
        {
            this.settings = new Settings();
            this.renderSettings = settings;
            SettingsControl = new SettingsCtrl(this.settings);
            ((SettingsCtrl)SettingsControl).PaletteChanged += () => { ReloadTrackColors(); };
            PreviewImage = PluginUtils.BitmapToImageSource(Properties.Resources.pluginPreview);

            for (int i = 0; i < blackKeys.Length; i++) blackKeys[i] = isBlackNote(i);
            int b = 0;
            int w = 0;
            for (int i = 0; i < keynum.Length; i++)
            {
                if (blackKeys[i]) keynum[i] = b++;
                else keynum[i] = w++;
            }
        }

        public void Init()
        {
            quadShader = MakeShader(quadShaderVert, quadShaderFrag);
            inverseQuadShader = MakeShader(quadShaderVert, invertQuadShaderFrag);
            evenquadShader = MakeShader(quadShaderVert, evenQuadShaderFrag);

            int loc;
            int[] samplers = new int[12];
            for (int i = 0; i < 12; i++)
            {
                samplers[i] = i;
            }

            GL.UseProgram(quadShader);
            for (int i = 0; i < 12; i++)
            {
                loc = GL.GetUniformLocation(quadShader, "textureSampler" + (i + 1));
                GL.Uniform1(loc, i);
            }
            GL.UseProgram(inverseQuadShader);
            for (int i = 0; i < 12; i++)
            {
                loc = GL.GetUniformLocation(inverseQuadShader, "textureSampler" + (i + 1));
                GL.Uniform1(loc, i);
            }
            GL.UseProgram(evenquadShader);
            for (int i = 0; i < 12; i++)
            {
                loc = GL.GetUniformLocation(evenquadShader, "textureSampler" + (i + 1));
                GL.Uniform1(loc, i);
            }

            quadVertexbuff = new double[quadBufferLength * 8];
            quadColorbuff = new float[quadBufferLength * 16];
            quadUVbuff = new double[quadBufferLength * 8];
            quadTexIDbuff = new float[quadBufferLength * 4];

            LoadPack();

            GL.GenBuffers(1, out vertexBufferID);
            GL.GenBuffers(1, out colorBufferID);
            GL.GenBuffers(1, out uvBufferID);
            GL.GenBuffers(1, out texIDBufferID);
            for (uint i = 0; i < indexes.Length / 6; i++)
            {
                indexes[i * 6 + 0] = i * 4 + 0;
                indexes[i * 6 + 1] = i * 4 + 1;
                indexes[i * 6 + 2] = i * 4 + 3;
                indexes[i * 6 + 3] = i * 4 + 1;
                indexes[i * 6 + 4] = i * 4 + 3;
                indexes[i * 6 + 5] = i * 4 + 2;
            }

            GL.GenBuffers(1, out indexBufferId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(indexes.Length * 4),
                indexes,
                BufferUsageHint.StaticDraw);
            Initialized = true;
            Console.WriteLine("Initialised TextureRender");
        }

        Color4[] keyColors = new Color4[514];
        double[] x1arrayKeys = new double[257];
        double[] x1arrayNotes = new double[257];
        double[] wdtharrayKeys = new double[257];
        double[] wdtharrayNotes = new double[257];
        double maxTopCapSize = 0;
        double maxBottomCapSize = 0;

        void SwitchShader(TextureShaderType shader)
        {
            if (shader == TextureShaderType.Normal) GL.UseProgram(quadShader);
            if (shader == TextureShaderType.Inverted) GL.UseProgram(inverseQuadShader);
            if (shader == TextureShaderType.Hybrid) GL.UseProgram(evenquadShader);
        }

        public void RenderFrame(FastList<Note> notes, double midiTime, int finalCompositeBuff)
        {
            CheckPack();
            if (currPack == null) return;
            GL.Enable(EnableCap.Blend);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.Enable(EnableCap.Texture2D);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.Max);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, finalCompositeBuff);
            GL.Viewport(0, 0, renderSettings.PixelWidth, renderSettings.PixelHeight);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            #region Vars
            long nc = 0;
            int firstNote = settings.firstNote;
            int lastNote = settings.lastNote;
            int kbfirstNote = settings.firstNote;
            int kblastNote = settings.lastNote;
            if (blackKeys[firstNote] || (currPack.whiteKeysFullOctave && currPack.whiteKeyLeftTex == null && firstNote != 0)) kbfirstNote--;
            if (blackKeys[lastNote - 1] || (currPack.whiteKeysFullOctave && currPack.whiteKeyRightTex == null)) kblastNote++;

            double deltaTimeOnScreen = settings.deltaTimeOnScreen;
            double viewAspect = (double)renderSettings.PixelWidth / renderSettings.PixelHeight;
            double keyboardHeightFull = currPack.keyboardHeight / (lastNote - firstNote) * 128 / (1920.0 / 1080.0) * viewAspect;
            double keyboardHeight = keyboardHeightFull;
            double barHeight = keyboardHeightFull * currPack.barHeight;
            if (currPack.useBar) keyboardHeight -= barHeight;
            bool sameWidth = currPack.sameWidthNotes;
            for (int i = 0; i < 514; i++) keyColors[i] = Color4.Transparent;
            double wdth;
            float r, g, b, a, r2, g2, b2, a2;
            double x1;
            double x2;
            double y1;
            double y2;
            int pos;
            quadBufferPos = 0;
            bool interpolateUnendedNotes = currPack.interpolateUnendedNotes != 0;
            float interpolateUnendedNotesVal = 1.0f / renderSettings.FPS / currPack.interpolateUnendedNotes;

            if (sameWidth)
            {
                for (int i = 0; i < 257; i++)
                {
                    x1arrayKeys[i] = (float)(i - firstNote) / (lastNote - firstNote);
                    wdtharrayKeys[i] = 1.0f / (lastNote - firstNote);
                    x1arrayNotes[i] = (float)(i - firstNote) / (lastNote - firstNote);
                    wdtharrayNotes[i] = 1.0f / (lastNote - firstNote);
                }
            }
            else
            {
                for (int i = 0; i < 257; i++)
                {
                    if (!blackKeys[i])
                    {
                        x1arrayKeys[i] = keynum[i];
                        x1arrayNotes[i] = keynum[i];
                        wdtharrayKeys[i] = 1.0f;
                        wdtharrayNotes[i] = 1.0f;
                    }
                    else
                    {
                        int _i = i + 1;
                        wdth = currPack.blackKeyScale * currPack.advancedBlackKeySizes[keynum[i] % 5];
                        int bknum = keynum[i] % 5;
                        double offset = wdth / 2;
                        if (bknum == 0) offset += wdth / 2 * currPack.blackKey2setOffset;
                        if (bknum == 2) offset += wdth / 2 * currPack.blackKey3setOffset;
                        if (bknum == 1) offset -= wdth / 2 * currPack.blackKey2setOffset;
                        if (bknum == 4) offset -= wdth / 2 * currPack.blackKey3setOffset;

                        offset -= currPack.advancedBlackKeyOffsets[keynum[i] % 5] * wdth / 2;

                        x1arrayKeys[i] = keynum[_i] - offset;
                        wdtharrayKeys[i] = wdth;

                        offset -= wdth / 2 * (1 - currPack.blackNoteScale);
                        if (bknum == 0) offset += wdth / 2 * currPack.blackNote2setOffset;
                        if (bknum == 2) offset += wdth / 2 * currPack.blackNote3setOffset;
                        if (bknum == 1) offset -= wdth / 2 * currPack.blackNote2setOffset;
                        if (bknum == 4) offset -= wdth / 2 * currPack.blackNote3setOffset;
                        wdth *= currPack.blackNoteScale;

                        x1arrayNotes[i] = (float)keynum[_i] - offset;
                        wdtharrayNotes[i] = wdth;
                    }
                }
                double knmfn = x1arrayKeys[firstNote];
                double knmln = x1arrayKeys[lastNote - 1] + wdtharrayKeys[lastNote - 1];
                double width = knmln - knmfn;
                for (int i = 0; i < 257; i++)
                {
                    x1arrayKeys[i] = (x1arrayKeys[i] - knmfn) / width;
                    x1arrayNotes[i] = (x1arrayNotes[i] - knmfn) / width;
                    wdtharrayKeys[i] /= width;
                    wdtharrayNotes[i] /= width;
                }
            }

            #endregion

            #region Notes
            quadBufferPos = 0;
            double notePosFactor = 1 / deltaTimeOnScreen * (1 - keyboardHeightFull);

            maxTopCapSize = 0;
            maxBottomCapSize = 0;
            foreach (var tex in currPack.NoteTextures)
            {
                var topSize = tex.noteTopAspect * viewAspect * wdtharrayNotes[5] / notePosFactor;
                if (tex.squeezeEndCaps) topSize *= tex.noteTopOversize;
                var bottomSize = tex.noteBottomAspect * viewAspect * wdtharrayNotes[5] / notePosFactor;
                if (tex.squeezeEndCaps) bottomSize *= tex.noteBottomOversize;
                if (maxTopCapSize < topSize) maxTopCapSize = topSize;
                if (maxBottomCapSize < bottomSize) maxBottomCapSize = bottomSize;
            }

            double renderCutoff = midiTime + deltaTimeOnScreen;

            var currNoteTex = currPack.NoteTextures[0];
            var noteTextures = currPack.NoteTextures;
            GL.BindTexture(TextureTarget.Texture2D, currNoteTex.noteMiddleTexID);
            for (int i = 0; i < noteTextures.Length; i++)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + (i * 3));
                GL.BindTexture(TextureTarget.Texture2D, noteTextures[i].noteMiddleTexID);
                if (noteTextures[i].useCaps)
                {
                    GL.ActiveTexture(TextureUnit.Texture0 + (i * 3) + 1);
                    GL.BindTexture(TextureTarget.Texture2D, noteTextures[i].noteBottomTexID);
                    GL.ActiveTexture(TextureUnit.Texture0 + (i * 3) + 2);
                    GL.BindTexture(TextureTarget.Texture2D, noteTextures[i].noteTopTexID);
                }
            }
            SwitchShader(currPack.noteShader);
            for (int i = 0; i < 2; i++)
            {
                bool black = false;
                bool blackabove = settings.blackNotesAbove;
                if (blackabove && i == 1) black = true;
                if (!blackabove && i == 1) break;
                foreach (Note n in notes)
                {
                    if (n.end >= midiTime || !n.hasEnded)
                    {
                        if (n.start < renderCutoff + maxBottomCapSize)
                        {
                            nc++;
                            int k = n.key;
                            if (blackabove && !black && blackKeys[k]) continue;
                            if (blackabove && black && !blackKeys[k]) continue;
                            if (!(k >= firstNote && k < lastNote)) continue;
                            Color4 coll = n.color.Left;
                            Color4 colr = n.color.Right;
                            if (n.start <= midiTime)
                            {
                                Color4 origcoll = keyColors[k * 2];
                                Color4 origcolr = keyColors[k * 2 + 1];
                                float blendfac = coll.A;
                                float revblendfac = 1 - blendfac;
                                keyColors[k * 2] = new Color4(
                                    coll.R * blendfac + origcoll.R * revblendfac,
                                    coll.G * blendfac + origcoll.G * revblendfac,
                                    coll.B * blendfac + origcoll.B * revblendfac,
                                    1);
                                blendfac = colr.A;
                                revblendfac = 1 - blendfac;
                                keyColors[k * 2 + 1] = new Color4(
                                    colr.R * blendfac + origcolr.R * revblendfac,
                                    colr.G * blendfac + origcolr.G * revblendfac,
                                    colr.B * blendfac + origcolr.B * revblendfac,
                                    1);
                            }
                            x1 = x1arrayNotes[k];
                            wdth = wdtharrayNotes[k];
                            x2 = x1 + wdth;
                            y1 = 1 - (renderCutoff - n.end) * notePosFactor;
                            y2 = 1 - (renderCutoff - n.start) * notePosFactor;
                            if (!n.hasEnded)
                            {
                                y1 = 1.0;
                                if (interpolateUnendedNotes)
                                    n.meta = 1.0f;
                            }
                            else if (interpolateUnendedNotes)
                            {
                                if (n.meta == null) n.meta = 0f;
                                n.meta = (float)n.meta - interpolateUnendedNotesVal;
                                if ((float)n.meta < 0f) n.meta = 0f;
                                y1 = 1.0 * (float)n.meta + y1 * (1 - (float)n.meta);
                            }
                            double texSize = (y1 - y2) / wdth / viewAspect;
                            NoteTexture ntex = null;
                            int tex = 0;
                            foreach (var t in noteTextures)
                            {
                                if (t.maxSize > texSize)
                                {
                                    if (t.keyType != KeyType.Both)
                                    {
                                        if (blackKeys[k] ^ (t.keyType == KeyType.Black))
                                        {
                                            tex++;
                                            continue;
                                        }
                                    }
                                    ntex = t;
                                    break;
                                }
                                tex++;
                            }
                            tex *= 3;

                            if (ntex.highlightHitNotes > 0 && n.start <= midiTime)
                            {
                                var col = ntex.highlightHitNotesColor;
                                float blendfac = (float)ntex.highlightHitNotes;
                                float iblendfac = 1 - blendfac;
                                coll = new Color4(
                                    coll.R * iblendfac + col.R / 255.0f * blendfac,
                                    coll.G * iblendfac + col.G / 255.0f * blendfac,
                                    coll.B * iblendfac + col.B / 255.0f * blendfac,
                                    coll.A
                                );
                                colr = new Color4(
                                    colr.R * iblendfac + col.R / 255.0f * blendfac,
                                    colr.G * iblendfac + col.G / 255.0f * blendfac,
                                    colr.B * iblendfac + col.B / 255.0f * blendfac,
                                    colr.A
                                );
                            }

                            double topHeight;
                            double bottomHeight;
                            double yy1 = 0, yy2 = 0;
                            if (ntex.useCaps)
                            {
                                topHeight = wdth * ntex.noteTopAspect * viewAspect;
                                bottomHeight = wdth * ntex.noteBottomAspect * viewAspect;
                                yy1 = y1 + topHeight * ntex.noteTopOversize;
                                yy2 = y2 - bottomHeight * ntex.noteBottomOversize;
                                if (n.hasEnded)
                                    y1 -= topHeight * (1 - ntex.noteTopOversize);
                                y2 += bottomHeight * (1 - ntex.noteBottomOversize);
                                if (y2 > y1)
                                {
                                    double middley = (y2 + y1) / 2;
                                    y1 = middley;
                                    y2 = middley;
                                    if (!ntex.squeezeEndCaps)
                                    {
                                        yy1 = y1 + topHeight;
                                        yy2 = y2 - bottomHeight;
                                    }
                                }
                                texSize = (y1 - y2) / wdth / viewAspect;
                            }

                            if (ntex.stretch)
                                texSize = 1;
                            else
                                texSize /= currNoteTex.noteMiddleAspect;
                            if (n.hasEnded)
                                texSize = -Math.Round(texSize);
                            if (texSize == 0) texSize = -1;
                            pos = quadBufferPos * 8;
                            quadVertexbuff[pos++] = x2;
                            quadVertexbuff[pos++] = y2;
                            quadVertexbuff[pos++] = x2;
                            quadVertexbuff[pos++] = y1;
                            quadVertexbuff[pos++] = x1;
                            quadVertexbuff[pos++] = y1;
                            quadVertexbuff[pos++] = x1;
                            quadVertexbuff[pos++] = y2;

                            pos = quadBufferPos * 16;
                            if (blackKeys[n.key])
                            {
                                float multiply = (float)currNoteTex.darkenBlackNotes;
                                r = coll.R * multiply;
                                g = coll.G * multiply;
                                b = coll.B * multiply;
                                a = coll.A;
                                r2 = colr.R * multiply;
                                g2 = colr.G * multiply;
                                b2 = colr.B * multiply;
                                a2 = colr.A;
                            }
                            else
                            {
                                r = coll.R;
                                g = coll.G;
                                b = coll.B;
                                a = coll.A;
                                r2 = colr.R;
                                g2 = colr.G;
                                b2 = colr.B;
                                a2 = colr.A;
                            }
                            quadColorbuff[pos++] = r;
                            quadColorbuff[pos++] = g;
                            quadColorbuff[pos++] = b;
                            quadColorbuff[pos++] = a;
                            quadColorbuff[pos++] = r;
                            quadColorbuff[pos++] = g;
                            quadColorbuff[pos++] = b;
                            quadColorbuff[pos++] = a;
                            quadColorbuff[pos++] = r2;
                            quadColorbuff[pos++] = g2;
                            quadColorbuff[pos++] = b2;
                            quadColorbuff[pos++] = a2;
                            quadColorbuff[pos++] = r2;
                            quadColorbuff[pos++] = g2;
                            quadColorbuff[pos++] = b2;
                            quadColorbuff[pos++] = a2;

                            pos = quadBufferPos * 8;
                            quadUVbuff[pos++] = 1;
                            quadUVbuff[pos++] = 0;
                            quadUVbuff[pos++] = 1;
                            quadUVbuff[pos++] = texSize;
                            quadUVbuff[pos++] = 0;
                            quadUVbuff[pos++] = texSize;
                            quadUVbuff[pos++] = 0;
                            quadUVbuff[pos++] = 0;

                            pos = quadBufferPos * 4;
                            quadTexIDbuff[pos++] = tex;
                            quadTexIDbuff[pos++] = tex;
                            quadTexIDbuff[pos++] = tex;
                            quadTexIDbuff[pos++] = tex;

                            quadBufferPos++;
                            FlushQuadBuffer();

                            if (ntex.useCaps)
                            {
                                pos = quadBufferPos * 8;
                                quadVertexbuff[pos++] = x2;
                                quadVertexbuff[pos++] = yy2;
                                quadVertexbuff[pos++] = x2;
                                quadVertexbuff[pos++] = y2;
                                quadVertexbuff[pos++] = x1;
                                quadVertexbuff[pos++] = y2;
                                quadVertexbuff[pos++] = x1;
                                quadVertexbuff[pos++] = yy2;

                                pos = quadBufferPos * 16;
                                quadColorbuff[pos++] = r;
                                quadColorbuff[pos++] = g;
                                quadColorbuff[pos++] = b;
                                quadColorbuff[pos++] = a;
                                quadColorbuff[pos++] = r;
                                quadColorbuff[pos++] = g;
                                quadColorbuff[pos++] = b;
                                quadColorbuff[pos++] = a;
                                quadColorbuff[pos++] = r2;
                                quadColorbuff[pos++] = g2;
                                quadColorbuff[pos++] = b2;
                                quadColorbuff[pos++] = a2;
                                quadColorbuff[pos++] = r2;
                                quadColorbuff[pos++] = g2;
                                quadColorbuff[pos++] = b2;
                                quadColorbuff[pos++] = a2;

                                pos = quadBufferPos * 8;
                                quadUVbuff[pos++] = 1;
                                quadUVbuff[pos++] = 1;
                                quadUVbuff[pos++] = 1;
                                quadUVbuff[pos++] = 0;
                                quadUVbuff[pos++] = 0;
                                quadUVbuff[pos++] = 0;
                                quadUVbuff[pos++] = 0;
                                quadUVbuff[pos++] = 1;

                                pos = quadBufferPos * 4;
                                tex++;
                                quadTexIDbuff[pos++] = tex;
                                quadTexIDbuff[pos++] = tex;
                                quadTexIDbuff[pos++] = tex;
                                quadTexIDbuff[pos++] = tex;

                                quadBufferPos++;
                                FlushQuadBuffer();

                                pos = quadBufferPos * 8;
                                quadVertexbuff[pos++] = x2;
                                quadVertexbuff[pos++] = y1;
                                quadVertexbuff[pos++] = x2;
                                quadVertexbuff[pos++] = yy1;
                                quadVertexbuff[pos++] = x1;
                                quadVertexbuff[pos++] = yy1;
                                quadVertexbuff[pos++] = x1;
                                quadVertexbuff[pos++] = y1;

                                pos = quadBufferPos * 16;
                                quadColorbuff[pos++] = r;
                                quadColorbuff[pos++] = g;
                                quadColorbuff[pos++] = b;
                                quadColorbuff[pos++] = a;
                                quadColorbuff[pos++] = r;
                                quadColorbuff[pos++] = g;
                                quadColorbuff[pos++] = b;
                                quadColorbuff[pos++] = a;
                                quadColorbuff[pos++] = r2;
                                quadColorbuff[pos++] = g2;
                                quadColorbuff[pos++] = b2;
                                quadColorbuff[pos++] = a2;
                                quadColorbuff[pos++] = r2;
                                quadColorbuff[pos++] = g2;
                                quadColorbuff[pos++] = b2;
                                quadColorbuff[pos++] = a2;

                                pos = quadBufferPos * 8;
                                quadUVbuff[pos++] = 1;
                                quadUVbuff[pos++] = 1;
                                quadUVbuff[pos++] = 1;
                                quadUVbuff[pos++] = 0;
                                quadUVbuff[pos++] = 0;
                                quadUVbuff[pos++] = 0;
                                quadUVbuff[pos++] = 0;
                                quadUVbuff[pos++] = 1;

                                pos = quadBufferPos * 4;
                                tex++;
                                quadTexIDbuff[pos++] = tex;
                                quadTexIDbuff[pos++] = tex;
                                quadTexIDbuff[pos++] = tex;
                                quadTexIDbuff[pos++] = tex;

                                quadBufferPos++;
                                FlushQuadBuffer();
                            }

                        }
                        else break;
                    }
                    else
                    { }
                }
            }
            FlushQuadBuffer(false);
            quadBufferPos = 0;

            LastNoteCount = nc;
            #endregion

            RenderOverlays(true);

            #region Keyboard

            GL.UseProgram(quadShader);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, currPack.barTexID);
            pos = quadBufferPos * 8;
            quadVertexbuff[pos++] = 0;
            quadVertexbuff[pos++] = keyboardHeightFull;
            quadVertexbuff[pos++] = 1;
            quadVertexbuff[pos++] = keyboardHeightFull;
            quadVertexbuff[pos++] = 1;
            quadVertexbuff[pos++] = keyboardHeight;
            quadVertexbuff[pos++] = 0;
            quadVertexbuff[pos++] = keyboardHeight;

            pos = quadBufferPos * 16;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;
            quadColorbuff[pos++] = 1;

            pos = quadBufferPos * 8;
            quadUVbuff[pos++] = 0;
            quadUVbuff[pos++] = 0;
            quadUVbuff[pos++] = 1;
            quadUVbuff[pos++] = 0;
            quadUVbuff[pos++] = 1;
            quadUVbuff[pos++] = 1;
            quadUVbuff[pos++] = 0;
            quadUVbuff[pos++] = 1;

            pos = quadBufferPos * 4;
            quadTexIDbuff[pos++] = 0;
            quadTexIDbuff[pos++] = 0;
            quadTexIDbuff[pos++] = 0;
            quadTexIDbuff[pos++] = 0;
            quadBufferPos++;
            FlushQuadBuffer(false);

            y1 = keyboardHeight;
            y2 = 0;
            Color4[] origColors = new Color4[257];
            SwitchShader(currPack.whiteKeyShader);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, currPack.whiteKeyPressedTexID);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, currPack.whiteKeyTexID);
            if (currPack.whiteKeyLeftTex != null)
            {
                GL.ActiveTexture(TextureUnit.Texture3);
                GL.BindTexture(TextureTarget.Texture2D, currPack.whiteKeyPressedLeftTexID);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, currPack.whiteKeyLeftTexID);
            }
            if (currPack.whiteKeyRightTex != null)
            {
                GL.ActiveTexture(TextureUnit.Texture5);
                GL.BindTexture(TextureTarget.Texture2D, currPack.whiteKeyPressedRightTexID);
                GL.ActiveTexture(TextureUnit.Texture4);
                GL.BindTexture(TextureTarget.Texture2D, currPack.whiteKeyRightTexID);
            }
            for (int k = kbfirstNote; k < kblastNote; k++)
            {
                if (isBlackNote(k))
                    origColors[k] = Color4.Black;
                else
                    origColors[k] = Color4.White;
            }

            float pressed;
            for (int n = kbfirstNote; n < kblastNote; n++)
            {
                x1 = x1arrayKeys[n];
                wdth = wdtharrayKeys[n];
                x2 = x1 + wdth;

                if (!blackKeys[n])
                {
                    y2 = 0;
                    if (sameWidth)
                    {
                        int _n = n % 12;
                        if (_n == 0)
                            x2 += wdth * 0.666;
                        else if (_n == 2)
                        {
                            x1 -= wdth / 3;
                            x2 += wdth / 3;
                        }
                        else if (_n == 4)
                            x1 -= wdth / 3 * 2;
                        else if (_n == 5)
                            x2 += wdth * 0.75;
                        else if (_n == 7)
                        {
                            x1 -= wdth / 4;
                            x2 += wdth / 2;
                        }
                        else if (_n == 9)
                        {
                            x1 -= wdth / 2;
                            x2 += wdth / 4;
                        }
                        else if (_n == 11)
                            x1 -= wdth * 0.75;
                    }
                }
                else continue;

                var coll = keyColors[n * 2];
                var colr = keyColors[n * 2 + 1];
                var origcol = origColors[n];
                float blendfac1 = coll.A;
                float revblendfac = 1 - blendfac1;
                coll = new Color4(
                    coll.R * blendfac1 + origcol.R * revblendfac,
                    coll.G * blendfac1 + origcol.G * revblendfac,
                    coll.B * blendfac1 + origcol.B * revblendfac,
                    1);
                r = coll.R;
                g = coll.G;
                b = coll.B;
                a = coll.A;
                float blendfac2 = coll.A;
                blendfac2 = colr.A;
                revblendfac = 1 - blendfac2;
                colr = new Color4(
                    colr.R * blendfac2 + origcol.R * revblendfac,
                    colr.G * blendfac2 + origcol.G * revblendfac,
                    colr.B * blendfac2 + origcol.B * revblendfac,
                    1);
                r2 = colr.R;
                g2 = colr.G;
                b2 = colr.B;
                a2 = colr.A;
                if (blendfac1 + blendfac2 != 0) pressed = 1;
                else pressed = 0;

                if (currPack.whiteKeyLeftTex != null && n == kbfirstNote && !blackKeys[n])
                    pressed += 2;
                else if (currPack.whiteKeyRightTex != null && n == kblastNote - 1 && !blackKeys[n])
                    pressed += 4;

                double yy1 = y1;
                if (pressed == 1) yy1 += keyboardHeightFull * currPack.whiteKeyPressedOversize;
                else yy1 += keyboardHeightFull * currPack.whiteKeyOversize;

                pos = quadBufferPos * 8;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = yy1;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = yy1;

                pos = quadBufferPos * 16;
                quadColorbuff[pos++] = r;
                quadColorbuff[pos++] = g;
                quadColorbuff[pos++] = b;
                quadColorbuff[pos++] = a;
                quadColorbuff[pos++] = r;
                quadColorbuff[pos++] = g;
                quadColorbuff[pos++] = b;
                quadColorbuff[pos++] = a;
                quadColorbuff[pos++] = r2;
                quadColorbuff[pos++] = g2;
                quadColorbuff[pos++] = b2;
                quadColorbuff[pos++] = a2;
                quadColorbuff[pos++] = r2;
                quadColorbuff[pos++] = g2;
                quadColorbuff[pos++] = b2;
                quadColorbuff[pos++] = a2;

                if (!currPack.whiteKeysFullOctave)
                {
                    pos = quadBufferPos * 8;
                    quadUVbuff[pos++] = 0;
                    quadUVbuff[pos++] = 1;
                    quadUVbuff[pos++] = 1;
                    quadUVbuff[pos++] = 1;
                    quadUVbuff[pos++] = 1;
                    quadUVbuff[pos++] = 0;
                    quadUVbuff[pos++] = 0;
                    quadUVbuff[pos++] = 0;
                }
                else
                {
                    var k = keynum[n] % 7;
                    double uvl = k / 7.0;
                    double uvr = (k + 1) / 7.0;
                    pos = quadBufferPos * 8;
                    quadUVbuff[pos++] = uvl;
                    quadUVbuff[pos++] = 1;
                    quadUVbuff[pos++] = uvr;
                    quadUVbuff[pos++] = 1;
                    quadUVbuff[pos++] = uvr;
                    quadUVbuff[pos++] = 0;
                    quadUVbuff[pos++] = uvl;
                    quadUVbuff[pos++] = 0;
                }

                pos = quadBufferPos * 4;
                quadTexIDbuff[pos++] = pressed;
                quadTexIDbuff[pos++] = pressed;
                quadTexIDbuff[pos++] = pressed;
                quadTexIDbuff[pos++] = pressed;
                quadBufferPos++;
                FlushQuadBuffer();
            }
            FlushQuadBuffer(false);
            SwitchShader(currPack.blackKeyShader);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, currPack.blackKeyPressedTexID);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, currPack.blackKeyTexID);
            for (int n = kbfirstNote; n < kblastNote; n++)
            {
                x1 = x1arrayKeys[n];
                wdth = wdtharrayKeys[n];
                x2 = x1 + wdth;

                if (blackKeys[n])
                {
                    y2 = keyboardHeight * currPack.blackKeyHeight;
                }
                else continue;

                var coll = keyColors[n * 2];
                var colr = keyColors[n * 2 + 1];
                var origcol = origColors[n];
                float blendfac1 = coll.A;
                float revblendfac = 1 - blendfac1;
                coll = new Color4(
                    coll.R * blendfac1 + origcol.R * revblendfac,
                    coll.G * blendfac1 + origcol.G * revblendfac,
                    coll.B * blendfac1 + origcol.B * revblendfac,
                    1);
                r = coll.R;
                g = coll.G;
                b = coll.B;
                a = coll.A;
                float blendfac2 = coll.A;
                blendfac2 = colr.A;
                revblendfac = 1 - blendfac2;
                colr = new Color4(
                    colr.R * blendfac2 + origcol.R * revblendfac,
                    colr.G * blendfac2 + origcol.G * revblendfac,
                    colr.B * blendfac2 + origcol.B * revblendfac,
                    1);
                r2 = colr.R;
                g2 = colr.G;
                b2 = colr.B;
                a2 = colr.A;
                if (blendfac1 + blendfac2 != 0) pressed = 1;
                else pressed = 0;

                double yy1 = y1;
                if (pressed == 1) yy1 += keyboardHeightFull * currPack.blackKeyPressedOversize;
                else yy1 += keyboardHeightFull * currPack.blackKeyOversize;
                if (pressed == 0 && currPack.blackKeyDefaultWhite)
                {
                    r = g = b = a = 1;
                    r2 = g2 = b2 = a2 = 1;
                }

                pos = quadBufferPos * 8;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = yy1;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = yy1;

                pos = quadBufferPos * 16;
                quadColorbuff[pos++] = r;
                quadColorbuff[pos++] = g;
                quadColorbuff[pos++] = b;
                quadColorbuff[pos++] = a;
                quadColorbuff[pos++] = r;
                quadColorbuff[pos++] = g;
                quadColorbuff[pos++] = b;
                quadColorbuff[pos++] = a;
                quadColorbuff[pos++] = r2;
                quadColorbuff[pos++] = g2;
                quadColorbuff[pos++] = b2;
                quadColorbuff[pos++] = a2;
                quadColorbuff[pos++] = r2;
                quadColorbuff[pos++] = g2;
                quadColorbuff[pos++] = b2;
                quadColorbuff[pos++] = a2;

                if (!currPack.blackKeysFullOctave)
                {
                    pos = quadBufferPos * 8;
                    quadUVbuff[pos++] = 0;
                    quadUVbuff[pos++] = 1;
                    quadUVbuff[pos++] = 1;
                    quadUVbuff[pos++] = 1;
                    quadUVbuff[pos++] = 1;
                    quadUVbuff[pos++] = 0;
                    quadUVbuff[pos++] = 0;
                    quadUVbuff[pos++] = 0;
                }
                else
                {
                    var k = keynum[n] % 5;
                    double uvl = k / 5.0;
                    double uvr = (k + 1) / 5.0;
                    pos = quadBufferPos * 8;
                    quadUVbuff[pos++] = uvl;
                    quadUVbuff[pos++] = 1;
                    quadUVbuff[pos++] = uvr;
                    quadUVbuff[pos++] = 1;
                    quadUVbuff[pos++] = uvr;
                    quadUVbuff[pos++] = 0;
                    quadUVbuff[pos++] = uvl;
                    quadUVbuff[pos++] = 0;
                }

                pos = quadBufferPos * 4;
                quadTexIDbuff[pos++] = pressed;
                quadTexIDbuff[pos++] = pressed;
                quadTexIDbuff[pos++] = pressed;
                quadTexIDbuff[pos++] = pressed;

                quadBufferPos++;
                FlushQuadBuffer(true);
            }
            FlushQuadBuffer(false);
            #endregion

            RenderOverlays(false);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Disable(EnableCap.Blend);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.Disable(EnableCap.Texture2D);

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.DisableVertexAttribArray(2);
            GL.DisableVertexAttribArray(3);
        }

        void RenderOverlays(bool below)
        {
            double getx1(int key)
            {
                int k = key % 12;
                if (k < 0) k += 12;
                int o = (key - k) / 12;

                return x1arrayKeys[k] + (x1arrayKeys[12] - x1arrayKeys[0]) * o;
            }

            double getwdth(int key)
            {
                int k = key % 12;
                if (k < 0) k += 12;
                if (isBlackNote(k)) return wdtharrayKeys[1];
                else return wdtharrayKeys[0];
            }

            double viewAspect = (double)renderSettings.PixelWidth / renderSettings.PixelHeight;
            foreach (var o in currPack.OverlayTextures)
            {
                if (o.overlayBelow != below) continue;
                int pos;
                GL.UseProgram(quadShader);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, o.texID);
                pos = quadBufferPos * 8;
                double start = getx1(o.firstKey);
                double end = getx1(o.lastKey) + getwdth(o.lastKey);
                double height = Math.Abs(start - end) / o.texAspect * viewAspect;
                quadVertexbuff[pos++] = start;
                quadVertexbuff[pos++] = height;
                quadVertexbuff[pos++] = end;
                quadVertexbuff[pos++] = height;
                quadVertexbuff[pos++] = end;
                quadVertexbuff[pos++] = 0;
                quadVertexbuff[pos++] = start;
                quadVertexbuff[pos++] = 0;

                pos = quadBufferPos * 16;
                quadColorbuff[pos++] = 1;
                quadColorbuff[pos++] = 1;
                quadColorbuff[pos++] = 1;
                quadColorbuff[pos++] = (float)o.alpha;
                quadColorbuff[pos++] = 1;
                quadColorbuff[pos++] = 1;
                quadColorbuff[pos++] = 1;
                quadColorbuff[pos++] = (float)o.alpha;
                quadColorbuff[pos++] = 1;
                quadColorbuff[pos++] = 1;
                quadColorbuff[pos++] = 1;
                quadColorbuff[pos++] = (float)o.alpha;
                quadColorbuff[pos++] = 1;
                quadColorbuff[pos++] = 1;
                quadColorbuff[pos++] = 1;
                quadColorbuff[pos++] = (float)o.alpha;

                pos = quadBufferPos * 8;
                quadUVbuff[pos++] = 0;
                quadUVbuff[pos++] = 0;
                quadUVbuff[pos++] = 1;
                quadUVbuff[pos++] = 0;
                quadUVbuff[pos++] = 1;
                quadUVbuff[pos++] = 1;
                quadUVbuff[pos++] = 0;
                quadUVbuff[pos++] = 1;

                pos = quadBufferPos * 4;
                quadTexIDbuff[pos++] = 0;
                quadTexIDbuff[pos++] = 0;
                quadTexIDbuff[pos++] = 0;
                quadTexIDbuff[pos++] = 0;
                quadBufferPos++;
                FlushQuadBuffer(false);
            }
        }

        void FlushQuadBuffer(bool check = true)
        {
            if (quadBufferPos < quadBufferLength && check) return;
            if (quadBufferPos == 0) return;
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(quadBufferPos * 8 * 8),
                quadVertexbuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Double, false, 16, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferID);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(quadBufferPos * 16 * 4),
                quadColorbuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 16, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, uvBufferID);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(quadBufferPos * 8 * 8),
                quadUVbuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Double, false, 16, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, texIDBufferID);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(quadBufferPos * 1 * 4 * 4),
                quadTexIDbuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, 4, 0);
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, 4, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
            GL.IndexPointer(IndexPointerType.Int, 1, 0);
            GL.DrawElements(PrimitiveType.Triangles, quadBufferPos * 6, DrawElementsType.UnsignedInt, IntPtr.Zero);
            quadBufferPos = 0;
        }

        bool isBlackNote(int n)
        {
            n = n % 12;
            return n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
        }

        public void ReloadTrackColors()
        {
            if (NoteColors == null) return;
            var cols = ((SettingsCtrl)SettingsControl).paletteList.GetColors(NoteColors.Length);

            for (int i = 0; i < NoteColors.Length; i++)
            {
                for (int j = 0; j < NoteColors[i].Length; j++)
                {
                    if (NoteColors[i][j].isDefault)
                    {
                        NoteColors[i][j].Left = cols[i * 32 + j * 2];
                        NoteColors[i][j].Right = cols[i * 32 + j * 2 + 1];
                    }
                }
            }
        }
    }
}
