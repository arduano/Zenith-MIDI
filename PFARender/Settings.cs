using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PFARender
{
    public enum TopColor
    {
        Red,
        Blue,
        Green
    }

    public class Settings
    {
        public int firstNote = 0;
        public int lastNote = 128;
        public double pianoHeight = 0.151;
        public double deltaTimeOnScreen = 300;
        public double borderWidth = 1;
        public bool sameWidthNotes = false;
        public TopColor topColor = TopColor.Red;
        public bool middleC = false;
        public bool blackNotesAbove = true;

        public float topBarR = .585f;
        public float topBarG = .0392f;
        public float topBarB = .0249f;

        public string palette = "Random";
    }
}
