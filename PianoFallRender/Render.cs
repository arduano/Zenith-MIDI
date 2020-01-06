using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMEngine;
using Jitter;
using Jitter.Collision;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace PianoFallRender
{
    public class Render : IPluginRender
    {
        public string Name => "PianoFall+";

        public string Description => "3D Physics note simulations";

        #region Shaders
        string noteShaderVert = @"#version 330 core

layout(location=0) in vec3 in_position;
layout(location=1) in vec4 in_color;
layout(location=2) in float in_shade;

out vec4 v2f_color;
out vec3 world_pos;

uniform mat4 MVP;

void main()
{
    world_pos = in_position;
    gl_Position = MVP * vec4(in_position, 1.0);
    v2f_color = vec4(in_color.xyz + in_shade, in_color.w);
}
";
        string noteShaderFrag = @"#version 330 core

in vec4 v2f_color;
in vec3 world_pos;
layout (location=0) out vec4 out_color;

uniform vec3 shading;

void main()
{
	vec3 dFdxPos = dFdx( world_pos );
	vec3 dFdyPos = dFdy( world_pos );
	vec3 facenormal = normalize( cross(dFdxPos,dFdyPos ));
    float shade = dot(facenormal, shading);
    shade = max(0, shade) / 2 + 0.5f;
	//out_color = vec4(facenormal*0.5 + 0.5,1.0);
    out_color = vec4(v2f_color.xyz * shade, v2f_color.w);
}
";

        int MakeShader(string vert, string frag)
        {
            int _vertexObj = GL.CreateShader(ShaderType.VertexShader);
            int _fragObj = GL.CreateShader(ShaderType.FragmentShader);
            int statusCode;
            string info;

            GL.ShaderSource(_vertexObj, vert);
            GL.CompileShader(_vertexObj);
            info = GL.GetShaderInfoLog(_vertexObj);
            GL.GetShader(_vertexObj, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            GL.ShaderSource(_fragObj, frag);
            GL.CompileShader(_fragObj);
            info = GL.GetShaderInfoLog(_fragObj);
            GL.GetShader(_fragObj, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            int shader = GL.CreateProgram();
            GL.AttachShader(shader, _fragObj);
            GL.AttachShader(shader, _vertexObj);
            GL.LinkProgram(shader);
            return shader;
        }

        #endregion

        public bool Initialized { get; set; }

        public System.Windows.Media.ImageSource PreviewImage { get; set; } = null;

        public bool ManualNoteDelete => true;

        public int NoteCollectorOffset => 0;

        public double LastMidiTimePerTick { get; set; }
        public MidiInfo CurrentMidi { get; set; }

        public double NoteScreenTime => 0;

        public long LastNoteCount { get; private set; }

        public System.Windows.Controls.Control SettingsControl { get; set; } = null;

        CollisionSystem collision;
        World world;

        RenderSettings renderSettings;
        Settings settings;

        Util util;

        int noteShader;
        int uNoteMVP;
        int uNoteShading;

        int buffer3dtex;
        int buffer3dbuf;
        int buffer3dbufdepth;

        int noteVert;
        int noteCol;
        int noteIndx;
        int noteShade;

        int noteBuffLen = 2048 * 256;

        double[] noteVertBuff;
        float[] noteColBuff;
        float[] noteShadeBuff;
        int[] noteIndxBuff;

        int noteBuffPos = 0;

        public Render(RenderSettings settings)
        {
            renderSettings = settings;
            this.settings = new Settings();
        }

        public void Dispose()
        {
            world.Clear();
            GL.DeleteBuffers(4, new int[] {
                noteVert, noteCol, noteIndx, noteShade
            });
            GL.DeleteProgram(noteShader);

            noteVertBuff = null;
            noteColBuff = null;
            noteShadeBuff = null;
            noteIndxBuff = null;

            GL.DeleteProgram(noteShader);
            util.Dispose();
            Console.WriteLine("Disposed of PianoFallRender");
            Initialized = false;
        }

        public void Init()
        {
            noteShader = MakeShader(noteShaderVert, noteShaderFrag);
            //collision = new CollisionSystemSAP();
            collision = new CollisionSystemPersistentSAP();
            world = new World(collision);
            world.Gravity = new JVector(0, -30, 0);
            util = new Util();

            uNoteMVP = GL.GetUniformLocation(noteShader, "MVP");
            uNoteShading = GL.GetUniformLocation(noteShader, "shading");

            noteVert = GL.GenBuffer();
            noteCol = GL.GenBuffer();
            noteIndx = GL.GenBuffer();
            noteShade = GL.GenBuffer();

            noteVertBuff = new double[noteBuffLen * 8 * 3];
            noteColBuff = new float[noteBuffLen * 8 * 4];
            noteShadeBuff = new float[noteBuffLen * 8];

            noteIndxBuff = new int[noteBuffLen * 12 * 3];

            for (int i = 0; i < noteBuffLen; i++)
            {
                var j = i * 3 * 12;
                var p = i * 8;
                noteIndxBuff[j++] = p + 0;
                noteIndxBuff[j++] = p + 1;
                noteIndxBuff[j++] = p + 3;
                noteIndxBuff[j++] = p + 0;
                noteIndxBuff[j++] = p + 3;
                noteIndxBuff[j++] = p + 2;

                noteIndxBuff[j++] = p + 0;
                noteIndxBuff[j++] = p + 5;
                noteIndxBuff[j++] = p + 1;
                noteIndxBuff[j++] = p + 0;
                noteIndxBuff[j++] = p + 4;
                noteIndxBuff[j++] = p + 5;

                noteIndxBuff[j++] = p + 1;
                noteIndxBuff[j++] = p + 5;
                noteIndxBuff[j++] = p + 7;
                noteIndxBuff[j++] = p + 1;
                noteIndxBuff[j++] = p + 7;
                noteIndxBuff[j++] = p + 3;

                noteIndxBuff[j++] = p + 3;
                noteIndxBuff[j++] = p + 6;
                noteIndxBuff[j++] = p + 7;
                noteIndxBuff[j++] = p + 2;
                noteIndxBuff[j++] = p + 6;
                noteIndxBuff[j++] = p + 3;

                noteIndxBuff[j++] = p + 0;
                noteIndxBuff[j++] = p + 2;
                noteIndxBuff[j++] = p + 4;
                noteIndxBuff[j++] = p + 2;
                noteIndxBuff[j++] = p + 6;
                noteIndxBuff[j++] = p + 4;

                noteIndxBuff[j++] = p + 4;
                noteIndxBuff[j++] = p + 6;
                noteIndxBuff[j++] = p + 7;
                noteIndxBuff[j++] = p + 4;
                noteIndxBuff[j++] = p + 7;
                noteIndxBuff[j++] = p + 5;
            }
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, noteIndx);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(noteIndxBuff.Length * 4),
                noteIndxBuff,
                BufferUsageHint.StaticDraw);

            GLUtils.GenFrameBufferTexture3d(renderSettings.width, renderSettings.height, out buffer3dbuf, out buffer3dtex, out buffer3dbufdepth);
            Console.WriteLine("Initialised PianoFallRender");
            Initialized = true;
        }

        public void RenderFrame(FastList<Note> notes, double midiTime, int finalCompositeBuff)
        {
            GL.Enable(EnableCap.Blend);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer3dbuf);
            GL.Viewport(0, 0, renderSettings.width, renderSettings.height);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            GL.UseProgram(noteShader);

            double aspect = (double)renderSettings.width / renderSettings.height;
            var mat = Matrix4.Identity *
            Matrix4.CreateTranslation(0, 0, -100) *
            Matrix4.CreatePerspectiveFieldOfView((float)1.3, (float)aspect, 0.01f, 400)
            ;
            GL.UniformMatrix4(uNoteMVP, false, ref mat);
            GL.Uniform3(uNoteShading, new Vector3(1, 1, 1));

            long nc = 0;
            Vector3[] vecs = new Vector3[8];
            Random rand = new Random();
            foreach (Note n in notes)
            {
                if (n.start < midiTime)
                {
                    nc++;
                    if (n.meta == null)
                    {
                        JVector size = new JVector(1.0f, 5.0f, 5.0f);
                        Shape shape = new BoxShape(size);
                        shape.Tag = size;
                        RigidBody body = new RigidBody(shape);
                        body.Position = new JVector(n.note - 64 + (float)(rand.NextDouble() - 0.5f) / 1000, 0, 0);
                        n.meta = body;
                        world.AddBody(body);
                    }
                    else
                    {
                        if (!n.delete && ((RigidBody)n.meta).Position.Y < -50)
                        {
                            n.delete = true;
                            world.RemoveBody(((RigidBody)n.meta));
                            n.meta = null;
                        }
                    }

                    if (n.meta != null)
                    {
                        var m = (RigidBody)n.meta;
                        var p = m.Orientation;
                        var shape = (BoxShape)m.Shape;
                        var size = (JVector)shape.Tag;
                        var pos = noteBuffPos * 24;
                        for (int x = 0; x < 2; x++)
                            for (int y = 0; y < 2; y++)
                                for (int z = 0; z < 2; z++)
                                {
                                    float _x = x == 0 ? -size.X : size.X;
                                    float _y = y == 0 ? -size.Y : size.Y;
                                    float _z = z == 0 ? -size.Z : size.Z;
                                    noteVertBuff[pos++] = _x * p.M11 + _y * p.M12 + _z * p.M13 + m.Position.X;
                                    noteVertBuff[pos++] = _x * p.M21 + _y * p.M22 + _z * p.M23 + m.Position.Y;
                                    noteVertBuff[pos++] = _x * p.M31 + _y * p.M32 + _z * p.M33 + m.Position.Z;
                                }
                        float r = n.color.left.R;
                        float g = n.color.left.G;
                        float b = n.color.left.B;

                        pos = noteBuffPos * 32;
                        noteColBuff[pos++] = r;
                        noteColBuff[pos++] = g;
                        noteColBuff[pos++] = b;
                        noteColBuff[pos++] = 1;
                        noteColBuff[pos++] = r;
                        noteColBuff[pos++] = g;
                        noteColBuff[pos++] = b;
                        noteColBuff[pos++] = 1;
                        noteColBuff[pos++] = r;
                        noteColBuff[pos++] = g;
                        noteColBuff[pos++] = b;
                        noteColBuff[pos++] = 1;
                        noteColBuff[pos++] = r;
                        noteColBuff[pos++] = g;
                        noteColBuff[pos++] = b;
                        noteColBuff[pos++] = 1;
                        noteColBuff[pos++] = r;
                        noteColBuff[pos++] = g;
                        noteColBuff[pos++] = b;
                        noteColBuff[pos++] = 1;
                        noteColBuff[pos++] = r;
                        noteColBuff[pos++] = g;
                        noteColBuff[pos++] = b;
                        noteColBuff[pos++] = 1;
                        noteColBuff[pos++] = r;
                        noteColBuff[pos++] = g;
                        noteColBuff[pos++] = b;
                        noteColBuff[pos++] = 1;
                        noteColBuff[pos++] = r;
                        noteColBuff[pos++] = g;
                        noteColBuff[pos++] = b;
                        noteColBuff[pos++] = 1;
                        noteBuffPos++;

                        FlushNoteBuffer();
                    }
                }
                else { break; }
            }
            FlushNoteBuffer(false);
            LastNoteCount = nc;
            world.Step((float)(1.0f / renderSettings.fps), true);

            GL.Disable(EnableCap.Blend);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.DepthTest);

            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.DisableVertexAttribArray(2);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, finalCompositeBuff);
            GL.BindTexture(TextureTarget.Texture2D, buffer3dtex);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            util.DrawScreenQuad();
        }

        void FlushNoteBuffer(bool check = true)
        {
            if (noteBuffPos < noteBuffLen && check) return;
            if (noteBuffPos == 0) return;
            GL.BindBuffer(BufferTarget.ArrayBuffer, noteVert);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(noteBuffPos * 3 * 8 * 8),
                noteVertBuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Double, false, 24, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, noteCol);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(noteBuffPos * 4 * 8 * 4),
                noteColBuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 16, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, noteShade);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(noteBuffPos * 1 * 8 * 4),
                noteShadeBuff,
                BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 4, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, noteIndx);
            GL.IndexPointer(IndexPointerType.Int, 1, 0);
            GL.DrawElements(PrimitiveType.Triangles, noteBuffPos * 12 * 3, DrawElementsType.UnsignedInt, IntPtr.Zero);
            noteBuffPos = 0;
        }

        public void SetTrackColors(NoteColor[][] trakcs)
        {

        }
    }
}
