//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Drawing;
//using System.IO;
//using System.IO.Pipes;
//using System.Linq;
//using System.Reflection;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using ZenithEngine;
//using OpenTK;
//using OpenTK.Graphics;
//using OpenTK.Graphics.OpenGL;
//using OpenTK.Input;
//using System.Drawing.Drawing2D;
//using System.Drawing.Text;
//using System.Text.RegularExpressions;
//using System.Drawing.Imaging;
//using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

//namespace Zenith
//{
//    class RenderWindow : GameWindow
//    {
//        #region Shaders
//        string postShaderVert = @"#version 330 compatibility

//in vec3 position;
//out vec2 UV;

//void main()
//{
//    gl_Position = vec4(position.x * 2 - 1, position.y * 2 - 1, position.z * 2 - 1, 1.0f);
//	//color = glColor;
//    UV = vec2(position.x, position.y);
//}
//";
//        string postShaderFlipVert = @"#version 330 compatibility

//in vec3 position;
//out vec2 UV;

//void main()
//{
//    gl_Position = vec4(position.x * 2 - 1, -(position.y * 2 - 1), position.z * 2 - 1, 1.0f);
//	//color = glColor;
//    UV = vec2(position.x, position.y);
//}
//";
//        string postShaderFrag = @"#version 330 compatibility

//in vec2 UV;

//out vec4 color;

//uniform sampler2D TextureSampler;

//void main()
//{
//    color = texture2D( TextureSampler, UV );
//    color.a = sqrt(color.a);
//    color.rgb /= color.a;
//}
//";
//        string postShaderFragDownscale = @"#version 330 compatibility

//in vec2 UV;

//out vec4 color;

//uniform sampler2D TextureSampler;
//uniform vec2 res;
//uniform int factor;

//void main()
//{
//    color = vec4(0, 0, 0, 0);
//    float stepX = 1 / res.x / factor;
//    float stepY = 1 / res.y / factor;
//    for(int i = 0; i < factor; i += 1){
//        for(int j = 0; j < factor; j += 1){
//            color += texture2D(TextureSampler, UV + vec2(i * stepX, j * stepY));
//        }
//    }
//    color /= factor * factor;
//}
//";

//        string postShaderFragAlphaMask = @"#version 330 compatibility

//in vec2 UV;

//out vec4 color;

//uniform sampler2D myTextureSampler;

//void main()
//{
//    color = texture2D( myTextureSampler, UV );
//    color.x = color.w;
//    color.y = color.w;
//    color.z = color.w;
//    color.w = 1;
//}
//";
//        string postShaderFragAlphaMaskColor = @"#version 330 compatibility

//in vec2 UV;

//out vec4 color;

//uniform sampler2D myTextureSampler;

//void main()
//{
//    color = texture2D( myTextureSampler, UV );
//    color.x /= color.w;
//    color.y /= color.w;
//    color.z /= color.w;
//    color.w = 1;
//}
//";
//        string postShaderFragBlackFill = @"#version 330 compatibility

//in vec2 UV;

//out vec4 color;

//uniform sampler2D myTextureSampler;

//void main()
//{
//    color = vec4( 0,0,0,1 );
//}
//";


//        int MakeShader(string vert, string frag)
//        {
//            int _vertexObj = GL.CreateShader(ShaderType.VertexShader);
//            int _fragObj = GL.CreateShader(ShaderType.FragmentShader);
//            int statusCode;
//            string info;

//            GL.ShaderSource(_vertexObj, vert);
//            GL.CompileShader(_vertexObj);
//            info = GL.GetShaderInfoLog(_vertexObj);
//            GL.GetShader(_vertexObj, ShaderParameter.CompileStatus, out statusCode);
//            if (statusCode != 1) throw new ApplicationException(info);

//            GL.ShaderSource(_fragObj, frag);
//            GL.CompileShader(_fragObj);
//            info = GL.GetShaderInfoLog(_fragObj);
//            GL.GetShader(_fragObj, ShaderParameter.CompileStatus, out statusCode);
//            if (statusCode != 1) throw new ApplicationException(info);

