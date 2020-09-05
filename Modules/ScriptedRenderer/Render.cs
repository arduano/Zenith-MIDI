using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK;
using System.Drawing;
using System.Drawing.Imaging;
using ScriptedEngine;
using Font = ScriptedEngine.Font;

namespace ScriptedRender
{
    class jsIterator<T>
    {
        IEnumerator<T> en;
        IEnumerable<T> iter;

        public jsIterator(IEnumerable<T> iter)
        {
            this.iter = iter;
            reset();
        }

        public T current => en.Current;
        public bool next() => en.MoveNext();
        public void reset() => en = iter.GetEnumerator();
    }

    public class Render : IPluginRender
    {
        public string Name => "Scripted";
        public string Description => "Uses user-made C# scripts (compiled in runtime) to give almost full control over rendering.\nExtremely customizable, relatively easy to use.";
        public string LanguageDictName => "scripted";

        public bool ManualNoteDelete { get; private set; } = false;
        public double NoteCollectorOffset { get; private set; } = 0;

        public System.Windows.Media.ImageSource PreviewImage { get; private set; }
        public System.Windows.Controls.Control SettingsControl => settingsControl;
        public bool Initialized { get; private set; }
        public double NoteScreenTime { get; private set; }
        public long LastNoteCount { get; private set; }

        public NoteColor[][] NoteColors { get; set; }
        public double Tempo { get; set; }

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
    if(texid < -0.5) col = vec4(1, 1, 1, 1);
    else if(texid < 0.5) col = texture2D( textureSampler1, uv );
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
    if(texid < -0.5) col = vec4(0, 0, 0, 1);
    else if(texid < 0.5) col = texture2D( textureSampler1, uv );
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
    if(texid < -0.5) col = vec4(0.5, 0.5, 0.5, 1);
    else if(texid < 0.5) col = texture2D( textureSampler1, uv );
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

        #region Vars
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

        int indexBufferId;
        uint[] indexes = new uint[2048 * 128 * 6];

        double[] x1arrayKeys = new double[257];
        double[] x1arrayNotes = new double[257];
        double[] wdtharrayKeys = new double[257];
        double[] wdtharrayNotes = new double[257];

        int[] activeTexIds = new int[12];

        public long lastScriptChangeTime = 0;

        RenderStatus renderSettings;
        Settings settings;

        SettingsCtrl settingsControl;

        Script currScript;

        TextureShaders currentShader;
        BlendFunc currentBlendFunc;
        #endregion

        public Render(RenderStatus settings)
        {
            renderSettings = settings;

            this.settings = new Settings();
            settingsControl = new SettingsCtrl(this.settings);
            ((SettingsCtrl)SettingsControl).PaletteChanged += () => { ReloadTrackColors(); };
            PreviewImage = PluginUtils.BitmapToImageSource(Properties.Resources.preview);
        }

        RenderOptions GetRenderOptions(double midiTime = 0)
        {
            return new RenderOptions()
            {
                firstKey = settings.firstNote,
                lastKey = settings.lastNote,
                midiTime = midiTime,
                noteScreenTime = settings.deltaTimeOnScreen,

                renderFPS = renderSettings.FPS,
                renderWidth = renderSettings.PixelWidth,
                renderHeight = renderSettings.PixelHeight,
                renderSSAA = renderSettings.SSAA,
                renderAspectRatio = renderSettings.PixelWidth / (double)renderSettings.PixelHeight,

                midiPPQ = CurrentMidi.division,
                midiTimeBased = renderSettings.TimeBased,
                midiTimeSignature = CurrentMidi.timeSig,
                midiBarLength = CurrentMidi.division * CurrentMidi.timeSig.numerator / CurrentMidi.timeSig.denominator * 4
            };
        }

        void UnloadScript(Script s)
        {
            foreach (var lt in s.textures)
            {
                GL.DeleteTexture(lt.texId);
                lt.texId = -1;
            }
            foreach (var lt in s.fonts)
            {
                lt.engine.Dispose();
                lt.engine = null;
            }
            if (s.hasPostRender) s.instance.RenderDispose();
        }

