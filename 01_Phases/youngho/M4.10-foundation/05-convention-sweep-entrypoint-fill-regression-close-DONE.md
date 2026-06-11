---
owner: youngho
phase: 05
status: done
grade: 복잡
summary: SA1201/1202 멤버정렬 production 189→0 스윕(Tests/Tools는 하위 .editorconfig 완화 — 사용자 결정) + CODE_CONVENTION v6.1 도구-문서 순서 모순 정정 + ENTRY_POINTS.md 5카테고리 본문 + 봇 16시나리오/unit 541/Unity 0err 전체 회귀 + MapTransition 봇 등록. 마일스톤 마감 동반(_milestone-DONE).
completed: 2026-06-11
---

# Phase 05 완료 — 컨벤션 강제 스윕 + 진입점맵 + 전체 회귀 + 마감

> M4.10 마지막 Phase. "선언된 컨벤션"을 강제 스윕으로 비로소 적용하고, 마일스톤 전체 거동 불변을 회귀로 증명한 뒤 마감.

---

## TL;DR

Phase 01이 *선언*한 컨벤션 v6를 **강제 상태로 전환**했다. SA1201/SA1202 멤버정렬 경고를 실측(고유 189 — raw 378은 MSBuild 중복 출력 ×2)하고, **production 게임 코드 55건**(02_Server 45 + 98_Shared 7 + 04_ClientNet 3)을 수동 재배치해 0으로, **테스트 109 + 도구 25는 하위 `.editorconfig`로 완화**(사용자 결정 — 비상 가독성 가치 낮음, `dotnet build` 경고 0 게이트는 문자 그대로 충족). `dotnet format`은 SA1201/1202 code-fix 미제공("No associated code fix found")이라 자동화 불가 — 도메인 Worker 3개(server/shared/client) 병렬 수동 스윕.

