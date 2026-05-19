# M4 Backlog

> M3 5/20 emergency demo에서 의도적으로 미룬 전투/프로토콜/검증 후속.  
> M4 계획 분해 시 이 문서를 입력으로 사용한다.

---

## 목적

M3는 "보이기만 하면 OK" 응급 데모다. 다만 헌법 #1 서버 권위, #3 신뢰 경계, #5 틱 블로킹 금지는 지킨다.

아래 항목은 M3에서 단순화했지만 본 마감 품질로 올릴 때 다시 설계해야 하는 backlog다.

---

## Combat

### 1. Damage formula shared 분리

- 현재: 서버 단독 `CombatConstants` / fixed damage 10
- M4 목표: `98_Shared/GameData/Formulas.cs` 순수 함수로 damage formula 분리
- 이유:
  - 서버는 authoritative execution
  - 클라는 UI preview / tooltip / prediction에 같은 공식을 읽을 수 있음
  - 공식 중복과 drift 차단
- 주의:
  - RNG가 필요하면 seed는 server tick + entity id 계열로 설계
  - 클라가 damage를 authoritative로 계산하면 헌법 #1 위반

### 2. Damage formula full set

- 현재: fixed 10
- M4 목표:
  - attacker stat
  - target defense
  - skill coefficient
  - crit/block/resist 여부
  - min/max clamp
- 응급에서 미룬 이유: 5/20 demo는 HP 감소와 death 흐름 시연이 우선

### 3. Precision hitbox

- 현재: `dist² < range²` 원형/거리 판정
- M4 목표:
  - entity bounds
  - attack arc / capsule / rectangle
  - facing direction
  - vertical tolerance
- 주의:
  - client-provided facing/direction은 untrusted input
  - 서버 권위 position/facing만 사용

### 4. Lag compensation

- 현재: lag compensation 없음
- M4 목표:
  - position history ring buffer
  - ~200ms rewind
  - attack arrived tick 기준 target historical position 검사
- 근거:
  - root `CLAUDE.md` Gameplay Pillars에 combat position history가 박혀 있음
  - Phase 05 remote interpolation도 200ms buffer를 사용
- 주의:
  - rewind는 서버 저장 history만 사용
  - 클라가 보낸 position을 신뢰하면 헌법 #1/#3 위반

### 5. Enemy AI / movement

- 현재: enemy AI 없음, 고정 표적
- M4 목표:
  - patrol
  - aggro range
  - simple attack cooldown
  - server-side movement snapshot
- M3에서 미룬 이유:
  - enemy movement까지 넣으면 spawn, snapshot, animation, combat range flake가 동시에 늘어남

### 6. PvP support decision

- 현재: PvE enemy target만 응급 지원
- M4 결정 필요:
  - PvP를 M4에서 열지, M5+로 미룰지
  - player target attack 허용 여부
  - friendly fire / duel / arena 같은 룰 필요 여부
- 주의:
  - PvP는 trust boundary와 abuse surface가 훨씬 큼
  - rate-limit, ownership, range, cooldown, hitbox 검증을 먼저 안정화해야 함

---

## Trust Boundary / Anti-cheat

### 7. Cheat-flag table

- 현재: rate-limit 초과는 silent drop 위주
- M4 목표:
  - suspicious event logging
  - cheat flag persistence table
  - event type: rate-limit, invalid target, out-of-range repeated, stale/dead target spam
- 이유:
  - 헌법 #3은 의심 패턴 cheat-flag 로깅을 요구
  - M3는 demo 안정성을 위해 no-op만 구현
- 주의:
  - tick loop 안 동기 DB 호출 금지
  - persistence writer queue로 넘겨야 헌법 #5 정합

### 8. Attack reject feedback policy

- 현재: silent drop
- M4 목표:
  - silent drop 유지할 케이스
  - client UX feedback이 필요한 reject 케이스
  - cheat-flag만 남길 케이스
- 후보:
  - cooldown 중 공격: local animation only, server no-op
  - invalid target: no-op + cheat flag
  - protocol violation: disconnect 가능

