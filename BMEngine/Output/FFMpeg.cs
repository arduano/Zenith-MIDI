using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ZenithEngine.Output
{
    class FFMpeg : IDisposable
    {
        Task writeTask = null;
        Task diagnostic = null;

        public FFMpeg(int width, int height, int fps, string extraArgs, string output)
        {
            string mainArgs = $"-f rawvideo -s {width}x{height} -pix_fmt rgb32 -r {fps} -i - -vf vflip";
            string.Join()
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
