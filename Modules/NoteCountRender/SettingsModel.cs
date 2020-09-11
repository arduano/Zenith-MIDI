using System;
using System.CodeDom;
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

    class FontItem
    {
        public FontItem(string name, FontFamily font)
        {
            Name = name;
            Font = font;
        }

        public string Name { get; }
        public FontFamily Font { get; }
    }

    class BaseModel : INotifyPropertyChanged
    {
        public SettingsModel State { get; set; } = new SettingsModel();
        public FontItem[] AllFonts { get; } = null;
        public FontItem Font { get; set; } = new FontItem("Arial", null);

        public BaseModel()
        {
            var fonts = Fonts.SystemFontFamilies;
            AllFonts = fonts.Select(f => new FontItem(f.Source, f)).OrderBy(f => f.Name).ToArray();

            this.PropertyChanged += BaseModel_PropertyChanged;

            BindState();
        }

        void BindState()
        {
            Font = AllFonts.Where(f => f.Name == State.FontName).FirstOrDefault();
            State.PropertyChanged += State_PropertyChanged;
            State.FontName = Font.Name;
        }

        private void State_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(State.FontName))
            {
                Font = AllFonts.Where(f => f.Name == State.FontName).FirstOrDefault();
            }
        }

        private void BaseModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(State))
            {
                BindState();
            }
            if (e.PropertyName == nameof(Font))
            {
                State.FontName = Font.Name;
            }
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
