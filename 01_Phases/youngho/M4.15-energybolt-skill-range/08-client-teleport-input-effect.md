---
owner: youngho
milestone: M4.15
phase: 08
title: 클라 4방향 입력 송신 + depart/arrive 이펙트 버그 수정
status: pending
grade: 보통
domain: client
summary: LocalPlayerInput 위/아래 폴링 + verticalDir 송신 + SkillCastHandler 출발/도착 이펙트 타이밍 레이스 결정론적 수정
---

# Phase 08: 클라 4방향 입력 송신 + depart/arrive 이펙트 버그 수정

> **상태**: pending
> **마일스톤**: M4.15 (워크스트림 D)
> **등급**: 보통 (client 스크립트만 — prefab 무변경, unity-asset 깃발 미발동)
> **담당**: client (Sonnet Worker — 메인 file:line 게이트)

---

## 🎯 목표

두 가지: ① 클라가 텔레포트 시 **위/아래 입력을 읽어 `verticalDir`을 송신**(P06 필드), ② **출발/도착 이펙트가 제대로 안 뜨는 버그를 결정론적으로 수정**. 영호 진단: "시작점 이펙트 → 도착지점(캐릭터 위치 기준) 이펙트 생성이 제대로 안됨".

---

## ⏪ 사전 조건

- [ ] Phase 06 완료 (`C_SkillUse.verticalDir` 송신 필드 존재).
- [ ] Phase 07 권장 선행 (서버 4방향 거동 = 통합 Play-test 기준). 단, 클라 입력/이펙트 코드는 P06만 있으면 작성 가능.

---

## 📝 작업 내용

### (1) 4방향 입력 송신

- [ ] `03_Client/.../Input/LocalPlayerInput.cs` — 텔레포트 송신 시 위/아래 입력 캡처. 스킬 키 폴링(`Update`, Q/E)과 동형으로 `Keyboard.current`의 위/아래(W/S 또는 ↑/↓) `isPressed` 조회 → `verticalDir`(0=없음/1=위/2=아래) 산출.
  - 우선순위: 수직 입력 있으면 수직, 없으면 수평(기존 facing). MapleStory식 4-cardinal(대각 X).
  - `C_SkillUse`에 `verticalDir` 세팅 후 송신 (`facing`은 기존대로 좌우 유지 — 스프라이트 방향).
- [ ] (검토) `LocalPlayerMovement.NotifyTeleport` 선예측 — 수직 텔레포트도 force-adopt 스냅으로 서버 위치 채택(이미 방향 무관 = 변경 불필요 가능성 높음, 실측 확인).

### (2) depart/arrive 이펙트 레이스 수정

- [ ] **근본 원인** (메인 실측): `SkillCastHandler.HandleTeleport`(L126-155)가 출발 위치를 *S_SkillCast 수신 시점*에 `casterTf.position`으로 잡는데(L128), 그 사이 force-adopt 스냅이 이미 일어나면 출발 이펙트가 도착지점에 찍힘. 게다가 `NotifyTeleport`가 송신 시점(`LocalPlayerInput:183`) + 수신 시점(`SkillCastHandler:134`) **2회** 호출 → arrive 콜백이 stale 스냅에 먼저 소비될 수 있음(레이스).
- [ ] **수정 방향** — **로컬은 송신 시점에 결정론화**:
  - 출발 이펙트 + arrive 콜백 등록을 **`LocalPlayerInput` 송신 시점**(아직 출발 위치 확정 + 스냅 전)으로 이동. → 출발 이펙트 = 항상 출발점, arrive 콜백 = 첫 force-adopt 스냅(=도착 확정) 직후 캐릭터 위치 기준.
  - `SkillCastHandler.HandleTeleport`의 **로컬 분기는 이펙트 생략**(이미 송신 시점 처리) — `S_SkillCast`는 **원격 플레이어 이펙트만** 구동(원격은 송신 시점을 모르므로 기존 콜백 경로 유지).
  - `NotifyTeleport` 이중 호출 정리 — 송신 시점 1회로 통일(콜백 포함). 수신 시점 재호출 제거.
  - (대안 검토) 송신 시점 스폰은 서버 거부(쿨다운) 시 spurious 이펙트 가능 — 쿨다운은 클라 거울 게이트(`CanUseTeleport`)라 드묾 + cosmetic(헌법 §1 비권위)이라 허용. Dash 선예측과 동일 사고.
- [ ] 도착 이펙트는 `EffectAnchor.ResolvePosition(entityTf)`(캐릭터 위치 기준) 유지 — 영호 요구 "도착지점에 캐릭터 위치 기준" 정합.

