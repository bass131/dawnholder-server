---
owner: youngho
milestone: M5
phase: 15
title: 퀘스트 진행 HUD ("N/40" 카운터, placeholder swap-ready)
status: pending
grade: 단순
risk: unity-asset
domain: client
estimated: 0.5~1h
---

# Phase 15: 퀘스트 진행 HUD ("N/40" 카운터, placeholder swap-ready)

> **상태**: pending
> **마일스톤**: M5 (트랙 P — 클라 파티/퀘스트 표현 / P4)
> **등급**: 단순 (**unity-asset 위험 깃발** — UI prefab/배경 sprite)
> **담당**: client / unity-bridge (스크립트 + UI 배선, 기능 검증 아침)

---

## 🎯 목표

40킬 퀘스트의 진행 상황을 화면에 **"N/40" 카운터 HUD**로 보여준다. 몬스터를 잡을 때마다 서버 `S_QuestUpdate`(파티 공유 킬카운트)가 와서 카운터가 갱신된다. **target(40)은 하드코딩하지 않고 서버 `S_QuestUpdate` 값을 그대로 사용**한다 — 서버가 진실. 배경은 보유 에셋(`Quest_Panel.png`) 재사용, **swap-ready** 배선.

---

## ⏪ 사전 조건

- [ ] Phase 12 (P1 — 클라 파티 핸들러) 완료 — `S_QuestUpdate`도 같은 dispatch 패턴으로 받음 (P1 핸들러 인프라 의존).
- [ ] Phase 07 (Q2 — 킬카운트 + S_QuestUpdate) 완료 — 서버가 `S_QuestUpdate`(current/target) 송신.

---

## 📝 작업 내용

- [ ] 신규 `03_Client/Assets/Scripts/UI/QuestProgressHud.cs`:
  - `S_QuestUpdate` 수신 → 카운터 텍스트 갱신 (`"{current}/{target}"`).
  - **target은 서버 `S_QuestUpdate` 값 사용** (40을 클라에 하드코딩 X).
- [ ] (필요 시) `S_QuestUpdate` 핸들러 — P1 dispatch 패턴 동형 (신규 핸들러 1개 + `UnityClientSession` 1줄). 또는 P1에서 이미 등록됐으면 HUD가 구독만.
- [ ] **UI 배경 = `Quest_Panel.png` 재사용 (swap-ready)** — 배경 Image sprite `[SerializeField]` 슬롯 분리, 위젯 레이아웃 진짜 디자인 동일 구조.
- [ ] **swap 지점 박제 의무** — `-DONE.md`에 "QuestProgressHud 배경 Image 슬롯 경로" 명시.

---

## ✅ 완료 조건

- [ ] **킬 시 카운터 갱신** (육안) — 몬스터 처치마다 N 증가.
- [ ] **target = 서버값 사용** (하드코딩 X) — `S_QuestUpdate.target`을 그대로 표시 (코드에 `40` 리터럴 없음).
- [ ] swap 지점이 `-DONE.md`에 박힘.
- [ ] Unity 컴파일 0err (메인 MCP).

---

## 🧪 테스트

**자동**: Unity 컴파일 0err.
**수동(아침)**: 봇/클라 — 몬스터 처치 시 카운터 증가, 파티 공유 합산 반영, 40 도달 표시.

---

## 📚 학습 포인트

- **서버가 진실 (하드코딩 금지)** — target 40을 클라에 박으면, 나중에 서버가 35로 바꿔도 클라는 40을 표시한다(불일치). `S_QuestUpdate.target`을 그대로 쓰면 서버 변경이 자동 반영. 헌법 §1 — 게임 규칙 값은 서버, 클라는 표시.
- **파티 공유 진행** — 카운트는 파티 합산(서버 `PartyState.KillCount`). 클라는 합산 결과만 받아 표시. 합산 로직은 서버에 있다.

---

## ⚠️ 함정 / 주의사항

- **target 하드코딩 금지** — `S_QuestUpdate`에서만 target을 읽는다. 코드에 `40` 리터럴 박지 말 것 (서버 변경 추종).
- **swap-ready 의무** — 배경 sprite 슬롯 분리(`Quest_Panel.png` placeholder). swap 지점 `-DONE.md` 박제.
- **Q2(P07) 선행 필수** — 서버가 `S_QuestUpdate`를 안 보내면 HUD가 갱신될 소스가 없다. Q2 완료 후 검증 가능.

---

## ➡️ 다음 Phase

- Phase 16 (P5) — 보스 포탈 잠금 피드백 토스트.

---

## 📋 박제 (완료 후)

- 단순 → work-pin + commit message. **swap 지점은 `-DONE.md`에 별도 박제 의무** (swap-ready Phase 규율).

---

## 작업 로그

- 2026-06-14: 생성.
