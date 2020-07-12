using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.MIDI;

namespace ZenithEngine.ModuleUtil
{
    public static class Extensions
    {
        static bool[] blackKeys = new bool[256];
        static bool[] whiteKeys = new bool[256];

        static Extensions()
        {
            for(int i = 0; i < 256; i++)
            {
                blackKeys[i] = KeyboardState.IsBlackKey(i);
                whiteKeys[i] = !KeyboardState.IsBlackKey(i);
            }
        }

        public static IEnumerable<Note> BlackNotesAbove(this IEnumerable<Note> notes) =>
            BlackNotesAbove(notes, true);
        public static IEnumerable<Note> BlackNotesAbove(this IEnumerable<Note> notes, bool blackAbove)
        {
            if (blackAbove)
            {
                foreach (var n in notes)
                {
                    if (whiteKeys[n.Key]) yield return n;
                }

                foreach (var n in notes)
                {
                    if (blackKeys[n.Key]) yield return n;
                }
            }
            else
            {
                foreach (var n in notes)
                {
                    yield return n;
                }
            }
        }
    }
}
