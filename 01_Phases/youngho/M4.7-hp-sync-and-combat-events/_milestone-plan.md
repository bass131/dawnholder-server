---
owner: youngho
milestone: M4.7
title: v10 구조급 — 플레이어 HP 동기화 + 공격 모델 정비(허공 스윙 + 원격 이벤트)
status: planned
grade: 대규모
risk: trust-boundary, irreversible(ProtocolVersion bump)
estimated: 12~18h (총합, 6 Phase)
domain: shared+server+client+qa
---

# M4.7 — v10 구조급 (HP 동기화 + 공격 모델 정비)

> **상태**: planned — 2026-06-09 세션27, M4.6 ActionState FSM 완전 마감(PR #82, main `a6c3086`) 직후.
> **선행**: M4.6 완전 마감. **설계 근거**: 승인된 plan(plan-mode `delegated-prancing-gray.md`) — Explore 2건(HP 동기화 / 공격 이벤트) + Explore 1건(공격 스윙 게이팅) + Plan 에이전트 설계 + 사용자 결정 3건(방향 v10 / HP=전용 이벤트 / 허공 스윙 동승).

---

## 🎯 마일스톤 목표

세 구조급 결함을 한 묶음으로 봉합한다 — 셋 다 ProtocolVersion bump(9→10) 또는 같은 전투 코드를 건드려 함께 처리하는 게 깨끗하다.

1. **플레이어 HP 권위 동기화** — 주기 `S_Snapshot`(ID 6)에 HP가 없어 사망/부활/회복 HP를 클라가 `PlayerStats.MaxHp` 표시 미러(M4.5 임시 봉합)로 때우는 것을, 전용 `S_PlayerHp` 이벤트로 교체.
2. **원격 공격 이벤트** — `S_PlayerAttack` 신설로 원격 플레이어 공격(Mage 투사체 + 근접 스윙)을 다른 클라가 보게 함(현재 `animState=Attack` 지속상태 미러만 와서 투사체 표시 불가).
3. **공격 스윙 ↔ 명중판정 분리** — 사용자 피드백: 적에 충분히 근접하지 않으면 공격 모션이 안 나옴. 4겹 직렬 게이트가 전부 타겟 존재에 묶임 → 스윙(연출)을 명중(데미지)에서 떼어 허공 스윙 허용(데미지는 서버 AABB 별도 판정).

**설계 결정 (확정)**:
- **HP = 전용 `S_PlayerHp` 이벤트(on-change)**: 고빈도 snapshot 비대화 회피(헌법 #5), 위치 reconcile과 분리, TCP 신뢰전송이라 on-change 손실 약점 무효, `entityId` 필드로 미래 파티/원격 HP 바 확장 개방. broadcast = 본인에게만(미래 승격 시 패킷 모양 불변 → 추가 bump 불필요).
- **공격 = 스윙/명중 분리**: 데미지 모델(단일 타겟 AABB)은 유지, *연출/이벤트*만 명중에서 분리. 서버는 rate-limit/rewind 통과한 유효 시도 시 `EnterAttackState` + `S_PlayerAttack` broadcast, AABB는 데미지만 게이트. "연출만 분리, 권위 판정 불변".
- **ProtocolVersion 9→10 단일 bump**: `S_PlayerHp`(21) + `S_PlayerAttack`(22) append. `C_Attack`(ID 11)은 모양 불변(targetEntityId 의미만 "필수 타겟"→"선택 힌트 0=없음").
- **유지할 경계**: AnimState(시각) ↔ EnemyState(AI) 분리 유지. 권위 판정(HP/데미지/AABB)은 전부 서버(헌법 #1).

---

## 📋 Phase 분해 (6개)

| # | Phase | 등급 | 도메인 | 예상 | risk |
|---|---|---|---|---|---|
| 01 | 프로토콜 신설 — S_PlayerHp(21)+S_PlayerAttack(22) append, PacketGenerator 재생성, ProtocolVersion 10, Shared.dll | 보통 | shared | 1~2h | **irreversible**(bump) |
| 02 | 서버 HP 송신 — GameMap.SendPlayerHp + 3트리거(진입/피격/부활) | 보통 | server | 2~3h | — |
| 03 | 서버 공격 모델 정비 — 스윙↔명중 분리 + S_PlayerAttack broadcast | 복잡 | server | 3~4h | **trust-boundary** |
| 04 | 클라 HP 신뢰 경로 — PlayerHpHandler + 표시 미러 제거 | 보통 | client | 2~3h | — |
| 05 | 클라 공격 입력+예측+원격 연출 — 항상 스윙 + 로컬 Attack 예측 + 원격 투사체/스윙 | 복잡 | client | 3~4h | unity-asset(VFX 시) |
| 06 | 회귀 + 마감 — 봇 신규 + xUnit 회귀 + 2클라 매트릭스 + PR + 5단계 보고 | 보통 | qa | 1~2h | irreversible(PR) |

**총 등급 = 대규모** (shared+server+client+qa 4도메인 관통 + ProtocolVersion bump = irreversible + 전투 신뢰 경계 trust-boundary).

---

## 🔗 의존성 그래프

```
01 (shared 프로토콜)
   ├──────────────────────┬───────────────────────┐
   ↓                      ↓                        
02 (server HP)          03 (server 공격모델)        [trust-boundary]
   ↓                      ↓
04 (client HP)          05 (client 공격)            ※ HP갈래(02→04) ↔ 공격갈래(03→05) 병렬 가능
   └──────────────────────┴───────────────────────┐
                                                   ↓
                                                 06 (qa 회귀+마감)  ← 02·03·04·05 모두 필요
```

**병렬 가능**: HP갈래(02→04) ↔ 공격갈래(03→05) — 01 완료 후 도메인/파일이 갈려 충돌 0. HP갈래는 `GameMap.SendPlayerHp`/`PlayerHpHandler`, 공격갈래는 `CombatSystem`/`PlayerAttackHandler`로 파일 분리. 단 학습 호흡상 직렬 권장(01 → HP갈래 완결 → 공격갈래 → 06). 사이클 없음(DAG).

**권장 머지 순서**: 01 → (02→04) HP갈래 완결 → (03→05) 공격갈래 → 06. 갈래별 중간 상태도 헌법 정합(half-shipped 안전 — 이전 마일스톤 수직 슬라이스 패턴).

---

## ✅ 마일스톤 완료 조건

- [ ] `S_PlayerHp`(21) + `S_PlayerAttack`(22) 신설 — PDL append-only, GenPackets enum 21/22(ID 시프트 0), `ProtocolVersion.Current == 10`
- [ ] 플레이어 HP가 **전용 이벤트로 권위 동기화** — 진입/피격/부활 시 서버가 currentHp+maxHp 송신, 클라 표시 미러(PlayerStats.MaxHp 추론) 은퇴
- [ ] 사망→부활 시 HUD가 **서버 권위 full HP** 표시 (M4.5 "HUD 0 고착" 근본 봉합, 표시 미러 콜백 제거)
- [ ] **허공 스윙 허용** — 타겟 없거나 사거리 밖이어도 공격 입력 시 스윙 모션 재생(로컬 즉시 + 원격 관측). 데미지는 서버 AABB 명중 시만(단일 타겟 유지)
- [ ] **원격 공격 이벤트** — `S_PlayerAttack`로 원격 Mage 투사체 + 근접 스윙 연출, attacker 제외 broadcast, 로컬 중복 0
- [ ] 공격 모델 정비 후 **rate-limit/rewind/AABB 데미지 게이트 불변** — 스윙 스팸·조작 차단 유지(헌법 #3), 데미지 판정은 그대로 서버 권위
- [ ] 로컬 Attack 예측 reconcile **rubber-band 0** (공격 시 위치 튐 없음)
- [ ] `dotnet test` green + 봇 전 시나리오 PASS + 신규 봇(HpSyncSmoke/RemoteAttackSmoke/WhiffSwingSmoke) green
- [ ] 2클라 Play — 허공 스윙 양방향 관측 + 원격 투사체 + HP 사망/부활 동기화 (직업 2종)
- [ ] CHANGELOG + PR 머지(사용자 GO) + 5단계 보고 MD/HTML

---

## 🚫 이번에 명시적으로 뺀 것

- **근접 AABB 스윕(AoE)** — 이번은 단일 타겟 데미지 유지, 스윙 *연출*만 분리. 범위 다수 타격은 별도
- **원격 머리 위 HP 바** — `S_PlayerHp.entityId`로 미래 개방했으나 이번 X(본인 HUD만)
- **회복 아이템·스킬** — HP 송신 헬퍼는 재사용 가능하나 트리거 소스(회복 로직)가 아직 없음
- **`S_EnemyAttack.targetCurrentHp` deprecated/필드 제거** — HP 권위는 S_PlayerHp로 일원화하되 S_EnemyAttack은 이펙트/연출 책임 유지. 필드 정리는 별도 bump
- **attackType 스킬별 세분화** — byte 여유로 미래 확장 가능, 지금은 0=Melee/1=Ranged만

---

## ➡️ 다음 마일스톤

- **외관/연출 디테일**(배경/컷신/NPC) 또는 **M5 Persistence**(LocalDB Linux 결정 + GenPackets Write 풀링 + Serilog/DI) — 사용자 가닥
- M4.6 이월 흡수: `ActionFsm↔Fsm` 네이밍 통일 / harness REVIEW_CHECKLIST.md 봉합

---

## 갱신 이력

- 2026-06-09 — 신설 (세션27, M4.6 마감 직후). 승인된 plan-mode 설계 정식화. v10 bump = 본 마일스톤 유일 목표. 사용자 결정: 방향 v10 / HP=전용 이벤트 / 허공 스윙 동승.
