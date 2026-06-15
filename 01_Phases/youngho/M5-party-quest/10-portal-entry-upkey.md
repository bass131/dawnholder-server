---
owner: youngho
milestone: M5
phase: 10
title: 포탈 진입 = 겹침 상태 + 위 방향키 (자동 진입 폐지)
status: pending
grade: 보통
domain: client
estimated: 1~2h
---

# Phase 10: 포탈 진입 = 겹침 상태 + 위 방향키 (자동 진입 폐지)

> **상태**: pending
> **마일스톤**: M5 (트랙 B — 포탈 메커니즘 / B2)
> **등급**: 보통 (1 도메인 × 2 파일 / 클라 스크립트 — 기능 검증은 아침 Play)
> **담당**: client (Sonnet Worker — 메인 file:line 게이트, Unity 컴파일 검증)

---

## 🎯 목표

지금은 포탈에 *닿기만 하면* 자동으로 다음 맵으로 넘어간다(`OnTriggerEnter2D` 즉시 전송). 이게 불편하다 — 지나가다 실수로 빨려 들어간다. 이 Phase가 끝나면 포탈 진입이 **MapleStory식 "포탈 위에 겹친 상태 + 위 방향키 눌러야 진입"**으로 바뀐다. 포탈 위에 가만히 서 있어도 자동 진입 X, **위 방향키 down-edge(눌리는 순간)에만** `C_EnterPortal`을 송신한다.

---

## ⏪ 사전 조건

- [ ] Phase 09 (B1 — 양방향 포탈 데이터) 완료 — 진입 시 서버가 dest를 lookup할 수 있어야 동작 확인 가능.
- [ ] **(선행 확인 의무) `*.inputactions`에서 위 방향키(↑)가 점프에 바인딩돼 있는지** — 리스크 4 직결. 점프 바인딩이면 포탈 위 점프로 trigger를 이탈해버린다.

---

## 📝 작업 내용

- [ ] `03_Client/Assets/Scripts/Gameplay/PortalTrigger.cs` 수정:
  - `OnTriggerEnter2D` 즉시 송신 → **`overlap` 플래그 방식**으로 전환. `OnTriggerEnter2D`/`OnTriggerExit2D`에서 `_isOverlapping` 플래그만 켜고 끔.
  - `Update` 폴링 — `_isOverlapping == true` 일 때만, **위 방향키 down-edge**(이번 프레임에 눌림, 직전 프레임 안 눌림)를 감지해 `C_EnterPortal` 1회 송신.
  - 기존 송신 **쿨다운 유지** (연타/중복 송신 방지).
- [ ] **`*.inputactions` 점프 바인딩 확인** (선행) — ↑가 점프면, 포탈 겹침 중에는 클라 로컬 점프를 억제할지 검토 (포탈 위에서 ↑ = 진입, 점프 X). 텔레포트 `verticalDir`(↑+E *조합*)와는 직접 충돌 X — 단독 ↑만 다르게 해석.

---

## ✅ 완료 조건

- [ ] 포탈 위에 **가만히 정지 = 자동 진입 X** (overlap 만으로는 안 넘어감).
- [ ] **위 방향키 down-edge 에만** `C_EnterPortal` 송신 — 누른 채 유지해도 1회만 (홀드 반복 송신 X).
- [ ] 송신 쿨다운 유지 (중복 송신 방지).
- [ ] **Unity 컴파일 0err** (메인 MCP RunCommand probe). 기능 검증(실제 진입)은 아침 Play.

---

## 🧪 테스트

**자동**: Unity 컴파일 0err (MCP). down-edge 산출 로직이 순수 분리 가능하면 EditMode.
**수동(아침)**: 영호 Play — 포탈 위 정지 시 안 넘어감 확인, ↑ 누르는 순간 진입, 홀드 시 1회만, 양방향(09 데이터) 왕복.

---

## 📚 학습 포인트

- **edge-triggered vs level-triggered 입력** — "눌려 있음(level)"이 아니라 "눌리는 *순간*(edge)"을 감지해야 1프레임 1회 동작. `isPressed`(level)와 `wasPressedThisFrame`(edge)의 차이. 홀드로 반복 발동을 막는 표준 패턴.
- **상태 플래그 + 폴링 vs 이벤트 즉시 처리** — `OnTriggerEnter`에서 바로 행동하면 "닿자마자"가 된다. 플래그만 켜고 `Update`에서 조건(입력)을 보면 "닿은 상태에서 의도적으로" 행동하게 된다.

---

## ⚠️ 함정 / 주의사항

- **↑ = 점프 바인딩이면 충돌 (리스크 4)** — 포탈 위에서 ↑를 누르면 점프로 trigger를 이탈해 overlap 플래그가 꺼질 수 있다. `*.inputactions` 확인 후, 포탈 겹침 중 클라 로컬 점프 억제를 검토. **이 확인 없이 B2 확정 불가.**
- **텔레포트 verticalDir와 혼동 X** — 텔레포트는 ↑+E *조합*, 포탈 진입은 단독 ↑. 같은 키지만 조합/단독으로 갈린다.
- **권위는 서버** (헌법 §1) — 클라는 `C_EnterPortal`을 *요청*만. 실제 맵 전환은 서버 검증 후. 클라가 직접 위치를 바꾸지 않는다.

---

## ➡️ 다음 Phase

- Phase 11 (B3) — 역방향 포탈 씬 배치 (Unity 외관, 아침).

---

## 📋 박제 (완료 후)

- 보통 → work-pin + commit message. 마일스톤 `-DONE.md`에 흡수.

---

## 작업 로그

- 2026-06-14: 생성.
