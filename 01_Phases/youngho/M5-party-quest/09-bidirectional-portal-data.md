---
owner: youngho
milestone: M5
phase: 09
title: 양방향 포탈 데이터 (역방향 항목)
status: pending
grade: 단순
domain: server
estimated: 0.5~1h
---

# Phase 09: 양방향 포탈 데이터 (역방향 항목)

> **상태**: pending
> **마일스톤**: M5 (트랙 B — 포탈 메커니즘 / B1)
> **등급**: 단순 (1 도메인 × 1 파일 / 가역적 — 데이터 항목 추가만)
> **담당**: server (Sonnet Worker — 메인 file:line 게이트)

---

## 🎯 목표

지금까지 포탈은 *정방향*(Town→HuntingGround→BossRoom→Ending)만 정의돼 있다. 이 Phase가 끝나면 **역방향 포탈 데이터**(HuntingGround→Town, BossRoom→HuntingGround)가 `PortalTable`에 등록돼, 플레이어가 왔던 맵으로 되돌아갈 수 있는 *데이터 기반*이 마련된다. 실제 씬 배치(외관)는 Phase 11, 클라 진입 입력은 Phase 10에서 한다 — 여기는 **서버 lookup 테이블만**.

---

## ⏪ 사전 조건

- [ ] Phase 01 (A0 — PDL 8패킷 + v15 bump) 완료 — Shared.dll 재빌드 통과.
- [ ] `PortalTable.cs` 정방향 항목 구조 확인 (역방향이 같은 스키마를 재사용).

---

## 📝 작업 내용

- [ ] `02_Server/GameServer/Maps/PortalTable.cs` — 역방향 portal 항목 추가. `portalId=2`로 ×3 (정방향은 `portalId=1` 관례 가정 — 진입 시 실측 확인):
  - HuntingGround → Town
  - BossRoom → HuntingGround
  - (Ending → Town 은 기존 항목이면 재사용, 없으면 추가 — 실측)
- [ ] 역방향 spawn 좌표 = **정방향 포탈 안쪽**(도착하자마자 또 역방향 포탈에 겹쳐 무한 왕복하는 재겹침 방지). 예: HuntingGround→Town 도착 시 Town의 정방향(Town→HG) 포탈 *바로 옆/안쪽*이 아니라 한 발 떨어진 안전 위치.
- [ ] 역방향 portal lookup이 정방향과 동일 경로(`PortalTable`)로 조회되는지 확인 — `portalId`로 dest 맵 + spawn 좌표 반환.

---

## ✅ 완료 조건

- [ ] 역방향 portal lookup이 `portalId=2`로 HuntingGround→Town, BossRoom→HuntingGround dest를 정확히 반환 (단위 테스트 또는 디버그 로그).
- [ ] 기존 `MapTransitionScenario` 봇 시나리오 **회귀 0** (정방향 이동이 그대로 동작).
- [ ] 역방향 spawn 좌표가 정방향 포탈과 겹치지 않음 (좌표 값 육안 검토 + 봇/Play 시 무한 왕복 X).

---

## 🧪 테스트

**자동**:
- 기존 `MapTransitionScenario` 봇 회귀 (정방향 무회귀 확인).
- (가능 시) `PortalTable` lookup 단위 테스트 — `portalId=2`로 dest/spawn 반환 검증.

**수동**:
- 디버그 로그로 역방향 lookup 결과 좌표 확인.

---

## 📚 학습 포인트

- **데이터와 동작의 분리** — 포탈 "데이터"(테이블 항목)와 "진입 동작"(클라 입력/씬 배치)을 다른 Phase로 쪼갰다. 서버 데이터부터 먼저 두면 클라/씬은 그 계약을 보고 만들 수 있다. 작은 데이터 변경이 가역적이라 야간 자율에 안전.
- **재겹침(re-overlap) 함정** — 도착 좌표를 도착지 포탈 위에 두면 trigger가 즉시 다시 발동돼 무한 왕복. 게임 월드에서 "전송 도착점은 전송 트리거 밖"은 일반 패턴이다.

---

## ⚠️ 함정 / 주의사항

- **역방향 spawn 좌표 = 정방향 포탈 안쪽이되 재겹침 방지** — 도착하자마자 또 포탈에 겹치지 않게 한 발 떨어뜨림.
- **Q3(보스 게이트, Phase 다름)와 병렬 안전** — `PortalTable`(데이터)과 `MapMigration`(게이트 조건)은 다른 파일이라 충돌 X. 단 **B1(이 Phase)을 Q3보다 먼저 권장** — 양방향 테이블이 완성된 상태를 Q3가 보고 게이트를 짤 수 있어 더 안전.
- **portalId 관례 실측** — 정방향 id가 `1`이라는 가정은 진입 시 `PortalTable.cs`에서 직접 확인 (stale 좌표 carry-over 주의).

---

## ➡️ 다음 Phase

- Phase 10 (B2) — 포탈 진입 = 겹침 상태 + 위 방향키 (클라 입력).

---

## 📋 박제 (완료 후)

- 단순 → work-pin + commit message. 마일스톤 `-DONE.md`에 흡수.

---

## 작업 로그

- 2026-06-14: 생성.
