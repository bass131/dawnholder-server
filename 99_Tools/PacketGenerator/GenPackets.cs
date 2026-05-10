using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using ServerCore;

// 유니티에서는 이전에는 ReadOnlySpan을 지원하지 않았지만
// 이제는 지원하므로, ReadOnlySpan을 사용하여 패킷 버퍼를 읽는 방법을 사용
// Span도 이제 지원하므로, 패킷 버퍼에 데이터를 쓰는 방법에도 Span을 사용하여 최적화된 코드를 작성할 수 있음

public enum PacketID
{
    C_Ping = 1,
	S_Pong = 2,
	
}

public interface IPacket
{
    ushort Protocol { get; } // 패킷 ID를 반환하는 프로퍼티

    void Read(ArraySegment<byte> _Segment); // 패킷 읽기 메서드
    ArraySegment<byte> Write(); // 패킷 쓰기 메서드
}


public class C_Ping : IPacket // C_Ping 패킷
{
    // 멤버 변수들
    public long clientTimestampMs;
    public ushort Protocol { get { return (ushort)PacketID.C_Ping; } }

    public void Read(ArraySegment<byte> _Segment)
    {
        // C++ 서버에서는 더 최적화 가능하지만, C#에서는 BitConverter로 간단히 읽는 방법을 사용

        ushort count = 0; // 패킷 버퍼에서 현재 위치를 나타내는 카운트

        // 패킷 읽기 시작.
#if !NET_LEGACY
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(_Segment.Array, _Segment.Offset, _Segment.Count); // 패킷 버퍼에 대한 ReadOnlySpan<byte> 생성
#endif

        count += sizeof(ushort); // 패킷 크기를 읽고 카운트 증가
        count += sizeof(ushort); // 패킷 ID를 읽고 카운트 증가
        
        // 읽기 시작

        #if !NET_LEGACY
		// clientTimestampMs 읽기
		this.clientTimestampMs = BitConverter.ToInt64(s.Slice(count, s.Length - count));
		count += sizeof(long);
		#else
		// clientTimestampMs 읽기
		this.clientTimestampMs = BitConverter.ToInt64(s.Array, s.Offset + count);
		count += sizeof(long);
		#endif
		
    }


    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> Segment = SendBufferHelper.Open(65535); // 패킷 버퍼 열기
        ushort count = 0; // 패킷 버퍼에서 현재 위치를 나타내는 카운트

#if !NET_LEGACY
        bool success = true; // 쓰기 성공 여부
        Span<byte> s = new Span<byte>(Segment.Array, Segment.Offset, Segment.Count); // 패킷 버퍼에 대한 Span<byte> 생성
        
        count += sizeof(ushort); // 패킷 크기를 기록하기 위해 2바이트 공간 예약
        
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.C_Ping); // 패킷 ID 기록
        
        count += sizeof(ushort); // 패킷 ID 기록 후 카운트 증가
#else
        count += sizeof(ushort); // 패킷 크기를 기록하기 위해 2바이트 공간 예약
        
        byte[] buffer = BitConverter.GetBytes((ushort)PacketID.C_Ping); // 패킷 ID를 바이트 배열로 변환
        Array.Copy(buffer, 0, Segment.Array, Segment.Offset + count, sizeof(ushort)); // 패킷 ID를 패킷 버퍼에 복사
        
        count += sizeof(ushort); // 패킷 ID 기록 후 카운트 증가
#endif

        // 쓰기 시작

        #if !NET_LEGACY
		// clientTimestampMs 읽기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.clientTimestampMs);
		count += sizeof(long);
		#else
		// clientTimestampMs 읽기
		Array.Copy(BitConverter.GetBytes(this.clientTimestampMs), 0, Segment.Array, Segment.Offset + count, sizeof(long));
		count += sizeof(long);
		#endif
		

#if !NET_LEGACY
        success &= BitConverter.TryWriteBytes(s, count); // 최종 카운트 기록

        if (success == false)
            return null;
#else
        Array.Copy(BitConverter.GetBytes(count), 0, Segment.Array, Segment.Offset, sizeof(ushort));
