# Client.Net — Unity 클라용 socket 라이브러리 (Y2 분리)

## ⚠️ Y2 분리 모델 (ADR-012)

본 프로젝트는 **socket 인프라를 클라/서버 양쪽 분리**합니다 (Y2 갈래). 패킷 정의(`98_Shared/Protocol/`)만 공유하고 *전송 코드는 분리*. 서버 측 카운터파트는 `02_Server/Network/`. 두 영역은 *동일 인터페이스가 아니라 책임 단위가 분리*된 자매 구현입니다.

본 라이브러리는 .NET Standard 2.1로 빌드되어 `03_Client/Assets/Plugins/`로 자동 복사되고, Unity가 참조 (ADR-010 + ADR-012). Unity 측 wrapper는 `03_Client/Assets/Scripts/Network/`에 박힙니다.

## Layout

```
04_ClientNet/
├── Dawnholder.Client.Net.csproj  .NET Standard 2.1 (ADR-010)
├── ClientSession.cs              PacketSession 기반, OnConnected/OnRecv/OnSend
├── Connector.cs                  비동기 connect (헤드리스 봇 재사용 — ADR-011 자리잡이 효과)
├── RecvBuffer.cs                 수신 ring buffer (서버와 동형 구조)
├── SendBuffer.cs                 송신 ring buffer (자리잡이 — M5+ broadcast 최적화 시 활용)
└── SmokeProbe.cs                 연결 smoke test 유틸 (현재 미사용, 자리잡이)
```

## 컨벤션

- **`ClientSession`**: `02_Server/Network/Session.cs`의 *클라 미러*. `OnRecvPacket` 안에서 직접 dispatch 또는 Unity 측 wrapper로 위임. 헌법 #1 — 클라는 *권위 상태 변경 X*, 서버 알림만 표시.
- **Connector**: server `Listener`의 *반대편*. `02_Server`엔 connector 없음 (서버는 listen만). 본 영역 단독 보유.
- **Buffer**: 서버와 동형 구조지만 *별도 컴파일*. 양쪽 같이 진화 (`98_Shared/Protocol/` 변경 시 양쪽 점검 의무).
- **Logging**: Unity 측에서 wrapping. 본 라이브러리는 raw exception/`Console.WriteLine` 정도만.
- **No Unity dependencies**: 본 영역은 `UnityEngine` 참조 *절대 금지*. .NET Standard 2.1 pure (DI 친화 + 테스트 가능성 보존).

## 금지 사항

- `UnityEngine`/`UnityEditor` 어셈블리 참조 (CompileError 직접 차단).
- `Console.WriteLine` 외 IO (파일 로그 등은 Unity wrapper 책임).
- 정적 mutable 게임 상태. `static` 필드는 readonly 또는 const만.
- 헌법 #1 위반 — 권위 상태(HP/XP/inventory/위치) 직접 변경. 서버 snapshot 반영만.

## 헌법 #3 (Trust Boundary) 정합

M4.1 Phase 03 봉합 완료 (2026-05-23). `FrameValidator` helper가 클라/서버 양쪽 각자 박힘 — 옵션 B 변형, ServerCore 재사용성 정신 보존. 두 helper는 같은 시그니처(`TryValidateFrameHeader(ushort, out string?)`) + 같은 상수(`MinFrameSize=4`, `MaxFrameSize=4096`) 동기화 약속으로 헌법 #4 정신을 유지합니다. 어느 한쪽 상수/시그니처 변경 시 반드시 양쪽 동시 변경 의무.

봉합된 결함 3종: `dataSize=0` 무한 루프 / `dataSize<4` 깨진 frame 해석 / `dataSize>4096` disconnect 안 됨. 검증 실패 시 `Disconnect()` 호출(silent drop X, 헌법 #3 정합).

## 변경 머지 전

실행: `dotnet build Dawnholder.slnx` — 통과해야 함. PostBuild에서 `Dawnholder.Client.Net.dll`이 `03_Client/Assets/Plugins/ClientNet/`로 자동 복사됨 (`.gitignore` 화이트리스트 정합, PR #19 박힘).

## 자매 영역 동기화

| 본 영역 변경 | 영향 |
|---|---|
| `ClientSession.OnRecvPacket` dispatch 추가 | `02_Server/Network/GameSession.OnRecvPacket`도 같이 (PacketID 정합) |
| `RecvBuffer`/`SendBuffer` 시그니처 변경 | 서버 동형 변경 필요 (양쪽 컴파일 검증 필수) |
| `Connector` API 변경 | `99_Tools/headless-bot/`도 같이 (Connector 재사용 — ADR-011 자리잡이 활용 사례) |
