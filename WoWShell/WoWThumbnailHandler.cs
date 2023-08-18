using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using SharpShell.SharpThumbnailHandler;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing.Text;
using SharpShell.Attributes;
using System.Runtime.InteropServices;
using WoWShell.Renderer;
using SharpDX;
using SharpShell.Helpers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using SharpShell.Interop;
using System.Threading;

namespace WoWShell
{
    public class TestApp
    {
        public static void Main()
        {
            WoWThumbnailHandler tmb = new WoWThumbnailHandler();
            //Bitmap bmp = tmb.RenderTestCube(512, 512, "Test Cube");
            var filePath = ("C:\\Users\\cg3\\Desktop\\Creature\\AbominationSmall\\AbominationSmall.m2");
            Bitmap bmp = tmb.RenderM2(512, 512, filePath);
            bmp.Save("test.jpg", ImageFormat.Jpeg);
        }
    }

    [ComVisible(true)]
    [COMServerAssociation(AssociationType.FileExtension, ".m2")]
    public class WoWThumbnailHandler : /*SharpThumbnailHandler*/ FileThumbnailHandler
    {
        private readonly Lazy<Font> lazyThumbnailFont;
        private readonly Lazy<Brush> lazyThumbnailTextBrush;

        public WoWThumbnailHandler()
        {
            lazyThumbnailFont = new Lazy<Font>(() => new Font("Courier New", 12f));
            lazyThumbnailTextBrush = new Lazy<Brush>(() => new SolidBrush(System.Drawing.Color.White));
        }

        protected override Bitmap GetThumbnailImage(uint width)
        {
            try
            {
                return RenderM2(width, width, SelectedItemPath /*SelectedItemStream*/);
            }
            catch (Exception exception)
            {
                LogError("Error rendering M2", exception);
                return null;
            }
        }

        public Bitmap RenderM2(uint sizeX, uint sizeY, string filePath)
        {
            using (FileStream fs = File.OpenRead(filePath))
            using (BinaryReader br = new BinaryReader(fs))
            {
                return RenderM2(512, 512, filePath, br);
            }
        }

        public Bitmap RenderM2(uint sizeX, uint sizeY, ComStream comStream)
        {
            var br = new BinaryReader(comStream);
            return RenderM2(sizeX, sizeY, comStream.Name, br);
        }

        public Bitmap RenderM2(uint sizeX, uint sizeY, string path, BinaryReader br)
        {
            try
            {
                M2 m2 = new M2(br);

                if (GetSkinPath(m2, path, out string skinPath))
                {
                    using (FileStream sfs = File.OpenRead(skinPath))
                    using (BinaryReader sbr = new BinaryReader(sfs))
                    {
                        Skin skin = new Skin(sbr);

                        Mesh[] meshes = new Mesh[skin.submeshes.Length];
                        for (int s = 0; s < skin.submeshes.Length; s++)
                        {
                            meshes[s] = new Mesh("m2", skin.submeshes[s], m2.md20.vertices, skin.vertexIndices, skin.triangleIndices);
                        }

                        Camera camera = new Camera();
                        camera.fov = 0.78f;
                        camera.aspectRatio = (float)sizeX / sizeY;
                        camera.nearPlane = 0.01f;
                        camera.farPlane = 100.0f;

                        var distance = CalculateCameraDistanceForBoundingBox(m2.md20.boundingBox, camera);
                        var center = m2.md20.boundingBox.Center;

                        camera.position = new Vector3(distance, center.Y, 0);
                        camera.target = new Vector3(0, center.Y, 0);

                        Bitmap bmp = new Bitmap((int)sizeX, (int)sizeY);
                        Device device = new Device(ref bmp);
                        device.Render(camera, meshes);
                        return bmp;
                    }
                }
                else
                {
                    return RenderErrorThumbnail("p: " + skinPath, sizeX, sizeY);
                }
            }
            catch(Exception ex)
            {
                LogError(ex.Message, ex);
                return RenderErrorThumbnail(ex.Message, sizeX, sizeY);
            }
        }

