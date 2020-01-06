using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;

namespace PianoFallRender
{
    class PhysObject
    {
        public Matrix3 RotMatrix => Matrix3.Identity;

        public Vector3 Pos = Vector3.Zero;
        public Vector3 Vel = Vector3.Zero;

        public Quaternion Rot;
        public Quaternion RotVel;

        public Vector3 NegativeBound = Vector3.Zero;
        public Vector3 PositiveBound = Vector3.Zero;

        public Vector3 Size = Vector3.Zero;

        public PhysObject()
        {
            //Rot.
        }
    }
}
