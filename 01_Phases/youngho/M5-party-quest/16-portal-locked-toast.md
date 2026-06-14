---
owner: youngho
milestone: M5
phase: 16
title: 보스 포탈 잠금 피드백 토스트
status: pending
grade: 단순
domain: client
estimated: 0.5h
---

# Phase 16: 보스 포탈 잠금 피드백 토스트

> **상태**: pending
> **마일스톤**: M5 (트랙 P — 클라 파티/퀘스트 표현 / P5)
> **등급**: 단순 (1 도메인 × 2~3 파일 / 클라 스크립트 — 보유 UI 패턴 재사용)
> **담당**: client (Sonnet Worker — 메인 file:line 게이트, Unity 컴파일 검증)

---

## 🎯 목표

40킬을 채우기 전에 보스방 포탈로 진입하려 하면, 서버가 거부(`S_PortalLocked`)하고 클라는 **"보스 입장: 40킬 필요(현재 N)" 토스트**를 띄운다. 플레이어가 "왜 안 들어가지?"를 바로 알게 하는 피드백. required/current 숫자는 **서버 `S_PortalLocked` 값을 그대로** 표시한다.

---

## ⏪ 사전 조건

- [ ] Phase 08 (Q3 — 보스 포탈 잠금 게이트) 완료 — 서버가 `<40` 진입 시도에 `S_PortalLocked(required, current)` 송신.
- [ ] Phase 01 (A0 — PDL 8패킷 + v15 bump) 완료 — `S_PortalLocked` 패킷 정의 + Shared.dll 재참조.

---

## 📝 작업 내용

- [ ] 신규 `03_Client/Assets/Scripts/Network/Handlers/Zone/PortalLockedHandler.cs`:
  - `S_PortalLocked` 파싱 → required/current 추출 → `ToastUI`에 메시지 요청.
- [ ] 신규 `03_Client/Assets/Scripts/UI/ToastUI.cs`:
  - 짧게 떴다 사라지는 토스트 메시지 (자동 페이드/타이머). 재사용 가능한 범용 토스트.
- [ ] `UnityClientSession` — `PortalLockedHandler` dispatch 등록 (1줄).
- [ ] 토스트 문구 = **서버값 사용**: `"보스 입장: {required}킬 필요(현재 {current})"`.

---

## ✅ 완료 조건

- [ ] **40킬 미만 진입 시도 → 토스트 표시** (육안) — "보스 입장: 40킬 필요(현재 N)".
- [ ] required/current = **서버 `S_PortalLocked` 값** (하드코딩 X).
- [ ] 토스트가 자동으로 사라짐 (타이머/페이드).
- [ ] Unity 컴파일 0err (메인 MCP).

---

## 🧪 테스트

**자동**: Unity 컴파일 0err.
**수동(아침)**: 봇/클라 — 40킬 전 보스 포탈 진입 시도 → 토스트, 40 채운 뒤 진입 성공(토스트 X).

---

## 📚 학습 포인트

- **거부 피드백의 가치** — 서버가 "안 됨"만 보내면 플레이어는 멍하다. 이유(required/current)를 함께 보내 토스트로 보여주면 UX가 산다. 서버 거부 + 클라 피드백의 분업.
- **범용 토스트 컴포넌트** — `ToastUI`를 한 번 만들면 포탈 잠금 외에도 다양한 짧은 알림에 재사용. 재사용 가능한 UI 유틸리티 설계.

---

## ⚠️ 함정 / 주의사항

- **required/current 서버값 표시** — `S_PortalLocked`에서 읽은 값을 그대로. 클라가 40을 하드코딩하지 않는다 (서버 `QuestConstants.BossUnlockKillCount` 변경 추종).
- **Q3(P08) 선행 필수** — 서버 게이트가 `S_PortalLocked`를 송신해야 토스트 소스가 생긴다.
- **권위는 서버 (헌법 §1)** — 진입 차단 자체는 서버 `MapMigration` 게이트가 한다. 클라 토스트는 *결과 통보 표시*만. 클라가 진입을 막는 게 아니다.

---

## ➡️ 다음 Phase

- (트랙 P 완료) 이후 R1/R2 회귀 봇 + R3 마일스톤 마감으로 합류.

---

## 📋 박제 (완료 후)

- 단순 → work-pin + commit message. 마일스톤 `-DONE.md`에 흡수.

---

## 작업 로그

- 2026-06-14: 생성.
