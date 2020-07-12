using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenithEngine.DXHelper
{
    public interface ITextureResource
    {
        public ShaderResourceView TextureResource { get; }
        public Texture2D Texture { get; }

        public int Width { get; }
        public int Height { get; }
    }
}
