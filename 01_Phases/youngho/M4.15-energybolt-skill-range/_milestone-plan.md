---
owner: youngho
milestone: M4.15
title: Mage Energy Bolt 다듬기 + 전 스킬 히트박스 범위 재정비
status: in_progress
grade: 복잡
estimated: 7~12h (총합, 6 Phase)
domains: [server, client, qa]
---

# M4.15 — Mage Energy Bolt 다듬기 + 전 스킬 히트박스 범위 재정비

> **상태**: planned — 2026-06-14 영호 의논 + 메인 세션 ReadOnly 실측 기반 작성
> **시작**: 2026-06-14 (M4.14 마감 + PR #109 머지 후, `feature/m4.15-energybolt-skill-range`)
> **목표 마감**: 미정 — 발표 마감 경고 없이 논리적 응집으로 가름 (일정 = 영호 컨트롤)
> **선행 근거 문서**: `C:\Users\bass1\.claude\plans\buzzing-jingling-raccoon.md` (영호 승인 설계 플랜)

---

## 🎯 마일스톤 목표

영호가 직접 플레이한 결과 **Mage 기본 공격(평타)을 메이플스토리 *에너지 볼트* 매커니즘으로 다듬고** 싶다는 방향 확정. 의논 끝에 거칠음 3종이 코드의 구조적 결함과 1:1로 드러남:

1. **투사체 속도가 거리에 비례해 폭증** — 서버 `travelTicks = clamp(거리÷2.0, 2, 10)` 상한 + 클라 `속도 = 거리÷비행시간` 역산 조합. 거리 20유닛 초과 시 비행시간이 0.5초에 고정되는데 거리는 계속 늘어 → 멀수록 순간이동처럼 보임.
2. **히트박스가 정사각형이라 Y범위가 과함** — `GetAttackHitbox`가 `new AABB(중심,(half,half))`라 Mage는 X도 ±8, **Y도 ±8** → 위아래 여러 층(1층↔2층)을 다 때림. 사이드스크롤 공격은 납작해야(X 넓고 Y 얇게) 하는데 정사각형.
3. **Mage 평타가 적을 0.5~0.9초 얼림(freeze/stun-lock)** — 메이플 에너지 볼트엔 없는 거동.

"다른 스킬도 비슷"(영호) — 범위 문제는 평타뿐 아니라 Thunderbolt/Dash 등 전 스킬 공통.

### 핵심 3 워크스트림

- **(A)** 투사체 = **일정 속도** + **사거리 제한** (멀면 더 *오래* 걸리게, 속도 폭증 제거)
- **(B)** 모든 스킬 히트박스 = **X/Y 분리 + Y 좁히기** (층 간격 이하로)
- **(C)** **freeze 적용 제거** — 단, freeze *인프라*(미래 빙결 스킬용)는 보존

### ⚠️ 등급 = 복잡 사유

- **2 주 도메인** (server combat + client projectile) + qa(테스트/봇 갱신) = §grade-and-risk 복잡 (M4.14 선례 정합: 3영역 *순차* 단일도메인 Phase = 복잡, 대규모는 *동시* 3+도메인/비가역).
- **risk 깃발 0**: trust-boundary 아님(`CombatSystem`/`Actions`는 검증 경계가 아닌 전투 기하 — `ValidateRewind`/rate-limit *불변*). irreversible 아님(PDL 0, `Protocol.Version` v13 유지, DB 0). unity-asset 아님(prefab 이미 존재, `ProjectileVisual.cs` 스크립트만).
- 구현 Worker 기본 **Sonnet** (Opus-B 미발동 — 어떤 Phase도 `복잡+trust-boundary`/`대규모` 아님). 리뷰·plan-auditor = Opus.

### 핵심 원칙 (헌법 + 설계)

- **헌법 #1 Server Authority** — 명중 판정·데미지·freeze는 서버 전용 유지. 클라는 투사체 *연출*만(서버 `S_ProjectileLaunch` 확정 후 스폰, 선예측 스폰 부활 금지 = M4.8 기둥1 보존).
- **헌법 #2/#4 wire 신성** — 어느 Phase도 PDL 변경 0. `S_ProjectileLaunch.travelTicks` 필드 유지(의미만 "고정속도 도착 틱"으로 정리). `Protocol.Version` v13 그대로.
- **헌법 #5 틱 루프** — 투사체/freeze 모두 tick 카운트 기반(blocking 0). `DeferredDamageSystem` 패턴 유지.
- **동작 보존 게이트** — 각 Phase 전후 WSL2(ADR-029) `dotnet test` 회귀 0 + Unity 컴파일 0err로 정량 증명. baseline = Phase 01 스냅샷(M4.14 마감 = 서버 568/0/5 + EditMode 147).
- **숫자는 영호 Play 튜닝** — 박스 X/Y·투사체 속도 *값*은 영호 결정(구조만 코드). Phase 01에서 시작값 표 확정 게이트.
- **freeze 인프라 보존** — 메이플도 빙결 계열만 얼림. `FrozenUntilTick`/`ApplyFreeze`/`EnemyAISystem` 가드/Boss 면역은 미래 빙결 스킬 재사용 위해 *남김*. `ApplyFreeze` *호출*만 제거.

---

## 📋 Phase 분해 (6개)

| # | Phase | 등급 | 도메인 | 예상 | risk | 비고 |
|---|---|---|---|---|---|---|
| 01 | Baseline 회귀 그린 + 시작값 표 영호 확정 | 단순 | qa/메타 | 1h | — | 안전망 + 수치 합의 게이트 |
| 02 | 히트박스 X/Y 분리 + 전 스킬 Y 재튜닝 | 복잡 | server | 2~3h | — | 구조 키스톤 (`GetAttackHitbox` 비정사각화) |
| 03 | freeze 적용 제거 (인프라 보존) | 보통 | server | 1~2h | — | `ApplyFreeze` 2곳 제거 (Melee+Thunderbolt) |
| 04 | 투사체 일정 속도 (서버 travelTicks 모델) | 보통 | server | 1~2h | — | `MaxTravelTicks` 상한 artifact 제거 |
| 05 | 투사체 클라 정합 + 호밍 polish | 보통 | client | 1~2h | — | clamp 제거로 속도 자동 일정 → 검증 + 호밍 jank 완화 |
| 06 | 회귀 + 봇 시나리오 갱신 + 마일스톤 마감 | 복잡 | qa/메타 | 1~2h | — | `FreezeSmoke` 전환, WSL2 회귀, -DONE.md + HTML, PR 게이트 |

**총 등급 = 복잡**. Phase 02·06이 복잡 → 각 `-DONE.md`. 마일스톤 마감 = `-DONE.md` + HTML(ADR-031, 복잡 임계).

---

## 🔗 의존성 그래프

```
Phase 01 (baseline + 시작값 표 확정)
   │  └─ 영호 승인 게이트: 박스 X/Y·투사체 속도 시작값
   │
   ↓
Phase 02 (히트박스 X/Y 분리 + 전 스킬 Y)   ←── server, GetAttackHitbox 시그니처 키스톤
   │
   ↓
Phase 03 (freeze 적용 제거)   ←── 02 후 (MeleeAction.cs/CombatConstants.cs 동일 파일 직렬화)
   │
   ↓
Phase 04 (투사체 서버 모델)   ←── 03 후 (MeleeAction.cs Mage 분기 동일 파일 직렬화)
   │
   ↓
Phase 05 (투사체 클라 정합)   ←── 04 후 (서버 travelTicks 계약 의존)
   │
   ↓
Phase 06 (회귀 + 봇 갱신 + 마감)   ←── 01~05 전부 후
```

- **01 → 전부**: baseline 테스트 카운트 = 회귀 0 증명 기준선 + 시작값 합의.
- **02·03·04 직렬**: 셋 다 `MeleeAction.cs`/`CombatConstants.cs`(특히 Mage 분기 — travelTicks L64-68 + ApplyFreeze L79가 같은 함수)를 손대 머지 충돌 방지 직렬화. *논리적으론* 03(freeze)은 02(범위)와 독립이나, **03→04는 파일 직렬 + 논리 의존**(03이 freeze-travelTicks 결합을 끊어 04가 travelTicks를 자유롭게 손봐도 freeze 부작용 0). 단일 세션이라 순차 진행 = 병렬 기회 놓침 0(같은 2파일 공유).
- **04 → 05**: 클라 투사체 속도는 서버 travelTicks 계약에 정합. (단, clamp 제거 시 클라 거리역산 속도가 *자동으로* 일정해짐 — P05는 검증 + 호밍 polish 중심, 경량 가능.)
- **권장 순서**: 01 → 02 → 03 → 04 → 05 → 06 (선형).

---

## ✅ 마일스톤 완료 조건

- [ ] **baseline (Phase 01)**: WSL2 `dotnet test` green 카운트 + Unity 컴파일 0err를 박제 (M4.14 = 서버 568/0/5, EditMode 147 — 회귀 비교 기준). 시작값 표 영호 승인 박제.
- [ ] **(B) 히트박스 X/Y 분리**: `GetAttackHitbox`가 X/Y 별도 half-extent 반환. Mage 평타 Y범위 ≤ 층 간격(테스트로 위층 적 miss 검증). Thunderbolt/Dash Y 재튜닝 반영.
- [ ] **(C) freeze 제거**: `ApplyFreeze` 호출 2곳(Melee:79, Thunderbolt:43) 제거. 적이 평타/번개 맞아도 `FrozenUntilTick==0`(테스트). **인프라 보존**: `FrozenUntilTick`/`ApplyFreeze`/`EnemyAISystem` 가드/Boss 면역 무변경(인프라 테스트 green 유지).
- [ ] **(A) 투사체 일정 속도**: `travelTicks`가 거리 비례 단조증가(상한 폭증 0). 먼 거리도 속도 일정(영호 Play 육안). 클라 투사체 일정 속도 비행.
- [ ] **wire 무변경**: PDL 변경 0, `Protocol.Version` v13 유지.
- [ ] **회귀 0**: WSL2 `dotnet build` 0/0 + `dotnet test` green(범위/freeze 테스트 갱신분 반영) + 봇 시나리오 회귀 0 + Unity 컴파일 0err.
- [ ] Phase 02 reviewer 헌법 hard 위반 0.
- [ ] (복잡) Phase 02·06 -DONE.md + 마일스톤 -DONE.md + HTML.

---

## 🚫 이번에 명시적으로 뺀 것 (사유 박음)

- **투사체를 "진짜 회피 가능"으로 변경**: 메이플 에너지 볼트는 *유도 명중*(이동으로 회피 X) — 회피형 투사체는 메이플에서 *멀어지는* 방향. 현 "유도+서버 확정" 구조 유지, 속도/범위만 다듬음.
- **freeze 인프라 통째 제거**: 메이플 빙결 계열 스킬이 미래에 재사용 → `FrozenUntilTick`/가드/Boss면역 *보존*. 호출만 제거.
- **보스 공격 박스 Y 재튜닝**: 적 AI 영역이라 이번 스코프 제외(영호 선택 — 필요 시 별 Phase). 기본 제외.
- **데미지/사거리/쿨다운 밸런스 재설계**: 이번은 *범위 기하* + *투사체 이동* + *freeze*만. Mage 평타 BaseDamage(Knight와 동일 10) 차별화는 후속 밸런싱 마일스톤.
- **Unity 투사체 prefab/이펙트 신규 제작**: prefab 이미 존재(`Projectile`/`ProjectileImpact`/`RemoteProjectile`). 자산 제작 = 영호/유현 영역 — 이번은 코드 wiring만.

---

## 갱신 이력

- 2026-06-14 — **사전 작성** (메인 세션). 영호 의논(에너지 볼트 방향 + freeze 제거 + 전 스킬 범위 재논의) + ReadOnly 실측(투사체 속도 clamp artifact, GetAttackHitbox 정사각, freeze 2곳)을 6 Phase로 분해. 승인 플랜 = `buzzing-jingling-raccoon.md`.
- 2026-06-14 — **plan-auditor GO** (🔴 0 확정). auditor가 🔴 1건(`Protocol.Version` v13→v12 라벨 불일치) 제기했으나 **메인 file:line 실측으로 false positive 판정** — `ProtocolVersion.cs:66 Current=13`(M4.13 P5 bump)이 실재, auditor가 stale `98_Shared/CLAUDE.md:19`("Current=12")를 믿고 `.cs` const 줄을 놓침. plan v13 라벨 정확 → 무수정(v12로 "고치면" 오류 주입). **carry-over 학습 "메인 file:line 게이트 모델무관" 실례** — Opus auditor도 stale 문서에 오도됨. 🟡 2건 반영: ①의존성 그래프 03→04 = 파일직렬+freeze결합해소 논리의존 명시 ②Phase 05 사전조건에 "직선 vs 호밍보간 영호 택1" 연출 분기 게이트 추가. **별건 발견(M4.15 스코프 외)**: `98_Shared/CLAUDE.md` Current=12 stale(실제 13) — 별도 doc 정정 후보(영호 결정).
