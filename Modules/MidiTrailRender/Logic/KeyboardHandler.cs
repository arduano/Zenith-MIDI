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
                Util.ReadShader("keys.fx"),
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

            PhysicsKey physics;

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
                    Matrix.RotationX((float)Math.PI / 2 * physics.Keys[key].Press * 0.03f) *
                    Matrix.Translation(0, 0, -1) *
                    Matrix.Scaling((float)keyboard.BlackKeyWidth * 2 * 0.865f) *
                    Matrix.Translation(pos) *
                    Matrix.RotationY((float)Math.PI / 2 * 0) *
                    Matrix.Translation(0, 0, 0);

                var obj = new KeyRenderObject(config, handler, keyPart.GetKey(key), pos, modelmat);

                obj.keyColor = keyboard.Colors[key];
                obj.physics = physics.Keys[key];

                return obj;
            }

            KeyRenderObject(BaseModel config, KeyboardHandler handler, ModelBuffer<KeyVert> model, Vector3 pos, Matrix transform) : base(config, pos, transform)
            {
                this.model = model;
                this.handler = handler;
            }

            static Color4 AdjustDiffuse(Color4 col, float fac)
            {
                col.Red *= fac;
                col.Green *= fac;
                col.Blue *= fac;
                return col;
            }

            static Color4 AdjustSpec(Color4 col, float fac)
            {
                return new Color4(1, 1, 1, fac);
            }

            static Color4 AdjustEmit(Color4 col, float fac)
            {
                col.Alpha *= fac;
                return col;
            }

            static Color4 AdjustWater(Color4 col, float fac)
            {
                return AdjustEmit(col, fac);
            }

            public override void Render(DeviceContext context, Camera camera)
            {
                var keyConfig = Config.Keys;
                var unpCol = keyConfig.UnpressedColor;
                var pCol = keyConfig.PressedColor;

                var blend = Math.Max(0, Math.Min(1, physics.Press));

                var leftCol = physics.GetLeftCol(true);
                var rightCol = physics.GetRightCol(true);

                var leftWater = leftCol;
                var rightWater = rightCol;

                var keyShader = handler.keyShader;

                var diffuse = Util.Lerp((float)unpCol.Diffuse, (float)pCol.Diffuse, blend);
                var specular = Util.Lerp((float)unpCol.Specular, (float)pCol.Specular, blend);
                var emit = Util.Lerp((float)unpCol.Emit, (float)pCol.Emit, blend);
                var water = Util.Lerp((float)unpCol.Water, (float)pCol.Water, blend);

                keyShader.ConstData.View = camera.ViewPerspective;
                keyShader.ConstData.ViewPos = camera.ViewLocation;
                keyShader.ConstData.Time = (float)camera.Time;

                keyShader.ConstData.Model = Transform;

                keyShader.ConstData.LeftColor.Diffuse = AdjustDiffuse(leftCol, diffuse);
                keyShader.ConstData.RightColor.Diffuse = AdjustDiffuse(rightCol, diffuse);

                keyShader.ConstData.LeftColor.Specular = AdjustSpec(leftCol, specular);
                keyShader.ConstData.RightColor.Specular = AdjustSpec(rightCol, specular);

                keyShader.ConstData.LeftColor.Emit = AdjustEmit(leftCol, emit);
                keyShader.ConstData.RightColor.Emit = AdjustEmit(rightCol, emit);

                keyShader.ConstData.LeftColor.Water = AdjustEmit(leftWater, water);
                keyShader.ConstData.RightColor.Water = AdjustEmit(rightWater, water);

                using (keyShader.UseOn(context))
                    model.BindAndDraw(context);
            }
        }
    }
}
