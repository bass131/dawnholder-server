using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace PacketGenerator
{
    internal class PacketFormat
    {
        // {0} : 패킷 등록 코드
        public static string managerFormat =
@"using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PacketManager
{{
    #region SINGLETON
#nullable enable
    static PacketManager? _instance = new PacketManager();
#nullable disable

    public static PacketManager Instance
    {{
        get {{ return _instance; }}
        private set {{ }}
    }}
    #endregion
    
    PacketManager()
    {{
        Register();
    }}

    // < 프로토콜 ID, 패킷 처리 콜백>
    Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> m_MakeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
    Dictionary<ushort, Action<PacketSession,IPacket>> m_Handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();

    public void Register()
    {{
        {0}
    }}
        
    public void OnRecvPacket(PacketSession session,
        ArraySegment<byte> buffer,
        Action<PacketSession, IPacket> onRecvCallBack = null)
    {{
        ushort count = 0;

        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        count += 2;

        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);
        count += 2;

        Func<PacketSession, ArraySegment<byte>, IPacket> func = null;
        if(m_MakeFunc.TryGetValue(id, out func))
        {{
            IPacket packet = func.Invoke(session, buffer);

            if (onRecvCallBack != null)
                onRecvCallBack.Invoke(session, packet);
            else
                HandlePacket(session, packet);
        }}

        // System.Console.WriteLine($""Recv PacketId: {{id}}, Size: {{size}}"");
    }}

    T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer)
        where T : IPacket, new()
    {{
        T packet = new T();
        packet.Read(buffer);

        return packet;
    }}

    public void HandlePacket(PacketSession _session, IPacket _packet)
    {{
        Action<PacketSession, IPacket> action = null;
        if (m_Handler.TryGetValue(_packet.Protocol, out action))
            action.Invoke(_session, _packet);
    }}
}}";

        // {0} : 패킷 이름
        public static string mangerRegisterFormat =
@"
        m_MakeFunc.Add((ushort)PacketID.{0}, MakePacket<{0}>);
        m_Handler.Add((ushort)PacketID.{0}, PacketHandler.{0}Handler);";
        // {0} : 패킷 이름/번호 목록
        // {1} : 패킷 목록
        public static string fileFormat =
@"using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using ServerCore;

// 유니티에서는 이전에는 ReadOnlySpan을 지원하지 않았지만
// 이제는 지원하므로, ReadOnlySpan을 사용하여 패킷 버퍼를 읽는 방법을 사용
// Span도 이제 지원하므로, 패킷 버퍼에 데이터를 쓰는 방법에도 Span을 사용하여 최적화된 코드를 작성할 수 있음

public enum PacketID
{{
    {0}
}}

public interface IPacket
{{
    ushort Protocol {{ get; }} // 패킷 ID를 반환하는 프로퍼티

    void Read(ArraySegment<byte> _Segment); // 패킷 읽기 메서드
    ArraySegment<byte> Write(); // 패킷 쓰기 메서드
}}


{1}
";
        // {0} : 패킷 이름
        // {1} : 패킷 번호
        public static string packetEnumFormat =
@"{0} = {1},";


        // {0} : 패킷 이름
        // {1} : 패킷 멤버 변수들
        // {2} : 멤버 변수 Read
        // {3} : 멤버 변수 Write
        public static string packetFormat =
