using SharpDX;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.ModuleUtil;

namespace MIDITrailRender.Logic
{
    public struct PhysicsKey
    {
        public float Press { get; set; }
        public float Vel { get; set; }
        public bool Touching { get; set; }
        public Color4 LastLeftColor { get; set; }
        public Color4 LastRightColor { get; set; }
        public Color4 OriginalLeftColor { get; set; }
        public Color4 OriginalRightColor { get; set; }

        public int Key { get; }

        public PhysicsKey(int key)
        {
            Press = 0;
            Vel = 0;
            Touching = false;
            LastLeftColor = new Color4(0, 0, 0, 1);
            LastRightColor = new Color4(0, 0, 0, 1);
            Key = key;
            var black = KeyboardState.IsBlackKey(key);
            var original = black ? new Color4(0, 0, 0, 1) : new Color4(1, 1, 1, 1);
            OriginalLeftColor = original;
            OriginalRightColor = original;
        }

        static Color4 LerpCol(Color4 a, Color4 b, float fac)
        {
            return new Color4(
                Util.Lerp(a.Red, b.Red, fac),
                Util.Lerp(a.Green, b.Green, fac),
                Util.Lerp(a.Blue, b.Blue, fac),
                Util.Lerp(a.Alpha, b.Alpha, fac)
                );
        }

        public Color4 GetLeftCol(bool blend)
        {
            if (!blend) return Touching ? LastLeftColor : OriginalLeftColor;
            return LerpCol(OriginalLeftColor, LastLeftColor, Press);
        }

        public Color4 GetRightCol(bool blend)
        {
            if (!blend) return Touching ? LastRightColor : OriginalRightColor;
            return LerpCol(OriginalRightColor, LastRightColor, Press);
        }

        public void Step(KeyboardState state, float delta)
        {
            Touching = state.Pressed[Key];
            if (Touching)
            {
                LastLeftColor = state.Colors[Key].Left;
                LastRightColor = state.Colors[Key].Right;
            }
            if (Touching)
            {
                Vel += 0.1f * delta;
            }
            else
            {
                Vel += -Press / 1 * delta;
            }
            Vel *= (float)Math.Pow(0.5f, delta);
            Press += Vel * delta;

            float maxPress = 1;
            if (Press > maxPress)
            {
                Press = maxPress;
                Vel = 0;
            }
        }
    }

    public class KeyboardPhysics
    {
        public PhysicsKey[] Keys { get; }

        double lastMidiTime = double.NaN;

        public KeyboardPhysics()
        {
            Keys = new PhysicsKey[256];
            for (int k = 0; k < 256; k++) Keys[k] = new PhysicsKey(k);
        }

        public void UpdateFrom(KeyboardState state, double time)
        {
            if (double.IsNaN(lastMidiTime)) lastMidiTime = time;
            float timeScale = (float)(time - lastMidiTime) * 60;
            for (int k = 0; k < 256; k++)
            {
                Keys[k].Step(state, timeScale);
            }
            lastMidiTime = time;
        }
    }
}
