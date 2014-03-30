using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SyncVideoServer.Core
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
        public string md5;
        public long startByte;
        public long endByte;
        public int downloadedByte;
    }
    public class FileDiscripter
    {
        public long fileLength;
        public int sectionCount;
        public int sectionLength;
        FileSection[] sections;
        public static FileDiscripter LoadFromStream(FileStream stream)
        {
            
            FileDiscripter disc = new FileDiscripter();
            byte[] buffer = new byte[16];
            //disc.fileLength = stream.Read(
            return disc;
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
