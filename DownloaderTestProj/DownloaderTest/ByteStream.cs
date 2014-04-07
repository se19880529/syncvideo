using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib
{
    class ByteStream
    {
        public ByteStream() { ptr = 0; ptrLast = 0; }

        public void PushPtr()
        {
            ptrLast = ptr;
        }

        public void RestorePtr()
        {
            ptr = ptrLast;
            ClearLastPtr();
        }

        public void ClearLastPtr()
        {
            ptrLast = 0;
        }

        public void TrySaveSomeMemory()
        {
            if (ptr > 1024 || (_currentSize > 512 && ptr > _currentSize / 2))
            {
                _currentSize = _currentSize - ptr;
                byte[] newBufer = new Byte[_currentSize];
                for (int i = 0; i < _currentSize; i++)
                {
                    newBufer[i] = _buffer[ptr + i];
                }
                ptr = 0;
                ClearLastPtr();
                _buffer = newBufer;
            }
        }

        private void _EnsureSize(int size)
        {
            if (_buffer.Length < size)
            {
                Byte[] newbuffer = new Byte[size * 2];
                for (int i = 0; i < _buffer.Length; i++)
                {
                    newbuffer[i] = _buffer[i];
                }
                _buffer = newbuffer;
            }
        }

        public void Append(Byte[] data)
        {
            _EnsureSize(_currentSize + data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                _buffer[_currentSize + i] = data[i];
            }
            _currentSize += data.Length;
        }

        public void Append(byte[] data, int start, int len)
        {
            _EnsureSize(_currentSize + len);
            for (int i = 0; i < len; i++)
            {
                _buffer[_currentSize + i] = data[start + i];
            }
            _currentSize += len;
        }

        public Byte[] Read(int len)
        {
            if (_currentSize - ptr < len)
                throw new System.Exception("error");
            Byte[] res = new Byte[len];
            for (int i = 0; i < len; i++)
            {
                res[i] = _buffer[ptr + i];
            }
            ptr += len;
            return res;
        }

        public void Append(int value)
        {
            Append(BitConverter.GetBytes(value));
        }

        public int ReadInt()
        {
            if (_currentSize - ptr < 4)
                throw new System.Exception("stream error");
            int res = BitConverter.ToInt32(_buffer, ptr);
            ptr += 4;
            return res;
        }

        public void Append(long value)
        {
            Append(BitConverter.GetBytes(value));
        }

        public long ReadInt64()
        {
            if (_currentSize - ptr < 8)
                throw new System.Exception("stream error");
            long res = BitConverter.ToInt64(_buffer, ptr);
            ptr += 8;
            return res;
        }

        public void Append(byte value)
        {
            Append(BitConverter.GetBytes(value));
        }

        public byte ReadByte()
        {
            if (_currentSize - ptr < 1)
                throw new System.Exception("stream error");
            byte res = _buffer[ptr];
            ptr += 1;
            return res;
        }

        public void Append(double value)
        {
            Append(BitConverter.GetBytes(value));
        }

        public double ReadDouble()
        {
            if (_currentSize - ptr < 8)
                throw new System.Exception("stream error");
            double res = BitConverter.ToDouble(_buffer, ptr);
            ptr += 8;
            return res;
        }

        public void Append(bool value)
        {
            Append(BitConverter.GetBytes(value));
        }

        public bool ReadBool()
        {
            if (_currentSize - ptr < 1)
                throw new System.Exception("stream error");
            bool res = BitConverter.ToBoolean(_buffer, ptr);
            ptr += 1;
            return res;
        }

        public void Append(string value)
        {
            byte[] data = Encoding.UTF8.GetBytes(value);
            Append((int)data.Length);
            Append(data);
        }

        public string ReadString()
        {
            int len = ReadInt();
            if (len == 0)
                return "";
            if (_currentSize - ptr < len)
                throw new System.Exception("string stream is wrong");
            string res = Encoding.UTF8.GetString(_buffer, ptr, len);// BitConverter.ToString(_buffer, ptr, len);
            ptr += len;
            return res;
        }

        void Append(float value)
        {
            Append(BitConverter.GetBytes(value));
        }

        float ReadFloat()
        {
            if (_currentSize - ptr < 8)
                throw new System.Exception("stream error");
            float res = (float)BitConverter.ToDouble(_buffer, ptr);
            ptr += 8;
            return res;
        }

        public byte[] GetBuffer(out int size)
        {
            size = _currentSize;
            return _buffer;
        }

        public long GetSize()
        {
            return _currentSize - ptr;
        }

        public long FillBuffer(byte[] arr)
        {
            long sz = GetSize();
            sz = (sz > arr.Length) ? arr.Length : sz;
            Array.Copy(_buffer, ptr, arr, 0, sz);
            return sz;
        }

        public byte[] GetBuffer()
        {
            long sz = GetSize();
            byte[] res = new byte[sz];
            Array.Copy(_buffer, ptr, res, 0, sz);
            return res;
        }


        private int _currentSize;
        private Byte[] _buffer = new Byte[2];
        private int ptr;
        private int ptrLast;
    }
}