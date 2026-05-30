using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dawnholder.Server.Network
{
    public class RecvBuffer
    {
        // Receive Buffer의 동작 원리
        // 10바이트 배열이라고 가정하고 시작
        // [r/w][][][][][][][][][]  : _readPos, _writePos 처음에 있고 나머지가 비어있음 시작 하는 부분
        // [r/][][][][][w][][][][]  : 5바이트
        // [][][][][][][][r/w][][]  : 전송 완료 후 다시 초기화
        // [][][r/][w][][][]        : 이상태에서 대기 // 각 2바이트 경우
        // [r][w][][][][][]         : 유효 범위를 바꿔줌 // 다시 처음으로 옮김

        ArraySegment<byte> _buffer;
        int _readPos;
        int _writePos;

        public RecvBuffer(int bufferSize)
        {
            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        public int DataSize { get {return _writePos - _readPos;} }
        public int FreeSize { get {return _buffer.Count - _writePos;} }

        public ArraySegment<byte> ReadSegment
        {
            get {return new ArraySegment<byte>(_buffer.Array!, _buffer.Offset + _readPos, DataSize);}
        }
        public ArraySegment<byte> WriteSegment
        {
            get {return new ArraySegment<byte>(_buffer.Array!, _buffer.Offset + _writePos, FreeSize);}
        }

        public void Clean()
        {
            int dataSize = DataSize;

            if (dataSize == 0)
            {
                // 남은 데이터가 없으면 복사하지 않고 커서 위치만 리셋
                _readPos = 0;
                _writePos = 0;
                return;
            }
            else
            {
                // 남은 찌끄래기가 있으면 시작 위치로 복사
                // [][][][][r/w][][][][][]  : 5바이트
                // [r/w][][][][][][][][][]  : 전송 완료 후 다시 초기화

                Array.Copy(
                _buffer.Array!,
                _buffer.Offset + _readPos,
                _buffer.Array!,
                _buffer.Offset,
                dataSize);

                _readPos = 0;
                _writePos = dataSize;
            }
        }

        public bool OnRead(int numOfBytes)
        {
            if (numOfBytes > DataSize)
            {
                return false;
            }

            _readPos += numOfBytes;
            return true;
        }

        public bool OnWrite(int numOfBytes)
        {
            if (numOfBytes > FreeSize)
            {
                return false;
            }

            _writePos += numOfBytes;
            return true;
        }
    }
}