@"public class {0} : IPacket // {0} 패킷
{{
    // 멤버 변수들
    {1}
    public ushort Protocol {{ get {{ return (ushort)PacketID.{0}; }} }}

    public void Read(ArraySegment<byte> _Segment)
    {{
        // C++ 서버에서는 더 최적화 가능하지만, C#에서는 BitConverter로 간단히 읽는 방법을 사용

        ushort count = 0; // 패킷 버퍼에서 현재 위치를 나타내는 카운트

        // 패킷 읽기 시작.
#if !NET_LEGACY
        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(_Segment.Array, _Segment.Offset, _Segment.Count); // 패킷 버퍼에 대한 ReadOnlySpan<byte> 생성
#endif

        count += sizeof(ushort); // 패킷 크기를 읽고 카운트 증가
        count += sizeof(ushort); // 패킷 ID를 읽고 카운트 증가
        
        // 읽기 시작

        {2}
    }}


    public ArraySegment<byte> Write()
    {{
        ArraySegment<byte> Segment = SendBufferHelper.Open(65535); // 패킷 버퍼 열기
        ushort count = 0; // 패킷 버퍼에서 현재 위치를 나타내는 카운트

#if !NET_LEGACY
        bool success = true; // 쓰기 성공 여부
        Span<byte> s = new Span<byte>(Segment.Array, Segment.Offset, Segment.Count); // 패킷 버퍼에 대한 Span<byte> 생성
        
        count += sizeof(ushort); // 패킷 크기를 기록하기 위해 2바이트 공간 예약
        
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacketID.{0}); // 패킷 ID 기록
        
        count += sizeof(ushort); // 패킷 ID 기록 후 카운트 증가
#else
        count += sizeof(ushort); // 패킷 크기를 기록하기 위해 2바이트 공간 예약
        
        byte[] buffer = BitConverter.GetBytes((ushort)PacketID.{0}); // 패킷 ID를 바이트 배열로 변환
        Array.Copy(buffer, 0, Segment.Array, Segment.Offset + count, sizeof(ushort)); // 패킷 ID를 패킷 버퍼에 복사
        
        count += sizeof(ushort); // 패킷 ID 기록 후 카운트 증가
#endif

        // 쓰기 시작

        {3}

#if !NET_LEGACY
        success &= BitConverter.TryWriteBytes(s, count); // 최종 카운트 기록

        if (success == false)
            return null;
#else
        Array.Copy(BitConverter.GetBytes(count), 0, Segment.Array, Segment.Offset, sizeof(ushort));
#endif
        return SendBufferHelper.Close(count);
    }}
}}

";
        // {0} : 변수 타입
        // {1} : 변수 이름
        public static string MemberFormat =
@"public {0} {1};";


        // {0} : 리스트 이름 [대문자]
        // {1} : 리스트 이름 [소문자]
        // {2} : 멤버 변수들
        // {3} : 멤버 변수 Read
        // {4} : 멤버 변수 Write
        public static string MemberListFormat =
@"
public struct {0}
{{
#if NET10_0_OR_GREATER
    public {0}()
    {{
        // {0} 구조체 생성자
    }}
#endif

    // {0}List 멤버 변수들
    {2}

#if !NET_LEGACY
    public void Read(ReadOnlySpan<byte> s, ref ushort count)
    {{
        // {0} 리스트 멤버 읽기
        // C++ 서버에서는 더 최적화 가능하지만, C#에서는 BitConverter로 간단히 읽는 방법을 사용

        {3}
    }}
#else
    public void Read(ArraySegment<byte> s, ref ushort count)
    {{
        // {0} 리스트 멤버 읽기
        // C++ 서버에서는 더 최적화 가능하지만, C#에서는 BitConverter로 간단히 읽는 방법을 사용

        {3}
    }}
#endif
    
#if !NET_LEGACY
    public bool Write(Span<byte> s, ref ushort count)
    {{
        bool success = true; // 쓰기 성공 여부

        // {0} 리스트 멤버 쓰기

        {4}

        return success;
    }}
#else
    public bool Write(ArraySegment<byte> s, ref ushort count)
    {{
        bool success = true; // 쓰기 성공 여부

        // {0} 리스트 멤버 쓰기

        {4}

        return success;
    }}
#endif

}}

public List<{0}> {1}s = new List<{0}>(); // {0} 리스트";

        // {0} : 변수 이름
        // {1} : To~ 변수 형식
        // {2} : 변수 형식
        public static string ReadFormat =
