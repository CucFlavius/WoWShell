using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static WoWShell.Skin;

namespace WoWShell.Renderer
{
    public class Mesh
    {
        public string name { get; set; }
        public Color4 wireColor { get; set; }
        public BoundingBox boundingBox { get; set; }
        public Vertex[] vertices { get; private set; }
        public Face[] faces { get; set; }
        public Vector3 position { get; set; }
        public Vector3 rotation { get; set; }

        public Mesh(string name, int verticesCount, int facesCount)
        {
            this.wireColor = Color4.White;
            vertices = new Vertex[verticesCount];
            faces = new Face[facesCount];
            this.name = name;
        }

        public Mesh(string name, M2SkinSection submesh, M2.Vertex[] vertices, ushort[] vertexIndices, ushort[] triangles)
        {
            this.name = name;
            this.position = Vector3.Zero;
            this.rotation = Vector3.Zero;
            this.wireColor = Color4.White;

            var vertCount = submesh.vertexCount;
            var vertStart = submesh.startVertex;
            this.vertices = new Vertex[vertCount];

            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
            for (int v = 0; v < vertCount; v++)
            {
                var vertIdx = vertexIndices[v + vertStart];
                var vertex = new Vertex(vertices[vertIdx]);
                this.vertices[v] = vertex;

                if (vertex.Coordinates.X < minX) minX = vertex.Coordinates.X;
                if (vertex.Coordinates.Y < minY) minY = vertex.Coordinates.Y;
                if (vertex.Coordinates.Z < minZ) minZ = vertex.Coordinates.Z;

                if (vertex.Coordinates.X > maxX) maxX = vertex.Coordinates.X;
                if (vertex.Coordinates.Y > maxY) maxY = vertex.Coordinates.Y;
                if (vertex.Coordinates.X > maxZ) maxZ = vertex.Coordinates.Z;
            }
            this.boundingBox = new BoundingBox(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));

            var triCount = submesh.triangleCount;
            var triStart = submesh.startTriangle + (submesh.level << 16);

            int[] triangleBuffer = new int[triCount];
            for (int t = 0; t < triCount; t++)
            {
                triangleBuffer[t] = triangles[t + triStart] - submesh.startVertex;
            }

            this.faces = new Face[triCount / 3];
            for (int i = 0; i < this.faces.Length; i++)
            {
                this.faces[i] = new Face(triangleBuffer[i * 3], triangleBuffer[i * 3 + 1], triangleBuffer[i * 3 + 2]);
            }
        }
    }
}
