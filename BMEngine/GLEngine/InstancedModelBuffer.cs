using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace ZenithEngine.GLEngine
{
    public class InstancedModelBuffer<TM, TI> : IDisposable
        where TM : struct
        where TI : struct
    {
        int instanceBufferId;
        int modelBufferId;
        int modelIndexBufferId;
        InputAssemblyPart[] modelInputParts;
        InputAssemblyPart[] instanceInputParts;
        int instanceStructByteSize;
        int modelStructByteSize;

        TM[] modelData;
        int[] modelIndices;

        TI[] instances;

        int pos = 0;

        bool initialized = false;

        public InstancedModelBuffer(int instanceBufferLength, ModelBuffer<TM> model)
            : this(instanceBufferLength, model.data, model.indices, BufferTools.GetAssemblyParts(typeof(TM)), BufferTools.GetAssemblyParts(typeof(TI))) { }


        public InstancedModelBuffer(int instanceBufferLength, TM[] modelData, int[] modelIndices)
            : this(instanceBufferLength, modelData, modelIndices, BufferTools.GetAssemblyParts(typeof(TM)), BufferTools.GetAssemblyParts(typeof(TI))) { }

        public InstancedModelBuffer(int instanceBufferLength, TM[] modelData, int[] modelIndices, InputAssemblyPart[] modelInputParts, InputAssemblyPart[] instanceInputParts)
        {
            modelStructByteSize = Marshal.SizeOf(default(TM));
            instanceStructByteSize = Marshal.SizeOf(default(TI));
            this.modelInputParts = modelInputParts;
            this.instanceInputParts = instanceInputParts;

            instances = new TI[instanceBufferLength];

            this.modelData = modelData;
            this.modelIndices = modelIndices;
        }

        public void Init()
        {
            if (initialized) return;

            modelIndexBufferId = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, modelIndexBufferId);
            BufferTools.BindIndices(modelIndices, modelIndices.Length, sizeof(int));

            modelBufferId = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, modelBufferId);
            BufferTools.BindData(modelData, modelData.Length, modelStructByteSize);

            instanceBufferId = GL.GenBuffer();

            initialized = true;
        }

        public void PushInstance(TI instance)
        {
            instances[pos++] = instance;
            if (pos >= instances.Length)
            {
                Flush();
            }
        }

        public void Flush()
        {
            if (pos == 0) return;
            Init();
            using (
                new GLEnabler()
                .UseAttribArrays(modelInputParts.Length + instanceInputParts.Length)
            )
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, modelBufferId);
                BufferTools.BindBufferParts(modelInputParts, modelStructByteSize);

                GL.BindBuffer(BufferTarget.ArrayBuffer, instanceBufferId);
                BufferTools.BindData(instances, pos, instanceStructByteSize);
                BufferTools.BindBufferParts(instanceInputParts, instanceStructByteSize, modelInputParts.Length);

                using (new GLEnabler().UseInstancedBuffers(modelInputParts.Length, instanceInputParts.Length))
                {
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, modelIndexBufferId);
                    GL.IndexPointer(IndexPointerType.Int, 1, 0);
                    GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, modelIndices.Length, pos);
                }
            }
            pos = 0;
        }

        public void Dispose()
        {
            if (!initialized) return;
            GL.DeleteBuffer(modelBufferId);
            GL.DeleteBuffer(modelIndexBufferId);
            GL.DeleteBuffer(instanceBufferId);
            initialized = false;
        }
    }
}
