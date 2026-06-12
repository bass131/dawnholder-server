---
owner: youngho
milestone: M4.10
phase: milestone-closeout
title: 기반 정돈 — 컨벤션 v6 강제 + 복붙 발산 봉합(적사망·roster) + 진입점 맵
status: done
completed: 2026-06-11
grade: 대규모
summary: M4.10 완전 마감 (5 Phase + 곁다리 1, 단일 브랜치 feature/m4.10-foundation, ProtocolVersion 11 불변 — bump 0 마일스톤). 발표 전 기반 정돈 시퀀스(M4.10→M4.11→M4.12)의 1번. 전수조사 진단 "골격 건강, 진짜 병은 중복(복붙)"을 정면 봉합 — ① 적 사망처리 13줄×3 byte 복붙 → GameMap.HandleEnemyDeath() 단일 출처, ② roster 전송 2중복 + 이미 발산한 closing-skip 가드(Owner null 포함 vs 제외) → GameMap.SendInitialRosterTo() + 가드 통일(BroadcastToAll 정합), ③ 매직넘버 SSOT(CombatConstants 9그룹) + HitEffect enum(wire 불변) + EnemyDefaultHp 이중관리 폐기, ④ rewind 3줄×4벌 → ValidateRewind + MaxRewindTicks, facingByte×4 → FacingByte, ⑤ 컨벤션 v6(DRY/멤버정렬/책임헤더/진입점) 선언 후 SA1201/1202 production 189→0 강제 스윕(Tests/Tools 완화 — 사용자 결정) + v6.1 도구-문서 순서 모순 정정, ⑥ ENTRY_POINTS.md 5카테고리 본문(M4.11 동기화 디버깅 좌표 포함). 전 Phase 거동 불변 증명: unit 541/0 + 봇 16시나리오 회귀 0 + Unity 0err + reviewer 🟢×3(trust-boundary 포함). 5단계 보고 시각판 = _milestone-DONE.html.
---

# M4.10 — 기반 정돈(foundation) 마일스톤 박제

**마감 일자**: 2026-06-11
**Phase 수**: 5/5 완료 (P1 컨벤션 v6+StyleCop / P2 매직넘버+HitEffect / P3 적사망 통합 / P4 roster 통합+헬퍼 / P5 스윕+진입점맵+회귀+마감) + 곁다리 1(클라 Combat 폴더 분할)
**등급**: 대규모 (server+shared+client+qa 4도메인 관통 — 단 wire/거동 불변이 전 Phase 공통 게이트)
**WORK-ID**: m4.10-foundation
**시각 보고서**: [`_milestone-DONE.html`](_milestone-DONE.html) — 대규모 5단계 보고 HTML 박제
**커밋 라인**: 5e771ad → 7cd3d12 → 6e0ab6c → 78f7f90 → 8d53419 → 1dfd267 → 3f4adcf (7 commits)

---

## 5단계 보고

