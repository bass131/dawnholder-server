---
owner: youngho
milestone: M4.4
title: World & Player — 타일맵 지형 충돌(서버 권위) + 직업 조작 분리
status: planned
grade: 대규모
risk: unity-asset
estimated: 20~32h (총합, 6 Phase — 세션13 Phase 03 대규모 상향 반영)
domain: shared+server+client
---

# M4.4 — World & Player (지형 + 직업)

> **상태**: planned — 2026-06-06 세션11 `/work:plan` (승인된 세션 plan 기반)
> **선행**: M4.3 Phase 12 경량 마감 (현 브랜치 PR 머지) 후 진입
> **배경 plan**: 세션11 승인 plan — 탐색(플레이어 구조/몬스터 구조/SOLID 스캔) + Plan agent 설계 + 사용자 결정 3건(조작 전반 분리/적 prefab 전환/건드는 김에 분리) 반영

---

## 🎯 마일스톤 목표

유현이 깔아둔 **타일맵 지형(언덕·단차·공중 지형)을 서버 권위 물리로 구현**하고(데모 동선의 바닥 — 필수), **직업(전사/원거리) 조작을 컴포넌트 장착 구조로 분리**한다(조작 클래스 내부 if-분기 0). 두 작업 모두 `98_Shared/GameData/Physics.cs`를 관통하므로 한 마일스톤으로 묶고, 지형을 먼저 직렬 처리한다.

**핵심 설계 (세션11 승인)**:
- **지형**: Unity 타일맵 = 저작 도구, 충돌 데이터 = 98_Shared 단일 진실. 에디터 bake 스크립트(`TerrainBaker`)가 솔리드 셀 → **98_Shared 생성 C# 데이터** 출력 (PacketGenerator 코드젠 문화 정합). `Physics.Step` 평지(GroundY=0) → 솔리드 AABB + one-way 플랫폼. 서버·클라 prediction이 같은 생성 데이터 → drift 0, **프로토콜 변경 0**.
  - **세션13 전환 (2026-06-06)**: 산출물을 생성 C# → **바이너리 파일(무결성 헤더) + 런타임 로드**로 전환 + bake 범위를 오브젝트 위치(스폰 마커·kill-plane)까지 확장. `MapSpawnTable`/`MapTerrainData.cs` 은퇴. 상세 = Phase 03 문서 D1~D6 (미래 맵 에디터 마일스톤의 기반).
- **직업**: 단일 prefab + `ClassConfig` ScriptableObject (CharacterClass → AnimatorController + MoveSpeed/JumpVel + 공격 전략). `LocalPlayerController`(206줄 God class 4책임) → Input/Movement/AttackStrategy 분리. `PlayerAnimatorSync`(옛 IsMoving) → `LocalPlayerMotion`+`AnimatorDriver`(08b 계약) 교체 완성.
- **과추상 경계(§0.3)**: SO 2종(ClassConfig) + 전략 인터페이스 1까지. 그 이상 금지.

---

## 📋 Phase 분해 (6개)

| # | Phase | 등급 | 도메인 | 예상 | risk |
|---|---|---|---|---|---|
| 01 | 지형 bake 도구 (TerrainBaker → 98_Shared 생성 데이터) | 복잡 | client(에디터)+shared | 2~4h | — |
| 02 | 지형 물리 (Physics.Step 솔리드 AABB + one-way 플랫폼 + 단위 테스트) | 복잡 | shared | 3~5h | — (기술 난도 최고 — /cross-review 후보) |
| 03 | 맵 데이터 파이프라인 전환(바이너리 bake + 런타임 로드 + 스폰/kill-plane bake) + 지형 통합 실측 | **대규모** (세션13 상향) | shared+server+client+qa | 8~12h | unity-asset(씬) |
| 04 | 직업 이동 분리 (PlayerStats.JumpVel + 파라미터 주입 + LocalPlayerController 4분할) | 복잡 | shared+server+client (shared/server는 ~20줄 연결) | 3~4h | — |
| 05 | 직업 장착 구조 (ClassConfig SO + IAttackStrategy + AnimatorDriver 교체 + Mage 투사체 연출) | 복잡 | client | 3~5h | unity-asset(prefab) |
| 06 | 회귀 + 마감 | 보통 | qa | 1~2h | irreversible(PR) |

