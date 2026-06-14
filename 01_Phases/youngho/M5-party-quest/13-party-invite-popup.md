---
owner: youngho
milestone: M5
phase: 13
title: 파티 초대 송신 + 수락/거절 팝업 UI (placeholder swap-ready)
status: pending
grade: 보통
risk: unity-asset
domain: client
estimated: 1~2h
---

# Phase 13: 파티 초대 송신 + 수락/거절 팝업 UI (placeholder swap-ready)

> **상태**: pending
> **마일스톤**: M5 (트랙 P — 클라 파티/퀘스트 표현 / P2)
> **등급**: 보통 (**unity-asset 위험 깃발** — UI prefab/배경 sprite)
> **담당**: client / unity-bridge (스크립트 + UI 배선, 기능 검증 아침)

---

## 🎯 목표

플레이어가 다른 플레이어를 **파티에 초대**(`C_PartyInvite` 송신)하고, 초대를 받은 쪽은 **수락/거절 팝업**이 뜬다(`S_PartyInviteRecv` → 팝업, 버튼 → `C_PartyRespond` 송신). 이게 끝나면 첫 *플레이어 간 협동* 인터랙션이 화면에 보인다. 팝업 배경은 보유 에셋(`Dialog_Frame_Temporary.png`)을 재사용하되 **swap-ready**로 배선한다.

---

## ⏪ 사전 조건

- [ ] Phase 12 (P1 — 클라 파티 핸들러 + `PartyState`) 완료 — `S_PartyInviteRecv` 수신 경로 + 클라 미러 존재.

---

## 📝 작업 내용

- [ ] **초대 송신** — 타겟 플레이어 지정 후 `C_PartyInvite(targetEntityId)` 송신.
  - **타겟 지정 UX = 야간 기본: 근접 최단 타겟** (가장 가까운 다른 플레이어 entityId). **아침 영호 확인** — 근접 자동 vs 클릭 선택 (리스크 5).
- [ ] 신규 `03_Client/Assets/Scripts/UI/PartyInvitePopup.cs`:
  - `S_PartyInviteRecv` 수신(P1 핸들러 경유) → 팝업 표시 (초대자 정보).
  - 수락 버튼 → `C_PartyRespond(accept=true)` 송신, 팝업 닫기.
  - 거절 버튼 → `C_PartyRespond(accept=false)` 송신, 팝업 닫기.
- [ ] **UI 배경 = `Dialog_Frame_Temporary.png` 재사용 (swap-ready)** — 아래 규율.

### Swap-Ready 배선

- [ ] **배경 sprite 슬롯 분리** — 팝업 배경 Image의 sprite를 코드 하드코딩 X. `[SerializeField] Image` 슬롯(Inspector)으로 노출. placeholder = `Dialog_Frame_Temporary.png`.
- [ ] **위젯 레이아웃은 진짜 에셋과 동일 구조** — 버튼/텍스트 배치를 진짜 디자인과 같은 컴포넌트 트리로. 배경 sprite reference만 바꾸면 교체.
- [ ] **swap 지점 박제 의무** — `-DONE.md`에 "PartyInvitePopup의 배경 Image 슬롯에 진짜 다이얼로그 sprite를 꽂으면 동작" 명시.

---

## ✅ 완료 조건

- [ ] **초대 수신 시 팝업이 뜸** (육안) — 초대자 표시.
- [ ] **수락 버튼 → `C_PartyRespond` 송신** (육안/로그) — 서버 수신 확인.
- [ ] 거절 버튼 → `C_PartyRespond(accept=false)` 송신.
- [ ] **swap 지점이 `-DONE.md`에 박힘** (배경 sprite 슬롯 경로).
- [ ] Unity 컴파일 0err (메인 MCP).

---

## 🧪 테스트

**자동**: Unity 컴파일 0err.
**수동(아침)**: 2-클라 또는 봇+클라 — 한쪽이 초대 → 다른 쪽 팝업 → 수락 → `S_PartyUpdate`로 파티 결성 (P3 HUD와 연계 확인). 근접 타겟 UX 영호 확인.

---

## 📚 학습 포인트

- **요청-응답 비대칭 패킷** — 초대(`C_PartyInvite`)와 응답(`C_PartyRespond`)은 클라가 보내는 *요청*, `S_PartyInviteRecv`/`S_PartyUpdate`는 서버가 보내는 *통보*. 클라는 요청만, 결과는 서버 통보로 안다 (헌법 §1 권위 분리).
- **타겟 지정 UX의 트레이드오프** — 근접 자동(간편, 오타겟 가능) vs 클릭(정확, 입력 1단계 더). 야간엔 근접 기본으로 진행하되 영호 결정 게이트 (설계 분기는 사용자 확인).
- **swap-ready UI** — 배경 sprite를 Inspector 슬롯으로 빼면 placeholder→진짜 에셋 교체가 reference만. 위젯 레이아웃은 유지.

---

## ⚠️ 함정 / 주의사항

- **swap-ready 의무** — UI 배경 sprite 슬롯을 분리(`Dialog_Frame_Temporary.png` placeholder). 진짜 sprite 드롭 = 코드 무변경. swap 지점 `-DONE.md` 박제.
- **초대 타겟 UX = 야간 근접 기본 → 아침 영호 확인 (리스크 5)** — `targetEntityId`를 근접 최단으로 자동 선택. 클릭 방식은 영호 결정 후.
- **클라는 요청만 (권위 X)** — 수락해도 클라가 파티를 만들지 않는다. 서버 `S_PartyUpdate`가 와야 실제 결성. 팝업은 요청 송신까지만 책임.

---

## ➡️ 다음 Phase

- Phase 14 (P3) — 파티 멤버 HUD (병렬 가능, P1 의존).

---

## 📋 박제 (완료 후)

- 보통 → work-pin + commit message. **swap 지점은 `-DONE.md`에 별도 박제 의무** (swap-ready Phase 규율).

---

## 작업 로그

- 2026-06-14: 생성.