@"#if !NET_LEGACY
// {0} 읽기
this.{0} = BitConverter.{1}(s.Slice(count, s.Length - count));
count += sizeof({2});
#else
// {0} 읽기
this.{0} = BitConverter.{1}(s.Array, s.Offset + count);
count += sizeof({2});
#endif
";
        // {0} : 변수 이름
        // {1} : 변수 형식
        public static string ReadByteFormat =
@"// {0} 읽기
this.{0} = ({1})_Segment.Array[_Segment.Offset + count]; // testByte 읽기
count += sizeof({1});
";

        // {0} : 변수 이름
        public static string ReadStringFormat =
@"#if !NET_LEGACY
// {0} 문자열 읽기
ushort {0}Len = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
count += sizeof(ushort);
this.{0} = Encoding.Unicode.GetString(s.Slice(count, {0}Len));
count += {0}Len;
#else
ushort {0}Len = BitConverter.ToUInt16(s.Array, s.Offset + count);
count += sizeof(ushort);
this.{0} = Encoding.Unicode.GetString(s.Array, s.Offset + count, {0}Len);
count += {0}Len;
#endif
";

        // {0} : 리스트 이름 [대문자]
        // {1} : 리스트 이름 [소문자]
        public static string ReadListFormat =
@"#if !NET_LEGACY
// {0} 리스트 읽기
this.{1}s.Clear();
ushort {1}Len = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
count += sizeof(ushort);
for (int i = 0; i < {1}Len; i++)
{{
    {0} {1} = new {0}();
    {1}.Read(s, ref count);
    {1}s.Add({1});
}}
#else
this.{1}s.Clear();
ushort {1}Len = BitConverter.ToUInt16(s.Array, s.Offset + count);
count += sizeof(ushort);
for (int i = 0; i < {1}Len; i++)
{{
    {0} {1} = new {0}();
    {1}.Read(s, ref count);
    {1}s.Add({1});
}}
#endif
";

        // {0} : 변수 이름
        // {1} : 변수 형식
        public static string WriteFormat =
@"#if !NET_LEGACY
// {0} 읽기
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.{0});
count += sizeof({1});
#else
// {0} 읽기
Array.Copy(BitConverter.GetBytes(this.{0}), 0, Segment.Array, Segment.Offset + count, sizeof({1}));
count += sizeof({1});
#endif
";

        // {0} : 변수 이름
        // {1} : 변수 형식
        public static string WriteByteFormat =
@"// {0} 쓰기
Segment.Array[Segment.Offset + count] = (byte)this.{0}; // testByte 읽기
count += sizeof({1});
";

        // {0} : 변수 이름
        public static string WriteStringFormat =
@"#if !NET_LEGACY
// {0} 문자열 읽기
ushort {0}Len = (ushort)Encoding.Unicode.GetBytes(this.{0}, 0, this.{0}.Length, Segment.Array, Segment.Offset + count + sizeof(ushort));
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), {0}Len);
count += sizeof(ushort);
count += {0}Len;
#else
// {0} 문자열 읽기
ushort {0}Len = (ushort)Encoding.Unicode.GetBytes(this.{0}, 0, this.{0}.Length, Segment.Array, Segment.Offset + count + sizeof(ushort));
Array.Copy(BitConverter.GetBytes({0}len), 0, Segment.Array, Segment.Offset + count, sizeof(ushort));
count += sizeof(ushort);
count += {0}Len;
#endif
";

        // {0} : 리스트 이름 [대문자]
        // {1} : 리스트 이름 [소문자]
        public static string WriteListFormat =
@"#if !NET_LEGACY
// {0} 리스트 쓰기
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.{1}s.Count);
count += sizeof(ushort);
foreach ({0} {1} in this.{1}s)
    success &= {1}.Write(s, ref count);
#else
// {0} 리스트 쓰기
Array.Copy(BitConverter.GetBytes((ushort)this.{1}s.Count), 0, Segment.Array, Segment.Offset + count, sizeof(ushort));
count += sizeof(ushort);
foreach ({0} {1} in this.{1}s)
    {1}.Write(Segment, ref count);
#endif
";
    }
}
