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

    /*
     *  思路：FileSection存储文件的一个区域，FileSectionContainer存储一系列区域，主要功能是将现有区域和给定区域融合
     *  FileDescripter是文件的描述，保存了文件的实际下载区域和等待下载的区域。主要功能是增加等待下载区域，改变等待下载区域（将oldstart-oldend之间的区域替换成start-end的区域），融合已下载区域 该结构可以保存至文件，下次打开继续下载
     *  SourceFile表示文件源，可以是正在下载的文件，空文件和完整的视频文件。外界首先调用区域获取函数获取一个正在下载区段中的合适的空区域，然后ReserveSection来将其保存到下载中区段。然后开始下载
     *  下载完成以后，调用CommitSection，给一个小于之前给定区段的区域（这是必然的，下载不可能多下，只可能少下），CommitSection的同时就释放了Reserve时的区域与传入区域作差的区域，这些区域之后就成了下载中区段里的空区域
     * 
     */
    public class FileSection
    {
        public long startByte;
        public long endByte;
        public override string ToString()
        {
            return Utility.GetClassDesc("FileSection",new  string[]{ "startByte", "endByte",},
                                   new string[]{ startByte.ToString(), endByte.ToString(),}); 
        }
    }
    public class FileSectionContainer
    {
        public const int CompareMethodLarge = 1;
        public const int CompareMethodLess = 0;
        public const int SelectMethodStartPosition = 1;
        public const int SelectMethodEndPosition = 0;
        FileSection[] sections = new FileSection[0];                     //按递增排列

        public override string ToString()
        {
            return Utility.GetClassDesc("FileSectionContainer", new string[] { "sections", },
                                   new string[]{Utility.ArrayToStr(sections)});
        }

        public FileSectionContainer Assign(FileSectionContainer other)
        {
            FileSectionContainer res = new FileSectionContainer();
            if (other.sections == null)
                return res;
            res.sections = new FileSection[other.sections.Length];
            for (int i = 0; i < other.sections.Length; i++)
            {
                res.sections[i] = new FileSection { startByte = other.sections[i].startByte, endByte = other.sections[i].endByte };
            }
            return res;
        }

        public static FileSectionContainer LoadFromStream(FileStream stream)
        {
            FileStreamEasy easy = new FileStreamEasy(stream);
            FileSectionContainer res = new FileSectionContainer();
            res.sections = new FileSection[easy.ReadInt64()];
            for (int i = 0; i < res.sections.Length; i++)
            {
                res.sections[i] = new FileSection();
                res.sections[i].startByte = easy.ReadInt64();
                res.sections[i].endByte = easy.ReadInt64();
            }
            return res;
        }

        public void SaveToStream(FileStream stream)
        {
            FileStreamEasy easy = new FileStreamEasy(stream);
            if (sections == null)
                sections = new FileSection[0];
            easy.Write((long)sections.Length);
            for (int i = 0; i < sections.Length; i++)
            {
                easy.Write(sections[i].startByte);
                easy.Write(sections[i].endByte);
            }
            stream.Flush();
        }

        //将start和end融合进container中
        public void MergeSection(long start, long end)
        {
            long idStart = _GetBorderSection(start, CompareMethodLarge, SelectMethodEndPosition);
            long idEnd = _GetBorderSection(end, CompareMethodLess, SelectMethodStartPosition);
            if (idStart < 0 && idEnd < 0)               //this means we have no element in sections
            {
                sections = new FileSection[] { new FileSection { endByte = end, startByte = start } };
            }
            else if (idStart < 0 && idEnd >= 0)          //this means section to merge is in whole right
            {
                FileSection[] newSections = new FileSection[sections.Length + 1];
                Array.Copy(sections, 0, newSections, 0, sections.Length);
                newSections[sections.Length] = new FileSection { startByte = start, endByte = end };
            }
            else if (idStart >= 0 && idEnd < 0)      //this means section to merge is in whole left
            {
                FileSection[] newSections = new FileSection[sections.Length + 1];
                Array.Copy(sections, 0, newSections, 1, sections.Length);
                newSections[0] = new FileSection { startByte = start, endByte = end };
            }
            else// this means section to merge is in middle of whole
            {
                FileSection[] newSections = new FileSection[sections.Length - (idEnd - idStart)];
                Array.Copy(sections, 0, newSections, 0, idStart);
                Array.Copy(sections, idEnd + 1, newSections, idStart + 1, sections.Length - idEnd - 1);
                newSections[idStart] = new FileSection { startByte = Math.Min(sections[idStart].startByte, start), endByte = Math.Max(sections[idEnd].endByte, end) };
                sections = newSections;
            }
        }

        public void DelSection(long start, long end)
        {
            long idStart = _GetBorderSection(start, CompareMethodLarge, SelectMethodEndPosition);
            if (idStart >= 0 && sections[idStart].startByte <= start && sections[idStart].endByte >= end)
            {
                if (sections[idStart].startByte == start && sections[idStart].endByte > end)
                {
                    sections[idStart].startByte = end + 1;
                }
                else if (sections[idStart].endByte == end && sections[idStart].startByte < start)
                {
                    sections[idStart].endByte = start - 1;
                }
                else if(sections[idStart].endByte == end && sections[idStart].startByte == start)
                {
                    FileSection[] newSection = new FileSection[sections.Length - 1];
                    Array.Copy(sections, 0, newSection, 0, idStart);
                    Array.Copy(sections, idStart, newSection, idStart + 1, sections.Length - 1 - idStart);
                    sections = newSection;
                }
                else if (sections[idStart].endByte > end && sections[idStart].startByte < start)
                {
                    FileSection[] newSection = new FileSection[sections.Length + 1];
                    Array.Copy(sections, 0, newSection, 0, idStart);
                    Array.Copy(sections, idStart + 2, newSection, idStart + 1, sections.Length - 1 - idStart);
                    newSection[idStart] = new FileSection { startByte = sections[idStart].startByte, endByte = start - 1 };
                    newSection[idStart + 1] = new FileSection { startByte = end + 1, endByte = sections[idStart].endByte };
                    sections = newSection;
                }
            }
        }

        public long GetSectionCount()
        {
            return sections.Length;
        }
        public bool GetSection(long index, out long start, out long end)
        {
            start = -1;
            end = -1;
            if (index < 0 || index >= sections.Length)
                return false;
            else
            {
                start = sections[index].startByte;
                end = sections[index].endByte;
                return true;
            }
        }
        public bool GetLargestSpaceBetween(long start, long end, out long outstart, out long outend)
        {
            outstart = -2;
            outend = -1;
            long idStart = _GetBorderSection(start, CompareMethodLess, SelectMethodStartPosition);
            long idEnd = _GetBorderSection(end, CompareMethodLarge, SelectMethodEndPosition);
            if (idEnd < 0 || idStart < 0)
                return false;
            for (long i = idStart; i < idEnd; i++)
            {
                long min = Math.Max(start, sections[i].endByte);
                long max = Math.Min(end, sections[i].endByte);
                if (max - min + 1 > outend - outstart + 1)
                {
                    outstart = min;
                    outend = max;
                }
            }
            return true;
        }
        public bool GetLargestSectionBetween(long start, long end, out long outstart, out long outend)
        {
            outstart = -2;
            outend = -1;
            long idStart = _GetBorderSection(start, CompareMethodLarge, SelectMethodEndPosition);
            long idEnd = _GetBorderSection(end, CompareMethodLess, SelectMethodStartPosition);
            if (idEnd < 0 || idStart < 0)
                return false;
            for (long i = idStart; i <= idEnd; i++)
            {
                long min = Math.Max(start, sections[i].startByte);
                long max = Math.Min(end, sections[i].endByte);
                if (max - min + 1 > outend - outstart + 1)
                {
                    outstart = min;
                    outend = max;
                }
            }
            return true;
        }
        public bool GetFirstSectionBetween(long start, long end, out long outstart, out long outend)
        {
            outstart = -2;
            outend = -1;
            long idStart = _GetBorderSection(start, CompareMethodLarge, SelectMethodEndPosition);
            long idEnd = _GetBorderSection(end, CompareMethodLess, SelectMethodStartPosition);
            if (idEnd < 0 || idStart < 0)
                return false;
            outstart = Math.Max(start, sections[idStart].startByte);
            outend = Math.Min(end, sections[idStart].endByte);
            return true;
        }
        public bool GetLastSectionBetween(long start, long end, out long outstart, out long outend)
        {
            outstart = -2;
            outend = -1;
            long idStart = _GetBorderSection(start, CompareMethodLarge, SelectMethodEndPosition);
            long idEnd = _GetBorderSection(end, CompareMethodLess, SelectMethodStartPosition);
            if (idEnd < 0 || idStart < 0)
                return false;
            outstart = Math.Max(start, sections[idEnd].startByte);
            outend = Math.Min(end, sections[idEnd].endByte);
            return true;
        }
        //获取边界section的函数(内部函数)
        //CompareMethod: CompareMethodLess: 最后一个小于等于pos， CompareMethodLarge: 第一个大于等于pos
        //SelectMethod: SelectMethodStartPosition: pos和section的start相比, SelectMethodEndPosition: pos和section的end相比
        //返回：-1 未找到
        long _GetBorderSection(long pos, int CompareMethod, int SelectMethod)
        {
            if (sections.Length == 0)
                return -1;
            long l = -1, h = GetSectionCount() - 1;
            if (CompareMethod == CompareMethodLarge)
            {
                l = 0;
                h = GetSectionCount();
            }
            long m = 0;
            long v = 0;
            while (h > l)
            {
                switch (CompareMethod)
                {
                    case CompareMethodLess:
                        m = (l + h + 1) / 2;
                        break;
                    default:
                        m = (l + h) / 2;
                        break;
                }
                switch (SelectMethod)
                {
                    case SelectMethodEndPosition:
                        v = sections[m].endByte;
                        break;
                    default:
                        v = sections[m].startByte;
                        break;
                }
                switch (CompareMethod)
                {
                    case CompareMethodLess:
                        if (pos < v)
                        {
                            h = m - 1;
                        }
                        else
                        {
                            l = m;
                        }
                        break;
                    default:
                        if (pos <= v)
                        {
                            h = m;
                        }
                        else
                        {
                            l = m + 1;
                        }
                        break;
                }
            }
            switch (CompareMethod)
            {
                case CompareMethodLess:
                    m = h;
                    break;
                default:
                    m = l;
                    break;
            }
            if (m == sections.Length || m == -1)
                return -1;
            return m;
        }
    }

    public class FileDescripter
    {
        public long fileLength;

        FileSectionContainer actualSections;
        FileSectionContainer expectedSections;

        public override string ToString()
        {
            return Utility.GetClassDesc("FileDescripter", new string[] { "fileLength", "actualSections", "expectedSections" },
                new string[] {  fileLength.ToString(), actualSections.ToString(), expectedSections.ToString() });
        }


        public void GetLastSectionBetween(bool isExpected, long start, long end, out long actureStart, out long actureEnd)
        {
            if (isExpected)
                expectedSections.GetLastSectionBetween(start, end, out actureStart, out actureEnd);
            else
                actualSections.GetLastSectionBetween(start, end, out actureStart, out actureEnd);

        }

        public void GetFirstSectionBetween(bool isExpected, long start, long end, out long actureStart, out long actureEnd)
        {
            if (isExpected)
                expectedSections.GetFirstSectionBetween(start, end, out actureStart, out actureEnd);
            else
                actualSections.GetFirstSectionBetween(start, end, out actureStart, out actureEnd);
        }

        public void GetMaxSectionBetween(bool isExpected, long start, long end, out long actureStart, out long actureEnd)
        {
            if (isExpected)
                expectedSections.GetLargestSectionBetween(start, end, out actureStart, out actureEnd);
            else
                actualSections.GetLargestSectionBetween(start, end, out actureStart, out actureEnd);
        }

        public void ExpandExpectedSection(long start, long end)
        {
            expectedSections.MergeSection(start, end);
        }

        public void ModifyExpectedSection(long oldstart, long oldend, long newstart, long newend)
        {
            expectedSections.DelSection(oldstart, oldend);
            expectedSections.MergeSection(newstart, newend);
        }

        public void MergeSection(long start, long end)
        {
            actualSections.MergeSection(start, end);
        }

        public static FileDescripter CreateFromFile(string path)
        {
            FileStream file = null;
            try
            {
                file = File.Open(path, FileMode.Open, FileAccess.Read);
            }
            catch (System.Exception exp)
            {
                return null;
            }
            FileDescripter res = new FileDescripter();
            res.fileLength = file.Length;
            res.actualSections = new FileSectionContainer();
            res.actualSections.MergeSection(0, file.Length - 1);
            res.expectedSections = new FileSectionContainer();
            res.expectedSections.Assign(res.actualSections);
            file.Close();
            return res;
        }

        public static FileDescripter LoadFromStream(FileStream stream)
        {
            FileDescripter desc = new FileDescripter();
            FileStreamEasy file = new FileStreamEasy(stream);
            byte[] buffer = new byte[16];
            desc.fileLength = file.ReadInt64();
            desc.actualSections = FileSectionContainer.LoadFromStream(stream);
            desc.expectedSections = new FileSectionContainer();
            desc.expectedSections.Assign(desc.actualSections);
            return desc;
        }
        public static FileDescripter SaveToStream(FileDescripter desc, FileStream stream)
        {
            FileStreamEasy file = new FileStreamEasy(stream);
            byte[] buffer = new byte[16];
            file.Write(desc.fileLength);
            desc.actualSections.SaveToStream(stream);
            return desc;
        }
    }

    public class SourceFile
    {
        const string tempFileExt = "tmp";
        const string configFileExt = "cfg";
        FileDescripter _config;
        FileStream _file;
        List<FileSection> _commitList = new List<FileSection>();            //start 从小到大

        public FileDescripter GetDescripter()
        {
            return _config;
        }

        public void GetLastSectionBetween(bool isExpected, long start, long end, out long actureStart, out long actureEnd)
        {
            _config.GetLastSectionBetween(isExpected, start, end, out actureStart, out actureEnd);
        }

        public void GetFirstSectionBetween(bool isExpected, long start, long end, out long actureStart, out long actureEnd)
        {
            _config.GetFirstSectionBetween(isExpected, start, end, out actureStart, out actureEnd);
        }

        public void GetMaxSectionBetween(bool isExpected, long start, long end, out long actureStart, out long actureEnd)
        {
            _config.GetMaxSectionBetween(isExpected, start, end, out actureStart, out actureEnd);
        }

        public bool ReserveSection(long start, long end)
        {
            int l = -1, h = _commitList.Count - 1;
            while (h > l)
            {
                int m = (l + h + 1) / 2;
                if (_commitList[m].endByte > start)
                    h = m - 1;
                else
                    l = m;
            }
            if (h + 1 < _commitList.Count && _commitList[h + 1].startByte <= end )
            {
                return false;
            }
            if (h >= 0 && _commitList[h].endByte == start)
                return false;
            _commitList.Insert(h + 1, new FileSection { endByte = end, startByte = start });

            _config.ExpandExpectedSection(start, end);
            return true;
        }

        public void CommitSection(long start, long end)
        {
            int l = -1, h = _commitList.Count - 1;
            while (h > l)
            {
                int m = (l + h + 1) / 2;
                if (_commitList[m].startByte > start)
                    h = m - 1;
                else
                    l = m;
            }
            if (_commitList.Count >= l && h >= 0 && _commitList[h].endByte >= end)
            {
                FileSection fs = _commitList[l];
                _commitList.RemoveAt(l);
                _config.ModifyExpectedSection(fs.startByte, fs.endByte, start, end);
            }
        }

        public long GetFileBits(long start, long end, byte[] buffer, int offset)
        {
            if (start < 0)
                start = 0;
            end = (buffer.Length - offset < end - start + 1) ? (buffer.Length - offset + start - 1) : end;
            end = _file.Length <= end?_file.Length - 1:end;
            if (end < start)
                return 0;
            _file.Seek(start, SeekOrigin.Begin);
            _file.Read(buffer, offset, (int)(end - start + 1));
            return end - start + 1;
        }

        public static SourceFile OpenExist(string fn)
        {
            SourceFile res = new SourceFile();
            var _file = File.Open(fn, FileMode.Open, FileAccess.Read);
            res._config = FileDescripter.CreateFromFile(fn);
            return res;
        }

        public static SourceFile OpenDownloading(string fn)
        {
            string configFile = "";
            string tmpFile = "";
            string ext = Utility.GetFileExt(fn);
            if (ext.Equals(configFileExt, StringComparison.CurrentCultureIgnoreCase))
            {
                configFile = fn;
            }
            else if (ext == "")
            {
                configFile = fn + "." + configFileExt;
            }
            else if (ext.Equals(tempFileExt, StringComparison.CurrentCultureIgnoreCase))
            {
                configFile = fn.Substring(0, fn.LastIndexOf(".")) + "." + configFileExt;
            }
            else
            {
                configFile = fn;
            }

            if (!File.Exists(configFile))
                return null;
            tmpFile = configFile.Substring(0, configFile.LastIndexOf(".")) + "." + tempFileExt;
            FileStream file = File.Open(configFile, FileMode.Open, FileAccess.Read);
            SourceFile res = new SourceFile();
            res._config = FileDescripter.LoadFromStream(file);
            res._file = File.Open(tmpFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            return res;
        }

        public static SourceFile CreateEmptyFromDescripter(FileDescripter desc, string path)
        {
            SourceFile res = new SourceFile();
            res._config = desc;
            res._file = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            res._file.SetLength(desc.fileLength);
            return res;
        }
    }
}
