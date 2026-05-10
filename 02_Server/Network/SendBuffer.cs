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
        public static ThreadLocal<SendBuffer?> m_CurrentBuffer = new ThreadLocal<SendBuffer?>(() => null);

        public static int ChunkSize {get; set;} = 65535 * 1000;

        public static ArraySegment<byte> Open(int _ReserveSize)
        {
            if(m_CurrentBuffer.Value == null)
            {
                m_CurrentBuffer.Value = new SendBuffer(ChunkSize);
            }

            if(m_CurrentBuffer.Value!.FreeSize < _ReserveSize)
            {
                m_CurrentBuffer.Value = new SendBuffer(ChunkSize);
            }

            return m_CurrentBuffer.Value!.Open(_ReserveSize);
        }

        public static ArraySegment<byte> Close(int _UsedSize)
        {
            return m_CurrentBuffer.Value!.Close(_UsedSize);
        }

        public SendBuffer? Current { get { return m_CurrentBuffer.Value; } }

        public SendBuffer? New(int _BufferSize)
        {
            if(_BufferSize > 10000)
            {
                Console.WriteLine("SendBuffer Error : Buffer Size is too large");
                return null;
            }

            SendBuffer? sendBuffer = m_CurrentBuffer.Value;
            if(sendBuffer == null)
            {
                sendBuffer = new SendBuffer(_BufferSize);
                m_CurrentBuffer.Value = sendBuffer;
            }
            else if(sendBuffer.FreeSize < _BufferSize)
            {
                sendBuffer = new SendBuffer(_BufferSize);
                m_CurrentBuffer.Value = sendBuffer;
            }

            return sendBuffer;
        }
    }

    // SendBuffer : 전송할 데이터를 저장하는 버퍼
    public class SendBuffer
    {
        // [u][][][][][][][][][] : 사용하지 않음
        // [][][][][u][][][][][] : 사용
        byte[] m_Buffer;
        int m_UsedSize = 0;

        public int FreeSize { get { return m_Buffer.Length - m_UsedSize; } } // 남은 공간

        public SendBuffer(int _ChunkSize)
        {
            m_Buffer = new byte[_ChunkSize];
        }

        public ArraySegment<byte> Open(int _ReserveSize)
        {
            if(_ReserveSize > FreeSize)
                return default;

            return new ArraySegment<byte>(m_Buffer, m_UsedSize, _ReserveSize);
        }

        public ArraySegment<byte> Close(int _UsedSize)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(m_Buffer, m_UsedSize, _UsedSize);
            m_UsedSize += _UsedSize;

            return segment;
        }
    }
}