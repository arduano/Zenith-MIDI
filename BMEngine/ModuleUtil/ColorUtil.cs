using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.ModuleUtil
{
    public static class ColorUtil
    {
        public static Color4 ToDX(this System.Drawing.Color color) => new Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        public static System.Drawing.Color ToDrawing(this Color4 color) => System.Drawing.Color.FromArgb(
            (int)(color.Alpha * 255),
            (int)(color.Red * 255),
            (int)(color.Green * 255),
            (int)(color.Blue * 255)
        );

        public static Color4 FromHsl(float r, float g, float b, float a)
        {
            return FromHsl(new Vector4(r, g, b, a));
        }

        public static Color4 FromHsv(float r, float g, float b, float a)
        {
            return FromHsv(new Vector4(r, g, b, a));
        }

        public static Color4 FromHsv(Vector4 hsv)
        {
            var hue = hsv.X * 360.0f;
            var saturation = hsv.Y;
            var value = hsv.Z;

            var c = value * saturation;

            var h = hue / 60.0f;
            var x = c * (1.0f - Math.Abs((h % 2.0f) - 1.0f));

            float r, g, b;
            if (h >= 0.0f && h < 1.0f)
            {
                r = c;
                g = x;
                b = 0.0f;
            }
            else if (h >= 1.0f && h < 2.0f)
            {
                r = x;
                g = c;
                b = 0.0f;
            }
            else if (h >= 2.0f && h < 3.0f)
            {
                r = 0.0f;
                g = c;
                b = x;
            }
            else if (h >= 3.0f && h < 4.0f)
            {
                r = 0.0f;
                g = x;
                b = c;
            }
            else if (h >= 4.0f && h < 5.0f)
            {
                r = x;
                g = 0.0f;
                b = c;
            }
            else if (h >= 5.0f && h < 6.0f)
            {
                r = c;
                g = 0.0f;
                b = x;
            }
            else
            {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            var m = value - c;
            return new Color4(r + m, g + m, b + m, hsv.W);
        }

        public static Color4 FromHsl(Vector4 hsl)
        {
            var hue = hsl.X * 360.0f;
            var saturation = hsl.Y;
            var lightness = hsl.Z;

            var c = (1.0f - Math.Abs((2.0f * lightness) - 1.0f)) * saturation;

            var h = hue / 60.0f;
            var x = c * (1.0f - Math.Abs((h % 2.0f) - 1.0f));

            float r, g, b;
            if (h >= 0.0f && h < 1.0f)
            {
                r = c;
                g = x;
                b = 0.0f;
            }
            else if (h >= 1.0f && h < 2.0f)
            {
                r = x;
                g = c;
                b = 0.0f;
            }
            else if (h >= 2.0f && h < 3.0f)
            {
                r = 0.0f;
                g = c;
                b = x;
            }
            else if (h >= 3.0f && h < 4.0f)
            {
                r = 0.0f;
                g = x;
                b = c;
            }
            else if (h >= 4.0f && h < 5.0f)
            {
                r = x;
                g = 0.0f;
                b = c;
            }
            else if (h >= 5.0f && h < 6.0f)
            {
                r = c;
                g = 0.0f;
                b = x;
            }
            else
            {
                r = 0.0f;
                g = 0.0f;
                b = 0.0f;
            }

            var m = lightness - (c / 2.0f);
            if (m < 0)
            {
                m = 0;
            }
            return new Color4(r + m, g + m, b + m, hsl.W);
        }
    }
}
