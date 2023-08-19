using System;
using System.Collections.Generic;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace WoWShell
{
    public class ADT
    {
        public uint version;
        public MCNK[] chunks;
        public float min;
        public float max;
        public float[,] heightmap;
        public byte[,] blendMap0;
        public byte[,] blendMap1;
        public byte[,] blendMap2;

        public ADTType adtType = ADTType.Terrain;

        public enum ADTType
        {
            Terrain,
            TEX,
            OBJ,
            LOD
        }

        readonly HashSet<uint> texChunks = new HashSet<uint>()
        {
            0x4D414D50, // MCNK
            0x4D444944, // MDID
            0x4D484944, // MHID
        };

        readonly HashSet<uint> objChunks = new HashSet<uint>()
        {
            0x4D444446, // MDDF
            0x4D4F4446, // MODF
        };

        readonly HashSet<uint> lodChunks = new HashSet<uint>()
        {
            0x4D4C4844, // MLHD
            0x4D4C5648, // MLVH
            0x4D4C4C4C, // MLLL
            0x4D4C4E44, // MLND
            0x4D4C5649, // MLVI
            0x4D4C5349, // MLSI
            0x4D4C4C44, // MLLD
            0x4D4C4C4E, // MLLN
            0x4D4C4C49, // MLLI
            0x4D4C4C56, // MLLV
        };

        public ADT(BinaryReader br)
        {
            this.min = float.MaxValue;
            this.max = float.MinValue;

            int mcnkIndex = 0;
            long streamPos = 0;
            this.min = float.MaxValue;
            this.max = float.MinValue;

            this.chunks = new MCNK[256];

            while (streamPos < br.BaseStream.Length)
            {
                br.BaseStream.Position = streamPos;
                uint chunkID = br.ReadUInt32();
                int chunkSize = br.ReadInt32();

                streamPos = br.BaseStream.Position + chunkSize;

                if (texChunks.Contains(chunkID))
                    this.adtType = ADTType.TEX;

                if (objChunks.Contains(chunkID))
                    this.adtType = ADTType.OBJ;

                if (lodChunks.Contains(chunkID))
                    this.adtType = ADTType.LOD;

                if (chunkID == 0x4d434e4b)  // MCNK
                {
                    switch (adtType)
                    {
                        case ADTType.Terrain:
                            {
                                TerrainMCNK t = new TerrainMCNK(br, chunkSize);
                                if (t.min < this.min)
                                    this.min = t.min;
                                if (t.max > this.max)
                                    this.max = t.max;
                                this.chunks[mcnkIndex] = t;
                            }
                            mcnkIndex++;
                            break;
                        case ADTType.TEX:
                            {
                                TextureMCNK t = new TextureMCNK(br, chunkSize);
                                this.chunks[mcnkIndex] = t;
                            }
                            break;
                        case ADTType.OBJ:
                            break;
                        case ADTType.LOD:
                            break;
                        default:
                            break;
                    }
                }
            }

            if (adtType == ADTType.Terrain)
            {
                CalcHeightMap();
            }

            if (adtType == ADTType.TEX)
            {
                CalcBlendMap();
            }
        }


        void CalcHeightMap()
        {
            this.heightmap = new float[128, 128];

            if (this.chunks != null)
            {
                int cIndex = 0;
                for (int cx = 0; cx < 16; cx++)
                {
                    for (int cy = 0; cy < 16; cy++)
                    {
                        var chunk = this.chunks[cIndex] as TerrainMCNK;

                        if (chunk != null)
                        {
                            int currentVertex = 0;

                            for (int i = 0; i < 17; i++)
                            {
                                // Selecting squares only
                                if (i % 2 == 0)
                                {
                                    var vx = i / 2;
                                    for (int vy = 0; vy < 9; vy++)
                                    {
                                        if (vx < 8 && vy < 8)
                                        {
                                            if (chunk.heightMap != null)
                                                this.heightmap[cx * 8 + vx, cy * 8 + vy] = chunk.heightMap[currentVertex];
                                        }
                                        currentVertex++;
                                    }
                                }
                                if (i % 2 == 1)
                                {
                                    for (int j1 = 0; j1 < 8; j1++)
                                    {
                                        currentVertex++;
                                    }
                                }
                            }
                        }

                        cIndex++;
                    }
                }
            }
        }


        void CalcBlendMap()
        {
            this.blendMap0 = new byte[64, 64];
            this.blendMap1 = new byte[64, 64];
            this.blendMap2 = new byte[64, 64];

            if (this.chunks != null)
            {
                int cIndex = 0;
                for (int cx = 0; cx < 16; cx++)
                {
                    for (int cy = 0; cy < 16; cy++)
                    {
                        var chunk = this.chunks[cIndex] as TextureMCNK;

                        if (chunk != null)
                        {
                            int currentPixel = 0;

                            for (int vx = 0; vx < 64; vx++)
                            {
                                for (int vy = 0; vy < 64; vy++)
                                {
                                    if (chunk.alphaLayers != null && chunk.alphaLayers.Count >= 1)
                                        this.blendMap0[cx * 8 + vx, cy * 8 + vy] = chunk.alphaLayers[0][currentPixel];
                                    if (chunk.alphaLayers != null && chunk.alphaLayers.Count >= 2)
                                        this.blendMap1[cx * 8 + vx, cy * 8 + vy] = chunk.alphaLayers[1][currentPixel];
                                    if (chunk.alphaLayers != null && chunk.alphaLayers.Count >= 3)
                                        this.blendMap2[cx * 8 + vx, cy * 8 + vy] = chunk.alphaLayers[2][currentPixel];

                                    currentPixel++;
                                }
                            }
                        }

                        cIndex++;
                    }
                }
            }
        }

        public abstract class MCNK { }
        
        public class TerrainMCNK : MCNK
        {
            public float min;
            public float max;
            public float[] heightMap;
            public float positionX;
            public float positionY;
            public float positionZ;

            public TerrainMCNK(BinaryReader br, int mcnkSize)
            {
                long save = br.BaseStream.Position;

                br.BaseStream.Position += 104;
                this.positionX = br.ReadSingle();
                this.positionY = br.ReadSingle();
                this.positionZ = br.ReadSingle();
                br.BaseStream.Position += 12;

                this.min = float.MaxValue;
                this.max = float.MinValue;
                this.heightMap = new float[145];

                long streamPos = br.BaseStream.Position;
                while (streamPos < save + mcnkSize)
                {
                    br.BaseStream.Position = streamPos;
                    uint chunk = br.ReadUInt32();
                    int size = br.ReadInt32();

                    streamPos = br.BaseStream.Position + size;

                    if (chunk == 0x4d435654)
                    {
                        for (int i = 0; i < 145; i++)
                        {
                            float h = br.ReadSingle() + this.positionZ;

                            // Calc minmax
                            if (h < this.min)
                                this.min = h;
                            if (h > this.max)
                                this.max = h;

                            this.heightMap[i] = h;
                        }
                    }
                }
            }
        }

        public class TextureMCNK : MCNK
        {
            public int layers;
            public LayerDefinition[] layerDefs;
            public List<byte[]> alphaLayers;

            public TextureMCNK(BinaryReader br, int mcnkSize)
            {
                long save = br.BaseStream.Position;

                long streamPos = br.BaseStream.Position;
                while (streamPos < save + mcnkSize)
                {
                    br.BaseStream.Position = streamPos;
                    uint chunk = br.ReadUInt32();
                    int size = br.ReadInt32();

                    streamPos = br.BaseStream.Position + size;

                    if (chunk == 0x4d434c59)    // MCLY
                    {
                        this.layers = size / 16;
                        this.layerDefs = new LayerDefinition[this.layers];
                        for (int i = 0; i < this.layers; i++)
                        {
                            this.layerDefs[i] = new LayerDefinition(br);
                        }
                    }

                    if (chunk == 0x4d43414c)    // MCAL
                    {
                        if (this.layers > 1)
                        {
                            this.alphaLayers = new List<byte[]>();

                            var start = br.BaseStream.Position;

                            for (int l = 1; l < this.layers; l++)
                            {
                                if ((this.layerDefs[l].flags & 0x200) != 0)
                                    alphaLayers.Add(ReadAlphaMapRLE(br));
                                else
                                    alphaLayers.Add(ReadAlphaMap4096(br));
                            }

                            var end = br.BaseStream.Position;

                            // a hack because don't have access to wdt
                            if (end - start > size)
                            {
                                // redo
                                br.BaseStream.Position = start;

                                for (int l = 1; l < this.layers; l++)
                                {
                                    if ((this.layerDefs[l].flags & 0x200) != 0)
                                        alphaLayers.Add(ReadAlphaMapRLE(br));
                                    else
                                        alphaLayers.Add(ReadAlphaMap2048(br));
                                }
                            }
                        }
                    }
                }
            }

            public struct LayerDefinition
            {
                public uint textureIDIndex;
                public uint flags;
                public uint offsetInMCAL;
                public uint effectID;

                public LayerDefinition(BinaryReader br)
                {
                    this.textureIDIndex = br.ReadUInt32();
                    this.flags = br.ReadUInt32();
                    this.offsetInMCAL = br.ReadUInt32();
                    this.effectID = br.ReadUInt32();
                }
            }


            byte[] ReadAlphaMapRLE(BinaryReader br)
            {
                uint offO = 0;
                byte[] buffOut = new byte[4096];

                while (offO < 4096)
                {
                    byte buffIn = br.ReadByte();
                    bool fill = (buffIn & 0x80) != 0;
                    int count = buffIn & 0x7F;
                    if (fill)
                        buffIn = br.ReadByte();
                    for (uint k = 0; k < count; k++)
                    {
                        if (fill)
                        {
                            buffOut[offO] = buffIn;
                        }
                        else
                        {
                            buffOut[offO] = br.ReadByte();
                        }
                        offO++;

                        if (offO >= 4096)
                            break;
                    }
                }

                return buffOut;
            }

            byte[] ReadAlphaMap2048(BinaryReader br)
            {
                int index = 0;
                byte[] buffer = new byte[4096];
                for (int i = 0; i < 2048; i++)
                {
                    byte onebyte = br.ReadByte();
                    byte nibble1 = (byte)(onebyte & 0x0F);
                    byte nibble2 = (byte)((onebyte & 0xF0) >> 4);
                    int first = nibble2 * 255 / 15;
                    int second = nibble1 * 255 / 15;
                    buffer[i + index + 0] = (byte)first;
                    buffer[i + index + 1] = (byte)second;
                    index = index + 1;
                }
                return buffer;
            }

            byte[] ReadAlphaMap4096(BinaryReader br)
            {
                byte[] buffer = new byte[4096];
                br.Read(buffer, 0, 4096);
                return buffer;
            }
        }

    }
}
