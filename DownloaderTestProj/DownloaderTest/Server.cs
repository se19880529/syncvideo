using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataStructure;
using NET;
using lib;

namespace FileService
{
    class ServerProtocol : Protocol
    {
        public ServerProtocol(int tp)
            : base()
        {
            type = tp;
        }
    }
    class ShareInfoQuery : ServerProtocol
    {
        public const int TYPE = 1;
        public byte method;         //0:first block, 1: largest block, 2: last block
        public long startByte;
        public int queryLength;
        public ShareInfoQuery() : base(TYPE) { }
        public override void DoMarshal(ByteStream stream)
        {
            base.DoMarshal(stream);
            stream.Append(method);
            stream.Append(startByte);
            stream.Append(queryLength);
        }
        public override void DoUnmarshal(ByteStream stream)
        {
            base.DoUnmarshal(stream);
            method = stream.ReadByte();
            startByte = stream.ReadInt64();
            queryLength = stream.ReadInt();
        }
    }
    class ShareInfoRe : ServerProtocol
    {
        public const int TYPE = 2;
        public byte method;
        public long queryStart;
        public int queryLength;
        public long actureStart;
        public int actureLength;
        public ShareInfoRe() : base(TYPE) { }
        public override void DoMarshal(ByteStream stream)
        {
            base.DoMarshal(stream);
            stream.Append(method);
            stream.Append(queryStart);
            stream.Append(queryLength);
            stream.Append(actureStart);
            stream.Append(actureLength);
        }
        public override void DoUnmarshal(ByteStream stream)
        {
            base.DoUnmarshal(stream);
            method = stream.ReadByte();
            queryStart = stream.ReadInt64();
            queryLength = stream.ReadInt();
            actureStart = stream.ReadInt64();
            actureLength = stream.ReadInt() ;
        }
    }
    class DownloadRequest : ServerProtocol
    {
        public byte method;
        public long startByte;
        public int length;
        public const int TYPE = 3;
        public DownloadRequest() : base(TYPE) { }
        public override void DoMarshal(ByteStream stream)
        {
            base.DoMarshal(stream);
            stream.Append(method);
            stream.Append(startByte);
            stream.Append(length);
        }
        public override void DoUnmarshal(ByteStream stream)
        {
            base.DoUnmarshal(stream);
            method = stream.ReadByte();
            startByte = stream.ReadInt64();
            length = stream.ReadInt();
        }
    }
    class DownloadContent : ServerProtocol
    {
        public const int TYPE = 4;
        public byte flag;           //0:normal, 1: end of stream, 2:rejected, 3: error
        public long start;
        public byte[] data;
        public DownloadContent() : base(TYPE) {}
        public override void DoMarshal(ByteStream stream)
        {
            base.DoMarshal(stream);
            stream.Append(flag);
            stream.Append(start);
            stream.Append((int)data.Length);
            stream.Append(data);
        }
        public override void DoUnmarshal(ByteStream stream)
        {
            base.DoUnmarshal(stream);
            flag = stream.ReadByte();
            start = stream.ReadInt64();
            data = stream.Read(stream.ReadInt());
        }
    }
    class Server
    {
        long workerid = -1;
        ThreadPool _fileSendThreadPool = new ThreadPool();
        class FileSendContext : TaskContext
        {
            public long start;
            public int length;
            public long socket;
        }
        class FileSendWorker : ThreadTask
        {
            Server server = null;
            public FileSendWorker(Server s) { server = s; }
            public override void DoWork(TaskContext context)
            {
                FileSendContext data = context as FileSendContext;
                if (data != null)
                {
                    SourceFile file = server._file;
                    byte[] buffer = new byte[1024];              //buffer of send
                    long ptr = data.start;
                    while (data.length > 0)
                    {
                        long start, end;
                        file.GetFirstSectionBetween(false, ptr, ptr + data.length - 1, out start, out end);
                        if (start < 0 || end < 0)
                        {
                            DownloadContent dc = new DownloadContent { flag = 3 };
                            byte[] bits = dc.ToByteArray();
                            _manager.Send(data.socket, bits, bits.Length);
                            break;
                        }
                        while (start <= end)
                        {
                            long begin = start;
                            long last = Math.Min(end, start + buffer.Length - 1);
                            if (end < start)
                            {
                                //a error occured
                                DownloadContent dc = new DownloadContent();
                                dc.flag = 3;
                                byte[] bits = dc.ToByteArray();
                                _manager.Send(data.socket, bits, bits.Length);
                                break;
                            }
                            else
                            {
                                //send file data
                                long len = file.GetFileBits(start, end, buffer, 0);

                                DownloadContent dc = new DownloadContent();
                                dc.flag = 0;
                                if (last >= data.start + data.length - 1)
                                    dc.flag = 1;
                                dc.start = begin;
                                dc.data = new byte[len];
                                Array.Copy(buffer, 0, dc.data, 0, len);
                                byte[] stream = dc.ToByteArray();
                                _manager.Send(data.socket, stream, stream.Length);
                                System.Threading.Thread.Sleep(0);
                            }
                            start = last + 1;
                        }
                        data.length -= (int)(end - start + 1);
                    }
                }
                server.connectedUser--;
            }
        }
        static ISocketManager _manager;
        int port;
        SourceFile _file;
        ProtocolSet _protocolSet = new ProtocolSet();
        ByteStream _stream = new ByteStream();
        byte[] _buffer = new byte[1024];
        long listenSocket;
        int connectedUser;
        int maxUser;

