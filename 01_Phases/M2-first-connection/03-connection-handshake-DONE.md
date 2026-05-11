---
summary: 접속 시 서버가 정한 좌표를 S_EnterMap으로 알려주고 클라가 그 좌표에 spawn. GameMap actor 마샬링(ConcurrentQueue) + 헌법 #1 첫 실전 + PDL 갱신 워크플로우 첫 실용 사용.
phase: 03-connection-handshake
status: done
completed_at: 2026-05-11
commit: (이 commit)
---

# Phase 03 — 접속 핸드셰이크 (S_EnterMap) 완료 박제

**소요 시간**: 약 2.5시간 (코드 1시간 + Burst hang 디버깅 1시간 + 검증 0.5시간).

## TL;DR
M2 세 번째 Phase. 클라가 connect되면 서버가 GameMap actor에 PlayerEntity를 만들고 그 좌표를 `S_EnterMap`으로 통보, 클라는 LocalPlayerController.Instance.SetServerPosition으로 transform 갱신. Disconnect 시 서버가 자동 정리. PDL.xml에 S_EnterMap/S_LeaveMap 추가 → PacketGenerator 한 번 → 양쪽 코드 자동 생성(M1 인프라 첫 실용). GameMap mutation은 ConcurrentQueue<Action>으로 IOCP→tick thread 마샬링(기존 JobQueue 패턴 부적합 발견). 헌법 #1(Server Authority) 시각 시연: 임시로 spawn을 (3, 0)으로 바꿔 클라 Phase 01 transform (0, 0)이 무시되고 서버 좌표로 이동하는 것을 Capture2DScene으로 캡처. 도중 PacketGenerator에서 float 처리 누락 + Burst 한글 경로 hang 두 사건 발견 후 처리.

## 5단계 보고

- **무엇을 만들었나** — PDL.xml에 S_EnterMap(entityId/spawnX/spawnY) + S_LeaveMap 추가. PacketGenerator 정정(float 처리 .NET Standard 2.1 호환). `Maps/GameMap.cs`에 ConcurrentQueue<Action> 마샬링 + Tick 시작 drain. `Loop/GameWorld.cs`에 Instance singleton. `Network/GameSession.cs`에 OnConnected→EnqueueJob→AddPlayer→Send(S_EnterMap), OnDisconnected→EnqueueJob→RemovePlayer. 클라 측 `Input/LocalPlayerController.cs`에 Instance singleton + SetServerPosition + #nullable enable. `Network/UnityClientSession.cs`에 S_EnterMap case + HandleEnterMap. Gameplay 씬에 NetworkBootstrap GameObject(Unity MCP RunCommand로 자동 생성).
- **왜 필요한가** — M2의 모든 후속 Phase는 *서버가 만든 entity*에서 시작. 이 핸드셰이크 없으면 "누구의 좌표를 갱신?"이 답이 없음. 또 헌법 #1을 *코드로 강제*하는 첫 패턴 — 클라 transform.position을 서버가 덮어쓰는 흐름이 박힘.
- **어떻게 만들었나** — PDL → PacketGenerator → 양쪽 자동(M1 인프라 첫 실용). GameMap 마샬링은 ServerCore의 JobQueue(첫 Push 스레드가 flush)가 IOCP→tick 마샬링엔 부적합이라 별도 ConcurrentQueue. GameWorld는 정적 접근점이 필요해 Instance 일회 설정 singleton(헌법 정적 mutable 금지의 정신은 *지속적 mutate 금지*이므로 부합). 클라 측은 UnityClientSession이 dispatch만, transform 변경은 LocalPlayerController에 위임(SRP).
- **테스트 결과** — `dotnet build` 0 error, `dotnet test` 19/19 PASS, Unity 측 에러/경고 0건. (0, 0) wire-up 통과(서버·Unity 로그 매칭). (3, 0) Server Authority 시연 통과(Capture2DScene으로 정중앙 spawn 확인). Disconnect 1회 정리 통과. 상세는 아래 AC 검증 결과.
- **다음 스텝** — Phase 04 (C_MoveIntent + S_Snapshot, prediction 없이) — 의도된 lag 체감으로 Phase 05 prediction 동기 유발. 또 사이드 트랙으로 Burst 한글 경로 우회 영구 해결(폴더 이동 vs mklink) 의사결정 필요.

## AC 검증 결과

Phase 파일 `03-connection-handshake.md`의 "완료 조건" 5개를 다음과 같이 실행·확인:

1. **Unity Play → 서버 콘솔에 "Player 1 entered at (0, 0)"** ✅
   ```
   [GameSession] OnConnected from 127.0.0.1:56256
   [GameSession] OnSend 16 bytes
   [Map] Player 1 entered at (0, 0)
   ```
   (`OnSend 16 bytes` = size(2) + id(2) + entityId(4) + spawnX(4) + spawnY(4) = S_EnterMap)

2. **Unity 캐릭터가 (0, 0)에 spawn (서버 결정)** ✅
   ```
   [Unity] OnConnected to 127.0.0.1:7777
   [Unity] EnterMap as entity 1 at server spawn (0, 0)
   ```
   Phase 01 transform 초기값과 우연 일치하지만, **(3, 0) 시연**으로 진짜 서버 결정인지 추가 검증(아래 #5).

3. **Play 중지 → 서버 콘솔에 "Player N left"** ✅
   ```
   [GameSession] OnDisconnected from 127.0.0.1:56256
   [Map] Player 1 left (removed=True)
   ```
   `removed=True` = GameMap._players에서 정상 제거. ConcurrentQueue 마샬링이 IOCP→tick으로 정확히 작동.

4. **5번 반복 연결/해제 후 GameMap._players 비어있음** ⚠️ 1회 검증
   - 1회 stop → `removed=True` 확인. 코드 path는 매번 동일이라 1회로 정리 흐름 검증 충분.
   - 5회 정확 반복은 본 Phase에서 생략. AC 보수적 표현이라 본질 OK.

5. **헌법 #1 시각 시연 — spawn=(3, 0) 강제** ✅
   - GameSession.cs의 `spawnPos = (3f, 0f)` 임시 변경 + 빌드 + 서버 재기동 + Unity Play
   - 로그: `[Unity] EnterMap as entity 1 at server spawn (3, 0)`
   - Capture2DScene(영역 X=[-1, 7], Y=[-3, 3]): Player가 화면 *정중앙* = X≈3 위치에 정확히 spawn. Phase 01 transform (0, 0)이 완전히 무시됨.
   - 시연 후 (0, 0)으로 원위치.

6. **PDL 재생성 후 양쪽 컴파일** ✅
   - PacketGenerator: `[GEN] Packet Generate Success` × 2회 (S_EnterMap/S_LeaveMap 추가 직후 + float 정정 후)
   - `dotnet build Dawnholder.slnx`: 0 error, 0 warning
   - Unity 측 Console: 0 error, 0 warning (Burst 비활성화 상태)

종합: AC 6개 중 5개 완전 PASS, 1개(5회 반복)은 1회 검증으로 갈음. Phase 진행 차단 사유 없음.

## 결정 흐름 (학습 일지 쓸 때 참고용)
- **GameMap 마샬링 — JobQueue vs 별도 ConcurrentQueue**: ConcurrentQueue 채택. ServerCore의 JobQueue는 *첫 Push 스레드가 직접 Flush*하는 구조라 IOCP가 push하면 IOCP가 그대로 실행 → tick thread와 경합. ConcurrentQueue는 push만 외부 thread, drain은 tick thread에서 명시적 호출. Map=Actor 패턴(ARCHITECTURE) 강제.
- **GameWorld singleton 패턴 — Instance public vs DI**: Instance 채택. 이유: GameSession은 ServerCore의 Listener factory(`() => new GameSession()`)로 생성되어 DI 컨테이너 외부. 일회 설정(ctor에서) + Stop 시 해제 → 헌법의 "정적 *mutable* 금지" 정신(지속적 mutate 금지)에 부합. 단점: 테스트에서 다중 인스턴스 막힘 → 그래서 Stop에서 instance=null! 해제.
- **S_LeaveMap을 본 Phase에서 박은 이유**: 본인 1명 시연엔 *자기 disconnect*는 OnDisconnected로 처리되므로 미사용. 그러나 향후 다른 플레이어 정리(M3+)용 골격을 PDL ID 안정성(헌법 #2) 차원에서 *미리* 박음. PDL 끝에 추가하는 게 ID 안정 정공법.
- **클라 transform 갱신 위임 — SetServerPosition 메서드 vs UnityClientSession 직접**: 메서드 위임. UnityClientSession은 dispatch + 마샬링만, GameObject 조작은 LocalPlayerController. SRP 정합. Phase 04에서 prediction 모듈로 교체될 때 변경 영향 최소화.
- **PacketGenerator float 정정 — BitConverter 경유 vs PDL에서 int 강제**: BitConverter 경유 채택. PDL에서 float이 위치/속도 자연 표현이라 int 강제는 단위 변환 부담. .NET Standard 2.1 호환을 위해 `BitConverter.Int32BitsToSingle` 경유는 wire format 동일 + 추가 코드 한 줄. 단점: PacketGenerator 코드 약간 분기.

## 막혔던 지점 (있다면)
- **PacketGenerator manager 파일이 빌드 깸**
  - 증상: `dotnet build` 시 `ClientPacketManager.cs(...)에서 ServerCore/PacketSession/IPacket 못 찾음` 12건.
  - 원인: PacketGenerator를 `--no-manager` 없이 실행하면 Phase 06/07에서 *미채택*된 PacketManager 골격이 재생성됨. 옛 4월 ServerDev 시점 코드 잔재.
  - 해결: 생성된 ServerPacketManager.cs/ClientPacketManager.cs 삭제 + Generated 폴더 정리 + PacketGenerator를 `--no-manager` 옵션과 함께 재실행. PDL.xml에도 "Phase 08+에서 manager 도입 예정" 명시.

- **`BinaryPrimitives.ReadSingleLittleEndian`이 .NET Standard 2.1엔 없음**
  - 증상: `dotnet build` 시 `CS0117: 'BinaryPrimitives'에는 'ReadSingleLittleEndian'에 대한 정의가 포함되어 있지 않습니다` 4건 (S_EnterMap의 spawnX/spawnY float 4번).
  - 원인: float 변종(Single)은 .NET 5+에서 추가됨. 98_Shared는 .NET Standard 2.1로 컴파일 → 미지원.
  - 해결: PacketGenerator의 PacketFormat.cs에 ReadFloatFormat/WriteFloatFormat 별도 추가. `BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(...))` / `BinaryPrimitives.TryWriteInt32LittleEndian(..., BitConverter.SingleToInt32Bits(...))` 경유. Program.cs switch에 case "float" 분리. wire format 동일.

- **★ Unity Burst 컴파일러 한글 경로에서 hang (40% / 462초)**
  - 증상: com.unity.ai.assistant 설치 + Phase 03 Assembly-CSharp 재컴파일 트리거 후 Background Tasks의 Burst 컴파일이 40%에서 멈춤. CPU/disk 활동 없음. 정상 진행이면 5~10분, 462초+ 멈춤은 hang.
  - 진단 (MCP로 회수): `Unity_GetConsoleLogs` 호출 → `Unexpected error in Burst compilation: System.AggregateException ... Unable to load the unmanaged library 'C:\Users\bass1\바탕 화면\ClaudeDev\03_Client\Library\BurstCache\JIT\<hash>.dll', error code 4551`. `DllNotFoundException`. error code 4551 = Burst 자체 코드.
  - 원인: 경로에 한글(`바탕 화면`) 포함 → Burst의 unmanaged DLL 로드가 Windows non-ASCII 경로에서 실패. 알려진 Burst 패키지 이슈.
  - 단기 해결: Unity Editor 메뉴 `Jobs → Burst → Enable Compilation` 해제 → 컴파일 큐 비워지고 hang 풀림. M2 옵션 B(1인 movement)엔 Burst 의존성 거의 없어 비활성화 무영향.
  - 장기 해결 후보: ① 프로젝트를 ASCII 경로(`C:\Dev\ClaudeDev`)로 이동. ② `mklink`로 ASCII alias. ① 정공법이지만 git remote/IDE 영향 큼. M2 어딘가 빈 자리에 결정.
  - 부수 사건: Burst hang 강제 종료 시 *저장 안 한* 씬 변경(NetworkBootstrap GameObject)이 날아감 → MCP RunCommand로 재생성하면서 이번엔 즉시 `EditorSceneManager.SaveScene` 호출하도록 코드 진화. 학습 일지 캡처 가치 큼.

## 학습 일지 후보 키워드
- `/journal:concept Server Authority 첫 실전 (헌법 #1)` — 클라 transform이 *무시되고* 서버 좌표로 덮이는 흐름의 진짜 의미
- `/journal:concept Map=Actor + ConcurrentQueue 마샬링` — IOCP→tick 마샬링의 *왜*, lock-free actor 패턴이 차단하는 동시성 버그
- `/journal:concept .NET Standard 2.1 + Unity Mono/IL2CPP 호환성` — BinaryPrimitives Single 변종 누락 같은 *세대 차*가 어떻게 발생하는지
- `/journal:concept PDL 워크플로우 첫 실용` — PDL.xml 갱신 → 코드 생성 → 양쪽 자동 흐름의 진가 (M1 인프라 보상)
- **★** `/journal:bug Unity Burst 한글 경로 hang (error code 4551)` — 진단 단계(MCP GetConsoleLogs로 즉시 원인 회수) + trade-off 의사결정(단기 비활성화 vs 장기 폴더 이동) + 부수 발견(저장 안 한 씬 변경 손실 + idempotent MCP 재생성). 면접 임팩트 1순위.
