using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMEngine
{
    public class MidiInfo
    {
        public int division;
        public int trackCount;
        public long noteCount;
        public int firstTempo;
        public long tickLength;
        public double secondsLength;
        public TimeSignature timeSig = new TimeSignature() { numerator = 4, denominator = 4 };
    }
}
