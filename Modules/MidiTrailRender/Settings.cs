using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIDITrailRender
{
    public class Settings
    {
        public int firstNote = 0;
        public int lastNote = 128;
        public double deltaTimeOnScreen = 400;
        public bool sameWidthNotes = true;

        public double FOV = 3.1415 / 3;
        public double viewHeight = 0.5;
        public double viewOffset = 0.4;
        public double viewPan = 0.0;
        public double camAng = 0.56;
        public double camRot = 0;
        public double camSpin = 0;
        public double viewdist = 14;
        public double viewback = 0.2;

        public bool verticalNotes = false;

        public double noteDownSpeed = 0.6;
        public double noteUpSpeed = 0.2;
        public bool boxNotes = false;
        public bool lightShade = false;
        public bool showKeyboard = true;

        public bool tiltKeys = true;

        public bool eatNotes = false;

        public string palette = "Random";

        public string selectedAuraImage = "ring";
        public double auraStrength = 2;
        public bool auraEnabled = true;

        public bool useVel = false;

        public bool notesChangeSize = false;
        public bool notesChangeTint = true;


        public float noteBrightness = 1;

        public bool tickBased = true;
    }
}
