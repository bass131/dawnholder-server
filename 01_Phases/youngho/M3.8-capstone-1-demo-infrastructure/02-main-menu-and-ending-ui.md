---
owner: youngho
milestone: M3.8
phase: 02
title: 메인화면 흐름 재정렬 + 엔딩 화면 신설 + Build Settings cleanup
status: in-progress
grade: 보통
risk: unity-asset
estimated: 1~2h
domain: client
summary: 옛 정유현 박은 MainMenu.unity + SceneTransition 활용 + MainMenuController OnStartClicked 흐름 변경 (Gameplay → CharacterSelect placeholder) + Ending.unity Scene 신설 + Build Settings cleanup (GameplayTest stale 제거 + SampleScene 리스트만 제거 파일 유지)
---

# Phase 02: 메인화면 흐름 재정렬 + 엔딩 화면 신설 + Build Settings cleanup

> **상태**: in-progress (2026-05-23 재정의)
> **마일스톤**: M3.8 Capstone-1 Demo Infrastructure
> **등급**: 보통 (위험 깃발 `unity-asset` — Scene 신설 + Build Settings 변경)
> **담당**: 메인 직접 + unity-bridge (Scene asset 차원) + Unity MCP

---

## 🚨 옛 plan false-promise 봉합 (재정의 사유)

옛 Phase 02 정의 (2026-05-22 박힘) = *실측 점검 안 박은 false-promise 변종* — 다음 결함:

| 옛 plan 박은 약속 | 실측 (2026-05-23) | 봉합 |
|---|---|---|
| `MainMenu.unity` 신설 | 이미 박힘 (정유현 `feat(yuhyeon): Phase 01` 학기 초) | *활용*으로 변경 |
| `Scripts/Scene/MainMenuController.cs` 신설 | 이미 박힘 — `Scripts/UI/MainMenuController.cs` (정유현 영역) | *옛 거 활용 + 흐름만 정정* |
| Build Settings에 Ending 추가만 | `GameplayTest.unity` stale 박혀있음 (M3 cleanup 누락) | cleanup 흡수 봉합 |

본 결함 = **9번째 false-promise 변종** (옛 plan 박을 때 *실측 점검 안 박은 결함*). ADR-024 cadence 정합 *발견 즉시 봉합* 박음. 학습 후보 ★★★ = "plan 박을 때 실측 점검 의무" 패턴.

**영역 결정** (사용자 결정 박힘, 2026-05-23):
- CODEOWNERS 룰 *유지* (폐기 X)
- 본 시점 = *예외적 본인 박음* (정유현 학습 비용 ↓ + 마감 우선)
- M3.8 마감 PR 시 `--admin` bypass + 사후 디스코드 알림 정합

---

## 🎯 목표

캡스톤 1 발표 시연의 *시작점* / *종료점* 박음 + 옛 학기 초 자산 정리. 본 Phase 끝나면:

- **MainMenu** = 옛 정유현 박은 Scene + Controller 활용. *시작 버튼 흐름*만 정정 (Gameplay → CharacterSelect placeholder)
- **Ending** = 별 Scene 신설 (옵션 A 의무, ADR-021 정합). 보스 처치 후 시연 종료점
- **Build Settings** = stale 정리 (GameplayTest 제거 + SampleScene 리스트만 제거 + Ending 추가)
- **SceneTransition Singleton** = 유지 + 활용 (fade 효과, M3.8 모든 Scene 전환 자연 흐름)

---

## ⏪ 사전 조건

- [x] Phase 01 (PRD 갱신) ✅ 마감 (commit `15fd7c7`)
- [x] Unity 6.4 LTS Editor 부팅 가능 + Unity MCP 연결 ✅ 양호 (2026-05-23 실측)
- [x] M3 Phase 08a/08b 산출물 (`Gameplay.unity` + `PlayerBase.prefab` variant 체인) 박혀있음
- [x] 옛 정유현 박은 `MainMenu.unity` + `MainMenuController.cs` + `SceneTransition.cs` 실측 확인

---

## 📝 작업 내용

