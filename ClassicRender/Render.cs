using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using BMEngine;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;

namespace ClassicRender
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

        #region Info
        public string Name { get; } = "Original";
        public string Description { get; } = "The original graphics renderer with full rendering functionality and vibrant colours";
        public bool Initialized { get; private set; } = false;
        public ImageSource PreviewImage { get; private set; }
        public string LanguageDictName { get; } = "classic";

        public double NoteScreenTime => settings.deltaTimeOnScreen;
        #endregion

        #region Shaders
        string noteShaderVert = @"#version 330 compatibility

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 glColor;
layout(location = 2) in vec2 attrib;

out vec4 color;

void main()
{
    gl_Position = vec4(position.x * 2 - 1, position.y * 2 - 1, 1.0f, 1.0f);
    color = vec4(glColor.xyz + attrib.x, glColor.w);
}
";
        string noteShaderFrag = @"#version 330 compatibility
 
in vec4 color;
 
out vec4 outputF;
layout(location = 0) out vec4 texOut;

void main()
{
    outputF = color;
	texOut = outputF;
}
";
        #endregion

        RenderSettings renderSettings;
        Settings settings;

        public long LastNoteCount { get; private set; }

        public Control SettingsControl { get; private set; }

        public double Tempo { get; set; }

        public NoteColor[][] NoteColors { get; set; }

        public MidiInfo CurrentMidi { get; set; }

        public bool ManualNoteDelete => false;

        public double NoteCollectorOffset => 0;

        int noteShader;

        int vertexBufferID;
        int colorBufferID;
        int attribBufferID;

        int quadBufferLength = 2048 * 64;
        double[] quadVertexbuff;
        float[] quadColorbuff;
        double[] quadAttribbuff;
        int quadBufferPos = 0;

        int indexBufferId;
        uint[] indexes;

        bool[] blackKeys = new bool[257];
        int[] keynum = new int[257];

        public void Dispose()
        {
            GL.DeleteBuffers(3, new int[] { vertexBufferID, colorBufferID, attribBufferID });
            GL.DeleteProgram(noteShader);
            quadVertexbuff = null;
            quadColorbuff = null;
            quadAttribbuff = null;
            Initialized = false;
            Console.WriteLine("Disposed of ClassicRender");
        }

        public Render(RenderSettings settings)
        {
            this.settings = new Settings();
            this.renderSettings = settings;
            SettingsControl = new SettingsCtrl(this.settings);
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

        public void Init()
        {
            int _vertexObj = GL.CreateShader(ShaderType.VertexShader);
            int _fragObj = GL.CreateShader(ShaderType.FragmentShader);
            int statusCode;
            string info;

            GL.ShaderSource(_vertexObj, noteShaderVert);
            GL.CompileShader(_vertexObj);
            info = GL.GetShaderInfoLog(_vertexObj);
            GL.GetShader(_vertexObj, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            GL.ShaderSource(_fragObj, noteShaderFrag);
            GL.CompileShader(_fragObj);
            info = GL.GetShaderInfoLog(_fragObj);
            GL.GetShader(_fragObj, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            noteShader = GL.CreateProgram();
            GL.AttachShader(noteShader, _fragObj);
            GL.AttachShader(noteShader, _vertexObj);
            GL.LinkProgram(noteShader);

            quadVertexbuff = new double[quadBufferLength * 8];
            quadColorbuff = new float[quadBufferLength * 16];
            quadAttribbuff = new double[quadBufferLength * 8];

            GL.GenBuffers(1, out vertexBufferID);
            GL.GenBuffers(1, out colorBufferID);
            GL.GenBuffers(1, out attribBufferID);
            indexes = new uint[quadBufferLength * 6];
            for (uint i = 0; i < indexes.Length / 6; i++)
            {
                indexes[i * 6 + 0] = i * 4 + 0;
                indexes[i * 6 + 1] = i * 4 + 1;
                indexes[i * 6 + 2] = i * 4 + 3;
                indexes[i * 6 + 3] = i * 4 + 1;
                indexes[i * 6 + 4] = i * 4 + 3;
                indexes[i * 6 + 5] = i * 4 + 2;
            }
            for (int i = 0; i < quadAttribbuff.Length;)
            {
                quadAttribbuff[i++] = -0.1;
                quadAttribbuff[i++] = 0;
                quadAttribbuff[i++] = 0.3;
                quadAttribbuff[i++] = 0;
                quadAttribbuff[i++] = -0.3;
                quadAttribbuff[i++] = 0;
                quadAttribbuff[i++] = 0.3;
                quadAttribbuff[i++] = 0;
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, attribBufferID);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(quadAttribbuff.Length * 8),
                quadAttribbuff,
                BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out indexBufferId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(indexes.Length * 4),
                indexes,
                BufferUsageHint.StaticDraw);
            Initialized = true;
            Console.WriteLine("Initialised ClassicRender");
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
                        NoteColors[i][j].left = cols[i * 32 + j * 2];
                        NoteColors[i][j].right = cols[i * 32 + j * 2 + 1];
                    }
                }
            }
        }

        Color4[] keyColors = new Color4[514];
        bool[] keyPressed = new bool[256];
        double[] x1array = new double[257];
        double[] wdtharray = new double[257];

        public void RenderFrame(FastList<Note> notes, double midiTime, int finalCompositeBuff)
        {
            GL.Enable(EnableCap.Blend);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.Enable(EnableCap.Texture2D);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, finalCompositeBuff);
            GL.Viewport(0, 0, renderSettings.width, renderSettings.height);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(noteShader);

            #region Vars
            long nc = 0;
            int firstNote = settings.firstNote;
            int lastNote = settings.lastNote;
            int kbfirstNote = settings.firstNote;
            int kblastNote = settings.lastNote;
            if (blackKeys[firstNote]) kbfirstNote--;
            if (blackKeys[lastNote - 1]) kblastNote++;

            double deltaTimeOnScreen = NoteScreenTime;
            double pianoHeight = settings.pianoHeight;
            bool sameWidth = settings.sameWidthNotes;
            for (int i = 0; i < 514; i++) keyColors[i] = Color4.Transparent;
            for (int i = 0; i < 256; i++) keyPressed[i] = false;
            double wdth;
            float r, g, b, a, r2, g2, b2, a2, r3, g3, b3, a3;
            double x1;
            double x2;
            double y1;
            double y2;
            quadBufferPos = 0;
            double xx1, xx2, yy1, yy2;
            double ys1, ys2;

            if (settings.sameWidthNotes)
            {
                for (int i = 0; i < 257; i++)
                {
                    x1array[i] = (float)(i - firstNote) / (lastNote - firstNote);
                    wdtharray[i] = 1.0f / (lastNote - firstNote);
                }
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
                        wdth = 0.6f / (knmln - knmfn + 1);
                        int bknum = keynum[i] % 5;
                        double offset = wdth / 2;
                        if (bknum == 0 || bknum == 2)
                        {
                            offset *= 1.3;
                        }
                        else if (bknum == 1 || bknum == 4)
                        {
                            offset *= 0.7;
                        }
                        x1array[i] = (float)(keynum[_i] - knmfn) / (knmln - knmfn + 1) - offset;
                        wdtharray[i] = wdth;
                    }
                }
            }
            double paddingx = wdtharray[5] * 0.1;
            double paddingy = paddingx * renderSettings.width / renderSettings.height;

            #endregion

            #region Notes
            quadBufferPos = 0;
            double notePosFactor = 1 / deltaTimeOnScreen * (1 - pianoHeight);
            double renderCutoff = midiTime + deltaTimeOnScreen;
            for (int noteKey = 0; noteKey < 2; noteKey++)
            {
                if (!settings.blackNotesAbove && !settings.sameWidthNotes && noteKey == 1) break;
                foreach (Note n in notes)
                {
                    if ((settings.blackNotesAbove && !settings.sameWidthNotes))
                    {
                        if ((noteKey == 0) ^ !blackKeys[n.note]) continue;
                    }
                    if (n.end >= midiTime || !n.hasEnded)
                    {
                        if (n.start < renderCutoff)
                        {
                            unsafe
                            {
                                nc++;
                                int k = n.note;
                                if (!(k >= firstNote && k < lastNote)) continue;
                                Color4 coll = n.color.left;
                                Color4 colr = n.color.right;
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
                                    blendfac = colr.A * 0.8f;
                                    revblendfac = 1 - blendfac;
                                    keyColors[k * 2 + 1] = new Color4(
                                        colr.R * blendfac + origcolr.R * revblendfac,
                                        colr.G * blendfac + origcolr.G * revblendfac,
                                        colr.B * blendfac + origcolr.B * revblendfac,
                                        1);
                                    keyPressed[k] = true;
                                }
                                x1 = x1array[k];
                                wdth = wdtharray[k];
                                x2 = x1 + wdth;
                                y1 = 1 - (renderCutoff - n.end) * notePosFactor;
                                y2 = 1 - (renderCutoff - n.start) * notePosFactor;
                                if (!n.hasEnded)
                                    y1 = 1;

                                xx1 = x1 + paddingx;
                                xx2 = x2 - paddingx;
                                yy1 = y1 - paddingy;
                                yy2 = y2 + paddingy;

                                if (yy1 < yy2)
                                {
                                    double yyavg = (yy1 + yy2) / 2;
                                    yy1 = yyavg;
                                    yy2 = yyavg;
                                }

                                int pos = quadBufferPos * 8;
                                quadVertexbuff[pos++] = x2;
                                quadVertexbuff[pos++] = y2;
                                quadVertexbuff[pos++] = x2;
                                quadVertexbuff[pos++] = y1;
                                quadVertexbuff[pos++] = x1;
                                quadVertexbuff[pos++] = y1;
                                quadVertexbuff[pos++] = x1;
                                quadVertexbuff[pos++] = y2;

                                pos = quadBufferPos * 16;
                                r = coll.R * 0.5f;
                                g = coll.G * 0.5f;
                                b = coll.B * 0.5f;
                                a = coll.A;
                                quadColorbuff[pos++] = r;
                                quadColorbuff[pos++] = g;
                                quadColorbuff[pos++] = b;
                                quadColorbuff[pos++] = a;
                                quadColorbuff[pos++] = r;
                                quadColorbuff[pos++] = g;
                                quadColorbuff[pos++] = b;
                                quadColorbuff[pos++] = a;
                                r = colr.R * 0.5f;
                                g = colr.G * 0.5f;
                                b = colr.B * 0.5f;
                                a = colr.A;
                                quadColorbuff[pos++] = r;
                                quadColorbuff[pos++] = g;
                                quadColorbuff[pos++] = b;
                                quadColorbuff[pos++] = a;
                                quadColorbuff[pos++] = r;
                                quadColorbuff[pos++] = g;
                                quadColorbuff[pos++] = b;
                                quadColorbuff[pos++] = a;

                                pos = quadBufferPos * 8;
                                quadAttribbuff[pos++] = 0.1;
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = 0.1;
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = -0.3;
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = -0.3;
                                quadAttribbuff[pos++] = 0;

                                quadBufferPos++;
                                FlushQuadBuffer();

                                pos = quadBufferPos * 8;
                                quadVertexbuff[pos++] = xx2;
                                quadVertexbuff[pos++] = yy2;
                                quadVertexbuff[pos++] = xx2;
                                quadVertexbuff[pos++] = yy1;
                                quadVertexbuff[pos++] = xx1;
                                quadVertexbuff[pos++] = yy1;
                                quadVertexbuff[pos++] = xx1;
                                quadVertexbuff[pos++] = yy2;

                                pos = quadBufferPos * 16;
                                r = coll.R;
                                g = coll.G;
                                b = coll.B;
                                a = coll.A;
                                quadColorbuff[pos++] = r;
                                quadColorbuff[pos++] = g;
                                quadColorbuff[pos++] = b;
                                quadColorbuff[pos++] = a;
                                quadColorbuff[pos++] = r;
                                quadColorbuff[pos++] = g;
                                quadColorbuff[pos++] = b;
                                quadColorbuff[pos++] = a;
                                r = colr.R;
                                g = colr.G;
                                b = colr.B;
                                a = colr.A;
                                quadColorbuff[pos++] = r;
                                quadColorbuff[pos++] = g;
                                quadColorbuff[pos++] = b;
                                quadColorbuff[pos++] = a;
                                quadColorbuff[pos++] = r;
                                quadColorbuff[pos++] = g;
                                quadColorbuff[pos++] = b;
                                quadColorbuff[pos++] = a;

                                pos = quadBufferPos * 8;
                                quadAttribbuff[pos++] = 0.1;
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = 0.1;
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = -0.3;
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = -0.3;
                                quadAttribbuff[pos++] = 0;

                                quadBufferPos++;
                                FlushQuadBuffer();
                            }

                        }
                        else break;
                    }
                }
            }

            FlushQuadBuffer(false);
            quadBufferPos = 0;

            LastNoteCount = nc;
            #endregion

            #region Keyboard
            y1 = pianoHeight;
            y2 = 0;
            Color4[] origColors = new Color4[257];
            for (int k = kbfirstNote; k < kblastNote; k++)
            {
                if (isBlackNote(k))
                    origColors[k] = Color4.Black;
                else
                    origColors[k] = Color4.White;
            }

            for (int n = kbfirstNote; n < kblastNote; n++)
            {
                x1 = x1array[n];
                wdth = wdtharray[n];
                x2 = x1 + wdth;

                if (!blackKeys[n])
                {
                    y2 = 0;
                    if (settings.sameWidthNotes)
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
                float blendfac = coll.A;
                float revblendfac = 1 - blendfac;
                coll = new Color4(
                    coll.R * blendfac + origcol.R * revblendfac,
                    coll.G * blendfac + origcol.G * revblendfac,
                    coll.B * blendfac + origcol.B * revblendfac,
                    1);
                r = coll.R;
                g = coll.G;
                b = coll.B;
                a = coll.A;
                blendfac = colr.A;
                revblendfac = 1 - blendfac;
                colr = new Color4(
                    colr.R * blendfac + origcol.R * revblendfac,
                    colr.G * blendfac + origcol.G * revblendfac,
                    colr.B * blendfac + origcol.B * revblendfac,
                    1);
                r2 = colr.R;
                g2 = colr.G;
                b2 = colr.B;
                a2 = colr.A;

                int pos = quadBufferPos * 8;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = y1;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = y1;

                pos = quadBufferPos * 8;
                quadAttribbuff[pos++] = 0.0;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = -0.1;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 0.0;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = -0.3;
                quadAttribbuff[pos++] = 0;

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
                quadBufferPos++;
                FlushQuadBuffer();

                pos = quadBufferPos * 8;
                x2 = x1 + wdth / 30;
                x1 -= wdth / 30;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = y1;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = y1;

                pos = quadBufferPos * 8;
                quadAttribbuff[pos++] = -0.3;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = -0.3;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = -0.1;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = -0.1;
                quadAttribbuff[pos++] = 0;

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
                quadBufferPos++;
                FlushQuadBuffer();
            }
            for (int n = kbfirstNote; n < kblastNote; n++)
            {
                x1 = x1array[n];
                wdth = wdtharray[n];
                x2 = x1 + wdth;

                if (blackKeys[n])
                {
                    y2 = pianoHeight / 10 * 3.7;
                }
                else continue;

                var coll = keyColors[n * 2];
                var colr = keyColors[n * 2 + 1];
                var origcol = origColors[n];
                float blendfac = coll.A;
                float revblendfac = 1 - blendfac;
                coll = new Color4(
                    coll.R * blendfac + origcol.R * revblendfac,
                    coll.G * blendfac + origcol.G * revblendfac,
                    coll.B * blendfac + origcol.B * revblendfac,
                    1);
                r = coll.R;
                g = coll.G;
                b = coll.B;
                a = coll.A;
                blendfac = colr.A;
                revblendfac = 1 - blendfac;
                colr = new Color4(
                    colr.R * blendfac + origcol.R * revblendfac,
                    colr.G * blendfac + origcol.G * revblendfac,
                    colr.B * blendfac + origcol.B * revblendfac,
                    1);
                r2 = colr.R;
                g2 = colr.G;
                b2 = colr.B;
                a2 = colr.A;
                var colm = new Color4(
                    (coll.R + colr.R) / 2,
                    (coll.G + colr.G) / 2,
                    (coll.B + colr.B) / 2,
                    (coll.A + colr.A) / 2
                    );
                r3 = colm.R;
                g3 = colm.G;
                b3 = colm.B;
                a3 = colm.A;

                xx1 = x1 + wdth / 6;
                xx2 = x2 - wdth / 6;
                if (keyPressed[n])
                    yy1 = y1 + 0.001;
                else
                    yy1 = y1 + 0.002;
                if (keyPressed[n])
                    yy2 = y2 + 0.005;
                else
                    yy2 = y2 + 0.01;

                ys1 = pianoHeight / 7 * 5;
                ys2 = pianoHeight / 7 * 6;

                //Middle Top
                int pos = quadBufferPos * 8;
                quadVertexbuff[pos++] = xx1;
                quadVertexbuff[pos++] = ys1;
                quadVertexbuff[pos++] = xx2;
                quadVertexbuff[pos++] = ys2;
                quadVertexbuff[pos++] = xx2;
                quadVertexbuff[pos++] = yy1;
                quadVertexbuff[pos++] = xx1;
                quadVertexbuff[pos++] = yy1;

                pos = quadBufferPos * 8;
                quadAttribbuff[pos++] = 0.3;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 0.3;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 0.2;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 0.4;
                quadAttribbuff[pos++] = 0;

                pos = quadBufferPos * 16;
                quadColorbuff[pos++] = r3;
                quadColorbuff[pos++] = g3;
                quadColorbuff[pos++] = b3;
                quadColorbuff[pos++] = a3;
                quadColorbuff[pos++] = r3;
                quadColorbuff[pos++] = g3;
                quadColorbuff[pos++] = b3;
                quadColorbuff[pos++] = a3;
                quadColorbuff[pos++] = r2;
                quadColorbuff[pos++] = g2;
                quadColorbuff[pos++] = b2;
                quadColorbuff[pos++] = a2;
                quadColorbuff[pos++] = r2;
                quadColorbuff[pos++] = g2;
                quadColorbuff[pos++] = b2;
                quadColorbuff[pos++] = a2;
                quadBufferPos++;
                FlushQuadBuffer();

                //Middle Bottom
                pos = quadBufferPos * 8;
                quadVertexbuff[pos++] = xx1;
                quadVertexbuff[pos++] = yy2;
                quadVertexbuff[pos++] = xx2;
                quadVertexbuff[pos++] = yy2;
                quadVertexbuff[pos++] = xx2;
                quadVertexbuff[pos++] = ys2;
                quadVertexbuff[pos++] = xx1;
                quadVertexbuff[pos++] = ys1;

                pos = quadBufferPos * 8;
                quadAttribbuff[pos++] = 0.0;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = -0.2;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 0.3;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 0.3;
                quadAttribbuff[pos++] = 0;

                pos = quadBufferPos * 16;
                quadColorbuff[pos++] = r;
                quadColorbuff[pos++] = g;
                quadColorbuff[pos++] = b;
                quadColorbuff[pos++] = a;
                quadColorbuff[pos++] = r;
                quadColorbuff[pos++] = g;
                quadColorbuff[pos++] = b;
                quadColorbuff[pos++] = a;
                quadColorbuff[pos++] = r3;
                quadColorbuff[pos++] = g3;
                quadColorbuff[pos++] = b3;
                quadColorbuff[pos++] = a3;
                quadColorbuff[pos++] = r3;
                quadColorbuff[pos++] = g3;
                quadColorbuff[pos++] = b3;
                quadColorbuff[pos++] = a3;
                quadBufferPos++;
                FlushQuadBuffer();

                //Bottom
                pos = quadBufferPos * 8;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = xx2;
                quadVertexbuff[pos++] = yy2;
                quadVertexbuff[pos++] = xx1;
                quadVertexbuff[pos++] = yy2;

                pos = quadBufferPos * 8;
                quadAttribbuff[pos++] = 0.2;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = -0.2;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 0.4;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 0.4;
                quadAttribbuff[pos++] = 0;

                pos = quadBufferPos * 16;
                quadColorbuff[pos++] = r;
                quadColorbuff[pos++] = g;
                quadColorbuff[pos++] = b;
                quadColorbuff[pos++] = a;
                quadColorbuff[pos++] = r;
                quadColorbuff[pos++] = g;
                quadColorbuff[pos++] = b;
                quadColorbuff[pos++] = a;
                quadColorbuff[pos++] = r;
                quadColorbuff[pos++] = g;
                quadColorbuff[pos++] = b;
                quadColorbuff[pos++] = a;
                quadColorbuff[pos++] = r;
                quadColorbuff[pos++] = g;
                quadColorbuff[pos++] = b;
                quadColorbuff[pos++] = a;
                quadBufferPos++;
                FlushQuadBuffer();

                //Left
                pos = quadBufferPos * 8;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = xx1;
                quadVertexbuff[pos++] = yy2;
                quadVertexbuff[pos++] = xx1;
                quadVertexbuff[pos++] = yy1;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = y1;

                pos = quadBufferPos * 8;
                quadAttribbuff[pos++] = 0.2;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 0.2;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 0.4;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 0.2;
                quadAttribbuff[pos++] = 0;

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
                quadBufferPos++;
                FlushQuadBuffer();


                //Right
                pos = quadBufferPos * 8;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = xx2;
                quadVertexbuff[pos++] = yy2;
                quadVertexbuff[pos++] = xx2;
                quadVertexbuff[pos++] = yy1;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = y1;

                pos = quadBufferPos * 8;
                quadAttribbuff[pos++] = -0.2;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = -0.2;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = -0.2;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = -0.2;
                quadAttribbuff[pos++] = 0;

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
                quadBufferPos++;
                FlushQuadBuffer();
            }
            FlushQuadBuffer(false);
            #endregion

            GL.Disable(EnableCap.Blend);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.Disable(EnableCap.Texture2D);

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.DisableVertexAttribArray(2);
        }

        void FlushQuadBuffer(bool check = true)
        {
            if (quadBufferPos < quadBufferLength && check) return;
            if (quadBufferPos == 0) return;
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(quadBufferPos * 2 * 8 * 4),
                quadVertexbuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Double, false, 16, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferID);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(quadBufferPos * 4 * 4 * 4),
                quadColorbuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 16, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, attribBufferID);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(quadBufferPos * 2 * 8 * 4),
                quadAttribbuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Double, false, 16, 0);
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
    }
}
