using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZenithEngine;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace MIDITrailRender
{
    public class Render : IPluginRender
    {
        #region PreviewConvert
        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
        #endregion

        public string Name => "MIDITrail+";
        public string Description => "Clone of the popular tool MIDITrail for black midi rendering. Added exclusive bonus features, and less buggy. Extremely customisable.";
        public string LanguageDictName { get; } = "miditrail";

        public bool Initialized { get; set; } = false;

        public ImageSource PreviewImage { get; private set; }

        #region Shaders
        string whiteKeyShaderVert = @"#version 330 core

layout(location=0) in vec3 in_position;
layout(location=1) in float in_brightness;
layout(location=2) in float blend_fac;

out vec4 v2f_color;

uniform mat4 MVP;
uniform vec4 coll;
uniform vec4 colr;

void main()
{
    gl_Position = MVP * vec4(in_position, 1.0);
    v2f_color = vec4((coll.xyz * blend_fac + colr.xyz * (1 - blend_fac)) * in_brightness, 1);
}
";
        string whiteKeyShaderFrag = @"#version 330 core

in vec4 v2f_color;
layout (location=0) out vec4 out_color;

void main()
{
    out_color = v2f_color;
}
";

        string blackKeyShaderVert = @"#version 330 core

layout(location=0) in vec3 in_position;
layout(location=1) in float in_brightness;
layout(location=2) in float blend_fac;

out vec4 v2f_color;

uniform mat4 MVP;
uniform vec4 coll;
uniform vec4 colr;

void main()
{
    gl_Position = MVP * vec4(in_position, 1.0);
    v2f_color = vec4(1 - in_brightness + (coll.xyz * blend_fac + colr.xyz * (1 - blend_fac)) * in_brightness, 1);
}
";
        string blackKeyShaderFrag = @"#version 330 core

in vec4 v2f_color;
layout (location=0) out vec4 out_color;

void main()
{
    out_color = v2f_color;
}
";
        string noteShaderVert = @"#version 330 core

layout(location=0) in vec3 in_position;
layout(location=1) in vec4 in_color;
layout(location=2) in float in_shade;

out vec4 v2f_color;

uniform mat4 MVP;

void main()
{
    gl_Position = MVP * vec4(in_position, 1.0);
    v2f_color = vec4(in_color.xyz + in_shade, in_color.w);
}
";
        string noteShaderFrag = @"#version 330 core

in vec4 v2f_color;
layout (location=0) out vec4 out_color;

void main()
{
    out_color = v2f_color;
}
";
        string circleShaderVert = @"#version 330 core

layout(location=0) in vec3 in_position;
layout(location=1) in vec4 in_color;
layout(location=2) in vec2 in_uv;

out vec4 v2f_color;
out vec2 uv;

uniform mat4 MVP;

void main()
{
    gl_Position = MVP * vec4(in_position, 1.0);
    v2f_color = in_color;
    uv = in_uv;
}
";
        string circleShaderFrag = @"#version 330 core

in vec4 v2f_color;
in vec2 uv;

uniform sampler2D textureSampler;

layout (location=0) out vec4 out_color;

void main()
{
    out_color = v2f_color;
    out_color.w *=  texture2D( textureSampler, uv ).w;
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

        int whiteKeyShader;
        int blackKeyShader;
        int noteShader;
        int circleShader;

        int uWhiteKeyMVP;
        int uWhiteKeycoll;
        int uWhiteKeycolr;

        int uBlackKeyMVP;
        int uBlackKeycoll;
        int uBlackKeycolr;

        int uNoteMVP;

        int uCircleMVP;

        public bool ManualNoteDelete => false;

        public double Tempo { get; set; }

        public double NoteScreenTime => settings.viewdist * settings.deltaTimeOnScreen;

        public NoteColor[][] NoteColors { get; set; }

        public long LastNoteCount { get; private set; }

        SettingsCtrl settingsCtrl;
        public Control SettingsControl => settingsCtrl;

        public double NoteCollectorOffset
        {
            get
            {
                if (settings.eatNotes) return 0;
                return -settings.deltaTimeOnScreen * settings.viewback;
            }
        }

        bool[] blackKeys = new bool[257];
        int[] keynum = new int[257];

        RenderStatus renderSettings;
        Settings settings;

        int buffer3dtex;
        int buffer3dbuf;
        int buffer3dbufdepth;

        int[] whiteKeyVert = new int[7 * 3];
        int whiteKeyCol;
        int whiteKeyIndx;
        int whiteKeyBlend;

        int blackKeyVert;
        int blackKeyCol;
        int blackKeyIndx;
        int blackKeyBlend;

        int noteVert;
        int noteCol;
        int noteIndx;
        int noteShade;

        int noteBuffLen = 2048 * 256;

        double[] noteVertBuff;
        float[] noteColBuff;
        float[] noteShadeBuff;
        int[] noteIndxBuff;

        int noteBuffPos = 0;

        int circleVert;
        int circleColor;
        int circleUV;
        int circleIndx;

        double[] circleVertBuff;
        float[] circleColorBuff;
        double[] circleUVBuff;
        int[] circleIndxBuff;

        int circleBuffPos = 0;

        long lastAuraTexChange;
        int auraTex;

        void loadImage(Bitmap image, int texID)
        {
            GL.BindTexture(TextureTarget.Texture2D, texID);
            BitmapData data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            image.UnlockBits(data);
        }

        public void Dispose()
        {
            lastAuraTexChange = 0;
            GL.DeleteTexture(auraTex);

            GL.DeleteFramebuffer(buffer3dbuf);
            GL.DeleteTexture(buffer3dtex);
            GL.DeleteRenderbuffer(buffer3dbufdepth);

            GL.DeleteBuffers(7 * 3, whiteKeyVert);
            GL.DeleteBuffers(11, new int[] {
                whiteKeyCol, blackKeyVert, blackKeyCol,
                whiteKeyIndx, blackKeyIndx, whiteKeyBlend, blackKeyBlend,
                noteVert, noteCol, noteIndx, noteShade
            });
            GL.DeleteProgram(whiteKeyShader);
            GL.DeleteProgram(blackKeyShader);
            GL.DeleteProgram(noteShader);
            GL.DeleteProgram(circleShader);

            noteVertBuff = null;
            noteColBuff = null;
            noteShadeBuff = null;
            noteIndxBuff = null;

            circleVertBuff = null;
            circleColorBuff = null;
            circleUVBuff = null;
            circleIndxBuff = null;

            util.Dispose();
            Initialized = false;
            Console.WriteLine("Disposed of MIDITrailRender");
        }

        Util util;
        public Render(RenderStatus settings)
        {
            this.settings = new Settings();
            this.renderSettings = settings;
            settingsCtrl = new SettingsCtrl(this.settings);
            ((SettingsCtrl)SettingsControl).PaletteChanged += () => { ReloadTrackColors(); };
            PreviewImage = BitmapToImageSource(Properties.Resources.preview);
            for (int i = 0; i < blackKeys.Length; i++) blackKeys[i] = isBlackNote(i);
            int b = 0;
            int w = 0;
            for (int i = 0; i < keynum.Length; i++)
            {
                if (blackKeys[i]) keynum[i] = b++;
                else keynum[i] = w++;
            }
        }

        void ReloadAuraTexture()
        {
            loadImage(settingsCtrl.auraselect.SelectedImage, auraTex);
            lastAuraTexChange = settingsCtrl.auraselect.lastSetTime;
        }

        int whiteKeyBufferLen = 0;
        int blackKeyBufferLen = 0;
        public void Init()
        {
            whiteKeyShader = MakeShader(whiteKeyShaderVert, whiteKeyShaderFrag);
            blackKeyShader = MakeShader(blackKeyShaderVert, blackKeyShaderFrag);
            noteShader = MakeShader(noteShaderVert, noteShaderFrag);
            circleShader = MakeShader(circleShaderVert, circleShaderFrag);

            uWhiteKeyMVP = GL.GetUniformLocation(whiteKeyShader, "MVP");
            uWhiteKeycoll = GL.GetUniformLocation(whiteKeyShader, "coll");
            uWhiteKeycolr = GL.GetUniformLocation(whiteKeyShader, "colr");

            uBlackKeyMVP = GL.GetUniformLocation(blackKeyShader, "MVP");
            uBlackKeycoll = GL.GetUniformLocation(blackKeyShader, "coll");
            uBlackKeycolr = GL.GetUniformLocation(blackKeyShader, "colr");

            uNoteMVP = GL.GetUniformLocation(noteShader, "MVP");
            uCircleMVP = GL.GetUniformLocation(circleShader, "MVP");

            GLUtils.GenFrameBufferTexture3d(renderSettings.PixelWidth, renderSettings.PixelHeight, out buffer3dbuf, out buffer3dtex, out buffer3dbufdepth);

            util = new Util();
            Initialized = true;
            Console.WriteLine("Initialised MIDITrailRender");

            GL.GenBuffers(7 * 3, whiteKeyVert);
            whiteKeyCol = GL.GenBuffer();
            whiteKeyIndx = GL.GenBuffer();
            whiteKeyBlend = GL.GenBuffer();

            blackKeyVert = GL.GenBuffer();
            blackKeyCol = GL.GenBuffer();
            blackKeyIndx = GL.GenBuffer();
            blackKeyBlend = GL.GenBuffer();

            noteVert = GL.GenBuffer();
            noteCol = GL.GenBuffer();
            noteIndx = GL.GenBuffer();
            noteShade = GL.GenBuffer();

            circleVert = GL.GenBuffer();
            circleColor = GL.GenBuffer();
            circleUV = GL.GenBuffer();
            circleIndx = GL.GenBuffer();

            noteVertBuff = new double[noteBuffLen * 4 * 3];
            noteColBuff = new float[noteBuffLen * 4 * 4];
            noteShadeBuff = new float[noteBuffLen * 4];

            noteIndxBuff = new int[noteBuffLen * 4];

            circleVertBuff = new double[256 * 4 * 3];
            circleColorBuff = new float[256 * 4 * 4];
            circleUVBuff = new double[256 * 4 * 2];
            circleIndxBuff = new int[256 * 4];

            for (int i = 0; i < noteIndxBuff.Length; i++) noteIndxBuff[i] = i;
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, noteIndx);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(noteIndxBuff.Length * 4),
                noteIndxBuff,
                BufferUsageHint.StaticDraw);

            for (int i = 0; i < circleIndxBuff.Length; i++) circleIndxBuff[i] = i;
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, circleIndx);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(circleIndxBuff.Length * 4),
                circleIndxBuff,
                BufferUsageHint.StaticDraw);

            auraTex = GL.GenTexture();

            ReloadAuraTexture();

            float whitekeylen = 5.0f;
            float blackkeylen = 6.9f;
            float lenfac = 0.69f;
            float blackKeyEnd = whitekeylen * lenfac;
            float left = 0.3f;
            float right = 0.7f;

            #region White Key Model

            double[] verts = new double[] {
                //front
                0, 0, -whitekeylen,//0
                0, 0.7, -whitekeylen,
                1, 0.7, -whitekeylen,
                1, 0, -whitekeylen,
                //front dark
                0, 0.7, -whitekeylen,//12
                0, 1, -whitekeylen,
                1, 1, -whitekeylen,
                1, 0.7, -whitekeylen,
                //top
                1, 1, -blackKeyEnd,//24
                1, 1, -whitekeylen,
                0, 1, -whitekeylen,
                0, 1, -blackKeyEnd,
                //notch
                0, 1, -whitekeylen,
                0.03, 0.95, -whitekeylen - 0.1,//36
                0.97, 0.95, -whitekeylen - 0.1,
                1, 1, -whitekeylen,

                0, 0.90, -whitekeylen - 0.07,//48
                0.03, 0.95, -whitekeylen - 0.1,
                0.97, 0.95, -whitekeylen - 0.1,
                1, 0.90, -whitekeylen - 0.07,
                //left
                0, 1, -blackKeyEnd,//60
                0, 1, -whitekeylen,
                0, 0, -whitekeylen,
                0, 0, -blackKeyEnd,
                //right
                1, 1, -blackKeyEnd,//72
                1, 1, -whitekeylen,
                1, 0, -whitekeylen,
                1, 0, -blackKeyEnd,
                //back
                left, 0, 0,//84
                left, 1, 0,
                right, 1, 0,
                right, 0, 0,

                //left2
                left, 1, 0,//96
                left, 1, -blackKeyEnd,
                left, 0, -blackKeyEnd,
                left, 0, 0,
                //right2
                right, 1, 0,//108
                right, 1, -blackKeyEnd,
                right, 0, -blackKeyEnd,
                right, 0, 0,
                //top
                left, 1, 0,//120
                left, 1, -blackKeyEnd,
                right, 1, -blackKeyEnd,
                right, 1, 0,
                //left inner
                0, 1, -blackKeyEnd,//96
                left, 1, -blackKeyEnd,
                left, 0, -blackKeyEnd,
                0, 0, -blackKeyEnd,
                //right inner
                1, 1, -blackKeyEnd,//108
                right, 1, -blackKeyEnd,
                right, 0, -blackKeyEnd,
                1, 0, -blackKeyEnd,
            };
            int[] rightIndxs = new int[] {
                7 * 12 + 6, 7 * 12 + 9,
                9 * 12, 9 * 12 + 3, 9 * 12 + 6, 9 * 12 + 9,
                10 * 12 + 6, 10 * 12 + 9,
                12 * 12 + 3, 12 * 12 + 6,
            };
            int[] leftIndxs = new int[] {
                7 * 12, 7 * 12 + 3,
                8 * 12, 8 * 12 + 3, 8 * 12 + 6, 8 * 12 + 9,
                10 * 12, 10 * 12 + 3,
                11 * 12 + 3, 11 * 12 + 6,
            };

            float[] cols = new float[] {
                //front
                0.7f,
                0.8f,
                0.8f,
                0.7f,
                //front dark
                0.6f,
                0.6f,
                0.6f,
                0.6f,
                //top
                1,
                1,
                1,
                1,
                //notch
                1f,
                0.9f,
                0.9f,
                1f,

                0.9f,
                0.9f,
                0.9f,
                0.9f,
                //left
                0.6f,
                0.6f,
                0.6f,
                0.6f,
                //right
                0.6f,
                0.6f,
                0.6f,
                0.6f,
                //back
                0.8f,
                0.8f,
                0.8f,
                0.8f,
                //left2
                0.6f,
                0.6f,
                0.6f,
                0.6f,
                //right2
                0.6f,
                0.6f,
                0.6f,
                0.6f,
                //top2
                1,
                1,
                1,
                1,
                //left inner
                0.6f,
                0.6f,
                0.6f,
                0.6f,
                //right inner
                0.6f,
                0.6f,
                0.6f,
                0.6f,
            };
            float[] blend = new float[] {
                //front
                1, 1, 1, 1,
                //front dark
                1, 1, 1, 1,
                //top
                lenfac, 1, 1, lenfac,
                //notch
                1, 1, 1, 1,

                1, 1, 1, 1,
                //left
                lenfac, 1, 1, lenfac,
                //right
                lenfac, 1, 1, lenfac,
                
                //back
                0, 0, 0, 0,
                
                //left2
                0, lenfac, lenfac, 0,
                //right2
                0, lenfac, lenfac, 0,
                //top2
                0, lenfac, lenfac, 0,

                //left inner
                lenfac, lenfac, lenfac, lenfac,
                //right inner
                lenfac, lenfac, lenfac, lenfac,
            };
            int[] indexes = new int[52];
            for (int i = 0; i < indexes.Length; i++) indexes[i] = i;
            whiteKeyBufferLen = indexes.Length;

            GL.BindBuffer(BufferTarget.ArrayBuffer, whiteKeyCol);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(cols.Length * 4),
                cols,
                BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, whiteKeyBlend);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(blend.Length * 4),
                blend,
                BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, whiteKeyIndx);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(indexes.Length * 4),
                indexes,
                BufferUsageHint.StaticDraw);

            float[] offsets = new float[] {
                0, 0.6f,
                0.2f, 0.8f,
                0.4f, 1,
                0, 0.55f,
                0.15f, 0.7f,
                0.3f, 0.85f,
                0.45f, 1,
            };
            for (int i = 0; i < 7; i++)
            {
                foreach (var j in leftIndxs) verts[j] = offsets[i * 2];
                foreach (var j in rightIndxs) verts[j] = offsets[i * 2 + 1];
                GL.BindBuffer(BufferTarget.ArrayBuffer, whiteKeyVert[i]);
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    (IntPtr)(verts.Length * 8),
                    verts,
                    BufferUsageHint.StaticDraw);
            }
            for (int i = 0; i < 7; i++)
            {
                foreach (var j in leftIndxs) verts[j] = offsets[i * 2];
                foreach (var j in rightIndxs) verts[j] = 1;
                GL.BindBuffer(BufferTarget.ArrayBuffer, whiteKeyVert[i + 7]);
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    (IntPtr)(verts.Length * 8),
                    verts,
                    BufferUsageHint.StaticDraw);
            }
            for (int i = 0; i < 7; i++)
            {
                foreach (var j in leftIndxs) verts[j] = 0;
                foreach (var j in rightIndxs) verts[j] = offsets[i * 2 + 1];
                GL.BindBuffer(BufferTarget.ArrayBuffer, whiteKeyVert[i + 14]);
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    (IntPtr)(verts.Length * 8),
                    verts,
                    BufferUsageHint.StaticDraw);
            }
            #endregion

            #region Black Key Model
            verts = new double[] {
                //front
                0, 0, -blackkeylen,
                0, 1, -blackkeylen + 1,
                1, 1, -blackkeylen + 1,
                1, 0, -blackkeylen,
                //top
                0, 1, -blackkeylen + 1,
                0, 1, -0,
                1, 1, -0,
                1, 1, -blackkeylen + 1,
                //left
                0, 0, 0,
                0, 0, -blackkeylen,
                0, 1, -blackkeylen + 1,
                0, 1, 0,
                //right
                1, 0, 0,
                1, 0, -blackkeylen,
                1, 1, -blackkeylen + 1,
                1, 1, 0,
                //back
                0, -1, 0,
                0, 1, 0,
                1, 1, 0,
                1, -1, 0,
                //left2
                0, 0, 0,
                0, 0, -blackkeylen,
                0, -1, -blackkeylen,
                0, -1, 0,
                //right2
                1, 0, 0,
                1, 0, -blackkeylen,
                1, -1, -blackkeylen,
                1, -1, 0,
                //front2
                0, 0, -blackkeylen,
                0, -1, -blackkeylen,
                1, -1, -blackkeylen,
                1, 0, -blackkeylen,
            };

            cols = new float[] {
                //front
                0.9f,
                0.95f,
                0.95f,
                0.9f,                
                //top
                1f,
                0.94f,
                0.94f,
                1f,              
                //left
                0.8f,
                0.8f,
                0.9f,
                0.8f,        
                //right
                0.8f,
                0.8f,
                0.9f,
                0.8f,        
                //back
                0.9f,
                0.9f,
                0.9f,
                0.9f,       
                //left2
                0.8f,
                0.8f,
                0.8f,
                0.8f,        
                //right2
                0.8f,
                0.8f,
                0.8f,
                0.8f,
                //front2
                0.9f,
                1f,
                1f,
                0.9f,
            };
            blend = new float[] {
                //front
                1, 1, 1, 1,
                //top
                1, 0, 0, 1,
                //left
                0, 1, 1, 0,
                //right
                0, 1, 1, 0,
                //back
                0, 0, 0, 0,
                //left2
                0, 1, 1, 0,
                //right2
                0, 1, 1, 0,
                //front2
                1, 1, 1, 1,
            };

            indexes = new int[32];
            for (int i = 0; i < indexes.Length; i++) indexes[i] = i;
            blackKeyBufferLen = indexes.Length;

            GL.BindBuffer(BufferTarget.ArrayBuffer, blackKeyVert);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(verts.Length * 8),
                verts,
                BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, blackKeyCol);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(cols.Length * 4),
                cols,
                BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, blackKeyBlend);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(blend.Length * 4),
                blend,
                BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, blackKeyIndx);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(indexes.Length * 4),
                indexes,
                BufferUsageHint.StaticDraw);
            #endregion

            keyPressFactor = new double[257];
        }

        #region Vars
        int firstNote;
        int lastNote;
        bool sameWidth;
        double deltaTimeOnScreen;
        double noteDownSpeed;
        double noteUpSpeed;
        bool blockNotes;
        bool useVel;
        bool changeSize;
        bool changeTint;
        double tempoFrameStep;
        bool eatNotes;
        float auraStrength;
        bool auraEnabled;
        bool lightShade;
        bool tiltKeys;

        double fov;
        double aspect;
        double viewdist;
        double viewback;
        double viewheight;
        double viewpan;
        double viewoffset;
        double camAng;
        double camRot;
        double camSpin;

        double circleRadius;
        #endregion

        Color4[] keyColors = new Color4[514];
        double[] x1array = new double[257];
        double[] wdtharray = new double[257];
        double[] keyPressFactor = new double[257];
        double[] auraSize = new double[256];
        public void RenderFrame(FastList<Note> notes, double midiTime, int finalCompositeBuff)
        {
            if (lastAuraTexChange != settingsCtrl.auraselect.lastSetTime) ReloadAuraTexture();
            GL.Enable(EnableCap.Blend);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Always);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer3dbuf);
            GL.Viewport(0, 0, renderSettings.PixelWidth, renderSettings.PixelHeight);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            long nc = 0;
            firstNote = settings.firstNote;
            lastNote = settings.lastNote;
            sameWidth = settings.sameWidthNotes;
            deltaTimeOnScreen = NoteScreenTime;
            noteDownSpeed = settings.noteDownSpeed;
            noteUpSpeed = settings.noteUpSpeed;
            blockNotes = settings.boxNotes;
            useVel = settings.useVel;
            changeSize = settings.notesChangeSize;
            changeTint = settings.notesChangeTint;
            tempoFrameStep = 1 / (60000000 / Tempo / CurrentMidi.division) * (1000000.0 / renderSettings.FPS);
            eatNotes = settings.eatNotes;
            auraStrength = (float)settings.auraStrength;
            auraEnabled = settings.auraEnabled;
            lightShade = settings.lightShade;
            tiltKeys = settings.tiltKeys;

            fov = settings.FOV;
            aspect = (double)renderSettings.PixelWidth / renderSettings.PixelHeight;
            viewdist = settings.viewdist;
            viewback = settings.viewback;
            viewheight = settings.viewHeight;
            viewpan = settings.viewPan;
            viewoffset = -settings.viewOffset;
            camAng = settings.camAng;
            camRot = settings.camRot;
            camSpin = settings.camSpin;
            fov /= 1;
            for (int i = 0; i < 514; i++) keyColors[i] = Color4.Transparent;
            for (int i = 0; i < 256; i++) auraSize[i] = 0;
            for (int i = 0; i < keyPressFactor.Length; i++) keyPressFactor[i] = Math.Max(keyPressFactor[i] / 1.05 - noteUpSpeed, 0);
            float wdth;
            double wdthd;
            float r, g, b, a, r2, g2, b2, a2;
            double x1d;
            double x2d;
            double y1;
            double y2;
            Matrix4 mvp;
            if (settings.sameWidthNotes)
            {
                for (int i = 0; i < 257; i++)
                {
                    x1array[i] = (float)(i - firstNote) / (lastNote - firstNote);
                    wdtharray[i] = 1.0f / (lastNote - firstNote);
                }
                circleRadius = 1.0f / (lastNote - firstNote);
            }
            else
            {
                double knmfn = keynum[firstNote];
                double knmln = keynum[lastNote - 1];
                if (blackKeys[firstNote]) knmfn = keynum[firstNote - 1] + 0.5;
                if (blackKeys[lastNote - 1]) knmln = keynum[lastNote] - 0.5;
                for (int i = 0; i < 257; i++)
                {
                    if (!blackKeys[i])
                    {
                        x1array[i] = (float)(keynum[i] - knmfn) / (knmln - knmfn + 1);
                        wdtharray[i] = 1.0f / (knmln - knmfn + 1);
                    }
                    else
                    {
                        int _i = i + 1;
                        wdth = (float)(0.6f / (knmln - knmfn + 1));
                        int bknum = keynum[i] % 5;
                        double offset = wdth / 2;
                        if (bknum == 0)
                        {
                            offset *= 1.4;
                        }
                        else if (bknum == 1)
                        {
                            offset *= 1 / 1.4;
                        }
                        if (bknum == 2)
                        {
                            offset *= 1.5;
                        }
                        else if (bknum == 4)
                        {
                            offset *= 1 / 1.5;
                        }
                        x1array[i] = (float)(keynum[_i] - knmfn) / (knmln - knmfn + 1) - offset;
                        wdtharray[i] = wdth;
                    }
                }
                circleRadius = (float)(0.6f / (knmln - knmfn + 1));
            }


            #region Notes
            noteBuffPos = 0;
            GL.UseProgram(noteShader);

            mvp = Matrix4.Identity;
            if (settings.verticalNotes) mvp *= Matrix4.CreateRotationX(-(float)Math.PI / 2);
            mvp = mvp *
                Matrix4.CreateTranslation((float)viewpan, -(float)viewheight, -(float)viewoffset) *
                Matrix4.CreateScale(1, 1, -1) *
                Matrix4.CreateRotationZ((float)camSpin) *
                Matrix4.CreateRotationY((float)camRot) *
                Matrix4.CreateRotationX((float)camAng) *
                Matrix4.CreatePerspectiveFieldOfView((float)fov, (float)aspect, 0.01f, 400)
                ;
            GL.UniformMatrix4(uNoteMVP, false, ref mvp);

            double renderCutoff = midiTime + deltaTimeOnScreen;
            double renderStart = midiTime + NoteCollectorOffset;
            double maxAuraLen = tempoFrameStep * renderSettings.FPS;

            if (blockNotes)
            {
                foreach (Note n in notes)
                {
                    if (n.end >= renderStart || !n.hasEnded)
                    {
                        if (n.start < renderCutoff)
                        {
                            nc++;
                            int k = n.key;
                            if (!(k >= firstNote && k < lastNote)) continue;
                            Color4 coll = n.color.Left;
                            Color4 colr = n.color.Right;
                            float shade = 0;
                            x1d = x1array[k] - 0.5;
                            wdthd = wdtharray[k];
                            y1 = n.end - midiTime;
                            y2 = n.start - midiTime;
                            if (eatNotes && y1 < 0) y1 = 0;
                            if (eatNotes && y2 < 0) y2 = 0;
                            if (!n.hasEnded)
                                y1 = viewdist * deltaTimeOnScreen;
                            y1 /= deltaTimeOnScreen / viewdist;
                            y2 /= deltaTimeOnScreen / viewdist;

                            if (x1d < -viewpan) x1d += wdthd;
                            if (n.start < midiTime && (n.end > midiTime || !n.hasEnded))
                            {
                                double factor = 0.5;
                                if (n.hasEnded)
                                {
                                    double len = n.end - n.start;
                                    double offset = n.end - midiTime;
                                    if (offset > maxAuraLen) offset = maxAuraLen;
                                    if (len > maxAuraLen) len = maxAuraLen;
                                    factor = Math.Pow(offset / len, 0.3);
                                    factor /= 2;
                                }
                                else
                                {
                                    factor = 0.5;
                                }
                                if (changeTint)
                                    shade = (float)(factor * 0.7);
                                if (changeSize)
                                {
                                    if (x1d > 0)
                                        x1d -= wdthd * 0.3 * factor;
                                    else
                                        x1d += wdthd * 0.3 * factor;
                                }
                            }
                            if (lightShade)
                                shade += 0.2f;
                            else
                                shade -= 0.3f;

                            r = coll.R;
                            g = coll.G;
                            b = coll.B;
                            a = coll.A;
                            r2 = colr.R;
                            g2 = colr.G;
                            b2 = colr.B;
                            a2 = colr.A;

                            int pos = noteBuffPos * 12;
                            noteVertBuff[pos++] = x1d;
                            noteVertBuff[pos++] = 0;
                            noteVertBuff[pos++] = y2;
                            noteVertBuff[pos++] = x1d;
                            noteVertBuff[pos++] = 0;
                            noteVertBuff[pos++] = y1;
                            noteVertBuff[pos++] = x1d;
                            noteVertBuff[pos++] = -wdthd;
                            noteVertBuff[pos++] = y1;
                            noteVertBuff[pos++] = x1d;
                            noteVertBuff[pos++] = -wdthd;
                            noteVertBuff[pos++] = y2;

                            pos = noteBuffPos * 16;
                            noteColBuff[pos++] = r;
                            noteColBuff[pos++] = g;
                            noteColBuff[pos++] = b;
                            noteColBuff[pos++] = a;
                            noteColBuff[pos++] = r;
                            noteColBuff[pos++] = g;
                            noteColBuff[pos++] = b;
                            noteColBuff[pos++] = a;
                            noteColBuff[pos++] = r2;
                            noteColBuff[pos++] = g2;
                            noteColBuff[pos++] = b2;
                            noteColBuff[pos++] = a2;
                            noteColBuff[pos++] = r2;
                            noteColBuff[pos++] = g2;
                            noteColBuff[pos++] = b2;
                            noteColBuff[pos++] = a2;

                            pos = noteBuffPos * 4;
                            noteShadeBuff[pos++] = shade;
                            noteShadeBuff[pos++] = shade;
                            noteShadeBuff[pos++] = shade;
                            noteShadeBuff[pos++] = shade;

                            noteBuffPos++;
                            FlushNoteBuffer();

                        }
                        else break;
                    }
                }

                FlushNoteBuffer(false);

                foreach (Note n in notes)
                {
                    if (n.end >= renderStart || !n.hasEnded)
                    {
                        if (n.start < renderCutoff)
                        {
                            nc++;
                            int k = n.key;
                            if (!(k >= firstNote && k < lastNote)) continue;
                            Color4 coll = n.color.Left;
                            Color4 colr = n.color.Right;
                            float shade = 0;
                            x1d = x1array[k] - 0.5;
                            wdthd = wdtharray[k];
                            x2d = x1d + wdthd;
                            y1 = n.end - midiTime;
                            y2 = n.start - midiTime;
                            if (eatNotes && y1 < 0) y1 = 0;
                            if (eatNotes && y2 < 0) y2 = 0;
                            if (!n.hasEnded)
                                y1 = viewdist * deltaTimeOnScreen;
                            y1 /= deltaTimeOnScreen / viewdist;
                            y2 /= deltaTimeOnScreen / viewdist;
                            if ((settings.verticalNotes && y2 < viewheight) || (!settings.verticalNotes && y2 < viewoffset)) y2 = y1;
                            if (n.start < midiTime && (n.end > midiTime || !n.hasEnded))
                            {
                                double factor = 0.5;
                                if (n.hasEnded)
                                {
                                    double len = n.end - n.start;
                                    double offset = n.end - midiTime;
                                    if (offset > maxAuraLen) offset = maxAuraLen;
                                    if (len > maxAuraLen) len = maxAuraLen;
                                    factor = Math.Pow(offset / len, 0.3);
                                    factor /= 2;
                                }
                                else
                                {
                                    factor = 0.5;
                                }
                                if (changeTint)
                                    shade = (float)(factor * 0.7);
                                if (changeSize)
                                {
                                    x1d -= wdthd * 0.3 * factor;
                                    x2d += wdthd * 0.3 * factor;
                                }
                            }
                            shade -= 0.2f;

                            r = coll.R;
                            g = coll.G;
                            b = coll.B;
                            a = coll.A;
                            r2 = colr.R;
                            g2 = colr.G;
                            b2 = colr.B;
                            a2 = colr.A;

                            int pos = noteBuffPos * 12;
                            noteVertBuff[pos++] = x2d;
                            noteVertBuff[pos++] = -wdthd;
                            noteVertBuff[pos++] = y2;
                            noteVertBuff[pos++] = x2d;
                            noteVertBuff[pos++] = 0;
                            noteVertBuff[pos++] = y2;
                            noteVertBuff[pos++] = x1d;
                            noteVertBuff[pos++] = 0;
                            noteVertBuff[pos++] = y2;
                            noteVertBuff[pos++] = x1d;
                            noteVertBuff[pos++] = -wdthd;
                            noteVertBuff[pos++] = y2;

                            pos = noteBuffPos * 16;
                            noteColBuff[pos++] = r;
                            noteColBuff[pos++] = g;
                            noteColBuff[pos++] = b;
                            noteColBuff[pos++] = a;
                            noteColBuff[pos++] = r;
                            noteColBuff[pos++] = g;
                            noteColBuff[pos++] = b;
                            noteColBuff[pos++] = a;
                            noteColBuff[pos++] = r2;
                            noteColBuff[pos++] = g2;
                            noteColBuff[pos++] = b2;
                            noteColBuff[pos++] = a2;
                            noteColBuff[pos++] = r2;
                            noteColBuff[pos++] = g2;
                            noteColBuff[pos++] = b2;
                            noteColBuff[pos++] = a2;

                            pos = noteBuffPos * 4;
                            noteShadeBuff[pos++] = shade;
                            noteShadeBuff[pos++] = shade;
                            noteShadeBuff[pos++] = shade;
                            noteShadeBuff[pos++] = shade;

                            noteBuffPos++;
                            FlushNoteBuffer();

                        }
                        else break;
                    }
                }
            }

            foreach (Note n in notes)
            {
                if (n.end >= renderStart || !n.hasEnded)
                {
                    if (n.start < renderCutoff)
                    {
                        unsafe
                        {
                            nc++;
                            int k = n.key;
                            if (!(k >= firstNote && k < lastNote)) continue;
                            Color4 coll = n.color.Left;
                            Color4 colr = n.color.Right;
                            float shade = 0;

                            x1d = x1array[k] - 0.5;
                            wdthd = wdtharray[k];
                            x2d = x1d + wdthd;
                            y1 = n.end - midiTime;
                            y2 = n.start - midiTime;
                            if (eatNotes && y1 < 0) y1 = 0;
                            if (eatNotes && y2 < 0) y2 = 0;
                            if (!n.hasEnded)
                                y1 = viewdist * deltaTimeOnScreen;
                            y1 /= deltaTimeOnScreen / viewdist;
                            y2 /= deltaTimeOnScreen / viewdist;

                            if (n.start < midiTime && (n.end > midiTime || !n.hasEnded))
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
                                blendfac = colr.A * 0.8f;
                                revblendfac = 1 - blendfac;
                                keyColors[k * 2 + 1] = new Color4(
                                    colr.R * blendfac + origcolr.R * revblendfac,
                                    colr.G * blendfac + origcolr.G * revblendfac,
                                    colr.B * blendfac + origcolr.B * revblendfac,
                                    1);
                                if (useVel)
                                    keyPressFactor[k] = Math.Min(1, keyPressFactor[k] + noteDownSpeed * n.vel / 127.0);
                                else
                                    keyPressFactor[k] = Math.Min(1, keyPressFactor[k] + noteDownSpeed);
                                double factor = 0;
                                double factor2 = Math.Pow(Math.Max(10 - (midiTime - n.start) / tempoFrameStep, 0), 2) / 600;
                                if (n.hasEnded)
                                {
                                    double len = n.end - n.start;
                                    double offset = n.end - midiTime;
                                    if (offset > maxAuraLen) offset = maxAuraLen;
                                    if (len > maxAuraLen) len = maxAuraLen;
                                    factor = Math.Pow(offset / len, 0.3);
                                    factor /= 2;
                                }
                                else
                                {
                                    factor = 0.5;
                                }

                                if (auraSize[k] < factor + factor2) auraSize[k] = factor + factor2;

                                if (changeTint)
                                    shade = (float)(factor * 0.7);
                                if (changeSize)
                                {
                                    x1d -= wdthd * 0.3 * factor;
                                    x2d += wdthd * 0.3 * factor;
                                }
                            }

                            r = coll.R;
                            g = coll.G;
                            b = coll.B;
                            a = coll.A;
                            r2 = colr.R;
                            g2 = colr.G;
                            b2 = colr.B;
                            a2 = colr.A;

                            int pos = noteBuffPos * 12;
                            noteVertBuff[pos++] = x2d;
                            noteVertBuff[pos++] = 0;
                            noteVertBuff[pos++] = y2;
                            noteVertBuff[pos++] = x2d;
                            noteVertBuff[pos++] = 0;
                            noteVertBuff[pos++] = y1;
                            noteVertBuff[pos++] = x1d;
                            noteVertBuff[pos++] = 0;
                            noteVertBuff[pos++] = y1;
                            noteVertBuff[pos++] = x1d;
                            noteVertBuff[pos++] = 0;
                            noteVertBuff[pos++] = y2;

                            pos = noteBuffPos * 16;
                            noteColBuff[pos++] = r;
                            noteColBuff[pos++] = g;
                            noteColBuff[pos++] = b;
                            noteColBuff[pos++] = a;
                            noteColBuff[pos++] = r;
                            noteColBuff[pos++] = g;
                            noteColBuff[pos++] = b;
                            noteColBuff[pos++] = a;
                            noteColBuff[pos++] = r2;
                            noteColBuff[pos++] = g2;
                            noteColBuff[pos++] = b2;
                            noteColBuff[pos++] = a2;
                            noteColBuff[pos++] = r2;
                            noteColBuff[pos++] = g2;
                            noteColBuff[pos++] = b2;
                            noteColBuff[pos++] = a2;

                            pos = noteBuffPos * 4;
                            noteShadeBuff[pos++] = shade;
                            noteShadeBuff[pos++] = shade;
                            noteShadeBuff[pos++] = shade;
                            noteShadeBuff[pos++] = shade;

                            noteBuffPos++;
                        }
                        FlushNoteBuffer();

                    }
                    else break;
                }
            }

            FlushNoteBuffer(false);
            noteBuffPos = 0;

            LastNoteCount = nc;
            #endregion


            if ((!settings.verticalNotes && viewoffset < 0) || (settings.verticalNotes && viewheight < 0.025))
            {
                if (auraEnabled)
                {
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                    RenderAura();
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                }
                GL.DepthFunc(DepthFunction.Less);

                RenderKeyboard();
            }
            else
            {
                GL.DepthFunc(DepthFunction.Less);
                RenderKeyboard();
                GL.DepthFunc(DepthFunction.Always);

                if (auraEnabled)
                {
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                    RenderAura();
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                }
                GL.DepthFunc(DepthFunction.Less);
            }



            GL.Disable(EnableCap.Blend);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.DepthTest);

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.DisableVertexAttribArray(2);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, finalCompositeBuff);
            GL.BindTexture(TextureTarget.Texture2D, buffer3dtex);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            util.DrawScreenQuad();
        }

        void RenderAura()
        {
            #region Aura
            double wdthd;
            float r, g, b, a, r2, g2, b2, a2;
            double x1d;
            double x2d;
            double y1;
            double y2;
            Matrix4 mvp;

            GL.UseProgram(circleShader);

            GL.BindTexture(TextureTarget.Texture2D, auraTex);

            mvp = Matrix4.Identity;
            if (settings.verticalNotes) mvp *= 
                    Matrix4.CreateRotationX(-(float)Math.PI / 2) *
                    Matrix4.CreateTranslation(0, 0.025f, 0);
            mvp *= 
                Matrix4.CreateTranslation((float)viewpan, -(float)viewheight, -(float)viewoffset) *
                Matrix4.CreateScale(1, 1, -1) *
                Matrix4.CreateRotationZ((float)camSpin) *
                Matrix4.CreateRotationY((float)camRot) *
                Matrix4.CreateRotationX((float)camAng) *
                Matrix4.CreatePerspectiveFieldOfView((float)fov, (float)aspect, 0.01f, 400)
                ;
            GL.UniformMatrix4(uCircleMVP, false, ref mvp);

            circleBuffPos = 0;
            for (int n = firstNote; n < lastNote; n++)
            {
                x1d = x1array[n] - 0.5;
                wdthd = wdtharray[n];
                x2d = x1d + wdthd;
                double size = circleRadius * 12 * auraSize[n];
                if (!blackKeys[n])
                {
                    y2 = 0;
                    if (settings.sameWidthNotes)
                    {
                        int _n = n % 12;
                        if (_n == 0)
                            x2d += wdthd * 0.666f;
                        else if (_n == 2)
                        {
                            x1d -= wdthd / 3;
                            x2d += wdthd / 3;
                        }
                        else if (_n == 4)
                            x1d -= wdthd / 3 * 2;
                        else if (_n == 5)
                            x2d += wdthd * 0.75f;
                        else if (_n == 7)
                        {
                            x1d -= wdthd / 4;
                            x2d += wdthd / 2;
                        }
                        else if (_n == 9)
                        {
                            x1d -= wdthd / 2;
                            x2d += wdthd / 4;
                        }
                        else if (_n == 11)
                            x1d -= wdthd * 0.75f;
                    }
                }

                Color4 coll = keyColors[n * 2];
                Color4 colr = keyColors[n * 2 + 1];

                r = coll.R * auraStrength;
                g = coll.G * auraStrength;
                b = coll.B * auraStrength;
                a = coll.A * auraStrength;
                r2 = colr.R * auraStrength;
                g2 = colr.G * auraStrength;
                b2 = colr.B * auraStrength;
                a2 = colr.A * auraStrength;

                double middle = (x1d + x2d) / 2;
                x1d = middle - size;
                x2d = middle + size;
                y1 = size;
                y2 = -size;


                int pos = circleBuffPos * 12;
                circleVertBuff[pos++] = x1d;
                circleVertBuff[pos++] = y1;
                circleVertBuff[pos++] = 0;
                circleVertBuff[pos++] = x1d;
                circleVertBuff[pos++] = y2;
                circleVertBuff[pos++] = 0;
                circleVertBuff[pos++] = x2d;
                circleVertBuff[pos++] = y2;
                circleVertBuff[pos++] = 0;
                circleVertBuff[pos++] = x2d;
                circleVertBuff[pos++] = y1;
                circleVertBuff[pos++] = 0;

                pos = circleBuffPos * 16;
                circleColorBuff[pos++] = r;
                circleColorBuff[pos++] = g;
                circleColorBuff[pos++] = b;
                circleColorBuff[pos++] = a;
                circleColorBuff[pos++] = r;
                circleColorBuff[pos++] = g;
                circleColorBuff[pos++] = b;
                circleColorBuff[pos++] = a;
                circleColorBuff[pos++] = r2;
                circleColorBuff[pos++] = g2;
                circleColorBuff[pos++] = b2;
                circleColorBuff[pos++] = a2;
                circleColorBuff[pos++] = r2;
                circleColorBuff[pos++] = g2;
                circleColorBuff[pos++] = b2;
                circleColorBuff[pos++] = a2;

                pos = circleBuffPos * 8;
                circleUVBuff[pos++] = 0;
                circleUVBuff[pos++] = 0;
                circleUVBuff[pos++] = 1;
                circleUVBuff[pos++] = 0;
                circleUVBuff[pos++] = 1;
                circleUVBuff[pos++] = 1;
                circleUVBuff[pos++] = 0;
                circleUVBuff[pos++] = 1;

                circleBuffPos++;
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, circleVert);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(circleVertBuff.Length * 8),
                circleVertBuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Double, false, 24, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, circleColor);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(circleColorBuff.Length * 4),
                circleColorBuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 16, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, circleUV);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(circleUVBuff.Length * 8),
                circleUVBuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Double, false, 16, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, circleIndx);
            GL.IndexPointer(IndexPointerType.Int, 1, 0);
            GL.DrawElements(PrimitiveType.Quads, circleBuffPos * 4, DrawElementsType.UnsignedInt, IntPtr.Zero);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            #endregion
        }

        void RenderKeyboard()
        {
            if (settings.showKeyboard)
            {
                #region Keyboard
                float wdth;
                float wdth2;
                float x1;
                float x2;
                double y2;
                Matrix4 mvp;
                Color4[] origColors = new Color4[257];
                for (int k = firstNote; k < lastNote; k++)
                {
                    if (isBlackNote(k))
                        origColors[k] = Color4.Black;
                    else
                        origColors[k] = Color4.White;
                }

                GL.UseProgram(whiteKeyShader);

                GL.BindBuffer(BufferTarget.ArrayBuffer, whiteKeyCol);
                GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, 4, 0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, whiteKeyBlend);
                GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 4, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, whiteKeyIndx);
                GL.IndexPointer(IndexPointerType.Int, 1, 0);

                for (int n = firstNote; n < lastNote; n++)
                {
                    x1 = (float)x1array[n];
                    wdth = (float)wdtharray[n];
                    x2 = x1 + wdth;

                    if (!blackKeys[n])
                    {
                        y2 = 0;
                        if (settings.sameWidthNotes)
                        {
                            int _n = n % 12;
                            if (_n == 0)
                                x2 += wdth * 0.666f;
                            else if (_n == 2)
                            {
                                x1 -= wdth / 3;
                                x2 += wdth / 3;
                            }
                            else if (_n == 4)
                                x1 -= wdth / 3 * 2;
                            else if (_n == 5)
                                x2 += wdth * 0.75f;
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
                                x1 -= wdth * 0.75f;
                            wdth2 = wdth * 2;
                        }
                        else
                        {
                            wdth2 = wdth;
                        }
                    }
                    else continue;
                    wdth = x2 - x1;
                    x1 -= 0.5f;

                    var coll = keyColors[n * 2];
                    var colr = keyColors[n * 2 + 1];
                    var origcol = origColors[n];
                    float blendfac = coll.A * 0.8f;
                    float revblendfac = 1 - blendfac;
                    coll = new Color4(
                        coll.R * blendfac + origcol.R * revblendfac,
                        coll.G * blendfac + origcol.G * revblendfac,
                        coll.B * blendfac + origcol.B * revblendfac,
                        1);
                    blendfac = colr.A * 0.8f;
                    revblendfac = 1 - blendfac;
                    colr = new Color4(
                        colr.R * blendfac + origcol.R * revblendfac,
                        colr.G * blendfac + origcol.G * revblendfac,
                        colr.B * blendfac + origcol.B * revblendfac,
                        1);

                    GL.Uniform4(uWhiteKeycoll, coll);
                    GL.Uniform4(uWhiteKeycolr, colr);
                    float scale = 1;
                    if (!sameWidth)
                    {
                        scale = 1.17f;
                        wdth2 = wdth;
                    }
                    else wdth2 = (float)wdtharray[firstNote] * 2;
                    mvp = Matrix4.Identity *
                        Matrix4.CreateScale(0.95f, 1, scale);
                    if (tiltKeys)
                        mvp *=
                            Matrix4.CreateTranslation(0, 0, -4) *
                            Matrix4.CreateRotationX((float)-keyPressFactor[n] / 20) *
                            Matrix4.CreateTranslation(0, 0, 4);
                    else
                        mvp *= Matrix4.CreateTranslation(0, (float)-keyPressFactor[n] / 2, 0);
                    mvp *=
                        Matrix4.CreateTranslation(0, -0.3f, 0) *
                        (sameWidth ? Matrix4.CreateScale(wdth, wdth2 * 0.9f, wdth2 * 1.01f) : Matrix4.CreateScale(wdth2, wdth2, wdth2)) *
                        Matrix4.CreateTranslation(x1, 0, 0) *
                        (settings.verticalNotes ? Matrix4.CreateTranslation(0, 0, 0.05f) : Matrix4.Identity) *
                        Matrix4.CreateTranslation((float)viewpan, -(float)viewheight, -(float)viewoffset) *
                        Matrix4.CreateScale(1, 1, -1) *
                        Matrix4.CreateRotationZ((float)camSpin) *
                        Matrix4.CreateRotationY((float)camRot) *
                        Matrix4.CreateRotationX((float)camAng) *
                        Matrix4.CreatePerspectiveFieldOfView((float)fov, (float)aspect, 0.01f, 400)
                    ;

                    if (n == firstNote)
                        GL.BindBuffer(BufferTarget.ArrayBuffer, whiteKeyVert[keynum[n] % 7 + 14]);
                    else if (n == lastNote - 1)
                        GL.BindBuffer(BufferTarget.ArrayBuffer, whiteKeyVert[keynum[n] % 7 + 7]);
                    else
                        GL.BindBuffer(BufferTarget.ArrayBuffer, whiteKeyVert[keynum[n] % 7]);
                    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Double, false, 24, 0);
                    GL.UniformMatrix4(uWhiteKeyMVP, false, ref mvp);
                    GL.DrawElements(PrimitiveType.Quads, whiteKeyBufferLen, DrawElementsType.UnsignedInt, IntPtr.Zero);
                }

                GL.UseProgram(blackKeyShader);

                GL.BindBuffer(BufferTarget.ArrayBuffer, blackKeyVert);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Double, false, 24, 0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, blackKeyCol);
                GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, 4, 0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, blackKeyBlend);
                GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 4, 0);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, blackKeyIndx);
                GL.IndexPointer(IndexPointerType.Int, 1, 0);

                for (int n = firstNote; n < lastNote; n++)
                {
                    x1 = (float)x1array[n];
                    wdth = (float)wdtharray[n];
                    x1 -= 0.5f;

                    if (!blackKeys[n]) continue;

                    var coll = keyColors[n * 2];
                    var colr = keyColors[n * 2 + 1];
                    var origcol = origColors[n];
                    float blendfac = coll.A * 0.8f;
                    float revblendfac = 1 - blendfac;
                    coll = new Color4(
                        coll.R * blendfac + origcol.R * revblendfac,
                        coll.G * blendfac + origcol.G * revblendfac,
                        coll.B * blendfac + origcol.B * revblendfac,
                        1);
                    blendfac = colr.A * 0.8f;
                    revblendfac = 1 - blendfac;
                    colr = new Color4(
                        colr.R * blendfac + origcol.R * revblendfac,
                        colr.G * blendfac + origcol.G * revblendfac,
                        colr.B * blendfac + origcol.B * revblendfac,
                        1);

                    GL.Uniform4(uBlackKeycoll, coll);
                    GL.Uniform4(uBlackKeycolr, colr);

                    float vertOffset = 1.2f;
                    if (!sameWidth) vertOffset = 1.1f;
                    float scale = 1;
                    if (!sameWidth) scale = 0.97f;
                    mvp = Matrix4.Identity *
                        Matrix4.CreateScale(0.95f, 1, scale);
                    if (tiltKeys)
                        mvp *=
                            Matrix4.CreateTranslation(0, 0, -4) *
                            Matrix4.CreateRotationX((float)-keyPressFactor[n] / 20) *
                            Matrix4.CreateTranslation(0, 0, 4);
                    else
                        mvp *= Matrix4.CreateTranslation(0, (float)-keyPressFactor[n] / 1.2f, 0);
                    mvp *=
                    Matrix4.CreateTranslation(0, vertOffset, 0) *
                    Matrix4.CreateScale(wdth, wdth / scale, wdth) *
                    Matrix4.CreateTranslation(x1, 0, 0) *
                    (settings.verticalNotes ? Matrix4.CreateTranslation(0, 0, 0.05f) : Matrix4.Identity) *
                    Matrix4.CreateTranslation((float)viewpan, -(float)viewheight, -(float)viewoffset) *
                    Matrix4.CreateScale(1, 1, -1) *
                    Matrix4.CreateRotationZ((float)camSpin) *
                    Matrix4.CreateRotationY((float)camRot) *
                    Matrix4.CreateRotationX((float)camAng) *
                    Matrix4.CreatePerspectiveFieldOfView((float)fov, (float)aspect, 0.01f, 400)
                    ;

                    GL.UniformMatrix4(uWhiteKeyMVP, false, ref mvp);
                    GL.DrawElements(PrimitiveType.Quads, blackKeyBufferLen, DrawElementsType.UnsignedInt, IntPtr.Zero);
                }
                #endregion
            }
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

        void FlushNoteBuffer(bool check = true)
        {
            if (noteBuffPos < noteBuffLen && check) return;
            if (noteBuffPos == 0) return;
            GL.BindBuffer(BufferTarget.ArrayBuffer, noteVert);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(noteBuffPos * 12 * 8),
                noteVertBuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Double, false, 24, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, noteCol);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(noteBuffPos * 16 * 4),
                noteColBuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 16, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, noteShade);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(noteBuffPos * 4 * 4),
                noteShadeBuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 4, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, noteIndx);
            GL.IndexPointer(IndexPointerType.Int, 1, 0);
            GL.DrawElements(PrimitiveType.Quads, noteBuffPos * 4, DrawElementsType.UnsignedInt, IntPtr.Zero);
            noteBuffPos = 0;
        }

        bool isBlackNote(int n)
        {
            n = n % 12;
            return n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
        }
    }
}