//            int shader = GL.CreateProgram();
//            GL.AttachShader(shader, _fragObj);
//            GL.AttachShader(shader, _vertexObj);
//            GL.LinkProgram(shader);
//            return shader;
//        }
//        #endregion

//        void loadImage(Bitmap image, int texID, bool loop, bool linear)
//        {
//            GL.BindTexture(TextureTarget.Texture2D, texID);
//            BitmapData data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
//                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

//            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
//                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

//            if (linear)
//            {
//                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
//                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
//            }
//            else
//            {
//                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
//                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
//            }
//            if (loop)
//            {
//                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
//                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
//            }
//            else
//            {
//                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
//                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
//            }

//            image.UnlockBits(data);
//        }

//        MidiFile midi;

//        RenderStatus settings;

//        public double midiTime = 0;
//        public long frameStartTime = 0;
//        public double tempoFrameStep = 10;

//        Process ffmpegvideo = new Process();
//        Process ffmpegmask = new Process();
//        Task lastRenderPush = null;
//        Task lastRenderPushMask = null;

//        GLPostbuffer finalCompositeBuff;
//        GLPostbuffer downscaleBuff;
//        GLPostbuffer ffmpegOutputBuff;

//        int postShader;
//        int postShaderFlip;
//        int postShaderDownscale;
//        int postShaderMask;
//        int postShaderMaskColor;
//        int postShaderBlackFill;

//        int uDownscaleRes;
//        int uDownscaleFac;

//        int screenQuadBuffer;
//        int screenQuadIndexBuffer;
//        double[] screenQuadArray = new double[] { 0, 0, 0, 1, 1, 1, 1, 0 };
//        int[] screenQuadArrayIndex = new int[] { 0, 1, 2, 3 };

//        byte[] pixels;
//        byte[] pixelsmask;

//        Process startNewFF(string path)
//        {
//            Process ffmpeg = new Process();
//            string args = "-hide_banner";
//            if (settings.IncludeAudio)
//            {
//                double fstep = ((double)midi.Division / lastTempo) * (1000000 / settings.FPS);
//                double offset = -midiTime / fstep / settings.FPS;
//                offset = Math.Round(offset * 100) / 100;
//                args = "" +
//                    " -f rawvideo -s " + settings.PixelWidth / settings.SSAA + "x" + settings.PixelHeight / settings.SSAA +
//                    " -pix_fmt rgb32 -r " + settings.FPS + " -i -" +
//                    " -itsoffset " + offset.ToString().Replace(",", ".") + " -i \"" + settings.AudioInputPath + "\"" + " -vf vflip -pix_fmt yuv420p ";
//                args += settings.CustomFFmpeg ? "" : "-vcodec libx264 -acodec aac";
//            }
//            else
//            {
//                args = "" +
//                    " -f rawvideo -s " + settings.PixelWidth / settings.SSAA + "x" + settings.PixelHeight / settings.SSAA +
//                    " -strict -2" +
//                    " -pix_fmt rgb32 -r " + settings.FPS + " -i -" +
//                    " -vf vflip -pix_fmt yuv420p ";
//                args += settings.CustomFFmpeg ? "" : "-vcodec libx264";
//            }
//            if (settings.UseBitrate)
//            {
//                args += " -b:v " + settings.Bitrate + "k" +
//                    " -maxrate " + settings.Bitrate + "k" +
//                    " -minrate " + settings.Bitrate + "k";
//            }
//            else if (settings.CustomFFmpeg)
//            {
//                args += settings.FFmpegCustomArgs;
//            }
//            else
//            {
//                args += " -preset " + settings.RenderCRFPreset + " -crf " + settings.RenderCRF;
//            }
//            args += " -y \"" + path + "\"";
//            ffmpeg.StartInfo = new ProcessStartInfo("ffmpeg", args);
//            ffmpeg.StartInfo.RedirectStandardInput = true;
//            ffmpeg.StartInfo.UseShellExecute = false;
//            ffmpeg.StartInfo.RedirectStandardError = !settings.FFmpegDebug;
//            try
//            {
//                ffmpeg.Start();
//                if (!settings.FFmpegDebug)
//                {
//                    Console.OpenStandardOutput();
//                    Regex messageMatch = new Regex("\\[.*@.*\\]");
//                    ffmpeg.ErrorDataReceived += (s, e) =>
//                    {
//                        if (e.Data == null) return;
//                        if (e.Data.Contains("frame="))
//                        {
//                            Console.Write(e.Data);
//                            Console.SetCursorPosition(0, Console.CursorTop);
//                        }
//                        if (e.Data.Contains("Conversion failed!"))
//                        {
//                            Console.ForegroundColor = ConsoleColor.Red;
//                            Console.WriteLine("An error occured in FFMPEG, closing!");
//                            Console.ResetColor();
//                            settings.Running = false;
//                        }
//                        if (messageMatch.IsMatch(e.Data))
//                        {
//                            Console.WriteLine(e.Data);
//                        }
//                    };
//                    ffmpeg.BeginErrorReadLine();
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show("There was an error starting the ffmpeg process\nNo video will be written\n(Is ffmpeg.exe in the same folder as this program?)\n\n\"" + ex.Message + "\"");
//                settings.IsRendering = false;
//            }
//            return ffmpeg;
//        }

