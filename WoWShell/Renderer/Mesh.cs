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

            for (int v = 0; v < vertCount; v++)
            {
                var vertIdx = vertexIndices[v + vertStart];
                this.vertices[v] = new Vertex(vertices[vertIdx]);
            }

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
