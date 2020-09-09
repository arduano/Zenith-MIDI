using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NoteCountRender
{
    public enum Alignments
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        TopSpread,
        BottomSpread,
    }

    class BaseModel : INotifyPropertyChanged
    {
        public SettingsModel State { get; set; } = new SettingsModel();
        string[] AllFonts { get; set; } = null;

        public BaseModel()
        {
            var fonts = Fonts.SystemFontFamilies;
            AllFonts = fonts.Select(f => f.Source).ToArray();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    class SettingsModel : INotifyPropertyChanged
    {
        public string TextTemplate { get; set; } = 
@"Notes: {nc}/{nc-max}/{nc-rem}
Seconds: {sec}/{sec-max}/{sec-rem}
Polyphony: {plph}
NPS:
{nps-2}
{nps-1}
{nps-05}
{nps-025}";

        public Alignments Alignment { get; set; } = Alignments.TopLeft;
        public string FontName { get; set; } = "Arial";
        public int FontSize { get; set; } = 32;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
