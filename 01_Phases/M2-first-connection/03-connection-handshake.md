# Phase 03: 접속 핸드셰이크 — Enter/Leave 패킷

> **상태**: pending
> **마일스톤**: M2 First Connection
> **예상 소요**: 2시간
> **담당 에이전트**: netcode (+ gameplay 살짝)

---

## 🎯 목표

Unity 클라가 서버에 연결되면 자동으로 `S_EnterMap`을 받아 자기 캐릭터의 **초기 위치를 서버가 정해서** 알려준다. 클라는 그 좌표로 PlayerView를 배치한다. 연결 끊기면 서버가 자동 정리. **헌법 #1(Server Authority)의 첫 실전 적용** — 클라가 자기 좌표를 결정하지 않는다.

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (Unity 캐릭터 로컬 이동)
- [ ] Phase 02 완료 (서버 GameLoop + GameMap)

---

## 📝 작업 내용

- [ ] **PDL 정의 추가** (`99_Tools/PacketGenerator/PDL.xml` 또는 동등 위치):
    - `S_EnterMap { int entityId; float spawnX; float spawnY; }` (server → client)
    - `S_LeaveMap { int entityId; }` (server → client, 추후 다른 플레이어 정리용 — 지금은 본인만)
- [ ] PacketGenerator 재실행 → `98_Shared/Protocol/Generated/GenPackets.cs` 갱신
- [ ] `02_Server/GameServer/Network/GameSession.cs` — `OnConnected`에서 `GameMap.AddPlayer(this)` 호출. 결과로 받은 `PlayerEntity`에서 spawn 좌표 읽어 `S_EnterMap` 전송.
- [ ] `02_Server/GameServer/Maps/GameMap.cs` — `AddPlayer(GameSession)` → `PlayerEntity` 생성(EntityId 자동 발급, Position = (0, 0)). `RemovePlayer(EntityId)`.
- [ ] `02_Server/GameServer/Handlers/PacketDispatcher.cs` — 받은 패킷의 PacketID → handler 매핑 (S_*는 클라용이라 서버 dispatcher엔 C_* 만. 지금은 dispatch 받을 게 없지만 골격을 세움).
- [ ] **클라 측** (`03_Client/Assets/Scripts/Network/`):
    - `S_EnterMap` 핸들러 — 받은 좌표로 Player GameObject 배치. **MainThreadDispatcher 경유** 필수.
    - `OnDisconnected` 콜백에서 화면 정리 (옵션 — 지금은 로그만).
- [ ] 서버 콘솔 로그: `[Map] Player {entityId} entered at ({x}, {y})` / `[Map] Player {entityId} left`.

---

## ✅ 완료 조건

- [ ] Unity Play → 서버 콘솔에 "Player N entered at (0, 0)" 한 줄
- [ ] Unity 캐릭터가 (0, 0)에 spawn (Phase 01에서 직접 박은 좌표가 아니라 서버가 정함)
- [ ] Play 중지 → 서버 콘솔에 "Player N left"
- [ ] 5번 반복 연결/해제 후 GameMap 안의 `_players`가 비어있음 (메모리 누수 없음)
- [ ] PDL 재생성 후 클라/서버 모두 컴파일 성공

---

## 🧪 테스트

**자동 테스트:**
- `GameServer.Tests/Maps/GameMapTests.cs` — AddPlayer × 10 → RemovePlayer × 10 → `_players.Count == 0`.
- 라운드트립 회귀 (M1 Phase 07 패턴 재사용): DummyClient로 connect → S_EnterMap 수신 확인.

**수동 테스트:**
- Unity Play → 정상 spawn 확인
- 서버를 spawn 좌표를 (10, 0)으로 강제 변경 → Unity에서 캐릭터가 (10, 0)에 뜨는지 확인 (Server Authority 시연)

---

## 📚 학습 포인트

- **헌법 #1 실전**: 클라가 자기 spawn 좌표를 결정하지 않는다. 이게 권위 서버의 첫 발걸음.
- **PDL 워크플로우**: XML 한 줄 추가 → 코드 생성 → 양쪽 동시 갱신. M1에서 깐 파이프라인의 실용 첫 사용.
- **MainThreadDispatcher**: Unity API(transform.position 등)는 main thread에서만. socket 콜백은 IOCP thread에서 옴 → dispatcher 경유 필수. 안 하면 크래시.
- **packet ID 안정성** (헌법 #2): 한 번 박은 ID는 영구. 은퇴 시키더라도 재사용 X.

---

## ⚠️ 함정 / 주의사항

- PDL 갱신 후 PacketGenerator 재실행 안 하면 양쪽 코드가 옛 시그니처 — 컴파일 에러로 잡힘.
- Unity socket 콜백에서 `gameObject.transform.position = ...` 바로 하면 Editor 강제 종료 → 반드시 dispatcher.
- `OnDisconnected`가 IOCP thread에서 호출 → `_players.Remove` 시 GameMap의 tick thread와 경합 우려. **AddPlayer/RemovePlayer도 JobQueue 통해 tick thread에서 실행**해야 lock-free.
- spawn 좌표를 클라 코드 어디에서 0으로 초기화해두면 "서버가 안 보내도 우연히 맞아 보임" 함정 → 일부러 `(-999, -999)` 초기값 두고 서버 좌표로 덮이는지 확인.

---

## ➡️ 다음 Phase

- Phase 04: C2S_MoveIntent + S2C_Snapshot — 입력 전송 + 서버 권위 적용 + 위치 브로드캐스트 (prediction 아직 없음)

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
