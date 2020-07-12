using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.DXHelper;
using Buffer = SharpDX.Direct3D11.Buffer;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using ZenithEngine.MIDI;

namespace ZenithEngine.ModuleUtil
{

    public class ThreadedKeysLoop<T> : BufferFlusher<T>
        where T : struct
    {
        public DeviceContext Context { get; private set; } = null;

        BlockingCollection<T[]> arrays;

        int pos = 0;

        int threads;

        public ThreadedKeysLoop(int length, int threads, PrimitiveTopology topology, ShapePresets indicesType) : this(length, threads, topology, IndicesFromPreset(indicesType)) { }
        public ThreadedKeysLoop(int length, int threads) : this(length, threads, PrimitiveTopology.TriangleList, ShapePresets.Quads) { }
        public ThreadedKeysLoop(int length, int threads, PrimitiveTopology topology) : this(length, threads, topology, null) { }
        public ThreadedKeysLoop(int length, int threads, PrimitiveTopology topology, int[] indices) : base(length, topology, indices)
        {
            this.threads = threads;
        }

        protected override void InitInternal()
        {
            base.InitInternal();
            arrays = new BlockingCollection<T[]>();
            for (int i = 0; i < threads; i++)
            {
                arrays.Add(new T[vertsCount]);
            }
        }

        protected override void DisposeInternal()
        {
            base.DisposeInternal();
            arrays = null;
        }

        public void UseContext(DeviceContext context) => Context = context;

        public void Render(DeviceContext context, int firstKey, int lastKey, bool blackKeysOnTop, Action<int, Action<T>> renderKey)
        {
            void RenderKeyArray(IEnumerable<int> keys)
            {
                object l = new object();
                Parallel.ForEach(keys, new ParallelOptions() { MaxDegreeOfParallelism = threads }, key =>
                   {
                       int pos = 0;
                       var arr = arrays.Take();
                       renderKey(key, vert =>
                       {
                           arr[pos++] = vert;
                           if (pos == arr.Length)
                           {
                               lock (l)
                               {
                                   FlushArray(context, arr, pos);
                               }
                               pos = 0;
                           }
                           FlushArray(context, arr, pos);
                       });
                       arrays.Add(arr);
                   });
            }

            IEnumerable<int> iterateAllKeys()
            {
                for (int i = firstKey; i < lastKey; i++)
                    yield return i;
            }

            IEnumerable<int> iterateSelectKeys(bool black)
            {
                for (int i = firstKey; i < lastKey; i++)
                    if (KeyboardState.IsBlackKey(i) == black)
                        yield return i;
            }

            if (blackKeysOnTop)
            {
                RenderKeyArray(iterateSelectKeys(false));
                RenderKeyArray(iterateSelectKeys(true));
            }
            else
            {
                RenderKeyArray(iterateAllKeys());
            }
        }

        public void Push(T vert)
        {
            verts[pos++] = vert;
            if (pos >= verts.Length)
            {
                Flush();
            }
        }

        public unsafe void Flush()
        {
            if (Context == null) throw new Exception("Device context was not initialised on this shape buffer");

            FlushArray(Context, verts, pos);

            pos = 0;
        }
    }
}
