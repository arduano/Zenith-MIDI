using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using ZenithEngine;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using ZenithEngine.GLEngine;
using System.Runtime.InteropServices;
using ZenithEngine.ModuleUtil;

namespace FlatRender
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
        public string Name { get; } = "Flat";
        public string Description { get; } = "Flat renderer, requested by SquareWaveMidis for his channel";
        public bool Initialized { get; private set; } = false;
        public ImageSource PreviewImage { get; private set; }
        public string LanguageDictName { get; } = "flat";
        #endregion

        #region Shaders
        string noteShaderVert = @"#version 330 compatibility

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 glColor;

out vec4 color;

void main()
{
    gl_Position = vec4(position.x * 2 - 1, position.y * 2 - 1, 1.0f, 1.0f);
    color = glColor;
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

        SettingsCtrl settingsControl;

        public long LastNoteCount { get; private set; }

        public Control SettingsControl { get { return settingsControl; } }

        public double NoteCollectorOffset => 0;

        public bool ManualNoteDelete => false;

        public NoteColor[][] NoteColors { get; set; }

        public double Tempo { get; set; }

        public double StartOffset => settings.deltaTimeOnScreen;

        int noteShader;

        ShapeBuffer<VertType> quadBuffer;

        MidiFile midifile = null;

        public void Dispose()
        {
            if (!Initialized) return;
            midifile = null;
            GL.DeleteProgram(noteShader);
            quadBuffer.Dispose();
            Initialized = false;
            Console.WriteLine("Disposed of FlatRender");
        }

        public Render(RenderSettings settings)
        {
            this.renderSettings = settings;
            this.settings = new Settings();
            PreviewImage = BitmapToImageSource(Properties.Resources.preview);
            settingsControl = new SettingsCtrl(this.settings);
            ((SettingsCtrl)SettingsControl).PaletteChanged += () => { ReloadTrackColors(); };
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        struct VertType
        {
            float x;
            float y;
            Color4 color;

            public VertType(float x, float y, Color4 color)
            {
                this.x = x;
                this.y = y;
                this.color = color;
            }
        }
        public void Init(MidiFile file)
        {
            midifile = file;

            quadBuffer = new ShapeBuffer<VertType>(100, ShapeTypes.Triangles, ShapePresets.Quads, new[] {
                new InputAssemblyPart(2, VertexAttribPointerType.Float, 0),
                new InputAssemblyPart(4, VertexAttribPointerType.Float, 8),
            });

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

            Initialized = true;
            Console.WriteLine("Initialised FlatRender");
        }

        public void RenderFrame(FastList<Note> notes, double midiTime, int finalCompositeBuff)
        {
            midifile.CheckParseDistance(settings.deltaTimeOnScreen);

            GL.Enable(EnableCap.Blend);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, finalCompositeBuff);
            GL.Viewport(0, 0, renderSettings.PixelWidth, renderSettings.PixelHeight);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(noteShader);

            #region Vars
            long nc = 0;
            int firstNote = settings.firstNote;
            int lastNote = settings.lastNote;

            var keyboard = new KeyboardState(firstNote, lastNote, new KeyboardParams()
            {
                SameWidthNotes = settings.sameWidthNotes,
            });

            int kbfirstNote = settings.firstNote;
            int kblastNote = settings.lastNote;
            if (keyboard.BlackKey[firstNote]) kbfirstNote--;
            if (keyboard.BlackKey[lastNote - 1]) kblastNote++;

            double screenTime = settings.deltaTimeOnScreen;
            float pianoHeight = (float)settings.pianoHeight;
            #endregion

            double notePosFactor = 1 / screenTime * (1 - pianoHeight);

            var iter = notes.Iterate();
            for (Note n = null; iter.MoveNext(out n);)
            {
                double renderCutoff = midiTime + screenTime;
                if (n.end < midiTime && n.hasEnded)
                {
                    iter.Remove();
                    continue;
                }

                if (n.start >= renderCutoff) break;
                if (n.key < firstNote || n.key >= lastNote) continue;

                if (n.start < midiTime)
                {
                    keyboard.BlendNote(n.key, n.color);
                }

                float left = (float)keyboard.Notes[n.key].Left;
                float right = (float)keyboard.Notes[n.key].Right;
                float end = (float)(1 - (renderCutoff - n.end) * notePosFactor);
                float start = (float)(1 - (renderCutoff - n.start) * notePosFactor);
                if (!n.hasEnded)
                    end = 1;

                quadBuffer.PushVertex(new VertType(right, start, n.color.Left));
                quadBuffer.PushVertex(new VertType(right, end, n.color.Left));
                quadBuffer.PushVertex(new VertType(left, end, n.color.Right));
                quadBuffer.PushVertex(new VertType(left, start, n.color.Right));

                nc++;
            }

            LastNoteCount = nc;

            for (int n = kbfirstNote; n < kblastNote; n++)
            {
                if (keyboard.BlackKey[n]) continue;

                float left = (float)(keyboard.Keys[n].Left);
                float right = (float)(keyboard.Keys[n].Right);
                var coll = keyboard.Colors[n].Left;
                var colr = keyboard.Colors[n].Right;

                quadBuffer.PushVertex(new VertType(left, 0, coll));
                quadBuffer.PushVertex(new VertType(right, 0, coll));
                quadBuffer.PushVertex(new VertType(right, pianoHeight, colr));
                quadBuffer.PushVertex(new VertType(left, pianoHeight, colr));
            }
            for (int n = kbfirstNote; n < kblastNote; n++)
            {
                if (!keyboard.BlackKey[n]) continue;

                float left = (float)(keyboard.Keys[n].Left);
                float right = (float)(keyboard.Keys[n].Right);
                var coll = keyboard.Colors[n].Left;
                var colr = keyboard.Colors[n].Right;
                float keyBottom = (float)(pianoHeight / 10 * 3.7);

                quadBuffer.PushVertex(new VertType(left, keyBottom, coll));
                quadBuffer.PushVertex(new VertType(right, keyBottom, coll));
                quadBuffer.PushVertex(new VertType(right, pianoHeight, colr));
                quadBuffer.PushVertex(new VertType(left, pianoHeight, colr));

            }

            quadBuffer.Flush();

            GL.Disable(EnableCap.Blend);

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
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
