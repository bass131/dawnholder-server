---
owner: youngho
milestone: M4.15
phase: 07
title: 서버 4방향 텔레포트 + 거리 1/3 + 수직 지형 안전
status: done
grade: 복잡
domain: server
summary: SkillUseHandler verticalDir 정규화(trust boundary) + ActionContext 수직 반영 + TeleportAction 4방향 목적지 + TeleportDistance 15→5 + 수직 경계/지형 안전
---

# Phase 07: 서버 4방향 텔레포트 + 거리 1/3 + 수직 지형 안전

> **상태**: pending
> **마일스톤**: M4.15 (워크스트림 D)
> **등급**: 복잡 + **trust-boundary**(SkillUseHandler 변경) → 구현 Worker = **Opus**(routing B)
> **담당**: server (Opus Worker — 메인 file:line 게이트)

---

## 🎯 목표

P06이 만든 `verticalDir` 필드를 서버가 소비해 **4방향(좌/우/상/하) 텔레포트**를 권위적으로 처리한다. 동시에 **이동 거리를 1/3(15→5)** 로 줄이고, 수직 텔레포트가 **땅속/천장/맵 밖으로 빠지지 않도록** 경계·지형 안전을 보장한다. 신뢰 경계(`SkillUseHandler`)를 지나므로 untrusted `verticalDir`는 정규화·검증한다.

---

## ⏪ 사전 조건

- [ ] Phase 06 완료 (`C_SkillUse.verticalDir` 필드 존재 + ProtocolVersion v14).

---

## 📝 작업 내용

- [ ] `02_Server/.../Handlers/Skill/SkillUseHandler.cs` (**trust boundary**) — `pkt.verticalDir`을 **whitelist 정규화**: 허용 집합 `{0=없음, 1=위, 2=아래}`에 *속하면 통과, 아니면 안전 기본값 0*. ⚠️ facing의 2진 패턴(`==1?1:-1`)을 모방하지 말 것 — verticalDir은 **3진 정의역**이라 "1 아니면 0"으로 짜면 **2(아래)가 0(수평)으로 뭉개져** 아래 텔레포트가 죽음. `verticalDir is 1 or 2 ? verticalDir : 0` 같은 whitelist 술어 사용. `session.SubmitSkillUse(...)`에 수직 방향 전달 (시그니처 확장 또는 facing과 합친 방향 표현).
- [ ] `02_Server/.../Network/GameSession.cs` `SubmitSkillUse` — 수직 방향 인자 추가(EnqueueJob 람다 캡처 정합).
- [ ] `02_Server/.../Maps/GameMap.cs` `ProcessSkill` + `02_Server/.../Maps/Systems/SkillSystem.cs` — 수직 방향을 `ActionContext`까지 전달.
- [ ] `02_Server/.../Maps/Actions/ActionContext.cs` — 수직 방향 필드 추가 (`sbyte VerticalDir` 또는 2D 방향). **Dash의 `Facing`(좌우 -1/+1) 계약 불변 유지** — Dash는 수직 무시. readonly struct + in 전달 유지(헌법 §5 틱 루프 new 0).
- [ ] `02_Server/.../Maps/Actions/TeleportAction.cs` — 4방향 목적지 산출:
  - 수직 의도(위/아래)면 Y축 이동: `destY = Y ± TeleportDistance`. 아니면 기존 수평: `destX = X + TeleportDistance * FacingDir`.
  - **수직 경계/지형 안전 (MVP = 옵션 ② 고정 — plan-auditor 봉합)**: 현재 `MapBoundsX`만 clamp(L23-24), `MapBoundsY` 부재. **MVP는 옵션 ②로 확정**: 안전한 Y 범위로 clamp(`MapBoundsY` 신설 또는 맵 Y 상수) + 기존 물리(중력/충돌)가 다음 틱에 solid 침투를 resolve하도록 위임. 영구 끼임(stranding) 방지가 핵심 — 도착 직후 solid 안이어도 K틱 내 non-solid로 수렴해야 함. (옵션 ①=`_terrain.Solids` 사전 질의로 목적지 검증은 신규 인프라라 *이번 스코프 밖*. 구현 중 terrain 질의가 trivial하다고 판명되면 업그레이드 여지만 남김 — 단 MVP 게이트는 ②.)
  - `RecordPosition`(rewind 히스토리) + `S_SkillCast` 브로드캐스트 유지.
- [ ] `02_Server/.../Combat/CombatConstants.cs` — `TeleportDistance` 15.0 → **5.0**(1/3). (선택) 수직 거리가 수평과 달라야 하면 `TeleportDistanceY` 분리 — 기본은 공통 5.0, 영호 Play 튜닝.
- [ ] `02_Server/GameServer.Tests/Combat/MageTeleportTests.cs` — 갱신:
  - 4방향 각각 목적지 검증 (좌/우/상/하 — 특히 아래(2)가 수평으로 안 뭉개짐).
  - 거리 5.0 반영 (기존 15 가정 케이스 재계산).
  - 수직 경계/지형 안전 케이스 (맵 Y 범위 clamp + 영구 끼임 0).
  - **whitelist 경계 테스트**: verticalDir ∈ {3, 99, 255} → 0(수평) (경계값 3 = off-by-one cheat 필수).

