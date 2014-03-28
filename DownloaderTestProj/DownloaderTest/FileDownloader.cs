using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileDownloader
{
    class LocalFile
    {
        class Section
        {
            public long startByte;
            public long endByte;
            public int bytesExistInDisk;
        }
        public string path;
        public long size; // in byte
        public int section_size;    // in byte
        DataStructure.BitMap section_status;
        Section[] sections;
    }
}
