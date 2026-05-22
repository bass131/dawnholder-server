---
owner: youngho
milestone: M4.1
phase: 02
title: 데미지 공식 → 98_Shared/GameData/Formulas.cs 순수 함수 분리
status: pending
grade: 보통
risk: low
estimated: 1~2h
domain: shared+server
---

# Phase 02: 데미지 공식 → `98_Shared/GameData/Formulas.cs` 순수 함수 분리

> **상태**: pending
> **마일스톤**: M4.1
> **등급**: 보통 (2 도메인 shared+server / ~50줄 / 가역적)
> **담당**: server SubAgent (Sonnet) + shared SubAgent (Sonnet, Protocol 정합 점검)

---

## 🎯 목표

**M3 응급 박힌 `CombatConstants.BaseDamage=10` 고정 데미지를 `98_Shared/GameData/Formulas.cs` 순수 함수로 분리** + 스탯/방어 반영 기초 박음. 클라/서버 공유 코드 정합 (헌법 #4 "복사-붙여넣기 금지" 정신).

본 Phase가 끝나면 = (a) `98_Shared/GameData/Formulas.cs` 신설, (b) `ComputeDamage(attackerStats, targetStats, baseDamage) → int` 순수 함수 박힘, (c) `GameMap.ProcessAttack`에서 Formulas 위임, (d) 단위 테스트 5건+ 통과.

---

## ⏪ 사전 조건

- [ ] Phase 01 (Codex 크로스 리뷰) 마감 — 발견 항목 처리 결정 박힘
- [ ] **M3.8 Phase 03 마감 (PlayerStats 박힘, 2026-05-22 결정)** — 본 Phase Formulas.cs는 M3.8 박힌 `PlayerStats` 흡수 의무. M3.8 미마감 상태에서 본 Phase 진입 X (의존성 직결)
- [x] `98_Shared/` .NET Standard 2.1 빌드 환경 (ADR-010)
- [x] `CombatConstants.BaseDamage=10` 현재 위치 인지 (`02_Server/GameServer/Combat/CombatConstants.cs:26`)
- [x] PacketGenerator 재생성 의무 인지 (Shared 변경 시, ADR-002)

---

## 📝 작업 내용

### 1단계: `98_Shared/GameData/Formulas.cs` 신설

- [ ] `98_Shared/GameData/` 디렉토리 확인 (이미 박혀있으면 OK, 없으면 신설)
- [ ] `Formulas.cs` 신설 — `public static class Formulas` (헌법 #1 정합 — readonly static만)
- [ ] **`PlayerStats`는 M3.8 Phase 03에서 박힌 클래스 재활용** (`02_Server/GameServer/Combat/PlayerStats.cs` — 전사/원거리 분기 `{ Class, Hp, MaxHp, Attack, Defense, MoveSpeed }`). 본 Phase에선 *PlayerStats 활용*만, 신설 X.
- [ ] `EnemyStats` struct 정의 (defense/maxHp — defense 0이면 응급 정합) — 본 Phase에서 신설
- [ ] `ComputeDamage(PlayerStats attacker, EnemyStats target, int baseDamage) → int` 시그니처 박음 — M3.8 PlayerStats `Attack`/`Defense` 필드 활용
- [ ] 응급 공식 = `Math.Max(1, baseDamage + attacker.Attack - target.Defense)` (최소 1 데미지 보장). 전사(Attack=15) / 원거리(Attack=12) 정합 = baseDamage 10 기준 데미지 차이 자연 발생
- [ ] xmldoc 박음 (헌법 #1 정합 명시 — 본 함수는 *서버 권위 판정용*, 클라가 *스탯 hint 표시* 목적 호출은 OK)

### 2단계: `PlayerEntity` / `EnemyEntity` 스탯 필드 연결

- [ ] `PlayerEntity.Stats` (PlayerStats 타입) — **M3.8 Phase 03에서 박힘 `GameSession._stats` 활용**. `PlayerEntity` 생성 시 `GameSession.Stats`를 ctor 인자로 받음. default = `PlayerStats.Warrior()` (M3.8 정합)
- [ ] `EnemyEntity.Stats` (EnemyStats 타입, ctor에서 default 박힘 — Defense=0 응급)
- [ ] 스탯 *변경* 메서드는 박지 않음 (M5 영속화 도입 시 박을 영역, 헌법 #1 정합)

### 3단계: `GameMap.ProcessAttack` Formulas 위임

- [ ] `GameMap.cs:189` — `target.Hp -= CombatConstants.BaseDamage` → `int damage = Formulas.ComputeDamage(attacker.Stats, target.Stats, CombatConstants.BaseDamage); target.Hp -= damage;`
- [ ] `S_HitResult.damage` 필드도 `damage` 변수 박음 (Formulas 결과)
- [ ] `CombatConstants.BaseDamage` 자체는 *남김* — Formulas의 base 입력으로 활용 (Phase 03 lag comp와 conflict X)
- [ ] xmldoc 갱신 (Formulas 위임 흔적 박음)

### 4단계: Shared.dll 재빌드 + Unity 측 자동 복사 확인

- [ ] `dotnet build Dawnholder.slnx` (Shared.csproj의 CopyToUnityPlugins PostBuild target 작동)
- [ ] `03_Client/Assets/Plugins/Shared/Shared.dll` 새 빌드 commit 확인 (`.gitignore` 화이트리스트 정합)
- [ ] Unity 측 빌드 검증 (`unity-bridge` SubAgent 위임 가능 — batchmode compile)

### 5단계: 단위 테스트 박음

- [ ] `02_Server/GameServer.Tests/Combat/FormulasTests.cs` 신설
- [ ] 테스트 6건+ (M3.8 PlayerStats 흡수 후 캐릭터 클래스별 검증 추가):
  1. `ComputeDamage_WarriorHappyPath` — PlayerStats.Warrior(Attack=15)/target(Defense=2)/base(10) → 23
  2. `ComputeDamage_RangerHappyPath` — PlayerStats.Ranger(Attack=12)/target(Defense=2)/base(10) → 20
  3. `ComputeDamage_TargetDefenseHigh` — Warrior/target(Defense=30)/base(10) → 1 (최소 1 보장)
  4. `ComputeDamage_AttackerZeroStats` — PlayerStats(Attack=0/Defense=0)/target(0)/base(10) → 10
  5. `ComputeDamage_NegativeStatsOverflow` — 음수 입력 시 safe (overflow X)
  6. `ComputeDamage_LargeStats` — int.MaxValue 근처 입력 시 overflow 검증
- [ ] dotnet test green (M3 baseline 회귀 0)

### 6단계: PDL 정합 점검

- [ ] `S_HitResult.damage` 필드 = `int` 박혀있는지 확인 (PDL XML)
- [ ] Formulas 결과가 `int` 반환 정합 — PDL 변경 불필요 (응급 정합)
- [ ] Phase 03에서 PDL 변경 예정 (`C_Attack.attackerClientTick`) — 본 Phase는 *Formulas 분리만*

---

## ✅ 완료 조건

- [ ] `98_Shared/GameData/Formulas.cs` 신설 + `ComputeDamage` 순수 함수 박힘
- [ ] **`PlayerStats`는 M3.8 박힘 재활용** (신설 X), `EnemyStats` struct 박힘 (응급 = defense/maxHp 2개 필드)
- [ ] `PlayerEntity.Stats` = `GameSession.Stats` 연결 (M3.8 PlayerStats 흡수)
- [ ] `GameMap.ProcessAttack` Formulas 위임 박힘
- [ ] `S_HitResult.damage` = Formulas 결과 정합
- [ ] dotnet test green + 새 단위 테스트 6건+ 통과 (전사/원거리 분리 검증 포함)
- [ ] Shared.dll 새 빌드 commit (Unity 측 자동 복사)
- [ ] Unity batchmode compile green (`unity-bridge` SubAgent 확인)
- [ ] 본 Phase 보통 등급 = -DONE.md 없음, work-pin + commit message 충분

---

## 🧪 테스트

**자동**:
- `FormulasTests.cs` 5건+ (위 박힘)
- 기존 `AttackHandlerTests` / `BossStageClearTests` 회귀 0 확인 (Formulas 위임 후도 damage 값 정합)

**수동**:
- 헤드리스 봇 smoke 실행 (`99_Tools/headless-bot/`) — Normal enemy hp 30 → 0 (3 hit) + Boss hp 100 → 0 (10 hit) 정합 (응급 default stats 0/0 → BaseDamage 10 그대로)

---

## 📚 학습 포인트

- **순수 함수의 가치** — `static int ComputeDamage(...)` = side-effect 없음 + 입력만으로 결정 = 단위 테스트 용이. 함수형 프로그래밍 기본 정신.
- **헌법 #4 "복사-붙여넣기 금지" 정신 정합** — Formulas 한 곳에 박으면 클라/서버 동일 결과 보장. 클라는 *hint 표시*용, 서버는 *권위 판정*용 — 같은 함수 호출.
- **PDL 변경 없는 Shared 변경 패턴** — 본 Phase = 새 struct + 새 함수만 박음, PDL 패킷 모양 변경 X. ProtocolVersion bump 불필요 (헌법 #2 정합). Phase 03에서 PDL 변경.
- **응급 default stats (0/0)의 가치** — 새 필드 박지만 *기본값이 옛 행동 그대로* (BaseDamage 10 정확). 후속 마일스톤에서 점진 도입 가능 = 학부생 호흡 정합.

---

## ⚠️ 함정 / 주의사항

- **Shared 변경 후 Shared.dll commit 누락** — CHANGELOG 2026-05-17 학습 정합 (정유현 pull 사고). PacketGenerator 재생성 불필요 (PDL 변경 X)이지만 *Shared.dll 새 빌드 commit*은 의무. `.gitignore` 화이트리스트 정합.
- **클라가 Formulas 활용해서 자체 damage 판정 함정** — 헌법 #1 위반. Formulas는 *서버 권위* + 클라 *hint 표시*만. 클라 코드 review 시 `Formulas.ComputeDamage` 호출 후 `PlayerEntity.Hp -=` 패턴 발견하면 즉시 차단.
- **PlayerStats overflow 함정** — int 입력 시 음수 + 큰 양수 조합 overflow 위험. `Math.Max(1, ...)` 최소 보장 + 단위 테스트 4·5번이 검증.
- **CombatConstants.BaseDamage 제거 함정** — 본 Phase는 *위임*만, BaseDamage 자체는 남김. 제거 시 Formulas 입력 hardcoded 12 같은 함정. 의도적 보존.

---

## ➡️ 다음 Phase

- **Phase 03 (lag compensation + precision hitbox)** — Formulas 분리 후 자연 진입. Phase 03 hitbox 통과 후 damage apply 분기에서 `Formulas.ComputeDamage` 호출.

---

## 📋 박제 (완료 후)

- 보통 등급 = -DONE.md 없음, work-pin + commit message 충분
- 단, 함정 발견 (예: 클라 측 Formulas 오용 발견 + 별 Phase 신설) 시 *복잡 자동 상향* → -DONE.md 박음

---

## 작업 로그

- 2026-05-22: Phase 정의 박힘 (M4.1 plan 박는 시점)
