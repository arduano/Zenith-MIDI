using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.DXHelper;

namespace MIDITrailRender.Logic
{
    public class NoteBufferParts : DeviceInitiable
    {
        public NoteBufferParts(ShapeBuffer<NoteInstance> body, ShapeBuffer<NoteInstance> cap)
        {
            Body = init.Add(body);
            Cap = init.Add(cap);
        }


        public NoteBufferParts(ShapeBuffer<NoteInstance> body)
        {
            Body = init.Add(body);
            Cap = null;
        }

        public bool HasCap => Cap != null;

        public ShapeBuffer<NoteInstance> Body { get; }
        public ShapeBuffer<NoteInstance> Cap { get; }
    }

    public class KeyModelDataEdge : DeviceInitiable
    {
        public KeyModelDataEdge(ModelBuffer<KeyVert>[] models)
        {
            Models = models;
            foreach (var m in Models) init.Add(m);
        }

        public ModelBuffer<KeyVert>[] Models { get; }

        public ModelBuffer<KeyVert> GetKey(int key) => Models[key % 12];
    }

    public class KeyModelDataType : DeviceInitiable
    {
        public KeyModelDataType(KeyModelDataEdge normal, KeyModelDataEdge left, KeyModelDataEdge right)
        {
            Normal = init.Add(normal);
            Left = init.Add(left);
            Right = init.Add(right);
        }

        protected override void DisposeInternal()
        {
            base.DisposeInternal();
        }

        public KeyModelDataEdge Normal { get; }
        public KeyModelDataEdge Left { get; }
        public KeyModelDataEdge Right { get; }
    }

    public class KeyModelData : DeviceInitiable
    {
        public KeyModelData(KeyModelDataType sameWidth, KeyModelDataType differentWidth)
        {
            SameWidth = init.Add(sameWidth);
            DifferentWidth = init.Add(differentWidth);
        }

        public KeyModelDataType SameWidth { get; }
        public KeyModelDataType DifferentWidth { get; }
    }

    public class NoteModelData : DeviceInitiable
    {
        public NoteModelData(NoteBufferParts flat, NoteBufferParts cube, NoteBufferParts rounded)
        {
            Flat = init.Add(flat);
            Cube = init.Add(cube);
            Rounded = init.Add(rounded);
        }

        public NoteBufferParts Flat { get; }
        public NoteBufferParts Cube { get; }
        public NoteBufferParts Rounded { get; }
    }

    public class FullModelData : DeviceInitiable
    {
        public FullModelData(NoteModelData notes, KeyModelData keys)
        {
            Notes = init.Add(notes);
            Keys = init.Add(keys);
        }

        public NoteModelData Notes { get; }
        public KeyModelData Keys { get; }
    }
}
