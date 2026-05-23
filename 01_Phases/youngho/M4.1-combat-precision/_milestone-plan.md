---
owner: youngho
milestone: M4.1
title: Combat Precision (Codex 크로스 리뷰 + Formulas.cs 분리 + lag compensation + precision hitbox)
status: pending
grade: 복잡
risk: low
estimated: 5~8h (총합, 3 Phase)
domain: server+shared+qa
depends_on: M3.8 Phase 03 (PlayerStats + CharacterClass 박힘)
---

# M4.1 — Combat Precision

> **상태**: pending (M3.8 신설로 진입 시점 재조정 — M3.8 마감 후 진입)
> **시작 예정**: 2026-05-27 (M3.8 마감 후 = 5/26 예상)
> **마감 목표**: 2026-06-02 (캡스톤 1 발표 1주 전, M4.2 진입 여유)
> **사전 조건 (신설)**: M3.8 Phase 03 마감 = PlayerStats 박힘 (Phase 02 Formulas.cs가 PlayerStats 흡수 의무)

---

## 🎯 마일스톤 목표

**M3 응급 데모용 박힌 전투 단순화를 본 마감 정밀화로 승격**. M4 전체(Combat & Map Transition) 3토막 중 첫 토막 = "전투 정밀화" 영역. 캡스톤 1 발표(6/10) 전반 흡수.