        public void Start()
        {
            listenSocket = _manager.Listen(ref port);
            if (workerid < 0)
            {
                workerid = _fileSendThreadPool.AddWorker(new FileSendWorker(this), 0);
                _fileSendThreadPool.Start();
            }
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
            long len = _manager.Recv(socket, _buffer, _buffer.Length);
            _stream.Append(_buffer, 0, (int)len);
            Protocol p = _protocolSet.Decode(_stream);
            if (p != null)
            {
                Handler(p, socket);

            }
        }

        void Handler(Protocol p, long socket)
        {
            switch (p.type)
            {
                case ShareInfoQuery.TYPE:
                    {
                        ShareInfoQuery pro = p as ShareInfoQuery;
                        ShareInfoRe re = new ShareInfoRe();
                        FileDescripter des = _file.GetDescripter();
                        long start = -1, end = -1;
                        switch (pro.method)
                        {
                            case 0:
                                {
                                    _file.GetFirstSectionBetween(false, pro.startByte, pro.startByte + pro.queryLength, out start, out end);
                                }
                                break;
                            case 1:
                                {
                                    _file.GetMaxSectionBetween(false, pro.startByte, pro.startByte + pro.queryLength, out start, out end);
                                }
                                break;
                            case 2:
                                {
                                    _file.GetLastSectionBetween(false, pro.startByte, pro.startByte + pro.queryLength, out start, out end);
                                }
                                break;
                        }
                        re.method = pro.method;
                        re.queryStart = pro.startByte;
                        re.queryLength = pro.queryLength;
                        re.actureStart = start;
                        re.actureLength = (int)(end - start + 1);
                        ByteStream bs = new ByteStream();
                        byte[] buffer = re.Marshal(bs).GetBuffer();
                        _manager.Send(socket, buffer, buffer.Length);  
                    }
                    break;
                case DownloadRequest.TYPE:
                    {
                        DownloadRequest req = p as DownloadRequest;
                        FileDescripter des = _file.GetDescripter();
                        if (connectedUser >= maxUser)
                        {
                            DownloadContent dc = new DownloadContent { flag = 2 };  //max user, reject remote
                            byte[] bits = dc.ToByteArray();
                            _manager.Send(socket, bits, bits.Length);
                        }
                        else
                        {
                            connectedUser++;
                            long start = -1, end = -1;
                            switch (req.method)
                            {
                                case 0:
                                    {
                                        _file.GetFirstSectionBetween(false, req.startByte, req.startByte + req.length, out start, out end);
                                    }
                                    break;
                                case 1:
                                    {
                                        _file.GetMaxSectionBetween(false, req.startByte, req.startByte + req.length, out start, out end);
                                    }
                                    break;
                                case 2:
                                    {
                                        _file.GetLastSectionBetween(false, req.startByte, req.startByte + req.length, out start, out end);
                                    }
                                    break;
                            }
                            FileSendContext fsc = new FileSendContext();
                            fsc.socket = socket;
                            fsc.length = req.length;
                            fsc.start = start;
                            fsc.length = (int)(end - start + 1);
                            _fileSendThreadPool.AddJob(fsc, workerid);
                        }
                    }
                    break;
            }
        }
    }
}