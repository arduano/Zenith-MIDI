using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexturedRender
{
    public enum TextureShaderType
    {
        Normal,
        Inverted,
        Hybrid
    }

    public enum KeyType
    {
        White,
        Black,
        Both
    }

    public enum PackType
    {
        Folder, Zip, Zrp, Rar, SevenZip, Tar
    }

    public class KeyboardOverlay
    {
        public int firstKey;
        public int lastKey;
        public double alpha = 1;
        public bool overlayBelow = false;
        public Bitmap tex;
        public int texID;
        public double texAspect;
    }

    public class NoteTexture
    {
        public double maxSize;
        public bool useCaps;
        public bool stretch;
        public bool squeezeEndCaps = true;

        public double darkenBlackNotes = 1;
        public double highlightHitNotes = 0;
        public Color highlightHitNotesColor = Color.FromArgb(255, 255, 255, 255);

        public double noteMiddleAspect;
        public Bitmap noteMiddleTex;
        public int noteMiddleTexID;
        public double noteTopOversize;
        public double noteTopAspect;
        public Bitmap noteTopTex;
        public int noteTopTexID;
        public double noteBottomOversize;
        public double noteBottomAspect;
        public Bitmap noteBottomTex;
        public int noteBottomTexID;

        public KeyType keyType = KeyType.Both;
    }

    public class Pack
    {
        public string filepath;
        public PackType filetype;

        public List<string> switchOrder = new List<string>();
        public Dictionary<string, string> switchValues = new Dictionary<string, string>();
        public Dictionary<string, string[]> switchChoices = new Dictionary<string, string[]>();

        public string name;
        public bool error = false;
        public Bitmap preview = null;

        public bool sameWidthNotes = false;
        public double keyboardHeight = 0.15;
        public double blackKeyHeight = 0.4;

        public string description = "";

        public TextureShaderType noteShader = TextureShaderType.Normal;
        public TextureShaderType whiteKeyShader = TextureShaderType.Normal;
        public TextureShaderType blackKeyShader = TextureShaderType.Normal;
        public bool blackKeyDefaultWhite = false;

        public double blackKey2setOffset = 0.3;
        public double blackKey3setOffset = 0.5;
        public double blackKeyScale = 0.6;

        public double blackNote2setOffset = 0;
        public double blackNote3setOffset = 0;
        public double blackNoteScale = 1;

        public bool linearScaling = true;

        public double[] advancedBlackKeyOffsets = new double[] { 0, 0, 0, 0, 0 };

        public Bitmap whiteKeyTex;
        public int whiteKeyTexID;
        public double whiteKeyOversize = 0;

        public Bitmap blackKeyTex;
        public int blackKeyTexID;
        public double blackKeyOversize = 0;

        public Bitmap whiteKeyPressedTex;
        public int whiteKeyPressedTexID;
        public double whiteKeyPressedOversize = 0;

        public Bitmap blackKeyPressedTex;
        public int blackKeyPressedTexID;
        public double blackKeyPressedOversize = 0;

        public bool useBar = false;
        public Bitmap barTex;
        public int barTexID;
        public double barHeight = 0.05;

        public Bitmap whiteKeyLeftTex = null;
        public int whiteKeyLeftTexID;
        public Bitmap whiteKeyPressedLeftTex = null;
        public int whiteKeyPressedLeftTexID;

        public Bitmap whiteKeyRightTex = null;
        public int whiteKeyRightTexID;
        public Bitmap whiteKeyPressedRightTex = null;
        public int whiteKeyPressedRightTexID;

        public float interpolateUnendedNotes = 0;

        public bool whiteKeysFullOctave = false;
        public bool blackKeysFullOctave = false;

        public NoteTexture[] NoteTextures;
        public KeyboardOverlay[] OverlayTextures;

        public bool disposed = false;
    }
}
