using MIDITrailRender.Models;
using SharpDX;
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

        public Camera(BaseModel context, RenderStatus status)
        {
            var camera = context.Camera;
            View =
                Matrix.Translation((float)camera.CamX, (float)camera.CamY, (float)camera.CamZ) *
                Matrix.RotationY((float)(camera.CamRotY / 180 * Math.PI)) *
                Matrix.RotationX((float)(camera.CamRotX / 180 * Math.PI)) *
                Matrix.RotationZ((float)(camera.CamRotZ / 180 * Math.PI)) *
                Matrix.Scaling(1, 1, -1);
            Perspective = Matrix.PerspectiveFovLH((float)(camera.CamFOV / 180 * Math.PI), status.AspectRatio, 0.1f, 100f);
            ViewLocation = View.TranslationVector;
            ViewPerspective = View * Perspective;
        }
    }
}
