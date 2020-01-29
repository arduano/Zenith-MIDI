using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZenithEngine;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace PFARender
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

#if !NORMAL_SHADE
        public string Name => "PFA+";
#else
        public string Name => "PFA+ Original";
#endif
        public string Description => "A replica of PFA with some special extra features";
        public ImageSource PreviewImage { get; private set; }
        public string LanguageDictName { get; } = "pfa";

        #region Shaders
        string noteShaderVert = @"#version 330 compatibility

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 glColor;
layout(location = 2) in vec2 attrib;

out vec4 color;

void main()
{
    gl_Position = vec4(position.x * 2 - 1, position.y * 2 - 1, 1.0f, 1.0f);
    color = vec4(glColor.xyz * attrib.y + attrib.x, glColor.w);
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

        public double NoteCollectorOffset => 0;
        public bool ManualNoteDelete => false;

        RenderSettings renderSettings;
        Settings settings;

        SettingsCtrl settingsControl;

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
        uint[] indexes = new uint[2048 * 128 * 6];

        bool[] blackKeys = new bool[257];
        int[] keynum = new int[257];

        public bool Initialized { get; private set; } = false;

        public long LastNoteCount { get; private set; }

        public MidiInfo CurrentMidi { get; set; }

        public NoteColor[][] NoteColors { get; set; }

        public double NoteScreenTime => settings.deltaTimeOnScreen;

        public System.Windows.Controls.Control SettingsControl => settingsControl;

        public double Tempo { get; set; }

        public void Dispose()
        {
            GL.DeleteBuffers(3, new int[] { vertexBufferID, colorBufferID, attribBufferID });
            GL.DeleteProgram(noteShader);
            quadVertexbuff = null;
            quadColorbuff = null;
            quadAttribbuff = null;
            Initialized = false;
            Console.WriteLine("Disposed of PFARender");
        }

        public Render(RenderSettings settings)
        {
            this.settings = new Settings();
            this.renderSettings = settings;
            settingsControl = new SettingsCtrl(this.settings);
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
            Console.WriteLine("Initialised PFARender");
        }

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
            bool blackNotesAbove = settings.blackNotesAbove;
            double scwidth = renderSettings.width;
            Color4[] keyColors = new Color4[514];
            bool[] keyPressed = new bool[257];
            for (int i = 0; i < 514; i++) keyColors[i] = Color4.Transparent;
            for (int i = 0; i < 257; i++) keyPressed[i] = false;
            quadBufferPos = 0;

            int pos;

            double x1, x2, y1, y2;
            double wdth;
            float r, g, b, a, r2, g2, b2, a2, r3, g3, b3, a3;
            double paddingx = 0.001 * settings.borderWidth;
            double paddingy = paddingx * renderSettings.width / renderSettings.height;

            double[] x1array = new double[257];
            double[] wdtharray = new double[257];
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
                        wdth = 0.64f / (knmln - knmfn + 1);
                        int bknum = keynum[i] % 5;
                        double offset = wdth / 2;
                        if (bknum == 0) offset += offset * 0.4;
                        if (bknum == 2) offset += offset * 0.4;
                        if (bknum == 1) offset -= offset * 0.4;
                        if (bknum == 4) offset -= offset * 0.4;

                        //if (bknum == 0 || bknum == 2)
                        //{
                        //    offset *= 1.3;
                        //}
                        //else if (bknum == 1 || bknum == 4)
                        //{
                        //    offset *= 0.7;
                        //}
                        x1array[i] = (float)(keynum[_i] - knmfn) / (knmln - knmfn + 1) - offset;
                        wdtharray[i] = wdth;
                    }
                }
            }
            double sepwdth = Math.Round(wdtharray[0] * scwidth / 20);
            if (sepwdth == 0) sepwdth = 1;
            #endregion

            #region Notes
            quadBufferPos = 0;
            double notePosFactor = 1 / deltaTimeOnScreen * (1 - pianoHeight);
            foreach (Note n in notes)
            {
                double renderCutoff = midiTime + deltaTimeOnScreen;
                if (n.end >= midiTime || !n.hasEnded)
                {
                    if (n.start < renderCutoff)
                    {
                        unsafe
                        {
                            int k = n.key;
                            if (!(k >= firstNote && k < lastNote) || (blackKeys[k] && blackNotesAbove)) continue;
                            nc++;
                            Color4 coll = n.color.left;
                            Color4 colr = n.color.right;
                            if (n.start < midiTime)
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
                                keyPressed[k] = true;
                            }
                            x1 = x1array[k];
                            wdth = wdtharray[k];
                            x2 = x1 + wdth;
                            y1 = 1 - (renderCutoff - n.end) * notePosFactor;
                            y2 = 1 - (renderCutoff - n.start) * notePosFactor;
                            if (!n.hasEnded)
                                y1 = 1;

                            r = coll.R;
                            g = coll.G;
                            b = coll.B;
                            a = coll.A;
                            r2 = colr.R;
                            g2 = colr.G;
                            b2 = colr.B;
                            a2 = colr.A;

                            //Outside
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
                            quadAttribbuff[pos++] = 0;
                            quadAttribbuff[pos++] = 0.2;
                            quadAttribbuff[pos++] = 0;
                            quadAttribbuff[pos++] = 0.2;
                            quadAttribbuff[pos++] = 0;
                            quadAttribbuff[pos++] = 0.2;
                            quadAttribbuff[pos++] = 0;
                            quadAttribbuff[pos++] = 0.2;

                            quadBufferPos++;
                            FlushQuadBuffer();
                        }
                        //Inside
                        unsafe
                        {
                            if (y1 - y2 > paddingy * 2)
                            {
                                x1 += paddingx;
                                x2 -= paddingx;
                                y1 -= paddingy;
                                y2 += paddingy;
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
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = 0.5;
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = 0.5;
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = 1;
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = 1;

                                quadBufferPos++;
                            }
                            FlushQuadBuffer();
                        }
                    }
                    else break;
                }
            }
            if (blackNotesAbove)
            {
                FlushQuadBuffer(false);
                foreach (Note n in notes)
                {
                    double renderCutoff = midiTime + deltaTimeOnScreen;
                    if (n.end >= midiTime || !n.hasEnded)
                        if (n.start < renderCutoff)
                        {
                            unsafe
                            {
                                int k = n.key;
                                if (!(k >= firstNote && k < lastNote) || !blackKeys[k]) continue;
                                nc++;
                                Color4 coll = n.color.left;
                                Color4 colr = n.color.right;
                                if (n.start < midiTime)
                                {
                                    keyColors[k * 2] = coll;
                                    keyColors[k * 2 + 1] = colr;
                                    keyPressed[k] = true;
                                }
                                x1 = x1array[k];
                                wdth = wdtharray[k];
                                x2 = x1 + wdth;
                                y1 = 1 - (renderCutoff - n.end) * notePosFactor;
                                y2 = 1 - (renderCutoff - n.start) * notePosFactor;
                                if (!n.hasEnded)
                                    y1 = 1;

                                r = coll.R;
                                g = coll.G;
                                b = coll.B;
                                a = coll.A;
                                r2 = colr.R;
                                g2 = colr.G;
                                b2 = colr.B;
                                a2 = colr.A;

                                //Outside
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
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = 0.2;
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = 0.2;
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = 0.2;
                                quadAttribbuff[pos++] = 0;
                                quadAttribbuff[pos++] = 0.2;

                                quadBufferPos++;
                            }
                            FlushQuadBuffer();

                            //Inside
                            unsafe
                            {
                                if (y1 - y2 > paddingy * 2)
                                {
                                    x1 += paddingx;
                                    x2 -= paddingx;
                                    y1 -= paddingy;
                                    y2 += paddingy;
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
                                    quadAttribbuff[pos++] = 0;
                                    quadAttribbuff[pos++] = 0.5;
                                    quadAttribbuff[pos++] = 0;
                                    quadAttribbuff[pos++] = 0.5;
                                    quadAttribbuff[pos++] = 0;
                                    quadAttribbuff[pos++] = 1;
                                    quadAttribbuff[pos++] = 0;
                                    quadAttribbuff[pos++] = 1;

                                    quadBufferPos++;
                                }
                                FlushQuadBuffer();
                            }
                        }
                        else break;
                }
            }
            FlushQuadBuffer(false);
            LastNoteCount = nc;
            #endregion

            #region Keyboard
            quadBufferPos = 0;

            double topRedStart = pianoHeight * .99;
            double topRedEnd = pianoHeight * .94;
            double topBarEnd = pianoHeight * .927;

            double wEndUpT = pianoHeight * 0.03 + pianoHeight * 0.020;
            double wEndUpB = pianoHeight * 0.03 + pianoHeight * 0.005;
            double wEndDownT = pianoHeight * 0.01;
            double bKeyEnd = pianoHeight * .345;
            double bKeyDownT = topBarEnd + pianoHeight * 0.015;
            double bKeyDownB = bKeyEnd + pianoHeight * 0.015;
            double bKeyUpT = topBarEnd + pianoHeight * 0.045;
            double bKeyUpB = bKeyEnd + pianoHeight * 0.045;

            double bKeyUSplitLT = pianoHeight * 0.78;
            double bKeyUSplitRT = pianoHeight * 0.71;
            double bKeyUSplitLB = pianoHeight * 0.65;
            double bKeyUSplitRB = pianoHeight * 0.58;
            double keySpacing = 0;

            double ox1, ox2, oy1, oy2, ix1, ix2, iy1, iy2;

            #region Decorations
            //Grey thing
            pos = quadBufferPos * 8;
            quadVertexbuff[pos++] = 0;
            quadVertexbuff[pos++] = pianoHeight;
            quadVertexbuff[pos++] = 1;
            quadVertexbuff[pos++] = pianoHeight;
            quadVertexbuff[pos++] = 1;
            quadVertexbuff[pos++] = topRedStart;
            quadVertexbuff[pos++] = 0;
            quadVertexbuff[pos++] = topRedStart;

            pos = quadBufferPos * 8;
            quadAttribbuff[pos++] = 0;
            quadAttribbuff[pos++] = 1;
            quadAttribbuff[pos++] = 0;
            quadAttribbuff[pos++] = 1;
            quadAttribbuff[pos++] = 0;
            quadAttribbuff[pos++] = 1;
            quadAttribbuff[pos++] = 0;
            quadAttribbuff[pos++] = 1;

            r = .086f;
            g = .086f;
            b = .086f;
            a = 1f;
            r2 = .0196f;
            g2 = .0196f;
            b2 = .0196f;
            a2 = 1f;

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

            //Red thing
            pos = quadBufferPos * 8;
            quadVertexbuff[pos++] = 0;
            quadVertexbuff[pos++] = topRedStart;
            quadVertexbuff[pos++] = 1;
            quadVertexbuff[pos++] = topRedStart;
            quadVertexbuff[pos++] = 1;
            quadVertexbuff[pos++] = topRedEnd;
            quadVertexbuff[pos++] = 0;
            quadVertexbuff[pos++] = topRedEnd;

            pos = quadBufferPos * 8;
            quadAttribbuff[pos++] = 0;
            quadAttribbuff[pos++] = 1;
            quadAttribbuff[pos++] = 0;
            quadAttribbuff[pos++] = 1;
            quadAttribbuff[pos++] = 0;
            quadAttribbuff[pos++] = 1;
            quadAttribbuff[pos++] = 0;
            quadAttribbuff[pos++] = 1;

            if (settings.topColor == TopColor.Red)
            {
                r2 = settings.topBarR;
                g2 = settings.topBarG;
                b2 = settings.topBarB;
                a2 = 1f;
                r = r2 * 0.5f;
                g = g2 * 0.5f;
                b = b2 * 0.5f;
                a = 1f;
            }
            else if (settings.topColor == TopColor.Blue)
            {
                b = .313f;
                r = .0196f;
                g = .0274f;
                a = 1f;
                b2 = .585f;
                r2 = .0392f;
                g2 = .0249f;
                a2 = 1f;
            }
            else if (settings.topColor == TopColor.Green)
            {
                g = .313f;
                b = .0196f;
                r = .0274f;
                a = 1f;
                g2 = .585f;
                b2 = .0392f;
                r2 = .0249f;
                a2 = 1f;
            }

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

            //Small grey thing
            pos = quadBufferPos * 8;
            quadVertexbuff[pos++] = 0;
            quadVertexbuff[pos++] = topRedEnd;
            quadVertexbuff[pos++] = 1;
            quadVertexbuff[pos++] = topRedEnd;
            quadVertexbuff[pos++] = 1;
            quadVertexbuff[pos++] = topBarEnd;
            quadVertexbuff[pos++] = 0;
            quadVertexbuff[pos++] = topBarEnd;

            pos = quadBufferPos * 8;
            quadAttribbuff[pos++] = 0;
            quadAttribbuff[pos++] = 1;
            quadAttribbuff[pos++] = 0;
            quadAttribbuff[pos++] = 1;
            quadAttribbuff[pos++] = 0;
            quadAttribbuff[pos++] = 1;
            quadAttribbuff[pos++] = 0;
            quadAttribbuff[pos++] = 1;

            r = .239f;
            g = .239f;
            b = .239f;
            a = 1f;
            r2 = .239f;
            g2 = .239f;
            b2 = .239f;
            a2 = 1f;

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
            #endregion

            y2 = 0;
            y1 = topBarEnd;
            Color4[] origColors = new Color4[257];
            for (int k = kbfirstNote; k < kblastNote; k++)
            {
                if (isBlackNote(k))
                    origColors[k] = Color4.Black;
                else
                    origColors[k] = Color4.White;
            }

            #region White
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

                if (keyPressed[n])
                {
                    //White key panel
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = x1;
                    quadVertexbuff[pos++] = wEndDownT;
                    quadVertexbuff[pos++] = x2;
                    quadVertexbuff[pos++] = wEndDownT;
                    quadVertexbuff[pos++] = x2;
                    quadVertexbuff[pos++] = y1;
                    quadVertexbuff[pos++] = x1;
                    quadVertexbuff[pos++] = y1;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.5;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.5;

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

                    //Key End Notch 
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = x1;
                    quadVertexbuff[pos++] = y2;
                    quadVertexbuff[pos++] = x2;
                    quadVertexbuff[pos++] = y2;
                    quadVertexbuff[pos++] = x2;
                    quadVertexbuff[pos++] = wEndDownT;
                    quadVertexbuff[pos++] = x1;
                    quadVertexbuff[pos++] = wEndDownT;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.6;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.6;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.6;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.6;

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
                else
                {
                    //White key panel
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = x1;
                    quadVertexbuff[pos++] = wEndUpT;
                    quadVertexbuff[pos++] = x2;
                    quadVertexbuff[pos++] = wEndUpT;
                    quadVertexbuff[pos++] = x2;
                    quadVertexbuff[pos++] = y1;
                    quadVertexbuff[pos++] = x1;
                    quadVertexbuff[pos++] = y1;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.8;

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

                    //Key End Notch 
                    r = .529f;
                    g = .529f;
                    b = .529f;
                    a = 1f;
                    r2 = .329f;
                    g2 = .329f;
                    b2 = .329f;
                    a2 = 1f;
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = x1;
                    quadVertexbuff[pos++] = wEndUpB;
                    quadVertexbuff[pos++] = x2;
                    quadVertexbuff[pos++] = wEndUpB;
                    quadVertexbuff[pos++] = x2;
                    quadVertexbuff[pos++] = wEndUpT;
                    quadVertexbuff[pos++] = x1;
                    quadVertexbuff[pos++] = wEndUpT;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;

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

                    //Key bottom side 
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = x1;
                    quadVertexbuff[pos++] = y2;
                    quadVertexbuff[pos++] = x2;
                    quadVertexbuff[pos++] = y2;
                    quadVertexbuff[pos++] = x2;
                    quadVertexbuff[pos++] = wEndUpB;
                    quadVertexbuff[pos++] = x1;
                    quadVertexbuff[pos++] = wEndUpB;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;

                    r = .615f;
                    g = .615f;
                    b = .615f;
                    a = 1f;
                    r2 = .729f;
                    g2 = .729f;
                    b2 = .729f;
                    a2 = 1f;
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

                //Middle C
                if (n == 60 && settings.middleC)
                {
                    r = coll.R;
                    g = coll.G;
                    b = coll.B;
                    a = coll.A;
                    double _wdth = x2 - x1;
                    double _x1 = x1 + _wdth / 4;
                    double _x2 = x2 - _wdth / 4;
                    double _y1, _y2;
                    if (keyPressed[n])
                    {
                        _y2 = wEndDownT + wdth / 4;
                        _y1 = _y2 + _wdth / 4.0 * 2.0 / renderSettings.height * renderSettings.width;
                    }
                    else
                    {
                        _y2 = wEndUpT + _wdth / 4;
                        _y1 = _y2 + wdth / 4.0 * 2.0 / renderSettings.height * renderSettings.width;
                    }
                    //Key End Notch 
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = _x1;
                    quadVertexbuff[pos++] = _y2;
                    quadVertexbuff[pos++] = _x2;
                    quadVertexbuff[pos++] = _y2;
                    quadVertexbuff[pos++] = _x2;
                    quadVertexbuff[pos++] = _y1;
                    quadVertexbuff[pos++] = _x1;
                    quadVertexbuff[pos++] = _y1;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.8;

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

                //Separator
                pos = quadBufferPos * 8;
                x2 = x1;

                x1 = Math.Floor(x1 * scwidth - sepwdth / 2);
                x2 = Math.Floor(x2 * scwidth + sepwdth / 2);
                if (x1 == x2) x2++;
                x1 /= scwidth;
                x2 /= scwidth;

                r = .0431f;
                g = .0431f;
                b = .0431f;
                a = 1f;
                r2 = .556f;
                g2 = .556f;
                b2 = .556f;
                a2 = 1f;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = y2;
                quadVertexbuff[pos++] = x2;
                quadVertexbuff[pos++] = y1;
                quadVertexbuff[pos++] = x1;
                quadVertexbuff[pos++] = y1;

                pos = quadBufferPos * 8;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 1;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 1;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 1;
                quadAttribbuff[pos++] = 0;
                quadAttribbuff[pos++] = 1;

                pos = quadBufferPos * 16;
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
                quadColorbuff[pos++] = r;
                quadColorbuff[pos++] = g;
                quadColorbuff[pos++] = b;
                quadColorbuff[pos++] = a;
                quadBufferPos++;
                FlushQuadBuffer();
            }
            #endregion

            #region Black
            for (int n = kbfirstNote; n < kblastNote; n++)
            {
                if (!blackKeys[n])
                {
                    continue;
                }

                ox1 = x1array[n];
                wdth = wdtharray[n];
                ox2 = ox1 + wdth;
                ix1 = ox1 + wdth / 8;
                ix2 = ox2 - wdth / 8;

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

                if (!keyPressed[n])
                {
                    #region Unpressed
                    //Middle Top
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUSplitLT;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUSplitRT;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUpT;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUpT;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0.25f;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0.25f;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0.15f;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0.15f;
                    quadAttribbuff[pos++] = 1;

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

                    //Middle Middle
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUSplitLB;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUSplitRB;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUSplitRT;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUSplitLT;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0.25f;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0.25f;
                    quadAttribbuff[pos++] = 1;

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

                    //Middle Bottom
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUpB;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUpB;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUSplitRB;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUSplitLB;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;

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

                    //Left
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = ox1;
                    quadVertexbuff[pos++] = bKeyEnd;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUpB;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUpT;
                    quadVertexbuff[pos++] = ox1;
                    quadVertexbuff[pos++] = topBarEnd;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0.3f;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0.3f;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;

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
                    quadVertexbuff[pos++] = ox2;
                    quadVertexbuff[pos++] = bKeyEnd;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUpB;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUpT;
                    quadVertexbuff[pos++] = ox2;
                    quadVertexbuff[pos++] = topBarEnd;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0.3f;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0.3f;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;

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

                    //Bottom
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = ox1;
                    quadVertexbuff[pos++] = bKeyEnd;
                    quadVertexbuff[pos++] = ox2;
                    quadVertexbuff[pos++] = bKeyEnd;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUpB;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUpB;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0.3f;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0.3f;
                    quadAttribbuff[pos++] = 1;

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
                    #endregion
                }
                else
                {
                    #region Pressed
                    //Middle Top
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUSplitLT;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUSplitRT;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyDownT;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyDownT;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.85f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.85f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.85f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.85f;

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

                    //Middle Middle
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUSplitLB;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUSplitRB;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUSplitRT;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUSplitLT;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.7f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.7f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.85f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.85f;

                    pos = quadBufferPos * 16;
                    quadColorbuff[pos++] = r3;
                    quadColorbuff[pos++] = g3;
                    quadColorbuff[pos++] = b3;
                    quadColorbuff[pos++] = a3;
                    quadColorbuff[pos++] = r3;
                    quadColorbuff[pos++] = g3;
                    quadColorbuff[pos++] = b3;
                    quadColorbuff[pos++] = a3;
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

                    //Middle Bottom
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyDownB;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyDownB;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyUSplitRB;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyUSplitLB;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.7f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.7f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.7f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.7f;

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

                    //Left
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = ox1;
                    quadVertexbuff[pos++] = bKeyEnd;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyDownB;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyDownT;
                    quadVertexbuff[pos++] = ox1;
                    quadVertexbuff[pos++] = topBarEnd;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.7f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.7f;

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
                    quadVertexbuff[pos++] = ox2;
                    quadVertexbuff[pos++] = bKeyEnd;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyDownB;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyDownT;
                    quadVertexbuff[pos++] = ox2;
                    quadVertexbuff[pos++] = topBarEnd;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.7f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.7f;

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

                    //Bottom
                    pos = quadBufferPos * 8;
                    quadVertexbuff[pos++] = ox1;
                    quadVertexbuff[pos++] = bKeyEnd;
                    quadVertexbuff[pos++] = ox2;
                    quadVertexbuff[pos++] = bKeyEnd;
                    quadVertexbuff[pos++] = ix2;
                    quadVertexbuff[pos++] = bKeyDownB;
                    quadVertexbuff[pos++] = ix1;
                    quadVertexbuff[pos++] = bKeyDownB;

                    pos = quadBufferPos * 8;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.7f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 0.7f;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;
                    quadAttribbuff[pos++] = 0;
                    quadAttribbuff[pos++] = 1;

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
                    #endregion
                }
            }
            #endregion

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
            GL.BindBuffer(BufferTarget.ArrayBuffer, attribBufferID);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(quadBufferPos * 8 * 8),
                quadAttribbuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Double, false, 16, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
            GL.IndexPointer(IndexPointerType.Int, 1, 0);
            GL.DrawElements(PrimitiveType.Triangles, quadBufferPos * 6, DrawElementsType.UnsignedInt, IntPtr.Zero);
            quadBufferPos = 0;
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

        bool isBlackNote(int n)
        {
            n = n % 12;
            return n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
        }
    }
}
