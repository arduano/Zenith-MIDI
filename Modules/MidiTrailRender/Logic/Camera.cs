using MIDITrailRender.Models;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZenithEngine;
using ZenithEngine.DXHelper;

namespace MIDITrailRender.Logic
{
    public class Camera
    {
        public Matrix View { get; }
        public Vector3 ViewLocation { get; }
        public Matrix Perspective { get; }
        public Matrix ViewPerspective { get; }
        public double Time { get; }

        public Camera(BaseModel context, RenderStatus status, double time)
        {
            var camera = context.Camera;
            ViewLocation = new Vector3((float)camera.CamX, (float)camera.CamY, (float)camera.CamZ);
            View =
                Matrix.Translation(ViewLocation) *
                Matrix.RotationY((float)(camera.CamRotY / 180 * Math.PI)) *
                Matrix.RotationX((float)(camera.CamRotX / 180 * Math.PI)) *
                Matrix.RotationZ((float)(camera.CamRotZ / 180 * Math.PI)) *
                Matrix.Scaling(1, 1, -1);
            Perspective = Matrix.PerspectiveFovLH((float)(camera.CamFOV / 180 * Math.PI), status.AspectRatio, 0.01f, 100f)
                * Matrix.Translation(0, (float)camera.OffsetZ, 0);
            ViewPerspective = View * Perspective;
            Time = time;
        }

        public void RenderOrdered(IEnumerable<RenderObject> objects, DeviceContext context)
        {
            var ordered = objects.OrderBy(obj => -(obj.Position - ViewLocation).Length());
            foreach(var obj in ordered)
            {
                obj.Render(context, this);
            }
        }
    }
}
