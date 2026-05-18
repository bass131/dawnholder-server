# M3 Phase 03+04 — Codex Review (γ 방식 5회차)

- 검토 대상:
  - Phase 03: `4065616` (`feat(phase03): 핸들러 layer 분리...`)
  - Phase 04: `5ea1123` (`feat(phase04): 서버 broadcast...`)
- 현재 HEAD: `0af786f` (`docs(CHANGELOG): M3 Phase 03...`) 포함 상태에서 재검증
- 검토 일시: 2026-05-18

## 결론

Phase 03+04는 서버 런타임 기준으로 진행 가능하다.

`dotnet test`는 Codex 환경에서 재측정했고, 예상 baseline 증가(`151 → 160`)와 일치하게 회귀 0건이다. Phase 04의 핵심 의도인 `GameSession.IsClosing` + `GameMap.BroadcastToAll` skip도 구현 경로에는 일관 적용되어 있다.

다만 lifecycle race 테스트 1개는 현재 순서상 실제로 `IsClosing` skip 분기를 때리지 못해, “패턴이 깨졌을 때 실패하는 테스트”로는 약하다. 이 부분은 작은 보강을 권장한다.

## 실행 결과

### 전체 테스트

```text
dotnet test Dawnholder.slnx --nologo

Passed: 160
Failed: 0
Skipped: 1
Total: 161
```

판정: PASS. 사용자 머신 SAC On 차단으로 실측하지 못한 부분을 Codex 환경에서 재측정했고, Phase 04 예상치(`151 → 160`)와 맞는다.

### 관련 테스트 필터

```text
dotnet test Dawnholder.slnx --nologo --no-build --filter "FullyQualifiedName~BroadcastTests|FullyQualifiedName~HandlerTests"

Passed: 13
Failed: 0
Skipped: 0
```

판정: PASS.

### 형식 검사

```text
git diff --check HEAD
```

판정: PASS. whitespace error 없음.

주의: `dotnet test` 재빌드 과정에서 `03_Client/Assets/Plugins/Shared/Shared.dll`이 modified 상태가 되었다. 테스트 산출 side-effect로 보이며, 임의로 되돌리지는 않았다.

## Findings

### 1. Medium — lifecycle race 테스트가 현재는 `IsClosing` skip 분기를 직접 검증하지 못함

근거:

- `02_Server/GameServer.Tests/Network/BroadcastTests.cs:169`
- `02_Server/GameServer.Tests/Network/BroadcastTests.cs:180`
- `02_Server/GameServer.Tests/Network/BroadcastTests.cs:198`
- `02_Server/GameServer/Maps/GameMap.cs:62`
- `02_Server/GameServer/Maps/GameMap.cs:68`

`LifecycleRace_NewJoinBroadcastSkipsClosingSession`의 의도는 “disconnect 중인 session에는 신규 join broadcast가 들어가지 않는다”를 검증하는 것이다. 그런데 현재 테스트 흐름은 대략 다음 순서다.

```text
s1.OnDisconnected()
s2.OnConnected()
_map.Tick(2)
```

`GameMap.EnqueueJob`가 FIFO라면 tick에서 먼저 s1 cleanup이 처리되고, 그 다음 s2 enter가 처리된다. 이 경우 s1은 이미 `_players`에서 빠져 있으므로 `BroadcastToAll`의 `p.Owner.IsClosing` skip이 없어도 테스트가 통과할 수 있다.

즉 구현은 맞지만, 테스트가 구현의 핵심 안전망을 실제로 물고 있지는 않다. 이 테스트는 lifecycle 1순위 risk를 방어하는 회귀 테스트라서 false confidence를 줄이는 편이 좋다.

권장 보강:

```text
s1.OnConnected(); _map.Tick(1);
s2.OnConnected();        // s2 EnterGameWorld job 먼저 enqueue
s1.OnDisconnected();     // s1 cleanup job 나중 enqueue, 단 IsClosing=true는 즉시 세팅
_map.Tick(2);
```

이 순서면 s2 enter 처리 시점에 s1이 아직 map 안에 남아 있지만 `IsClosing=true`인 상태가 된다. 따라서 `BroadcastToAll` skip 및 initial roster skip이 실제로 동작해야만 테스트가 통과한다.