//        protected override void OnResize(EventArgs e)
//        {
//            base.OnResize(e);
//        }

//        CurrentRendererPointer render;
//        GLTextEngine textEngine;
//        public RenderWindow(CurrentRendererPointer renderer, MidiFile midi, RenderStatus settings) : base(16, 9, new GraphicsMode(new ColorFormat(8, 8, 8, 8)), "Render", GameWindowFlags.Default, DisplayDevice.Default)
//        {
//            Width = (int)(DisplayDevice.Default.Width / 1.5);
//            Height = (int)((double)Width / settings.PixelWidth * settings.PixelHeight);
//            Location = new Point((DisplayDevice.Default.Width - Width) / 2, (DisplayDevice.Default.Height - Height) / 2);
//            //textEngine = new GLTextEngine();
//            render = renderer;
//            this.settings = settings;
//            lock (render)
//            {
//                midiTime = -render.renderer.StartOffset;
//                if (settings.TimeBased) tempoFrameStep = 1000.0 / settings.FPS;
//                else tempoFrameStep = ((double)midi.Division / lastTempo) * (1000000 / settings.FPS);
//                midiTime -= tempoFrameStep * settings.RenderStartDelay * settings.FPS;
//            }

//            this.midi = midi;
//            if (settings.IsRendering)
//            {
//                pixels = new byte[settings.PixelWidth * settings.PixelHeight * 4 / settings.SSAA / settings.SSAA];
//                ffmpegvideo = startNewFF(settings.RenderOutput);
//                if (settings.IsRenderingMask)
//                {
//                    pixelsmask = new byte[settings.PixelWidth * settings.PixelHeight * 4 / settings.SSAA / settings.SSAA];
//                    ffmpegmask = startNewFF(settings.RenderMaskOutput);
//                }
//            }

//            finalCompositeBuff = new GLPostbuffer(settings.PixelWidth, settings.PixelHeight);
//            ffmpegOutputBuff = new GLPostbuffer(settings.PixelWidth / settings.SSAA, settings.PixelHeight / settings.SSAA);
//            downscaleBuff = new GLPostbuffer(settings.PixelWidth / settings.SSAA, settings.PixelHeight / settings.SSAA);

//            GL.GenBuffers(1, out screenQuadBuffer);
//            GL.GenBuffers(1, out screenQuadIndexBuffer);

//            GL.BindBuffer(BufferTarget.ArrayBuffer, screenQuadBuffer);
//            GL.BufferData(
//                BufferTarget.ArrayBuffer,
//                (IntPtr)(screenQuadArray.Length * 8),
//                screenQuadArray,
//                BufferUsageHint.StaticDraw);

//            GL.BindBuffer(BufferTarget.ElementArrayBuffer, screenQuadIndexBuffer);
//            GL.BufferData(
//                BufferTarget.ElementArrayBuffer,
//                (IntPtr)(screenQuadArrayIndex.Length * 4),
//                screenQuadArrayIndex,
//                BufferUsageHint.StaticDraw);

