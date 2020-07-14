using SharpDX;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper.Presets
{
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct Vert2D
    {
        [AssemblyElement("POSITION", Format.R32G32_Float)]
        public Vector2 Pos;

        [AssemblyElement("COLOR", Format.R32G32B32A32_Float)]
        public Color4 Col;

        public Vert2D(Vector2 pos, Color4 col)
        {
            Pos = pos;
            Col = col;
        }

        public Vert2D(float x, float y, Color4 col)
        {
            Pos = new Vector2(x, y);
            Col = col;
        }
    }

    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct VertTex2D
    {
        [AssemblyElement("POSITION", Format.R32G32_Float)]
        public Vector2 Pos;

        [AssemblyElement("UV", Format.R32G32_Float)]
        public Vector2 UV;

        [AssemblyElement("COLOR", Format.R32G32B32A32_Float)]
        public Color4 Col;

        public VertTex2D(Vector2 pos, Vector2 uv, Color4 col)
        {
            Pos = pos;
            UV = uv;
            Col = col;
        }
    }
}
