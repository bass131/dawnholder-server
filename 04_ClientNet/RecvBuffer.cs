namespace Dawnholder.Client.Net;

/// <summary>
/// 수신된 바이트 스트림을 임시 저장하는 링 버퍼.
///
/// **클라 컨텍스트 메모**:
/// - 클라는 단일 connection만 가짐 (서버 vs 다수 sessions와 다름).
/// - 그럼에도 같은 링 버퍼 패턴을 쓰는 이유: TCP는 *byte stream*이라 한 번의
///   ReceiveAsync로 "패킷 1개 전체"가 깨끗이 도착한다는 보장 없음. 절반만 와서
///   기다려야 할 수도 있고, 1.5개가 와서 0.5개를 다음 수신과 합쳐야 할 수도 있음.
/// - 이 부분 수신/연쇄 수신을 깔끔하게 처리하려면 read/write 커서가 분리된 버퍼가
///   필요. 서버측과 동일한 발상.
///
/// **링 버퍼 시각화** (10바이트 가정):
/// <code>
/// [r/w][][][][][][][][][]   초기 상태 (read=0, write=0)
/// [r][][][][][w][][][][]    5바이트 수신 (write=5)
/// [][][][][][r/w][][][][]   5바이트 처리 완료 (read=5, write=5)
/// </code>
/// 한쪽 끝에 도달하면 Clean()에서 남은 데이터를 앞으로 당김.
/// </summary>
public class RecvBuffer
{
    ArraySegment<byte> _buffer;
    int _readPos;
    int _writePos;

    public RecvBuffer(int bufferSize)
    {
        _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
    }

    /// <summary>아직 처리되지 않은 데이터의 크기 (write - read).</summary>
    public int DataSize => _writePos - _readPos;

    /// <summary>버퍼 끝까지 남은 빈 공간 (write 가능량).</summary>
    public int FreeSize => _buffer.Count - _writePos;

    /// <summary>처리해야 할 데이터의 슬라이스 (OnRecv에 넘김).</summary>
    public ArraySegment<byte> ReadSegment =>
        new ArraySegment<byte>(_buffer.Array!, _buffer.Offset + _readPos, DataSize);

    /// <summary>다음 ReceiveAsync가 쓸 수 있는 슬라이스 (SetBuffer에 넘김).</summary>
    public ArraySegment<byte> WriteSegment =>
        new ArraySegment<byte>(_buffer.Array!, _buffer.Offset + _writePos, FreeSize);

    /// <summary>
    /// 버퍼 끝에 가까워졌으면 미처리 데이터를 앞으로 당김.
    /// 비어있으면 단순 커서 리셋.
    /// </summary>
    public void Clean()
    {
        int dataSize = DataSize;

        if (dataSize == 0)
        {
            _readPos = 0;
            _writePos = 0;
            return;
        }

        Array.Copy(
            sourceArray: _buffer.Array!,
            sourceIndex: _buffer.Offset + _readPos,
            destinationArray: _buffer.Array!,
            destinationIndex: _buffer.Offset,
            length: dataSize);

        _readPos = 0;
        _writePos = dataSize;
    }

    /// <summary>처리 완료한 바이트 수만큼 read 커서 전진. 음수/초과면 false.</summary>
    public bool OnRead(int numOfBytes)
    {
        if (numOfBytes > DataSize)
            return false;

        _readPos += numOfBytes;
        return true;
    }

    /// <summary>새로 수신된 바이트 수만큼 write 커서 전진. 초과면 false.</summary>
    public bool OnWrite(int numOfBytes)
    {
        if (numOfBytes > FreeSize)
            return false;

        _writePos += numOfBytes;
        return true;
    }
}