        void LoadScript(Script s)
        {
            CopyScriptValues();
            foreach (var lt in s.textures)
            {
                lt.texId = GL.GenTexture();
                loadImage(lt.bitmap, lt.texId, lt.looped, lt.linear);
            }
            foreach (var lt in s.fonts)
            {
                lt.engine = new GLTextEngine();
                if(lt.charMap == null)
                {
                    lt.engine.SetFont(lt.fontName, (System.Drawing.FontStyle)lt.fontStyle, lt.fontPixelSize);
                }
                else
                {
                    lt.engine.SetFont(lt.fontName, (System.Drawing.FontStyle)lt.fontStyle, lt.fontPixelSize, lt.charMap);
                }
            }
            if (s.hasPreRender) s.instance.RenderInit(GetRenderOptions());
            CopyScriptValues();
        }

        void CheckScript(IEnumerable<Note> notes = null)
        {
            if (settings.lastScriptChangeTime > lastScriptChangeTime)
            {
                if (currScript != null)
                {
                    if (notes != null) foreach (var n in notes) n.meta = null;
                    UnloadScript(currScript);
                }
                currScript = settings.currScript;
                lastScriptChangeTime = settings.lastScriptChangeTime;
                if (currScript != null) LoadScript(currScript);
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

            UnloadScript(currScript);

            Console.WriteLine("Disposed of ScriptedRender");
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

            if (currScript != null)
            {
                try
                {
                    LoadScript(currScript);
                }
                catch
                {
                    currScript = null;
                    lastScriptChangeTime = 0;
                }
            }
            CheckScript();

            IO.renderQuad = RenderQuad;
            IO.renderShape = RenderShape;
            IO.forceFlush = () => FlushQuadBuffer(false);
            IO.selectTexShader = (s) =>
            {
                if (currentShader != s) FlushQuadBuffer(false);
                if (s == TextureShaders.Normal) GL.UseProgram(quadShader);
                if (s == TextureShaders.Inverted) GL.UseProgram(inverseQuadShader);
                if (s == TextureShaders.Hybrid) GL.UseProgram(evenquadShader);
                currentShader = s;
            };
            IO.setBlendFunc = (f) =>
            {
                if (currentBlendFunc != f) FlushQuadBuffer(false);
                if (f == BlendFunc.Mix) OpenTK.Graphics.OpenGL4.GL.BlendFuncSeparate(OpenTK.Graphics.OpenGL4.BlendingFactorSrc.SrcAlpha, OpenTK.Graphics.OpenGL4.BlendingFactorDest.OneMinusSrcAlpha, OpenTK.Graphics.OpenGL4.BlendingFactorSrc.One, OpenTK.Graphics.OpenGL4.BlendingFactorDest.One);
                if (f == BlendFunc.Add) OpenTK.Graphics.OpenGL4.GL.BlendFuncSeparate(OpenTK.Graphics.OpenGL4.BlendingFactorSrc.SrcAlpha, OpenTK.Graphics.OpenGL4.BlendingFactorDest.One, OpenTK.Graphics.OpenGL4.BlendingFactorSrc.One, OpenTK.Graphics.OpenGL4.BlendingFactorDest.One);
                currentBlendFunc = f;
            };
            IO.getTextSize = (Font f, string t) =>
            {
                var bb = f.engine.GetBoundBox(t);
                return bb.Width / (double)bb.Height / renderSettings.PixelWidth * renderSettings.PixelHeight;
            };
            IO.renderText = (double left, double bottom, double height, Color4 color, Font f, string text) =>
            {
                if (text.Contains("\n")) throw new Exception("New line characters not allowed when rendering text (yet)");

                FlushQuadBuffer(false);

                var size = f.engine.GetBoundBox(text);
                Matrix4 transform = Matrix4.Identity;
                transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / renderSettings.PixelWidth * renderSettings.PixelHeight / f.fontPixelSize * (float)height, -1.0f / f.fontPixelSize * (float)height, 1.0f));
                transform = Matrix4.Mult(transform, Matrix4.CreateTranslation((float)left * 2 - 1, (float)(bottom + height * 0.7) * 2 - 1, 0));
                
                f.engine.Render(text, transform, color);

                if (currentShader == TextureShaders.Normal) GL.UseProgram(quadShader);
                if (currentShader == TextureShaders.Inverted) GL.UseProgram(inverseQuadShader);
                if (currentShader == TextureShaders.Hybrid) GL.UseProgram(evenquadShader);
            };

            Initialized = true;
            Console.WriteLine("Initialised ScriptedRender");
        }

