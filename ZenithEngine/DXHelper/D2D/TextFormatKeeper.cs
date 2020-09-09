using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace ZenithEngine.DXHelper.D2D
{
    public class TextFormatKeeper : DeviceInitiable
    {
        public TextFormatKeeper(string fontFamily, float fontSize, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch)
        {
            FontFamily = fontFamily;
            FontSize = fontSize;
            FontWeight = fontWeight;
            FontStyle = fontStyle;
            FontStretch = fontStretch;
        }

        public TextFormatKeeper(string fontFamily, float fontSize, FontStyle fontStyle, FontWeight fontWeight)
            : this(fontFamily, fontSize, fontStyle, fontWeight, FontStretch.Normal)
        { }

        public TextFormatKeeper(string fontFamily, float fontSize, FontStyle fontStyle)
            : this(fontFamily, fontSize, fontStyle, FontWeight.Normal)
        { }

        public TextFormatKeeper(string fontFamily, float fontSize)
            : this(fontFamily, fontSize, FontStyle.Normal)
        { }

        public static implicit operator TextFormat(TextFormatKeeper keeper) => keeper.TextFormat;
        public TextFormat TextFormat { get; private set; }

        public string FontFamily { get; }
        public float FontSize { get; }
        public FontWeight FontWeight { get; }
        public FontStyle FontStyle { get; }
        public FontStretch FontStretch { get; }

        public ParagraphAlignment ParagraphAlignment
        {
            get => TextFormat.ParagraphAlignment;
            set => TextFormat.ParagraphAlignment = value;
        }

        public TextAlignment TextAlignment
        {
            get => TextFormat.TextAlignment;
            set => TextFormat.TextAlignment = value;
        }

        protected override void InitInternal()
        {
            TextFormat = dispose.Add(new TextFormat(Device, FontFamily, FontWeight, FontStyle, FontStretch, FontSize ));
        }

        public TextLayout GetLayout(string text, float maxWidth, float minWidth)
        {
            return new TextLayout(Device, text, this, maxWidth, minWidth);
        }

        public TextLayout GetLayout(string text) =>
            GetLayout(text, float.MaxValue, 0);
    }
}