### A. Build Settings cleanup (가장 먼저 박음)

- [ ] `EditorBuildSettings.asset` 리스트 정정:
  - **제거**: `GameplayTest.unity` (파일 미존재, M3 cleanup 누락 stale 봉합)
  - **제거**: `SampleScene.unity` (Unity 템플릿, 사용 X — 단 *파일 자체는 유지*, 템플릿 후보 보존)
  - **추가**: `Ending.unity` (본 Phase 신설 후)

### B. Ending Scene + Controller 신설

- [ ] `03_Client/Assets/Scenes/Ending.unity` 신설 (옵션 A 의무, ADR-021 정합)
  - Canvas + EventSystem 박음 (Unity UI 기본)
  - 텍스트 ("Stage Clear" 또는 "엔딩") + "메인으로" 버튼
  - 배경 = placeholder 단색
- [ ] `03_Client/Assets/Scripts/Scene/EndingController.cs` 신설 (본인 영역, 정유현 침범 X)
  - 메인 버튼 OnClick → `SceneTransition.Instance.LoadScene("MainMenu")` (정유현 박은 fade 패턴 활용)
  - 헌법 #1 정합 = 본 Scene은 단순 UI, 네트워크 X
  - 보스 처치 트리거 (`S_StageClear` 패킷 수신 → Scene 전환)는 Phase 03/04 박힌 후 GameplayController에서 dispatch — *본 Phase 박지 X*

### C. MainMenu 흐름 정정 (정유현 영역 침범, --admin bypass 시점 정합)

- [ ] `03_Client/Assets/Scripts/UI/MainMenuController.cs` `OnStartClicked` 정정:
  - 옛 = `SceneTransition.Instance.LoadScene(gameplaySceneName)` (캐릭터 선택 건너뜀)
  - 새 = `SceneTransition.Instance.LoadScene("CharacterSelect")` 박음 (Phase 03 박힌 후 자동 작동)
  - 본 시점 미박힘 시 fade 후 *Scene 미존재 exception* — **옵션 3 (Debug.Log placeholder) 박음**: `Debug.Log("[MainMenu] Start → CharacterSelect (Phase 03 박힌 후 활성화)")` 박고 LoadScene 호출은 *주석 처리*
  - 옛 `gameplaySceneName` SerializeField는 *제거* (Inspector 잔재 청소) — *MainMenu.unity Inspector 값 동반 정리 의무* (Scene asset 변경 필요)
- [ ] **OnQuitClicked = 그대로 유지** (정유현 박은 거 정합)
- [ ] `MainMenu.unity` Inspector 정리 (옛 `Gameplay` 박힌 `gameplaySceneName` 값 제거 또는 빈 값)

### D. SceneTransition Singleton 검증

- [ ] `03_Client/Assets/Scripts/UI/SceneTransition.cs` *변경 X* (옛 정유현 Phase 05 박은 거 그대로 활용)
- [ ] 옛 작동 검증 — `LoadScene("CharacterSelect")` 호출 시 fade out → Scene 미존재면 fade 처리 어떻게 박힌지 확인 (예외 흐름)

---

## ✅ 완료 조건

- [ ] `Ending.unity` 별 Scene 박힘 + 메인 버튼 작동 (Editor Play → SceneTransition.LoadScene("MainMenu") 호출)
- [ ] `EndingController.cs` `Scripts/Scene/` 신설 (정유현 영역 침범 X)
- [ ] `MainMenuController.cs` `OnStartClicked` 정정 (Debug.Log placeholder + Phase 03 박힌 후 활성화 주석)
- [ ] Build Settings = `MainMenu` + `Gameplay` + `UI` + `Ending` 4행 박힘 (`GameplayTest` + `SampleScene` 제거)
- [ ] `SampleScene.unity` 파일 *유지* (Build Settings 리스트만 제거, 템플릿 후보 보존)
- [ ] Unity batchmode compile green (`unity-bridge` 또는 Unity MCP `RunCommand` 검증)
- [ ] dotnet test green (회귀 0, 본 Phase는 클라 단독이라 서버 영향 X)
- [ ] commit 박힘 (단독 PR 분리 X — M3.8 전체 마감 시 한 PR `--admin` bypass + 사후 디스코드 알림)

