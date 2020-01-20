using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using BMEngine;
using OpenTK.Graphics;

namespace ScriptedEngine
{
    public class Texture
    {
        public string path;
        public Bitmap bitmap;
        public int texId = -1;
        public bool linear = false;
        public bool looped = true;

        public int width;
        public int height;
        public double aspectRatio;
    }

    public enum TextureShaders
    {
        Normal,
        Inverted,
        Hybrid
    }

    public enum BlendFunc
    {
        Mix,
        Add
    }

    public class RenderOptions
    {
        public int firstKey;
        public int lastKey;
        public int renderWidth;
        public int renderHeight;
        public double renderAspectRatio;
        public int renderFPS;
        public int renderSSAA;
        public double midiTime;
        public double noteScreenTime;
    }

    public class KeyLayout
    {
        public class pos
        {
            public double left;
            public double right;
        }

        public KeyLayout()
        {
            keys = new pos[257];
            notes = new pos[257];

            for (int i = 0; i < blackKey.Length; i++) blackKey[i] = Util.IsBlackKey(i);
            int b = 0;
            int w = 0;
            for (int i = 0; i < keyNumber.Length; i++)
            {
                if (blackKey[i]) keyNumber[i] = b++;
                else keyNumber[i] = w++;
            }
        }

        public pos[] keys;
        public pos[] notes;
        public bool[] blackKey = new bool[257];
        public int[] keyNumber = new int[257];

        public double blackKeyWidth;
        public double whiteKeyWidth;
        public double blackNoteWidth;
        public double whiteNoteWidth;
    }

    public class KeyboardOptions
    {
        public bool sameWidthNotes = false;

        public double blackKey2setOffset = 0.3;
        public double blackKey3setOffset = 0.5;
        public double blackKeyScale = 0.6;

        public double blackNote2setOffset = 0;
        public double blackNote3setOffset = 0;
        public double blackNoteScale = 1;

        public double[] advancedBlackKeyOffsets = new double[] { 0, 0, 0, 0, 0 };
    }

    public static class IO
    {
        public static Func<string, bool, bool, Texture> loadTexture;
        public static Texture LoadTexture(string path)
        {
            if (!IsInFunction("Load") && !IsInFunction(".ctor")) throw new Exception("Can't call LoadTexture outside the load function");
            return loadTexture(path, true, false);
        }

        public static Texture LoadTexture(string path, bool linear)
        {
            if (!IsInFunction("Load")) throw new Exception("Can't call LoadTexture outside the load function");
            return loadTexture(path, true, linear);
        }

        public static Texture LoadTexture(string path, bool uvLoop, bool linear)
        {
            if (!IsInFunction("Load")) throw new Exception("Can't call LoadTexture outside the load function");
            return loadTexture(path, uvLoop, linear);
        }

        public static Action<double, double, double, double, Color4, Color4, Color4, Color4, Texture, double, double, double, double> renderQuad;
        public static Action<TextureShaders> selectTexShader;
        public static Action<BlendFunc> setBlendFunc;
        public static Action forceFlush;
        public static Action<Vector2d, Vector2d, Vector2d, Vector2d, Color4, Color4, Color4, Color4, Texture, Vector2d, Vector2d, Vector2d, Vector2d> renderShape;

        public static void RenderQuad(double left, double top, double right, double bottom, Color4 col) =>
            renderQuad(left, top, right, bottom, col, col, col, col, null, 0, 0, 0, 0);

        public static void RenderQuad(double left, double top, double right, double bottom, Color4 col, Texture tex) =>
            renderQuad(left, top, right, bottom, col, col, col, col, tex, 0, 0, 1, 1);

        public static void RenderQuad(double left, double top, double right, double bottom, Color4 col, Texture tex, double uvLeft, double uvTop, double uvRight, double uvBottom) =>
            renderQuad(left, top, right, bottom, col, col, col, col, tex, uvLeft, uvTop, uvRight, uvBottom);

        public static void RenderQuad(double left, double top, double right, double bottom, Color4 topLeft, Color4 topRight, Color4 bottomRight, Color4 bottomLeft) =>
            renderQuad(left, top, right, bottom, topLeft, topRight, bottomRight, bottomLeft, null, 0, 0, 0, 0);

