---
owner: youngho
milestone: M4.8
title: 원거리 전투 모델 + 최소 스킬 시스템 + 썬더볼트 AoE
status: planned
grade: 대규모
slug: M4.8-ranged-and-skills
created: 2026-06-09
protocol_bump: 10→11
domains: [shared, server, client, qa]
---

# M4.8 — 원거리 전투 + 최소 스킬 시스템 + 썬더볼트 AoE

> 직전 main = M4.7 v10 마감(ec98264, ProtocolVersion 10). 텔레포트는 M4.9로 분리.
> **사용자 확정(세션28)**: ①평타 단일 원거리 + 별도 스킬 키(쿨다운만, 마나 후속) ②썬더볼트 AoE 실구현 ③freeze는 Normal/Golem만(Boss 면역) ④밸런스 임시값(Play 튜닝).
> **썬더볼트 메커니즘(사용자 설명)**: "캐릭터 위치 기준 X,Y 범위에 들어오는지 판정 → 들어온 몬스터 각자 위치에 낙뢰 발생."

---

## Context (왜)

M4.7 시점 Mage는 **구조상 근접**이다. `CombatSystem.GetAttackHitbox`(CombatSystem.cs:140)에 class 분기가 없어 Knight·Mage 둘 다 공격자 주변 ±1.5f AABB로 판정하고, 클라 투사체(`ProjectileVisual`)는 콜라이더 0 순수 비주얼이라 멀리 쏘면 **그림만 나가고 데미지 0**(세션27 Play 발견).

M4.8은 두 갈래를 한 토대 위에 올린다:
1. **Mage 평타 = 진짜 원거리** — 사거리 안 타겟 있으면 서버 확정 투사체 발사 + 도착까지 freeze + 도착 순간 지연 데미지. 사거리 밖이면 캐스팅 스윙만(투사체 X = M4.7 허공 스윙).
2. **최소 스킬 시스템 + 썬더볼트 AoE** — 평타와 분리된 스킬 키로 발동. 공격자 중심 X,Y 박스 안 적들을 즉발 스캔해 각자 위치에 낙뢰 + 광역 지연 데미지.

**핵심 = "맞출지는 쏘는 순간 서버가 즉발 판정(lag-comp), HP는 도착/낙뢰 순간 깎임(지연 적용)".** 평타(단일)·썬더볼트(N개)는 **같은 지연 데미지 큐 + freeze + 박스/단일 공용 스캔**을 공유 — AoE는 단일 모델의 자연 확장.

---

## 설계 결정 (확정)

