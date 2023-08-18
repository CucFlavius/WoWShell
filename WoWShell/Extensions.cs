using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoWShell
{
    public static class Extensions
    {
        public static Vector2 ReadVector2(this BinaryReader br)
        {
            var x = br.ReadSingle();
            var y = br.ReadSingle();
            return new Vector2(x, y);
        }

        public static Vector3 ReadVector3(this BinaryReader br)
        {
            var x = br.ReadSingle();
            var y = br.ReadSingle();
            var z = br.ReadSingle();
            return new Vector3(x, z, -y);
        }

        public static BoundingBox ReadBoundingBox(this BinaryReader br)
        {
            return new BoundingBox(br.ReadVector3(), br.ReadVector3());
        }

        public static System.Drawing.Color ToSystemColor(this SharpDX.Color4 c)
        {
            return System.Drawing.Color.FromArgb((int)(c.Alpha * 255), (int)(c.Red * 255), (int)(c.Green * 255), (int)(c.Blue * 255));
        }
    }
}
