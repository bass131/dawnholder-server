---
owner: youngho
milestone: M6
phase: 04
title: NPC 대화 패널 재구축
status: pending
grade: 복잡
risk: unity-asset
estimated: 3~5h
domain: client
summary: NpcDialogPanel을 BuildRuntime 패턴으로 재구축 — Menu_Button.png 배경 + NPC 초상화 + 대사 텍스트, CombatBootstrap 통합
---

# Phase 04: NPC 대화 패널 재구축

> **상태**: pending
> **마일스톤**: M6
> **등급**: 복잡 (client 주도 + prefab/scene)
> **담당**: client + 영호 (초상화/배경 에셋)

---

## 🎯 목표

E키로 NPC와 상호작용하면 대화 패널이 **확실히 뜬다** (현재는 Scene 미배치 시 silent skip → 2차 플레이테스트에서 출력 없음).
패널은 **Menu_Button.png 배경 + NPC 초상화 + 대사 텍스트**로 구성되고, 런타임 생성(BuildRuntime)되어
Scene 수동 배치 의존을 없앤다.

---

## ⏪ 사전 조건

- [ ] Phase 03(상단 HUD)와 BuildRuntime 패턴 공유 — 먼저/병렬 무관하나 패턴 일관성 참고
- [ ] 영호 미커밋 NPC 초상화 reorg(Portrait/ 폴더)가 M6 브랜치에 이월돼 있음 (충족 — working tree)
- [ ] **분담 확인**: 초상화/배경 에셋 선정·import는 영호, 코드 wiring은 AI

---

## 📝 작업 내용

대상 (실측):
- `03_Client/Assets/Scripts/Gameplay/NpcDialogPanel.cs` (89줄) — static _instance, Scene 배치 의존, legacy Text, 배경/초상화 없음
- `03_Client/Assets/Scripts/Gameplay/NpcInteractable.cs` (81줄) — E키 InputAction → Show(_dialogText)
- `03_Client/Assets/Resources/UI/Menu_Button.png` (존재 확인) — 배경 소재
- `03_Client/Assets/Art/Characters/NPC/<NPC>/Portrait/` — 초상화 소재 (BlackSmith/Glocery, 영호 reorg)

- [ ] NpcDialogPanel을 BuildRuntime 패턴으로 재작성 (PartyMemberHud 참조): Canvas + Panel(Menu_Button.png 9-slice 배경) + 초상화 Image + 대사 TMP_Text
- [ ] CombatBootstrap(또는 적절 부트스트랩)에서 `NpcDialogPanel.BuildRuntime(...)` 호출 → Scene 수동 배치 제거
- [ ] legacy `Text` → TMP_Text 전환 (한글 폰트 정합 — Pretendard 폴백 기존 적용분 재사용)
- [ ] NpcInteractable에 초상화 sprite 필드 추가 + `Show(text, portraitSprite)` 오버로드
- [ ] 각 NPC(BlackSmith/Glocery)에 초상화 sprite 배정 + 대사 연결
- [ ] _instance null 경고 silent skip 제거 (런타임 생성이므로 항상 존재) — 방어 코드는 유지하되 원인 제거
- [ ] **구 `NpcDialogPanel.prefab` + `.meta` 삭제** (Opus plan-auditor 지적): 현재 어느 Scene에도 박혀있지 않은 고아 자산. BuildRuntime 패턴 도입 후 deprecated → 유지 시 향후 "코드빌드 + 씬자산 중복 박힘" 위험(CombatBootstrap.cs:48-50 ZoneVisualizer 사고 학습 정합). git history는 보존.

---

## ✅ 완료 조건

- [ ] BlackSmith/Glocery에 다가가 E키 → 배경+초상화+대사 패널이 뜬다 (영호 육안)
- [ ] 다시 E 또는 ESC로 닫힌다
- [ ] Scene에 NpcDialogPanel prefab을 수동 배치하지 않아도 동작 (BuildRuntime)
- [ ] 한글 대사가 ㅁㅁ 없이 정상 렌더 (폰트 폴백)
- [ ] **구 NpcDialogPanel.prefab + .meta 삭제 확인** (git status로 D 표시)
- [ ] WSL2 회귀 게이트 green (서버 무관)

---

## 🧪 테스트

**수동 (영호 육안)**:
- 두 NPC 각각 E키 상호작용 → 패널/초상화/대사 확인, 닫기 확인
- Scene을 새로 열어도(또는 빌드에서) 배치 누락 없이 뜨는지
- **race 시나리오 3건** (Opus 지적):
  - ① OnTriggerExit 중(`_isPlayerNear=false`로 Hide 트리거)에 즉시 E를 재입력하면 어떻게 되는가
  - ② 두 NPC trigger zone이 겹친 위치에서 E를 누르면 어느 대사가 뜨는가
  - ③ 전투 중(피격 hitstun 상태) E 입력이 정상 처리되는가

---

## 📚 학습 포인트

- **BuildRuntime 패턴**이 왜 Scene 배치보다 안전한가 — static _instance가 Awake 의존이면 prefab이 씬에 없을 때 NRE/무반응. 코드 생성은 그 의존을 없앤다.
- 9-slice Image로 버튼/패널 배경을 늘려 쓰는 법.
- 클라 단독 대사(서버 동기 없음)가 헌법 #1에 어긋나지 않는 이유 — 표현 콘텐츠.

---

## ⚠️ 함정 / 주의사항

- 기존 NpcDialogPanel.prefab을 Scene에서 참조 중이면 제거/대체 시 끊긴 참조 확인.
- 초상화 sprite import 설정(Sprite mode, pivot) 누락 시 깨져 보임 — 영호 import 확인.
- TMP 폰트 에셋 미할당 경고(M5에서 관측된 퀘스트 HUD 폰트 경고와 동류) 재발 주의 — 폰트 에셋 명시 할당.

---

## ➡️ 다음 Phase

- Phase 05 — 통합 플레이테스트 + 클로즈아웃