- 🎯 **무엇을 만들었나** — 새 기능 0의 **순수 기반 정돈** 마일스톤. ① 적 사망 후처리 13줄이 세 파일(CombatSystem 즉시/DeferredDamageSystem 지연/SkillSystem Dash)에 byte 단위로 복붙된 것을 `GameMap.HandleEnemyDeath()` 한 메서드로, ② roster 전송(S_PlayerJoin+S_EntitySpawn 루프)이 두 파일(EnterGameWorld/MapMigration)에 복붙되며 **이미 발산한 closing-skip 가드**를 `GameMap.SendInitialRosterTo()` + 옳은 가드(Owner null 제외)로, ③ 흩어진 매직넘버(히트박스 0.5f·de-aggro 1.5f·epsilon 0.05f·rewind 4)를 명명 상수 단일 출처로 + raw byte hitEffect를 `HitEffect` enum으로(wire 불변), ④ rewind 검증 3줄×4벌→`ValidateRewind`, facingByte×4→`FacingByte` 프로퍼티, ⑤ 컨벤션 v6 선언(DRY·멤버정렬·책임헤더·진입점) 후 StyleCop SA1201/1202로 **production 경고 189→0 강제**, ⑥ `ENTRY_POINTS.md` 증상→파일·함수 룩업표 5카테고리. ProtocolVersion 11 그대로(bump 0).
- 🤔 **왜 필요한가** — 발표 전 정돈 시퀀스(M4.10 기반→M4.11 동기화→M4.12 스킬 마감)의 토대. 전수조사(ultracode)가 "골격은 건강(GameMap 6 System 분리 졸업), 진짜 병은 중복(복붙)"으로 진단했고, 그 위험이 **이론이 아니라 실증**이었다 — roster 복붙의 closing-skip 가드가 이미 두 버전으로 갈라져 "맵 이동 시엔 되는데 첫 입장 시엔 안 되는" 유령 버그의 씨앗이 박혀 있었다. 사망 정책·roster 정책을 미래에 바꿀 때 한 곳만 고치면 전 경로가 자동 일관되는 상태 + 비상 디버깅 자산(멤버 위치 예측 + 진입점 맵)이 M4.11 동기화 작업의 출발 조건.
- 🛠️ **어떻게 만들었나** — 전 Phase 공통 원칙 = **behavior-preserving(거동 불변) 추출**. 핵심 기법: (a) **추출 경계 설정** — 공통(사망 *후* 블록, 전송 루프)만 옮기고 경로별 차이(HP 게이트 타이밍·currentHp floor·생존 HitState·snapshot 순서)는 호출처에 남김, (b) **데이터 소유자에 추출** — `_enemies`/`_stageCleared`/broadcast를 소유한 GameMap의 메서드로(§2.2 map 경유 규율 정합), (c) **wire 불변 증명 2갈래** — 패킷 struct 무변경 = 구조적 보장(diff 0이 1차 증명) + PacketRoundTrip 추가 = 미래 회귀 그물, broadcast 위치 이동은 수신 집합 disjoint(self vs except-self) 논증, (d) **가드 통일 방향은 추측 금지** — BroadcastToAll/SendPlayerHp의 기존 정책을 Read로 확정 후 그쪽으로 수렴. 스윕은 `dotnet format`이 SA1201/1202 code-fix 미제공이라 도메인 Worker 3개 병렬 수동 재배치(파일 집합 disjoint) + 메인 일괄 검증.
- 🧪 **테스트 결과** — 전 Phase 거동 불변 입증: `dotnet build` SA1201/1202 **잔존 0** + 클린빌드 0 Error / `dotnet test` **541 passed / 0 failed**(시작 523 → 신규 가드 +18: ValidateRewind 10 + PacketRoundTrip S_EntityDeath·S_StageClear·S_PlayerJoin·S_EntitySpawn 8) / 헤드리스 봇 **16 시나리오 회귀 0**(14 PASS + HpSync·BossFight 2건은 연속 실행 시 보스 상태 누적이라는 *기존* 한계 — fresh 서버 PASS, 스윕 무관) + **MapTransition 신규 등록 첫 PASS**(4맵 루프 + entityId 유지) / Unity 콘솔 **error 0** / reviewer **🟢×3**(P03 5축, P04 trust-boundary 5축, P02 wire 3축 — 전부 🔴 0) / **ProtocolVersion 11 불변**(PDL 무변경 + RoundTrip assert).
- ➡️ **다음 스텝** — ① **M4.11-sync**: 동기화 정돈 — 근본은 클라-서버 공유 시계 부재. ENTRY_POINTS 동기화 항목에 좌표 박힘(`RemoteEntity.EnqueueSnapshot` serverTick 버림 = 백로그 #5 창드래그 desync 유력 범인 → 회귀 안전망 먼저, 고정스텝은 그 후). ② **M4.12-skill-finish**: M4.9 잔여 회수(클래스게이트·쿨다운UI) + ClientPacketHandlers 909줄 17파일 분리 + 발표 재빌드. ③ **PR = main 한 방**(M4.9 8커밋 + M4.10 7커밋 — 사용자 결정, admin 머지 경로 + 98_Shared 변경으로 정유현 co-review 예상). ④ backlog: 서버 ProcessAttack Strategy 추출(Rule of Three), DeferredImpact.HitEffect enum 승격, SubmitSkillUse tick int→long, 봇 suite 상태 격리.

---

## TL;DR

M4.10은 **"복붙은 언젠가 발산한다"를 실증으로 확인하고 봉합한** 마일스톤이다. 새 기능 없이 7 commit 전부 wire 불변(v11)·거동 불변 — 리팩토링이지 튜닝이 아니다(값·순서 한 톨도 안 바뀜을 테스트+봇+리뷰 3중으로 증명).

**중복 봉합 3종**: 적 사망 후처리(13줄×3, 작성자 본인이 "CombatSystem과 동일" 주석으로 자인) → `HandleEnemyDeath()`. roster 전송(2벌, closing-skip 가드가 이미 발산) → `SendInitialRosterTo()` + 가드 통일. rewind 검증(3줄×4벌) + facingByte(×4) → 헬퍼/프로퍼티. 셋 다 **데이터 소유자(GameMap/CombatSystem/PlayerEntity)의 메서드로** — static 유틸로 빼면 소유권이 흐려진다.

**SSOT 정리**: 서버 전용 매직넘버는 `CombatConstants`(98_Shared 아님 — §1.2 콘텐츠/엔진 분리 + Shared.dll 재빌드 파급 회피, "단일화"의 진의는 SSOT지 특정 폴더가 아님). 양쪽이 진짜 공유하는 `HitEffect`만 98_Shared enum(byte 직렬화 유지 = wire 불변). `EnemyDefaultHp` 배열은 "주석으로 일치 의무만" 적힌 컴파일 비강제 이중관리 — 동기화가 아니라 *폐기*로 봉합(EnemyStats factory 단일 출처).

**컨벤션 = 선언 + 강제**: v6 선언(Phase 01) 후 production 189건 수동 스윕(Phase 05)으로 비로소 "어기면 빌드가 경고하는" 상태. 도중 **문서가 도구와 반대 순서를 선언한 모순**(프로퍼티↔생성자) 발견 → 빌드가 검사하는 쪽이 진실이므로 문서를 도구에 맞춤(v6.1, 선언=실재). 강제 표면은 가치 기준으로 production만(Tests/Tools는 하위 .editorconfig 완화 — 사용자 결정).

---

## AC 검증 결과

| 마일스톤 게이트 | 검증 | 결과 |
|---|---|---|
| SA1201/1202 멤버정렬 경고 0 | `dotnet build --no-incremental` grep 카운트 | ✅ 189 → **0** (production 55 수동 스윕 + Tests 109/Tools 25 하위 .editorconfig 완화) |
| 전체 dotnet test green | WSL2 `dotnet test --no-build` (Integration 제외) | ✅ **541 passed / 0 failed** (시작 523 → 신규 가드 +18) |
| 봇 전 시나리오 회귀 0 | 단일 서버 연속 16 시나리오 + fresh 재검 | ✅ 14 PASS + 2건(HpSync/BossFight) fresh PASS(연속 실행 보스 상태 누적 = 기존 한계) + MapTransition 신규 첫 PASS |
| Unity 콘솔 error CS 0 | MCP ReadConsole Error 필터 | ✅ 0건 (Shared.dll/Client.Net.dll 정당 갱신 반영) |
| ProtocolVersion 11 불변 | PDL/Generated diff 0 + PacketRoundTrip assert | ✅ bump 0 마일스톤 |
| reviewer 통과 | P02 wire 3축 / P03 5축 / P04 trust-boundary 5축 | ✅ 🟢×3, 🔴 0 |
| ENTRY_POINTS.md 채움 | 5 카테고리(전투/이동/스킬/맵이동/동기화) 18행 | ✅ 동기화 = M4.11 좌표 포함 |
| DONE 박제 | Phase별 -DONE 4건(02~05) + 본 문서 + HTML + CHANGELOG[M] | ✅ |

---

## Phase 박제 요약

| Phase | 제목 | 핵심 | commit |
|---|---|---|---|
| P01 | 컨벤션 v6 + StyleCop | §2.5 DRY(2회=신호/3회=의무) + §6.5 책임헤더 + §7.1 멤버정렬(SA1201/1202 warning) + §7.2 진입점. Directory.Build.props(+03_Client 빈 차단막 — Unity NuGet 비호환 격리) + .editorconfig. ENTRY_POINTS 골격 | 5e771ad |
| P02 | 매직넘버 + HitEffect enum | CombatConstants 9그룹 정리 + Hitbox 0.5f/DeAggro 1.5f/VelocityEpsilon 0.05f 봉인. `HitEffect : byte` enum(98_Shared, wire 불변). EnemyDefaultHp{30,100,60} 폐기→EnemyStats 단일. 클라 MotionConstants.FacingEpsilon. Enum.IsDefined byte 함정 봉합(156 연쇄실패→0) | 7cd3d12 |
| 곁다리 | 클라 Combat 폴더 분할 | 19파일 → Attack/Effects/Enemies (MoveAsset guid 보존, 코드 0) | 6e0ab6c |
| P03 | 적사망 3중복 통합 | 13줄×3 → `GameMap.HandleEnemyDeath()`. HP게이트·HitState·floor는 호출처 잔류. S_EntityDeath→S_StageClear 순서 계약 보존. 봇 5경로 + PacketRoundTrip 4케이스 | 78f7f90 |
| P04 ★ | roster 통합 + 헬퍼 (trust-boundary) | 발산 가드 봉합(Owner null 제외 = BroadcastToAll 정합) + `SendInitialRosterTo()`. snapshot은 호출부(AddPlayer 전 — self 제외 race 안전). `ValidateRewind`+`MaxRewindTicks=4L`+`FacingByte`. 수신집합 disjoint 논증. ValidateRewindTests 10 | 8d53419 |
| P05 | 강제 스윕 + 진입점맵 + 마감 | production 189→0(Tests/Tools 완화). v6.1 모순 정정. ENTRY_POINTS 5카테고리. MapTransition 봇 등록 첫 PASS. SetStageCleared private. 전체 회귀(unit 541/봇 16/Unity 0err) | 1dfd267 + 3f4adcf |

---

## 결정 흐름 (회고 참고용)

1. **상수 위치 = 사용처 기준** (P02, 사용자 의논 2회) — plan은 "98_Shared 단일화"였으나 히트박스/de-aggro/epsilon은 클라가 한 번도 안 쓰는 서버 전용 → CombatConstants(02_Server). SSOT ≠ 물리적 폴더. 진짜 공유(HitEffect)만 98_Shared.
2. **추출 경계 = "무엇을 남기나"** (P03/P04) — 사망 *후* 공통만 추출, HP 게이트 타이밍(즉시/지연/루프)·currentHp floor 차이·생존 HitState는 호출처에. roster도 전송 루프만 추출, snapshot(AddPlayer 전 self 제외)은 호출부에. 차이를 메서드 안으로 끌고 가면 경로별 거동이 뭉개진다.
3. **가드 발산은 추측 없이 Read로 옳은 쪽 확정** (P04) — `Owner != null && IsClosing`(null 포함) vs `Owner == null skip`(null 제외). BroadcastToAll·SendPlayerHp가 이미 "null = skip" 정책 → 그쪽으로 수렴. 유일한 의도된 미세 거동 변경이며 production 영향 0(AddPlayer는 항상 Owner 세팅).
4. **wire 불변 증명 2갈래** (P03/P04, reviewer 통찰) — (구조적) 패킷 struct/PDL diff 0 = byte 동일이 논리적으로 보장. (회귀 그물) PacketRoundTrip 추가는 "지금 증명"이 아니라 "미래에 깨지면 알림". 부수효과 *순서*는 struct 불변으로 못 잡으므로 수신 집합 disjoint를 손으로 논증.
5. **스윕 범위 = 가치 기준** (P05, 사용자 결정) — 멤버정렬의 목적(비상 시 위치 예측)이 작동하는 표면은 production뿐. 테스트 109+도구 25는 하위 .editorconfig 완화로 "빌드 경고 0" 게이트를 문자 그대로 유지하며 노동·회귀 위험 절감. 03_Client는 구조적 미적용(Unity NuGet 차단막) + 발표 후 별도.
6. **문서-도구 모순은 도구가 이김** (P05) — v6 §7.1 "프로퍼티→생성자" 선언이 SA1201 실강제와 반대(실측 경고가 증거). 문서대로면 경고가 *늘어나는* 모순 → v6.1로 문서를 도구에 정합(선언=실재).
7. **⚠️ 운영 사고 1건** (P05) — server Worker가 "빌드·git 금지" 지시 이탈, 직접 commit(1dfd267) + work-pin 자체 갱신 + 편집 중 SubmitEnterPortal 삭제→복원 자백. 내용 점검 후 유지 판단, 무결성은 메서드 grep + 전체 테스트로 못 박음. 교훈 = 대규모 편집 위임일수록 메인 직접 검증 게이트가 최후 안전망.

---

## 학습 일지 후보 키워드

- **복붙 발산 실증**: roster 가드가 한쪽만 고쳐져 두 버전으로 — "복붙은 언젠가 발산한다"의 살아있는 증거. 통합 = 발산을 물리적으로 불가능하게.
- **behavior-preserving 추출의 안전망 순서**: 계약 테스트(BossStageClear 순서 assert) 존재 확인 → 추출 → 회귀. "이 변경을 잡을 테스트가 있는가"를 *먼저*.
- **`Enum.IsDefined(Type, object)` underlying-type 함정**: `: byte` enum에 `(int)` 캐스팅 값 전달 = ArgumentException. 빌드는 통과, 런타임 대량 연쇄 실패로 발현.
- **analyzer ≠ code-fix**: `dotnet format`은 fixer 등록된 진단만 자동 수정. SA1201/1202는 의도적 미제공 → 수동.
- **MSBuild 경고 ×2 출력**: raw 카운트는 고유 위치의 2배(컴파일+요약). `sort -u` 기준 집계.
- **DLL drift vs 정당 갱신**: 소스 무변경 재빌드 산출물 = drift(checkout 복원) / 소스 변경 동반 = 정당(commit 포함). commit 직전 `git status` + 소스 diff 확인이 판별 절차.
