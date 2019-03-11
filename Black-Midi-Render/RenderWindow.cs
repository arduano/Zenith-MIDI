using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BMEngine;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text.RegularExpressions;

namespace Black_Midi_Render
{
    interface INoteRender : IDisposable
    {
        long Render(FastList<Note> notes, double midiTime);
    }

    interface IKeyboardRender : IDisposable
    {
        void Render();
    }

    class RenderWindow : GameWindow
    {
        #region Shaders
        string postShaderVert = @"#version 330 compatibility

in vec3 position;
out vec2 UV;

void main()
{
    gl_Position = vec4(position.x * 2 - 1, position.y * 2 - 1, position.z * 2 - 1, 1.0f);
	//color = glColor;
    UV = vec2(position.x, position.y);
}
";
        string postShaderFrag = @"#version 330 compatibility

in vec2 UV;

out vec4 color;

uniform sampler2D myTextureSampler;

void main()
{
    color = texture2D( myTextureSampler, UV );
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

        FastList<Note> globalDisplayNotes;
        FastList<Tempo> globalTempoEvents;
        FastList<ColorChange> globalColorEvents;
        MidiFile midi;

        RenderSettings settings;

        public double midiTime = 0;
        public double tempoFrameStep = 10;

        Process ffmpeg = new Process();
        long imgnumber = 0;
        Task lastRenderPush = null;

        GLPostbuffer finalCompositeBuff;

        int postShader;

        int screenQuadBuffer;
        int screenQuadIndexBuffer;
        double[] screenQuadArray = new double[] { 0, 0, 0, 1, 1, 1, 1, 0 };
        int[] screenQuadArrayIndex = new int[] { 0, 1, 2, 3 };

        byte[] pixels;

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
        }

        CurrentRendererPointer render;
        GLTextEngine textEngine;
        public RenderWindow(CurrentRendererPointer renderer, MidiFile midi, RenderSettings settings) : base(16, 9, new GraphicsMode(new ColorFormat(8, 8, 8, 8)), "Render", GameWindowFlags.Default, DisplayDevice.Default)
        {
            Width = (int)(DisplayDevice.Default.Width / 1.5);
            Height = (int)((double)Width / settings.width * settings.height);
            Location = new Point((DisplayDevice.Default.Width - Width) / 2, (DisplayDevice.Default.Height - Height) / 2);
            textEngine = new GLTextEngine();
            render = renderer;
            this.settings = settings;
            lastTempo = midi.zerothTempo;
            lock (render)
            {
                render.renderer.LastMidiTimePerTick = (double)midi.zerothTempo / midi.division;
                midiTime = -render.renderer.NoteScreenTime;
                tempoFrameStep = ((double)midi.division / lastTempo) * (1000000 / settings.fps);
                midiTime -= tempoFrameStep * settings.renderSecondsDelay  *settings.fps;
            }
            //WindowBorder = WindowBorder.Hidden;
            globalDisplayNotes = midi.globalDisplayNotes;
            globalTempoEvents = midi.globalTempoEvents;
            globalColorEvents = midi.globalColorEvents;
            this.midi = midi;
            if (settings.ffRender)
            {
                pixels = new byte[settings.width * settings.height * 4];
                string args = "-hide_banner";
                if (settings.includeAudio)
                {
                    double fstep = ((double)midi.division / lastTempo) * (1000000 / settings.fps);
                    double offset = -midiTime / fstep / settings.fps;
                    offset = Math.Round(offset * 100) / 100;
                    args = "" +
                        " -f rawvideo -s " + settings.width + "x" + settings.height +
                        " -pix_fmt rgb32 -r " + settings.fps + " -i -" +
                        " -itsoffset " + offset.ToString().Replace(",", ".") + " -i \"" + settings.audioPath + "\"" +
                        " -vf vflip -vcodec libx264 -pix_fmt yuv420p -acodec aac";
                }
                else
                {
                    args = "" +
                        " -f rawvideo -s " + settings.width + "x" + settings.height +
                        " -strict -2" +
                        " -pix_fmt rgb32 -r " + settings.fps + " -i -" +
                        " -vf vflip -vcodec libx264 -pix_fmt yuv420p";
                }
                if (settings.useBitrate)
                {
                    args += " -b:v " + settings.bitrate + "k" +
                        " -maxrate " + settings.bitrate + "k" +
                        " -minrate " + settings.bitrate + "k";
                }
                else
                {
                    args += " -preset " + settings.crfPreset + " -crf " + settings.crf;
                }
                args += " -y \"" + settings.ffPath + "\"";
                ffmpeg.StartInfo = new ProcessStartInfo("ffmpeg", args);
                ffmpeg.StartInfo.RedirectStandardInput = true;
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.RedirectStandardError = !settings.ffmpegDebug;
                try
                {
                    ffmpeg.Start();
                    if (!settings.ffmpegDebug)
                    {
                        Console.OpenStandardOutput();
                        Regex messageMatch = new Regex("\\[.*@.*\\]");
                        ffmpeg.ErrorDataReceived += (s, e) =>
                        {
                            if (e.Data == null) return;
                            if (e.Data.Contains("frame="))
                            {
                                Console.Write(e.Data);
                                Console.SetCursorPosition(0, Console.CursorTop);
                            }
                            if (e.Data.Contains("Conversion failed!"))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("An error occured in FFMPEG, closing!");
                                Console.ResetColor();
                                settings.running = false;
                            }
                            if (messageMatch.IsMatch(e.Data))
                            {
                                Console.WriteLine(e.Data);
                            }
                        };
                        ffmpeg.BeginErrorReadLine();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There was an error starting the ffmpeg process\nNo video will be written\n(Is ffmpeg.exe in the same folder as this program?)\n\n\"" + ex.Message + "\"");
                    settings.ffRender = false;
                }
            }

