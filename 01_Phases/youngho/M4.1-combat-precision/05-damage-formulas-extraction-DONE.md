---
owner: youngho
milestone: M4.1
phase: 05
title: Damage Formulas Extraction (P0-3 + P1 봉합)
status: done
grade: 복잡
risk: low
summary: M3 응급 박힌 `CombatConstants.BaseDamage=10` 고정 데미지를 `98_Shared/GameData/Formulas.cs` 순수 함수로 분리 + `PlayerStats` 02_Server → 98_Shared 이동 (헌법 #4 정합) + `PlayerStats.Attack` 진짜 데미지 반영 (Warrior 25dmg / Ranger 22dmg). 신규 단위 테스트 6건 + 회귀 211/0/1 통과 + reviewer Tier 2-A PASS.
---

# Phase 05 — DONE

**완료 일자**: 2026-05-24
**소요**: ~30분 (Coordinator 분해 ~4분 / Worker A shared ~4분 / Worker B server ~6분 / reviewer Tier 2-A ~2분 / Minor 봉합 ~3분 / 의논·검증 잔여)

---

## TL;DR

P0-3 결함 (PlayerStats가 PlayerEntity와 전투 공식에 *진짜* 반영) 봉합 + P1 정밀도 베이스 박음. **공식 순수 함수 분리** (`Formulas.ComputeDamage(PlayerStats, EnemyStats, int) → int`) + **PlayerStats 98_Shared로 이동** (헌법 #4 Shared Code Discipline 정합, 옛 02_Server 위치 = 서버 권위 ≠ 서버 전용 코드 위치 미인지 함정). 옛 `target.Hp -= BaseDamage` 고정 10dmg → 새 `Warrior Attack=15 + base 10 - Defense 0 = 25dmg` 진짜 반영. **reviewer Tier 2-A PASS** + Minor 1건 즉시 봉합 + 결함 0건.

---

## 📦 산출물 분담 (Coordinator 분해 → shared+server 직렬)

### Worker A — shared SubAgent (먼저)
- `98_Shared/GameData/Formulas.cs` 신설 (`Formulas.ComputeDamage` 순수 함수 + `EnemyStats` struct)
- `98_Shared/GameData/PlayerStats.cs` 신설 (02_Server에서 이동, `init` setter 함정 회피 = private ctor + factory 패턴)
- `02_Server/GameServer/Combat/PlayerStats.cs` git rm (이동, 복사 X)
- `dotnet build Dawnholder.slnx` green + Shared.dll 자동 복사 (`03_Client/Assets/Plugins/Shared/Shared.dll`)
- PDL 정합 점검 (`S_HitResult.damage = int` 정합, ProtocolVersion bump X)

### Worker B — server SubAgent (Worker A 박힘 후)
- `PlayerEntity.Stats` + `EnemyEntity.Stats` 필드 박음
- `GameMap.ProcessAttack` Formulas 위임 (`Formulas.ComputeDamage(attacker.Stats, target.Stats, BaseDamage)`)
- `GameMap.AddPlayer` 시그니처 `PlayerStats? stats = null` 추가 + `GameSession.cs:137` 호출지 정정
- `FormulasTests.cs` 6건 신설 (Warrior happy / Ranger happy / Defense high / Defense zero / Negative baseDamage / Large baseDamage)
- 회귀 정합 갱신 — `AttackHandlerTests` / `BossStageClearTests` 옛 10dmg → 새 25dmg 동적 계산 (`Formulas.ComputeDamage(Warrior, default, 10)` 박음 = drift 방지)
- `dotnet test` 통과 211 / 실패 0 / skip 1 (45s)
- `CombatConstants.BaseDamage` 보존 (Formulas 입력 상수로 활용, 제거 X)

### Minor 봉합 (reviewer 후속)
- `FormulasTests.cs:60~67` 변명 코멘트 정리 (옛 9줄 → 새 4줄, 짧고 깔끔)

---

## AC 검증 결과

| AC | 결과 | 검증 |
|---|---|---|
| `Formulas.cs` 신설 + `ComputeDamage` 순수 함수 박힘 | ✅ | 파일 존재 + 시그니처 정합 |
| `EnemyStats` struct 박힘 (Defense/MaxHp default=0) | ✅ | Formulas.cs:53~60 |
| `PlayerStats` 98_Shared로 이동 + 02_Server 복사본 X | ✅ | git status `D 02_Server/.../PlayerStats.cs` + `?? 98_Shared/GameData/PlayerStats.cs` |
| `PlayerEntity.Stats` ↔ `GameSession._stats` 연결 | ✅ | `GameSession.cs:137` `map.AddPlayer(self, spawnPos, self._stats)` |
| `GameMap.ProcessAttack` Formulas 위임 박힘 | ✅ | `GameMap.cs:189` 근처 `Formulas.ComputeDamage(attacker.Stats, target.Stats, BaseDamage)` |
| `S_HitResult.damage` = Formulas 결과 | ✅ | `damage` 변수 박음 (옛 `CombatConstants.BaseDamage` 박지 X) |
| `CombatConstants.BaseDamage` 보존 | ✅ | Formulas 입력 상수로 활용 |
| 새 단위 테스트 6건 통과 | ✅ | `FormulasTests` PASS |
| 회귀 0 (총 211건 + skip 1) | ✅ | `dotnet test Dawnholder.slnx` 45s green |
| `dotnet build` green (경고 0 오류 0) | ✅ | Dawnholder.slnx |
| Shared.dll 새 hash 자동 복사 | ✅ | `M 03_Client/Assets/Plugins/Shared/Shared.dll` |
| PDL ProtocolVersion bump 불필요 | ✅ | `S_HitResult.damage = int` 그대로 |
| reviewer Tier 2-A 통과 | ✅ | 5축 PASS, Minor 1건 즉시 봉합 |

---

## 결정 흐름

1. **PlayerStats 이동 vs primitive 분리** — Coordinator 분해 시점에 미세 함정 발견. Phase 정의 시그니처 `ComputeDamage(PlayerStats attacker, ...)` 박혀있어 헌법 #4 (Shared Code Discipline) 정합 = **이동 (옵션 a)** 결정. primitive 분리 (`int attackerAttack, int targetDefense`)는 Phase 정의와 어긋남 + 헌법 #4 정신 약해짐. 사용자 GO 받음.
2. **`init` setter 함정 사전 회피** — Worker A가 PlayerStats 이동 시 옛 `public int Attack { get; init; }` 패턴이 .NET Standard 2.1에서 `IsExternalInit` shim 누락으로 CS0518 오류 발생 = **private ctor + static factory (Warrior/Ranger) 패턴**으로 전환. 불변성 동등 + cross-runtime (Unity/Server) 호환 안전망.
3. **회귀 테스트 기대값 갱신 패턴** — Worker B가 `AttackHandlerTests` / `BossStageClearTests`에서 옛 `const ExpectedBaseDamage = 10` 박힌 자리에 **`Formulas.ComputeDamage(Warrior, default, 10)` 동적 계산** 박음 = mirror 상수 drift 위험 0 (옛 BaseDamage 가정 박힌 테스트가 새 25dmg 정합 자동 갱신). reviewer 칭찬 박힘.
4. **`CombatConstants.BaseDamage` 보존** — Phase 정의 박힘 + 의도적 보존. Formulas 입력으로 활용. 제거 시 Formulas 입력 hardcoded 함정.

---

## 학습 일지 후보 키워드

### 1. `shared-code-discipline-relocation-pattern` (★★)
"서버 권위 = 서버 전용 코드 위치"가 아님. 헌법 #1 (Server Authority)은 *판정의 책임*이 서버라는 뜻이지 *코드 위치*가 서버라는 뜻 X. 공식 코드는 양쪽 (서버 권위 + 클라 hint 표시)이 봐도 OK — 단 *판정 실행*은 서버만, 클라는 *표시 hint*만. Formulas.cs xmldoc 11~12줄 "클라 `Hp -=` 금지" 명시가 그 경계선 박는 안전망. 면접 결정타 키워드 = "Server Authority 정신 vs Shared Code Discipline 정합 패턴". 옛 PlayerStats가 Combat/에 박혀있던 이유 = "서버 전용 데미지 계산 도구"였기 때문. M4.1에서 *클라 hint 표시 가능* 가닥 박히면서 즉시 Shared 이동 의무.

### 2. `init-setter-net-standard-2-1-trap` (★★)
`public int Attack { get; init; }` 패턴(C# 9.0+)이 .NET Standard 2.1에서 `IsExternalInit` 타입 shim을 직접 박지 않으면 컴파일 CS0518 오류. 객체 지향에서 *불변성 강제 방법* 3가지:
- ① `readonly` 필드 + 생성자 (가장 안전)
- ② `init` setter (C# 9.0+ 런타임 또는 shim 필요 — cross-runtime 위험)
- ③ `private` setter + factory (호환성 안전 + 의미 명확)

다인 환경 + Unity 협업 같은 *cross-runtime 코드*에서는 ②를 *피하는 게 안전 default*. Worker A가 사전 회피 박은 게 큰 학습 자산. .NET Standard 2.1 = ADR-010 박힌 베이스라 본 패턴은 98_Shared 전체에 적용 룰.

---

## false-promise 점검 결과 (ADR-024 cadence)

본 Phase 진행 중 **누적 26번째 발본** 발견:
- `98_Shared/CLAUDE.md` Layout 표에 `Formulas.cs (M4 진입 시 박힘 예정 — 현재 미박힘)` 박혀있었음 → 본 Phase 05에서 박힘으로 **stale**. 본 commit 묶음에 같이 정정 박음 (Formulas.cs 현재 박힘 + Tables/는 여전히 M5+ 예정).
- 추가 정정 = `98_Shared/CLAUDE.md` Layout에 `PlayerStats.cs` 누락 → 본 commit에 한 줄 추가.

M4.1 누적 = 7건 (Codex 슬래시 23 + CLAUDE.md:38 stale + Codex β cross-review 4건 + 본 26번째).

---

## 작업 로그

- 2026-05-24: Phase 05 진입 — `/session:start` 게이트 통과 + 사용자 가닥 Phase 05 결정 + Coordinator 분해 + Worker A → Worker B 직렬 + reviewer Tier 2-A PASS + Minor 1건 봉합 + -DONE.md 박음
- 2026-05-22: Phase 정의 박힘 (M4.1 plan 박는 시점)
- 2026-05-23: M4.1 재구성 옵션 A' GO — Phase 02 → 05 rename + 보통 → 복잡 자동 상향 (P0-3 결함 흡수)

---

## ➡️ 다음 Phase

- **Phase 06 (lag compensation + AABB hitbox + B1/B3 sweep)** — Formulas 분리 후 자연 진입. Phase 06 hitbox 통과 후 damage apply 분기에서 `Formulas.ComputeDamage` 호출. 본 Phase 마감 = M4.1 5/6 Phase 마감 (Phase 01~05 ✅).
