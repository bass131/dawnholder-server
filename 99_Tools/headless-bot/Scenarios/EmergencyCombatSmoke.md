# EmergencyCombatSmoke — Phase 06 시연 봇 시나리오 명세

> **상태**: spec only, 코드 X  
> **목적**: Phase 06 서버 응급 전투 완료 직후 `.cs`로 옮겨 자동 smoke 검증 진입  
> **소유 영역**: `99_Tools/headless-bot/Scenarios/`  
> **비충돌 약속**: `02_Server/`, `98_Shared/`, `99_Tools/PacketGenerator/`, Phase 06 문서, work-pin 수정 금지

---

## 목표

Phase 06의 최소 end-to-end 전투 흐름을 headless-bot으로 검증한다.

검증 대상은 "정밀 전투"가 아니라 5/20 면담용 응급 흐름이다.

- handshake version 3 통과
- enemy spawn 수신
- target entity id 확보
- `C_Attack` 송신
- 서버 권위 HP 감소 수신
- death 또는 HP 0 처리 수신
- rate-limit silent drop 확인

---

## 전제 패킷

Phase 06 Option A 기준:

```text
C_Attack { int targetEntityId }
S_EntitySpawn { int entityId, byte entityKind, float x, float y, int currentHp, int maxHp }
S_HitResult { int attackerEntityId, int targetEntityId, int damage, int currentHp, int maxHp }
S_EntityDeath { int entityId }
```

Phase 06 Option B 기준:

```text
C_Attack { int targetEntityId }
S_EntitySpawn { int entityId, byte entityKind, float x, float y, int currentHp, int maxHp }
S_HitResult { int attackerEntityId, int targetEntityId, int damage, int currentHp, int maxHp }
```

Option B에서는 `S_EntityDeath`가 없으므로 `S_HitResult.currentHp == 0`을 death-equivalent로 판정한다.

---

## CLI 예정

```powershell
dotnet run --project 99_Tools/headless-bot -- --host 127.0.0.1 --port 7777 --scenario EmergencyCombatSmoke
```

예상 출력:

```text
[Bot] EmergencyCombatSmoke: success=True entity=1 target=1000 hits=3 death=True
      hp: 30 -> 20 -> 10 -> 0
      rateLimitDropped=True
```

---

## 시나리오 흐름

### 1. Connect + Handshake

기존 `M2BasicMovement` / `MultiRosterSmoke` 패턴 재사용.

1. TCP connect
2. 즉시 `C_Handshake { clientVersion = ProtocolVersion.Current }` 송신
3. `S_HandshakeResult.ok == true` 확인
4. `S_EnterMap` 수신 후 `localEntityId` 저장

실패 조건:

- connect timeout 5s
- `S_HandshakeResult` timeout 5s
- handshake rejected
- `S_EnterMap` timeout 5s

### 2. Enemy Spawn 수신

`S_EntitySpawn`을 기다린다.

필수 assertion:

- `entityId > 0`
- `entityKind == Normal` 또는 Phase 06 정의 enum의 Normal 값
- `currentHp == maxHp` 또는 `0 < currentHp <= maxHp`
- `maxHp == 30`이면 best, 단 시연 튜닝 가능성 때문에 hard assert는 `maxHp > 0`

기록:

```text
targetEntityId = spawn.entityId
initialHp = spawn.currentHp
maxHp = spawn.maxHp
```

실패 조건:

- `S_EntitySpawn` timeout 5s
- `currentHp <= 0`
- `maxHp <= 0`

### 3. Happy Attack

`C_Attack { targetEntityId }`를 1회 송신한다.

기대:

- `S_HitResult.targetEntityId == targetEntityId`
- `S_HitResult.attackerEntityId == localEntityId`
- `damage > 0`
- `currentHp == initialHp - damage`
- `0 <= currentHp <= maxHp`

응급 고정 데미지 10이면 best assertion:

```text
damage == 10
currentHp == initialHp - 10
```

단 damage 튜닝 가능성을 고려해 smoke 기본은 `damage > 0`으로 두고, Phase 06 코드가 고정 10으로 확정되면 `damage == 10`으로 올린다.

### 4. Rate-limit Silent Drop

