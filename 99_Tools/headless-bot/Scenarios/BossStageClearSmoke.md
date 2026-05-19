# BossStageClearSmoke — Phase 07 시연 봇 시나리오 명세

> **상태**: spec only, 코드 X  
> **목적**: Phase 07 서버 보스 + StageClear 완료 직후 `.cs`로 옮겨 자동 smoke 검증 진입  
> **소유 영역**: `99_Tools/headless-bot/Scenarios/`  
> **비충돌 약속**: `02_Server/`, `98_Shared/`, `99_Tools/PacketGenerator/`, Phase 06/07 정의 본문, work-pin 수정 금지

---

## 목표

Phase 07의 최소 end-to-end 보스 처치 + StageClear 흐름을 headless-bot으로 검증한다.

검증 대상은 "보스 AI"가 아니라 5/20 면담용 응급 흐름이다.

- handshake version 3 통과
- boss spawn 수신
- boss target entity id 확보
- `C_Attack` 반복 송신
- 서버 권위 HP 감소 수신
- boss death 처리 수신
- `S_StageClear` 1회 수신
- death 후 추가 attack에도 StageClear 중복 broadcast 없음

---

## 전제 패킷

Phase 06 기반:

```text
C_Attack { int targetEntityId }
S_EntitySpawn { int entityId, byte entityKind, float x, float y, int currentHp, int maxHp }
S_HitResult { int attackerEntityId, int targetEntityId, int damage, int currentHp, int maxHp }
S_EntityDeath { int entityId }           // Phase 06 Option A일 때
```

Phase 07 추가:

```text
S_StageClear { int bossEntityId }         // ID 15 예정
```

Phase 06 Option B에서는 `S_EntityDeath`가 없을 수 있으므로 `S_HitResult.currentHp == 0`을 boss-death-equivalent로 판정한다. `S_StageClear`는 Option B에서도 별도 유지한다.

---

## CLI 예정

```powershell
dotnet run --project 99_Tools/headless-bot -- --host 127.0.0.1 --port 7777 --scenario BossStageClearSmoke
```

예상 출력:

```text
[Bot] BossStageClearSmoke: success=True entity=1 boss=1001 hits=10 stageClear=True
      boss hp: 100 -> ... -> 0
      stageClearCount=1 duplicateSuppressed=True
```

---

## 시나리오 흐름

### 1. Connect + Handshake

`EmergencyCombatSmoke`와 같은 흐름을 재사용한다.

1. TCP connect
2. 즉시 `C_Handshake { clientVersion = ProtocolVersion.Current }` 송신
3. `S_HandshakeResult.ok == true` 확인
4. `S_EnterMap` 수신 후 `localEntityId` 저장

실패 조건:

- connect timeout 5s
- `S_HandshakeResult` timeout 5s
- handshake rejected
- `S_EnterMap` timeout 5s

### 2. Boss Spawn 수신

`S_EntitySpawn` 중 boss entity를 찾는다.

필수 assertion:

- `entityId > 0`
- `entityKind == Boss` 또는 Phase 07 정의 enum의 Boss 값
- `currentHp == maxHp` 또는 `0 < currentHp <= maxHp`
- `maxHp >= 100`이면 best, 단 시연 튜닝 가능성 때문에 hard assert는 `maxHp > 0`

기록:

```text
bossEntityId = spawn.entityId
initialBossHp = spawn.currentHp
bossMaxHp = spawn.maxHp
```

실패 조건:

- boss `S_EntitySpawn` timeout 5s
- `currentHp <= 0`
- `maxHp <= 0`

주의:

- Phase 06 normal enemy도 같이 spawn될 수 있다. `entityKind`로 boss만 골라야 한다.
- `S_EntitySpawn` 순서에 의존하지 않는다.

### 3. Boss Attack Loop

boss가 죽을 때까지 550ms 간격으로 공격한다.

```text
while bossCurrentHp > 0 and attackCount < 20:
    send C_Attack(bossEntityId)
    wait S_HitResult for boss or S_EntityDeath or S_StageClear
    wait 550ms before next attack
```

