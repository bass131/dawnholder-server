using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// 서버 연결 가용성 짧게 점검 (MainMenu Start 버튼 게이트용).
    ///
    /// **목적**: 게임 본 흐름(NetworkBootstrap → Gameplay Scene) 진입 전에 *서버 가용성*만 점검.
    /// connect 시도 → 즉시 close (1ms급). 게임 본 connection은 NetworkBootstrap이 Gameplay Scene에서 별도로 박음.
    ///
    /// **왜 필요한가** (M3.8 Phase 05 5-B 안전망):
    /// 발표장 환경에서 Hamachi IP 오타 / 서버 다운 / 방화벽 차단 등으로 연결 실패 시,
    /// CharacterSelect → Gameplay Scene 진입 후에야 발견되면 사용자 혼란.
    /// MainMenu에서 즉시 차단 + 오류 메시지가 학부생 시연 안전망.
    ///
    /// **스레드 모델**: Task.Run 워커 스레드에서 socket connect 시도 → 결과는
    /// MainThreadDispatcher.Enqueue로 main thread 콜백 (UnityEngine API 안전 호출 보장).
    ///
    /// **헌법 #1 정합**: 본 클래스는 권위 상태 변경 X. 단순 TCP probe만.
    /// </summary>
    public static class ConnectionProbe
    {
        /// <summary>
        /// 비동기 connect 점검. 콜백은 main thread에서 호출됨.
        /// </summary>
        /// <param name="host">서버 호스트 (IP 또는 도메인)</param>
        /// <param name="port">서버 포트 (기본 7777)</param>
        /// <param name="callback">(success, errorMessage). 성공 시 errorMessage = "".</param>
        /// <param name="timeoutMs">연결 시도 타임아웃 (기본 3초)</param>
        public static void TryConnect(string host, int port, Action<bool, string> callback, int timeoutMs = 3000)
        {
            Task.Run(() =>
            {
                Socket socket = null;
                try
                {
                    if (string.IsNullOrWhiteSpace(host))
                    {
                        MainThreadDispatcher.Enqueue(() => callback(false, "서버 주소가 비어있어요"));
                        return;
                    }

                    IPAddress[] addresses;
                    try
                    {
                        addresses = Dns.GetHostAddresses(host);
                    }
                    catch (Exception ex)
                    {
                        MainThreadDispatcher.Enqueue(() => callback(false, $"호스트 주소 확인 실패: {ex.Message}"));
                        return;
                    }

                    if (addresses.Length == 0)
                    {
                        MainThreadDispatcher.Enqueue(() => callback(false, "호스트 주소를 찾을 수 없어요"));
                        return;
                    }

                    IPAddress ip = addresses[0];
                    socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    IAsyncResult result = socket.BeginConnect(ip, port, null, null);
                    bool completed = result.AsyncWaitHandle.WaitOne(timeoutMs);

                    if (!completed)
                    {
                        // 타임아웃 — socket close하면 BeginConnect 콜백이 거부 처리되며 종료됨.
                        try { socket.Close(); } catch { /* swallow */ }
                        MainThreadDispatcher.Enqueue(() => callback(false, $"서버 응답 없음 ({timeoutMs}ms 타임아웃)"));
                        return;
                    }

                    try
                    {
                        socket.EndConnect(result);
                    }
                    catch (SocketException ex)
                    {
                        string reason = ex.SocketErrorCode switch
                        {
                            SocketError.ConnectionRefused => "서버가 응답하지 않아요 (서버가 켜져 있나요?)",
                            SocketError.HostUnreachable => "호스트에 도달할 수 없어요 (네트워크 경로 확인)",
                            SocketError.NetworkUnreachable => "네트워크 연결을 확인해주세요",
                            SocketError.TimedOut => "서버 응답 시간 초과",
                            _ => $"연결 실패: {ex.SocketErrorCode}"
                        };
                        MainThreadDispatcher.Enqueue(() => callback(false, reason));
                        return;
                    }

                    // 성공 — 즉시 close. 게임 본 connection은 NetworkBootstrap이 Gameplay Scene에서 박음.
                    try { socket.Shutdown(SocketShutdown.Both); } catch { /* swallow */ }
                    socket.Close();
                    MainThreadDispatcher.Enqueue(() => callback(true, ""));
                }
                catch (Exception ex)
                {
                    try { socket?.Close(); } catch { /* swallow */ }
                    MainThreadDispatcher.Enqueue(() => callback(false, $"오류: {ex.Message}"));
                }
            });
        }
    }
}
