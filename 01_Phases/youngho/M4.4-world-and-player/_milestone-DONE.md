---
owner: youngho
milestone: M4.4
phase: milestone-closeout
title: World & Player — 지형 + 직업 마일스톤 마감
status: done
completed: 2026-06-07
grade: 대규모
summary: M4.4 완전 마감 (6 Phase 풀세트, PR #63~#66 + 마감 PR). 유현 타일맵 지형이 씬 저작→bake→바이너리(terrain.bin/content.bin)→런타임 로드 파이프라인으로 서버 권위 물리가 됐고(StepWithTerrain AABB + one-way 발판), 직업(Warrior/Ranger) 조작이 ClassConfig SO 데이터 장착 구조로 분리됐다(조작 코드 직업 분기 0, 이동속도 4/6 실연결). 부수 인프라 2건 — GitHub Actions CI 신설 + ADR-029 WSL2 실행 표준(SAC 차단 해소, 로컬 테스트 부활). 최종 회귀 = 클린빌드 0/0 + test 392/0/4(WSL2=CI 동일) + 봇 5/5(desync 0) + Play 직업 2종×세 씬 매트릭스 이상 무 + ProtocolVersion 8 불변(전 마일스톤 bump 0 약속 이행). 5단계 보고 시각판 = _milestone-DONE.html.
---

# M4.4 — World & Player 마일스톤 박제

**마감 일자**: 2026-06-07 (세션18, Phase 06 회귀 + 마감)
**Phase 수**: 6/6 완료 (01~05 개별 PR 머지 + 06 본 마감 PR)
**등급**: 대규모 (3+ 도메인 관통 — shared/server/client/qa + unity-asset/irreversible 깃발)
**WORK-ID**: m4.4-world-and-player
**시각 보고서**: [`_milestone-DONE.html`](_milestone-DONE.html) — 대규모 5단계 보고 HTML 박제

---

## 5단계 보고

- 🎯 **무엇을 만들었나** — ① 유현이 깔아둔 타일맵 지형(언덕·단차·공중·발판)이 서버 권위 물리로 동작하는 데이터 파이프라인(씬 저작 → bake → 바이너리 → 서버·클라·봇 3경로 런타임 로드). ② 직업(전사/원거리)별 이동·점프·공격이 ScriptableObject 데이터 장착으로 갈리는 구조(조작 코드 직업 분기 0).
- 🤔 **왜 필요한가** — 옛 구조는 맵의 진실이 코드 두 군데(생성 C# + 하드코딩 테이블)에 흩어져 씬 수정마다 drift 위험이었고, 직업은 죽은 값(전 직업 5.0)으로 사실상 한 직업이었다. 콘텐츠(M4.5 몬스터/보스)가 늘기 전에 바닥 구조를 데이터 주도로 바꿔야 했다.
- 🛠️ **어떻게 만들었나** — 6 Phase 직렬: bake 도구(01) → 지형 물리(02) → 바이너리 파이프라인(03, 대규모) → 직업 이동 분리(04) → 직업 장착 구조(05) → 회귀+마감(06). 핵심 선택 3건은 본문 "핵심 설계 결정" 참조. 부수로 CI 신설 + WSL2 실행 표준(ADR-029)이라는 인프라 2건이 SAC 사고에서 파생됐다.
- 🧪 **테스트 결과** — 최종 회귀(세션18): 클린빌드 경고 0/오류 0 + `dotnet test` **392 통과/0 실패/4 skip**(WSL2 로컬 = CI 동일) + 헤드리스 봇 5종 전부 PASS(desync 0.00) + Play 실측 직업 2종 × 세 씬 매트릭스 이상 무 + `ProtocolVersion == 8` 불변(전 마일스톤 bump 0 약속 이행).
- ➡️ **다음 스텝** — M4.5 content-and-boss 정식 분해(`/work:plan`, 유현 UI 문서 입수 후): 몬스터 prefab 전환 → 골렘 → UI 게이트 → 보스(8→9 유일 bump) → 마감. 이월 백로그는 본문 "이월 명시" 참조.

---

## TL;DR (🎯 무엇 / 🤔 왜)

M4.4는 게임의 **바닥(지형)과 손(직업 조작)**을 데이터 주도 구조로 갈아끼운 마일스톤이다.

**지형**: 옛 구조는 평지(GroundY=0) 하드코딩 + 맵 데이터가 생성 C#(`MapTerrainData`)과 하드코딩 테이블(`MapSpawnTable`) 두 군데에 흩어져, 씬을 수정할 때마다 코드 동기가 사람 손에 달려 있었다. 이제 Unity 씬은 저작 도구일 뿐이고, `TerrainBaker`가 구운 `terrain.bin`(공유)/`content.bin`(서버 전용)이 단일 진실이다. 서버·클라 prediction·봇이 같은 파일 + 같은 `Physics.StepWithTerrain`을 쓰므로 지형 위에서도 desync 0.

**직업**: `PlayerStats.MoveSpeed`(4/6)가 정의만 있고 전 직업이 `Constants` 5.0을 쓰던 죽은 값을 `MoveParams` 필수 인자로 실연결하고, Animator·공격 전략을 `ClassConfig` SO lookup으로 장착해 조작 코드에서 직업 if/switch 분기를 0으로 만들었다. Warrior(4)와 Ranger(6)의 체감 속도 차이는 의도된 정정.

**경계(헌법 정합)**: `content.bin`(적/플레이어 스폰)은 클라 빌드에 미배포 — 클라는 적의 존재를 `S_EntitySpawn` 패킷으로만 안다(헌법 #1). 파일 경계가 곧 신뢰 경계.

---

## Phase 박제 요약

| Phase | 제목 | 핵심 | 머지 |
|---|---|---|---|
| 01 | 지형 bake 도구 | 레이어 약속(Tilemap_Solid/Platform) + `TerrainBaker`(행 run+수직 병합) + 씬 3분리 | PR #63 (`7fd8dc0`) |
| 02 | 지형 물리 | `Physics.StepWithTerrain` — 솔리드 AABB(축 분리+교차 판정 tunneling 방지) + one-way 발판. 호출부 12파일 무변경(오버로드) | PR #63 (`c5e615e`) |
| 03 | 맵 데이터 바이너리 파이프라인 (대규모) | `MapDataFile`(DWMP+CRC32 fail-closed) + terrain/content.bin + 서버 `MapDataLoader`+kill-plane + 3경로 대칭 + `MapSpawnTable`·`MapTerrainData` 은퇴 | PR #64 (10 commits, 71파일) |
| 04 | 직업 이동 분리 | `MoveParams` 필수 인자(silent fallback 타입 발본) + 죽은 값 4/6 실연결 + LPC 228줄 4분할 | PR #65 |
| 05 | 직업 장착 구조 | `ClassConfig` SO lookup(분기 0) + 로컬 애니 hybrid + `PlayerStats.ForClass` + **CI 신설** | PR #66 |
| 06 | 회귀 + 마감 | 발판 저작(세션17 — Phase 03 E 잔여 발본) + **ADR-029 WSL2 표준** + 회귀 풀세트 + 본 박제 | 본 마감 PR |

**Phase 06 포함분 (세션17~18)**:
- **one-way 발판 저작** — `PlatformPlank` Tile 신설(Cainos sprite) + HG 5발판 11셀 + BR 6발판 12셀(섬 보물상자 도달 가능화) + 재bake(HG platforms=5/BR=6/Town=0 — idempotent 3회째 확인). 두 맵 공중 지형은 발판 전까지 전부 도달 불가였음(진입 단차 +2~5 vs 최대 점프 1.6u).
- **ADR-029 WSL2 실행 표준** — SAC(Smart App Control)가 풀 리빌드 후 fresh unsigned dll의 CoreCLR 로드를 차단(차단 대상 비결정 "빌드 룰렛") → WSL2 Ubuntu를 로컬 dotnet 실행(서버/봇/test) 표준 경로로 채택. 세션16에 포기했던 로컬 테스트 부활(392/0/4 = CI 동일). Windows `dotnet build`는 유지(ADR-010 Unity DLL 공급).

---

## 결정 흐름 (🛠️ 어떻게 — 회고 참고용)

1. **바이너리 bake vs 생성 C# 유지** — 생성 C#은 컴파일 필요 + Unity/서버 별도 경로 + 콘텐츠 수정=코드 diff. 바이너리는 재컴파일 불필요 + 한 로더 공유 + 포맷 버전 축 분리(파일 formatVersion ≠ ProtocolVersion). 단점 = diff 불가 산출물 → CRC32 fail-closed + 실디코드 검증으로 흡수. 미래 맵 에디터 마일스톤의 기반.
2. **`MoveParams` 필수 인자 (옵셔널 금지)** — 옵셔널이었다면 죽은 값(5.0 fallback)이 또 조용히 살아남았을 것. 타입 시스템이 silent fallback을 원천 차단. "fallback 회귀 0"이 기존 미연결 값의 실연결을 가릴 수 있다는 M4.3 학습의 적용.
3. **과추상 경계 준수 (§0.3)** — SO 2종(ClassConfig) + 전략 인터페이스 1까지로 제한. 직업이 2개뿐인 시점에 직업 시스템 프레임워크를 만들지 않음.
4. **발판 = 출발면 +1 도달 스펙** — 최대 점프 1.6u(JumpVel 8/G -20, 틱 적분 ~1.8)라 계단/지그재그식 저작만 가능. 하향 점프(↓+점프) 미구현 = 명시 스펙(지형 v2 이월). 판자 비주얼 = one-way 시각 언어(솔리드 흙다리와 구분).
5. **SAC 사고 → 실행 환경 표준화로 승화** — 응급 우회(git HEAD 신뢰바이트)에 머물지 않고 WSL2 PoC 5항목 게이트(빌드/서버/봇/test/TCP relay)를 통과시켜 표준으로 채택. 로컬-CI 검증 환경이 같은 ubuntu 계열로 수렴한 부수 이득.

---

## AC 검증 결과

Phase 06 최종 회귀 (2026-06-07 세션18, WSL2 = ADR-029 표준 경로):

- [x] 세 씬 bake 산출물 박힘 + idempotent — bin 두벌(98_Shared+StreamingAssets) byte-identical, Town 무변경 3회째 재확인
- [x] 언덕·단차·공중 지형 이동/점프/착지 Play 실측 — 세션17(발판+WSL2 경유) + 세션18 매트릭스(직업 2종 × Town/HG/BR × 이동/점프/발판/공격/맵 전환) 전부 이상 무
- [x] reconcile drift 봇 회귀 0 — M2BasicMovement desync (0.00, 0.00)
- [x] 상호작용 지형 범위 — **명시 이월** (Phase 01에서 Interact 레이어 기각 박제, 상호작용은 오브젝트 축으로)
- [x] 전사 vs 원거리 이동속도(4 vs 6) Play 체감 + 조작 코드 if-class 분기 0
- [x] Knight 점프체인/공격랜덤/직업별 모션 관측 (Phase 05 -DONE, M4.3 이월 체크리스트 소화)
- [x] `dotnet build --no-incremental` 경고 0/오류 0 + `dotnet test --no-build` **392/0/4 skip** (skip 4 = 기존 장기 통합 박제분, 맵 이동은 MapTransitionIntegrationTests로 커버)
- [x] 헤드리스 봇 5종 신선 서버 일괄 PASS — M2(desync 0)/MultiRoster/EmergencyCombat(rate-limit drop 확인)/BossStageClear(중복 억제)/EnemyAi(patrol→chase)
- [x] `ProtocolVersion.Current == 8` — **전 마일스톤 bump 0 약속 이행** (다음 bump = M4.5-04 8→9 유일)
- [x] CHANGELOG entry + PR 머지 (사용자 GO 게이트) — 본 마감 PR

---

## 이월 명시 (➡️ 다음)

- **M4.5 정식 분해 트리거**: 본 PR 머지 + 유현 UI 문서 입수 → `/work:plan`. 스케치 = 01 몬스터 prefab(EnemyVisualTable SO) / 02 골렘(bump 0) / 03 UI 게이트 / 04 보스(8→9 유일 bump + S_PlayerJoin class append) / 05 마감
- **몬스터/보스 디테일** — Phase 06 Play 실측에서 본인 확인: 일반/보스 몬스터 외관·연출 디테일 미흡 = M4.5-01/04에서 흡수 (기본 동작은 전부 정상)
- **Mage(Ranger) 투사체 연출** — Phase 05에서 이월 (공격 판정 자체는 서버 권위로 동작 중)
- **지형 v2 후보** — drop-through(하향 점프)/이동 플랫폼/적 AI 지형 추적
- **reviewer 🟡 봇 2건** — probe 좌표 lock 비대칭 / EmergencyCombat 첫 마리 전제 대칭화
- **CI actions v5 bump** — Node20 deprecated, 2026-06-16부터 Node24 강제(run annotation 실측) + 봇 CI 편입 검토
- **LocalDB Linux 부재** — M5 영속화 진입 시 결정 (ADR-029 트레이드오프 ④)
- **옛 브랜치 6개 정리** / **원격 플레이어 직업 표시**(S_PlayerJoin append, M4.5-04 v9 묶음) / **NetworkService SRP** / **PortalTable bake**(M4.5+)

---

## 학습 일지 후보 키워드

파일 경계 = 신뢰 경계(content.bin 미배포) / 바이너리 산출물의 diff 불가 비용과 CRC32+실디코드 상쇄 / 필수 인자로 silent fallback 타입 발본 / 죽은 값 실연결은 의도된 정정으로 plan에 명시 / 발판 도달 가능성 = 점프 물리 적분의 저작 제약 / SAC 평판은 바이트 단위(빌드 룰렛) / 응급 우회 vs 환경 표준화 승화 / 로컬-CI 검증 환경 수렴(WSL2=ubuntu)
