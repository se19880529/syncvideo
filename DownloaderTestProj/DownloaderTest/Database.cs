using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using lib;

namespace FileService
{
    class FileStreamEasy
    {
        public byte[] buffer;
        FileStream _stream;
        void _Extend(int size)
        {
            if (buffer == null || buffer.Length < size)
            {
                long newsize = size;
                buffer = new byte[newsize];
            }
        }

        void Read(int length)
        {
            _Extend(length);
            _stream.Read(buffer, 0, length);
        }

        public FileStreamEasy(FileStream fs)
        {
            _stream = fs;
        }

        public int ReadInt32()
        {
            Read(4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public void Write(int val)
        {
            _stream.Write(BitConverter.GetBytes(val), 0, sizeof(int));
        }

        public byte ReadByte()
        {
            return (byte)_stream.ReadByte();
        }

        public void Write(byte val)
        {
            _stream.Write(new byte[] { val }, 0, sizeof(byte));
        }

        public byte[] ReadByteArray(int length)
        {
            byte[] arr = new byte[length];
            _stream.Read(arr, 0, length);
            return arr;
        }

        public void Write(byte[] val)
        {
            _stream.Write(val, 0, sizeof(byte) * val.Length);
        }

        public long ReadInt64()
        {
            Read(8);
            return BitConverter.ToInt64(buffer, 0);
        }
        public void Write(long val)
        {
            _stream.Write(BitConverter.GetBytes(val), 0, sizeof(long));
        }

        public float ReadFloat()
        {
            Read(sizeof(float));
            return BitConverter.ToSingle(buffer, 0);
        }
        public void Write(float val)
        {
            _stream.Write(BitConverter.GetBytes(val), 0, sizeof(float));
        }

        public double ReadDouble()
        {
            Read(sizeof(double));
            return BitConverter.ToDouble(buffer, 0);
        }
        public void Write(double val)
        {
            _stream.Write(BitConverter.GetBytes(val), 0, sizeof(double));
        }

        public string ReadString()
        {
            int length = ReadInt32();
            Read(length);
            StringBuilder builder = new StringBuilder();
            builder.Append(buffer);
            return builder.ToString();
        }
    }

    public class FileSection
    {
        public byte[] md5;
        public long startByte;
        public long endByte;
        public int downloadedByte;
        public override string ToString()
        {
            return Utility.GetClassDesc("FileSection",new  string[]{ "startByte", "endByte", "downloadedByte", "md5"},
                                   new string[]{ startByte.ToString(), endByte.ToString(), downloadedByte.ToString(), Utility.ByteToStr(md5)}); 
        }
    }
    public class FileDescripter
    {
        public int sectionCount;
        public int sectionLength;
        public byte md5Length;
        public long fileLength;

        FileSection[] sections;

        public override string ToString()
        {
            return Utility.GetClassDesc("FileDescripter", new string[]{"sectionCount", "sectionLength", "md5Length", "fileLength", "sections"}, 
                new string[]{sectionCount.ToString(), sectionLength.ToString(), md5Length.ToString(), fileLength.ToString(), Utility.ArrayToStr(sections)});
        }


        static byte[] _md5Buffer = new byte[1024000];
        public static byte[] GetMD5(FileStream stream, long start, long end, int bitCount)
        {
            int step = _md5Buffer.Length / 2;
            System.Security.Cryptography.MD5 md5Algorithm = System.Security.Cryptography.MD5.Create("MD5");
            stream.Seek(start, SeekOrigin.Begin);
            
            long ptr = start;
            while (stream.Position <= end)
            {
                long count = Math.Min(step, end - stream.Position);
                if (count <= 0)
                    break;
                if (end - stream.Position >= step)
                {
                    stream.Read(_md5Buffer, 0, step);
                    md5Algorithm.TransformBlock(_md5Buffer, 0, step, _md5Buffer, step);
                }
                else
                {
                    stream.Read(_md5Buffer, 0, (int)(end - stream.Position + 1));
                    md5Algorithm.TransformFinalBlock(_md5Buffer, 0, step);
                    break;
                }
            }
            byte[] res = new byte[bitCount];
            for (int i = 0; i < bitCount; i++)
            {
                res[i] = md5Algorithm.Hash[i % md5Algorithm.Hash.Length];
            }
            return res;
        }

        public static FileDescripter CreateFromFile(string path, int sectLen, byte md5Len)
        {
            FileStream file = null;
            try
            {
                file = File.Open(path, FileMode.Open);
            }
            catch (System.Exception exp)
            {
                return null;
            }
            FileDescripter res = new FileDescripter();
            res.fileLength = file.Length;
            res.md5Length = md5Len;
            res.sectionLength = sectLen;
            List<FileSection> list = new List<FileSection>();
            long start = 0;
            long end = res.sectionLength - 1;
            while (start < res.fileLength)
            {
                end = Math.Min(res.fileLength - 1, start + res.sectionLength - 1);
                FileSection fs = new FileSection
                {
                    startByte = start,
                    endByte = end,
                    downloadedByte = (int)(end - start + 1),
                };
                fs.md5 = GetMD5(file, start, end, md5Len);
                list.Add(fs);
                start = end + 1;
            }
            res.sections = list.ToArray();
            res.sectionCount = list.Count();
            file.Close();
            return res;
        }

        public static FileDescripter LoadFromStream(FileStream stream)
        {
            FileDescripter desc = new FileDescripter();
            FileStreamEasy file = new FileStreamEasy(stream);
            byte[] buffer = new byte[16];
            desc.fileLength = file.ReadInt64();
            desc.sectionCount = file.ReadInt32();
            desc.sectionLength = file.ReadInt32();
            desc.md5Length = file.ReadByte();
            desc.sections = new FileSection[desc.sectionCount];
            for (int i = 0; i < desc.sectionCount; i++)
            {
                desc.sections[i] = new FileSection();
                desc.sections[i].md5 = file.ReadByteArray(desc.md5Length);
                desc.sections[i].downloadedByte = file.ReadInt32();
                desc.sections[i].startByte = i * desc.sectionLength;
                desc.sections[i].endByte = Math.Min(desc.sections[i].startByte + desc.sectionLength - 1, desc.fileLength - 1); 
            }
            return desc;
        }
        public static FileDescripter SaveToStream(FileDescripter desc, FileStream stream)
        {
            FileStreamEasy file = new FileStreamEasy(stream);
            byte[] buffer = new byte[16];
            file.Write(desc.fileLength);
            file.Write(desc.sectionCount);
            file.Write(desc.sectionLength);
            file.Write(desc.md5Length);
            for (int i = 0; i < desc.sectionCount; i++)
            {
                file.Write(desc.sections[i].md5);
                file.Write(desc.sections[i].downloadedByte);
            }
            return desc;
        }
    }

    public class SourceFile
    {
        const string tempFileExt = "tmp";
        List<FileSection> sectionDiscripter = new List<FileSection>();
        FileStream _file;
        public static SourceFile Open(string fn)
        {
            SourceFile res = new SourceFile();
            return res;
        }
    }
}