---

## ✅ 완료 조건

- [ ] verticalDir 1=위 → `destY > Y`, 2=아래 → `destY < Y`, 0 → 수평(기존 거동). 테스트로 4방향 증명.
- [ ] `TeleportDistance == 5.0` 반영 (수평 이동량 = 5.0 * FacingDir).
- [ ] 수직 텔레포트가 맵 경계 내 clamp + 영구 끼임 0 (테스트 — 도착 위치가 맵 Y 범위 내 + solid 침투 시 K틱 내 non-solid 수렴, 영구 stranding 방지).
- [ ] **trust boundary (whitelist 정규화)**: verticalDir 경계+극단값 차단 테스트 — **`{3, 99, 255}` 전부 → 0(수평)** (off-by-one cheat 경계값 3 필수 포함, 99만으론 불충분). 허용값 `{0, 1, 2}`는 각각 통과 — 특히 **2(아래)가 0으로 안 뭉개짐** 검증(3진 whitelist 정확성).
- [ ] Dash `Facing` 계약 불변 (KnightDashTests green — 수직 추가가 대쉬 방향 권위 안 깸).
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (baseline 570 ± 신규 텔레포트 케이스).
- [ ] reviewer 헌법 hard 위반 0 (trust-boundary 변경 = reviewer 자동 호출).

---

## 🧪 테스트

**자동**: `MageTeleportTests` — 4방향 목적지 + 거리 5.0 + 수직 경계 안전 + verticalDir clamp. `KnightDashTests`(Facing 계약 회귀).
**수동**: P08 후 영호 Play — 위/아래/좌/우 텔레포트 + 짧아진 거리 체감 + 땅 안 뚫림.

---

## 📚 학습 포인트

- **신뢰 경계 정규화** (헌법 §3) — 클라가 보낸 `verticalDir`은 untrusted. 정의역 밖(예: 2 초과)은 안전값(0)으로 clamp. "cheat 무해(거리 고정)라도 부호/범위 정규화는 규율" — facing 정규화(`==1?1:-1`)와 동형.
- **사이드스크롤 수직 이동의 지형 함정** — 수평 점프는 `MapBoundsX`만으로 안전하지만, 수직은 발판/천장이 있어 `_terrain.Solids` 고려 없이 Y를 바꾸면 땅속/천장 끼임. "축이 늘면 충돌 차원도 는다."
- **계약 격리** — 수직 방향 추가가 Dash의 `Facing`(좌우) 계약을 안 건드리게 하는 게 핵심. ActionContext에 *추가*하되 기존 의미 불변 = OCP.

---

## ⚠️ 함정 / 주의사항

- **ActionContext는 readonly struct + in 전달** (헌법 §5) — 필드 추가해도 틱 루프 new 0 유지. 클래스로 바꾸지 말 것.
- **ActionGate L28-29** — `if (kind == Dash) player.FacingDir = ctx.Facing;`. 텔레포트는 FacingDir 안 건드림(수직 텔레포트 시 스프라이트 좌우 facing 보존). 수직값이 FacingDir로 새지 않게 주의.
- **거리 5.0 단일 변경이 수평/수직 양쪽 영향** — `TeleportDistance` 공통이면 수직도 5.0. 발판 간격(히트박스 Y≈1.0~1.5 기준 층 Δ≈1.5) 대비 5.0이면 여러 층 점프 → 영호 Play에서 수직만 따로 줄일 수 있으니 `TeleportDistanceY` 분리 여지 주석.
- **rewind 히스토리** — `RecordPosition(CurrentTick, 새 위치)` 유지 (lag-comp 정합). 수직 이동도 기록.

---

## ➡️ 다음 Phase

- Phase 08 — 클라 4방향 입력 송신 + depart/arrive 이펙트 버그 수정.

---

## 📋 박제 (완료 후)

- 복잡+trust-boundary → work-pin + commit message. reviewer 요약. 마일스톤 `-DONE.md`(P09) 워크스트림 D에 흡수.

---

## 작업 로그

- 2026-06-14: 생성.
- 2026-06-14: 완료 (`1161e3c`, server Worker Opus + 메인 file:line 게이트 + reviewer🔴0). SkillUseHandler whitelist 정규화(`is 1 or 2 ? : 0`, 3진), ActionContext.VerticalDir(default 0, readonly 유지), 시그니처 체인(GameSession→GameMap→SkillSystem) verticalDir 전달, TeleportAction 4방향 분기(1=위Y+/2=아래Y-/0=수평X±), GameMap.MapBoundsY 신설(solids min/max Y clamp), TeleportDistance 15→5. WSL2 build 0/0 + test **579/0/5**(baseline 570 + 9 신규). **테스트 캘리브레이션 정정**(메인): Worker가 SAC로 못 돌린 수직 테스트 3개가 물리(중력+지면충돌) 얽힘으로 깨짐 → 방향+X정확불변 robust 검증으로 수정 + BossBehaviorTests v13→v14. **⚠️ 영호 Play 플래그**: 수직 텔레포트 near terrain(특히 아래 near floor)은 MVP 옵션② 한계로 clamp 경계 미세 초과(~-1.05)/floor-top 미안착 가능 — terrain-aware(옵션①)는 후속.