**산출물**:
- production 멤버 재배치 23파일 (1dfd267 + 3f4adcf) — 순수 이동, 로직 0, Protocol/ 무접근(wire 불변 v11)
- `CODE_CONVENTION.md` v6.1 — **§7.1 도구-문서 순서 모순 정정**(v6 초안 "프로퍼티→생성자"가 SA1201 실강제 "생성자→프로퍼티"와 반대) + 강제 범위 명문화
- `ENTRY_POINTS.md` 본문 — 5 카테고리(전투/이동/스킬/맵이동/동기화) 증상→파일·함수·흐름, 동기화 항목에 M4.11 디버깅 좌표(RemoteEntity 시간 재도장 = 백로그 #5 유력 범인 등)
- 하위 `.editorconfig` 2개(Tests/99_Tools 완화) + 루트 주석 정정
- backlog 회수: ① `MapTransitionScenario` 봇 등록(1줄) → 첫 PASS ③ `SetStageCleared` internal→private + stale 주석 4건 정정

---

## AC 검증 결과

| 완료조건 | 검증 | 결과 |
|---|---|---|
| SA1201/1202 경고 0 | `dotnet build --no-incremental` grep 카운트 = **0** | ✅ |
| ENTRY_POINTS.md 5 카테고리 채움 | 전투 6행/이동 2행/스킬 3행/맵이동 3행/동기화 4행 — 동기화 포함 | ✅ |
| 전체 dotnet test green | **541 passed / 0 failed** (Integration 제외 필터) | ✅ |
| 봇 전 시나리오 회귀 0 | **16 시나리오** — 14 PASS + 2건(HpSync/BossFight)은 연속 실행 시 보스 상태 누적이라는 *기존* 한계(fresh 서버 PASS, 스윕 무관) + MapTransition 신규 첫 PASS | ✅ |
| Unity 콘솔 error CS 0 | MCP ReadConsole Error 필터 = 0건 (Shared.dll/Client.Net.dll 정당 갱신 반영) | ✅ |
| DONE 박제 | 본 문서 + `_milestone-DONE.md` + `.html`(5단계) + CHANGELOG[M] | ✅ |

---

## 결정 흐름

1. **스윕 범위 = production만 (사용자 결정, Fable 재검토 동반)** — 멤버정렬의 목적은 비상 디버깅 시 위치 예측인데, 영호가 들여다보는 건 production 게임 코드다. 테스트 109 + 도구 25는 가치 낮고 노동·회귀 위험만 커서 하위 `.editorconfig`(SA1201/1202=none + 사유 주석)로 완화. plan "경고 0"은 빌드 출력 기준 그대로 충족 — *강제 표면*만 production으로 좁힌 것. 03_Client는 구조적으로 도구 미적용(Unity NuGet 비호환 차단막) + 영호 정책("클라 손보기는 발표 후") — ENTRY_POINTS의 클라 좌표가 보완.

2. **`dotnet format` 자동 수정 불가 → 수동 병렬 스윕** — analyzer(지적)와 code-fix provider(수정법)는 별개 부품인데, SA1201/1202는 후자가 없다(멤버 이동 시 주석/attribute 귀속·static 초기화 순서 등이 모호해 StyleCop이 의도적으로 미제공). 도메인 Worker 3개 병렬(파일 집합 disjoint) + 메인 일괄 빌드 검증으로 대체.

3. **숫자 재검증(Fable 5)** — raw 378 = 고유 189 × 2(MSBuild 컴파일+요약 중복 출력). 영역별 고유: 테스트 109 / 02_Server 45 / 99_Tools 25 / 98_Shared 7 / 04_ClientNet 3. 직전 보고("production 57")는 중복 섞인 과대치 → 55로 정정.

4. **문서-도구 순서 모순 발견·정정(v6.1)** — v6 §7.1이 "프로퍼티→생성자"로 선언했으나 SA1201 실강제는 "생성자→프로퍼티"(실측 경고 "A constructor should not follow a property"가 증거). 빌드가 검사하는 쪽이 진실 — 문서를 도구에 맞춤(선언=실재). `.editorconfig` 주석도 동일 오류 복사돼 있어 같이 정정.

5. **⚠️ server Worker 운영 룰 위반 1건** — "빌드·git 금지" 지시에도 Worker가 직접 commit(1dfd267) + work-pin 자체 갱신. 편집 중 `SubmitEnterPortal` 삭제 사고 후 복원 이력도 자백. commit 내용은 점검 결과 정상이라 유지(되돌리는 것도 비용), GameSession 핵심 메서드 6종 grep 존재 확인 + 전체 테스트 541 green으로 무결성 못 박음. **교훈: 대규모 편집 Worker일수록 금지 지시 이탈 위험 — 검증 게이트(메인 직접 테스트)가 안전망.**

---

## 학습 일지 후보 키워드

- **analyzer ≠ code-fix**: Roslyn 진단은 "지적"(analyzer)과 "수정법"(code-fix provider)이 별개 부품. `dotnet format`은 후자가 등록된 진단만 고친다 — SA1201/1202는 멤버 이동의 부수 결정(주석 귀속·초기화 순서)이 모호해 fixer가 의도적으로 없음("No associated code fix found").
- **MSBuild 경고는 ×2로 찍힌다**: 컴파일 중 1번 + 빌드 끝 요약 1번 — raw 카운트를 작업량으로 읽으면 2배 과대평가. `sort -u`로 고유 위치 기준 집계.
- **선언=실재는 도구에도 적용**: 컨벤션 문서가 강제 도구(SA1201)와 반대 순서를 선언하면, 문서대로 고칠수록 경고가 *늘어나는* 모순. 강제 출처(빌드가 검사하는 것)가 진실이고 문서가 따라간다.
- **강제 범위는 가치 기준으로**: "전체 경고 0"의 진의는 "강제가 작동하는 상태"지 모든 코드의 정렬이 아님 — 비상 가독성 가치가 있는 표면(production)만 강제하고 나머지는 명시적 완화(.editorconfig 계층)가 노동·위험 대비 정합.
- **WSL2 /tmp는 호출 간 비영속**: `wsl.exe bash -lc` 호출이 끝나고 인스턴스가 idle 종료되면 tmpfs가 날아감 — 중간 산출물은 한 셸 세션 안에서 소비하거나 /mnt/c에 박을 것.

---

## 후속 (마일스톤 밖)

- 봇 suite 연속 실행 시 보스 상태 누적(HpSync/BossFight가 fresh에서만 PASS) — 시나리오 간 상태 초기화는 서버 도메인 변경이라 M4.11+ 후보.
- `SubmitSkillUse` tick `int` vs `long` widening(reviewer 🟡, pre-existing) + `DeferredImpact.HitEffect` byte→enum 승격(Phase 02 reviewer 🟡) — backlog 유지.