//            postShader = MakeShader(postShaderVert, postShaderFrag);
//            postShaderFlip = MakeShader(postShaderFlipVert, postShaderFrag);
//            postShaderMask = MakeShader(postShaderVert, postShaderFragAlphaMask);
//            postShaderMaskColor = MakeShader(postShaderVert, postShaderFragAlphaMaskColor);
//            postShaderDownscale = MakeShader(postShaderVert, postShaderFragDownscale);
//            postShaderBlackFill = MakeShader(postShaderVert, postShaderFragBlackFill);

//            uDownscaleRes = GL.GetUniformLocation(postShaderDownscale, "res");
//            uDownscaleFac = GL.GetUniformLocation(postShaderDownscale, "factor");
//        }

//        double microsecondsPerTick = 0;
//        bool playbackLoopStarted = false;
//        //void PlaybackLoop()
//        //{
//        //    PlaybackEvent pe;
//        //    int timeJump;
//        //    long now;
//        //    playbackLoopStarted = true;
//        //    return;
//        //    if (settings.IsRendering) return;
//        //    if (settings.Paused || !settings.PreviewAudioEnabled)
//        //    {
//        //        SpinWait.SpinUntil(() => !(settings.Paused || !settings.PreviewAudioEnabled));
//        //    }
//        //    KDMAPI.ResetKDMAPIStream();
//        //    KDMAPI.SendDirectData(0x0);
//        //    while (settings.Running)
//        //    {
//        //        if (settings.Paused || !settings.PreviewAudioEnabled)
//        //        {
//        //            SpinWait.SpinUntil(() => !(settings.Paused || !settings.PreviewAudioEnabled));
//        //        }
//        //        try
//        //        {
//        //            if (globalPlaybackEvents.ZeroLen) continue;
//        //            pe = globalPlaybackEvents.Pop();
//        //            now = DateTime.Now.Ticks;
//        //            if (now - 10000000 > frameStartTime)
//        //            {
//        //                SpinWait.SpinUntil(() => now - 10000000 < frameStartTime);
//        //            }
//        //            timeJump = (int)(((pe.time - midiTime) * microsecondsPerTick / settings.PreviewSpeed - now + frameStartTime) / 10000);
//        //            if (timeJump < -1000)
//        //                continue;
//        //            if (timeJump > 0)
//        //                Thread.Sleep(timeJump);
//        //            if (settings.PreviewAudioEnabled)
//        //                try
//        //                {
//        //                    KDMAPI.SendDirectData((uint)pe.val);
//        //                }
//        //                catch { continue; }
//        //        }
//        //        catch { continue; }
//        //    }
//        //    KDMAPI.ResetKDMAPIStream();
//        //    KDMAPI.SendDirectData(0x0);
//        //}

//        double lastTempo;
//        public double lastDeltaTimeOnScreen = 0;
//        public double lastMV = 1;

//        int bgTexID = -1;
//        long lastBGChangeTime = -1;
//        protected override void OnRenderFrame(FrameEventArgs e)
//        {
//            midi.StartPlaybackParse(0);

//            //Task.Factory.StartNew(() => PlaybackLoop(), TaskCreationOptions.LongRunning);
//            //SpinWait.SpinUntil(() => playbackLoopStarted);
//            Stopwatch watch = new Stopwatch();
//            watch.Start();
//            if (!settings.TimeBased) tempoFrameStep = ((double)midi.Division / lastTempo) * (1000000.0 / settings.FPS);

