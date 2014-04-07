using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataStructure;
using NET;
using lib;

namespace FileService
{
    class Client
    {
        static ISocketManager _manager;

        SourceFile _file;
        class SectionInfo
        {
            public class SectionHolderData
            {
                public string ip;
                public int port;
                public int downloadStart;
                public int downloadEnd;
            }
            public int sectionId;
            public List<SectionHolderData> datas = new List<SectionHolderData>();
            public bool PriorThan(SectionInfo info)
            {
                return datas.Count > info.datas.Count;
            }
        }
        List<SectionInfo> _sectionToDownload = new List<SectionInfo>();
        void PutPeer(string ip, int port, ShareInfoRe info)
        {
            SectionInfo si = null;
            FileSection fs = _file.GetDescripter().sections[info.sectionid];
            foreach(SectionInfo sec in _sectionToDownload)
            {
                if(sec.sectionId == info.sectionid)
                {
                    si = sec;
                    break;
                }
            }
            if(si != null)
                _sectionToDownload.Remove(si);
            else
            {
                si = new SectionInfo();
                si.sectionId = info.sectionid;
            }
            SectionInfo.SectionHolderData data = null;
            foreach (SectionInfo.SectionHolderData d in si.datas)
            {
                if (d.ip == ip)
                {
                    data = d;
                    break;
                }
            }
            if (data != null)
                si.datas.Remove(data);
            else if(info.downloaded > fs.downloadedByte)
            {
                data = new SectionInfo.SectionHolderData();
                data.ip = ip;
            }
            if (info.downloaded > fs.downloadedByte)
            {
                data.port = port;
                data.downloadStart = 0;
                data.downloadEnd = (int)(info.downloaded - 1);
                si.datas.Add(data);
            }
            Insert(si);
        }
        void RefreshPeerList()
        {
            foreach (SectionInfo si in _sectionToDownload)
            {
                FileSection fs = _file.GetDescripter().sections[si.sectionId];
                for (int i = si.datas.Count - 1; i >= 0; i--)
                {
                    if (si.datas[i].downloadEnd - si.datas[i].downloadStart + 1 < fs.downloadedByte)
                    {
                        si.datas.RemoveAt(i);
                    }
                }
            }
        }
        void Insert(SectionInfo info)
        {
            RefreshPeerList();
            int i = 0;
            for (i = 0; i < _sectionToDownload.Count && !info.PriorThan(_sectionToDownload[i]); i++);
            _sectionToDownload.Insert(i, info);
        }
        void PickNextPeer()
        {
            if (_sectionToDownload.Count > 0)
            {
                SectionInfo si = _sectionToDownload[0];
                
            }
        }
        void DownloadProc(string ip, int port)
        {
            long socket = _manager.Connect(ip, port);
            
        }
    }
}