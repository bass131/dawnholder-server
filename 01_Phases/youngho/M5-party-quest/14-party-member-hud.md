---
owner: youngho
milestone: M5
phase: 14
title: 파티 멤버 HUD (멤버 목록 표시, placeholder swap-ready)
status: pending
grade: 단순
risk: unity-asset
domain: client
estimated: 0.5~1h
---

# Phase 14: 파티 멤버 HUD (멤버 목록 표시, placeholder swap-ready)

> **상태**: pending
> **마일스톤**: M5 (트랙 P — 클라 파티/퀘스트 표현 / P3)
> **등급**: 단순 (**unity-asset 위험 깃발** — UI prefab/배경 sprite)
> **담당**: client / unity-bridge (스크립트 + UI 배선, 기능 검증 아침)

---

## 🎯 목표

파티가 결성되면 화면에 **파티 멤버 목록 HUD**가 뜬다 — 정원 2명이니 멤버 2명 표시. 파티가 해산되면 HUD가 숨는다. `S_PartyUpdate`(P1 핸들러 → `PartyState` 미러)를 구독해 자동 갱신한다. 배경은 보유 에셋(`Status_Frame.png`) 재사용, **swap-ready** 배선.

---

## ⏪ 사전 조건

- [ ] Phase 12 (P1 — 클라 파티 핸들러 + `PartyState`) 완료 — `S_PartyUpdate` 미러 + 갱신 이벤트 존재.

---

## 📝 작업 내용

- [ ] 신규 `03_Client/Assets/Scripts/UI/PartyMemberHud.cs`:
  - `PartyState` 갱신 이벤트 구독 (또는 `S_PartyUpdate` 핸들러 연동).
  - 파티 결성 시 → 멤버 2명(member0/member1, 빈 슬롯 제외) 표시.
  - 파티 해산 시 → HUD 숨김.
- [ ] **UI 배경 = `Status_Frame.png` 재사용 (swap-ready)** — 배경 Image sprite를 `[SerializeField]` 슬롯으로 분리, 위젯 레이아웃은 진짜 디자인과 동일 구조.
- [ ] **swap 지점 박제 의무** — `-DONE.md`에 "PartyMemberHud 배경 Image 슬롯 경로" 명시.

---

## ✅ 완료 조건

- [ ] **파티 결성 시 멤버 2명 표시** (육안).
- [ ] **파티 해산 시 HUD 숨김** (육안).
- [ ] swap 지점이 `-DONE.md`에 박힘.
- [ ] Unity 컴파일 0err (메인 MCP).

---

## 🧪 테스트

**자동**: Unity 컴파일 0err.
**수동(아침)**: 2-클라/봇 — 파티 결성 시 멤버 2명 HUD, 한쪽 탈퇴/해산 시 숨김.

---

## 📚 학습 포인트

- **이벤트 구독 기반 UI 갱신** — HUD가 매 프레임 폴링하지 않고 `PartyState` 갱신 *이벤트*를 구독한다. 상태가 바뀔 때만 다시 그린다 (효율 + 단순). 데이터 변경 → UI 반영의 단방향 흐름.
- **빈 슬롯 처리** — 정원 2 고정에 `entityId=0`이 빈 슬롯. 표시 전 빈 슬롯 필터링. 고정 슬롯 + sentinel 값(0) 패턴.

---

## ⚠️ 함정 / 주의사항

- **swap-ready 의무** — 배경 sprite 슬롯 분리(`Status_Frame.png` placeholder). swap 지점 `-DONE.md` 박제.
- **해산 시 숨김 누락 주의** — 파티가 깨졌는데 HUD가 남아 있으면 stale 표시. `S_PartyUpdate`(빈 파티)/해산 통보에 반드시 숨김 반응.
- **클라 미러만 읽음 (권위 X)** — HUD는 `PartyState`(서버 통보 미러)를 표시만. 직접 멤버를 추가/제거하지 않는다.

---

## ➡️ 다음 Phase

- Phase 15 (P4) — 퀘스트 진행 HUD (병렬 가능, P1 의존 + Q2 S_QuestUpdate 의존).

---

## 📋 박제 (완료 후)

- 단순 → work-pin + commit message. **swap 지점은 `-DONE.md`에 별도 박제 의무** (swap-ready Phase 규율).

---

## 작업 로그

- 2026-06-14: 생성.
