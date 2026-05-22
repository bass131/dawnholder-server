---
owner: youngho
milestone: M3.8
phase: 04
title: NPC 대화 (클라 단독 hardcoded)
status: pending
grade: 보통
risk: unity-asset
estimated: 1~2h
domain: client
summary: 마을 NPC GameObject placeholder + interactable 컴포넌트 (E 키 트리거) + 단순 텍스트 출력 (클라 단독 hardcoded, 서버 패킷 X)
---

# Phase 04: NPC 대화 (클라 단독 hardcoded)

> **상태**: pending
> **마일스톤**: M3.8 Capstone-1 Demo Infrastructure
> **등급**: 보통 (위험 깃발 `unity-asset` — NPC GameObject + UI 박음)
> **담당**: client SubAgent (Unity GameObject + UI Script)

---

## 🎯 목표

마을 Scene에 NPC GameObject placeholder 박음 + 플레이어가 NPC 옆에서 E 키 누르면 단순 텍스트 출력. *클라 단독 hardcoded* = 서버 패킷 X = MVP 제외 항목 "퀘스트/NPC"와의 PRD 정합 (캡스톤 1 시연용 단순 흡수, 본 마감 후 정식화).

본 Phase 끝나면 = 캐릭터 선택 후 마을 진입 → NPC 시각화 → E 키 누르면 dialog UI 표시 ("보스가 마을을 위협하고 있어요. 도와주세요!") → E 또는 ESC 누르면 dialog 닫힘 + 사냥터 방향 안내 텍스트.

---

## ⏪ 사전 조건

- [ ] Phase 03 (캐릭터 선택) 박혀있음 — 마을 진입 흐름 = 캐릭터 선택 후 자연
- [ ] M3 Phase 08a/08b 박힌 `Gameplay.unity` Scene 박혀있음 (마을 Scene = M3.8에선 *재활용*, M4.2에서 진짜 4맵 분리 시 별 Scene)
- [ ] 본인 머신 Unity batchmode compile 정상

---

## 📝 작업 내용

- [ ] `03_Client/Assets/Prefabs/NpcVillager.prefab` 신설:
  - SpriteRenderer (placeholder sprite, 정유현 영역과 정합 — 임시는 단색 사각형 OK)
  - BoxCollider2D (isTrigger=true, 인터랙션 범위 검출)
  - `Scripts/Gameplay/NpcInteractable.cs` 컴포넌트 박음
- [ ] `03_Client/Assets/Scripts/Gameplay/NpcInteractable.cs` 신설:
  - `OnTriggerEnter2D(Collider2D other)` → `if (other.CompareTag("Player")) _isPlayerNear = true`
  - `OnTriggerExit2D` → `_isPlayerNear = false`
  - `Update()` → `if (_isPlayerNear && Input.GetKeyDown(KeyCode.E)) ShowDialog()`
  - `ShowDialog()` → `NpcDialogPanel.Show(_dialogText)`
  - `_dialogText` SerializeField (Inspector에서 박음, 기본값 = "보스가 마을을 위협하고 있어요. 도와주세요!")
- [ ] `03_Client/Assets/Scripts/Gameplay/NpcDialogPanel.cs` 신설 (plan-auditor 결함 봉합 = **이름 결정 박음**, 정유현 영역 침범 차단):
  - `Scripts/UI/`는 정유현 영역이라 침범 X — `Scripts/Gameplay/`에 박음
  - Static `Show(string text)` / `Hide()` 메서드
  - Canvas + 텍스트 박스 (Scene 안 GameObject reference)
  - ESC 키 또는 E 키 재입력 → `Hide()`
  - 중복 표시 가드 = `if (_isShown) return` (함정 절 정합)
- [ ] `03_Client/Assets/Prefabs/NpcDialogPanel.prefab` 신설:
  - Canvas (Sort Order 박음, Gameplay UI 위)
  - Background Image (반투명 검정)
  - Text 컴포넌트 ("보스가 마을을 위협하고 있어요...")
  - "닫기" 버튼 또는 ESC/E 입력 안내
- [ ] `Gameplay.unity` Scene에 `NpcVillager.prefab` 1개 배치 (마을 영역)
- [ ] `Gameplay.unity` Scene에 `NpcDialogPanel.prefab` 1개 배치 (비활성화 상태)
- [ ] Unity batchmode compile green (`unity-bridge` 호출)

---

## ✅ 완료 조건

