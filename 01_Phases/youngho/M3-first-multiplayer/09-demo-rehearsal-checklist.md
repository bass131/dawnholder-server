# Phase 09: 데모 리허설 체크리스트

> **상태**: ready for rehearsal
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **면담**: 2026-05-20 13:40
> **담당 에이전트**: qa-sim + client

---

## 목표

2026-05-20 교수 중간 면담 직전 1회 풀-쓰루를 안정적으로 수행한다. 목표는 완성형 전투가 아니라 "서버 권위 멀티플레이 + enemy/boss placeholder + StageClear 흐름이 보인다"를 증명하는 것이다.

헤드리스 봇은 protocol/backend 정합을 증명하고, Unity는 visual/scene impact를 보여준다.

---

## 사전 점검 — 면담 1h 전

- [ ] PR #38 머지 후 `git checkout main`
- [ ] `git pull origin main`
- [ ] `dotnet build Dawnholder.slnx --nologo` PASS, 경고 0
- [ ] `dotnet test --no-build --nologo` PASS, 170 PASS / 1 SKIP / 0 FAIL
- [ ] `EmergencyCombatSmoke` fresh server 실측 PASS
- [ ] `BossStageClearSmoke` fresh server 실측 PASS
- [ ] Unity 클라 단독 접속 테스트
  - [ ] 정유현 prefab variant 체인 확인: `PlayerBase` / `LocalPlayer` / `RemotePlayer`
  - [ ] 3-zone visual 확인: 좌 마을 / 중 Normal enemy / 우 Boss
  - [ ] enemy/boss placeholder가 시각적으로 식별되는지 확인
- [ ] 서버 콘솔 로그 캡처 준비
  - [ ] 서버 실행 창
  - [ ] headless-bot 실행 창 2개 또는 터미널 history
  - [ ] 필요 시 Phase 06/07 DONE.md 실측 로그 열어둘 것

---

## 시연 순서

### 1. 서버 콘솔 실행

```powershell
dotnet run --project 02_Server/GameServer
```

기대:

```text
=== Dawnholder Server ===
Tick rate: 20 TPS (50ms)
Listening on 0.0.0.0:7777. Press Enter to stop.
```

### 2. Unity 클라 접속

보여줄 것:

- handshake v3 통과
- `S_EnterMap` 이후 local player 표시
- 3-zone visual 진입
- Normal enemy placeholder와 Boss placeholder 식별
- Remote/Local prefab variant 구조는 코드 설명 없이 화면 위주로 짧게 언급

주의:

- Unity combat dispatch가 아직 불완전하면 "시각 placeholder + 서버 backend smoke"로 분리해서 보여준다.
- 점프 Y축 mispredict 잔류 때문에 지상 이동/지상 공격 흐름 위주로 시연한다.

### 3. Phase 06 enemy combat smoke

```powershell
dotnet run --project 99_Tools/headless-bot -- --host 127.0.0.1 --port 7777 --scenario EmergencyCombatSmoke
```

예상 로그:

```text
[Bot] EmergencyCombatSmoke: success=True entity=3 target=1 hits=3 death=True
      hp: 30 -> 0 moveIntents=33 rateLimitDropped=True optionB=False
```

설명 포인트:

- 클라는 `C_Attack { targetEntityId }` 의도만 보낸다.
- 서버가 attacker, range, cooldown, damage, HP, death를 결정한다.
- 500ms rate-limit은 silent drop으로 검증됐다.

### 4. Phase 07 boss stage clear smoke

```powershell
dotnet run --project 99_Tools/headless-bot -- --host 127.0.0.1 --port 7777 --scenario BossStageClearSmoke
```

예상 로그:

```text
[Bot] BossStageClearSmoke: success=True entity=3 boss=2 hits=10 stageClear=True
      boss hp: 100 -> 0 moveIntents=113 death=True stageClearCount=1 duplicateSuppressed=True
```

설명 포인트:

- Boss는 `EnemyKind.Boss`인 `EnemyEntity`다.
- Boss HP 0에서 `S_EntityDeath` 후 `S_StageClear`가 1회 broadcast된다.
- 죽은 boss에 추가 attack을 보내도 duplicate StageClear가 없다.

### 5. Unity 화면으로 마무리

보여줄 것:

- placeholder와 3-zone visual
- server console tick 유지
- headless-bot PASS 로그

말할 것:

- "오늘 면담용 응급 데모는 backend authoritative flow가 핵심입니다."
- "Unity combat UI dispatch는 Phase 08b/08c 후속이고, 서버 packet과 smoke는 이미 통과했습니다."

---

## 면담 Q&A 준비

### 왜 클라가 데미지 계산을 안 하나?

서버 권위 원칙 때문이다. 클라는 attack intent만 보내고, 서버가 attacker identity, target 존재, range, cooldown, damage, HP, death를 계산한다. 이렇게 해야 클라 변조로 데미지나 stage clear를 위조할 수 없다.

### attacker 강제 패턴이 뭔가?

`C_Attack`에는 attacker id가 없다. 서버가 `GameSession`의 entity id를 attacker로 강제한다. 클라가 다른 player/entity id를 넣어 공격하는 표면을 없앤다.

### Trust Boundary는 어디서 지키나?

Phase 06 combat path는 6단계로 fail-closed 처리한다.

1. attacker player 존재
2. target enemy 존재
3. target alive
4. 500ms rate-limit
5. server-authoritative position 기반 `dist² < range²`
6. 통과 시에만 mutation + broadcast

응급 데모에서는 reject packet 대신 silent drop을 쓴다. M4에서는 cheat-flag 테이블로 의심 이벤트를 남긴다.

### Tick blocking은 어떻게 피했나?

handler는 packet decode만 하고 `GameSession.SubmitAttack`을 호출한다. 실제 mutation은 `GameMap.EnqueueJob`으로 map actor/tick thread에서 동기 처리한다. combat tick mutation path에는 `await`, `Task.Delay`, `Thread.Sleep`, DB call이 없다.

### Codex gamma 사전 검증의 가치는?

코드 진입 전에 HIGH 2건과 MEDIUM 3건을 봉합했다. 특히 `targetEntityId` 모델을 쓰려면 `S_EntitySpawn`으로 target id를 알려야 한다는 점, attacker는 packet이 아니라 session에서 강제해야 한다는 점을 사전에 잡았다.

### 왜 BossEntity를 따로 만들지 않았나?

응급 단계에서는 Boss AI/state machine이 없다. `EnemyKind.Boss`로 같은 `EnemyEntity`와 combat path를 재사용하고, StageClear trigger만 분기하는 편이 표면적이 작고 회귀 위험이 낮다. M4에서 EnemyType/AI 상태가 늘어나면 분리 여부를 다시 본다.

### 무엇을 M4로 미뤘나?

- lag compensation
- precision hitbox
- full damage formula
- cheat-flag persistence
- PvP
- enemy AI
- Phase 05 jump Y mispredict reconcile
- 진짜 4맵 구성

---

## Fallback

### Headless bot 실패 시

- `dotnet test --no-build --nologo` 170 PASS 결과를 먼저 보여준다.
- [06-server-combat-emergency-DONE.md](06-server-combat-emergency-DONE.md)와 [07-server-boss-stage-clear-DONE.md](07-server-boss-stage-clear-DONE.md)의 fresh server smoke 로그를 연다.
- 서버 콘솔에서 handshake/enter/tick 로그를 보여주며 backend 흐름을 설명한다.

### Unity 접속 실패 시

- 서버 콘솔 + headless-bot 2종으로 backend 시연을 진행한다.
- Unity는 정유현 prefab/3-zone visual 스크린샷 또는 scene view로 대체한다.
- 메시지: "visual integration은 Phase 08 후속이고, 오늘 핵심인 server-authoritative flow는 자동 smoke로 검증됐습니다."

### 시간이 부족할 때

우선순위:

1. `dotnet test --no-build --nologo`
2. `EmergencyCombatSmoke`
3. `BossStageClearSmoke`
4. Unity 3-zone visual
5. Q&A

---

## 완료 조건

- [ ] build PASS
- [ ] test 170 PASS / 1 SKIP
- [ ] EmergencyCombatSmoke PASS
- [ ] BossStageClearSmoke PASS
- [ ] Unity 단독 접속 + 3-zone visual 확인
- [ ] 면담 Q&A 답변 1회 구두 리허설

---

## 작업 로그

- 2026-05-18: pending (gamma 방식 3회차 Codex beta 권장 C 반영)
- 2026-05-19: Phase 06/07 server + smoke 2종 PASS 이후 면담 직전 리허설용 final checklist로 갱신
