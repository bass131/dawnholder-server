---
owner: youngho
milestone: M4.4
phase: 04
title: 직업 이동 분리 — Physics 파라미터 주입 + LocalPlayerController 4분할
status: done
completed: 2026-06-06
grade: 복잡
summary: M4.4 Phase 04 완료 (3 commits, 세션15). β10 죽은 값(PlayerStats.MoveSpeed 4/6 정의만 있고 전 직업 Constants 5.0 사용) 실연결 — MoveParams 필수 인자로 Physics.Step에 주입, silent fallback을 타입 시스템에서 발본. 서버는 GameMap 틱에서 p.Stats 주입, 클라는 PlayerPrefs 선택 클래스→PlayerStats factory(임시, Phase 05 ClassConfig가 정식 출처). LPC 228줄 God class를 LocalPlayerInput/LocalPlayerMovement/IAttackStrategy로 분할, prefab 컴포넌트 교체(본인). 검증 = build 0/0 + test 392/0/4skip + 봇 신선 서버 5/5 + Unity EditMode 37/0 + Play sanity(서버 로그 증빙) + reviewer 🔴0. 프로토콜 변경 0, ProtocolVersion 8 불변.
---

# Phase 04 박제: 직업 이동 분리

**소요**: 세션15 단일 세션 — server/qa/client Worker 3단 분담 + 메인 검수 + reviewer
**선행**: Phase 03 머지(PR #64) 직후 main `b2b6213` 기준 새 브랜치

## TL;DR

전사(이속 4)와 레인저(6)가 **서버 권위로 다르게 움직이고 클라 prediction이 일치**한다. 옛 구조는 `PlayerStats.MoveSpeed`(4/6)가 정의만 있고 전 직업이 `Constants.MoveSpeed=5.0` 하드코딩으로 움직이는 죽은 값 상태(β10) — 이번에 `Physics.Step`이 `MoveParams`(MoveSpeed/JumpVel)를 필수 인자로 받게 바꿔 실연결했다. 5.0→4/6 체감 변화는 의도된 정정. 동시에 `LocalPlayerController` 228줄 4책임 God class를 입력 번역(`LocalPlayerInput`) / 예측+송신+서버응답(`LocalPlayerMovement`) / 공격 위임(`IAttackStrategy`)으로 분할해 Phase 05 직업 장착(ClassConfig SO + 전략 구현)이 꽂힐 구조를 만들었다.

## 박제 사실

| 스테이지 | 산출 | commit |
|---|---|---|
| A shared+server+bot | `PlayerStats.JumpVel`(W 4/8, R 6/8 단일 출처) + `MoveParams` readonly struct + `Physics.Step` 4-인자 필수(옛 2/3-인자 삭제) + `Constants.MoveSpeed`·`Physics.JumpSpeed` const 은퇴 + GameMap 틱 `p.Stats` 주입 + 봇 5시나리오 Warrior factory 적응 | `6673dbc` |
| B client 분할 | LPC 228줄 4책임 → `LocalPlayerInput`(입력 번역) / `LocalPlayerMovement`(predict+50ms 송신+reconcile, Instance 승계) / `IAttackStrategy`+`NearestTargetAttackStrategy`(공격 위임, Phase 05 ClassConfig 주입 통로) + `PlayerPredictor` ctor `MoveParams` 필수 + 참조처 교체·주석 stale 정리 | `94a7340` |
| C prefab | LocalPlayer.prefab 컴포넌트 교체(본인 Editor 작업, MCP 검증: Missing 0 + SendMessages) + 셸 파일 은퇴 | `29ebc45` |

## 결정 흐름

1. **silent fallback을 타입으로 발본** — 옛 오버로드를 남기면 "5.0으로 조용히 도는" 경로가 잔존. 삭제해서 *미연결 = 컴파일 실패*로 강제 (Phase 03 GameWorld provider 필수 인자와 같은 정신).
2. **MoveParams struct (두 float 낱개 X)** — MoveSpeed/JumpVel 둘 다 float이라 호출부 자리 바꿈이 컴파일에 안 잡힘 → 명명 필드로 방지.
3. **클라 직업값 = prediction 파라미터, 권위 아님 (헌법 #1)** — PlayerPrefs 조작으로 Ranger(6) 사칭해도 서버는 자기 Stats(4)로 움직이고 snapshot reconcile이 정정. 치팅으로 빨라지는 게 아니라 화면만 잠깐 어긋났다 복귀. 클라·서버 모두 98_Shared factory 단일 출처 (헌법 #4).
4. **JumpVel 값 분화는 안 함** — 이번 Phase는 통로만 (W/R 모두 8f). 직업별 점프 차등은 디자인 결정 대기.
5. **지형 기하 테스트는 명시적 (5,8) 유지** — 좌표·벽 위치가 5.0 기준 저작이라 직업값으로 바꾸면 전부 재계산. 테스트 목적(지형 의미론)과 직업값 검증(신규 4건)을 분리.
6. **작업 순서 = shared+server → bot → client 직렬** — GameServer.Tests가 HeadlessBot.csproj를 참조해서 봇을 고치기 전엔 테스트 컴파일 자체가 안 됨 (스테이지 게이트를 프로젝트 경계로 분리).

## AC 검증 결과

- `dotnet build Dawnholder.slnx --no-incremental` → 경고 0 / 오류 0
- `dotnet test Dawnholder.slnx --no-build` → **392 passed / 0 failed / 4 skipped** (신규 4: Physics 4:6 비례 / jumpVel 반영 / factory 고정값 4·6·8·8 / GameMap Warrior vs Ranger end-to-end — 메인 세션 직접 재실행으로 숫자 확인)
- 봇 신선 서버 일괄 **5/5 PASS** (M2BasicMovement / MultiRosterSmoke / EnemyAiSmoke / EmergencyCombatSmoke / BossStageClearSmoke — M2 desync 검증은 봇 자체 시뮬도 Warrior factory 주입)
- Unity 컴파일 green (`scriptCompilationFailed=False`, 신규 3타입 어셈블리 존재 확인) + EditMode **37/0/0** (직업 비례 신규 1 포함)
- Play sanity (본인): Warrior 이동→포탈 HG 진입→C_Attack 7발 — 서버 로그 증빙 (`CharacterClass set to Warrior — Spd:4`), Reconcile 폭주 없음
- reviewer 🔴0 / 🟡1 (= prefab 교체 대기 — 커밋 `29ebc45`로 해소)
- 직업 if-분기 0 (factory 선택 1곳), ProtocolVersion 8 불변

## 학습 일지 후보 키워드

- prediction 파라미터 vs 권위 상태의 경계 (클라가 직업값을 들어도 §1 합헌인 이유)
- silent fallback의 타입 시스템 발본 (오버로드 삭제 = 미연결 컴파일 실패)
- God class 분할의 실전 기준 — 입력/시뮬/전략/서버응답 4책임과 Instance 승계
- 같은 타입 인자 자리바꿈 함정 → 명명 struct
- 테스트 기준값 분리 — 기하 의미론(명시적 5,8) vs 직업값 검증(factory)

## 관측/이월

- **EnemyAiSmoke 간헐 flake 1회 관측** (첫 일괄 실행에서 entity=0 실패 → 같은 순서 신선 서버 재실행 일괄 green). 봇 probe 좌표 lock 비대칭(reviewer 🟡 이월분)과 같은 계열 의심 — 해당 파일 다음 작업 시 동반 봉합.
- **정밀 mispredict 측정 + 직업별 Play 실측은 Phase 05 묶음** (기존 사용자 결정 유지). 클라 직업값 임시 출처(PlayerPrefs)는 Phase 05 ClassConfig SO가 정식 승계.
- 옛 5.0 체감 대비 Warrior 4.0이 약간 느림 = **의도된 정정** (죽은 밸런스 값의 실연결).