        void CopyScriptValues()
        {
            if (currScript == null) return;
            if (currScript.hasManualNoteDelete) ManualNoteDelete = (bool)currScript.instance.ManualNoteDelete;
            else ManualNoteDelete = false;
            if (currScript.hasCollectorOffset) NoteCollectorOffset = (double)currScript.instance.NoteCollectorOffset;
            else NoteCollectorOffset = 0;
            if (currScript.hasNoteCount) LastNoteCount = (long)currScript.instance.LastNoteCount;
            else LastNoteCount = 0;
            if (currScript.hasNoteScreenTime) NoteScreenTime = (double)currScript.instance.NoteScreenTime;
            else NoteScreenTime = settings.deltaTimeOnScreen;
        }

        public void RenderFrame(FastList<Note> notes, double midiTime, int finalCompositeBuff)
        {
            CheckScript(notes);

            if (currScript == null || currScript.error) return;


            GL.Enable(EnableCap.Blend);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.Enable(EnableCap.Texture2D);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.BlendEquationSeparate(BlendEquationMode.FuncAdd, BlendEquationMode.Max);
            OpenTK.Graphics.OpenGL4.GL.BlendEquationSeparate(OpenTK.Graphics.OpenGL4.BlendEquationMode.FuncAdd, OpenTK.Graphics.OpenGL4.BlendEquationMode.FuncAdd);
            OpenTK.Graphics.OpenGL4.GL.BlendFuncSeparate(OpenTK.Graphics.OpenGL4.BlendingFactorSrc.SrcAlpha, OpenTK.Graphics.OpenGL4.BlendingFactorDest.OneMinusSrcAlpha, OpenTK.Graphics.OpenGL4.BlendingFactorSrc.One, OpenTK.Graphics.OpenGL4.BlendingFactorDest.One);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, finalCompositeBuff);
            GL.Viewport(0, 0, renderSettings.PixelWidth, renderSettings.PixelHeight);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            #region Vars
            for (int i = 0; i < 12; i++) activeTexIds[i] = -1;
            currentShader = TextureShaders.Normal;
            currentBlendFunc = BlendFunc.Mix;

            #endregion

            GL.UseProgram(quadShader);
            currentShader = TextureShaders.Normal;

            var options = GetRenderOptions(midiTime);

            CopyScriptValues();
            currScript.instance.Render((IEnumerable<Note>)notes, options);
            CopyScriptValues();

            FlushQuadBuffer(false);

