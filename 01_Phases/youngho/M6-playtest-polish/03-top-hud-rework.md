---
owner: youngho
milestone: M6
phase: 03
title: 상단 HUD 재정비 (파티 HUD 이동 + 퀘스트 텍스트/패널 확장)
status: pending
grade: 복잡
risk: unity-asset
estimated: 2~4h
domain: client
summary: 파티 멤버 HUD를 퀘스트 mockup 위치로 이동(mockup 제거) + 퀘스트 이름/목표/카운트 3줄 표시 + 뒷 패널 확장
---

# Phase 03: 상단 HUD 재정비 (파티 HUD 이동 + 퀘스트 텍스트/패널 확장)

> **상태**: pending
> **마일스톤**: M6
> **등급**: 복잡 (client 다중 파일 + UI 위치)
> **담당**: client + 영호 육안

---

## 🎯 목표

상단 HUD를 정리한다:
1. 파티 멤버 HUD를 현재 좌상단에서 **퀘스트 mockup UI 위치로 이동**하고 mockup은 제거.
2. 퀘스트 HUD가 카운트("3/40")만이 아니라 **퀘스트 이름 + 목표 + 카운트 3줄**을 표시하고 뒷 패널을 확장.

---

## ⏪ 사전 조건

- [x] **설계 분기 확정**: 퀘스트 이름/목표 출처 = **클라 로컬 content (영호 승인 2026-06-15)**. 프로토콜 무변경(ProtocolVersion v16 유지).
  - 근거: S_QuestUpdate가 현재 `currentCount`/`targetCount` int 2개만 + 퀘스트 ID 미포함 + 단일 퀘스트(보스 20킬). 표시용 정적 텍스트는 헌법 §1상 서버 권위 대상 아님(진행도 카운트만 서버). NPC 대사가 클라 로컬인 것과 정합.
- [x] **클라 로컬 박을 위치 확정**: `QuestProgressHud.cs` 내부 `const string`으로 박음 (서버 미러 클래스 `QuestState` **무변경**).
  - 사유 (Opus plan-auditor 지적): QuestState.cs:8 주석이 "클라가 임의로 변경하지 않음 (헌법 §1)" — 표시용 텍스트를 같은 클래스에 섞으면 SRP 위반 + 미러 컨벤션 회색지대. 단일 퀘스트라 별도 모듈(QuestContent.cs) 신설은 과투자. 미래 다중 퀘스트는 맵 에디터 마일스톤 몫.

---

## 📝 작업 내용

대상 파일 (실측):
- `03_Client/Assets/Scripts/UI/PartyMemberHud.cs` (194줄) — 좌상단 anchoredPosition(20,-20)
- `03_Client/Assets/Scripts/UI/QuestProgressHud.cs` (153줄) — 상단중앙, 패널 220×50, 카운트만
- `03_Client/Assets/Scripts/State/QuestState.cs` (50줄) — **무변경** (서버 미러 컨벤션 유지)

- [ ] 퀘스트 "mockup/placeholder" UI 위치를 Scene에서 특정 (제거 대상 좌표 확보)
- [ ] PartyMemberHud RectTransform을 그 위치로 이동 + 필요시 슬롯 레이아웃 조정
- [ ] mockup UI 제거 (Scene 또는 코드)
- [ ] **QuestProgressHud.cs 내부에 `const string QuestName` + `const string QuestObjective` 박음** (QuestState는 손대지 않음)
- [ ] QuestProgressHud Refresh()를 3줄(이름/목표/카운트)로 확장 + 패널 sizeDelta 확대(예 360×120)
- [ ] 파티 HUD와 퀘스트 HUD가 같은 영역에서 겹치지 않게 좌표/크기 정리
- [ ] **Phase 02 SortingLayer 상수와 정합**: Phase 02가 SortingLayer 이름을 상수로 박은 위치(예: `RenderLayers.cs`)가 있으면 그걸 참조. UI Canvas sortingOrder(900~)는 ScreenSpace-Overlay 별개 체계라 영향 없음 — 다만 *명세 의존* 확인.

---

## ✅ 완료 조건

- [ ] 파티 멤버 HUD가 퀘스트 mockup 자리로 이동, mockup 흔적 없음 (영호 육안)
- [ ] 퀘스트 HUD가 이름/목표/카운트 3줄로 표시되고 패널이 텍스트를 가리지 않게 확장됨
- [ ] 파티/퀘스트 HUD 겹침 없음
- [ ] **QuestState.cs diff = 0 (서버 미러 무변경 확인)** + ProtocolVersion v16 유지
- [ ] **솔로 시나리오**: 파티 비구성 상태에서 파티 HUD 숨김 + 퀘스트 3줄 정상 표시 (영호 육안)
- [ ] WSL2 회귀 게이트 green

---

## 🧪 테스트

**수동 (영호 육안)**:
- 파티 구성 상태에서 멤버 HUD 위치 + 퀘스트 3줄 표시 동시 확인
- **솔로 상태**에서 파티 HUD 숨김 + 퀘스트 3줄 정상 표시
- 다양한 카운트(0/20, 10/20, 20/20)에서 텍스트 잘림 없는지

---

## 📚 학습 포인트

- **표시용 콘텐츠 vs 권위 상태**: 퀘스트 이름은 표현, 진행도 카운트는 서버 권위 — 이 경계가 프로토콜을 건드릴지 말지를 가른다.
- RectTransform anchor/pivot/anchoredPosition로 화면 상대 배치하는 법.
- BuildRuntime로 생성되는 UI는 코드에서 좌표가 결정됨 — Scene이 아니라 .cs를 고쳐야 함.

---

## ⚠️ 함정 / 주의사항

- 파티 HUD와 퀘스트 HUD가 둘 다 상단 영역 BuildRuntime → 좌표 충돌 쉬움. 한쪽 옮길 때 다른 쪽 기준으로 확인.
- 퀘스트 이름을 서버로 옮기고 싶은 충동 주의 — 다중 데이터 기반 퀘스트는 미래 맵 에디터/데이터 직렬화 마일스톤 몫. 지금 프로토콜 bump는 과투자.
- mockup 제거 시 그 UI를 참조하던 코드/이벤트가 없는지 확인(끊긴 참조 → NRE).

---

## ➡️ 다음 Phase

- Phase 04 — NPC 대화 패널 재구축 (독립, 병렬 가능)
