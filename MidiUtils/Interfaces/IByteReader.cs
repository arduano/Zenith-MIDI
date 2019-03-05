using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiUtils
{
    public interface IByteReader : IDisposable
    {
        byte Read();
        void Reset();
        void Skip(int count);
        int Pushback { set; get; }
        long Location
        {
            get;
        }
    }
}
