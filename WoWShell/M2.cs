using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static WoWShell.M2;

namespace WoWShell
{
    public class M2
    {
        public MD20 md20;

        public M2(BinaryReader br)
        {
            long streamPos = 0;

            while (streamPos < br.BaseStream.Length)
            {
                br.BaseStream.Position = streamPos;
                var chunk = br.ReadUInt32();
                uint size = br.ReadUInt32();

                streamPos = br.BaseStream.Position + size;

                switch (chunk)
                {
                    case 0x3132444D: this.md20 = new MD20(br); break;
                    case 0x44494653:    // SFID
                    case 0x44495854:    // TXID
                    case 0x44494B53:    // SKID
                    case 0x44494641:    // AFID
                    case 0x44494642:    // BFID
                    case 0x32505845:    // EXP2
                    case 0x43415854:    // TXAC
                    case 0x44494650:    // PFID
                    case 0x43444650:    // PFDC
                    case 0x31444750:    // PGD1
                    case 0x46474445:    // EDGF
                    case 0x3156444C:    // LDV1
                    default: br.BaseStream.Position += size; break;
                }
            }
        }

        public class MD20
        {
            public int version;
            public string name;
            public uint globalFlags;
            public M2.Vertex[] vertices;
            public uint lodCount;
            public BoundingBox boundingBox;
            public float boundingSphereRadius;
            public BoundingBox collisionBoundingBox;
            public float collisionSphereRadius;

            public MD20(BinaryReader br)
            {
                long sp = br.BaseStream.Position;
                int MD20 = br.ReadInt32();        
                this.version = br.ReadInt32();
                this.name = Encoding.ASCII.GetString(new ArrayByte(br, sp).data);
                this.globalFlags = br.ReadUInt32();
                ArraySkip global_loops = new ArraySkip(br);
                ArraySkip sequences = new ArraySkip(br);
                ArraySkip sequenceIdxHashById = new ArraySkip(br);
                ArraySkip bones = new ArraySkip(br);
                ArraySkip boneIndicesById = new ArraySkip(br);
                this.vertices = new ArrayStruct<M2.Vertex>(br, sp).data;
                this.lodCount = br.ReadUInt32();                       // Views (LOD) are now in .skins.
                ArraySkip colors = new ArraySkip(br);                              // Color and alpha animations definitions.
                ArraySkip textures = new ArraySkip(br);
                ArraySkip texture_weights = new ArraySkip(br);                     // Transparency of textures.
                ArraySkip texture_transforms = new ArraySkip(br);
                ArraySkip textureIndicesById = new ArraySkip(br);                  // (alt. name: replacable_texture_lookup)
                ArraySkip materials = new ArraySkip(br);                           // Blending modes / render flags.
                ArraySkip boneCombos = new ArraySkip(br);                          // (alt. name: bone_lookup_table)
                ArraySkip textureCombos = new ArraySkip(br);                       // (alt. name: texture_lookup_table)
                ArraySkip textureTransformBoneMap = new ArraySkip(br);             // (alt. name: tex_unit_lookup_table)
                ArraySkip textureWeightCombos = new ArraySkip(br);                 // (alt. name: transparency_lookup_table)
                ArraySkip textureTransformCombos = new ArraySkip(br);              // (alt. name: texture_transforms_lookup_table)
                this.boundingBox = br.ReadBoundingBox();                     // min/max( [1].z, 2.0277779f ) - 0.16f seems to be the maximum camera height
                this.boundingSphereRadius = br.ReadSingle();                 // detail doodad draw dist = clamp (bounding_sphere_radius * detailDoodadDensityFade * detailDoodadDist, …)
                this.collisionBoundingBox = br.ReadBoundingBox();
                this.collisionSphereRadius = br.ReadSingle();
                ArraySkip collisionIndices = new ArraySkip(br);                    // (alt. name: collision_triangles)
                ArraySkip collisionPositions = new ArraySkip(br);                  // (alt. name: collision_vertices)
                ArraySkip collisionFaceNormals = new ArraySkip(br);                // (alt. name: collision_normals) 
                ArraySkip attachments = new ArraySkip(br);                         // position of equipped weapons or effects
                ArraySkip attachmentIndicesById = new ArraySkip(br);               // (alt. name: attachment_lookup_table)
                ArraySkip events = new ArraySkip(br);                              // Used for playing sounds when dying and a lot else.
                ArraySkip lights = new ArraySkip(br);                              // Lights are mainly used in loginscreens but in wands and some doodads too.
                ArraySkip cameras = new ArraySkip(br);                             // The cameras are present in most models for having a model in the character tab. 
                ArraySkip cameraIndicesById = new ArraySkip(br);                   // (alt. name: camera_lookup_table)
                ArraySkip ribbon_emitters = new ArraySkip(br);                     // Things swirling around. See the CoT-entrance for light-trails.
                ArraySkip particle_emitters = new ArraySkip(br);
                ArraySkip textureCombinerCombos = new ArraySkip(br);
            }
        }

        public class Vertex : ArrayData
        {
            public Vector3 position;
            public byte[] boneWeights;
            public byte[] boneIndices;
            public Vector3 normal;
            public Vector2 uv0;
            public Vector2 uv1;

            public override void Read(BinaryReader br, long endOffset)
            {
                this.position = br.ReadVector3();
                this.boneWeights = new byte[] { br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte() };
                this.boneIndices = new byte[] { br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte() };
                this.normal = br.ReadVector3();
                this.uv0 = br.ReadVector2();
                this.uv1 = br.ReadVector2();
            }

            public override int GetDataSize()
            {
                return 48;
            }
        }
    }
}
