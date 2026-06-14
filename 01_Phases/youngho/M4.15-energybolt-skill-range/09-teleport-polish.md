---
owner: youngho
milestone: M4.15
phase: 09
title: 텔레포트 폴리시 — 지형 인식 수직 이동 + 도착 이펙트 결정론
status: pending
grade: 복잡
domain: server+client
summary: 수직 텔레포트 거리기반→지형 인식(발판 탐지·최대 사거리 snap·이동불가 시 이펙트만) + 도착 이펙트 타이밍(S_SkillCast 수신 arming) + 캐릭터 parenting
---

# Phase 09: 텔레포트 폴리시 — 지형 인식 수직 이동 + 도착 이펙트 결정론

> **상태**: pending
> **마일스톤**: M4.15 (워크스트림 D — 텔레포트, 영호 Play-test 2차 피드백)
> **등급**: 복잡 (server 지형 쿼리 + client 이펙트 — 2도메인, wire 불변 v14, irreversible/trust-boundary 미발동)
> **담당**: server Worker(Sonnet, 지형 수직) + client Worker(Sonnet, 이펙트) — 메인 file:line 게이트

---

## 🎯 목표

P07/P08 Play-test 2차 피드백 2종을 봉합한다:
1. **수직 텔레포트 = 지형 인식** — 고정 거리(P07 MVP) 폐기. 위/아래 발판을 찾아 **최대 사거리(`TeleportVerticalRange`) 안이면** 발판 윗면으로 snap. 발판 없거나 사거리 밖이면 **이동X(이펙트는 출력)**. (영호 결정 — 바닥 파고듦 해소.)
2. **도착 이펙트가 도착지에 안 뜸** — 로컬 arming이 송신 시점이라 텔레포트 반영 *전* 스냅이 콜백을 먼저 소비(옛 위치 발동 + 월드고정 미추적). → 로컬 arming을 `S_SkillCast 수신 시점`으로(원격 모델 정합) + arrive 이펙트 **캐릭터 parenting**("딱 붙어야").

---

## ⏪ 사전 조건

- [x] Phase 06~08 완료 (v14 + 서버 4방향 + 클라 입력/출발이펙트). 영호 Play-test 1차 피드백 반영.
- [x] 영호 결정 (AskUserQuestion 2026-06-14): 수직 = 지형 인식 + 최대 사거리 안이면 이동 / 도착 이펙트 = 캐릭터 글루.

---

## 📝 작업 내용

### Part 1 — 지형 인식 수직 텔레포트 (server)

- [ ] `02_Server/.../Maps/GameMap.cs` — `TryFindVerticalTeleportTarget(float x, float currentY, bool up, float maxRange, out float destY)` 신설.
  - 후보 = `Solids[].MaxY` + `Platforms[].Y` 중 `x ∈ [MinX-eps, MaxX+eps]` (eps=`GroundEpsilon` 0.0001).
  - **up**: `surfaceY > currentY + eps` 중 가장 낮은 것(가장 가까운 위, 현재 발판 자동 제외). **down**: `surfaceY < currentY - eps` 중 가장 높은 것(가장 가까운 아래). 공중(낙하 중)일 때 down은 발 밑 첫 착지 발판으로 — `< currentY - eps`가 자연 처리(auditor 🟡①).
  - 가장 가까운 발판이 `|surfaceY - currentY| <= maxRange`면 `destY=surfaceY` + true. 없거나 사거리 밖이면 false.
  - 틱 루프 내 호출 → span 순회만(alloc 0, 헌법 §5).
- [ ] `02_Server/.../Maps/Actions/TeleportAction.cs` 수직 분기 교체:
  - `verticalDir==1/2`: 쿼리 true → `destX=X, destY=발판표면`. false → **이동 없음**(`destX=X, destY=Y`).
  - ⚠️ **early-return 금지** (auditor 🔴①): 이동 불가 분기에서 `if(!found) return false` 하면 `S_SkillCast` 브로드캐스트가 빠져 영호가 본 "이펙트 안 뜸"을 *서버에서 재현*. → 두 경우 모두 `Position` 세팅(이동 없으면 현위치) 후 **무조건 `BroadcastToAll(castPkt)` 1회 + `return true`**. 현 구조(`:55-66`)가 이미 무조건 broadcast라 dest=현위치로만 두면 자연 충족 — 실패 분기에 별도 early-return 넣지 말 것.
  - 수평(`0`) = 현행 유지(`TeleportDistance` + MapBoundsX clamp).
  - **Position.Y = 발바닥 확정**(auditor 실측: `Physics.cs:94` "지면 Y = 발바닥", `:163` `Position.Y <= GroundY+eps`) → `destY = 발판 표면 Y` 직접 대입, **오프셋 보정 불요**.
