using SharpDX;
using SharpShell.Attributes;
using SharpShell.Helpers;
using SharpShell.SharpThumbnailHandler;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WoWShell.Renderer;

namespace WoWShell
{
    // Only used for Debug purposes, by setting project to console app instead of class library //
    public class TestApp
    {
        public static void Main()
        {
            //DebugM2Thumbs("C:\\Users\\cg3\\Desktop\\Creature", ".\\Output", 80);
            //DebugSingleM2Thumb("C:\\Users\\cg3\\Desktop\\Creature\\AbominationSmall\\AbominationSmall.m2", "test.jpg");
            //DebugBLPThumbs("C:\\Users\\cg3\\Desktop\\Creature", ".\\Output", 80);
        }

        public static void DebugM2Thumbs(string path, string outputFolder, int limit = -1)
        {
            M2ThumbnailHandler tmb = new M2ThumbnailHandler();

            var filePaths = Directory.GetFiles(path, "*.m2", SearchOption.AllDirectories);
            Directory.CreateDirectory(outputFolder);

            var filteredFiles = new List<string>();
            if (limit == -1)
            {
                filteredFiles = filePaths.ToList();
            }
            else
            {
                for (int i = 0; i < limit; i++)
                    filteredFiles.Add(filePaths[i]);
            }

            Parallel.ForEach(filteredFiles, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, (filePath) =>
            {
                Bitmap bmp = tmb.RenderM2(512, 512, filePath);
                bmp.Save($"{outputFolder}\\{Path.GetFileNameWithoutExtension(filePath)}.jpg", ImageFormat.Jpeg);
            });
        }

        public static void DebugSingleM2Thumb(string filePath, string outputPath)
        {
            M2ThumbnailHandler tmb = new M2ThumbnailHandler();
            Bitmap bmp = tmb.RenderM2(512, 512, filePath);
            bmp.Save(outputPath, ImageFormat.Jpeg);
        }

        public static void DebugBLPThumbs(string path, string outputFolder, int limit = -1)
        {
            BLPThumbnailHandler tmb = new BLPThumbnailHandler();

            var filePaths = Directory.GetFiles(path, "*.blp", SearchOption.AllDirectories);
            Directory.CreateDirectory(outputFolder);

            var filteredFiles = new List<string>();
            if (limit == -1)
            {
                filteredFiles = filePaths.ToList();
            }
            else
            {
                for (int i = 0; i < limit; i++)
                    filteredFiles.Add(filePaths[i]);
            }

            Parallel.ForEach(filteredFiles, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, (filePath) =>
            {
                using (var fs = File.OpenRead(filePath))
                {
                    Bitmap bmp = tmb.RenderBLP(512, 512, fs);
                    bmp.Save($"{outputFolder}\\{Path.GetFileNameWithoutExtension(filePath)}.jpg", ImageFormat.Jpeg);
                }
            });
        }
    }

    [ComVisible(true)]
    [COMServerAssociation(AssociationType.FileExtension, ".m2")]
    public class M2ThumbnailHandler : /*SharpThumbnailHandler*/ FileThumbnailHandler
    {
        public M2ThumbnailHandler() { }

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

