using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoWShell
{
    class ArraySkip
    {
        public uint size;
        public uint offs;

        public ArraySkip(BinaryReader br)
        {
            this.size = br.ReadUInt32();
            this.offs = br.ReadUInt32();
        }
    }

    abstract class Array<T>
    {
        public uint size;
        public uint offs;

        public T[] data;

        public long ReadArrayCommon(BinaryReader br, long startOffset)
        {
            this.size = br.ReadUInt32();
            this.offs = br.ReadUInt32();

            long save = br.BaseStream.Position;

            this.data = new T[this.size];
            br.BaseStream.Position = startOffset + this.offs;
            return save;
        }
    }

    class ArrayByte : Array<byte>
    {
        public ArrayByte(BinaryReader br, long startOffset)
        {
            long save = ReadArrayCommon(br, startOffset);
            this.data = br.ReadBytes((int)this.size);
            br.BaseStream.Position = save;
        }
    }

    class ArrayUInt16 : Array<ushort>
    {
        public ArrayUInt16(BinaryReader br, long startOffset)
        {
            long save = ReadArrayCommon(br, startOffset);
            for (uint i = 0; i < this.data.Length; i++)
            {
                this.data[i] = br.ReadUInt16();
            }
            br.BaseStream.Position = save;
        }
    }

    public abstract class ArrayData
    {
        public abstract void Read(BinaryReader br, long endOffset);
        public abstract int GetDataSize();
    }

    unsafe class ArrayStruct<T> where T : ArrayData, new()
    {
        public uint size;
        public uint offs;
        public T[] data;

        public ArrayStruct(BinaryReader br, long startOffset)
        {
            this.size = br.ReadUInt32();
            this.offs = br.ReadUInt32();

            long save = br.BaseStream.Position;
            br.BaseStream.Position = startOffset + this.offs;

            long endOffset = 0;
            this.data = new T[this.size];

            for (uint i = 0; i < this.size; i++)
            {
                T rec = new T();

                if (endOffset == 0)
                {
                    endOffset = startOffset + this.offs + (this.size * rec.GetDataSize());
                }

                rec.Read(br, endOffset);
                this.data[i] = rec;
            }
            br.BaseStream.Position = save;
        }
    }
}
