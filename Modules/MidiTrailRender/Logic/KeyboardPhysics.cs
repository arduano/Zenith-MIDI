using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.ModuleUtil;

namespace MIDITrailRender.Logic
{
    public class KeyboardPhysics
    {
        float[] keyPressPos = new float[256];
        float[] keyPressVel = new float[256];

        double lastMidiTime = double.NaN;

        public float this[int i] { get => keyPressPos[i]; }

        public KeyboardPhysics()
        {

        }

        public void UpdateFrom(KeyboardState state, double time)
        {
            if (double.IsNaN(lastMidiTime)) lastMidiTime = time;
            float timeScale = (float)(time - lastMidiTime) * 60;
            for (int i = 0; i < 256; i++)
            {
                if (state.Pressed[i])
                {
                    keyPressVel[i] += 0.1f * timeScale;
                }
                else
                {
                    keyPressVel[i] += -keyPressPos[i] / 1 * timeScale;
                }
                keyPressVel[i] *= (float)Math.Pow(0.5f, timeScale);
                keyPressPos[i] += keyPressVel[i] * timeScale;

                float maxPress = 1;
                if (keyPressPos[i] > maxPress)
                {
                    keyPressPos[i] = maxPress;
                    keyPressVel[i] = 0;
                }
            }
            lastMidiTime = time;
        }
    }
}