**총 등급 = 대규모** (3도메인 관통 + Physics 코어 교체 + prefab). **전 마일스톤 ProtocolVersion 8 불변** (bump 0 — 지형·직업 모두 패킷 무관).

---

## 🔗 의존성 그래프

```
01 (bake 도구)
   ↓
02 (지형 물리)  ←── 기술 난도 최고, /cross-review 후보
   ↓
03 (지형 통합 실측)
   ↓
04 (직업 이동 분리)  ←── 같은 Physics.Step이라 03 후 직렬
   ↓
05 (직업 장착 구조)
   ↓
06 (회귀 + 마감)
```

**전부 직렬** — 01→02→03은 지형 파이프라인 순서, 04는 02가 바꾼 Physics 시그니처 위에서 작업(충돌 회피 — **Physics.Step 호출부 12파일을 02와 04가 공유**: 서버 3/테스트 6/클라 2/봇 1, plan-auditor 실측), 05는 04의 분할 구조 위에 장착. 병렬 없음.

---

## ✅ 마일스톤 완료 조건

- [ ] 세 씬(Town/HuntingGround/BossRoom) 지형 bake 생성 데이터 박힘 + 재실행 idempotent
- [ ] 언덕·단차·공중 지형 위 이동/점프/착지 Play 실측 정상 (서버 권위 — 클라 끄면 서버 위치가 진실)
- [ ] reconcile drift 봇 회귀 0 (지형 위 prediction 일치)
- [ ] 상호작용 지형 범위 확정 박힘 (구현 or 명시 이월)
- [ ] 전사 vs 원거리: 이동속도(4 vs 6)·점프 체감 차이 Play 실측 + 조작 코드 if-class 분기 0
- [ ] Knight 점프체인/공격랜덤/직업별 모션 관측 (M4.3 Phase 11 이월 체크리스트)
- [ ] `dotnet test` green (회귀 0 + 지형 물리 신규 테스트)
- [ ] ProtocolVersion == 8 유지 (bump 0 검증)
- [ ] CHANGELOG entry + PR 머지 (사용자 GO)

---

## 🚫 이번에 명시적으로 뺀 것

- **drop-through**(아래로 내려가기)·**이동 플랫폼** — 지형 v2 후보, 이월
- **적 AI 지형 추적 이동** — 적은 평탄 구간 스폰 배치로 해결 (M4.5 몬스터 Phase에서 스폰 좌표만)
- **맵 에디터 저작 도구** — 기존 이월 유지 (이번엔 bake 추출 + 바이너리 파이프라인까지 — 에디터 *제작*은 여전히 이월)
- **포탈 bake** — `PortalTable` 보존 (M4.2 race 검증 완료 코드), 포탈의 데이터 파일 이행은 M4.5+ 이월
- **멀티 AI 오케스트레이터 도구** — 본인 제작 백로그 (세션13 의논 — 99_Tools/ 후보, 게임 외 도구 프로젝트)
- **원격 플레이어 직업 표시** — S_PlayerJoin class append 필요 (PDL bump) → M4.5 보스 Phase의 v9 묶음
- **NetworkService SRP 분리** — 이번에 안 건드림 → 이후 이월
- **몬스터 prefab 전환 + 골렘 / UI 5묶음 / 보스** → M4.5 content-and-boss

---

## ➡️ 다음 마일스톤 (M4.5 content-and-boss 스케치 — 정식 분해는 M4.4 마감 후)

01 몬스터 prefab 전환(EnemyVisualTable SO) → 02 골렘 서버 추가(EnemyKind=2, bump 0) → 03 UI 결정 게이트(유현 문서 대기) → 04 보스(기존 09 + S_PlayerJoin class append, **8→9 유일 bump**) → 05 마감(/cross-review + 발표 풀 루프).

---

## 갱신 이력

- 2026-06-06 — 신설 (세션11 마일스톤 재편 — M4.3 입자 초과분 분리, 승인 plan 기반 6 Phase 분해)
- 2026-06-06 (세션13) — **Phase 03 재정의**: 맵 데이터 관리 방식 의논 결과 반영 — 바이너리 bake + 런타임 로드 + 스폰/kill-plane bake 확장, 등급 복잡→대규모 (총 예상 14~24h → 20~32h)
