using System.Net;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// 서버 주소 정책: IP 주소만 받는다. 도메인 이름은 해석하지 않고 거부한다.
    /// MainMenu의 사전 점검(<see cref="ConnectionProbe"/>)과 실제 연결(<see cref="NetworkService"/>)이
    /// 이 판정 하나를 함께 써서, 점검을 통과한 주소가 실제 연결에서 거부되지 않는다.
    /// 포트 범위는 연결 쪽이 따로 검사한다.
    /// </summary>
    internal static class ServerAddressRule
    {
        internal static bool TryParse(string host, out IPAddress address)
        {
            address = null;
            return !string.IsNullOrWhiteSpace(host) && IPAddress.TryParse(host, out address);
        }
    }
}