대안으로 `GameMap.BroadcastToAll`을 직접 호출하는 더 작은 단위 테스트를 추가해도 된다.

### 2. Low — 서버 CLAUDE 문서의 handler async 규칙이 현재 코드와 다름

근거:

- `02_Server/CLAUDE.md:36`
- `02_Server/GameServer/Handlers/IPacketHandler.cs:15`

문서는 “핸들러는 `async Task`”라고 되어 있지만, Phase 03 결과물의 실제 인터페이스는 다음 형태다.

```csharp
void Handle(GameSession session, ArraySegment<byte> buffer);
```

런타임 버그는 아니지만, Phase 03의 목적이 handler layer 규칙을 봉합하는 것이었으므로 문서가 반대로 남아 있는 것은 추후 구현자에게 잘못된 기준을 줄 수 있다.

권장 수정:

- 현재 설계를 유지한다면 문서를 “핸들러는 현재 sync `Handle(...)`; IO/long-running work는 tick loop 밖으로 격리”로 수정
- 정말 async handler가 목표라면 `IPacketHandler`와 dispatcher를 `Task` 기반으로 맞춤

현 상태에서는 sync handler가 더 자연스럽다. 현재 핸들러들은 파싱/검증/상태 enqueue 수준이라 async가 필요하지 않다.

### 3. Low — handler 테스트는 단위 테스트라기보다 dispatcher 통합 테스트에 가까움

근거:

- `02_Server/GameServer/Handlers/IPacketHandler.cs:15`
- `02_Server/GameServer.Tests/Network/HandshakeHandlerTests.cs`
- `02_Server/GameServer.Tests/Network/MoveIntentHandlerTests.cs`
- `02_Server/GameServer.Tests/Network/PingHandlerTests.cs`

핸들러가 `internal`이고 `InternalsVisibleTo`가 없어 테스트는 handler 인스턴스를 직접 때리지 않고 `GameSession.OnRecvPacket` 경유로 검증한다. 현재 범위에서는 괜찮다. 오히려 registry wiring까지 같이 검증하므로 Phase 03 emergency 목적에는 충분하다.

다만 combat, inventory, match state처럼 분기 많은 handler가 들어오면 `InternalsVisibleTo("GameServer.Tests")`를 추가해 handler direct test를 열 가치가 있다.

권장 판정:

- 지금 당장 필수 아님
- Phase 05~10에서 handler 복잡도가 올라가면 도입

## 요청 항목별 판정

### 1. dotnet test 재실측 + 회귀 0 확정

판정: PASS.

- 전체: `160 passed / 0 failed / 1 skipped / 161 total`
- 관련 필터: `13 passed / 0 failed / 0 skipped`
- 예상 baseline `151 → 160`과 일치

### 2. Phase 04 race 봉합 일관성

판정: 구현 PASS, 테스트 보강 권장.

구현 근거:

- `GameSession.IsClosing`: `02_Server/GameServer/Network/GameSession.cs:67`
- `BroadcastToAll`: `02_Server/GameServer/Maps/GameMap.cs:62`
- closing owner skip: `02_Server/GameServer/Maps/GameMap.cs:68`
- snapshot broadcast: `02_Server/GameServer/Maps/GameMap.cs:122`
- join broadcast: `02_Server/GameServer/Network/GameSession.cs:144`
- leave broadcast: `02_Server/GameServer/Network/GameSession.cs:300`

모든 서버 broadcast 경로가 `GameMap.BroadcastToAll`로 수렴한다.

- snapshot: 자기 포함 전원 broadcast
- player join: `except: self`
- player leave: `except: self`
- initial roster: broadcast가 아니라 self 대상 Send지만, `existingEntity.Owner.IsClosing` skip이 별도로 있음

즉 runtime path는 의도대로 정리됐다. 단, Finding 1처럼 race 테스트가 실제 skip 분기를 때리도록 보강하는 게 좋다.

### 3. initial roster 순서

판정: PASS.

근거:

- `02_Server/GameServer/Network/GameSession.cs:109`
- `02_Server/GameServer/Network/GameSession.cs:122`
- `02_Server/GameServer/Network/GameSession.cs:144`

