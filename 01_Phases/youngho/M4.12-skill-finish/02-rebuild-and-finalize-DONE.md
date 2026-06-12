---
owner: youngho
milestone: M4.12
phase: 02-rebuild-and-finalize
title: 발표 재빌드 + 전체 회귀 + M4.9·M4.12 마감 박제
status: done
grade: 복잡
slug: 02-rebuild-and-finalize
summary: M4.10/11/12 전부 포함된 발표용 클라를 C:\Dev\Build에 재빌드(Succeeded errors=0 7씬, Managed DLL 신선도 확인) + 전체 회귀 baseline 비감소 green(WSL2 561 / EditMode 123 / 봇 16 / 콘솔 0err) + M4.9·M4.12 마감 박제 회수.
created: 2026-06-12
completed: 2026-06-12
domains: [qa]
risk_flags: [irreversible]
---

# M4.12 Phase 02 — 발표 재빌드 + 전체 회귀 + 마감 박제 (DONE)

## TL;DR

발표용 클라(M4.10/11/12 전부 포함)를 `C:\Dev\Build`에 재빌드하고, 전체 회귀가 **M4.11 baseline 대비 비감소 green**임을 확인한 뒤, 오래 미뤄둔 **M4.9 마감 박제**(정의 7개 / -DONE 0개)를 마일스톤 -DONE 1장으로 회수했다. P02는 코드 변경 0(회귀+빌드+박제만) — wire v12·워킹트리 clean 유지. 회귀 환경 = WSL2 ADR-029(SAC 우회).

## AC 검증 결과

| 항목 | 명령 / 근거 | 결과 |
|---|---|---|
| WSL2 서버 테스트 | `dotnet test Dawnholder.slnx --no-build` (rsync→build→test) | **561 passed / 0 failed / 4 skipped** (=baseline 561 비감소) |
| Unity EditMode | TestRunnerApi 콜백 `[P02-EditMode] RESULT` | **passed=123 failed=0 skipped=0** (baseline 119 +4 = SkillHudControllerTests) |
| 봇 16시나리오 (연속) | `run_bot_regression.sh` | 13 PASS / 3 FAIL(BossFight·HpSync·Freeze — entity=0 연속 누적) |
| 봇 3종 fresh 재검 | `run_bot_fresh_recheck.sh BossFightSmoke HpSyncSmoke FreezeSmoke` | **3종 전부 success=True** → 연속 FAIL = 누적 한계, 회귀 아님 확정 (16/16 green) |
| Unity 콘솔 | `ReadConsole error CS` | **0** |
| BuildPlayer | `BuildPipeline.BuildPlayer` (enabled 7씬 → `C:/Dev/Build/Client/03_Client.exe`) | **result=Succeeded errors=0 warnings=18 scenes=7** (warnings = 기존 CS8632/CS0618, P02 무관) |
| DLL mtime 신선도 | `Assembly-CSharp.dll` 20:57:05 vs 소스 최신 20:04:23 | **신선** (P02 클라 코드 포함 — exe 6/10은 엔진 런처라 정상) |
| Shared.dll 신선도 | 98_Shared 손작성 .cs 변경 0 (obj/ 자동생성 제외) | 13:49 정합 (§4 무변경) |
| wire 무변경 | `ProtocolVersion.Current` + `git status --porcelain` | **v12 유지** / 워킹트리 clean |

## 결정 흐름

- **회귀 순서 = 빌드 전 green 게이트**: 빌드는 "에디터 컴파일 0 전제"라 stale DLL로 성공처럼 보일 위험 → 회귀(특히 콘솔 0err)를 빌드보다 먼저 통과시킨 뒤 빌드.
- **봇 연속 FAIL 3종 = 회귀 아님 판정**: BossFight·HpSync는 entity=0(서버에 앞 시나리오 보스/몬스터 상태 누적), Freeze는 entity 붙었으나 freeze 실패 → 모두 *연속 실행 누적 한계*. fresh 단독 서버 재검에서 3종 전부 PASS → "fresh 단독 PASS가 회귀 판정 기준"(M4.10/M4.11 전례) 적용.
- **빌드 신선도 지표 = Managed DLL mtime**(not exe): Unity BuildPlayer는 증분 — exe(엔진 런처 바이너리)는 코드 변경 없으면 재생성 안 함(6/10 그대로). 게임 코드는 `Assembly-CSharp.dll`에 컴파일되므로 그 mtime이 신선도의 진짜 지표.
- **박제 입자 = 마일스톤 -DONE 1장 흡수**(영호 결정): M4.9 7 Phase가 이미 다 구현된 상태라 개별 7장 사후 회수는 형식 채우기 부담만 큼 → `_milestone-DONE.md` 1장에 7 Phase 결과 + Teleport 실상태(코드✅/VFX 미배치) 정직 흡수. M4.9 회수 박제 등급 = 복잡 경량 처리(원래 대규모지만 사후 요약 성격 — 본문 정직 명시).
- **P02 코드 변경 0**: 회귀·빌드·박제만 — irreversible 깃발은 끝의 PR/머지에만 걸리고 그건 별도 영호 GO. 회귀·재빌드 자체는 가역적이라 자율 진행.

## 학습 일지 후보 키워드

- 회귀(regression) = "기능 퇴행" — 새 변경이 기존 green을 깨뜨렸나. 통계 회귀분석과 별개 개념
- baseline 비감소 판정 = "green"만으론 신규 케이스 누락 못 잡음, *직전 숫자 대비*로 판정
- 봇 연속 FAIL ≠ 회귀: 서버 상태 누적(entity=0) → fresh 단독 재검 PASS가 판정 기준
- 빌드 신선도 = Managed DLL mtime (exe 아님 — 증분 빌드에서 엔진 런처는 재생성 안 함)
- WSL2 ADR-029 rsync→build→test 한 묶음 (sync 누락 = stale 코드 실행 1순위 함정)
- EditMode 회귀 = TestRunnerApi.Execute(EditMode 필터) + ICallbacks RunFinished 마커 콘솔 폴링

---

> P02 완료 → M4.12 마일스톤 마감(`_milestone-DONE.md`) + M4.9 회수 박제(`M4.9-skill-completion/_milestone-DONE.md`).
