using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Effects;

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
        public bool PaddingZeroes = false;
        public string csvOutput = "";
        public string csvFormat = "{nps},{plph},{bpm},{nc}";

        public int BPMintPad = 3;
        public int BPMDecPtPad = 2;
        public int NoteCountPad = 5;
        public int PolyphonyPad = 3;
        public int NPSPad = 3;
        public int TicksPad = 5;
        public int BarCountPad = 3;
        public int FrCountPad = 5;
    }
    public class Zeroes
    {
        public string bpm = "0.00";
        public string nc = "0";
        public string plph = "0";
        public string nps = "0";
        public string tick = "0";
        public string bars = "0";
        public string frms = "0";
    }
}
