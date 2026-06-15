---
owner: youngho
milestone: M5
phase: 20
title: 마을 NPC 배치 + E키 대사
status: pending
grade: 단순
risk: unity-asset
domain: client
estimated: 0.5~1h
---

# Phase 20: 마을 NPC 배치 + E키 대사

> **상태**: pending
> **마일스톤**: M5
> **등급**: 단순 (unity-asset — 씬에 GameObject 배치 + 스프라이트 할당)
> **담당**: youngho (client)

---

## 🎯 목표

마을(Town 씬)에 NPC 2종(상점 누나=Glocery, 대장장이=BlackSmith)을 배치하고 E키로 말을 걸면 대사가 뜨게 한다. 스크립트(`NpcInteractable`/`NpcDialogPanel`)는 이미 완비 — **배치 + 바인딩만** 한다. (확정 결정 1: 대화만, 상점/제작은 별도 마일스톤.)

---

## ⏪ 사전 조건

- [ ] 없음 (독립 Phase). `NpcInteractable`/`NpcDialogPanel` 스크립트 완비 상태 확인만.

---

## 📝 작업 내용

- [ ] Town 씬에 `BlackSmith` GameObject 배치 + `NpcInteractable`(dialogText) + 스프라이트 할당.
- [ ] Town 씬에 `Glocery` GameObject 배치 + `NpcInteractable`(dialogText) + 스프라이트 할당.
- [ ] 대사 텍스트는 클라 단독 hardcoded (상태 변경 없으니 서버 권위 무관 — 헌법 §1).
- [ ] 씬 편집 = **백업 의무** (Phase 08 BackGround prefab 사고 학습 — 편집 전 백업).

---

## ✅ 완료 조건 (정량)

- [ ] 마을 NPC 2종(BlackSmith/Glocery)에 E키로 상호작용 시 각자 대사 표시 (육안).
- [ ] Unity 컴파일 0err + 씬 깨짐 0.
- [ ] 에셋: `Art/Characters/NPC/{BlackSmith,Glocery}/` 스프라이트 사용 (보유 확인).

---

## 🧪 테스트

**자동**: Unity 컴파일 0err.
**수동**: 영호 Play — 마을에서 두 NPC에 각각 E키, 대사 패널 뜨는지.

---

## 📚 학습 포인트

- **클라 단독 정보 = 서버 권위 예외** — 하드코딩 대사는 게임 상태(HP/인벤토리/위치)를 바꾸지 않는 *순수 표현*이라 서버를 거치지 않아도 된다. 반대로 상점 구매(통화 변동)는 반드시 서버 (그래서 이번엔 대화만).
- **씬 편집 백업 의무** — Unity 씬/prefab YAML 편집은 손상 위험이 커서 편집 전 백업 (Phase 08 사고 박제).

---

## ⚠️ 함정 / 주의사항

- **대화만** — 상점/제작 기능 절대 X (확정 결정 1, 별도 마일스톤). 통화/인벤토리 변동이 생기면 서버 권위 위반.
- 씬 편집 백업 의무 — MCP 야간 시도 시에도 백업 후 진행 (Phase 08 학습).
- E키 = NPC 상호작용. 포탈 진입(B 트랙 "겹침+위키")·점프 바인딩과 입력 충돌 주의 (B2 inputactions 확인과 무관하지만 마을 동선 겹침 인지).

---

## ➡️ 다음 Phase

- Phase 21 — StageClear 폰트(TMP) → 애니메이션 스프라이트 교체.

---

## 📋 박제 (완료 후 -DONE.md)

- 단순 등급 → work-pin + commit message만.

---

## 작업 로그

- 2026-06-14: 생성.
