---
owner: youngho
phase: M7.7 구조 리팩토링 (P0~P6)
status: DONE
grade: 대규모
summary: behavior-invariant 구조 정비 — Network/ 소멸·Entities/ 통합·GameMap 분해·데이터화로 M8 토대 확보. 거동 불변(WSL2 708/0/5 + 봇 16/16 + reviewer 🔴0 + wire bump 0).
milestone: M7.7
date: 2026-06-20
branch: feature/m7.7-structure-refactor
visualization: _milestone-DONE.html
---

# M7.7 — 구조 리팩토링 마일스톤 종합 (DONE)

> 시각화: [`_milestone-DONE.html`](_milestone-DONE.html) · 등급: **대규모** · 23커밋 · 162파일

## TL;DR

코드 **거동을 한 줄도 안 바꾸고**(behavior-invariant) 폴더·네임스페이스·데이터 경계를 정리해 **M8(DB 영속화) 토대**를 깨끗이 닦았다. `GameServer/Network/` 3중 오버로드 폴더 소멸(→ `Sessions/` + `Maps/Transitions/`), 흩어진 엔티티 `Entities/` 통합, GameMap God-class 분해, 적/스킬 switch 산발 → 데이터 카탈로그(OCP). 23커밋 전부 **WSL2 708/0/5 비감소 + 봇 16/16**로 기계 검증, reviewer 🔴0, wire 불변.

## AC 검증 결과

plan §9의 가독성 AC(사람이 읽을 수 있음):

- **AC-R1** (기능 추적 3파일 이내): ✅ `FEATURE_MAP.md`가 기능별 Entry/Trust gate/Orchestration/State owner/Notification/Client mirror 좌표 제공 + P6 경로 sync 완료.
- **AC-R2** ("새 EnemyKind 추가" 수정 ≤2~3곳): ✅ `EnemyCatalog`(P5a) 데이터화 — 새 적 = 데이터 1행.
- **AC-R3** ("Map transition"이 Network/ 폴더에 없음): ✅ `MapMigration` → `Maps/Transitions/` 이동, `GameServer/Network/` 폴더 소멸.
- **AC-R5** (PlayerEntity 저장/휘발 구분): ✅ `PlayerSnapshot` DTO(P4c) — 저장 후보(Hp/Position/Stats) vs 휘발(input queue/FSM) 경계.
- **AC-R6** ("새 스킬 추가" 만질 클라 파일 감소, switch 3→데이터 1): ✅ `ClientSkillCatalog`(P5b) — 쿨다운/예측/연출 switch → 테이블.

기계 게이트 검증 명령 + 출력:

```
# WSL2 회귀 (매 이동 후 + 최종)
$ wsl -d Ubuntu -- dotnet test Dawnholder.slnx --no-build
  Passed!  - Failed: 0, Passed: 708, Skipped: 5, Total: 713

# 봇 전 시나리오 회귀
$ bash 99_Tools/run_bot_regression.sh
  ########## REGRESSION SUMMARY: PASS=16 FAIL=0 ##########

# 빌드 (서버) + 클라 Unity 컴파일
  Build succeeded. 0 Error(s)   /   Unity 콘솔 Error 0

# Windows→WSL Play 접속
  Test-NetConnection 127.0.0.1:7777 → TcpTestSucceeded: True
```

reviewer(Tier-2, 고위험): 🔴 0 — 72파일 전수 "old↔new 내용 diff = namespace+using으로만 환원" 기계 확인. 🟡 dead using 2건은 `ddcda7b`로 정리. Play 육안(영호 bucket-b): 이상 무.

## 결정 흐름

