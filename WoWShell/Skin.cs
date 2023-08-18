using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoWShell
{
    public class Skin
    {
        public ushort[] vertexIndices;
        public ushort[] triangleIndices;
        public M2SkinSection[] submeshes;
        public M2Batch[] batches;
        public int boneMaxCount;

        public Skin(BinaryReader br)
        {
            uint magic = br.ReadUInt32();
            this.vertexIndices = new ArrayUInt16(br, 0).data;
            this.triangleIndices = new ArrayUInt16(br, 0).data;
            ArraySkip bones = new ArraySkip(br);
            this.submeshes = new ArrayStruct<M2SkinSection>(br, 0).data;
            this.batches = new ArrayStruct<M2Batch>(br, 0).data;
            this.boneMaxCount = br.ReadInt32();
        }

        public class M2SkinSection : ArrayData
        {
            public ushort skinSectionID;
            public ushort level;
            public ushort startVertex;
            public ushort vertexCount;
            public ushort startTriangle;
            public ushort triangleCount;
            public ushort boneCount;
            public ushort boneComboIndex;
            public ushort boneInfluences;
            public ushort centerBoneIndex;
            public Vector3 centerPosition;
            public Vector3 sortCenterPosition;
            public float sortRadius;

            public override void Read(BinaryReader br, long endOffset)
            {
                this.skinSectionID = br.ReadUInt16();
                this.level = br.ReadUInt16();
                this.startVertex = br.ReadUInt16();
                this.vertexCount = br.ReadUInt16();
                this.startTriangle = br.ReadUInt16();
                this.triangleCount = br.ReadUInt16();
                this.boneCount = br.ReadUInt16();
                this.boneComboIndex = br.ReadUInt16();
                this.boneInfluences = br.ReadUInt16();
                this.centerBoneIndex = br.ReadUInt16();
                this.centerPosition = br.ReadVector3();
                this.sortCenterPosition = br.ReadVector3();
                this.sortRadius = br.ReadSingle();
            }

            public override int GetDataSize()
            {
                return 0x30;
            }
        }

        public class M2Batch : ArrayData
        {
            public byte flags;
            public sbyte priorityPlane;
            public ushort shaderID;
            public short skinSectionIndex;
            public short geosetIndex;
            public short colorIndex;
            public short materialIndex;
            public ushort materialLayer;
            public short textureCount;
            public short textureComboIndex;
            public short textureCoordComboIndex;
            public short textureWeightComboIndex;
            public short textureTransformComboIndex;

            public override void Read(BinaryReader br, long endOffset)
            {
                this.flags = br.ReadByte();
                this.priorityPlane = br.ReadSByte();
                this.shaderID = br.ReadUInt16();
                this.skinSectionIndex = br.ReadInt16();
                this.geosetIndex = br.ReadInt16();
                this.colorIndex = br.ReadInt16();
                this.materialIndex = br.ReadInt16();
                this.materialLayer = br.ReadUInt16();
                this.textureCount = br.ReadInt16();
                this.textureComboIndex = br.ReadInt16();
                this.textureCoordComboIndex = br.ReadInt16();
                this.textureWeightComboIndex = br.ReadInt16();
                this.textureTransformComboIndex = br.ReadInt16();
            }

            public override int GetDataSize()
            {
                return 0x18;
            }
        }
    }
}