            finalCompositeBuff = new GLPostbuffer(settings);

            GL.GenBuffers(1, out screenQuadBuffer);
            GL.GenBuffers(1, out screenQuadIndexBuffer);

            GL.BindBuffer(BufferTarget.ArrayBuffer, screenQuadBuffer);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(screenQuadArray.Length * 8),
                screenQuadArray,
                BufferUsageHint.StaticDraw);
            
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, screenQuadIndexBuffer);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(screenQuadArrayIndex.Length * 4),
                screenQuadArrayIndex,
                BufferUsageHint.StaticDraw);

            postShader = MakeShader(postShaderVert, postShaderFrag);
        }

        void RenderAllText(long lastNC)
        {
            if (settings.showNoteCount || settings.showNotesRendered)
                if (textEngine.Font != settings.font || textEngine.FontSize != settings.fontSize)
                {
                    textEngine.SetFont(settings.font, settings.fontSize);
                }
            finalCompositeBuff.BindBuffer();

            float offset = 0;
            if (settings.showNotesRendered)
            {
                string text = "Rendering: " + lastNC;
                var size = textEngine.GetBoundBox(text);
                Matrix4 transform = Matrix4.Identity;
                transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-settings.width / 2, -settings.height / 2 + offset, 0));
                transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
                transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / 1920.0f * 2, -1.0f / 1080.0f * 2, 1.0f));

                textEngine.Render(text, transform, Color4.White);

                offset += size.Height;
            }
            if (settings.showNoteCount)
            {
                string text = "asdfsfdb\n34kh5bk234jhhjbgfvd";
                var size = textEngine.GetBoundBox(text);
                Matrix4 transform = Matrix4.Identity;
                transform = Matrix4.Mult(transform, Matrix4.CreateTranslation(-settings.width / 2, -settings.height / 2 + offset, 0));
                transform = Matrix4.Mult(transform, Matrix4.CreateRotationZ(0));
                transform = Matrix4.Mult(transform, Matrix4.CreateScale(1.0f / 1920.0f * 2, -1.0f / 1080.0f * 2, 1.0f));

                textEngine.Render(text, transform, Color4.White);
            }
        }

        double lastTempo;
        public double lastDeltaTimeOnScreen = 0;
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            tempoFrameStep = ((double)midi.division / lastTempo) * (1000000 / settings.fps);
            lock (render)
            {
                lastDeltaTimeOnScreen = render.renderer.NoteScreenTime;
            }
            int noNoteFrames = 0;
            long lastNC = 0;
            while (settings.running && noNoteFrames < settings.fps * 5 || midi.unendedTracks != 0)
            {
                if (!settings.paused || settings.forceReRender)
                {
                    lock (render)
                    {
                        try
                        {
                            if (render.disposeQueue.Count != 0)
                                try
                                {
                                    while (true)
                                    {
                                        var r = render.disposeQueue.Dequeue();
                                        if (r.Initialized)
                                            try { r.Dispose(); }
                                            catch { }
                                    }
                                }
                                catch (InvalidOperationException) { }
                            if (!render.renderer.Initialized)
                            {
                                render.renderer.Init();
                                List<Color4[]> trkcolors = new List<Color4[]>();
                                foreach (var t in midi.tracks) trkcolors.Add(t.trkColor);
                                render.renderer.SetTrackColors(trkcolors.ToArray());
                                lock (globalDisplayNotes)
                                {
                                    foreach (Note n in globalDisplayNotes)
                                    {
                                        n.meta = null;
                                    }
                                }
                            }
                            render.renderer.LastMidiTimePerTick = lastTempo / midi.division;
                            lastDeltaTimeOnScreen = render.renderer.NoteScreenTime;
                            SpinWait.SpinUntil(() => midi.currentSyncTime > midiTime + lastDeltaTimeOnScreen + tempoFrameStep || midi.unendedTracks == 0 || !settings.running);
                            if (!settings.running) break;

                            render.renderer.RenderFrame(globalDisplayNotes, midiTime, finalCompositeBuff.BufferID);
                            lastNC = render.renderer.LastNoteCount;
                            RenderAllText(lastNC);
                            if (lastNC == 0 && midi.unendedTracks == 0) noNoteFrames++;
                            else noNoteFrames = 0;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("The renderer has crashed\n" + ex.Message);
                            break;
                        }
                    }
                }
                double mv = 1;
                lock (globalTempoEvents)
                {
                    while (globalTempoEvents.First != null && midiTime + (tempoFrameStep * mv * settings.tempoMultiplier) > globalTempoEvents.First.pos)
                    {
                        var t = globalTempoEvents.Pop();
                        var _t = ((t.pos) - midiTime) / (tempoFrameStep * mv * settings.tempoMultiplier);
                        mv *= 1 - _t;
                        tempoFrameStep = ((double)midi.division / t.tempo) * (1000000.0 / settings.fps);
                        lastTempo = t.tempo;
                        midiTime = t.pos;
                    }
                }
                if (!settings.paused)
                {
                    midiTime += mv * tempoFrameStep * settings.tempoMultiplier;
                }

                while (globalColorEvents.First != null && globalColorEvents.First.pos < midiTime)
                {
                    var c = globalColorEvents.Pop();
                    var track = c.track;
                    if (c.channel == 0x7F)
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            c.track.trkColor[i * 2] = c.col1;
                            c.track.trkColor[i * 2 + 1] = c.col2;
                        }
                    }
                    else
                    {
                        c.track.trkColor[c.channel * 2] = c.col1;
                        c.track.trkColor[c.channel * 2 + 1] = c.col2;
                    }
                }

                if (settings.ffRender)
                {
                    finalCompositeBuff.BindBuffer();
                    IntPtr unmanagedPointer = Marshal.AllocHGlobal(pixels.Length);
                    GL.ReadPixels(0, 0, settings.width, settings.height, PixelFormat.Rgba, PixelType.UnsignedByte, unmanagedPointer);
                    Marshal.Copy(unmanagedPointer, pixels, 0, pixels.Length);
                    if (lastRenderPush != null) lastRenderPush.GetAwaiter().GetResult();
                    lastRenderPush = Task.Run(() => ffmpeg.StandardInput.BaseStream.Write(pixels, 0, pixels.Length));
                    Marshal.FreeHGlobal(unmanagedPointer);
                }

                GL.UseProgram(postShader);
                GLPostbuffer.UnbindBuffers();
                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.Viewport(0, 0, Width, Height);
                finalCompositeBuff.BindTexture();
                DrawScreenQuad();
                GLPostbuffer.UnbindTextures();
                if (settings.ffRender) VSync = VSyncMode.Off;
                else if (settings.vsync) VSync = VSyncMode.On;
                else VSync = VSyncMode.Off;
                try
                {
                    SwapBuffers();
                }
                catch
                {
                    break;
                }
                ProcessEvents();
                double fr = 10000000.0 / watch.ElapsedTicks;
                settings.liveFps = (settings.liveFps * 2 + fr) / 3;
                watch.Reset();
                watch.Start();
            }
            settings.running = false;
            if (settings.ffRender)
            {
                if (lastRenderPush != null) lastRenderPush.GetAwaiter().GetResult();
                ffmpeg.StandardInput.Close();
                ffmpeg.Close();
            }
            try
            {
                while (true)
                {
                    var r = render.disposeQueue.Dequeue();
                    if (!r.Initialized) r.Dispose();
                }
            }
            catch (InvalidOperationException) { }
            try
            {
                render.renderer.Dispose();
            }
            catch { }
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            settings.running = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        void DrawScreenQuad()
        {
            GL.Enable(EnableCap.Blend);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.Enable(EnableCap.Texture2D);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindBuffer(BufferTarget.ArrayBuffer, screenQuadBuffer);
            GL.VertexPointer(2, VertexPointerType.Double, 16, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, screenQuadIndexBuffer);
            GL.IndexPointer(IndexPointerType.Int, 1, 0);
            GL.DrawElements(PrimitiveType.Quads, 4, DrawElementsType.UnsignedInt, IntPtr.Zero);

            GL.Disable(EnableCap.Blend);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.Disable(EnableCap.Texture2D);
        }
    }
}
