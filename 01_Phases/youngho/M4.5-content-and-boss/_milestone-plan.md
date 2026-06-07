---
owner: youngho
milestone: M4.5
title: Content & Boss — 몬스터 prefab 전환 + 골렘 + UI 연결 + 보스 양방향 전투
status: planned
grade: 대규모
risk: irreversible
estimated: 13~19h (총합, 6 Phase)
domain: shared+server+client+qa
---

# M4.5 — Content & Boss (콘텐츠 + 보스)

> **상태**: planned — 2026-06-07 세션18 `/work:plan` (M4.4 마감 직후, 유현 UI 핸드오프 문서 기반 UI 범위 의논 확정)
> **선행**: M4.4 완전 마감 (PR #67, main `c2a3406`)
> **배경**: M4.4 plan의 M4.5 스케치 + 옛 M4.3-09 보스 정의(설계 완료 상태로 이월) + 유현 `_handoff-ui-art-20260605.html`

---

## 🎯 마일스톤 목표

게임의 **콘텐츠 층**을 채운다: 적 시각 표현을 placeholder 런타임 조립에서 **prefab + 데이터 테이블 구조**로 전환하고, 세 번째 적 종류(**골렘**)를 데이터만으로 추가해 그 구조를 검증하고, 유현 UI 아트를 **서버 실값 표시**로 연결하고, 맞기만 하던 보스를 **스스로 공격하는 보스**(페이즈 1/2 + 양방향 권위 전투)로 만든다. 발표 데모의 클라이맥스 = 보스방 양방향 전투.

**핵심 설계 (세션18 의논 확정)**:
- **적 시각 = ClassConfig 패턴 재사용**: M4.4-05의 "SO lookup 데이터 장착"을 적에게 적용 — `EnemyVisualTable` SO(EnemyKind → prefab)로 `EnemyViewFactory` 런타임 조립 은퇴. 새 적 추가 = 데이터 1행.
- **골렘 = 구조 검증 콘텐츠**: EnemyKind append(2) + 스탯 factory + 씬 마커 + 시각 1행 — 엔진 코드 변경 0이 목표 (bump 0).
- **UI 범위 (사용자 확정)**: HP 실연결(보스 패킷 연동, mock 은퇴) + MP는 `UpdateMP` normalized 훅만(풀 바 고정) + 미니맵 ②(HUD 이동 + 프레임/맵 이름만, RenderTexture 카메라 이월) + EXP/퀘스트/스킬/다이얼로그/인벤 명시 이월.
- **보스 = 옛 M4.3-09 설계 계승**: 페이즈 1/2(HP 50% 임계) + tick 카운터 쿨다운(헌법 #5) + `S_EnemyAttack` 신설 + 사망 정책(스폰 리스폰 + HP full, 데모 최소 정책).
- **ProtocolVersion 8→9 = 본 마일스톤 유일 bump**: `S_EnemyAttack` 신설 + `S_PlayerJoin` characterClass append를 **Phase 04 한 묶음**으로 처리 (M4.2 학습 — bump는 묶어서 한 번).

---

## 📋 Phase 분해 (6개)

| # | Phase | 등급 | 도메인 | 예상 | risk |
|---|---|---|---|---|---|
| 01 | 몬스터 prefab 전환 (EnemyVisualTable SO + 외관 디테일) | 복잡 | client+unity-bridge | 2~3h | unity-asset(prefab) |
| 02 | 골렘 추가 (EnemyKind=2, 데이터만 — 엔진 변경 0 검증) | 복잡 | shared+server+client+씬 | 1.5~2h | unity-asset(씬) |
| 03 | UI 연결 (미니맵 HUD 이동 + 맵 이름 + MP 훅) | 보통 | client | 1.5~2h | unity-asset(씬) |
| 04 | 보스 프로토콜 + 서버 행동 (S_EnemyAttack + class append + **8→9 bump** + BossBehavior) | **대규모** | shared+server | 4~6h | irreversible(bump) + trust-boundary |
| 05 | 보스 클라 연출 + 원격 직업 표시 + HP 실연결 | 복잡 | client | 3~4h | unity-asset(prefab) |
| 06 | 회귀 + 마감 (cross-review + 발표 풀 루프 + PR) | 보통 | qa | 1~2h | irreversible(PR) |

**총 등급 = 대규모** (4 도메인 관통 + ProtocolVersion bump + prefab/씬).

---

## 🔗 의존성 그래프

```
01 (prefab 전환)
   ↓
02 (골렘 — 시각 테이블에 행 추가)     03 (UI 연결 — 01/02와 병렬 가능)
   ↓                                  ↓
04 (보스 프로토콜+서버 — 8→9 bump)  ←─┘   ※ 04도 서버 측이라 03과 병렬 가능
   ↓
05 (보스 클라 — 01 prefab + 03 HUD + 04 패킷 모두 필요)
   ↓
06 (회귀 + 마감)
```

**병렬 가능**: Phase 03 ↔ 01/02/04 (UI 연결은 적 시각·보스 서버와 의존성 0 — 단 03의 맵 이름은 기존 `S_MapTransition` 사용이라 04 불필요). 학습 호흡상 직렬 진행 권장, 병렬은 일정 압박 시 옵션.

---

## ✅ 마일스톤 완료 조건

- [ ] 적 시각이 prefab + `EnemyVisualTable` SO 경유 — `EnemyViewFactory` 런타임 조립 코드 은퇴, 클라 코드에 적 종류 if/switch 0
- [ ] 골렘이 HuntingGround에서 순찰/추격 — 추가 과정에서 엔진 코드(AI/전투/스폰) 변경 0 입증
- [ ] HP 바 = 서버 실값 (mock 은퇴), 미니맵 = HUD 상시 표시 + 현재 맵 이름
- [ ] 보스가 패턴 공격 + HP 50% 페이즈 2 전환 + 범위 내 플레이어만 서버 권위 데미지
- [ ] 플레이어 HP 0 → 스폰 리스폰 + HP full (데모 무중단)
- [ ] 원격 플레이어가 상대 직업 모습으로 보임 (S_PlayerJoin characterClass)
- [ ] **ProtocolVersion == 9** (Phase 04 유일 bump — 04 전후 다른 Phase bump 0)
- [ ] `dotnet test` green + 봇 전 시나리오 PASS (BossFightSmoke 신설 포함)
- [ ] 발표 데모 풀 루프 Play 실측 (마을 → 사냥터(골렘 포함) → 보스 양방향 전투 → StageClear)
- [ ] CHANGELOG + PR 머지 (사용자 GO) + 5단계 보고 MD/HTML

---

## 🚫 이번에 명시적으로 뺀 것 (세션18 의논 확정)

- **EXP/레벨/MP 실값** — 서버 스탯 체계 신설 회피 (스킬/성장 마일스톤에서). MP 바는 훅+풀 바 표시만
- **퀘스트/스킬/다이얼로그/인벤토리 UI 기능** — 서버 대응 시스템 부재 (M5~M6 영역). 아트 배치 그대로 보존
- **미니맵 RenderTexture 카메라** — 프레임+맵 이름까지만 (정석 코스는 이월)
- **보스 telegraph 정밀 튜닝** — 기본 예고 모션만, 회피 밸런스는 콘텐츠 밸런스 마일스톤
- **맵 간 enemy respawn 정책 / cheat-flag table / Serilog** — M4.3부터 이월 유지
- **지형 v2** (drop-through/이동 플랫폼/적 AI 지형 추적) / **PortalTable bake** / **NetworkService SRP** — 기존 이월 유지

---

## ➡️ 다음 마일스톤

- **M5 Persistence** — DB 연결 + 캐릭터/인벤토리 영속화. **선행 결정 = LocalDB Linux 부재 해소** (ADR-029 트레이드오프 ④ — WSL2에서 SQL Server 컨테이너 vs Windows LocalDB 분리 운영)

---

## 갱신 이력

- 2026-06-07 — 신설 (세션18 `/work:plan` — M4.4 마감 직후. UI 범위는 유현 핸드오프 문서 기반 사용자 의논 4항목 확정 반영)
