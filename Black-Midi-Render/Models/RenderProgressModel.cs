using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zenith.Models
{
    public class RenderProgressModel
    {
        public double Progress => (Seconds - StartSeconds) / EndSeconds;

        public double StartSeconds { get; }
        public double EndSeconds { get; }
        public double Seconds { get; }

        public long Ticks { get; }

        public long NotesRendering { get; }
    }
}
