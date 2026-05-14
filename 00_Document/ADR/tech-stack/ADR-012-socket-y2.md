### ADR-012: Unity 클라 socket 레이어 = 분리 클라용 라이브러리 (갈래 Y2)
**날짜**: 2026-05-10 · **2026-05-10 보강** (Phase 07: 책임 단위 분리/통합 표 + 카테고리 맥락)
**상태**: 채택됨
**결정**: Unity 클라이언트의 socket 레이어를 **서버측 ServerCore와 별개로** 신작한다. 새 csproj `04_ClientNet/Dawnholder.Client.Net.csproj` (.NET Standard 2.1)로 작성하고, 빌드 산출물을 `03_Client/Assets/Plugins/ClientNet/`에 자동 복사 (ADR-010 패턴 재사용). 갈래 X(서버 ServerCore를 `98_Shared/Net/`로 마이그해 양쪽이 같은 DLL 참조)는 채택하지 않음.
**이유**: ① **현업 표준 (한국 MMO 백엔드 카테고리)** — Rookiss 강의 패턴 = NCSoft/Nexon/Smilegate/Pearl Abyss 실무 패턴 (전용 서버 + 클라 socket layer 분리). ⚠️ Mirror/FishNet/Unity Netcode 같은 *Unity 인디 멀티플레이어* 카테고리는 통합 패턴이지만 *본 프로젝트와 다른 카테고리* (그쪽은 Unity 안에서 클라+서버 모두 호스팅). gRPC도 일반 RPC 영역. ② **socket 자체 학습 가치** — 클라 입장의 connect/recv/send를 한 번 직접 짜는 것이 면접 임팩트. ③ **변경 내성** — 서버측 nullable·인터페이스 변경이 클라 빌드를 즉시 깨지 않음. ④ 마이그 함정 실측(2026-05-09)에서 X도 무리 없음(~1시간, nullable 13개)이 확인됐지만, 위 세 이유로 Y2 우세.
**트레이드오프**: ① 코드 두 벌 — 같은 SocketAsyncEventArgs 패턴을 클라용으로 한 번 더. ~200~300줄 추가. ② 양쪽 socket 버그가 따로 발생할 수 있음 (서버는 잘 도는데 클라만 꺼짐 등). ③ Plugins 복사 파이프라인 한 번 더 셋업. 다만 Phase 01에서 `Shared.dll` 파이프라인 검증되어 패턴 그대로. ④ 추후 "클라/서버 양쪽이 진짜 같은 framing 로직을 써야겠다"가 되면 framing 부분만 `98_Shared/`로 떼어낼 수 있음 (열어둠).

**Phase 07 책임 단위 정제** (2026-05-10): 분리/통합은 *책임마다 따로* 결정. *전부 분리* 또는 *전부 통합*은 단순화. 각 책임의 *환경 의존성*을 기준으로:

| 책임 | 분리 vs 통합 | 위치 | 이유 |
|---|---|---|---|
| socket 라이프사이클 (Connector/Listener/Session) | 분리 | `02_Server/Network/` + `04_ClientNet/` | 환경별 GC + 변경 내성 (현재 코드는 거의 동일 — 미래 환경별 최적화 자유 보존) |
| 버퍼 관리 (SendBuffer/RecvBuffer) | 분리 | 위와 동일 | socket 인프라 부속 |
| **패킷 데이터 정의** (C_Ping/S_Pong) | **통합** | `98_Shared/Protocol/Generated/` | 와이어 포맷은 환경 무관. 양쪽 동기 필수 |
| PacketManager (dispatch table) | 분리 (예정) | 양쪽 (Phase 08+) | 서버=C_*만, 클라=S_*만 — 받는 데이터 다름 |
| 핸들러 함수 (HandlePing 등) | 분리 | GameSession / UnityClientSession | 서버=게임 로직, 클라=Unity API. 환경 진짜 다름 |

핵심 원칙: **환경 차이가 *코드에 박힐 만한* 곳만 분리**. 패킷 정의는 byte[] ↔ struct 변환이라 *환경 무관* → 통합. PDL.xml + 코드 생성기가 양쪽 동기 자동화.
