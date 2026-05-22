---
owner: youngho
milestone: M3.8
phase: 02
title: 메인화면 UI + 엔딩 화면
status: pending
grade: 보통
risk: unity-asset
estimated: 1~2h
domain: client
summary: MainMenu.unity Scene 신설 (시작/종료 버튼) + Ending.unity Scene 신설 (또는 StageClearUI.cs 활용 + 시연용 정밀화). ADR-021 별 Scene 분리 정합
---

# Phase 02: 메인화면 UI + 엔딩 화면

> **상태**: pending
> **마일스톤**: M3.8 Capstone-1 Demo Infrastructure
> **등급**: 보통 (위험 깃발 `unity-asset` — Scene 신설, prefab 사고 X)
> **담당**: client SubAgent (Unity Scene + UI Script)

---

## 🎯 목표

캡스톤 1 발표 시연의 *시작점*과 *종료점*을 박음. 메인화면 = 시연 진입 입구 (시작 버튼 → 캐릭터 선택 Scene), 엔딩 화면 = 보스 처치 후 시연 종료 (M3에서 박힌 `StageClearUI.cs` 시연용 정밀화).

본 Phase 끝나면 = `MainMenu.unity` Scene 실행 시 시작/종료 버튼 표시 + 시작 클릭 → `CharacterSelect.unity` 로드 (Phase 03 박힌 후) + 보스 처치 후 `Ending.unity` 자동 전환 (또는 `StageClearUI` 정밀화).

---

## ⏪ 사전 조건

- [ ] Phase 01 (PRD 갱신) 박혀있음 — *권장*이지만 *블로커 X* (병렬 가능)
- [ ] Unity 6.4 LTS Editor 부팅 가능 + 본인 머신 Unity batchmode compile 정상 (M3.5 셋업 검증 박힘)
- [ ] M3 Phase 08a/08b 산출물 (`Gameplay.unity` + `PlayerBase.prefab` variant 체인) 박혀있음 — 본 Phase에서 *건들지 X* (정유현 영역)

---

## 📝 작업 내용

- [ ] `03_Client/Assets/Scenes/MainMenu.unity` Scene 신설
  - Canvas + EventSystem 박음 (Unity UI 기본)
  - 시작 버튼 / 종료 버튼 / 게임 제목 텍스트 ("Dawnholder" + 부제)
  - 배경 = placeholder 단색 (정유현 영역 침범 X — Scripts/UI/는 정유현, 본 Phase는 Scripts/Scene/)
- [ ] `03_Client/Assets/Scripts/Scene/MainMenuController.cs` 신설
  - 시작 버튼 OnClick → `SceneManager.LoadScene("CharacterSelect")` (Phase 03 박힌 후 실제 작동)
  - 종료 버튼 OnClick → `Application.Quit()`
  - 헌법 #1 정합 = 본 Scene은 *네트워크 X* (단순 UI Scene)
- [ ] `03_Client/Assets/Scenes/Ending.unity` Scene 신설 (plan-auditor 결함 봉합 = **옵션 A 의무**, ADR-021 별 Scene 분리 정합):
  - 별 Scene 박음 (`Ending.unity` + `EndingController.cs`) — Scene 분리 정합
  - 기존 `StageClearUI.cs` (M3 Phase 박힌 `Gameplay.unity` 안 inline UI)는 *그대로 유지* (M3 응급 데모 호환 + Ending Scene은 시연용 정밀화 버전)
  - 본 마감 후 제거 가능 = 별 Scene 격리 정합
- [ ] `03_Client/Assets/Scripts/Scene/EndingController.cs` 신설 (옵션 A 채택 시)
  - "엔딩" 또는 "Stage Clear" 텍스트 + "메인으로" 버튼
  - 메인 버튼 OnClick → `SceneManager.LoadScene("MainMenu")`
  - 보스 처치 트리거 = M3 Phase 06+07 박힌 `S_StageClear` 패킷 수신 → Scene 전환 (Phase 03/04 박힌 후 `RemoteEntityRegistry` 또는 GameplayController에서 전환 dispatch)
- [ ] `03_Client/ProjectSettings/EditorBuildSettings.asset` Build Settings에 새 Scene 2개 추가 (`MainMenu.unity` / `Ending.unity`)
- [ ] Unity batchmode compile 통과 검증 (`unity-bridge` SubAgent 또는 본인 직접)

---

## ✅ 완료 조건

