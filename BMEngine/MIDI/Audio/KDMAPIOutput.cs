using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.MIDI.Audio
{
    public class KDMAPIOutput : IDisposable, IMidiOutput
    {
        static bool initialized = false;
        public static bool Initialized => initialized;

        public static void Init()
        {
            if (initialized) return;
            initialized = true;
            KDMAPI.InitializeKDMAPIStream();
        }

        public static void Terminate()
        {
            if (!initialized) return;
            initialized = false;
            KDMAPI.TerminateKDMAPIStream();
        }

        public KDMAPIOutput() 
        { }

        public void Dispose()
        {
            byte cc = 120;
            byte vv = 0;
            for(int i = 0; i < 16; i++)
            {
                var command = 0b10110000 & i;
                SendEvent((uint)(command | (cc << 8) | (vv << 16)));
            }
            Reset();
        }

        public void SendEvent(uint e)
        {
            if(initialized) KDMAPI.SendDirectData(e);
        }

        public void Reset()
        {
            KDMAPI.ResetKDMAPIStream();
        }
    }
}
