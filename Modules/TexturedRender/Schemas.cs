using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ZenithEngine.DXHelper;
using ZenithEngine.IO;

namespace TexturedRender
{
    class OpenerException : Exception
    {
        public OpenerException(string message) : base(message) { }
    };

    public class SwitchSchemaRoot : SchemaLoadable
    {
        public SwitchSchemaRoot(JObject obj, DirectoryFolder folder, string folderBasePath, Dictionary<string, SwitchItem> switches = null)
            : base("", obj, folder, folderBasePath, switches) { }

        public SwitchSchemaItem[] switches = new SwitchSchemaItem[0];
    }

    public enum NoteShaderType
    {
        Normal,
        Inverted,
        Hybrid
    }

    public enum NoteType
    {
        Black,
        White,
        Both
    }

    public class SwitchSchemaItem : SchemaLoadable
    {
        public SwitchSchemaItem(string path, JObject obj, DirectoryFolder folder, string folderBasePath, Dictionary<string, SwitchItem> switches = null)
            : base(path, obj, folder, folderBasePath, switches)
        {
            if (IsSwitch && text != null)
                throw new OpenerException($"{path}{nameof(text)} cannot be used with {nameof(name)} and {nameof(values)}");
        }

        public bool IsSwitch => name != null;

        [RequiredWith(nameof(values))]
        public string name;
        [RequiredWith(nameof(name))]
        public string[] values;

        public string text;
    }

    public class PackSchemaRoot : SchemaLoadable
    {
        public PackSchemaRoot(JObject obj, DirectoryFolder folder, string folderBasePath, Dictionary<string, SwitchItem> switches)
            : base("", obj, folder, folderBasePath, switches)
        {
            if (UseSideKeys && !whiteKeysFullOctave)
                throw new OpenerException($"{nameof(whiteKeysFullOctave)} must be true if using whiteKeyLeft/Right");
        }

        [Required]
        public string description;

        public ImageSource previewImage;

        public bool sameWidthNotes = false;

        [Required]
        public RenderTexture blackKey;
        [Required]
        public RenderTexture blackKeyPressed;
        [Required]
        public RenderTexture whiteKey;
        [Required]
        public RenderTexture whiteKeyPressed;

        public double keyboardHeight = 15;

        public bool UseBar => bar != null;
        public RenderTexture bar = null;
        public double barHeight = 5;

        public bool whiteKeysFullOctave = false;
        public bool blackKeysFullOctave = false;

        public double blackKeyOversize = 0;
        public double blackKeyPressedOversize = 0;

        public double whiteKeyOversize = 0;
        public double whiteKeyPressedOversize = 0;

        public double blackKeyHeight = 40;

        public double blackKey2setOffset = 0.3;
        public double blackKey3setOffset = 0.5;
        public double blackKeyScale = 0.6;

        public double blackNote2setOffset = 0;
        public double blackNote3setOffset = 0;
        public double blackNoteScale = 1;

        [WithLength(5)]
        public double[] advancedBlackKeyOffsets = new double[] { 0, 0, 0, 0, 0 };

        public bool UseSideKeys => whiteKeyRight != null;
        [RequiredWith(nameof(whiteKeyRight), nameof(whiteKeyRightPressed), nameof(whiteKeyLeft), nameof(whiteKeyLeftPressed))]
        public RenderTexture whiteKeyRight;
        [RequiredWith(nameof(whiteKeyRight), nameof(whiteKeyRightPressed), nameof(whiteKeyLeft), nameof(whiteKeyLeftPressed))]
        public RenderTexture whiteKeyRightPressed;
        [RequiredWith(nameof(whiteKeyRight), nameof(whiteKeyRightPressed), nameof(whiteKeyLeft), nameof(whiteKeyLeftPressed))]
        public RenderTexture whiteKeyLeft;
        [RequiredWith(nameof(whiteKeyRight), nameof(whiteKeyRightPressed), nameof(whiteKeyLeft), nameof(whiteKeyLeftPressed))]
        public RenderTexture whiteKeyLeftPressed;

        public NoteShaderType blackKeyShader = NoteShaderType.Normal;
        public NoteShaderType whiteKeyShader = NoteShaderType.Normal;
        public NoteShaderType noteShader = NoteShaderType.Normal;

        public bool blackKeysWhiteShade = true;

        [Required]
        public NoteSchema[] notes;

        public OverlaySchema[] overlays = new OverlaySchema[0];
    }

    public class NoteSchema : SchemaLoadable
    {
        public NoteSchema(string path, JObject obj, DirectoryFolder folder, string folderBasePath, Dictionary<string, SwitchItem> switches = null)
            : base(path, obj, folder, folderBasePath, switches) { }

        [Required]
        public bool alwaysStretch = false;

        public bool squeezeEndCaps = true;

        [Required]
        public RenderTexture middleTexture;

        [Required]
        public bool useEndCaps = true;

        public NoteType keyType = NoteType.Both;

        [RequiredIf(nameof(useEndCaps), true)]
        public RenderTexture topTexture;
        [RequiredIf(nameof(useEndCaps), true)]
        public RenderTexture bottomTexture;

        public double noteTopOversize = 0;
        public double noteBottomOversize = 0;
    }

    public class OverlaySchema : SchemaLoadable
    {
        public OverlaySchema(string path, JObject obj, DirectoryFolder folder, string folderBasePath, Dictionary<string, SwitchItem> switches = null)
            : base(path, obj, folder, folderBasePath, switches) { }

        [Required]
        public int firstKey;
        [Required]
        public int lastKey;

        [Required]
        public RenderTexture texture;

        public bool belowKeyboard = false;
    }
}
