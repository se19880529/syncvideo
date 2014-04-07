using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib
{
    class ProtocolSet
    {
        public Protocol Decode(ByteStream stream)
        {
            stream.PushPtr();
            try
            {
                int type = stream.ReadInt();
                Protocol protocol = null;
                if (!_protocolTable.TryGetValue(type, out protocol))
                {
                    stream.RestorePtr();
                    throw new System.Exception("unknown protocol");
                }
                else
                {
                    protocol = protocol.Clone() as Protocol;
                    protocol.DoUnmarshal(stream);
                    protocol.type = type;
                    return protocol;
                }
            }
            catch (Exception e)
            {
                stream.RestorePtr();
                return null;
            }
        }

        public bool Register(Protocol p)
        {
            if (_protocolTable.ContainsKey(p.type))
            {
                return false;
            }
            else
            {
                _protocolTable.Add(p.type, p);
                return true;
            }
        }

        Dictionary<int, Protocol> _protocolTable = new Dictionary<int, Protocol>();
    }
    class Protocol
    {
        public int type;

        public Protocol Clone()
        {
            return (Protocol)MemberwiseClone();
        }

        public byte[] ToByteArray()
        {
            ByteStream bs = new ByteStream();
            bs = Marshal(bs);
            return bs.GetBuffer();
        }

        public ByteStream Marshal(ByteStream stream)
        {
            stream.Append(type);
            DoMarshal(stream);
            return stream;
        }

        public virtual void DoMarshal(ByteStream stream)
        {

        }

        public virtual void DoUnmarshal(ByteStream stream)
        {

        }
    }
}