        public float CalculateCameraDistanceForBoundingBox(BoundingBox boundingBox, Camera camera)
        {
            var center = boundingBox.Center;
            var size = boundingBox.Size;
            var maxSize = Math.Max(Math.Max(size.X, size.Y), size.Z);
            var offset = 0;// size.X / 4;
            var startDistance = Vector3.Distance(center, camera.position);
            // here we must check if the screen is horizontal or vertical, because camera.fov is
            // based on the vertical direction.
            var endDistance = camera.aspectRatio > 1 ?
                                ((maxSize / 2) + offset) / Math.Abs(Math.Tan(camera.fov / 2.0f)) :
                                ((maxSize / 2) + offset) / Math.Abs(Math.Tan(camera.fov / 2.0f)) / camera.aspectRatio;

            var distance = endDistance / startDistance;
            return (float)distance;
        }

        public bool GetSkinPath(M2 m2, string m2Path, out string skinPath)
        {
            var dir = Path.GetDirectoryName(m2Path);
            var fileName = Path.GetFileNameWithoutExtension(dir);

            skinPath = $"{dir}/{fileName}00.skin";

            if (File.Exists(skinPath))
                return true;
            /*
            skinPath = $"{m2.md20.name}00.skin";

            if (File.Exists(skinPath))
                return true;
            */
            return false;
        }

        public Bitmap RenderErrorThumbnail(string errorText, uint sizeX, uint sizeY)
        {
            // Choose the back buffer resolution here
            Bitmap bmp = new Bitmap((int)sizeX, (int)sizeY);
            Camera camera = new Camera();
            Device device = new Device(ref bmp);

            var mesh = new Mesh("Cube", 8, 12);

            GenerateCubeMesh(ref mesh);

            camera.position = new Vector3(0, 0, 10.0f);
            camera.target = Vector3.Zero;

            // rotating slightly the cube during each frame rendered
            mesh.rotation = new Vector3(mesh.rotation.X + 0.01f, mesh.rotation.Y + 0.01f, mesh.rotation.Z);

            // Doing the various matrix operations
            device.Render(camera, new Mesh[] { mesh });

            using (var graphics = Graphics.FromImage(bmp))
                graphics.DrawString(errorText, lazyThumbnailFont.Value, lazyThumbnailTextBrush.Value, 0, 0);

            return bmp;
        }

        public void GenerateCubeMesh(ref Mesh mesh)
        {
            mesh.vertices[0] = new Vertex(new Vector3(-1, 1, 1), Vector3.One);
            mesh.vertices[1] = new Vertex(new Vector3(1, 1, 1), Vector3.One);
            mesh.vertices[2] = new Vertex(new Vector3(-1, -1, 1), Vector3.One);
            mesh.vertices[3] = new Vertex(new Vector3(1, -1, 1), Vector3.One);
            mesh.vertices[4] = new Vertex(new Vector3(-1, 1, -1), Vector3.One);
            mesh.vertices[5] = new Vertex(new Vector3(1, 1, -1), Vector3.One);
            mesh.vertices[6] = new Vertex(new Vector3(1, -1, -1), Vector3.One);
            mesh.vertices[7] = new Vertex(new Vector3(-1, -1, -1), Vector3.One);

            mesh.faces[0] = new Face(0, 1, 2);
            mesh.faces[1] = new Face(1, 2, 3);
            mesh.faces[2] = new Face(1, 3, 6);
            mesh.faces[3] = new Face(1, 5, 6);
            mesh.faces[4] = new Face(0, 1, 4);
            mesh.faces[5] = new Face(1, 4, 5);

            mesh.faces[6] = new Face(2, 3, 7);
            mesh.faces[7] = new Face(3, 6, 7);
            mesh.faces[8] = new Face(0, 2, 7);
            mesh.faces[9] = new Face(0, 4, 7);
            mesh.faces[10] = new Face(4, 5, 6);
            mesh.faces[11] = new Face(4, 6, 7);
        }
    }
}