#endif
        return SendBufferHelper.Close(count);
    }
}

public class S_Pong : IPacket // S_Pong 패킷
{
    // 멤버 변수들
    public long clientTimestampMs;
	public long serverTimestampMs;
    public ushort Protocol { get { return (ushort)PacketID.S_Pong; } }

    public void Read(ArraySegment<byte> _Segment)
    {
        // C++ 서버에서는 더 최적화 가능하지만, C#에서는 BitConverter로 간단히 읽는 방법을 사용

        ushort count = 0; // 패킷 버퍼에서 현재 위치를 나타내는 카운트

        // 패킷 읽기 시작.
#if !NET_LEGACY
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(_Segment.Array, _Segment.Offset, _Segment.Count); // 패킷 버퍼에 대한 ReadOnlySpan<byte> 생성
#endif

        count += sizeof(ushort); // 패킷 크기를 읽고 카운트 증가
        count += sizeof(ushort); // 패킷 ID를 읽고 카운트 증가
        
        // 읽기 시작

        #if !NET_LEGACY
		// clientTimestampMs 읽기
		this.clientTimestampMs = BitConverter.ToInt64(s.Slice(count, s.Length - count));
		count += sizeof(long);
		#else
		// clientTimestampMs 읽기
		this.clientTimestampMs = BitConverter.ToInt64(s.Array, s.Offset + count);
		count += sizeof(long);
		#endif
		
		#if !NET_LEGACY
		// serverTimestampMs 읽기
		this.serverTimestampMs = BitConverter.ToInt64(s.Slice(count, s.Length - count));
		count += sizeof(long);
		#else
		// serverTimestampMs 읽기
		this.serverTimestampMs = BitConverter.ToInt64(s.Array, s.Offset + count);
		count += sizeof(long);
		#endif
		
    }


    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> Segment = SendBufferHelper.Open(65535); // 패킷 버퍼 열기
        ushort count = 0; // 패킷 버퍼에서 현재 위치를 나타내는 카운트

#if !NET_LEGACY
        bool success = true; // 쓰기 성공 여부
        Span<byte> s = new Span<byte>(Segment.Array, Segment.Offset, Segment.Count); // 패킷 버퍼에 대한 Span<byte> 생성
        
        count += sizeof(ushort); // 패킷 크기를 기록하기 위해 2바이트 공간 예약
        
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.S_Pong); // 패킷 ID 기록
        
        count += sizeof(ushort); // 패킷 ID 기록 후 카운트 증가
#else
        count += sizeof(ushort); // 패킷 크기를 기록하기 위해 2바이트 공간 예약
        
        byte[] buffer = BitConverter.GetBytes((ushort)PacketID.S_Pong); // 패킷 ID를 바이트 배열로 변환
        Array.Copy(buffer, 0, Segment.Array, Segment.Offset + count, sizeof(ushort)); // 패킷 ID를 패킷 버퍼에 복사
        
        count += sizeof(ushort); // 패킷 ID 기록 후 카운트 증가
#endif

        // 쓰기 시작

        #if !NET_LEGACY
		// clientTimestampMs 읽기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.clientTimestampMs);
		count += sizeof(long);
		#else
		// clientTimestampMs 읽기
		Array.Copy(BitConverter.GetBytes(this.clientTimestampMs), 0, Segment.Array, Segment.Offset + count, sizeof(long));
		count += sizeof(long);
		#endif
		
		#if !NET_LEGACY
		// serverTimestampMs 읽기
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.serverTimestampMs);
		count += sizeof(long);
		#else
		// serverTimestampMs 읽기
		Array.Copy(BitConverter.GetBytes(this.serverTimestampMs), 0, Segment.Array, Segment.Offset + count, sizeof(long));
		count += sizeof(long);
		#endif
		

#if !NET_LEGACY
        success &= BitConverter.TryWriteBytes(s, count); // 최종 카운트 기록

        if (success == false)
            return null;
#else
        Array.Copy(BitConverter.GetBytes(count), 0, Segment.Array, Segment.Offset, sizeof(ushort));
#endif
        return SendBufferHelper.Close(count);
    }
}