//            int noNoteFrames = 0;
//            long lastNC = 0;
//            bool firstRenderer = true;
//            frameStartTime = DateTime.Now.Ticks;
//            if (settings.TimeBased) microsecondsPerTick = 10000;
//            else microsecondsPerTick = (long)((double)lastTempo / midi.Division * 10);
//            midi.ParseUpTo(midiTime);
//            while (settings.Running && (noNoteFrames < settings.FPS * 5 || midi.RemainingTracks != 0))
//            {
//                midi.AdvancePlayback(midi.TimeSeconds + 1.0 / settings.FPS);
//                if (!settings.Paused)
//                {
//                    if (settings.LastBGChangeTime != lastBGChangeTime)
//                    {
//                        if (settings.BGImage == null)
//                        {
//                            if (bgTexID != -1) GL.DeleteTexture(bgTexID);
//                            bgTexID = -1;
//                        }
//                        else
//                        {
//                            if (bgTexID == -1) bgTexID = GL.GenTexture();
//                            try
//                            {
//                                loadImage(new Bitmap(settings.BGImage), bgTexID, false, true);
//                            }
//                            catch
//                            {
//                                MessageBox.Show("Couldn't load image");
//                                if (bgTexID != -1) GL.DeleteTexture(bgTexID);
//                                bgTexID = -1;
//                            }
//                        }
//                        lastBGChangeTime = settings.LastBGChangeTime;
//                    }

//                    lock (render)
//                    {
//                        //try
//                        //{
//                        if (render.disposeQueue.Count != 0)
//                            try
//                            {
//                                while (true)
//                                {
//                                    var r = render.disposeQueue.Dequeue();
//                                    if (r.Initialized)
//                                    {
//                                        try
//                                        {
//                                            r.Dispose();
//                                        }
//                                        catch { }
//                                        GC.Collect();
//                                    }
//                                }
//                            }
//                            catch (InvalidOperationException) { }
//                        if (!render.renderer.Initialized)
//                        {
//                            render.renderer.Init(midi);
//                            render.renderer.NoteColors = midi.Tracks.Select(t => t.TrackColors).ToArray();
//                            render.renderer.ReloadTrackColors();
//                            if (firstRenderer)
//                            {
//                                firstRenderer = false;
//                            }
//                            //render.renderer.CurrentMidi = midi.info;
//                            foreach (Note n in midi.Notes)
//                            {
//                                n.meta = null;
//                            }
//                        }
//                        render.renderer.Tempo = 60000000.0 / lastTempo;
//                        if (!settings.Running) break;

//                        render.renderer.RenderFrame(finalCompositeBuff.BufferID);
//                        lastNC = render.renderer.LastNoteCount;
//                        if (lastNC == 0 && midi.RemainingTracks == 0) noNoteFrames++;
//                        else noNoteFrames = 0;
//                        //}
//                        //catch (Exception ex)
//                        //{
//                        //    MessageBox.Show("The renderer has crashed\n" + ex.Message + "\n" + ex.StackTrace);
//                        //    break;
//                        //}
//                    }
//                }

//                downscaleBuff.BindBuffer();
//                GL.Clear(ClearBufferMask.ColorBufferBit);
//                GL.Viewport(0, 0, settings.PixelWidth / settings.SSAA, settings.PixelHeight / settings.SSAA);
//                if (bgTexID != -1)
//                {
//                    GL.UseProgram(postShaderFlip);
//                    GL.BindTexture(TextureTarget.Texture2D, bgTexID);
//                    DrawScreenQuad();
//                }

//                if (settings.SSAA > 1)
//                {
//                    GL.UseProgram(postShaderDownscale);
//                    GL.Uniform1(uDownscaleFac, (int)settings.SSAA);
//                    GL.Uniform2(uDownscaleRes, new Vector2(settings.PixelWidth / settings.SSAA, settings.PixelHeight / settings.SSAA));
//                }
//                else
//                {
//                    GL.UseProgram(postShader);
//                }

//                finalCompositeBuff.BindTexture();
//                DrawScreenQuad();

//                if (settings.IsRendering)
//                {
//                    if (ffmpegvideo.HasExited || (settings.IsRenderingMask && ffmpegmask.HasExited))
//                    {
//                        Console.ForegroundColor = ConsoleColor.Red;
//                        Console.WriteLine("FFMPEG process closed unexpectedly!");
//                        Console.WriteLine("Use 'ffmpeg debug' for more advanced info.");
//                        Console.ResetColor();
//                        settings.Running = false;
//                    }

