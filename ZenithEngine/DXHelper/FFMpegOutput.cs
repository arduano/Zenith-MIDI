using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.DXHelper.Presets;

namespace ZenithEngine.DXHelper
{
    public class FFMpegException : Exception
    {
        public FFMpegException() : base("FFMPEG process has run into an error") { }
    }

    public class FFMpegOutput : IDisposable
    {
        CompositeRenderSurface staging;

        Initiator init = new Initiator();

        Texture2D stagingTexture;
        Compositor composite;
        ShaderProgram basicShader;

        Task writeTask = null;
        Task diagnostic = null;

        Process process;

        int frameSize;

        DeviceContext lastContext = null;

        Stream debugOutput;

        public FFMpegOutput(Device device, int width, int height, int fps, string extraArgs, string output, Stream debugOutput = null)
        {
            this.debugOutput = debugOutput;

            string mainArgs = $"-f rawvideo -s {width}x{height} -pix_fmt rgb32 -r {fps} -i -";
            string args = $"{mainArgs} {extraArgs} \"{output}\" -y";
            process = new Process();
            process.StartInfo = new ProcessStartInfo("ffmpeg", args);
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = debugOutput != null;
            process.Start();

            frameSize = width * height * 4;

            staging = init.Add(new CompositeRenderSurface(width, height, format: SharpDX.DXGI.Format.R8G8B8A8_UNorm));
            composite = init.Add(new Compositor());
            basicShader = init.Add(Shaders.BasicTextured());

            init.Init(device);

            if (debugOutput != null)
            {
                diagnostic = Task.Run(() =>
                {
                    try
                    {
                        process.StandardError.BaseStream.CopyTo(debugOutput);
                        debugOutput.Close();
                    }
                    catch { }
                });
            }

            stagingTexture = new Texture2D(device, new Texture2DDescription()
            {
                Width = width,
                Height = height,
                ArraySize = 1,
                BindFlags = BindFlags.None,
                Usage = ResourceUsage.Staging,
                CpuAccessFlags = CpuAccessFlags.Read,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
            });
        }

        public void Dispose()
        {
            writeTask?.Wait();
            if (lastContext != null) lastContext.UnmapSubresource(stagingTexture, 0);
            init.Dispose();
            process.StandardInput.Close();
            process.WaitForExit();
            diagnostic?.Wait();
        }

        public bool HasClosed => process.HasExited;

        public event EventHandler Errored;

        public void PushFrame(DeviceContext context, ITextureResource data)
        {
            if (HasClosed)
            {
                Errored?.Invoke(this, new EventArgs());
                throw new FFMpegException();
            }

            writeTask?.Wait();
            if (lastContext != null) lastContext.UnmapSubresource(stagingTexture, 0);
            composite.Composite(context, data, basicShader, staging);
            context.CopyResource(staging.Texture, stagingTexture);
            DataStream d;
            context.MapSubresource(stagingTexture, 0, MapMode.Read, MapFlags.None, out d);
            writeTask = Task.Run(() =>
            {
                try
                {
                    d.CopyTo(process.StandardInput.BaseStream);
                }
                catch { }
                d.Dispose();
            });
        }
    }
}
