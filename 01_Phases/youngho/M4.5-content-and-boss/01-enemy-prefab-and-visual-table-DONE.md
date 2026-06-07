---
owner: youngho
milestone: M4.5
phase: 01
title: 몬스터 prefab 전환 — EnemyVisualTable SO + 외관 디테일
status: done
completed: 2026-06-07
grade: 복잡
summary: M4.5 Phase 01 완료 (commit f224cd0 + 5ce2c17). EnemyViewFactory.BuildPlaceholder(149줄 런타임 조립) 은퇴 → EnemyVisualTable SO lookup + prefab Instantiate (new GameObject 0줄, kind 분기 0). prefab 2종 = Enemy_Normal(Slime)/Enemy_Boss(Boss_Vampire, 본인 제작 아트) — Mushroom/ToxicFrog 외부 에셋 113파일 삭제(guid 60개 참조 0 전수 확인). RemoteEnemy prefab-native 직렬화 전환 + InternalsVisibleTo 첫 도입. 검증 = EditMode 59/59(신규 10) + dotnet test 392/0/4 + Play 실측 4항목 + reviewer 🔴0/🟡3(2 반영). 발견 = 적 중력 부재(서버 AI 스폰 y 고정 — 이월).
---

# Phase 01 박제: 몬스터 prefab 전환 — EnemyVisualTable SO

**소요**: 세션19 (compact 후 연속 진입, 약 1.5h)

## TL;DR

적 시각 표현의 "구성의 진실"을 코드에서 에셋으로 옮겼다. 옛 `EnemyViewFactory.BuildPlaceholder`는 149줄짜리 런타임 GameObject 조립(sprite 로드 + HP바 자식 + offset 하드코드)이었고, 새 구조는 `EnemyVisualTable` SO(kind→prefab 매핑)를 lookup해 prefab을 Instantiate만 한다. 동시에 아트 스타일 통일 결정(2026-06-07)을 이행 — 외부 placeholder(Mushroom/ToxicFrog)를 은퇴시키고 본인 제작 Slime/Boss_Vampire prefab으로 교체. 새 적 추가 비용 = "prefab 1개 + 테이블 1행"이 됐고, Phase 02 골렘이 이 약속의 첫 실전 검증이다.

## 박제 사실

- **코드 (client Worker + 메인 검수)**: `EnemyVisualTable.cs` 신설(3단 fail-loud 폴백: 미등록 kind→LogError+Normal→Normal도 없으면 LogError+null — silent 빈 화면 금지) / `EnemyViewFactory.cs` 재작성(`Spawn(entityId, kind, x, y)` = lookup+Instantiate, 테이블 정적 캐시 1회 로드) / `RemoteEnemy.cs` prefab-native 전환(`[SerializeField] _visualFootOffset`·`_hpBarFill`·`_hpBarFullWidth`, `SetHpBar` 은퇴) / `EnemyRegistry.Spawn` 교체 + null drop
- **InternalsVisibleTo 첫 도입**: 테스트 전용 `SetEntriesForTest`가 internal인데 테스트는 별도 어셈블리(`Dawnholder.Client.Tests.EditMode`) → CS1061. `Scripts/AssemblyInfo.cs`로 테스트 어셈블리에만 internal 공개 (런타임 API public 오염 회피)
- **에셋 (Unity MCP RunCommand — 메인 세션 직접)**: `Prefabs/Enemies/Enemy_Normal.prefab`(Slime_Idle_0, 월드 2.05×1.48, HP바 y=1.73) / `Enemy_Boss.prefab`(Boss_Idle_0, 3.41×2.47, y=2.72) / `Resources/EnemyVisualTable.asset`(2행). HP바 sprite = 기존 `WhitePixel.png` 재사용. RunCommand 동적 어셈블리가 `Dawnholder.Client`를 직접 참조 가능 확인(Shared.dll과 달리 reflection 불요)
- **visualFootOffset = 0 확정**: Slime/Boss 둘 다 sprite pivot이 bottom-center(alignment 7)라 0 출발 → Play 실측으로 발 정합 확인. 옛 Boss -1.0f(ToxicFrog 투명 여백 보정)는 복사하지 않음 — plan 함정 항목 그대로 이행
- **은퇴 절차**: 두 폴더 113파일의 guid 60개를 씬/prefab/controller/anim/mat 전수 검색 → 참조 0 확인 후 별도 커밋으로 삭제 (Missing 사고 0)
- **commit**: `f224cd0`(코드+테스트+에셋 17파일) + `5ce2c17`(은퇴 113파일, −27,789줄). 브랜치 `feature/m4.5-01-enemy-prefab` (plan 브랜치에서 분기 — PR 한 장에 plan+Phase 01 동승)

