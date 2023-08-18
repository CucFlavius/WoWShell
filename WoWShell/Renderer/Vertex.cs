using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoWShell.Renderer
{
    public struct Vertex
    {
        public Vector3 Normal;
        public Vector3 Coordinates;
        public Vector3 WorldCoordinates;

        public Vertex(M2.Vertex vertex)
        {
            this.Coordinates = vertex.position;
            this.Normal = vertex.normal;
            this.WorldCoordinates = Vector3.Zero;
        }

        public Vertex(Vector3 position, Vector3 normal)
        {
            this.Coordinates = position;
            this.Normal = normal;
            this.WorldCoordinates = Vector3.Zero;
        }
    }
}
