using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicRender
{
    public class Settings
    {
        public int firstNote = 0;
        public int lastNote = 128;
        public double pianoHeight = 0.16;
        public double deltaTimeOnScreen = 294.067;
        public bool sameWidthNotes = true;
        public bool blackNotesAbove = true;

        public string palette = "Random";

        public float noteBrightness = 1;
    }
}