## AC 검증 결과

- `EnemyViewFactory`에 `new GameObject(...)` 0줄 + AddComponent 0줄 (Instantiate 호출만) — grep 확인
- 클라 적 시각 코드에 EnemyKind if/switch 분기 0 (잔여 = enum cast + 테이블 키 비교뿐) — reviewer 교차 확인
- 미등록 kind 폴백 EditMode 테스트 green (LogAssert로 fail-loud 로그까지 단언)
- EditMode 전체 **59/59 green** (신규 10 = 테이블 lookup 4 + prefab 계약 6) — TestRunnerApi로 AI 자체 실행
- WSL2 `dotnet test` **392 통과 / 0 실패 / 4 skip** (M4.4 마감과 동일 — 서버 무변경 확인)
- Play 실측 (본인): 좌우 반전 ✅ / HP바 기능 ✅ / 크기감 ✅ / 발 정합 ✅ (footOffset 0 유지). 발견 1건 = 적 중력 부재(아래 이월)
- reviewer Tier 2-A: **🔴 0 / 🟡 3** — ① footOffset 0은 Play 게이트로 확인(완료) ② fullWidth==fill.localScale.x 일치 단언 추가(반영) ③ 테스트 헬퍼 prefix 강제(반영)

## 결정 흐름 (회고 참고용)

- **visualFootOffset/HP바 참조를 테이블이 아닌 prefab에** — 발 위치·바 폭은 스프라이트 아트의 속성이라 "시각 스냅샷"인 prefab이 소유하는 게 정합. 테이블은 kind→prefab 라우팅만 (단일 책임)
- **`_hpBarFullWidth`를 별도 직렬화 필드로** — fill의 초기 localScale.x를 런타임에 읽으면 Instantiate 직후 타이밍 의존 발생. 대가 = 같은 진실의 복제 2곳 → contract 테스트가 둘의 일치를 단언해 silent divergence 봉합 (reviewer 🟡 반영)
- **internal + InternalsVisibleTo vs public 헬퍼** — 테스트 전용 API를 public으로 열면 런타임 표면이 오염되고, private+reflection은 깨지기 쉬움. 어셈블리 단위 공개가 표준 절충
- **prefab 에셋에 Initialize 호출 금지** — Worker 초안은 prefab 에셋의 컴포넌트를 직접 호출해 "크래시 없음"만 단언(항상 통과 + 에셋 메모리 오염). SerializedObject로 직렬화 필드를 읽는 방식으로 교체 — EditMode 테스트는 에디터 API를 쓸 수 있다는 이점 활용
- **에셋 생성을 Unity MCP RunCommand로** — prefab YAML 손편집(Phase 08 사고 전례) 대신 에디터 API로 조립 + PrefabUtility.SaveAsPrefabAsset. sprite bounds를 코드로 읽어 HP바 y를 계산(하드코드 추정치 회피)

## 막혔던 지점 / 이월

- **적 중력 부재 발견 (Play 실측)**: 서버 적 AI는 스폰 y 고정으로 순찰 — 발판 끝을 넘어가거나 다른 층에 있으면 공중에 떠 있음. placeholder 시절부터의 서버 물리 제약(M4.4 "적은 평탄 스폰" 결정의 배경)이라 본 Phase 회귀 아님. **이월**: 적 중력(지형 높이 샘플링)은 서버 물리 작업감 — M4.5 범위 밖, 마커 저작으로 우회 지속
- Worker 테스트 결함 2건을 메인 검수에서 발견(무의미 DoesNotThrow + prefab 에셋 mutate) — SubAgent 검수 의무 4회째 적중
- **Phase 02 이월**: EnemyKind 중복 정의(서버/클라) 98_Shared 이사 결정 포인트 + 골렘 행 추가(테이블 첫 실전)

## 학습 일지 후보 키워드

데이터 주도 시각 장착(ClassConfig 동형 반복) / prefab = 구성의 스냅샷(코드 진실→에셋 진실 이동과 그 대가) / InternalsVisibleTo(테스트 격리의 표준 절충) / SerializedObject 테스트(에셋 오염 없는 직렬화 필드 단언) / fail-loud 3단 폴백 / TestRunnerApi 자체 실행 경로

## 다음 Phase

- **Phase 02 — 골렘 추가** (데이터만으로 새 적 = 본 테이블 구조의 첫 실전 검증, EnemyKind 이사 결정 포인트 포함)
