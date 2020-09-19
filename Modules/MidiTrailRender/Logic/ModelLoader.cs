using ObjLoader.Loader.Data.Elements;
using ObjLoader.Loader.Loaders;
using OpenTK.Graphics.OpenGL;
using SharpCompress.Compressors.Xz;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.DXHelper;
using ZenithEngine.ModuleUtil;

namespace MIDITrailRender.Logic
{
    public class ModelLoader
    {
        #region Helper intermediate types
        struct BasicVert
        {
            public BasicVert(Vector3 pos, Vector3 normal)
            {
                Pos = pos;
                Normal = normal;
            }

            public Vector3 Pos;
            public Vector3 Normal;
        }

        struct BoundingBox
        {
            public BoundingBox(IEnumerable<BasicVert> data)
            {
                X = new BoundingDimention(data.Select(d => d.Pos.X));
                Y = new BoundingDimention(data.Select(d => d.Pos.Y));
                Z = new BoundingDimention(data.Select(d => d.Pos.Z));
            }

            public BoundingDimention X { get; }
            public BoundingDimention Y { get; }
            public BoundingDimention Z { get; }
        }

        struct BoundingDimention
        {
            public BoundingDimention(IEnumerable<float> data)
            {
                Min = data.Min();
                Max = data.Max();
                Range = Max - Min;
                Middle = (Max + Min) / 2;
            }

            public float Min { get; }
            public float Max { get; }
            public float Range { get; }
            public float Middle { get; }
        }

        enum NoteFaceType
        {
            Cap,
            Body
        }

        struct LabelledNoteFace
        {
            public LabelledNoteFace(Face face, NoteFaceType type)
            {
                Face = face;
                Type = type;
            }

            public Face Face { get; }
            public NoteFaceType Type { get; }
        }
        #endregion

        static LoadResult GetObjModel()
        {
            var factory = new ObjLoaderFactory();
            var objLoader = factory.Create();
            var embed = Util.OpenEmbedStream("MIDITrailRender.models.obj.xz");
            var extract = new XZStream(embed);
            return objLoader.Load(extract);
        }

        static IEnumerable<LabelledNoteFace> CategorizeVertices(IEnumerable<Face> faces, LoadResult obj)
        {
            foreach (var f in faces)
            {
                bool isBody = true;
                for (int i = 0; i < 3; i++)
                {
                    var v = f[i];
                    var norm = obj.Normals[v.NormalIndex - 1];
                    var dot = Vector3.Dot(new Vector3(norm.X, norm.Y, norm.Z), new Vector3(0, 0, 1));
                    if (Math.Abs(dot) > 0.1)
                    {
                        isBody = false;
                        break;
                    }
                }
                var type = isBody ? NoteFaceType.Body : NoteFaceType.Cap;
                yield return new LabelledNoteFace(f, type);
            }
        }

        static IEnumerable<BasicVert> ParseFaceVertices(Face face, LoadResult obj)
        {
            for (int i = 0; i < 3; i++)
            {
                var v = face[i];
                var vert = obj.Vertices[v.VertexIndex - 1];
                var norm = obj.Normals[v.NormalIndex - 1];
                var vertItem = new BasicVert(
                    new Vector3(vert.X, vert.Y, vert.Z),
                    new Vector3(norm.X, norm.Y, norm.Z)
                );
                yield return vertItem;
            }
        }

