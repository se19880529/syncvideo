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

        LinkedList<PeerInfo> _peerList = new LinkedList<PeerInfo>();
        public class PeerInfo
        {
            public string ip;
            public int port;
            public int refCount;
        }

        LinkedListNode<PeerInfo> _GetPeer(int order)
        {
            LinkedListNode<PeerInfo> ptr = _peerList.First;
            while (ptr == null && order > 0)
            {
                ptr = ptr.Next;
            }
            return ptr;
        }

        void _AddPeerRef(LinkedListNode<PeerInfo> info)
        {
            info.Value.refCount++;
            _ShiftDownPeer(info, true);
        }
        void _ShiftDownPeer(LinkedListNode<PeerInfo> info, bool passsame)
        {
            while (info != _peerList.Last && (info.Next.Value.refCount < info.Value.refCount || (passsame && info.Next.Value.refCount == info.Value.refCount)))
            {
                var pre = info.Next;
                _peerList.Remove(info);
                _peerList.AddAfter(pre, info);
            }
        }
        void _ReleasePeerRef(LinkedListNode<PeerInfo> info)
        {
            info.Value.refCount--;
            _ShiftUpPeer(info, true);
        }
        void _ShiftUpPeer(LinkedListNode<PeerInfo> info, bool passsame)
        {
            while (info != _peerList.First && (info.Previous.Value.refCount > info.Value.refCount || (passsame && info.Next.Value.refCount == info.Value.refCount)))
            {
                var next = info.Previous;
                _peerList.Remove(info);
                _peerList.AddBefore(next, info);
            }
        }

        public void AddPeer(string ip, int port)
        {
            foreach (var peer in _peerList)
            {
                if (peer.ip == ip && peer.port == port)
                    return;
            }
            LinkedListNode<PeerInfo> newInfo = new LinkedListNode<PeerInfo>(new PeerInfo());
            newInfo.Value.refCount = 0;
            newInfo.Value.port = port;
            newInfo.Value.ip = ip;
            _peerList.AddFirst(newInfo);
            _ShiftDownPeer(newInfo, false);
        }
        

    }
}