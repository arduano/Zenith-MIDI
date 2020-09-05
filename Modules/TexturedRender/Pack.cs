using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.DXHelper;

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

    public class KeyboardOverlay : DeviceInitiable
    {
        public int FirstKey { get; set; }
        public int LastKey { get; set; }
        public double Alpha { get; set; } = 1;
        public bool OverlayBelow { get; set; } = false;
        RenderTexture texture;
        public RenderTexture Texture { get => texture; set => init.Replace(ref texture, value); }
    }

    public class NoteTexture : DeviceInitiable
    {
        public double MaxSize { get; set; }
        public bool UseCaps { get; set; }
        public bool Stretch { get; set; }
        public bool SqueezeEndCaps { get; set; } = true;

        public double DarkenBlackNotes { get; set; } = 1;
        public double HighlightHitNotes { get; set; } = 0;
        public Color HighlightHitNotesColor { get; set; } = Color.FromArgb(255, 255, 255, 255);

        public RenderTexture NoteMiddleTex { get => noteMiddleTex; set => init.Replace(ref noteMiddleTex, value); }
        RenderTexture noteMiddleTex;
        public RenderTexture NoteTopTex { get => noteTopTex; set => init.Replace(ref noteTopTex, value); }
        RenderTexture noteTopTex;
        public double NoteTopOversize { get; set; }

        public RenderTexture NoteBottomTex { get => noteBottomTex; set => init.Replace(ref noteBottomTex, value); }
        RenderTexture noteBottomTex;
        public double NoteBottomOversize { get; set; }

        public KeyType keyType = KeyType.Both;
    }

    public class Pack : DeviceInitiable
    {
        public string Filepath { get; set; }
        public PackType Filetype { get; set; }

        public List<string> SwitchOrder { get; set; } = new List<string>();
        public Dictionary<string, string> SwitchValues { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string[]> SwitchChoices { get; set; } = new Dictionary<string, string[]>();

        public string Name { get; set; }
        public bool Error { get; set; } = false;
        public Bitmap Preview { get; set; } = null;

        public bool SameWidthNotes { get; set; } = false;
        public double KeyboardHeight { get; set; } = 0.15;
        public double BlackKeyHeight { get; set; } = 0.4;

        public string Description { get; set; } = "";

        public TextureShaderType NoteShader { get; set; } = TextureShaderType.Normal;
        public TextureShaderType WhiteKeyShader { get; set; } = TextureShaderType.Normal;
        public TextureShaderType BlackKeyShader { get; set; } = TextureShaderType.Normal;
        public bool BlackKeyDefaultWhite { get; set; } = false;

        public double BlackKey2setOffset { get; set; } = 0.3;
        public double BlackKey3setOffset { get; set; } = 0.5;
        public double BlackKeyScale { get; set; } = 0.6;

        public double BlackNote2setOffset { get; set; } = 0;
        public double BlackNote3setOffset { get; set; } = 0;
        public double BlackNoteScale { get; set; } = 1;

        public bool LinearScaling { get; set; } = true;

        public double[] AdvancedBlackKeyOffsets { get; set; } = new double[] { 0, 0, 0, 0, 0 };
        public double[] AdvancedBlackKeySizes { get; set; } = new double[] { 1, 1, 1, 1, 1 };

        public RenderTexture WhiteKeyTex { get => whiteKeyTex; set => init.Replace(ref whiteKeyTex, value); }
        RenderTexture whiteKeyTex;
        public double WhiteKeyOversize { get; set; } = 0;

        public RenderTexture BlackKeyTex { get => blackKeyTex; set => init.Replace(ref blackKeyTex, value); }
        RenderTexture blackKeyTex;
        public double BlackKeyOversize { get; set; } = 0;

        public RenderTexture WhiteKeyPressedTex { get => whiteKeyPressedTex; set => init.Replace(ref whiteKeyPressedTex, value); }
        RenderTexture whiteKeyPressedTex;
        public double WhiteKeyPressedOversize { get; set; } = 0;

        public RenderTexture BlackKeyPressedTex { get => blackKeyPressedTex; set => init.Replace(ref blackKeyPressedTex, value); }
        RenderTexture blackKeyPressedTex;
        public double BlackKeyPressedOversize { get; set; } = 0;

        public bool UseBar = false;
        public RenderTexture BarTex { get => barTex; set => init.Replace(ref barTex, value); }
        RenderTexture barTex;
        public double BarHeight { get; set; } = 0.05;

        public RenderTexture WhiteKeyLeftTex { get => whiteKeyLeftTex; set => init.Replace(ref whiteKeyLeftTex, value); }
        RenderTexture whiteKeyLeftTex;
        public RenderTexture WhiteKeyPressedLeftTex { get => whiteKeyPressedLeftTex; set => init.Replace(ref whiteKeyPressedLeftTex, value); }
        RenderTexture whiteKeyPressedLeftTex;

        public RenderTexture WhiteKeyRightTex { get => whiteKeyRightTex; set => init.Replace(ref whiteKeyRightTex, value); }
        RenderTexture whiteKeyRightTex;
        public RenderTexture WhiteKeyPressedRightTex { get => whiteKeyPressedRightTex; set => init.Replace(ref whiteKeyPressedRightTex, value); }
        RenderTexture whiteKeyPressedRightTex;

        public bool WhiteKeysFullOctave { get; set; } = false;
        public bool BlackKeysFullOctave { get; set; } = false;

        public NoteTexture[] NoteTextures { get; set; }
        public KeyboardOverlay[] OverlayTextures { get; set; }
    }
}