- [ ] **MapBoundsY 제거** (`GameMap.cs:137-152` — P07 수직 clamp 전용, 지형 쿼리가 대체). 소비자 = GameMap 정의 + TeleportAction + 테스트 2종뿐(auditor 실측, 외부 0). **제거 순서**(auditor 🟡②): 테스트 2종(`Vertical_Up/Down_ClampedToMapBoundsY`, `map.MapBoundsY` 직접 호출) **먼저** 제거 → 그 다음 프로퍼티 제거(안 그러면 컴파일 에러). MapBoundsX(`:115-130`) 유지(수평).
- [ ] `02_Server/.../Combat/CombatConstants.cs` — `TeleportVerticalRange` 신설(시작 5.0, Play 튜닝). `TeleportDistance`(5.0, 수평) 유지. P07 `TeleportDistanceY` 주석(코드 미존재, 주석만) 정리.
- [ ] `02_Server/GameServer.Tests/Combat/MageTeleportTests.cs` 수직 테스트 재작성:
  - **발판 포함 terrain 헬퍼 신설**(auditor 🔴②): `MakeBoundedTerrain`(`:131-137`)은 Solids만(`Array.Empty<TerrainPlatform>()`) → `MakeTerrainWithPlatforms(params float[] platformYs)` 같은 헬퍼 추가(발판 Y를 픽스처 상수로 고정 → 기대 destY 정확 검증).
  - 위 발판 사거리 안 → `destY == 발판 표면 Y`. 아래 발판 사거리 안 → `destY == 발판 표면 Y`.
  - **destY 정확 검증 시점**(auditor 🔴③ — P07이 부딪힌 중력 함정): `Execute` 직후(물리 Step 전) Position이 정확값. 단 테스트가 `map.Tick`으로 물리까지 돌리면 발판 위 안착이라 다음 틱 중력이 표면에 재snap → `±eps` 허용. **정확값 Assert는 발판 위(중력 중립) 케이스로 구성** + precision 명시(발판 표면이라 안착 후에도 안정).
  - 발판 사거리 밖 → 이동 X (Position 불변). 발판 없음 → 이동 X.
  - **이동 불가 케이스도 `S_SkillCast` 브로드캐스트 1회 + Position 불변 동시 Assert**(auditor 🔴① — early-return 회귀 차단 게이트).
  - P07 clamp 테스트(`Vertical_Up/Down_ClampedToMapBoundsY`) 제거(MapBoundsY 제거와 동반). whitelist/수평/거리 테스트 유지.

### Part 2 — 도착 이펙트 결정론 + parenting (client)

- [ ] `03_Client/.../Prediction/LocalPlayerMovement.cs` — `NotifyTeleport` 책임 분리:
  - 송신용: 쿨다운 예측 + `departPos` stash (snap-pending/arrive 콜백 X).
  - 확정용 `ArmTeleportSnap(Action arriveCallback)`: `_teleportSnapPending` set + arrive 콜백 등록.
  - `_teleportSnapPending` force-adopt 로직(`:407-426`) 불변.
- [ ] `03_Client/.../Input/LocalPlayerInput.cs` — 송신 시점 = departPos stash + 쿨다운만(arrive 콜백 등록 제거).
- [ ] `03_Client/.../Network/Handlers/Skill/SkillCastHandler.cs` 로컬 분기 = `S_SkillCast` 수신 시점에 출발 이펙트(stash 위치) + `ArmTeleportSnap(arriveCallback)`. → 텔레포트 반영 첫 snapshot에서 force-adopt + arrive 발동(도착지).
  - arrive 이펙트 = 캐릭터 transform **parenting**(방향 무관 facingSign=0, flip 없음). `SpawnEffectParented` 재사용 또는 facingSign=0 helper. EffectLifetime 0.52초라 스냅 전 만료 위험 0.
  - 원격 경로(`SetTeleportArriveCallback`/`SnapEntity`) 무변경.

---

## ✅ 완료 조건

