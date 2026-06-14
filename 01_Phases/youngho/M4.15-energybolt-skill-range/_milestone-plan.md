---
owner: youngho
milestone: M4.15
title: Mage Energy Bolt 다듬기 + 전 스킬 히트박스 범위 재정비 (+ 텔레포트 4방향/v14)
status: in_progress
grade: 대규모
estimated: 10~17h (총합, 9 Phase — 워크스트림 D 텔레포트 합류)
domains: [shared, server, client, qa]
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

### 핵심 워크스트림 (A/B/C 초기 + D 추가)

- **(A)** 투사체 = **일정 속도** + **사거리 제한** (멀면 더 *오래* 걸리게, 속도 폭증 제거) — ✅ Phase 04/05
- **(B)** 모든 스킬 히트박스 = **X/Y 분리 + Y 좁히기** (층 간격 이하로) — ✅ Phase 02
- **(C)** **freeze 적용 제거** — 단, freeze *인프라*(미래 빙결 스킬용)는 보존 — ✅ Phase 03
- **(D)** **텔레포트 4방향 + 거리 1/3 + 이펙트 버그** (영호 Play-test 중 추가, 2026-06-14) — Phase 06~08
  - ① 위/아래 방향 추가(현재 좌우만) → `C_SkillUse.verticalDir` 신규 필드 + **v14 bump**(영호 Option B GO)
  - ② 이동 거리 15→5 (1/3)
  - ③ depart/arrive 이펙트 레이스 결정론적 수정 (클라)

### ⚠️ 등급 = 대규모 사유 (워크스트림 D 합류로 복잡 → 대규모 상향)

- **초기(A/B/C)는 복잡**: 2 주 도메인(server combat + client projectile) + qa, risk 깃발 0, wire v13 유지.
- **D 합류로 대규모 상향**: ① **4 도메인**(shared PDL/protocol + server + client + qa — §grade-and-risk "3+ 도메인") ② **irreversible 깃발**(`Protocol.Version` v13→v14 bump — 영호 Option B GO) ③ **trust-boundary 깃발**(`SkillUseHandler` verticalDir 검증).
- **모델 라우팅**: A/B/C Phase는 기본 Sonnet(완료). D 중 **Phase 07(server, 복잡+trust-boundary)** = Opus Worker(routing B). Phase 06(shared)·08(client)은 Sonnet. 리뷰·plan-auditor = Opus. 메인 file:line 게이트는 모델 무관.

### 핵심 원칙 (헌법 + 설계)

- **헌법 #1 Server Authority** — 명중 판정·데미지·freeze는 서버 전용 유지. 클라는 투사체 *연출*만(서버 `S_ProjectileLaunch` 확정 후 스폰, 선예측 스폰 부활 금지 = M4.8 기둥1 보존).
- **헌법 #2/#4 wire 신성** — 어느 Phase도 PDL 변경 0. `S_ProjectileLaunch.travelTicks` 필드 유지(의미만 "고정속도 도착 틱"으로 정리). `Protocol.Version` v13 그대로.
- **헌법 #5 틱 루프** — 투사체/freeze 모두 tick 카운트 기반(blocking 0). `DeferredDamageSystem` 패턴 유지.
- **동작 보존 게이트** — 각 Phase 전후 WSL2(ADR-029) `dotnet test` 회귀 0 + Unity 컴파일 0err로 정량 증명. baseline = Phase 01 스냅샷(M4.14 마감 = 서버 568/0/5 + EditMode 147).
- **숫자는 영호 Play 튜닝** — 박스 X/Y·투사체 속도 *값*은 영호 결정(구조만 코드). Phase 01에서 시작값 표 확정 게이트.
- **freeze 인프라 보존** — 메이플도 빙결 계열만 얼림. `FrozenUntilTick`/`ApplyFreeze`/`EnemyAISystem` 가드/Boss 면역은 미래 빙결 스킬 재사용 위해 *남김*. `ApplyFreeze` *호출*만 제거.

---

## 📋 Phase 분해 (9개 — A/B/C 6개 + D 텔레포트 3개)