- [ ] `NpcVillager.prefab` 박힘 + `NpcInteractable.cs` 박힘
- [ ] `NpcDialogPanel.prefab` 박힘 + `NpcDialogPanel.cs` 박힘 (`Scripts/Gameplay/`)
- [ ] `Gameplay.unity` Scene에 NPC + Dialog Panel 배치 박힘
- [ ] Unity batchmode compile green
- [ ] dotnet test green (회귀 0, 본 Phase는 클라 단독이라 서버 영향 X)
- [ ] commit 박힘 (단독 PR 분리 X)

---

## 🧪 테스트

**자동**: 본 Phase = 클라 단독 + 서버 패킷 X → 자동 테스트 영향 X. 회귀 확인만 (`dotnet test` green 유지).

**수동**:
- Unity Editor Play → MainMenu → 캐릭터 선택 → 마을 진입 → NPC 시각화 확인
- 플레이어 NPC 옆 이동 → E 키 → dialog 표시 확인
- ESC 또는 E 재입력 → dialog 닫힘 확인
- *플레이어 NPC 멀리* → E 키 무반응 확인 (`_isPlayerNear == false` 정합)

---

## 📚 학습 포인트

- **클라 단독 vs 서버 권위 분기 판단** — NPC 대화는 *상태 변경 X* (단순 텍스트 출력) → 클라 단독 OK. 만약 *퀘스트 진행도* / *보상 지급* 같은 *상태 변경*이면 서버 권위 의무. 헌법 #1 정합 + 학부생 함정 회피.
- **OnTriggerEnter2D vs OnCollisionEnter2D** — Trigger = 통과 가능 (감지만), Collision = 물리적 반발. NPC 인터랙션 = Trigger 정합. 학부생 함정 (혼동).
- **Static Show 패턴 vs Singleton** — `NpcDialogUI.Show(text)` static = 단순, 한 Scene에 인스턴스 1개 가정. Singleton은 *DontDestroyOnLoad + 인스턴스 보장*. 본 Phase는 단순 static OK (Scene 안만, 전환 시 새로 박힘).
- **Input.GetKeyDown vs GetKey** — `GetKeyDown` = *프레임에 한 번 true* (눌리는 순간), `GetKey` = *프레임마다 true* (계속 눌림). 대화 트리거 = `GetKeyDown` 정합 (한 번만 발동). 학부생 함정 1순위.
- **MVP 제외 항목 우회 패턴** — 퀘스트/NPC = MVP 제외지만 시연용 단순 흡수. *분기 선택지 X + 보상 X + 진행도 X* = 단순 텍스트 출력만 = MVP 정합 (PRD 정정 박힘 Phase 01 정합).

---

## ⚠️ 함정 / 주의사항

- **정유현 영역 침범 차단** — `Scripts/UI/`는 정유현 영역. 본 Phase는 `Scripts/Gameplay/NpcDialogPanel.cs`로 이름 박는 게 정합 (또는 정유현과 미리 조율). UI prefab도 `Prefabs/UI/` 침범 X — `Prefabs/Gameplay/`에 박음.
- **NpcVillager prefab variant 사고** — M3 Phase 08 BackGround prefab 사고 학습 정합. NPC prefab 박을 때 *백업 의무* + `unity-bridge` SubAgent 통해 박는 게 정합.
- **dialog 중첩 실행** — `Show()` 호출 시 이미 열린 dialog 있으면 *중복 표시*. `if (_isShown) return` 가드 박음.
- **ESC 키 충돌** — ESC는 Unity Editor에서 *Play 모드 종료* 기본 키. Build 모드에선 정상 작동. 본 Phase는 ESC 사용 OK, 단 *Editor 테스트 시 Play 모드 종료* 인지.
- **interactable 범위 vs 시각화 분리** — BoxCollider2D 크기 = *인터랙션 범위*. NPC 옆에 가서 E 키 누를 수 있는 거리. 학부생 함정 = 시각화 sprite 크기와 BoxCollider 크기 일치 가정 → 너무 작아서 인터랙션 어려움.
- **headless smoke 영향 X** — 본 Phase 클라 단독 + 패킷 X → 서버 smoke 영향 X.

---

## ➡️ 다음 Phase

- Phase 05 — Hamachi 셋업 검증 + M3.8 마감 의례

---

## 📋 박제

본 Phase = 보통 등급 → work-pin 갱신 + commit message만 박음. -DONE.md 박지 않음.

work-pin "현재 작업" → "Phase 04 ✅ 마감, Phase 05 미진입" 갱신.

---

## 작업 로그

- 2026-05-22: Phase 정의 박힘 (M3.8 plan 박는 시점)
