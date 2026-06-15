---
owner: youngho
milestone: M6
phase: 02
title: 렌더 소팅 레이어 정립
status: pending
grade: 복잡
risk: unity-asset
estimated: 2~4h
domain: client
summary: SortingLayer를 BG→마을지형→세부지형→NPC→Player→UI 순으로 정의하고 prefab/scene/코드에 일관 배정
---

# Phase 02: 렌더 소팅 레이어 정립

> **상태**: pending
> **마일스톤**: M6
> **등급**: 복잡 (unity-asset + 여러 prefab/scene)
> **담당**: client + 영호 Scene 배치/육안

---

## 🎯 목표

2D 객체들이 항상 의도한 순서로 겹쳐 그려진다:
**BG → 마을 지형 → 세부 지형 → NPC → Player → UI**.
현재는 대부분 "Default" SortingLayer + Order in Layer만으로 제어돼 같은 y에서 겹침이 불안정.

---

## ⏪ 사전 조건

- [ ] **분담 확인**: TagManager.asset SortingLayers 정의 + prefab 배정을 AI(MCP) 1차 vs 영호 직접 — Phase 시작 시 질문
- [ ] 현재 SortingLayer/Order 사용처 실측 (아래 작업 내용 1번)

---

## 📝 작업 내용

- [ ] **실측**: `ProjectSettings/TagManager.asset`의 현재 SortingLayers 정의 + 코드/prefab의 sortingLayerName/sortingOrder 사용처 전수
  - 알려진 사용처: UI HUD들(PartyMemberHud 900 / QuestProgressHud 910 / StageClearUI 1000 / ToastUI 1100 / PartyInvitePopup 1200), Enemy prefab SortingOrder 3~4, NPC prefab SortingOrder 5, Tilemap(Default)
- [ ] **SortingLayer 정의** (TagManager.asset): Background → TownTerrain → DetailTerrain → NPC → Player → UI (이름·순서 영호 확인)
- [ ] **배경/지형**: Tilemap/배경 SpriteRenderer를 Background·TownTerrain·DetailTerrain에 배정
- [ ] **NPC**: NPC prefab SpriteRenderer → NPC 레이어
- [ ] **Player**: 로컬/원격 플레이어 SpriteRenderer → Player 레이어
- [ ] **적**: Enemy(보스/일반) SpriteRenderer → 적절 레이어(보통 Player와 동일 또는 DetailTerrain 위) + HP바 Order 정리
- [ ] **UI**: ScreenSpace-Overlay Canvas는 sortingOrder 체계 유지(월드와 별개) — 충돌 없는지 확인
- [ ] **코드 상수화**: SortingLayer 이름 문자열을 **`03_Client/Assets/Scripts/Rendering/RenderLayers.cs`(신규) 같은 단일 위치에 상수로** 박음 (Phase 03·04가 BuildRuntime UI 생성 시 참조). 오타 방지 + Phase 간 명세 의존 명시.
- [ ] **Enemy 배정 방향 확정 (영호 의논)**: Enemy(보스/일반몹)를 NPC와 동레이어로 둘지 별도(예: "Enemies") 레이어로 둘지 — Phase 진입 시 1줄 결정 후 박음.

---

## ✅ 완료 조건

- [ ] 마을에서 BG→지형→NPC→Player가 항상 올바른 순서로 겹침 (영호 육안)
- [ ] 플레이어가 NPC/지형 뒤로 잘못 숨거나 위로 잘못 뜨지 않음
- [ ] **SortingLayer 이름 상수 산출물 명시**: `RenderLayers.cs`(또는 동등 위치)에 모든 레이어 이름이 상수로 박힘 → Phase 03·04가 참조 가능
- [ ] Enemy 레이어 배정 방향이 확정·박힘 (NPC동레이어 또는 별도 — 영호 결정 박제)
- [ ] TagManager.asset SortingLayers diff + prefab/scene diff로 변경 추적 가능
- [ ] WSL2 회귀 게이트 green (서버 무관)

---

## 🧪 테스트

**수동 (영호 육안)**:
- 마을 곳곳에서 플레이어를 이동시키며 지형/NPC와의 앞뒤 겹침 확인
- UI HUD가 모든 월드 객체 위에 그려지는지

---

## 📚 학습 포인트

- **SortingLayer vs Order in Layer**: SortingLayer가 1차 정렬축(거시), Order in Layer가 같은 레이어 내 미세 정렬. 둘 다 있어야 안정적.
- ScreenSpace-Overlay Canvas는 항상 월드 위 — 월드 SortingLayer와 Canvas sortingOrder는 별개 체계.
- y-기반 동적 정렬(캐릭터가 위로 가면 뒤로)은 같은 SortingLayer 안에서 Order를 y로 갱신하는 패턴(이번 범위 밖일 수 있음 — 영호 확인).

---

## ⚠️ 함정 / 주의사항

- SortingLayer **이름을 바꾸면** 이미 배정된 모든 컴포넌트가 "Default"로 리셋될 수 있음 — 이름 먼저 확정 후 배정.
- TagManager.asset은 ProjectSettings → diff가 커 보일 수 있으나 SortingLayers 블록만 변경되도록 최소화.
- prefab 변경은 unity-asset 위험 — 백업/커밋 단위 작게(Phase 08 BackGround prefab 사고 학습).

---

## ➡️ 다음 Phase

- Phase 03 — 상단 HUD 재정비 (독립, 병렬 가능)
