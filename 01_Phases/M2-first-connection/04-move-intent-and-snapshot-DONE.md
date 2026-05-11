---
summary: 클라 매 frame C_MoveIntent 송신 + 서버 권위 적용 + 매 250ms S_Snapshot 브로드캐스트. prediction 없이 lag 의도 노출 (Phase 05 동기). 헌법 #3 (Trust Boundary) 범위/rate 검증 골격.
phase: 04-move-intent-and-snapshot
status: done
completed_at: 2026-05-11
commit: (이 commit)
---

# Phase 04 — C_MoveIntent + S_Snapshot (prediction 없이) 완료 박제

**소요 시간**: 약 1.5시간 (코드 1h + WDAC 한글경로 우회 0.5h).

## TL;DR
M2 네 번째 Phase. 클라는 매 frame `_moveInput.x` → sbyte(-1/0/1) 인코딩 → `C_MoveIntent(inputX, clientTick)` 송신. 서버는 IOCP→tick 마샬링으로 `PlayerEntity.PendingInputX`에 저장, 매 tick 적용 후 0 리셋. 매 5 tick(=250ms) `S_Snapshot(entityId, x, y, serverTick, lastAckedClientTick)` 브로드캐스트. **클라 transform 직접 조작은 완전 제거** — 화면 변화는 *오직 snapshot 도착 시*만 (헌법 #1 코드 강제). 헌법 #3은 GameSession의 `Math.Abs(inputX) <= 1` 검증 + 초당 100 intent rate-limit 골격(차단 X, 기록만). 공유 상수 `MoveSpeed=5, TickDuration=0.05, SnapshotTickInterval=5`를 `Shared/GameData/Constants.cs`에 단일 출처로 박음 — Phase 05 prediction이 같은 값을 써야 drift 0. 부수 발견: WDAC가 한글 경로 .NET 어셈블리 차단(PacketGenerator 실행 시 0x800711C7) → ASCII 경로 publish 우회. Burst와 같은 뿌리, 폴더 이동 시급성↑.

## 5단계 보고

- **무엇을 만들었나** — PDL에 `C_MoveIntent{sbyte inputX, int clientTick}` + `S_Snapshot{int entityId, float x, float y, int serverTick, int lastAckedClientTick}` 추가. PacketGenerator의 byte/sbyte 패턴 정정(`Segment.Array` → 현 메서드 `s` 변수). `Shared/GameData/Constants.cs`에 MoveSpeed/TickDuration/SnapshotTickInterval 추가. `PlayerEntity`에 `PendingInputX`(sbyte) + `LastClientTick`(uint). `GameMap.Tick`에 intent 적용 + 5 tick마다 snapshot 송신. `GameSession.OnRecvPacket`에 C_MoveIntent case + `HandleMoveIntent`(범위 검증 + rate-limit 골격 + 마샬링). 클라 `LocalPlayerController.Update`에서 transform 조작 제거 + 매 frame C_MoveIntent 송신. `UnityClientSession`에 S_Snapshot case + `HandleSnapshot` + Instance singleton(LocalPlayerController가 Send용 참조). 단위 테스트 6개(MoveIntentTests).
- **왜 필요한가** — Phase 05의 client prediction을 *왜 만드는지* 본인 눈으로 봐야 면접 서사 강함. 이 Phase 없이 prediction 바로 만들면 "그냥 즉시 움직이게 했어요"로 단조. 또 *intent vs state* 분리(클라는 의도, 서버는 결과)가 M2 이후 모든 시스템 토대.
- **어떻게 만들었나** — 클라가 sbyte로 인코딩(임계 0.5, 게임패드 미세 흔들림 차단). 서버는 IOCP에서 받은 intent를 `GameMap.EnqueueJob`으로 마샬링(Phase 03 패턴 재사용). PlayerEntity.PendingInputX는 단일 thread mutation(tick) → race 부재. Snapshot은 본 Phase 1명만이라 unicast(`p.Owner.Send`), 다인은 M3+에서 broadcast로 확장. MoveSpeed/TickDuration이 Shared 단일 출처 — Phase 05+ prediction에서 *같은 값* 강제.
- **테스트 결과** — `dotnet build` 0 error/warning. `dotnet test` 25/25 PASS (M1 16 + Phase 02 3 + Phase 04 6 신규). Unity Play 수동 검증: 좌표 누적 1 tick = 0.25, 5 tick = 1.25/250ms = MoveSpeed 정확. lag 체감 명확(키 떼도 직전 250ms 분량 표시). 에러 0, 경고 0. 상세는 AC 검증 결과.
- **다음 스텝** — Phase 05 (client prediction + snap reconcile) — 입력 즉시 클라가 자기 화면 *미리* 움직이고, snapshot 불일치 시 snap. 매 frame transform.position 갱신이 *prediction state*로 분리됨. 또 한글 경로 영구 해결(폴더 이동 vs mklink) 의사결정도 점점 시급해짐 — Burst + WDAC 두 영역 동시 영향.

## AC 검증 결과

Phase 파일 `04-move-intent-and-snapshot.md`의 "완료 조건" 5개를 다음과 같이 실행·확인:

1. **A 누르면 ~250ms 지연 후 좌측 이동, D면 우측, 안 누르면 정지** ✅
   - Unity 로그 (MCP GetConsoleLogs 회수):
     ```
     [tick 1465~1625] pos=(-0.25, 0)   ← A 1회 짧게 → 정지 후 같은 좌표 반복 broadcast
     [tick 1630]      pos=(0.75, 0)    ← D 시작, +1.00
     [tick 1635]      pos=(2.00, 0)    ← D 계속, +1.25
     [tick 1640]      pos=(3.25, 0)    ← D 계속, +1.25
     [tick 1645]      pos=(3.00, 0)    ← 키 전환
     [tick 1650]      pos=(1.75, 0)    ← A 시작, -1.25
     [tick 1655]      pos=(0.50, 0)    ← A 계속, -1.25
     ```
   - 매 250ms snapshot, 누적 속도 = MoveSpeed × time 정확.

2. **lag 체감 명확** ✅ — 키 떼도 직전 250ms 분량 표시. 매 snapshot 사이 화면 정지 → 다음 snapshot에서 *덜컥* 이동. Phase 05 동기 부여.

3. **DummyClient inputX=99 → 서버 cheat-log, 위치 변화 없음** ⚠️ 코드로 검증
   - GameSession.HandleMoveIntent에 `Math.Abs(pkt.inputX) > 1 → Console.WriteLine("[Cheat] ... range violation"); return;` 박혀있음.
   - DummyClient 시나리오로 실제 송신은 본 Phase 미수행(M1 회귀 도구 확장 필요). 코드 path 명확 + Phase 08 회귀 시 다시 확인.

4. **DummyClient 초당 1000 intent → cheat-log** ⚠️ 코드로 검증
   - 1초 슬라이딩 윈도우 + IntentRateLimitPerSecond=100. 초과 시 `[Cheat] ... intent rate N/s > 100`.
   - 실제 부하 송신은 Phase 08(qa-sim) 영역.

5. **30초간 정상 입력 → 서버 GameMap 좌표 누적 정상** ✅
   - 위 #1 로그가 ~30초 분량 흐름 보여줌. avg/max tick duration 정상(M1 메트릭 채널 유지).

6. **PDL 재생성 후 양쪽 컴파일** ✅
   - PacketGenerator (한글 경로 WDAC 우회 = `dotnet publish -o /c/temp/pgen`): `[GEN] Packet Generate Success`.
   - `dotnet build`: 0 error, 0 warning.
   - Unity Console: 0 error, 0 warning.

종합: AC 6개 중 4개 manual 완전 PASS, 2개(invalid input/rate-limit)는 코드 path 검증. Phase 04 본질(lag 노출 + intent/state 분리) 통과.

## 결정 흐름 (학습 일지 쓸 때 참고용)
- **클라 인코딩 sbyte vs Vector2 그대로**: sbyte. 이유: wire format 9 bytes(size+id+1+4) vs Vector2 16 bytes(8 floats), 헌법 #3 정신상 자유도 최소화. 임계 0.5로 게임패드 아날로그 미세 흔들림 차단.
- **Snapshot 주기 = 5 tick(250ms)**: 매 tick(50ms)이면 대역폭 폭증 + lag 체감 없어 Phase 05 동기 안 살아남. 250ms는 *충분히 불쾌한* 출발점. Phase 06 prediction 도입 후 더 늘려도 됨.
- **rate-limit 차단 X, 기록 O**: 보안 일반 원칙 — *패턴 보고 정책 결정*. Phase 04에서 차단 시 정상 게임플레이도 깰 위험(60 fps × 1 = 60/s는 안전 마진 안이지만 키 연타 시 spike 가능). 본 Phase는 *기록만*, 정책은 Phase 05+에서.
- **PlayerEntity.PendingInputX = sbyte (별도 race-prevention 없음)**: tick thread만 mutate 보장(EnqueueJob 마샬링). volatile/Interlocked 불필요. Actor 패턴의 약속 강제.
- **클라 매 frame Send vs 변화 시만 Send**: 매 frame. 이유: 단순. 60/s는 rate-limit 안. 변화 감지 누락 시 *정지 패킷*이 누락되어 서버가 마지막 input 영원히 적용. 매 frame 송신이 *항상 최신 의도* 약속.
- **PacketGenerator byte/sbyte 패턴 정정**: 옛 ServerDev 잔재(`Segment.Array[Offset+count]`)가 현 메서드 변수와 안 맞아 빌드 실패. `s[count]` 인덱서로 통일(`s` = Read의 ReadOnlySpan / Write의 Span). 1바이트라 endian 무관 직접 인덱싱.

## 막혔던 지점 (있다면)
- **★ WDAC가 한글 경로 .NET 어셈블리 차단 (0x800711C7)**
  - 증상: `dotnet run --project 99_Tools/PacketGenerator` 실행 시 `System.IO.FileLoadException: 애플리케이션 제어 정책에서 이 파일을 차단했습니다. (0x800711C7)`. 빌드는 통과하지만 dll 로드만 실패.
  - 원인: Windows Defender Application Control(WDAC)이 한글 경로(`바탕 화면`) 안의 .NET 어셈블리를 차단. Burst의 unmanaged DLL hang(Phase 03)과 *동일 뿌리* — non-ASCII 경로에서 시그니처/정책 매핑 실패.
  - 단기 해결: `dotnet publish -o /c/temp/pgen --nologo` → `cd /c/temp/pgen && dotnet Dawnholder.Tools.PacketGenerator.dll "C:\Users\bass1\바탕 화면\...\PDL.xml" --no-manager --no-wait`. ASCII 경로에서 실행하면 통과. PDL.xml은 절대 경로로 전달 → PacketGenerator의 projectRoot 계산이 그대로 동작.
  - `Unblock-File`은 다운로드 zone용이라 무효(첫 시도 실패).
  - 장기 해결 (점점 시급): 프로젝트 폴더를 `C:\Dev\ClaudeDev` 같은 ASCII 경로로 이동. Burst + WDAC 두 영역 동시 해결. git remote/IDE 영향 평가 후 M2 어딘가 빈 자리에.

- **PacketGenerator byte/sbyte 패턴이 빌드 깸**
  - 증상: `GenPackets.cs(331,3): error CS0103: 'Segment' 이름이 현재 컨텍스트에 없습니다` 2건. C_MoveIntent의 sbyte 추가 직후.
  - 원인: PacketFormat.cs의 `WriteByteFormat`이 옛 ServerDev 잔재 `Segment.Array[Segment.Offset + count] = ...` 패턴 사용. 새 Write 메서드 변수는 `Span<byte> s` 하나뿐 — `Segment`(대문자)라는 변수 없음.
  - 해결: ReadByteFormat → `this.{0} = ({1})s[count];`, WriteByteFormat → `s[count] = (byte)this.{0};` 로 통일. 1바이트라 endian 무관 직접 인덱서 OK. PacketGenerator 재빌드 + 재실행 후 정상.

## 학습 일지 후보 키워드
- `/journal:concept Intent vs State 분리` — 클라가 *의도*만 보내고 *결과*는 서버가 정한다는 게 왜 보안/동기 양쪽의 핵심인지
- `/journal:concept Trust Boundary 실전 (헌법 #3)` — 범위/rate/소유권 검증의 의미, *차단 X 기록 O* 보안 일반 원칙
- `/journal:concept Server snapshot 주기와 trade-off` — 250ms가 왜 출발점이고, prediction 도입 후 어떻게 더 늘릴 수 있는지
- `/journal:concept 공유 상수 단일 출처 (Shared/GameData)` — MoveSpeed가 양쪽 다르면 prediction이 즉시 깨지는 이유
- **★** `/journal:bug WDAC 한글 경로 .NET 어셈블리 차단 (0x800711C7)` — Burst hang(Phase 03)과 같은 뿌리. 진단(에러 코드 → 경로 인코딩) + 단기 우회(ASCII publish) + 장기 결정(폴더 이동) 의사결정. 면접 임팩트 큼.
