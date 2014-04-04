using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataStructure;
using NET;

namespace FileService
{
    
    class ServerProtocol
    {
        public int type;
        public ServerProtocol(int tp) { type = tp; }
        

        public virtual byte[] GetByte()
        {
            return BitConverter.GetBytes(type);
        }

        public virtual void FromByte(byte[] buffer)
        {
            type = BitConverter.ToInt32(buffer, 0);
        }

        public void Insert(ref int index, byte[] arr, byte[] dest)
        {
            Array.Copy(dest, 0, arr, index, dest.Length);
            index += dest.Length;
        }
    }
    class ShareInfoQuery : ServerProtocol
    {
        public const int TYPE = 1;
        public ShareInfoQuery() : base(TYPE) { }
    }
    class ShareInfoRe : ServerProtocol
    {
        public const int TYPE = 2;
        public int sectionid;
        public int downloaded;
        public long start;
        public long end;
        public byte[] md5;
        public ShareInfoRe() : base(TYPE) { }
        public override byte[] GetByte()
        {
            byte[] buffer = new byte[sizeof(int) * 3 + sizeof(long) * 2 + md5.Length];
            int index = 0;
            Insert(ref index, buffer, BitConverter.GetBytes(type));
            Insert(ref index, buffer, BitConverter.GetBytes(sectionid));
            Insert(ref index, buffer, BitConverter.GetBytes(downloaded));
            Insert(ref index, buffer, BitConverter.GetBytes(start));
            Insert(ref index, buffer, BitConverter.GetBytes(end));
            Insert(ref index, buffer, md5);
            return buffer;
        }
        public override void FromByte(byte[] buffer)
        {
            int index = 0;
            type = BitConverter.ToInt32(buffer, index);
            index += 4;
            sectionid = BitConverter.ToInt32(buffer, index);
            index += 4;
            downloaded = BitConverter.ToInt32(buffer, index);
            index += 4;
            start = BitConverter.ToInt64(buffer, index);
            index += 8;
            end = BitConverter.ToInt32(buffer, index);
            index += 8;
            md5 = new byte[buffer.Length - index];
            Array.Copy(buffer, index, md5, 0, buffer.Length - index);
        }
    }
    class Server
    {
        static ISocketManager _manager;
        int port;
        SourceFile _file;
        long listenSocket;
        int connectedUser;
        int maxUser;

        public ShareInfo GetSectionInfo(int section)
        {
            ShareInfo si = new ShareInfo();
            si.sectionAsked = section;
            si.sectionDownloaded = _file.GetDescripter().sections[section].downloadedByte;
            return si;
        }

        public void Start()
        {
            listenSocket = _manager.Listen(ref port);
        }

        void SessionProc()
        {
            while (true)
            {
                long socket = _manager.Accept(listenSocket);
                ServerProc(socket);
            }
        }

        void ServerProc(long socket)
        {
            byte[] buffer = new byte[4];
            _manager.Recv(socket, buffer, 4);
            buffer = new byte[BitConverter.ToInt32(buffer, 0)];
            _manager.Recv(socket, buffer, buffer.Length);
            switch (buffer[0])
            {
             //   case 0:         //ask for Share infomation
                    
            }
        }


    }
}