            OpenTK.Graphics.OpenGL4.GL.BlendEquationSeparate(OpenTK.Graphics.OpenGL4.BlendEquationMode.FuncAdd, OpenTK.Graphics.OpenGL4.BlendEquationMode.FuncAdd);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Disable(EnableCap.Blend);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.DisableClientState(ArrayCap.TextureCoordArray);
            GL.Disable(EnableCap.Texture2D);

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.DisableVertexAttribArray(2);
            GL.DisableVertexAttribArray(3);
        }

        void RenderQuad(double left, double top, double right, double bottom, Color4 topLeft, Color4 topRight, Color4 bottomRight, Color4 bottomLeft, Texture tex, double uvLeft, double uvTop, double uvRight, double uvBottom)
        {
            int texSlot = -1;
            if (tex != null) texSlot = FindTexSlot(tex.texId);

            int pos = quadBufferPos * 8;
            quadVertexbuff[pos++] = left;
            quadVertexbuff[pos++] = top;
            quadVertexbuff[pos++] = right;
            quadVertexbuff[pos++] = top;
            quadVertexbuff[pos++] = right;
            quadVertexbuff[pos++] = bottom;
            quadVertexbuff[pos++] = left;
            quadVertexbuff[pos++] = bottom;

            pos = quadBufferPos * 16;
            quadColorbuff[pos++] = topLeft.R;
            quadColorbuff[pos++] = topLeft.G;
            quadColorbuff[pos++] = topLeft.B;
            quadColorbuff[pos++] = topLeft.A;
            quadColorbuff[pos++] = topRight.R;
            quadColorbuff[pos++] = topRight.G;
            quadColorbuff[pos++] = topRight.B;
            quadColorbuff[pos++] = topRight.A;
            quadColorbuff[pos++] = bottomRight.R;
            quadColorbuff[pos++] = bottomRight.G;
            quadColorbuff[pos++] = bottomRight.B;
            quadColorbuff[pos++] = bottomRight.A;
            quadColorbuff[pos++] = bottomLeft.R;
            quadColorbuff[pos++] = bottomLeft.G;
            quadColorbuff[pos++] = bottomLeft.B;
            quadColorbuff[pos++] = bottomLeft.A;

            pos = quadBufferPos * 8;
            quadUVbuff[pos++] = uvLeft;
            quadUVbuff[pos++] = uvTop;
            quadUVbuff[pos++] = uvRight;
            quadUVbuff[pos++] = uvTop;
            quadUVbuff[pos++] = uvRight;
            quadUVbuff[pos++] = uvBottom;
            quadUVbuff[pos++] = uvLeft;
            quadUVbuff[pos++] = uvBottom;

            pos = quadBufferPos * 4;
            quadTexIDbuff[pos++] = texSlot;
            quadTexIDbuff[pos++] = texSlot;
            quadTexIDbuff[pos++] = texSlot;
            quadTexIDbuff[pos++] = texSlot;
            quadBufferPos++;
            FlushQuadBuffer(true);
        }

        void RenderShape(Vector2d v1, Vector2d v2, Vector2d v3, Vector2d v4, Color4 c1, Color4 c2, Color4 c3, Color4 c4, Texture tex, Vector2d uv1, Vector2d uv2, Vector2d uv3, Vector2d uv4)
        {
            int texSlot = -1;
            if (tex != null) texSlot = FindTexSlot(tex.texId);

            int pos = quadBufferPos * 8;
            quadVertexbuff[pos++] = v1.X;
            quadVertexbuff[pos++] = v1.Y;
            quadVertexbuff[pos++] = v2.X;
            quadVertexbuff[pos++] = v2.Y;
            quadVertexbuff[pos++] = v3.X;
            quadVertexbuff[pos++] = v3.Y;
            quadVertexbuff[pos++] = v4.X;
            quadVertexbuff[pos++] = v4.Y;

            pos = quadBufferPos * 16;
            quadColorbuff[pos++] = c1.R;
            quadColorbuff[pos++] = c1.G;
            quadColorbuff[pos++] = c1.B;
            quadColorbuff[pos++] = c1.A;
            quadColorbuff[pos++] = c2.R;
            quadColorbuff[pos++] = c2.G;
            quadColorbuff[pos++] = c2.B;
            quadColorbuff[pos++] = c2.A;
            quadColorbuff[pos++] = c3.R;
            quadColorbuff[pos++] = c3.G;
            quadColorbuff[pos++] = c3.B;
            quadColorbuff[pos++] = c3.A;
            quadColorbuff[pos++] = c4.R;
            quadColorbuff[pos++] = c4.G;
            quadColorbuff[pos++] = c4.B;
            quadColorbuff[pos++] = c4.A;

            pos = quadBufferPos * 8;
            quadUVbuff[pos++] = uv1.X;
            quadUVbuff[pos++] = uv1.Y;
            quadUVbuff[pos++] = uv2.X;
            quadUVbuff[pos++] = uv2.Y;
            quadUVbuff[pos++] = uv3.X;
            quadUVbuff[pos++] = uv3.Y;
            quadUVbuff[pos++] = uv4.X;
            quadUVbuff[pos++] = uv4.Y;

            pos = quadBufferPos * 4;
            quadTexIDbuff[pos++] = texSlot;
            quadTexIDbuff[pos++] = texSlot;
            quadTexIDbuff[pos++] = texSlot;
            quadTexIDbuff[pos++] = texSlot;
            quadBufferPos++;
            FlushQuadBuffer(true);
        }

        int FindTexSlot(int id)
        {
            for (int i = 0; i < 12; i++)
            {
                if (activeTexIds[i] == id) return i;
                if (activeTexIds[i] == -1)
                {
                    activeTexIds[i] = id;
                    return i;
                }
            }
            FlushQuadBuffer(false);
            activeTexIds[0] = id;
            return 0;
        }

        void FlushQuadBuffer(bool check = true, bool forceShader = true)
        {
            if (quadBufferPos < quadBufferLength && check) return;
            if (quadBufferPos == 0) return;

            if (forceShader)
            {
                if (currentShader == TextureShaders.Normal) GL.UseProgram(quadShader);
                if (currentShader == TextureShaders.Inverted) GL.UseProgram(inverseQuadShader);
                if (currentShader == TextureShaders.Hybrid) GL.UseProgram(evenquadShader);
            }

            for (int i = 0; i < 12 && activeTexIds[i] != -1; i++)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + i);
                GL.BindTexture(TextureTarget.Texture2D, activeTexIds[i]);
                activeTexIds[i] = -1;
            }

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
            for (int i = 0; i < 12; i++) activeTexIds[i] = -1;
        }

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