- [ ] `MainMenu.unity` 실행 시 시작/종료 버튼 표시 + 시작 클릭 시 Scene 전환 (CharacterSelect 미박힘 시 placeholder Scene 로드 OK, Phase 03에서 실제 박음)
- [ ] `Ending.unity` (별 Scene 옵션 A 의무) 실행 시 엔딩 텍스트 + 메인 버튼 표시 + 메인 클릭 시 MainMenu 로드
- [ ] Build Settings에 두 Scene 박힘
- [ ] Unity batchmode compile green (`unity-bridge` 또는 메인 검증)
- [ ] dotnet test green (회귀 0, 본 Phase는 클라 단독이라 서버 영향 X)
- [ ] commit 박힘 (단독 PR 분리 X — M3.8 전체 마감 시 한 PR)

---

## 🧪 테스트

**자동**:
- Unity batchmode compile (헌법 #4 + ADR-020 정합) — 본 Phase 박은 후 의무

**수동**:
- Unity Editor 실행 → MainMenu Scene Play → 시작 버튼 클릭 → Scene 전환 확인 (placeholder OK)
- Unity Editor 실행 → Ending Scene Play → 메인 버튼 클릭 → MainMenu 전환 확인

---

## 📚 학습 포인트

- **Scene 분리 vs 하나의 Scene** — Unity 신규 학부생 함정 = 모든 UI를 `Gameplay.unity` 한 Scene에 박음. ADR-021 = *별 Scene + Additive Load* 권장. 메인 + 캐릭터 선택 + 엔딩 = 3 별 Scene = 메모리/네트워크 영향 X (Scene 전환 시 정리 자동).
- **SceneManager.LoadScene 비용** — `LoadScene(name)` = *동기 호출*, 큰 Scene일 때 frame drop 가능. M3.8 Scene 작아서 동기 OK. M4.2 진짜 4맵 분리 시 `LoadSceneAsync` 검토.
- **Canvas Scaler 박음 의무** — UI Scene 박을 때 Canvas Scaler "Scale With Screen Size" 박지 않으면 해상도 변경 시 UI 깨짐. 학부생 자주 빠뜨림.
- **EventSystem 1개 의무** — Scene마다 EventSystem 박혀있어야 버튼 클릭 작동. *복수 Scene Additive Load 시 EventSystem 중복* 사고 자주 발생 — `if (FindObjectOfType<EventSystem>() == null) Instantiate(...)` 패턴.

---

## ⚠️ 함정 / 주의사항

- **정유현 영역 침범 차단** — `03_Client/Assets/Scripts/UI/`는 정유현 영역 (ADR 박힌 시점). 본 Phase는 `Scripts/Scene/` 박음 (Scene 흐름 = 본인 영역). 둘 충돌 X.
- **Scene 신설 사고** — Unity prefab 사고(M3 Phase 08 BackGround) 학습 정합. Scene 신설 시 *git status* 즉시 확인 + Scene `.unity` 파일 정상 박힘 검증.
- **EditorBuildSettings.asset 갱신 누락** — Scene 박았는데 Build Settings에 안 박으면 `SceneManager.LoadScene` 실행 시 *exception*. 학부생 자주 빠뜨림.
- **`StageClearUI.cs` 충돌 결정** — M3 박혀있는 기존 `StageClearUI.cs`를 *별 Scene Ending으로 분리*할지 *그대로 두고 시연용 정밀화*할지 결정. 권장 = 별 Scene (옵션 A) + 옛 `StageClearUI.cs`는 *Gameplay 안 inline UI*로 그대로 유지 (M3 응급 데모 호환 + Ending Scene은 정밀화 버전).
- **headless smoke 영향 X** — 본 Phase는 클라 Scene/UI 차원이라 headless-bot smoke 영향 X. 회귀 0 검증만.

---

## ➡️ 다음 Phase

- Phase 03 — 캐릭터 선택 (PDL + 서버 stats + 클라 UI). Phase 02 박힌 `MainMenu` 시작 버튼이 Phase 03 `CharacterSelect` Scene 로드.

---

## 📋 박제

본 Phase = 보통 등급 → work-pin 갱신 + commit message만 박음. -DONE.md 박지 않음.

work-pin "현재 작업" → "Phase 02 ✅ 마감, Phase 03 미진입" 갱신.

---

## 작업 로그

- 2026-05-22: Phase 정의 박힘 (M3.8 plan 박는 시점)
