using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ScriptedEngine;
using Font = ScriptedEngine.Font;

namespace ScriptedRender
{
    public enum ScriptType
    {
        Folder, Zip, Zrp, Rar, SevenZip, Tar
    }

    public class Script
    {
        public string filepath;
        public ScriptType filetype;

        public string name;
        public string description = "";
        public bool error = false;
        public Bitmap preview = null;

        public List<Texture> textures = new List<Texture>();
        public List<Font> fonts = new List<Font>();

        public bool hasPreRender = false;
        public bool hasPostRender = false;

        public bool hasCollectorOffset = false;
        public bool hasManualNoteDelete = false;
        public bool hasNoteScreenTime = false;
        public bool hasNoteCount = false;

        public bool hasProfiles = false;

        public dynamic instance;
        public Type renderType;

        public IEnumerable<UISetting> uiSettings = null;
    }
}