---

## ✅ 완료 조건

- [ ] 위/아래 키 + E → `verticalDir` 송신 (1=위/2=아래), 수평은 0 (Unity Play 또는 로그 확인).
- [ ] **출발 이펙트 = 출발점**(텔레포트 전 위치)에 1회 — 도착지점에 안 찍힘.
- [ ] **도착 이펙트 = 도착점**(텔레포트 후 캐릭터 위치)에 1회 — 누락 없음.
- [ ] 레이스 제거 — 연속 텔레포트/방향 전환에도 이펙트 위치 안정 (영호 Play 반복).
- [ ] **원격 출발 이펙트 회귀 게이트** (plan-auditor 봉합): ⚠️실측 — `HandleTeleport`(L126-155)의 출발 이펙트 스폰(L130)이 `if(isLocal)` 분기 *앞*에 있어 로컬/원격 **공유**. "로컬만 송신시점으로 옮긴다"고 L130을 옮기면 *원격*도 출발 이펙트를 잃음. → **원격 플레이어 출발 이펙트가 `S_SkillCast` 수신시점 `departPos`로 여전히 1회 스폰**됨을 별도 정량 게이트로 확인 (분기 앞 공유 줄을 로컬/원격 각자 경로로 갈라야).
- [ ] 원격 플레이어 도착 이펙트도 정상 — 로컬 경로 변경이 `RemoteEntityRegistry.SetTeleportArriveCallback`/`SnapEntity` 원격 경로 안 깸.
- [ ] Unity 컴파일 0err + EditMode 회귀 0 (메인 MCP).
- [ ] 헌법 §1 — 이펙트는 cosmetic(서버 위치 채택 후 도착 표현), 권위 위치는 서버 force-adopt 유지.

---

## 🧪 테스트

**자동**: EditMode (이펙트 위치 산출 순수 로직 분리 가능 시 — verticalDir 산출/우선순위). 어려우면 컴파일 + Play 검증.
**수동**: 영호 Play — 4방향 텔레포트, 출발/도착 이펙트 위치 정확, 연속 시전 안정. 원격(2-클라) 확인 가능 시.

---

## 📚 학습 포인트

- **레이스 = 비결정 타이밍 의존** — 이펙트 위치를 "수신 시점 현재 위치"에 의존하면 스냅 순서에 따라 흔들림. *송신 시점*(상태가 확정적)에 캡처하면 결정론. "언제 측정하느냐가 무엇을 측정하느냐를 바꾼다."
- **로컬 vs 원격 이펙트 경로 분리** — 로컬은 송신 시점을 알아 선예측 가능, 원격은 `S_SkillCast` 수신으로만 알아 콜백 경로 필요. 같은 연출도 정보 출처가 다르면 경로가 다르다 (Dash `HandleDash`와 동형 사고).
- **cosmetic 선예측의 안전성** (헌법 §1) — 이펙트는 비권위라 서버 거부 시에도 무해(투사체의 "데미지 0 그림 명중" 문제와 다름 — 그건 명중 *판정* 오해 소지라 서버 확정 후 스폰). 텔레포트 이펙트는 위치 표현만.

---

## ⚠️ 함정 / 주의사항

- **선예측 스폰 부활 아님** — M4.8 기둥1(투사체는 서버 확정 후 스폰)은 *투사체*에 한정. 텔레포트 이펙트는 cosmetic 위치 표현이라 송신 시점 스폰 허용 (다른 맥락).
- **NotifyTeleport 이중 호출 제거 시 force-adopt 스냅 보존** — 송신 시점 `_teleportSnapPending=true`는 유지해야 서버 위치 채택(보간 끊기)이 됨. 이펙트 콜백만 정리, 스냅 플래그 로직 건드리지 말 것.
- **원격 경로 회귀** — 로컬 분기 변경이 `RemoteEntityRegistry.SetTeleportArriveCallback`/`SnapEntity` 원격 경로를 깨지 않게. 원격은 기존 유지.
- **Unity 테스트 stale 함정** (carry-over) — 신규 EditMode 안 잡히면 DLL mtime 확인 + 영호 Test Runner Run All.

---

## ➡️ 다음 Phase

- Phase 09 — 회귀 + 봇 시나리오 갱신 + 마일스톤 마감 (텔레포트 회귀 흡수).

---

## 📋 박제 (완료 후)

- 보통 → work-pin + commit message. 마일스톤 `-DONE.md`(P09) 워크스트림 D에 흡수.

---

## 작업 로그

- 2026-06-14: 생성.