`List<PlayerEntity> existing = new(map.Players);`가 `map.AddPlayer(self, player);`보다 먼저 실행된다. 따라서 신규 세션 자신은 initial roster snapshot에 포함되지 않는다.

이후 신규 접속자는 `map.BroadcastToAll(joinNotice.Write(), except: self)`로 기존 플레이어에게만 `S_PlayerJoin`을 보낸다. “자기에게 자기 PlayerJoin” 사고는 현재 구조에서 차단되어 있다.

### 4. PDL 변경 의무 3종 정합

판정: PASS.

`5ea1123`에 다음 3종이 함께 포함되어 있다.

- `99_Tools/PacketGenerator/PDL.xml`
- `98_Shared/Protocol/Generated/GenPackets.cs`
- `03_Client/Assets/Plugins/Shared/Shared.dll`

라인 근거:

- PDL `S_PlayerJoin`: `99_Tools/PacketGenerator/PDL.xml:102`
- PDL `S_PlayerLeave`: `99_Tools/PacketGenerator/PDL.xml:112`
- enum id 반영: `98_Shared/Protocol/Generated/GenPackets.cs:28`, `98_Shared/Protocol/Generated/GenPackets.cs:29`
- generated class 반영: `98_Shared/Protocol/Generated/GenPackets.cs:595`, `98_Shared/Protocol/Generated/GenPackets.cs:668`

PDL → generated source → Unity Shared.dll 반영 의무는 충족했다.

### 5. handler 단위 테스트 격리도 / `InternalsVisibleTo` 가치

판정: 지금은 보류 가능.

현재 handler는 작고, 테스트가 `OnRecvPacket`을 통해 dispatcher + handler 조합을 검증한다. Phase 03의 목표인 “switch 괴물 회귀 차단”에는 충분하다.

`InternalsVisibleTo`는 다음 조건 중 하나가 생기면 넣는 게 낫다.

- handler 내부 branch가 3개 이상으로 늘어남
- map/session/network fake 구성 비용이 테스트마다 커짐
- protocol parse는 성공했지만 domain rule만 독립 검증하고 싶어짐
- handler 실패 케이스가 transport side effect와 섞여 원인 추적이 어려워짐

즉 지금은 추가하지 않아도 되지만, M3 후반부 전투/상태 동기화 handler가 들어오면 도입 가치가 높다.

## 추가 관찰

### Unity client는 Phase 05 전제 조건이 남아 있음

근거:

- `03_Client/Assets/Scripts/Network/UnityClientSession.cs:126`
- `03_Client/Assets/Scripts/Network/UnityClientSession.cs:190`
- `03_Client/Assets/Scripts/Network/UnityClientSession.cs:205`

현재 Unity client는 `S_PlayerJoin`, `S_PlayerLeave` dispatch가 아직 없고, `S_Snapshot`도 `LocalPlayerController.Instance.OnServerSnapshot(...)`로만 전달한다. 서버가 이제 snapshot을 전원에게 broadcast하므로, Phase 05에서 entityId 기반 routing을 반드시 넣어야 한다.

이는 Phase 04 서버 검토 실패 사유는 아니다. 다만 두 Unity client demo를 바로 붙이면 remote entity snapshot을 local predictor가 먹는 형태가 될 수 있으므로, Phase 05를 건너뛰면 안 된다.

## 권장 후속 조치

1. `LifecycleRace_NewJoinBroadcastSkipsClosingSession`을 job 순서상 실제 `IsClosing` skip이 필요한 케이스로 보강한다.
2. `02_Server/CLAUDE.md`의 handler async 문구를 현재 sync handler 설계에 맞게 수정한다.
3. `InternalsVisibleTo("GameServer.Tests")`는 지금은 보류하고, 분기 많은 handler가 들어오는 시점에 추가한다.
4. Phase 05에서 Unity `S_PlayerJoin`/`S_PlayerLeave` dispatch와 `S_Snapshot.entityId` routing을 먼저 처리한다.

## 최종 판정

Phase 03+04는 merge/proceed 가능하다.

Blocking runtime defect는 발견하지 못했다. 남은 것은 회귀 테스트의 정확도 보강 1건과 문서 정합성 1건이다.