**3 Phase 구성**:
1. **Phase 01** — Codex β 크로스 리뷰 + M3 응급 하드코딩 추가 발본 (사전 점검 / plan 갱신 게이트)
2. **Phase 02** — 데미지 공식을 `98_Shared/GameData/Formulas.cs` 순수 함수로 분리 (헌법 #1/#2 정합 강화)
3. **Phase 03** — position history 200ms buffer + lag compensation rewind + precision hitbox (AABB 또는 capsule)

**왜 본 마일스톤이 필요한가**:

- **M3 응급 단순화 8건 (ARCHITECTURE.md "M4 사전 과제")**: `BaseDamage=10` 고정 / 거리만 보는 hitbox / lag 보정 없음 / cheat-flag table 없음 등 = *권위·신뢰 경계는 지키되 정밀도 X* 상태. 본 마감(11/19)엔 전부 정밀화 필요.
- **캡스톤 1(6/10) 발표 데모 욕심 (C 선택)**: M3 그대로 발표 X, "정밀 전투 + 4맵 분리" 어필. 본 마일스톤 = 캡스톤 1 데모 절반 (나머지 절반 = M3.8 Demo 인프라 + M4.2 Map Transition).
- **3토막 분할 정합 (M3.5 학습)**: M3 9 Phase 과했음 정정. 본 마일스톤 = 3 Phase로 압축, 학습 호흡 유지.
- **M3.8 신설 흡수 (2026-05-22 박힘)**: 캡스톤 1 발표 데모 인프라(메인/캐릭터 선택/NPC/엔딩)는 M3.8 별 마일스톤로 분리. 본 M4.1은 *정밀 전투* 단독 영역. M3.8 Phase 03에서 `PlayerStats` (전사/원거리 스탯 분기) 박힘 = 본 M4.1 Phase 02 `Formulas.cs`가 흡수.

**비-목표 (M4.2/M4.3로 미룸)**:

- 진짜 4맵 분리 + portal handoff → M4.2
- 클라 측 맵 dispatch + UI → M4.2 (정유현 협업)
- cheat-flag table + Serilog 도입 → M4.2 (발표 직전 안정화 의례, M4.1 lag comp와 conflict 회피)
- enemy AI / boss behavior → M4.3
- Phase 05 jump Y mispredict 봉합 → M4.3
- PvP 지원 ADR → M4.3

---

## 📋 Phase 분해 (3개)

| # | Phase | 등급 | 도메인 | 예상 | 담당 |
|---|---|---|---|---|---|
| 01 | Codex 크로스 리뷰 + M3 하드코딩 추가 발본 | 보통 | qa+cross | 1~2h | 메인 + Codex β 외부 |
| 02 | `Formulas.cs` 분리 (데미지 공식 순수 함수) | 보통 | shared+server | 1~2h | server SubAgent + shared SubAgent |
| 03 | lag compensation 200ms + precision hitbox | 복잡 | server | 3~5h | server SubAgent (Sonnet) |

**총 등급 = 복잡** (마일스톤 자체) — Phase 03 복잡 + Phase 01·02 보통.

**박제 정합**:
- Phase 01·02 = 보통 = work-pin + commit (-DONE.md 없음)
- Phase 03 = 복잡 = -DONE.md 박음
- 마일스톤 마감 = M4.1-마감 별 -DONE.md (복잡 등급) + ADR-024 cadence 첫 시범 (false-promise 점검 결과 섹션 의무)

---

## 🔗 의존성 그래프

```
Phase 01 (Codex 크로스 리뷰)
   │
   │  발견된 추가 하드코딩이 Phase 02/03 scope 갱신 트리거 가능
   │  발견 0~소량 → Phase 02 그대로 진행
   │  발견 대량 → plan 재구성 (옵션 A: M4.1 Phase 추가 / 옵션 B: M4.3 흡수)
   ↓
Phase 02 (Formulas.cs 분리) — 헌법 #1/#2 정합 강화 (양쪽 공유 코드)
   │
   │  Formulas.cs의 `ComputeDamage(attacker, target)` 시그니처가 Phase 03 hitbox 검증 후
   │  damage application 분기에서 호출됨
   ↓
Phase 03 (lag compensation + precision hitbox)
```

**의존성 사유**:
- Phase 01 *먼저* = "사전 점검 후 본격 진행" 패턴 (M3 Phase 01 pre-flight smoke 정합). 발견 적으면 큰 비용 X, 대량이면 plan 재구성으로 후속 사고 회피.
- Phase 02 *Phase 03 전* = Formulas 분리가 Phase 03 hitbox 통과 후 damage apply 분기에서 자연 호출됨. 순서 반대면 Phase 03에서 임시 inline damage → 다시 Formulas로 옮기는 이중 작업.

**병렬 가능 = 없음** (3 Phase 순차).

---

## ✅ 마일스톤 완료 조건

- [ ] Phase 01 = Codex β 크로스 리뷰 보고서(`00_Document/reviews/2026-MM-DD-pre-m4-cross-review-codex.md`) 박힘 + 발견 항목별 처리 결정 (즉시 봉합 / M4.2/M4.3 이관 / 별 시점) 명시
- [ ] Phase 02 = `98_Shared/GameData/Formulas.cs` 신설 + `ComputeDamage(attackerStats, targetStats, baseDamage) → int` 순수 함수 + 단위 테스트 5건+ (경계값 / 음수 / overflow / 0 / 정상)
- [ ] Phase 02 = `CombatConstants.BaseDamage` → Formulas 위임 (`GameMap.ProcessAttack`의 `target.Hp -= CombatConstants.BaseDamage` → `target.Hp -= Formulas.ComputeDamage(attacker, target)`)
- [ ] Phase 03 = `PlayerEntity.PositionHistory` ring buffer (200ms / 4 tick 깊이) + `GetPositionAtTick(serverTick) → Vector2` 메서드
- [ ] Phase 03 = `C_Attack` PDL에 `attackerClientTick` 필드 추가 (or 기존 정합 활용) — 서버가 rewind 기준 시점 알 수 있게
- [ ] Phase 03 = precision hitbox = AABB (정합 단순) 또는 capsule (점프 정합) — Phase 01 Codex 리뷰 결과로 결정
- [ ] dotnet test green (회귀 0, M3 baseline 170+ → +단위 테스트 추가)
- [ ] M4.1-마감 별 -DONE.md 박음 (복잡 등급) + false-promise 점검 결과 섹션 의무 (ADR-024 cadence 첫 시범)
- [ ] CHANGELOG [M] entry 박음 (전투 정밀화 + Formulas 분리 + lag comp + hitbox)
- [ ] CONTEXT.md "⏸️ 현재 멈춤 지점" = M4.1 완료 + M4.2 진입 대기로 갱신
- [ ] commit + push + PR 게이트 (사용자 명시 GO 후 머지)

---

## ⚠️ 주의할 약속 (헌법 절대 원칙 충돌 가능 항목)

1. **헌법 #1 (Server Authority)** — Phase 02 Formulas.cs는 *클라/서버 공유 코드*. 클라가 Formulas를 *읽는* 건 OK (스탯 hint 표시 등), *권위 판정에 활용 X*. 데미지 최종 판정은 서버 단독 (`GameMap.ProcessAttack` 안에서만 적용).
2. **헌법 #2 (Protocol is Sacred)** — Phase 03 `C_Attack` 필드 추가 = PDL XML append-only + PacketGenerator 재생성 + Shared.dll commit 의무. **ProtocolVersion bump 4→5** (M3.8 Phase 03이 3→4 박은 후 본 M4.1 Phase 03이 4→5 별 bump 박음 — 2026-05-22 결정 박힘, 두 마일스톤 영역 분리 의미 + 시연 시점 호환성 추적 쉬움).
3. **헌법 #3 (Trust Boundary)** — Phase 03 lag compensation = "클라가 보낸 attacker tick 시점으로 rewind". 클라가 *과거 50 tick 거짓 보고* 시도 가능 → 서버측 rewind 범위 제한 (최대 4 tick = 200ms) + 범위 밖이면 silent drop (cheat-flag 후보, M4.2 cheat-flag 도입 시 기록).
4. **헌법 #4 (Shared Code Discipline)** — Phase 02 (Formulas.cs 신설) + Phase 03 (PDL `C_Attack` 변경 + ProtocolVersion bump) 모두 *Shared.dll 양쪽 영향* 변경. 변경 후 `dotnet build` 양쪽 컴파일 검증 + Unity 측 batchmode compile (`unity-bridge` SubAgent) + Shared.dll commit 정합 의무. CHANGELOG 2026-05-17 (Shared.dll 미commit 사고) 학습 정합.
5. **헌법 #5 (틱 블로킹 금지)** — Phase 03 ring buffer 갱신은 tick thread 안에서만, lock 없음 (GameMap actor 정합). `await`/`Task.Delay` 없음 의무.

---

## 📚 학습 포인트 (마일스톤 차원)

- **Pre-flight cross review 패턴 — 두 번째 실측** (M3 Phase 01 pre-flight smoke가 첫 번째). 본격 진입 *전* Codex β로 외부 시각 검증 = 사고 비용 ↓. 학부생 백지 단독 결정 위험 완화.
- **순수 함수 분리 (Formulas.cs)** — `static int ComputeDamage(...)` = side-effect 없음 + 입력만으로 결정 = 단위 테스트 용이 + 클라/서버 동일 결과 보장 (헌법 #4 "복사-붙여넣기 금지" 정신 정합).
- **lag compensation 본질** = "공정성 vs 권위 trade-off". 발쟁자 화면에선 적이 있었지만 서버는 이미 옮겨졌을 때, *공격 시점으로 rewind*. 단 rewind 범위 제한이 cheat 차단의 핵심.
- **precision hitbox AABB vs capsule** — AABB(축 정렬 박스) = 단순·빠름 / capsule = 점프·회전 정합. M3 단순 dist² < range² → AABB로 첫 승격 권장 (학부생 호흡).
- **PDL append-only + ProtocolVersion bump** — Phase 03 `C_Attack` 필드 추가 시 backward compatible이지만 옛 빌드 클라 호환성 위해 bump. 헌법 #2 가짜 약속 1번째 봉합(M3 Phase 02 handshake) 정합 패턴.

---

## ➡️ 다음 마일스톤

- **M4.2 — Map Transition** (진짜 4맵 분리 + portal handoff + 클라 측 dispatch + cheat-flag table + Serilog). 캡스톤 1 후반 = 6/3~6/10. 발표 데모 = M4.1 + M4.2 결과 종합.
- **M4.3 — AI + Polish** (enemy AI + boss behavior + jump Y mispredict 봉합 + PvP ADR + 마감 의례). 캡스톤 1 후 7월~10월.

---

## 갱신 이력

- 2026-05-22 — 본 세션 사용자 의논 후 박힘. M3 응급 코드 전수조사 필요 인지 → Phase 01 Codex 크로스 리뷰 흡수. M4 전체 7~9 Phase 추정을 3토막(M4.1/M4.2/M4.3) × 3 Phase로 분할.
- 2026-05-22 — M3.8 Capstone-1 Demo Infrastructure 신설 흡수 갱신. 사전 조건 = "M3.8 Phase 03 마감 (PlayerStats 박힘)" 추가. ProtocolVersion bump 3→4 → 4→5 정정 (M3.8 Phase 03이 3→4 박은 후 본 M4.1 Phase 03이 4→5). status `in-progress` → `pending` (M3.8 진입 우선). 시작 예정 5/22 → 5/27 (M3.8 마감 후), 마감 목표 6/3 → 6/2 (M4.2 진입 여유 동일).
- 2026-05-23 — M3.8 ✅ 완전 마감 + PR #49 머지 완료 (`2cd0fbc`) 흡수 갱신. **Phase 01 분담 정정** — 옛 "메인 직접 + Codex β 외부" → 새 "본인 (영호) Codex CLI 직접 호출 + Claude 점검 자료 + γ 비교 분담". 사유: (1) 옛 `/cross-review` 슬래시에 박힌 `codex review --files X --context Y` = false-promise 23번째 변종 발본 (실제 옵션 존재 X), (2) Claude가 Bash로 `codex` 호출 시 토큰 비용 ↑ + sandbox 옵션 결함 8회+ 누적 + 본인 학습 호흡 ↓, (3) 본인 직접 호출 = 비용 ↓ + 학습 호흡 ↑ + 본인 환경 sandbox 즉시 조정. memory [[unity-visual-work-user-owned]] "본인 직접" 정신을 외부 도구 호출 일반으로 확장 + memory [[codex-sandbox-permission-current-dir]]에 명령어별 옵션 표 박음 + 본인 직접 호출 default 절 박음. `/cross-review.md` 슬래시 정정 동반. 시작 예정 5/27 → 5/23 즉시 진입 검토 가닥 박힘 (별 의논 후 결정).
