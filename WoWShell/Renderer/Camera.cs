using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WoWShell.Renderer
{
    public class Camera
    {
        public Vector3 position { get; set; }
        public Vector3 target { get; set; }
        public float aspectRatio { get; set; }
        public float fov { get; set; }
        public float nearPlane { get; set; }
        public float farPlane { get; set; }
    }
}
