using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptedRender
{
    public class Settings
    {
        public int firstNote = 0;
        public int lastNote = 128;
        public double deltaTimeOnScreen = 294.067;

        public string palette = "Random";

        public Script currScript = null;
        public long lastScriptChangeTime = 0;
    }
}