- [ ] 수직 텔레포트가 발판 윗면에 착지(테스트: `Execute` 직후 destY == 발판 표면 Y, 발판 위 중력 중립 케이스 ±eps). 바닥 파고듦(P07 -1.05) 재현 0.
- [ ] 발판 사거리 밖/없음 → **Position 불변 + `S_SkillCast` 1회 브로드캐스트 동시 Assert**(이펙트 출력 보장 + early-return 회귀 차단).
- [ ] 수평 텔레포트 회귀 0 (거리 5.0 + MapBoundsX clamp 불변). MapBoundsY 제거가 수평 안 깸.
- [ ] WSL2 `dotnet build` 0/0 + `dotnet test` green (수직 신규 테스트 + 회귀 0).
- [ ] Unity 컴파일 0err (메인 MCP reimport+RunCommand probe).
- [ ] 영호 Play: 수직 발판 착지 깔끔 + 도착 이펙트 도착지 글루 1회 + 이동불가 시 이펙트.
- [ ] wire 불변(v14), 헌법 §1(목적지 서버 권위)·§5(쿼리 alloc 0) 보존.

---

## 🧪 테스트

**자동**: `MageTeleportTests` — 지형 수직(발판 위/아래/사거리밖/없음 + 이펙트 항상) + 수평 회귀. EditMode(이펙트 순수 로직 분리 가능 시).
**수동**: 영호 Play — 수직 발판 착지, 도착 이펙트 글루, 이동불가 이펙트.

---

## 📚 학습 포인트

- **고정 거리 vs 지형 인식** — 사이드스크롤 수직 이동은 "허공 N유닛"보다 "다음 발판으로"가 자연. 발판 탐지(공간 쿼리)가 거리 clamp의 물리 얽힘(파고듦)을 근본 해소.
- **타이밍의 진실 = 정보 출처** — 도착 이펙트가 "텔레포트 반영된 스냅"에 발동하려면 그 스냅을 *식별*해야. `S_SkillCast`(서버 권위 신호) 수신 후 첫 스냅 = 도착 스냅. 송신 시점 arming은 옛 스냅에 새는 근원. 원격이 이미 이 모델이라 로컬을 정합.
- **parenting = 위치 추적** — 월드 고정 이펙트는 캐릭터가 보간/snap하면 어긋남. 자식으로 묶으면 transform 공유로 따라감(Dash 패턴). "딱 붙어야"의 구현.

---

## ⚠️ 함정 / 주의사항

- **발판 표면 vs Position.Y** — Position.Y가 발 위치인지 중심인지 확인(spawn ground y=0 = Position.Y=0이면 발 위치). 오프셋 있으면 destY 보정.
- **현재 발판 제외** — up 쿼리 시 현재 서 있는 발판(surfaceY == currentY)은 제외(eps gap). 안 그러면 제자리.
- **MapBoundsY 제거 안전** — grep으로 다른 소비자 0 확인 후 제거(P07 수직 전용이었음). MapBoundsX는 수평이 쓰므로 유지.
- **parenting flip 누수** — facingSign=0이라 `SpawnEffectParented`의 localScale 반전 미발동. 부모 flipX는 sprite-only(transform 미전파) — 도착 이펙트엔 무해.
- **이펙트 항상 출력** — 이동 불가 분기에서 `return` 전에 `S_SkillCast` 브로드캐스트 빠뜨리지 말 것(영호 명시 요구).

---

## ➡️ 다음 Phase

- Phase 10 — 회귀 + 봇 시나리오 갱신 + 마일스톤 마감 (텔레포트 폴리시 포함).
  - ⚠️ **P10 점검**(auditor 🟡③): 이동 불가 분기 신설로 기존 `TeleportSmokeScenario`의 *텔레포트 후 위치 이동 가정*이 깨질 여지 — 봇 시나리오가 사거리 안 발판 있는 위치에서 시전하는지 확인(이동 가정 유지) 또는 이동 무관 검증으로 정합.

---

## 📋 박제 (완료 후)

- 복잡 → `09-...-DONE.md`(또는 마일스톤 `-DONE.md`에 흡수) + work-pin + commit. 마일스톤 종합(P10)에 워크스트림 D 폴리시 포함.

---

## 작업 로그

- 2026-06-14: 생성 (영호 Play-test 2차 피드백 → 지형 인식 수직 + 도착 이펙트 결정론. 승인 플랜 `buzzing-jingling-raccoon.md`).
