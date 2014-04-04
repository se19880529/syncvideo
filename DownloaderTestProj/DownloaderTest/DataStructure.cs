using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataStructure
{
    public enum ControlType
    {
        OpenNew,
        Stop,
        Pause,
        Play,
    }
    public class BitMap
    {
        int[] _buffer;
        long _sizeBit;
        byte _default;
        public BitMap(byte def)
        {
            byte mask = 1;
            _default = 0;
            for (int len = 0; len < sizeof(int); len++, mask <<= 1)
                _default |= mask;
        }
        void GetBitPos(long bit, out int byteindex, out int bitindex)
        {
            byteindex = (int)(bit / sizeof(int));
            bitindex = (int)(bit % sizeof(int));
        }
        public byte GetBit(long bit, byte defaultValue)
        {
            if (_sizeBit <= bit || _buffer == null)
                return defaultValue;
            int byteindex, bitindex;
            GetBitPos(bit, out byteindex, out bitindex);
            if (byteindex >= _buffer.Length)
                return defaultValue;
            return (byte)((byte)(_buffer[byteindex] >> bitindex) & (byte)1);
        }
        public void SetBit(long bit, bool reset)
        {
            SetBit(bit, bit, reset);
        }
        public void SetBit(long bitstart,long bitend, bool reset)
        {
            int byteindex, bitindex;
            GetBitPos(bitend, out byteindex, out bitindex);
            if (_buffer == null || _buffer.Length <= byteindex)
            {
                int oldlength = (_buffer != null)?_buffer.Length:0;
                int[] buffer = new int[byteindex + 1];
                for (int i = 0; i < oldlength; i++)
                {
                    buffer[i] = _buffer[i];
                }
                for (int i = oldlength; i <= byteindex; i++)
                {
                    buffer[i] = _default;
                }
                _buffer = buffer;
            }
            if ((reset && _default != 0) || (!reset && _default == 0))
            {
                for (long i = bitend - bitstart; i >= 0; i--)
                {
                    long temp = bitend - i;
                    int byteindex2, bitindex2;
                    GetBitPos(temp, out byteindex2, out bitindex2);
                    if ((temp % sizeof(int)) == 0 && i + 1 >= sizeof(int))
                    {
                        _buffer[byteindex2] = reset ? 0 : int.MaxValue;
                        i -= sizeof(int);
                    }
                    else
                    {
                        if (reset)
                        {
                            _buffer[byteindex2] &= ~(1 << bitindex2);
                        }
                        else
                        {
                            _buffer[byteindex2] |= (1 << bitindex2);
                        }
                    }
                }
            }
            _sizeBit = Math.Max(_sizeBit, bitend);
        }
    }
    public class ISerializableData
    {
        public void Serialize(){}
        public void UnSerialize(){}
    }

    public class UserInfo : ISerializableData
    {
        public string ip;
        public int port;
        public string name;
        public string detail;
        public bool isShareMovie;
    }

    public class ShareInfo :ISerializableData
    {
        public bool isShowMovie;
        public int maxConnection;
        public int currentConnection;
        public int sectionAsked;
        public int sectionDownloaded;
    }

    public class MovieInfo : ISerializableData
    {
        public string title;
        public string md5;
        public long length;     //in milliseconds
    }

    public class ChatData : ISerializableData
    {
        public string toip;
        public string content;
    }

    public class ControlData : ISerializableData
    {
        public ControlType type;
        public long para1;
        public long para2;
        public string reason;
    }
}