| # | Phase | 등급 | 도메인 | 예상 | risk | 비고 |
|---|---|---|---|---|---|---|
| 01 | Baseline 회귀 그린 + 시작값 표 영호 확정 | 단순 | qa/메타 | 1h | — | ✅ 완료 (`5beda1f`) |
| 02 | 히트박스 X/Y 분리 + 전 스킬 Y 재튜닝 | 복잡 | server | 2~3h | — | ✅ 완료 (`baff561`, reviewer🔴0) |
| 03 | freeze 적용 제거 (인프라 보존) | 보통 | server | 1~2h | — | ✅ 완료 (`40b6290`) |
| 04 | 투사체 일정 속도 (서버 travelTicks 모델) | 보통 | server | 1~2h | — | ✅ 완료 (`a0a1589`) |
| 05 | 투사체 클라 정합 + 호밍 polish | 보통 | client | 1~2h | — | ✅ 완료 (클라 무변경 — 서버 캐스케이드) |
| 06 | **Protocol v14 — C_SkillUse verticalDir** | 복잡 | **shared** | 1h | **irreversible** | 워크스트림 D 토대. PDL append + 재생성 + v13→v14 bump |
| 07 | **서버 4방향 텔레포트 + 거리 + 지형** | 복잡 | server | 2~3h | **trust-boundary** | Opus Worker. SkillUseHandler/TeleportAction/거리 5.0/Y 경계 |
| 08 | **클라 4방향 입력 + 이펙트 버그** | 보통 | client | 1~2h | — | LocalPlayerInput 위/아래 송신 + depart/arrive 레이스 수정 |
| 09 | 회귀 + 봇 시나리오 갱신 + 마일스톤 마감 | 복잡 | qa/메타 | 1~2h | irreversible | `FreezeSmoke` 전환 + 텔레포트 회귀, WSL2, -DONE.md+HTML, PR 게이트 |

**총 등급 = 대규모** (D 합류 — 4 도메인 + irreversible v14 + trust-boundary). 복잡+ Phase(02·06·07·09)는 각 `-DONE.md`. 마일스톤 마감 = `_milestone-DONE.md` + HTML(ADR-031).

---

## 🔗 의존성 그래프

```
Phase 01 (baseline + 시작값 표 확정)   ✅
   ↓
Phase 02 (히트박스 X/Y 분리)   ✅ ←── GetAttackHitbox 키스톤
   ↓
Phase 03 (freeze 적용 제거)   ✅ ←── 02 후 (동일 파일 직렬화)
   ↓
Phase 04 (투사체 서버 모델)   ✅ ←── 03 후 (MeleeAction.cs Mage 분기 직렬화)
   ↓
Phase 05 (투사체 클라 정합)   ✅ ←── 04 후 (서버 travelTicks 계약 의존)
   │
   │   ──── 워크스트림 D (텔레포트, 영호 Play-test 중 추가) ────
   ↓
Phase 06 (Protocol v14 — verticalDir)   ←── shared, 텔레포트 와이어 토대 (irreversible)
   │
   ├─────────────┬─────────────┐
   ↓             ↓             │
Phase 07         Phase 08      │  ←── 07(server 4방향)·08(client 입력+이펙트)
(서버 4방향)     (클라 입력)   │      둘 다 06(verticalDir)에 의존, 도메인 분리라 병렬 가능
   │             │             │      (단일 세션이면 07→08 순차도 무방)
   └──────┬──────┘             │
          ↓                    │
Phase 09 (회귀 + 봇 갱신 + 마감)   ←── 01~08 전부 후 (irreversible PR 게이트)
```

- **01 → 05 (워크스트림 A/B/C, 완료)**: 02·03·04 직렬(`MeleeAction.cs`/`CombatConstants.cs` 동일 파일 + 03이 freeze-travelTicks 결합 해소 → 04 자유), 04→05 클라 정합. 전부 wire v13 유지.
- **06 → {07, 08} (워크스트림 D)**: 06이 `C_SkillUse.verticalDir` 필드 + ProtocolVersion v14를 만들어 서버(07)·클라(08)가 모두 소비/송신. **07·08은 도메인 분리(server vs client)라 논리적 병렬** — 단, 통합 Play-test는 둘 다 필요. 단일 세션이면 07(서버 계약 먼저)→08(클라 정합) 순차 권장.
  - ⚠️ **verticalDir 인코딩 단일 출처** (drift 방지, plan-auditor 봉합): `0=수평/1=위/2=아래` 의미는 **P06이 PDL.xml 주석에 단일 박제**(이미 06 작업내용에 주석 명시). P07(서버 정규화)·P08(클라 송신)은 그 주석을 *진실로 참조* — 양쪽이 독립 해석하면 통합 Play에서만 발각되는 drift. 코드 실측상 PDL 주석이 단일 출처라 문서 한 줄로 충분.
- **{07, 08} → 09**: 텔레포트 회귀 + 봇 + 마감은 4방향/이펙트 둘 다 후.
- **권장 순서**: (완료)01~05 → 06 → 07 → 08 → 09 (선형).

---

## ✅ 마일스톤 완료 조건

