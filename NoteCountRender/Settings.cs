using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public enum Commas
    {
        Comma,
        Dot,
        Nothing,
    }
    public class Settings
    {
        public string text = "Notes: {nc} / {tn}\nBPM: {bpm}\nNPS: {nps}\nPPQ: {ppq}\nPolyphony: {plph}\nSeconds: {seconds}\nTime: {time}\nTicks: {ticks}";
        public Alignments textAlignment = Alignments.TopLeft;

        public int fontSize = 40;
        public string fontName = "Arial";
        public System.Drawing.FontStyle fontStyle = System.Drawing.FontStyle.Regular;

        public Commas thousandSeparator = Commas.Comma;

        public bool saveCsv = false;
        public string csvOutput = "";
        public string csvFormat = "{nps},{plph},{bpm},{nc}";
    }
}