---

## Prediction / Movement Interaction

### 9. Jump Y mispredict reconcile

- 출처: Phase 05 DONE known follow-up
- 증상: 본인 점프 연속 누름 시 Y축 reconcile 증가
- M4 목표:
  - jump edge 처리 재검토
  - client prediction dt와 server fixed tick 적분 정합
  - velocity reconcile smoothing 검토
- 전투 영향:
  - M3에서는 지상 공격 위주로 회피
  - M4 precision hitbox/lag compensation에서는 반드시 봉합 필요

### 10. Combat range vs vertical movement

- 현재: 단순 distance 판정
- M4 목표:
  - side-scroll Y축 tolerance 정의
  - jump 중 melee hit 허용 범위 결정
  - platform height 차이 처리
- 주의:
  - Y mispredict가 남아 있으면 전투 miss 체감으로 이어질 수 있음

---

## Protocol / Tooling

### 11. PacketGenerator explicit ID

- 현재: PDL append 순서대로 `++packetID`
- 문제:
  - `98_Shared/CLAUDE.md`에는 packet id range 예약이 있음
  - 실제 generator는 3000번대 combat range를 지원하지 않음
- M4 선택지:
  - A. PacketGenerator에 explicit id 속성 추가
  - B. range 예약 문서를 현재 append-only 정책에 맞게 정정
  - C. retired id registry만 유지하고 range 예약은 제거
- 주의:
  - stable PacketID와 retired ID 재사용 금지는 유지

### 12. Protocol schema evolution policy

- 현재: ProtocolVersion exact equality
- M4 목표:
  - additive packet 추가 시 bump 기준
  - field append 허용 기준
  - minor/major compatibility 여부
- M3 결정:
  - stale client cutoff가 중요해서 v3 bump

### 13. Packet smoke bots

- 현재:
  - `MultiRosterSmoke` 구현
  - `EmergencyCombatSmoke.md` spec
  - `BossStageClearSmoke.md` spec
- M4 목표:
  - combat smoke `.cs` 구현 정착
  - CI 또는 manual rehearsal command로 승격
  - failure reason을 PR body에 붙이기 쉬운 출력으로 정리

---

## Client / UI

### 14. HP UI authority boundary

- 현재: `S_HitResult.currentHp/maxHp` 표시 예정
- M4 목표:
  - UI state cache와 authoritative state 구분
  - damage number 표시
  - delayed death animation
- 주의:
  - 클라가 HP를 자체 계산하면 헌법 #1 위반

### 15. StageClear UX

- 현재: Phase 07 `S_StageClear` 수신 후 UI 표시 예정
- M4 목표:
  - fade timing
  - retry / next stage / return town flow
  - duplicate signal handling
- 주의:
  - 클라가 boss HP 0을 보고 StageClear 자체 판정하면 헌법 #1 위반

---

## Documentation / Review Follow-up

### 16. External review mini-Phase remaining items

- validate-shared-changes hook fix
- review-tiering 실측 문구 갱신
- settings 분리
- CI 별도 정리
- MessagePack 잔재 정정은 follow-up 1/5로 처리됨

### 17. Phase 06/07 learning keywords

- server-authoritative combat intent
- attacker-from-session-not-packet
- entity-spawn-target-identity
- protocol-version-bump-additive-demo
- hit-result-hp-update-collapse
- combat-rate-limit-silent-drop
- combat-map-actor-mutation
- boss-as-enemy-kind
- stage-clear-authority-event

M4 계획 분해 시 `CONTEXT_LearningJournalCandidates.md` 또는 phase DONE 문서에서 다시 회수한다.

---

## 우선순위 초안

| Priority | Item |
|---:|---|
| P0 | jump Y mispredict, damage formula shared 분리, cheat-flag queue 설계 |
| P1 | lag compensation, precision hitbox, packet explicit ID/doc alignment |
| P2 | enemy AI, PvP decision, richer HP/StageClear UX |

P0 기준: M4 combat 품질과 헌법 정합에 직접 영향이 있는 항목.