- **M8 *전* 리팩토링** (영호 2026-06-20): DB 스키마는 되돌리기 비싸다 → 영속화가 붙을 곳(엔티티·스냅샷 경계)을 먼저 정리. M8 차단 최소선 = P0 + P4.
- **ADR-033 (Accepted)**: 폴더 = 개념 = 네임스페이스 일치. 이동은 ADR 승인 + frozen grep 통과 후만(P6).
- **A(전체) vs B(보수)** → **A 채택**: D5·D6(Combat 재편 + 엔티티 통합)을 M8 전 포함. ADR 파일 권장은 B였으나 "이동 defer 거부, real reorg가 목표" 방향으로 A. 단점=D6(PlayerEntity 23참조) 最高위험 → 이동마다 게이트로 방어.
- **P6 = 맨 마지막**: 파일 이동이 frozen 참조 깨뜨릴 위험 최대(memory `project-reorg`) → P4/P5 정착 + ADR 승인 후.
- **behavior-invariant 방법론**: `git mv` + NS 변경 + 빌드 에러 구동 using sweep(컴파일러가 worklist 제공, 추측 X). done = 외부 기계 심판(WSL2/봇/reviewer), 자기판단 X (engine:goal).

## 5단계 보고

### 1. 무엇을 만들었나
`GameServer/Network/` 폴더 소멸 → `Sessions/`(GameSession·IntentRateLimiter) + `Maps/Transitions/`(MapMigration). `Entities/` 신설 + PlayerEntity·EnemyEntity·EnemyState 통합. GameMap 분해(MapPacketPublisher·PlayerPhysicsSystem·EnemyGravitySystem·PlayerSnapshot DTO). 적/스킬/부팅 데이터화(EnemyCatalog·ClientSkillCatalog·CombatBootstrap installer). 폴더=NS 정합(핸들러·Maps/Systems·98_Shared).

### 2. 왜 필요한가
M8(DB 영속화)이 흔들리는 토대 위에 안 올라가게. "Network" 폴더가 세션/전송/존이동 3개 의미를 오버로드해 읽는 사람이 매번 의심하던 비용(Codex #5) 제거. 새 적/스킬 추가 시 산탄총 수술(switch 산발) → OCP 데이터 1행. 자매 엔티티가 두 폴더에 흩어져 M8 저장 추상화가 붙을 데가 모호하던 것 → `Entities/` 한곳.

### 3. 어떻게 만들었나
engine:goal AI-driven 구동으로 P0~P6 23커밋. P0(FEATURE_MAP+ADR) → P1~P3(NS/그룹화, 이동 0~저위험) → **P4(M8 핵심 분해)** → P5(데이터화) → ADR-033 Accepted → **P6(실제 이동, D2~D6 각 독립 commit)**. P6 이동법 = `git mv` + namespace + 빌드에러구동 using sweep(enclosing NS 규칙으로 churn 최소화) + 매 이동 WSL2 708 비감소.

### 4. 테스트 결과
WSL2 **708/0/5**(매 이동 비감소) · 봇 **16/16 PASS** · reviewer **🔴0**(72파일 전수 거동 byte 불변) · build 0err(서버+클라 Unity) · byte 동치 테스트 9(P4a wire shape 불변) · Play 육안 이상 무 · wire/Protocol.Version 불변.

### 5. 다음 스텝
M8 = DB 영속화 착수(`feature/m8-persistence`). 토대 활용: `Entities/` 통합 + `PlayerSnapshot` 경계 = M8 스냅샷 청사진, 큐드 라이터(헌법 §5). 미결: 계정 식별 / 큐드 라이터 / 30초+이벤트 cadence (ADR-005 LocalDB+EF Core 10).

## 학습 일지 후보 키워드

- **behavior-invariant 이동 방법론**: git mv + NS + 빌드에러구동 using sweep (컴파일러 = worklist 오라클, 추측 0)
- **enclosing 네임스페이스 규칙**: 자식 NS는 부모 타입을 using 없이 봄 → 이동 churn 최소화 판단에 활용
- **frozen 참조 분류**: CODEOWNERS(active, 0이어야) vs active 문서(sync) vs frozen -DONE(append-only 수용) — 3분류
- **WSL2 백그라운드 서버 지속성**: `wsl -- bash -lc` 백그라운드는 VM 수명에 묶여 끊김 → `setsid` 데몬화 + 별도 호출 검증 필요
- **P6 reorg는 ADR 승인 게이트**: 대량 이동 전 이름/경계 기준 ADR 확정(Codex #4) — 이동이 frozen/serialized 참조 깨뜨리는 위험 선차단
