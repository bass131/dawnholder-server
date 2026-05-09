using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dawnholder.Server.Network
{
    // RecvBuffer : 수신된 데이터를 저장하는 버퍼
    public class RecvBuffer
    {
        // Receive Buffer의 동작 원리
        // 10바이트 배열이라고 가정하고 시작
        // [r/w][][][][][][][][][]  : _readPos, _writePos 처음에 있고 나머지가 비어있음 시작 하는 부분
        // [r/][][][][][w][][][][]  : 5바이트
        // [][][][][][][][r/w][][]  : 전송 완료 후 다시 초기화
        // [][][r/][w][][][]        : 이상태에서 대기 // 각 2바이트 경우
        // [r][w][][][][][]         : 유효 범위를 바꿔줌 // 다시 처음으로 옮김

        ArraySegment<byte> m_buffer; // 실제 데이터가 저장되는 버퍼
        int m_readPos; // 읽기 시작 위치
        int m_writePos; // 쓰기 시작 위치

        public RecvBuffer(int _bufferSize)
        {
            m_buffer = new ArraySegment<byte>(new byte[_bufferSize], 0, _bufferSize); // 버퍼 생성
        }

        public int DataSize { get {return m_writePos - m_readPos;} } // 현재 버퍼에 저장된 데이터 크기
        public int FreeSize { get {return m_buffer.Count - m_writePos;} } // 현재 버퍼에 남은 공간

        public ArraySegment<byte> ReadSegment // 읽을 수 있는 데이터의 크기
        {
            get {return new ArraySegment<byte>(m_buffer.Array!, m_buffer.Offset + m_readPos, DataSize);}
        }
        public ArraySegment<byte> WriteSegment // 쓸 수 있는 공간의 크기
        {
            get {return new ArraySegment<byte>(m_buffer.Array!, m_buffer.Offset + m_writePos, FreeSize);}
        }

        // 버퍼 정리
        public void Clean()
        {
            int dataSize = DataSize; // 현재 버퍼에 저장된 데이터 크기
            
            if (dataSize == 0)
            {
                // 남은 데이터가 없으면 복사하지 않고 커서 위치만 리셋
                m_readPos = 0; // 읽기 시작 위치
                m_writePos = 0; // 쓰기 시작 위치
                return;
            }
            else
            {
                // 남은 찌끄래기가 있으면 시작 위치로 복사
                // [][][][][r/w][][][][][]  : 5바이트
                // [r/w][][][][][][][][][]  : 전송 완료 후 다시 초기화

                Array.Copy(
                m_buffer.Array!, // 원본 배열
                m_buffer.Offset + m_readPos, // 원본 시작 위치
                m_buffer.Array!, // 복사할 배열
                m_buffer.Offset, // 복사할 시작 위치
                dataSize); // 복사할 크기

                m_readPos = 0; // 읽기 시작 위치
                m_writePos = dataSize; // 쓰기 시작 위치
            }
        }

        // 읽기 커서 이동
        public bool OnRead(int _numOfBytes)
        {
            // 읽을 수 있는 데이터의 크기보다 더 큰 값을 읽으려고 하면
            if (_numOfBytes > DataSize)
            {   
                return false; // 실패
            }

            m_readPos += _numOfBytes; // 읽기 커서 이동
            return true; // 성공
        }

        // 쓰기 커서 이동
        public bool OnWrite(int _numOfBytes)
        {
            // 쓸 수 있는 공간의 크기보다 더 큰 값을 쓰려고 하면
            if (_numOfBytes > FreeSize)
            {
                return false; // 실패
            }

            m_writePos += _numOfBytes; // 쓰기 커서 이동
            return true; // 성공
        }
    }
}