        public static void RenderQuad(double left, double top, double right, double bottom, Color4 topLeft, Color4 topRight, Color4 bottomRight, Color4 bottomLeft, Texture tex) =>
            renderQuad(left, top, right, bottom, topLeft, topRight, bottomRight, bottomLeft, tex, 0, 0, 1, 1);

        public static void RenderQuad(double left, double top, double right, double bottom, Color4 topLeft, Color4 topRight, Color4 bottomRight, Color4 bottomLeft, Texture tex, double uvLeft, double uvTop, double uvRight, double uvBottom) =>
            renderQuad(left, top, right, bottom, topLeft, topRight, bottomRight, bottomLeft, tex, uvLeft, uvTop, uvRight, uvBottom);

        public static void RenderShape(Vector2d v1, Vector2d v2, Vector2d v3, Vector2d v4, Color4 col) =>
            renderShape(v1, v2, v3, v4, col, col, col, col, null, Vector2d.Zero, Vector2d.Zero, Vector2d.Zero, Vector2d.Zero);

        public static void RenderShape(Vector2d v1, Vector2d v2, Vector2d v3, Vector2d v4, Color4 col, Texture tex) =>
            renderShape(v1, v2, v3, v4, col, col, col, col, tex, new Vector2d(0, 0), new Vector2d(1, 0), new Vector2d(1, 1), new Vector2d(0, 1));

        public static void RenderShape(Vector2d v1, Vector2d v2, Vector2d v3, Vector2d v4, Color4 col, Texture tex, Vector2d uv1, Vector2d uv2, Vector2d uv3, Vector2d uv4) =>
            renderShape(v1, v2, v3, v4, col, col, col, col, tex, uv1, uv2, uv3, uv4);

        public static void RenderShape(Vector2d v1, Vector2d v2, Vector2d v3, Vector2d v4, Color4 topLeft, Color4 topRight, Color4 bottomRight, Color4 bottomLeft) =>
            renderShape(v1, v2, v3, v4, topLeft, topRight, bottomRight, bottomLeft, null, new Vector2d(0, 0), new Vector2d(1, 0), new Vector2d(1, 1), new Vector2d(0, 1));

        public static void RenderShape(Vector2d v1, Vector2d v2, Vector2d v3, Vector2d v4, Color4 topLeft, Color4 topRight, Color4 bottomRight, Color4 bottomLeft, Texture tex) =>
            renderShape(v1, v2, v3, v4, topLeft, topRight, bottomRight, bottomLeft, tex, new Vector2d(0, 0), new Vector2d(1, 0), new Vector2d(1, 1), new Vector2d(0, 1));

        public static void RenderShape(Vector2d v1, Vector2d v2, Vector2d v3, Vector2d v4, Color4 topLeft, Color4 topRight, Color4 bottomRight, Color4 bottomLeft, Texture tex, Vector2d uv1, Vector2d uv2, Vector2d uv3, Vector2d uv4) =>
            renderShape(v1, v2, v3, v4, topLeft, topRight, bottomRight, bottomLeft, tex, uv1, uv2, uv3, uv4);

        public static void SelectTextureShader(TextureShaders shader) => selectTexShader(shader);

        public static void SetBlendFunc(BlendFunc function) => setBlendFunc(function);

        public static void ForceFlushBuffer() => forceFlush();

        static string[] functions = new string[] { "Load", "Render", "RenderInit", "RenderDispose", ".ctor" };

        static bool IsInFunction(string name)
        {
            var funcs = functions.Where(f => f != name).ToArray();
            StackTrace st = new StackTrace(true);
            bool has = false;
            for (int i = 0; i < st.FrameCount; i++)
            {
                var m = st.GetFrame(i).GetMethod();
                if (m.Name == name &&
                    m.DeclaringType.Name == "Script")
                    has = true;
                if (funcs.Contains(m.Name) &&
                    m.DeclaringType.Name == "Script")
                    has = false;
            }
            return has;
        }

        static public void callLoadFunction(dynamic script)
        {
            script.Load();
        }
    }

