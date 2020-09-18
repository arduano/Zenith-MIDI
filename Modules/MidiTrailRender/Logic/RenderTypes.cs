using MIDITrailRender.Models;
using SharpDX;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.DXHelper;

namespace MIDITrailRender.Logic
{
    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 16)]
    public struct FullColorData
    {
        public Color4 Diffuse;
        public Color4 Emit;
        public Color4 Specular;
        public Color4 Water;
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 16)]
    public struct FullColorConfig
    {
        public float Diffuse;
        public float Emit;
        public float Specular;
        public float Water;

        public static FullColorConfig FromColorModel(FullColorModel model)
        {
            return new FullColorConfig()
            {
                Diffuse = (float)model.Diffuse,
                Emit = (float)model.Emit,
                Specular = (float)model.Specular,
                Water = (float)model.Water,
            };
        }
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 16)]
    public struct FullColorAdjust
    {
        public FullColorConfig Unpressed;
        public FullColorConfig Pressed;
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 16)]
    public struct KeyShaderConstant
    {
        public Matrix Model;
        public Matrix View;
        public Vector3 ViewPos;
        public float Time;
        public FullColorData LeftColor;
        public FullColorData RightColor;
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 16)]
    public struct NoteShaderConstant
    {
        public Matrix Model;
        public Matrix View;
        public Vector3 ViewPos;
        public float Time;
        public FullColorAdjust ColAdjust;
        public float WaterOffset;
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct KeyVert
    {
        [AssemblyElement("POS", Format.R32G32B32_Float)]
        public Vector3 Pos;

        [AssemblyElement("NORM", Format.R32G32B32_Float)]
        public Vector3 Normal;

        [AssemblyElement("SIDE", Format.R32_Float)]
        public float Side;

        public KeyVert(Vector3 pos, Vector3 normal, float side)
        {
            Pos = pos;
            Normal = normal;
            Side = side;
        }
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NoteVert
    {
        [AssemblyElement("POS", Format.R32G32B32_Float)]
        public Vector3 Pos;

        [AssemblyElement("NORM", Format.R32G32B32_Float)]
        public Vector3 Normal;

        [AssemblyElement("SIDE", Format.R32_Float)]
        public float Side;

        [AssemblyElement("CORNER", Format.R32G32B32_Float)]
        public Vector3 Corner;

        public NoteVert(Vector3 pos, Vector3 normal, float side, Vector3 corner)
        {
            Pos = pos;
            Normal = normal;
            Side = side;
            Corner = corner;
        }
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct NoteInstance
    {
        [AssemblyElement("LEFT", Format.R32_Float)]
        public float Left;

        [AssemblyElement("RIGHT", Format.R32_Float)]
        public float Right;

        [AssemblyElement("START", Format.R32_Float)]
        public float Start;

        [AssemblyElement("END", Format.R32_Float)]
        public float End;

        [AssemblyElement("COLORLEFT", Format.R32G32B32A32_Float)]
        public Color4 ColorLeft;

        [AssemblyElement("COLORRIGHT", Format.R32G32B32A32_Float)]
        public Color4 ColorRight;

        [AssemblyElement("SCALE", Format.R32_Float)]
        public float Scale;

        [AssemblyElement("HEIGHT", Format.R32_Float)]
        public float Height;

        [AssemblyElement("PRESS", Format.R16_Float)]
        public Half Press;

        public NoteInstance(float left, float right, float start, float end, Color4 colorLeft, Color4 colorRight, float scale, float extraScale, bool pressed)
        {
            Left = left;
            Right = right;
            Start = start;
            End = end;
            ColorLeft = colorLeft;
            ColorRight = colorRight;
            Scale = scale;
            Height = extraScale;
            Press = pressed ? 1 : 0;
        }
    }
}
