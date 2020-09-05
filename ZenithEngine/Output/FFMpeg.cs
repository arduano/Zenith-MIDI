using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using ZenithEngine.GLEngine;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

namespace ZenithEngine.Output
{
    public class FFMpeg : IDisposable
    {
        Task writeTask = null;
        Task diagnostic = null;

        Process process;

        int frameSize;
        byte[] bufferFrame;

        public FFMpeg(int width, int height, int fps, string extraArgs, string output)
        {
            string mainArgs = $"-f rawvideo -s {width}x{height} -pix_fmt rgb32 -r {fps} -i -";
            string args = $"{mainArgs} {extraArgs} \"{output}\" -y";
            process = new Process();
            process.StartInfo = new ProcessStartInfo("ffmpeg", args);
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.UseShellExecute = false;
            //process.StartInfo.RedirectStandardError = !settings.ffmpegDebug;
            process.Start();

            frameSize = width * height * 4;
            bufferFrame = new byte[frameSize];
        }

        public unsafe void WriteFrame(RenderSurface surface)
        {
            surface.BindSurface();
            fixed (byte* frame = bufferFrame)
            {
                GL.ReadPixels(0, 0, surface.Width, surface.Height, PixelFormat.Bgra, PixelType.UnsignedByte, (IntPtr)frame);
            }
            if (writeTask != null) writeTask.Wait();
            writeTask = Task.Run(() =>
            {
                process.StandardInput.BaseStream.Write(bufferFrame, 0, bufferFrame.Length);
            });
        }

        public void Dispose()
        {
            if (writeTask != null) writeTask.Wait();
            process.StandardInput.Close();
            process.WaitForExit();
        }
    }
}
