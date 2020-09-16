using MIDITrailRender.Models;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine.DXHelper;
using ZenithEngine.MIDI;
using ZenithEngine.ModuleUtil;

namespace MIDITrailRender.Logic
{
    public class KeyboardHandler : DeviceInitiable
    {
        ShaderProgram<KeyShaderConstant> keyShader;

        KeyModelData keyModels;

        public KeyboardHandler(FullModelData allModels)
        {
            keyModels = init.Add(allModels.Keys);

            keyShader = init.Add(new ShaderProgram<KeyShaderConstant>(
                Util.ReadEmbed("MIDITrailRender.Shaders.keys.fx"),
                typeof(KeyVert),
                "4_0",
                "VS",
                "PS"
            ));
        }

        public IEnumerable<RenderObject> GetKeyObjects(BaseModel config, KeyboardState keyboard, KeyboardPhysics physics)
        {
            for(int i = keyboard.FirstKey; i < keyboard.LastKey; i++)
            {
                yield return KeyRenderObject.MakeKey(config, this, keyboard, physics, i);
            }
        }

        class KeyRenderObject : RenderObject
        {
            KeyboardHandler handler;
            ModelBuffer<KeyVert> model;
            float pressDepth;

            KeyboardState.Col keyColor;

            public static KeyRenderObject MakeKey(
                BaseModel config,
                KeyboardHandler handler,
                KeyboardState keyboard,
                KeyboardPhysics physics,
                int key
                )
            {
                var keySet = config.General.SameWidthNotes ? handler.keyModels.SameWidth : handler.keyModels.DifferentWidth;

                var layoutKey = keyboard.Keys[key];
                var width = layoutKey.Right - layoutKey.Left;
                var middle = (layoutKey.Right + layoutKey.Left) / 2 - 0.5f;

                var isFirst = key == keyboard.FirstKey;
                var isLast = key == keyboard.LastKey - 1;

                var keyPart = isFirst ? keySet.Right : isLast ? keySet.Left : keySet.Normal;

                var pos = new Vector3((float)middle, 0, 0);
                var modelmat = Matrix.Identity *
                    Matrix.RotationX((float)Math.PI / 2 * physics[key] * 0.03f) *
                    Matrix.Translation(0, 0, -1) *
                    Matrix.Scaling((float)keyboard.BlackKeyWidth * 2 * 0.865f) *
                    Matrix.Translation(pos) *
                    Matrix.RotationY((float)Math.PI / 2 * 0) *
                    Matrix.Translation(0, 0, 0);

                var obj = new KeyRenderObject(config, handler, keyPart.GetKey(key), pos, modelmat);

                obj.pressDepth = physics[key];
                obj.keyColor = keyboard.Colors[key];

                return obj;
            }

            KeyRenderObject(BaseModel config, KeyboardHandler handler, ModelBuffer<KeyVert> model, Vector3 pos, Matrix transform) : base(config, pos, transform)
            {
                this.model = model;
                this.handler = handler;
            }

            public override void Render(DeviceContext context, Camera camera)
            {
                var keyShader = handler.keyShader;

                keyShader.ConstData.View = camera.ViewPerspective;
                keyShader.ConstData.ViewPos = camera.ViewLocation;
                keyShader.ConstData.Time = (float)camera.Time;

                keyShader.ConstData.Model = Transform;

                keyShader.ConstData.LeftColor.Diffuse = keyColor.Left;
                keyShader.ConstData.RightColor.Diffuse = keyColor.Right;

                keyShader.ConstData.LeftColor.Emit.Alpha = 0;
                keyShader.ConstData.RightColor.Emit.Alpha = 0;

                keyShader.ConstData.LeftColor.Emit = keyColor.Left;
                keyShader.ConstData.RightColor.Emit = keyColor.Right;

                float glowStrength = Math.Max(pressDepth, 0);

                keyShader.ConstData.LeftColor.Emit.Alpha = 20 * glowStrength;
                keyShader.ConstData.RightColor.Emit.Alpha = 20 * glowStrength;

                //keyShader.ConstData.LeftColor.Emit.Alpha = 0;
                //keyShader.ConstData.RightColor.Emit.Alpha = 0;

                keyShader.ConstData.LeftColor.Specular = new Color4(1, 1, 1, 1);
                keyShader.ConstData.RightColor.Specular = new Color4(1, 1, 1, 1);

                using (keyShader.UseOn(context))
                    model.BindAndDraw(context);
            }
        }
    }
}