        static NoteBufferParts ParseNoteFaces(IEnumerable<Face> faces, LoadResult obj)
        {
            var labelledFaces = CategorizeVertices(faces, obj);

            var allVerts = new List<BasicVert>();
            var bodyVerts = new List<BasicVert>();
            var capVerts = new List<BasicVert>();

            foreach (var face in labelledFaces)
            {
                var verts = ParseFaceVertices(face.Face, obj);

                foreach (var v in verts)
                {
                    allVerts.Add(v);
                    if (face.Type == NoteFaceType.Body)
                        bodyVerts.Add(v);
                    if (face.Type == NoteFaceType.Cap)
                        capVerts.Add(v);
                }
            }

            var bd = new BoundingDimention(allVerts.Select(v => v.Pos.X));

            NoteVert convertVertex(BasicVert v)
            {
                var corner = new Vector3(
                    v.Pos.X > 0 ? 1 : 0,
                    v.Pos.Y > -0.5 ? 1 : 0,
                    v.Pos.Z > 0 ? 1 : 0
                );

                return new NoteVert(
                    v.Pos,
                    v.Normal,
                    (v.Pos.X - bd.Min) / bd.Range,
                    corner
                );
            }

            ShapeBuffer<NoteInstance> bufferFromModel(ModelBuffer<NoteVert> model)
            {
                return new ShapeBuffer<NoteInstance>(new InstancedBufferFlusher<NoteVert, NoteInstance>(1024 * 64, model));
            }

            var noteBody = new ModelBuffer<NoteVert>(
                bodyVerts.Select(convertVertex).ToArray(),
                Enumerable.Range(0, bodyVerts.Count).ToArray()
            );

            if(capVerts.Count != 0)
            {
                var noteCap = new ModelBuffer<NoteVert>(
                    capVerts.Select(convertVertex).ToArray(),
                    Enumerable.Range(0, capVerts.Count).ToArray()
                );

                return new NoteBufferParts(bufferFromModel(noteBody), bufferFromModel(noteCap));
            }
            else
            {
                return new NoteBufferParts(bufferFromModel(noteBody));
            }
        }

        static ModelBuffer<KeyVert> ParseKeyFaces(IEnumerable<Face> faces, LoadResult obj)
        {
            var labelledFaces = CategorizeVertices(faces, obj);

            var allVerts = new List<BasicVert>();

            foreach (var face in labelledFaces)
            {
                allVerts.AddRange(ParseFaceVertices(face.Face, obj));
            }

            var bd = new BoundingDimention(allVerts.Select(v => v.Pos.Z));

            KeyVert convertVertex(BasicVert v)
            {
                var corner = new Vector3(
                    v.Pos.X > 0 ? 1 : 0,
                    v.Pos.Y > -0.5 ? 1 : 0,
                    v.Pos.Z > 0 ? 1 : 0
                );

                return new KeyVert(
                    v.Pos,
                    v.Normal,
                    (v.Pos.Z - bd.Min) / bd.Range
                );
            }

            var model = new ModelBuffer<KeyVert>(
                allVerts.Select(convertVertex).ToArray(),
                Enumerable.Range(0, allVerts.Count).ToArray()
            );

            return model;
        }

        public static FullModelData LoadAllModels()
        {
            var obj = GetObjModel();

            var nonPreview = obj.Groups.Where(g => !g.Name.StartsWith("p-")).ToArray();

            var noteModels = new Dictionary<string, NoteBufferParts>();
            var keyModels = new Dictionary<string, ModelBuffer<KeyVert>>();

            var noteTask = Task.Run(() =>
            {
                Parallel.ForEach(nonPreview.Where(g => g.Name.StartsWith("note-")), g =>
                {
                    var model = ParseNoteFaces(g.Faces, obj);
                    lock (noteModels) noteModels.Add(g.Name, model);
                });
            });

            var keyTask = Task.Run(() =>
            {
                Parallel.ForEach(nonPreview.Where(g => !g.Name.StartsWith("note-")), g =>
                {
                    var model = ParseKeyFaces(g.Faces, obj);
                    lock (keyModels) keyModels.Add(g.Name, model);
                });
            });

            noteTask.Wait();
            keyTask.Wait();

            ModelBuffer<KeyVert>[] getArray(string category, string side)
            {
                var keys = new ModelBuffer<KeyVert>[12];
                for (int i = 0; i < 12; i++)
                {
                    if (KeyboardState.IsBlackKey(i))
                        keys[i] = keyModels[$"black-{category}"];
                    else
                        keys[i] = keyModels[$"white-{category}-{side}-{i}"];
                }
                return keys;
            }

            var notesBatch = new NoteModelData(
                flat: noteModels["note-flat"],
                cube: noteModels["note-cube"],
                rounded: noteModels["note-rounded"]
            );

            var keysBatch = new KeyModelData(
                sameWidth: new KeyModelDataType(
                    left: new KeyModelDataEdge(getArray("sw", "left")),
                    normal: new KeyModelDataEdge(getArray("sw", "both")),
                    right: new KeyModelDataEdge(getArray("sw", "right"))
                ),
                differentWidth: new KeyModelDataType(
                    left: new KeyModelDataEdge(getArray("dw", "left")),
                    normal: new KeyModelDataEdge(getArray("dw", "both")),
                    right: new KeyModelDataEdge(getArray("dw", "right"))
                )
            );

            return new FullModelData(notesBatch, keysBatch);
        }
    }
}
