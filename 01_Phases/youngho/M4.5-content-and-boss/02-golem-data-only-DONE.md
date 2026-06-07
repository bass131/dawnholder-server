---
owner: youngho
milestone: M4.5
phase: 02
title: 골렘 추가 — EnemyKind=2, 데이터만 (엔진 변경 0 검증)
status: done
completed: 2026-06-07
grade: 복잡
summary: M4.5 Phase 02 완료 (commit 9de94f6 + a4bba0b). 골렘(EnemyKind=2) 추가 — 데이터 4곳(enum/스탯/씬/시각)으로 새 적 완성, 단 화석 분기 3곳 정정 동반(사유 박제). 사용자 결정으로 EnemyKind 중복 정의를 98_Shared 단일 정의로 이사(헌법 #4 봉합). GolemDefault = HP60/방어5/속도1.2/aggro4/patrol2.5. 검증 = dotnet 399/0/4(신규 7) + EditMode 63/63(신규 4) + bake idempotent(hexdump kind=2 실측) + 봇 PASS + Play 실측. ProtocolVersion 8 유지(bump 0).
---

# Phase 02 박제: 골렘 추가 — 데이터만으로 새 적

**소요**: 세션19 (Phase 01 머지 직후 연속, 약 1h — server/client Worker 병렬)

## TL;DR

세 번째 적 골렘을 "엔진 코드 변경 0"으로 추가하는 게 목표였고, 결과는 **데이터 4곳(enum 행/스탯 factory/씬 마커/시각 테이블 행) + 화석 분기 3곳 정정**. 화석 3곳은 실패가 아니라 수확이다 — "적은 2종"이라는 암묵 가정(`!= Normal`이면 전부 Boss 취급)이 3종째에서 깨졌고, 긍정 비교(`== Boss`)로 바꾼 지금부터는 4종째가 진짜 데이터만으로 들어간다. 동시에 사용자 결정으로 EnemyKind 서버/클라 중복 정의를 98_Shared 단일 정의로 이사해 헌법 #4 위반 소지를 봉합했다.

## 박제 사실

- **EnemyKind 98_Shared 이사 (사용자 결정)**: `98_Shared/GameData/EnemyKind.cs` 신설(Normal=0/Boss=1/**Golem=2** append) — 서버 `Combat/EnemyKind.cs` 삭제 + 클라 `RemoteEnemy` 중첩 enum 삭제, 양쪽 모두 Shared 단일 정의 소비. wire는 byte cast 그대로라 **ProtocolVersion 8 유지(bump 0)**. plan-auditor가 우려한 "Shared.dll 02·04 두 번 흔들림"은 미발생 — GolemDefault가 어차피 Formulas.cs(98_Shared)를 만져 02에서 Shared.dll이 흔들리므로 이사 동승 비용 0. 공유 코드 대대적 정리는 별도 논의로 이월(사용자)
- **GolemDefault 수치 (구현 시 결정 약속 이행)**: MaxHp=60(Normal 2배 — Warrior 3타/Ranger 4타), Defense=5(데미지 25→20/22→17), MoveSpeed=1.2(<2.0 스펙), AggroRange=4.0(좁음), PatrolRange=2.5(<Aggro invariant). `EnemyDefaultHp.ByKind={30,100,60}` — GolemDefault.MaxHp와 일치 단언 테스트 동반(drift 방지)
- **화석 분기 3곳 정정 (AC "엔진 변경 0" 예외 — 사유)**: ① `EnemyAISystem.cs:36` `!= Normal`→`== Boss` (골렘이 보스 Idle 분기에 빨려드는 버그 차단) ② `EnemyEntity` ctor 초기 State 동형 정정 ③ `GameMap.SpawnEnemy` stats ternary→switch(+`default => default` fail-safe). 전부 기존 2종 가정의 최소 정정이며 신규 로직 0 — Boss Idle 불변 회귀 테스트(`Boss_StaysIdle_AfterGolemBranchFix`)로 보스 동작 무변경 증명
- **에셋/씬 (메인 세션, Unity MCP)**: `Enemy_Golem.prefab`(Golem_Idle_0, 월드 4.45×2.73 — 보스보다 큼, **사용자 확인 = 의도된 크기**) + `EnemyVisualTable.asset` 3행 + `Spawn_Enemy_Golem.prefab`(Normal 마커 복제) + HG 씬 (5.5, 0.5) 배치(지상층, patrol [3,8] — 기존 마커와 겹침 없음). 씬 백업 선행(`.claude/state/scene-backups/2026-06-07-m4.5-02/`)
- **bake 검증 (hexdump 실측)**: `map_1.content.bin` 68→77 bytes — +9 = EnemySpawnPoint 1개(kind byte + float×2) 정확히. 엔트리 `02 | x=5.5 | y=0.0`(지면 스냅 적용 확인). Town/BR bin 무변경(idempotent 4회째). GetConsoleLogs 빈 응답은 디스크 실물 검증으로 우회(M4.4-01 학습 재적용)
- **commit**: `9de94f6`(서버+Shared — server Worker 자체 commit) + `a4bba0b`(클라+에셋+씬+bin+Shared.dll 동반)

## AC 검증 결과

- 엔진 코드 변경: 화석 분기 3곳 정정만(사유 위 박제) — 신규 적 추가 자체는 enum append + factory + 테이블/씬 행으로 완결. 4종째부터 진짜 0 예상
- HG 골렘 순찰/추격/사망 Play 실측 (본인): 렌더 ✅ / AI patrol·chase ✅(Idle 박힘 없음 — 화석 정정 동작 확인) / 크기 의도대로 ✅
- bake idempotent: Town/BR bin 무변경 + content.bin 증분 9 bytes 정확(hexdump)
- WSL2 `dotnet test` **399 통과 / 0 실패 / 4 skip** (GolemTests 7 신규: 스탯 3 + AI 3 + Boss Idle 회귀 1)
- EditMode **63/63 green** (골렘 테이블/계약 4 신규)
- 봇 EnemyAiSmoke PASS (골렘 포함 5마리 배치 — success=True, patrol/chase/이동 관측)
- **ProtocolVersion == 8 유지** (PDL/Generated 접촉 0 — reviewer 교차 확인)
- reviewer Tier 2-A: **🔴 0 / 🟡 1** (ARCHITECTURE.md EnemyKind 블록 stale → 본 박제와 한 묶음 정정 완료)

## 결정 흐름 (회고 참고용)

- **이사를 02에 동승 (04 합류 옵션 기각)** — plan-auditor 🟡의 전제(Shared.dll 2회 흔들림)가 실측에서 깨짐: GolemDefault 추가만으로 02가 이미 Shared.dll을 만짐. 동승 비용 0이면 일찍 봉합이 정답
- **부정 조건 분기는 새 종류를 잘못된 쪽으로 빨아들인다** — `!= Normal`은 "Normal 아니면 전부 Boss"라는 단정. 적 3종째에서 깨졌고, 긍정 화이트리스트(`== Boss`)가 확장에 강함. switch `default => fail-safe`도 같은 정신
- **화석 정정 vs AC 사수** — "엔진 변경 0"을 지키려고 골렘을 Boss 분기에 두는 우회는 거짓 준수. AC가 예견한 "위반 시 사유 박제" 경로가 정직한 선택
- **GolemDefault 수치는 전투 체감 역산** — 추상적 "단단함"이 아니라 "Warrior 3타/Ranger 4타"라는 체감 단위로 역산해 Defense/MaxHp 확정 (테스트 주석에 근거 박음)

## 막혔던 지점 / 이월

- server Worker가 자체 commit(`9de94f6`)까지 진행 — 결과물 검수는 통과했으나 commit 권한은 메인 세션 게이트가 원칙. 다음 위임 프롬프트에 "commit 금지" 명시 예정
- **이월**: 공유 코드 대대적 정리 논의(사용자 — EnemyKind 이사를 시작점으로 Constants/CombatConstants 등 경계 재검토) / 적 중력 부재(Phase 01 이월 지속) / ARCHITECTURE.md 외 문서의 enum 위치 stale 가능성(발견 시 정정)

## 학습 일지 후보 키워드

콘텐츠 추가 비용 = 아키텍처 성적표 / 부정 조건 분기의 함정(긍정 화이트리스트) / 공유 타입 이사 패턴(shared-code-discipline 2회째) / 체감 단위 역산 밸런싱 / hexdump 바이너리 검증

## 다음 Phase

- **Phase 03 — UI 연결** (보통, 04와 병렬 가능) 또는 **Phase 04 — 보스 프로토콜+서버** (대규모, 8→9 유일 bump)
