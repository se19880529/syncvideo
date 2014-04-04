using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NET
{   
    //抽象平台无关的网络收发功能
    interface ISocketManager
    {
        //void SetProtocolHandler(ProtocolHandler handler);
        //void SendProtocol(NetFriend friend, Protocol protocol);
        //void Connect(string ip);
        //List<NetFriend> GetAllNetFriend();
        //void Start();
        //void Stop();
        //侦听(返回socket）
        long Listen(ref int port);
        //接受（返回socket）
        long Accept(long socket);
        //连接（返回socket）
        long Connect(string address, int port);
        //获取socket远端信息
        void GetLocalEndPoint(out string address, out int port);
        void GetRemoteEndPoint(out string address, out int port);
        //接收（返回接受字节数）
        long Recv(long socket, byte[] data, long len);
        //发送
        long Send(long socket, byte[] data, long len);
        //关闭
        void Close(long socket);
        //查询套接字是否未关闭
        bool IsSocketClose(long socket);
        //Select
        void Select(List<long> socks, List<long> sockRead, List<long> sockSend, List<long> sockError, long time);
    }
}
