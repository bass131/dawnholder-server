// ─────────────────────────────────────────────────────────────────────────────
// ⚠️ 본 코드는 04_ClientNet/SendBuffer.cs와 거의 동일 (의도된 두 벌).
//
// "왜 합치지 않았나" — Y2 분리 갈래(ADR-012):
//   - 서버: .NET 10 LTS, GC 최적화 자유
//   - 클라: .NET Standard 2.1, Unity Mono/IL2CPP 제약
//   환경별 최적화 자유 + 한쪽 변경이 반대편 빌드 안 깸 + 한국 MMO 백엔드
//   현업 패턴(Rookiss/NCSoft/Nexon — 전용 서버 + 클라 socket layer 분리).
//
// "동기는 어떻게 보장하나" — 차이가 알고리즘 자체에 생기면 *그것이 신호*.
//   현재는 같은 알고리즘 우연히 가능. 패킷 정의는 PDL.xml + 코드 생성기가
//   자동 동기화 (98_Shared/Protocol/Generated/).
//
// 책임 단위 분리/통합 표는 ADR-012 본문 참조.
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dawnholder.Server.Network
{
    public class SendBufferHelper
    {
        public static ThreadLocal<SendBuffer?> s_currentBuffer = new ThreadLocal<SendBuffer?>(() => null);

        public static int ChunkSize {get; set;} = 65535 * 1000;

        public static ArraySegment<byte> Open(int reserveSize)
        {
            if(s_currentBuffer.Value == null)
            {
                s_currentBuffer.Value = new SendBuffer(ChunkSize);
            }

            if(s_currentBuffer.Value!.FreeSize < reserveSize)
            {
                s_currentBuffer.Value = new SendBuffer(ChunkSize);
            }

            return s_currentBuffer.Value!.Open(reserveSize);
        }

        public static ArraySegment<byte> Close(int usedSize)
        {
            return s_currentBuffer.Value!.Close(usedSize);
        }

        public SendBuffer? Current { get { return s_currentBuffer.Value; } }

        public SendBuffer? New(int bufferSize)
        {
            if(bufferSize > 10000)
            {
                Console.WriteLine("SendBuffer Error : Buffer Size is too large");
                return null;
            }

            SendBuffer? sendBuffer = s_currentBuffer.Value;
            if(sendBuffer == null)
            {
                sendBuffer = new SendBuffer(bufferSize);
                s_currentBuffer.Value = sendBuffer;
            }
            else if(sendBuffer.FreeSize < bufferSize)
            {
                sendBuffer = new SendBuffer(bufferSize);
                s_currentBuffer.Value = sendBuffer;
            }

            return sendBuffer;
        }
    }

    public class SendBuffer
    {
        // [u][][][][][][][][][] : 사용하지 않음
        // [][][][][u][][][][][] : 사용
        byte[] _buffer;
        int _usedSize = 0;

        public int FreeSize { get { return _buffer.Length - _usedSize; } }

        public SendBuffer(int chunkSize)
        {
            _buffer = new byte[chunkSize];
        }

        public ArraySegment<byte> Open(int reserveSize)
        {
            if(reserveSize > FreeSize)
                return default;

            return new ArraySegment<byte>(_buffer, _usedSize, reserveSize);
        }

        public ArraySegment<byte> Close(int usedSize)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize);
            _usedSize += usedSize;

            return segment;
        }
    }
}