//                    if (!settings.IsRenderingMask)
//                        GL.UseProgram(postShader);
//                    else
//                        GL.UseProgram(postShaderMaskColor);
//                    finalCompositeBuff.BindTexture();
//                    ffmpegOutputBuff.BindBuffer();
//                    GL.Clear(ClearBufferMask.ColorBufferBit);
//                    GL.Viewport(0, 0, settings.PixelWidth / settings.SSAA, settings.PixelHeight / settings.SSAA);
//                    downscaleBuff.BindTexture();
//                    DrawScreenQuad();
//                    IntPtr unmanagedPointer = Marshal.AllocHGlobal(pixels.Length);
//                    GL.ReadPixels(0, 0, settings.PixelWidth / settings.SSAA, settings.PixelHeight / settings.SSAA, PixelFormat.Bgra, PixelType.UnsignedByte, unmanagedPointer);
//                    Marshal.Copy(unmanagedPointer, pixels, 0, pixels.Length);

//                    if (lastRenderPush != null) lastRenderPush.GetAwaiter().GetResult();
//                    lastRenderPush = Task.Run(() =>
//                    {
//                        ffmpegvideo.StandardInput.BaseStream.Write(pixels, 0, pixels.Length);
//                    });
//                    Marshal.FreeHGlobal(unmanagedPointer);

//                    if (settings.IsRenderingMask)
//                    {
//                        if (lastRenderPushMask != null) lastRenderPushMask.GetAwaiter().GetResult();
//                        GL.UseProgram(postShaderMask);
//                        ffmpegOutputBuff.BindBuffer();
//                        GL.Clear(ClearBufferMask.ColorBufferBit);
//                        GL.Viewport(0, 0, settings.PixelWidth / settings.SSAA, settings.PixelHeight / settings.SSAA);
//                        downscaleBuff.BindTexture();
//                        DrawScreenQuad();
//                        unmanagedPointer = Marshal.AllocHGlobal(pixelsmask.Length);
//                        GL.ReadPixels(0, 0, settings.PixelWidth / settings.SSAA, settings.PixelHeight / settings.SSAA, PixelFormat.Bgra, PixelType.UnsignedByte, unmanagedPointer);
//                        Marshal.Copy(unmanagedPointer, pixelsmask, 0, pixelsmask.Length);

//                        if (lastRenderPush != null) lastRenderPush.GetAwaiter().GetResult();
//                        lastRenderPush = Task.Run(() =>
//                        {
//                            ffmpegmask.StandardInput.BaseStream.Write(pixelsmask, 0, pixelsmask.Length);
//                        });
//                        Marshal.FreeHGlobal(unmanagedPointer);
//                    }
//                }

//                GLPostbuffer.UnbindBuffers();
//                GL.Clear(ClearBufferMask.ColorBufferBit);
//                GL.UseProgram(postShaderBlackFill);
//                DrawScreenQuad();
//                GL.UseProgram(postShader);
//                GL.Viewport(0, 0, Width, Height);
//                downscaleBuff.BindTexture();
//                DrawScreenQuad();
//                GLPostbuffer.UnbindTextures();
//                if (settings.IsRendering) VSync = VSyncMode.Off;
//                else if (settings.VSync) VSync = VSyncMode.On;
//                else VSync = VSyncMode.Off;
//                try
//                {
//                    SwapBuffers();
//                }
//                catch
//                {
//                    break;
//                }
//                ProcessEvents();
//                double fr = 10000000.0 / watch.ElapsedTicks;
//                settings.CurrentFPS = (settings.CurrentFPS * 2 + fr) / 3;
//                watch.Reset();
//                watch.Start();
//            }
//            Console.WriteLine("Left render loop");
//            settings.Running = false;
//            if (settings.IsRendering)
//            {
//                if (lastRenderPush != null) lastRenderPush.GetAwaiter().GetResult();
//                ffmpegvideo.StandardInput.Close();
//                ffmpegvideo.Close();
//                if (settings.IsRenderingMask)
//                {
//                    if (lastRenderPushMask != null) lastRenderPushMask.GetAwaiter().GetResult();
//                    ffmpegmask.StandardInput.Close();
//                    ffmpegmask.Close();
//                }
//            }
//            Console.WriteLine("Disposing current renderer");
//            try
//            {
//                render.renderer.Dispose();
//            }
//            catch { }
//            try
//            {
//                Console.WriteLine("Disposing of other renderers");
//                while (render.disposeQueue.Count != 0)
//                {
//                    var r = render.disposeQueue.Dequeue();
//                    try
//                    {
//                        if (r.Initialized) r.Dispose();
//                    }
//                    catch { }
//                }
//            }
//            catch (InvalidOperationException) { }
//            Console.WriteLine("Disposed of renderers");