### 기둥 1 — 서버 확정 후 발사 (클라 예측 스폰 폐지)
M4.7은 로컬 Mage가 C_Attack 직후 투사체를 **선예측 스폰**(MageRangedAttack.cs:22-55) → "그림은 맞았는데 서버 miss=데미지0" 위험. M4.8은 **서버가 명중 즉발 확정 → 통보 패킷 → 클라가 그때 스폰**(로컬/원격 통일). 클라 선예측은 commit window(이동/캐스팅 잠금)만(헌법 #1, M4.7 carry-over "예측 가능/불가능 비대칭").

### 기둥 2 — 지연 데미지 큐 (틱 카운트다운, 헌법 #5)
명중 즉시 `Hp -= damage` 대신 **`DeferredDamageSystem`에 enqueue**(impactTick = currentTick + delayTicks). 매 틱 Process에서 impactTick 도달 시 데미지 + `S_HitResult` + HP≤0 처리. **참조 모델 = `RespawnSystem`(RespawnSystem.cs:21-73)** — 동일 tick 카운트다운, await/Sleep 0, 역방향 순회. 평타(단일)·썬더볼트(AoE) 공용.

### 기둥 3 — freeze = 서버 권위 상태 (Boss 면역)
`EnemyEntity.FrozenUntilTick(long)` 신설. 발사/낙뢰 확정 시 `target.FrozenUntilTick = currentTick + delayTicks`. **`EnemyAISystem.Update`에만 가드** `if(FrozenUntilTick>0){ if(tick>=FrozenUntilTick) FrozenUntilTick=0; else continue; }` → Normal/Golem 이동·AI·latch 스킵. **`BossBehaviorSystem`엔 가드 안 넣음 = Boss freeze 면역**(데미지 지연만, telegraph→attack FSM 유지). position 안 갱신 → 클라 자동 정지.

### 기둥 4 — 평타 사거리 분리 + 즉발 명중 판정
`GetAttackHitbox`에 class 분기 — Mage면 더 긴 사거리(`MageAttackHalfExtent`, 임시값). 발사 순간 AABB 명중이면 travelTicks 계산 + deferred enqueue + freeze + `S_ProjectileLaunch`(발사 연출). miss면 `S_PlayerAttack` 캐스팅 스윙만. rate-limit/rewind는 앞단 유지(스팸 차단=헌법 #3). Knight 근접은 **기존 즉시 데미지 경로 유지**(분기).

### 기둥 5 — 최소 스킬 시스템 + 썬더볼트 AoE
- **스킬 발동**: 평타(C_Attack)와 별개 **스킬 키 → `C_SkillUse{skillId, attackerClientTick}`**. `SkillId`(Thunderbolt=1) 상수 = `98_Shared/GameData`. **쿨다운만 서버 권위**(PlayerEntity 스킬 쿨다운 필드, 마나는 범위 밖).
- **서버 처리**: `SkillSystem`(또는 CombatSystem 확장)이 C_SkillUse 수신 → 쿨다운 검증 → **`ResolveImpactTargets`로 공격자 중심 X,Y 박스(`ThunderboltBoxHalfX/HalfY`, facing 전방 우선) ∩ 적 목록 즉발 확정** → 각 적 DeferredDamage enqueue(impactTick=now+`LightningDelayTicks`) + `FrozenUntilTick`(Normal만) → **`S_SkillCast{casterEntityId, skillId, strikeDelayTicks, facing}`**(캐스팅 연출, **목록 없음**) broadcast.
- **클라**: 스킬 키(임시 Q) → `C_SkillUse` 송신 + 로컬 캐스팅 commit. `S_SkillCast` 수신 → 캐스터 캐스팅 모션. **개별 낙뢰 VFX + 데미지는 적별 `S_HitResult`(도착 시, hitEffect=낙뢰)** — 평타 투사체 도착과 같은 패킷 재사용.
- **AoE 일반화점**: `ResolveImpactTargets(map, origin, shape)` = 단일(대상 1개) / 박스(범위 스캔 N개) 공용 헬퍼. **발동 시점** 스캔(즉발 판정). 미래 다른 AoE는 박스→원형 등 모양만 추가.

### ⚠️ PDL list 미지원 → 적별 S_HitResult 회피 (세션28 실측)
PacketGenerator(`99_Tools/PacketGenerator/`)는 **고정 필드만** 지원 — 가변 길이 list/배열 문법 없음(PDL 전 패킷이 단일 필드, S_Snapshot도 엔티티 1개). 따라서 **S_SkillCast에 타격 적 목록을 담을 수 없다.** 도구를 list 지원으로 확장하는 길도 있으나 헌법 #2(프로토콜 신성) 도구 변경 = 과한 리스크.
→ **회피**: 썬더볼트 타격 적은 발동 시 박스 스캔으로 확정 후 **각 적을 개별 DeferredDamage 큐**에 넣고, 도착 시 **적마다 `S_HitResult` 발사**(평타 투사체 도착과 동일 경로). `S_HitResult` 끝에 **`hitEffect`(byte) append**(0=기본/근접, 1=투사체 도착, 2=낙뢰)로 클라 VFX 분기. **list 불필요 + 도구 확장 불필요 + 평타·썬더볼트 도착 처리 공유.**

### 프로토콜 모양 (PDL.xml append-only, 현재 최대 22=S_PlayerAttack)
```xml
<packet name="S_ProjectileLaunch">  <!-- 23: 평타 단일 호밍 투사체 발사 연출 -->
  <int  name="attackerEntityId"/>
  <int  name="targetEntityId"/>     <!-- 도착 대상(호밍) -->
  <byte name="projectileType"/>     <!-- 0=Mage 평타. 미래 분기 -->
  <int  name="travelTicks"/>        <!-- 발사~도착 틱(거리 비례) -->
</packet>
<packet name="C_SkillUse">          <!-- 24: 스킬 발동 요청(클라→서버) -->
  <byte name="skillId"/>            <!-- 1=Thunderbolt -->
  <int  name="attackerClientTick"/> <!-- rewind 기준 -->
</packet>
<packet name="S_SkillCast">         <!-- 25: 스킬 캐스팅 연출(서버→클라, 목록 없음) -->
  <int  name="casterEntityId"/>
  <byte name="skillId"/>
  <int  name="strikeDelayTicks"/>   <!-- 발동~낙뢰 틱(freeze 지속) -->
  <byte name="facing"/>
</packet>
```
- **S_HitResult 변경**: 끝에 `<byte name="hitEffect"/>` append(모양 변경 1건, append-only). **C_Attack(11)·S_PlayerAttack(22) 불변.** S_PlayerAttack은 평타 캐스팅/근접 스윙 연출 전용.
- `ProtocolVersion.cs` `Current = 10 → 11` + 이력 주석. 옛 빌드 핸드셰이크 거절.

### 밸런스 임시 상수 (CombatConstants.cs + GameData, 서버 권위 — Play 튜닝)
- 평타: `MageAttackHalfExtent`≈4.0f / `ProjectileSpeedPerTick`≈2u/tick / travelTicks=clamp(round(dist/speed), `MinTravelTicks=2`, `MaxTravelTicks=10`).
- 썬더볼트: `ThunderboltBoxHalfX`≈6.0f / `ThunderboltBoxHalfY`≈3.0f(전방 우선) / `LightningDelayTicks`≈4(200ms = freeze 지속) / `ThunderboltCooldownTicks`≈40(2s) / 데미지=단일 히트(다단 X).
- ⚠️ 전부 임시 시작값. P5 클라 연결 후 2클라 Play로 튜닝.

---

## Phase 분해 (6개)

| # | Phase | 등급 | 도메인 | 의존 | 완료 조건(정량) |
|---|---|---|---|---|---|
| P1 | 프로토콜 — S_ProjectileLaunch(23)+C_SkillUse(24)+S_SkillCast(25)+S_HitResult hitEffect append+SkillId, ProtocolVersion 11, Shared.dll | 보통 | shared | — | build 통과 · enum 23~25 시프트0 · PDL+GenPackets+Shared.dll 동반 commit · Current==11 · PacketRoundTrip 신규 4건 |
| P2 | 서버 인프라 — DeferredDamageSystem + FrozenUntilTick + EnemyAISystem freeze 가드(Boss 면역) + GameMap.Tick 끼움 | 복잡 | server | P1 | dotnet test: impactTick 도달 시 데미지+HP≤0 · frozen 적 이동0 · Boss freeze 무시 |
| P3 | 서버 평타 원거리 — GetAttackHitbox Mage 사거리 + ProcessAttack Mage(travelTicks+deferred+freeze+S_ProjectileLaunch) + ResolveImpactTargets(단일) + 평타 상수 | 복잡 | server | P2 | dotnet test(Mage 명중=S_ProjectileLaunch+deferred+freeze·도착틱 데미지·hitEffect=1 / 사거리밖=스윙만 / Knight=즉시 / rate-limit) |
| P4 | 서버 스킬+썬더볼트 AoE — C_SkillUse 핸들러+쿨다운 + ResolveImpactTargets(박스 스캔) + deferred N개+freeze(Normal만) + S_SkillCast + 썬더볼트 상수 | 복잡 | server | P2,P3 | dotnet test(박스 ∩ 적 목록 정확 · 각 적 낙뢰딜레이 후 데미지·hitEffect=2 · Boss 데미지O freezeX · 쿨다운 차단 · 빈 박스=캐스팅만) |
| P5 | 클라 서버확정 투사체+스킬 — 선예측 제거 + ProjectileLaunchHandler + 스킬키 C_SkillUse + SkillCastHandler(캐스팅) + HitResultHandler hitEffect VFX 분기 + freeze 시각 | 복잡 | client | P1,P3,P4 | Unity 컴파일 0err · 2클라 평타 투사체 1발(중복0)·도착≈서버틱·적 정지·HP감소 · 썬더볼트 박스 적들 낙뢰+HP감소 · 사거리밖 스윙만 |
| P6 | 회귀+마감 — 봇 4종 + xUnit 회귀 + 2클라 매트릭스 + 마감 박제 + PR | 보통 | qa | P2~P5 | 신규 봇 green · 기존 회귀0 · ProtocolVersion 11 assert · _milestone-DONE.md+.html 5단계 보고 + CHANGELOG[M] + PR(사용자 GO) |

**의존성**:
```
P1(shared) → P2(인프라) → P3(평타) ─┐
                       P2 → P4(스킬+AoE) ─┴→ P5(client) → P6(qa 마감)
                                P1 ─────────┘
```
**권장 머지**: ProtocolVersion 11 비가역 + 클라-서버 동시 정합 → **PR1 = P1~P5(v11 정합 단위)**, **PR2 = P6(마감)**. Worker 위임은 도메인별 6 Phase 유지(검수 입자). 98_Shared 변경 → Shared.dll → 03_Client CODEOWNERS(정유현) co-review → admin 머지 예상.

---

## 검증

1. **dotnet test**(`GameServer.Tests/`): DeferredDamageSystem(impactTick 데미지+HP≤0) · freeze(이동0, Boss 면역) · 평타 ProcessAttack 분기 · 썬더볼트 박스 스캔(∩ 적 목록·Boss 데미지O freezeX·쿨다운) · PacketRoundTrip(S_ProjectileLaunch/C_SkillUse/S_SkillCast/S_HitResult hitEffect).
2. **헤드리스 봇**(Scenarios/): RangedHitSmoke(평타 명중→S_ProjectileLaunch+travelTicks 후 S_HitResult(hitEffect=1)+HP감소) / FreezeSmoke(발사 후 적 정지·Boss 면역) / ThunderboltAoeSmoke(박스 내 다수 적 C_SkillUse→S_SkillCast·각 S_HitResult(hitEffect=2)·Boss 멈춤X) / RangedWhiffSmoke(사거리밖→스윙만·데미지0). 기존 봇 회귀0(전투/보스 봇 fresh 서버 단독). C_Attack/C_SkillUse는 attackerClientTick=최신 S_Snapshot.serverTick.
3. **2클라 Play(수동)**: 평타 사거리 안=투사체·적 정지·도착 HP감소 / 밖=캐스팅만 / 썬더볼트=박스 내 적들 낙뢰+HP감소·Normal 정지·Boss 안 멈춤 / 쿨다운 중 재발동X / 로컬 중복0 / 원격 상호 관측.

---

## 리스크 / 범위 밖

- **ProtocolVersion 11 비가역**: PDL append-only 23~25 *맨 아래만* + S_HitResult 끝 hitEffect. 재생성 후 enum 확인(시프트=전체 회귀).
- **PDL list 미지원**: 위 §기둥5 — 적별 S_HitResult로 회피(도구 확장 불필요).
- **trust-boundary**: 명중·travelTicks·박스 스캔·freeze·데미지·쿨다운 전부 서버(클라=skillId+attackerClientTick 힌트뿐). rate-limit/쿨다운/rewind 앞단 유지. 클라는 freeze/HP 표시만(force-adopt).
- **도착 전 타겟 사망/디스폰**: Process 도착 시 IsDead/존재 재확인 skip. respawn entityId 변경 → stale 무효(은퇴 ID 재사용 금지).
- **밸런스 임시값**: 박스 X,Y·투사체 속도·낙뢰 딜레이·쿨다운 = P5 후 Play 튜닝.
- **범위 밖 명시**: 텔레포트(M4.9) / 마나·MP(쿨다운만) / 다른 AoE 스킬(ResolveImpactTargets 모양 추가만) / 다단 히트 / 원격 머리 위 HP바 / frozen "얼음" 이펙트(position 정지만) / Knight 원거리화 / 스킬 키 리바인딩 UI / 투사체 중간 차폐.

---

## 미해결 — 구현 중 결정 (디폴트 + Play 확정)
- 썬더볼트 박스 = **facing 전방 우선** 디폴트(전방 ThunderboltBoxHalfX, 후방 절반?) vs 자기중심 — Play로 확정.
- 낙뢰 타이밍 = 고정 `LightningDelayTicks`(짧은 딜레이) 디폴트. 평타 travelTicks(거리비례)와 별 상수.
- 스킬 키 = 임시(Q). 정식 UI는 범위 밖.

---

## 승인 후 절차
1. Phase def 6개 작성 → **plan-auditor 자동 호출**(Tier 2-B).
2. 통과 후 P1(shared)부터 Worker 위임 → reviewer → 메인 WSL2 직접 실측(Worker 보고 불신) → commit.
3. PR1(P1~P5 v11) + PR2(P6 마감) — 각 **사용자 명시 GO**, admin 머지(사유 풀어쓰기).
4. 마감 시 `_milestone-DONE.md`+`.html` 5단계 보고(대규모) + CHANGELOG[M].