- [ ] **baseline (Phase 01)**: WSL2 `dotnet test` green 카운트 + Unity 컴파일 0err를 박제 (M4.14 = 서버 568/0/5, EditMode 147 — 회귀 비교 기준). 시작값 표 영호 승인 박제.
- [ ] **(B) 히트박스 X/Y 분리**: `GetAttackHitbox`가 X/Y 별도 half-extent 반환. Mage 평타 Y범위 ≤ 층 간격(테스트로 위층 적 miss 검증). Thunderbolt/Dash Y 재튜닝 반영.
- [ ] **(C) freeze 제거**: `ApplyFreeze` 호출 2곳(Melee:79, Thunderbolt:43) 제거. 적이 평타/번개 맞아도 `FrozenUntilTick==0`(테스트). **인프라 보존**: `FrozenUntilTick`/`ApplyFreeze`/`EnemyAISystem` 가드/Boss 면역 무변경(인프라 테스트 green 유지).
- [ ] **(A) 투사체 일정 속도**: `travelTicks`가 거리 비례 단조증가(상한 폭증 0). 먼 거리도 속도 일정(영호 Play 육안). 클라 투사체 일정 속도 비행.
- [ ] **(D) 텔레포트**: ① 위/아래/좌/우 4방향(verticalDir 1=위/2=아래/0=수평) ② 거리 5.0(1/3) ③ 출발 이펙트=출발점·도착 이펙트=도착점(레이스 제거). 수직 텔레포트가 땅속/천장/맵밖 안 빠짐.
- [ ] **wire**: 워크스트림 A/B/C는 PDL 0(v13). **워크스트림 D는 `C_SkillUse.verticalDir` append + `Protocol.Version` v13→v14**(영호 Option B GO). 기존 필드 순서 불변(append-only).
- [ ] **회귀 0**: WSL2 `dotnet build` 0/0 + `dotnet test` green(범위/freeze/텔레포트 테스트 갱신분 반영) + 봇 시나리오 회귀 0 + Unity 컴파일 0err. `ProtocolVersion.Current == 14` 정합.
- [ ] Phase 02·07 reviewer 헌법 hard 위반 0 (trust-boundary Phase는 reviewer 자동).
- [ ] (복잡+) Phase 02·06·07·09 -DONE.md + 마일스톤 -DONE.md + HTML.

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
- 2026-06-14 — **Phase 01~05 완료** (워크스트림 A/B/C). 01 baseline(`5beda1f`) → 02 히트박스 X/Y(`baff561`, reviewer🔴0) → 03 freeze 제거(`40b6290`) → 04 투사체 ceil(2D)+상한제거(`a0a1589`) → 05 클라 무변경(서버 캐스케이드가 클라 속도 자동 정합, 영호 Play "투사체 이상무" 통과). WSL2 570/0/5.
- 2026-06-14 — **워크스트림 D(텔레포트) 추가 + 등급 복잡→대규모 상향**. 영호 Play-test 중 텔레포트 3종 수정 요청(①4방향 ②거리 1/3 ③이펙트 버그). 메인 ReadOnly 실측(TeleportAction/SkillUseHandler/SkillCastHandler/LocalPlayerInput/ActionContext 흐름) 후 방향 채널 결정 = **영호 AskUserQuestion → Option B(전용 `verticalDir` 필드 + `Protocol.Version` v14 bump) GO**. Phase 06(shared/v14 토대)·07(server 4방향, Opus·trust-boundary)·08(client 입력+이펙트) 신설, 기존 06(마감) → 09 리넘버. irreversible(v14)+trust-boundary+4도메인 = 대규모.
- 2026-06-14 — **plan-auditor 재검 (워크스트림 D)**: 🔴2 + 🟡3, 전부 비가역 아님(plan 문서·테스트 명세) → 옵션 A(즉시 봉합) 적용. 🔴①P07 verticalDir whitelist 정규화 — facing(2진)과 달리 **3진 정의역**이라 "1 아니면 0" 패턴 금지(2=아래 뭉개짐), 경계값 3 테스트 필수({3,99,255}→0). 🔴②P07 수직 지형 안전 = **옵션 ②(Y clamp + 물리 resolve) MVP 고정**(옵션 ① terrain 사전질의는 신규 인프라라 스코프 밖), 영구 끼임 0 정량 게이트. 🟡①verticalDir 인코딩 단일 출처=PDL 주석 명시. 🟡②P08 원격 출발 이펙트 회귀 게이트(L130 분기 앞 공유 줄). 🟡③P09 봇 신규 안 만듦 확정. **등급 대규모·헌법 §1/§2/§5 = GO**. → Phase 06 착수.