기대:

- `S_HitResult.targetEntityId == bossEntityId`
- `S_HitResult.attackerEntityId == localEntityId`
- `damage > 0`
- HP는 증가하지 않음
- 마지막 hit에서 `currentHp == 0` 또는 `S_EntityDeath.entityId == bossEntityId`

응급 고정 데미지 10이면 best assertion:

```text
hitCount == 10 when bossMaxHp == 100
damage == 10
```

단 smoke 기본은 튜닝 여지를 남겨 `damage > 0`, `attackCount < 20`으로 둔다.

### 4. StageClear 수신

boss death-equivalent 이후 `S_StageClear`를 기다린다.

필수 assertion:

- `S_StageClear.bossEntityId == bossEntityId`
- `stageClearCount == 1`
- 수신 timeout 5s 이내

실패 조건:

- `S_StageClear` timeout
- boss id mismatch
- `S_StageClear`가 boss death 전 먼저 옴
- stage clear count 0 또는 2 이상

### 5. Duplicate Suppression

StageClear 수신 후 같은 `bossEntityId`로 `C_Attack`을 1회 더 보낸다.

기대:

- 500ms 동안 추가 `S_StageClear` 없음
- 500ms 동안 추가 boss `S_HitResult` 없음
- Option A면 추가 `S_EntityDeath` 없음

판정:

```text
duplicateSuppressed = true
```

---

## Result 모델 예정

```csharp
public class Result
{
    public bool Success;
    public string Reason = "";
    public int LocalEntityId;
    public int BossEntityId;
    public int InitialBossHp;
    public int FinalBossHp;
    public int HitCount;
    public int StageClearCount;
    public bool SawBossSpawn;
    public bool SawBossDeath;
    public bool SawStageClear;
    public bool DuplicateSuppressed;
    public bool UsedOptionBDeathEquivalent;
}
```

---

## 구현 위치 예정

```text
99_Tools/headless-bot/Scenarios/BossStageClearSmoke.cs
99_Tools/headless-bot/Program.cs
```

`Program.cs` 분기 예정:

```csharp
if (string.Equals(scenarioName, "BossStageClearSmoke", StringComparison.OrdinalIgnoreCase))
{
    BossStageClearSmoke.Result r = await BossStageClearSmoke.Run(host, port);
    ...
}
```

---

## 구현 시 재사용할 패턴

- `EmergencyCombatSmoke`
  - combat packet decode
  - HP 감소 검증
  - Option A/B death-equivalent 처리
- `MultiRosterSmoke.BotProbe`
  - 이벤트 리스트 저장
  - `WaitUntil(...)` polling helper
  - scenario result summary
- `M2BasicMovement.Run(...)`
  - timeout reason 작성
  - connect/handshake/enter-map 흐름

---

## 주의사항

- boss는 client가 직접 판정하지 않는다. `S_StageClear`가 권위 신호다.
- `S_EntityDeath`와 `S_StageClear`는 다른 의미다. death는 entity lifecycle, StageClear는 stage/game event다.
- Phase 06 normal enemy와 boss를 `entityKind`로 구분한다.
- boss respawn은 기대하지 않는다. 응급은 1회성이다.
- death 후 추가 attack은 disconnect/reject packet을 기대하지 않는다. 기대값은 no-op + duplicate broadcast 없음이다.
- StageClear UI 표시는 Unity Phase 08b 영역이다. 이 smoke는 packet 수신만 확인한다.

---

## Phase 07 완료 직후 체크리스트

- [ ] generated `S_StageClear` 필드명이 본 문서와 일치하는지 확인
- [ ] boss `entityKind` 값 확인
- [ ] `ProtocolVersion.Current == 3` 기준으로 bot build 되는지 확인
- [ ] `.cs` 구현 후 `dotnet build Dawnholder.slnx --nologo`
- [ ] 서버 실행 후 `--scenario BossStageClearSmoke` 1회 통과
- [ ] 통과 로그를 Phase 07 DONE 또는 Phase 09 rehearsal에 붙이기