        public Bitmap RenderM2(uint sizeX, uint sizeY, string m2Path, BinaryReader br)
        {
            try
            {
                M2 m2 = new M2(br);

                if (GetSkinPath(m2, m2Path, out string skinPath))
                {
                    using (FileStream sfs = File.OpenRead(skinPath))
                    using (BinaryReader sbr = new BinaryReader(sfs))
                    {
                        Skin skin = new Skin(sbr);

                        Mesh[] meshes = new Mesh[skin.submeshes.Length];
                        BoundingBox overallBounds = new BoundingBox();
                        for (int s = 0; s < skin.submeshes.Length; s++)
                        {
                            meshes[s] = new Mesh("m2", skin.submeshes[s], m2.md20.vertices, skin.vertexIndices, skin.triangleIndices);
                            overallBounds = BoundingBox.Merge(meshes[s].boundingBox, overallBounds);
                        }

                        Camera camera = new Camera();
                        camera.fov = 0.78f;
                        camera.aspectRatio = (float)sizeX / sizeY;
                        camera.nearPlane = 0.01f;
                        camera.farPlane = 100.0f;

                        var distance = CalculateCameraDistanceForBoundingBox(overallBounds, camera);
                        var center = overallBounds.Center;

                        camera.position = new Vector3(distance * 0.80f, center.Y, distance * 0.3f);
                        camera.target = new Vector3(0, center.Y, 0);

                        Bitmap bmp = new Bitmap((int)sizeX, (int)sizeY);
                        Device device = new Device(ref bmp);
                        device.Render(camera, meshes);

                        // DEBUG //
                        //RenderText(Path.GetFileName(m2Path), 0, 0, ref bmp);
                        //RenderText(Path.GetFileName(skinPath), 0, 20, ref bmp);

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
            var radius = Math.Max(Math.Max(boundingBox.Size.X, boundingBox.Size.Y), boundingBox.Size.Z);
            var distance = radius / (Math.Sin(camera.fov / 2f));
            return (float)distance;
        }

        public bool GetSkinPath(M2 m2, string m2Path, out string skinPath)
        {
            var dir = Path.GetDirectoryName(m2Path);
            var fileName = Path.GetFileNameWithoutExtension(m2Path);

            // Try finding the skin based on the m2 file name
            skinPath = $"{dir}/{fileName}00.skin";

            if (File.Exists(skinPath))
                return true;

            // Try finding the skin based on the m2 model name
            skinPath = $"{dir}/{m2.md20.name}00.skin";

            if (File.Exists(skinPath))
                return true;
            
            // Just find the first skin file in the directory
            var potentialFiles = Directory.GetFiles(dir, "*00.skin");
            if (potentialFiles.Length > 0)
            {
                skinPath = potentialFiles[0];
                return true;
            }

            return false;
        }

        public Bitmap RenderErrorThumbnail(string errorText, uint sizeX, uint sizeY)
        {
            Lazy<Font> lazyThumbnailFont = new Lazy<Font>(() => new Font("Courier New", 12f));
            Lazy<Brush> lazyThumbnailTextBrush = new Lazy<Brush>(() => new SolidBrush(System.Drawing.Color.White));

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

        public void RenderText(string text, int x, int y, ref Bitmap bmp)
        {
            Lazy<Font> lazyThumbnailFont = new Lazy<Font>(() => new Font("Courier New", 12f));
            Lazy<Brush> lazyThumbnailTextBrush = new Lazy<Brush>(() => new SolidBrush(System.Drawing.Color.White));

            using (var graphics = Graphics.FromImage(bmp))
                graphics.DrawString(text, lazyThumbnailFont.Value, lazyThumbnailTextBrush.Value, x, y);
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

    [ComVisible(true)]
    [COMServerAssociation(AssociationType.FileExtension, ".blp")]
    public class BLPThumbnailHandler : SharpThumbnailHandler
    {
        public BLPThumbnailHandler() { }

        protected override Bitmap GetThumbnailImage(uint width)
        {
            try
            {
                return RenderBLP(width, width, SelectedItemStream);
            }
            catch (Exception exception)
            {
                LogError("Error rendering BLP", exception);
                return null;
            }
        }

        public Bitmap RenderBLP(uint width, uint height, Stream str)
        {
            SereniaBLPLib.BlpFile blp = new SereniaBLPLib.BlpFile(str);
            var bmp = blp.GetBitmap(0);

            return bmp;
        }
    }

    /*
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.FileExtension, ".lua")]
    public class LuaThumbnailHandler : SharpThumbnailHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuaThumbnailHandler"/> class
        /// </summary>
        public LuaThumbnailHandler() { }

        /// <summary>
        /// Gets the thumbnail image
        /// </summary>
        /// <param name="width">The width of the image that should be returned.</param>
        /// <returns>
        /// The image for the thumbnail
        /// </returns>
        protected override Bitmap GetThumbnailImage(uint width)
        {
            //  Attempt to open the stream with a reader
            try
            {
                using (var reader = new StreamReader(SelectedItemStream))
                {
                    //  Read up to ten lines of text
                    var previewLines = new List<string>();
                    for (int i = 0; i < 10; i++)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                            break;
                        previewLines.Add(line);
                    }

                    //  Now return a preview of the lines
                    return CreateThumbnailForText(previewLines, width);
                }
            }
            catch (Exception exception)
            {
                //  Log the exception and return null for failure
                LogError("An exception occurred opening the text file.", exception);
                return null;
            }
        }

        /// <summary>
        /// Creates the thumbnail for text, using the provided preview lines
        /// </summary>
        /// <param name="previewLines">The preview lines.</param>
        /// <param name="width">The width.</param>
        /// <returns>
        /// A thumbnail for the text
        /// </returns>
        private Bitmap CreateThumbnailForText(IEnumerable<string> previewLines, uint width)
        {
            Lazy<Font> lazyThumbnailFont = new Lazy<Font>(() => new Font("Courier New", 12f));
            Lazy<Brush> lazyThumbnailTextBrush = new Lazy<Brush>(() => new SolidBrush(System.Drawing.Color.White));

            //  Create the bitmap dimensions
            var thumbnailSize = new Size((int)width, (int)width);

            //  Create the bitmap
            var bitmap = new Bitmap(thumbnailSize.Width,
                                    thumbnailSize.Height, PixelFormat.Format32bppArgb);

            //  Create a graphics object to render to the bitmap
            using (var graphics = Graphics.FromImage(bitmap))
            {
                //  Set the rendering up for anti-aliasing
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

                //  Draw the page background
                //graphics.DrawImage(Properties.Resources.Page, 0, 0,
                //                   thumbnailSize.Width, thumbnailSize.Height);

                //  Create offsets for the text
                var xOffset = width * 0.2f;
                var yOffset = width * 0.3f;
                var yLimit = width - yOffset;

                graphics.Clip = new Region(new System.Drawing.RectangleF(xOffset, yOffset,
                  thumbnailSize.Width - (xOffset * 2), thumbnailSize.Height - width * .1f));

                //  Render each line of text
                foreach (var line in previewLines)
                {
                    graphics.DrawString(line, lazyThumbnailFont.Value,
                                        lazyThumbnailTextBrush.Value, xOffset, yOffset);
                    yOffset += 14f;
                    if (yOffset + 14f > yLimit)
                        break;
                }
            }

            //  Return the bitmap
            return bitmap;
        }
    }
    */
}