첫 hit 직후 500ms 안에 `C_Attack { targetEntityId }`를 한 번 더 보낸다.

기대:

- 250ms 동안 해당 target에 대한 추가 `S_HitResult` 없음
- HP 변화 없음
- `S_EntityDeath` 없음

판정:

```text
rateLimitDropped = true
```

주의:

- 테스트가 flake 나면 대기창을 300ms로 둔다.
- 500ms 이후 다음 정상 공격을 해야 하므로, drop 확인 뒤 `Task.Delay(550ms)`를 둔다.

### 5. Kill Flow

enemy가 죽을 때까지 다음 루프를 돈다.

```text
while currentHp > 0 and attackCount < 10:
    wait 550ms
    send C_Attack(targetEntityId)
    wait S_HitResult or S_EntityDeath
```

Option A 기대:

- 마지막 hit에서 `currentHp == 0`
- `S_EntityDeath.entityId == targetEntityId` 1회 수신
- 추가 300ms 동안 duplicate death 없음

Option B 기대:

- 마지막 `S_HitResult.currentHp == 0`
- `S_EntityDeath`가 없어도 성공

공통 실패 조건:

- `attackCount > 10`
- HP가 증가
- target id mismatch
- duplicate death 수신
- timeout

### 6. Optional Negative: Dead Target Re-attack

death 판정 후 `C_Attack { targetEntityId }`를 다시 보낸다.

기대:

- 300ms 동안 `S_HitResult` 추가 없음
- 300ms 동안 `S_EntityDeath` 추가 없음

이 검증은 Phase 06 자동 테스트의 `DuplicateDeath`와 겹치지만, 시연 smoke에서도 idempotency를 빠르게 확인한다.

---

## Result 모델 예정

```csharp
public class Result
{
    public bool Success;
    public string Reason = "";
    public int LocalEntityId;
    public int TargetEntityId;
    public int InitialHp;
    public int FinalHp;
    public int HitCount;
    public bool SawSpawn;
    public bool SawDeath;
    public bool RateLimitDropped;
    public bool UsedOptionBDeathEquivalent;
}
```

---

## 구현 위치 예정

```text
99_Tools/headless-bot/Scenarios/EmergencyCombatSmoke.cs
99_Tools/headless-bot/Program.cs
```

`Program.cs` 분기 예정:

```csharp
if (string.Equals(scenarioName, "EmergencyCombatSmoke", StringComparison.OrdinalIgnoreCase))
{
    EmergencyCombatSmoke.Result r = await EmergencyCombatSmoke.Run(host, port);
    ...
}
```

---

## 구현 시 재사용할 패턴

- `M2BasicMovement.Run(...)`
  - connect/handshake/enter-map 기본 흐름
  - generated packet decode switch
  - timeout result reason 작성
- `MultiRosterSmoke.BotProbe`
  - 여러 이벤트를 리스트로 저장
  - `WaitUntil(...)` polling helper
  - scenario별 result summary

---

## 주의사항

- bot은 클라 position을 보내지 않는다. 공격은 `targetEntityId`만 보낸다.
- `attackerEntityId`는 bot이 보내지 않는다. 서버가 session entity로 강제해야 한다.
- rate-limit 초과는 reject packet을 기대하지 않는다. 기대값은 "no HP change + no broadcast"다.
- Option B에서는 `S_EntityDeath`가 없어도 실패가 아니다.
- Phase 07 `S_StageClear`는 별도 시나리오에서 다룬다. Phase 06 smoke에 섞지 않는다.
- ProtocolVersion 3 bump 이후 stale Shared.dll이면 handshake에서 실패하는 게 정상이다.

---

## Phase 06 완료 직후 체크리스트

- [ ] generated packet 이름/필드명이 본 문서와 일치하는지 확인
- [ ] `ProtocolVersion.Current == 3` 기준으로 bot build 되는지 확인
- [ ] `S_EntityDeath` 구현 여부 확인 후 Option A/B mode 결정
- [ ] `.cs` 구현 후 `dotnet build Dawnholder.slnx --nologo`
- [ ] 서버 실행 후 `--scenario EmergencyCombatSmoke` 1회 통과
- [ ] 통과 로그를 Phase 06 DONE 또는 Phase 09 rehearsal에 붙이기
