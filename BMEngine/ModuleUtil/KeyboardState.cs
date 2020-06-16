using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.MIDI;

namespace ZenithEngine.ModuleUtil
{
    public class KeyboardParams
    {
        public bool SameWidthNotes { get; set; } = false;

        public double BlackKey2setOffset { get; set; } = 0.3;
        public double BlackKey3setOffset { get; set; } = 0.5;
        public double BlackKeyScale { get; set; } = 0.6;

        public double BlackNote2setOffset { get; set; } = 0;
        public double BlackNote3setOffset { get; set; } = 0;
        public double BlackNoteScale { get; set; } = 1;

        public Color4 BlackKeyColor { get; set; } = Color4.Black;
        public Color4 WhiteKeyColor { get; set; } = Color4.White;

        public double[] AdvancedBlackKeyOffsets { get; set; } = new double[] { 0, 0, 0, 0, 0 };
    }

    public class KeyboardState
    {
        public class Pos
        {
            public Pos(double left, double right)
            {
                Left = left;
                Right = right;
            }

            public double Left { get; private set; }
            public double Right { get; private set; }
        }
        public class Col
        {
            public Col(Color4 left, Color4 right)
            {
                Left = left;
                Right = right;
            }

            public Color4 Left { get; set; }
            public Color4 Right { get; set; }
        }

        public KeyboardState (int firstNote, int lastNote, KeyboardParams options)
        {
            for (int i = 0; i < BlackKey.Length; i++) BlackKey[i] = IsBlackKey(i);
            int b = 0;
            int w = 0;
            for (int i = 0; i < KeyNumber.Length; i++)
            {
                if (BlackKey[i]) KeyNumber[i] = b++;
                else KeyNumber[i] = w++;
            }

            double wdth;

            double[] leftArrayKeys = new double[257];
            double[] widthArrayKeys = new double[257];
            double[] leftArrayNotes = new double[257];
            double[] widthArrayNotes = new double[257];

            if (options.SameWidthNotes)
            {
                var samewidth = 1.0 / (lastNote - firstNote);

                for (int i = 0; i < 257; i++)
                {
                    var left = (i - firstNote) / (double)(lastNote - firstNote);
                    var right = left + samewidth;

                    int n = i % 12;
                    if (n == 0)
                        right += samewidth * 0.666;
                    else if (n == 2)
                    {
                        left -= samewidth / 3;
                        right += samewidth / 3;
                    }
                    else if (n == 4)
                        left -= samewidth / 3 * 2;
                    else if (n == 5)
                        right += samewidth * 0.75;
                    else if (n == 7)
                    {
                        left -= samewidth / 4;
                        right += samewidth / 2;
                    }
                    else if (n == 9)
                    {
                        left -= samewidth / 2;
                        right += samewidth / 4;
                    }
                    else if (n == 11)
                        left -= samewidth * 0.75;

                    leftArrayKeys[i] = left;
                    widthArrayKeys[i] = right - left;
                    leftArrayNotes[i] = (i - firstNote) / (double)(lastNote - firstNote);
                    widthArrayNotes[i] = samewidth;
                }

                BlackKeyWidth = samewidth;
                WhiteKeyWidth = samewidth;
                BlackNoteWidth = samewidth;
                WhiteNoteWidth = samewidth;
            }
            else
            {
                for (int i = 0; i < 257; i++)
                {
                    if (!BlackKey[i])
                    {
                        leftArrayKeys[i] = KeyNumber[i];
                        leftArrayNotes[i] = KeyNumber[i];
                        widthArrayKeys[i] = 1.0f;
                        widthArrayNotes[i] = 1.0f;
                    }
                    else
                    {
                        int _i = i + 1;
                        wdth = options.BlackKeyScale;
                        int bknum = KeyNumber[i] % 5;
                        double offset = wdth / 2;
                        if (bknum == 0) offset += wdth / 2 * options.BlackKey2setOffset;
                        if (bknum == 2) offset += wdth / 2 * options.BlackKey3setOffset;
                        if (bknum == 1) offset -= wdth / 2 * options.BlackKey2setOffset;
                        if (bknum == 4) offset -= wdth / 2 * options.BlackKey3setOffset;

                        offset -= options.AdvancedBlackKeyOffsets[KeyNumber[i] % 5] * wdth / 2;

                        leftArrayKeys[i] = KeyNumber[_i] - offset;
                        widthArrayKeys[i] = wdth;

                        offset -= wdth / 2 * (1 - options.BlackNoteScale);
                        if (bknum == 0) offset += wdth / 2 * options.BlackNote2setOffset;
                        if (bknum == 2) offset += wdth / 2 * options.BlackNote3setOffset;
                        if (bknum == 1) offset -= wdth / 2 * options.BlackNote2setOffset;
                        if (bknum == 4) offset -= wdth / 2 * options.BlackNote3setOffset;
                        wdth *= options.BlackNoteScale;

                        leftArrayNotes[i] = KeyNumber[_i] - offset;
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

                BlackKeyWidth = options.BlackKeyScale / width;
                WhiteKeyWidth = 1 / width;
                BlackNoteWidth = options.BlackKeyScale * options.BlackNoteScale / width;
                WhiteNoteWidth = 1 / width;
            }

            for (int i = 0; i < 257; i++)
            {
                Keys[i] = new Pos(leftArrayKeys[i], leftArrayKeys[i] + widthArrayKeys[i]);
                Notes[i] = new Pos(leftArrayNotes[i], leftArrayNotes[i] + widthArrayNotes[i]);

                if (BlackKey[i]) Colors[i] = new Col(options.BlackKeyColor, options.BlackKeyColor);
                else Colors[i] = new Col(options.WhiteKeyColor, options.WhiteKeyColor);
            }
        }

        public void BlendNote(int key, Color4 left, Color4 right)
        {
            Colors[key].Left = Colors[key].Left.BlendWith(left);
            Colors[key].Right = Colors[key].Right.BlendWith(right);
        }

        public void BlendNote(int key, NoteColor color)
        {
            BlendNote(key, color.Left, color.Right);
        }

        public void PressKey(int key)
        {
            Pressed[key] = true;
        }

        public static bool IsBlackKey(int n)
        {
            n = n % 12;
            if (n < 0) n += 12;
            return n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
        }

        public Pos[] Keys { get; } = new Pos[257];
        public Pos[] Notes { get; } = new Pos[257];
        public bool[] BlackKey { get; } = new bool[257];
        public int[] KeyNumber { get; } = new int[257];

        public double BlackKeyWidth { get; }
        public double WhiteKeyWidth { get; }
        public double BlackNoteWidth { get; }
        public double WhiteNoteWidth { get; }

        public Col[] Colors { get; } = new Col[257]; 
        public bool[] Pressed { get; } = new bool[257]; 
    }
}