//            pixels = null;
//            pixelsmask = null;
//            if (settings.IsRendering)
//            {
//                ffmpegvideo.Dispose();
//                if (settings.IsRenderingMask) ffmpegmask.Dispose();
//            }
//            ffmpegvideo = null;
//            ffmpegmask = null;


//            finalCompositeBuff.Dispose();
//            ffmpegOutputBuff.Dispose();
//            downscaleBuff.Dispose();
//            finalCompositeBuff = null;
//            ffmpegOutputBuff = null;
//            downscaleBuff = null;

//            GL.DeleteBuffers(2, new int[] { screenQuadBuffer, screenQuadIndexBuffer });

//            GL.DeleteProgram(postShader);
//            GL.DeleteProgram(postShaderMask);
//            GL.DeleteProgram(postShaderMaskColor);
//            GL.DeleteProgram(postShaderDownscale);

//            midi = null;
//            render = null;
//            Console.WriteLine("Closing window");

//            this.Close();
//        }

//        protected override void OnKeyDown(KeyboardKeyEventArgs e)
//        {
//            base.OnKeyDown(e);
//            //if (e.Key == Key.Space && !settings.IsRendering) settings.Paused = !settings.Paused;
//            //if (e.Key == Key.Right && !settings.IsRendering)
//            //{
//            //    int skip = 5000;
//            //    if (e.Modifiers == KeyModifiers.Control) skip = 20000;
//            //    if (e.Modifiers == KeyModifiers.Shift) skip = 60000;
//            //    if (settings.TimeBased) midiTime += skip;
//            //    else
//            //    {
//            //        lock (midi)
//            //        {
//            //            double timeSkipped = 0;
//            //            for (; timeSkipped < skip; midiTime++)
//            //            {
//            //                midi.ParseUpTo(midiTime);
//            //                timeSkipped += 1 / midi.tempoTickMultiplier;
//            //            }
//            //        }
//            //    }
//            //}
//            //if (e.Key == Key.Enter)
//            //{
//            //    if (WindowState != WindowState.Fullscreen)
//            //        WindowState = WindowState.Fullscreen;
//            //    else
//            //        WindowState = WindowState.Normal;
//            //}
//        }

//        protected override void OnClosing(CancelEventArgs e)
//        {
//            base.OnClosing(e);

//            settings.Running = false;
//        }

//        protected override void OnClosed(EventArgs e)
//        {
//            base.OnClosed(e);

//            settings.Running = false;
//        }

//        void DrawScreenQuad()
//        {
//            GL.Enable(EnableCap.Blend);
//            GL.EnableClientState(ArrayCap.VertexArray);
//            GL.Enable(EnableCap.Texture2D);
//            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

//            GL.BindBuffer(BufferTarget.ArrayBuffer, screenQuadBuffer);
//            GL.VertexPointer(2, VertexPointerType.Double, 16, 0);

//            GL.BindBuffer(BufferTarget.ElementArrayBuffer, screenQuadIndexBuffer);
//            GL.IndexPointer(IndexPointerType.Int, 1, 0);
//            GL.DrawElements(PrimitiveType.Quads, 4, DrawElementsType.UnsignedInt, IntPtr.Zero);

//            GL.Disable(EnableCap.Blend);
//            GL.DisableClientState(ArrayCap.VertexArray);
//            GL.Disable(EnableCap.Texture2D);
//        }
//    }
//}