    public static class Util
    {
        public static KeyLayout GetKeyboardLayout(int firstNote, int lastNote, KeyboardOptions options)
        {
            double wdth;

            double[] leftArrayKeys = new double[257];
            double[] widthArrayKeys = new double[257];
            double[] leftArrayNotes = new double[257];
            double[] widthArrayNotes = new double[257];

            var layout = new KeyLayout();

            if (options.sameWidthNotes)
            {
                var samewidth = 1.0f / (lastNote - firstNote);

                for (int i = 0; i < 257; i++)
                {
                    leftArrayKeys[i] = (i - firstNote) / (double)(lastNote - firstNote);
                    widthArrayKeys[i] = samewidth;
                    leftArrayNotes[i] = (i - firstNote) / (double)(lastNote - firstNote);
                    widthArrayNotes[i] = samewidth;
                }

                layout.blackKeyWidth = samewidth;
                layout.whiteKeyWidth = samewidth;
                layout.blackNoteWidth = samewidth;
                layout.whiteNoteWidth = samewidth;
            }
            else
            {
                for (int i = 0; i < 257; i++)
                {
                    if (!layout.blackKey[i])
                    {
                        leftArrayKeys[i] = layout.keyNumber[i];
                        leftArrayNotes[i] = layout.keyNumber[i];
                        widthArrayKeys[i] = 1.0f;
                        widthArrayNotes[i] = 1.0f;
                    }
                    else
                    {
                        int _i = i + 1;
                        wdth = options.blackKeyScale;
                        int bknum = layout.keyNumber[i] % 5;
                        double offset = wdth / 2;
                        if (bknum == 0) offset += wdth / 2 * options.blackKey2setOffset;
                        if (bknum == 2) offset += wdth / 2 * options.blackKey3setOffset;
                        if (bknum == 1) offset -= wdth / 2 * options.blackKey2setOffset;
                        if (bknum == 4) offset -= wdth / 2 * options.blackKey3setOffset;

                        offset -= options.advancedBlackKeyOffsets[layout.keyNumber[i] % 5] * wdth / 2;

                        leftArrayKeys[i] = layout.keyNumber[_i] - offset;
                        widthArrayKeys[i] = wdth;

                        offset -= wdth / 2 * (1 - options.blackNoteScale);
                        if (bknum == 0) offset += wdth / 2 * options.blackNote2setOffset;
                        if (bknum == 2) offset += wdth / 2 * options.blackNote3setOffset;
                        if (bknum == 1) offset -= wdth / 2 * options.blackNote2setOffset;
                        if (bknum == 4) offset -= wdth / 2 * options.blackNote3setOffset;
                        wdth *= options.blackNoteScale;

                        leftArrayNotes[i] = layout.keyNumber[_i] - offset;
                        widthArrayNotes[i] = wdth;
                    }
                }
                double knmfn = leftArrayKeys[firstNote];
                double knmln = leftArrayKeys[lastNote - 1] + widthArrayKeys[lastNote - 1];
                double width = knmln - knmfn;

                for (int i = 0; i < 257; i++)
                {
                    leftArrayKeys[i] = (leftArrayKeys[i] - knmfn) / width;
                    leftArrayNotes[i] = (leftArrayNotes[i] - knmfn) / width;
                    widthArrayKeys[i] /= width;
                    widthArrayNotes[i] /= width;
                }

                layout.blackKeyWidth = options.blackKeyScale / width;
                layout.whiteKeyWidth = 1 / width;
                layout.blackNoteWidth = options.blackKeyScale * options.blackNoteScale / width;
                layout.whiteNoteWidth = 1 / width;
            }

            for (int i = 0; i < 257; i++)
            {
                layout.keys[i] = new KeyLayout.pos() { left = leftArrayKeys[i], right = leftArrayKeys[i] + widthArrayKeys[i] };
                layout.notes[i] = new KeyLayout.pos() { left = leftArrayNotes[i], right = leftArrayNotes[i] + widthArrayNotes[i] };
            }

            return layout;
        }

        public static bool IsBlackKey(int n)
        {
            n = n % 12;
            if (n < 0) n += 12;
            return n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
        }

        public static IEnumerable<Note> BlackNotesAbove(IEnumerable<Note> notes)
        {
            foreach (var n in notes)
            {
                if (!IsBlackKey(n.key)) yield return n;
            }

            foreach (var n in notes)
            {
                if (IsBlackKey(n.key)) yield return n;
            }
        }

        public static Color4 BlendColors(Color4 col1, Color4 col2)
        {
            float blendfac = col2.A;
            float revblendfac = 1 - blendfac;
            return new Color4(
                col2.R * blendfac + col1.R * revblendfac,
                col2.G * blendfac + col1.G * revblendfac,
                col2.B * blendfac + col1.B * revblendfac,
                col1.A + (1 - col1.A) * blendfac);
        }
    }
}