---

## 🧪 테스트

**자동**:
- Unity batchmode compile (헌법 #4 + ADR-020 정합) — Unity MCP `RunCommand` 활용 검증

**수동**:
- Unity Editor → MainMenu Scene Play → 시작 버튼 클릭 → 콘솔 로그 박힘 + fade 작동 X (placeholder 시점)
- Unity Editor → Ending Scene Play → 메인 버튼 클릭 → MainMenu 전환 (fade 작동 확인)

---

## 📚 학습 포인트

- **plan 박을 때 실측 점검 의무 (false-promise 9번째 변종)** — 옛 plan = *옛 박힘 점검 안 박은 결함*. 패턴 = "plan 박을 때 = grep + ls + git log 1회 박음 의무". 학습 ★★★ 후보.
- **옛 자산 활용 vs Rebuild 판단** — *전체 Rebuild 가닥*이면 옛 작동 보존 가치 ↓. 그러나 *시각 자산(Scene UI 위치)* + *재사용 가능 컴포넌트(SceneTransition fade)* = 보존 정합. *흐름 코드(OnStartClicked)* = 정정 필요. 분기 판단 패턴.
- **CODEOWNERS 룰 예외적 운영** — 룰 폐기 X + 본 시점 *예외적 본인 박음* + 사후 디스코드 알림 패턴. *팀장이 팀원 영역 박는* 한국 게임 회사 백엔드 흔한 패턴.
- **Build Settings stale 결함** — Scene 파일 제거 시 Build Settings 정합 의무. Unity *file → settings 자동 동기 X*. 학습 ★★ 후보.

---

## ⚠️ 함정 / 주의사항

- **MainMenu.unity Inspector 값 변경** — `gameplaySceneName` SerializeField 변경/제거 시 *Scene asset 자체*가 변경됨. unity-bridge 또는 Unity MCP 활용 + 백업 의무 (M3 Phase 08 BackGround 학습 정합).
- **SceneTransition fade 도중 Scene 미존재** — `LoadScene("CharacterSelect")` 호출 시 fade out 후 Scene 미존재 → exception 또는 fade 멈춤. **본 Phase에선 LoadScene 호출 자체 주석 처리** = fade 트리거 X.
- **Build Settings 인덱스 순서 변경** — Scene 제거 시 인덱스 변동. `SceneManager.LoadScene(int index)` 호출하는 코드 있으면 *깨짐*. 본 프로젝트엔 모두 *이름 기반 호출*이라 영향 X (검증 완료).
- **--admin bypass 시점 X** — 본 Phase 단독 PR X. M3.8 전체 마감 PR 시 한 번만 bypass. 본 Phase는 commit만, push는 M3.8 마감 묶음.

---

## ➡️ 다음 Phase

- Phase 03 — 캐릭터 선택 (PDL + 서버 stats + 클라 UI). 박힌 후 `MainMenuController.OnStartClicked`에서 `Debug.Log` 주석 해제 + `LoadScene("CharacterSelect")` 활성화 한 줄 변경.

---

## 📋 박제

본 Phase = 보통 등급 → work-pin 갱신 + commit message만 박음. -DONE.md 박지 않음.

work-pin "현재 작업" → "Phase 02 ✅ 마감, Phase 03 미진입" 갱신.

---

## 작업 로그

- 2026-05-22: Phase 정의 박힘 (M3.8 plan 박는 시점) — *false-promise 9번째 변종 박힘 (실측 점검 안 박음)*
- 2026-05-23: **재정의 박힘** (Phase 02 진입 시점 실측 점검 후 옛 plan 결함 발견 + ADR-024 cadence *발견 즉시 봉합*). 옛 가닥 (Scene 신설 + Scripts/Scene/) → 새 가닥 (옛 정유현 자산 활용 + 흐름 정정 + cleanup 흡수). plan-auditor 봉합 8번째 박음 (옵션 A 즉시 봉합 패턴 정합).
