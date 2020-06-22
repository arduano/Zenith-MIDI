using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ZenithEngine.GLEngine
{
    public class ModelBuffer<T> : IDisposable
        where T : struct
    {
        int bufferId;
        int indexBufferId;
        InputAssemblyPart[] inputParts;
        int structByteSize;

        internal T[] data;
        internal int[] indices;

        bool initialized = false;

        public ModelBuffer(T[] data, int[] indices)
            : this(data, indices, BufferTools.GetAssemblyParts(typeof(T))) { }

        public ModelBuffer(T[] data, int[] indices, InputAssemblyPart[] inputParts)
        {
            structByteSize = Marshal.SizeOf(default(T));
            this.inputParts = inputParts;
            this.indices = indices;
            this.data = data;
        }

        public void Init()
        {
            if (initialized) return;

            indexBufferId = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
            BufferTools.BindIndices(indices, indices.Length, sizeof(int));

            bufferId = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, bufferId);
            BufferTools.BindData(data, data.Length, structByteSize);

            initialized = true;
        }

        public void DrawSingle()
        {
            Init();
            using (new GLEnabler().UseAttribArrays(inputParts.Length))
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, bufferId);
                BufferTools.BindBufferParts(inputParts, structByteSize);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
                GL.IndexPointer(IndexPointerType.Int, 1, 0);
                GL.DrawArrays(PrimitiveType.Triangles, 0, indices.Length);
            }
        }

        public void Dispose()
        {
            if (!initialized) return;
            GL.DeleteBuffer(bufferId);
            GL.DeleteBuffer(indexBufferId);
            initialized = false;
        }
    }
}
