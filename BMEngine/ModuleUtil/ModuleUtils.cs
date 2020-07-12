using SharpDX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ZenithEngine.ModuleUtil
{
    public static class ModuleUtils
    {
        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
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

        public static Color4 BlendColors(Color4 col1, Color4 col2)
        {
            float blendfac = col2.Alpha;
            float revblendfac = 1 - blendfac;
            return new Color4(
                col2.Red * blendfac + col1.Red * revblendfac,
                col2.Green * blendfac + col1.Green * revblendfac,
                col2.Blue * blendfac + col1.Blue * revblendfac,
                col1.Alpha + (1 - col1.Alpha) * blendfac);
        }

        public static Color4 BlendWith(this Color4 col1, Color4 col2)
        {
            return BlendColors(col1, col2);
        }
    }
}
