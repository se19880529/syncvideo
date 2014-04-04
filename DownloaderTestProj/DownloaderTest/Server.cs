using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataStructure;
using NET;

namespace FileService
{
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
                case 0:         //ask for Share infomation
                    
            }
        }
    }
}
