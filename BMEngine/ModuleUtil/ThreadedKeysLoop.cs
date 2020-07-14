using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZenithEngine.DXHelper;

namespace ZenithEngine.ModuleUtil
{

    public class ThreadedKeysLoop<T> : BufferFlusher<T>
        where T : struct
    {
        public DeviceContext Context { get; private set; } = null;

        BlockingCollection<T[]> arrays;

        int pos = 0;

        int threads;

        public ThreadedKeysLoop(int length, PrimitiveTopology topology, ShapePresets indicesType) : this(length, topology, IndicesFromPreset(indicesType)) { }
        public ThreadedKeysLoop(int length) : this(length, PrimitiveTopology.TriangleList, ShapePresets.Quads) { }
        public ThreadedKeysLoop(int length, PrimitiveTopology topology) : this(length, topology, null) { }
        public ThreadedKeysLoop(int length, PrimitiveTopology topology, int[] indices) : base(length, topology, indices)
        {
            this.threads = Environment.ProcessorCount;
            if (threads > 8) threads = 8;
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

                void renderKeyWrapper(int key)
                {
                    int pos = 0;
                    var arr = arrays.Take();
                    renderKey(key, vert =>
                    {
                        arr[pos++] = vert;
                        if (pos == arr.Length)
                        {
                            lock (l)
                                FlushArray(context, arr, pos);
                            pos = 0;
                        }
                    });
                    if (pos != 0)
                    {
                        lock (l)
                            FlushArray(context, arr, pos);
                    }
                    arrays.Add(arr);
                }

                Parallel.ForEach(keys, new ParallelOptions() { MaxDegreeOfParallelism = threads }, renderKeyWrapper);
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
    }
}
