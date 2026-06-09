---
owner: youngho
milestone: M4.8
phase: milestone-closeout
title: 원거리 전투 모델 + 최소 스킬 시스템 + 썬더볼트 AoE — 서버 확정 투사체 + 지연 데미지 + freeze
status: done
completed: 2026-06-09
grade: 대규모
summary: M4.8 완전 마감 (6 Phase, 단일 브랜치 feature/m4.8-ranged-and-skills, ProtocolVersion 10→11). Mage를 "구조상 근접"(GetAttackHitbox에 class 분기 없음, 투사체 콜라이더 0)에서 진짜 원거리로 — ① 평타 = 서버가 명중을 즉발 확정(lag-comp)한 뒤 S_ProjectileLaunch로 발사 통보(클라 선예측 스폰 폐지), 도착 틱에 지연 데미지 + 도착 후 freeze, ② 최소 스킬 시스템(C_SkillUse, 쿨다운만 서버 권위) + 썬더볼트 AoE(공격자 중심 X,Y 박스 스캔 → 박스 내 적 각자 위치 낙뢰 + 광역 지연 데미지). 핵심 토대 = DeferredDamageSystem(RespawnSystem의 틱 카운트다운 패턴 재사용, 헌법 #5 논블로킹) + freeze(EnemyEntity.FrozenUntilTick + EnemyAISystem 가드, Boss 면역) + ResolveImpactTargets(단일/박스 공용 = AoE-ready). PDL이 가변 길이 list 미지원이라 썬더볼트 타격은 S_SkillCast에 목록을 싣는 대신 적별 개별 S_HitResult(hitEffect byte append=낙뢰)로 회피 — 평타·썬더볼트가 도착 처리 S_HitResult를 공유. freeze는 영호 결정으로 "도착까지"에서 "도착 후 StunTicks(8틱=400ms) 더"로 강화 = 진짜 stun-lock. 최종 회귀 = 클린빌드 0/0 + dotnet test 508/0/4skip + 신규 봇 4종 PASS + 기존 봇(EmergencyCombat/BossFight) v11 회귀 0 + Unity 컴파일 0err + reviewer P2~P5 전부 🔴0. 5단계 보고 시각판 = _milestone-DONE.html.
---

# M4.8 — 원거리 전투 모델 + 스킬 시스템 마일스톤 박제

**마감 일자**: 2026-06-09 (세션28)
**Phase 수**: 6/6 완료 (P1 프로토콜 / P2 인프라 / P3 평타 / P4 스킬+썬더볼트 / P5 클라 / P6 회귀+마감)
**등급**: 대규모 (shared 프로토콜 bump = irreversible + server 신뢰 경계(쿨다운·박스·freeze) + 스킬 시스템 신규 + client + qa = 4도메인 관통)
**WORK-ID**: m4.8-ranged-and-skills
**시각 보고서**: [`_milestone-DONE.html`](_milestone-DONE.html) — 대규모 5단계 보고 HTML 박제

---

## 5단계 보고

- 🎯 **무엇을 만들었나** — Mage를 진짜 원거리 직업으로 만들고 첫 스킬을 붙였다. ① **평타 원거리** — 서버가 명중을 즉발 확정(lag-comp)한 뒤 `S_ProjectileLaunch`(PacketID 23)로 투사체 발사를 통보하고, 클라는 그때 스폰한다(선예측 스폰 폐지). 데미지는 투사체 도착 틱에 지연 적용되고, 그 동안 + 추가 `StunTicks`만큼 적이 freeze된다. ② **최소 스킬 시스템 + 썬더볼트 AoE** — 평타(`C_Attack`)와 분리된 스킬 키 → `C_SkillUse`(24, 쿨다운만 서버 권위) → 공격자 중심 X,Y 박스를 스캔해 박스 내 적들 각자 위치에 낙뢰 + 광역 지연 데미지, `S_SkillCast`(25)로 캐스팅 연출 통보. ProtocolVersion 10→11(+ `S_HitResult`에 `hitEffect` byte append).
- 🤔 **왜 필요한가** — M4.7 시점 Mage는 **구조상 근접**이었다. 서버 `GetAttackHitbox`에 class 분기가 없어 Knight·Mage 둘 다 공격자 주변 ±1.5f AABB로 판정했고, 클라 투사체는 콜라이더 0의 순수 비주얼이라 멀리 쏘면 **그림만 나가고 데미지가 0**이었다(세션27 Play 발견). 또 평타 1종뿐이라 스킬을 붙일 발동 채널(평타와 구분된 입력)이 없었다. 추후 썬더볼트 외 광역기를 붙이려면 "도착 시 범위 내 N개 타겟 판정"으로 일반화된 토대가 처음부터 필요했다 — 단일 타겟 하드코딩은 광역기 붙일 때 다 뜯어야 한다.
- 🛠️ **어떻게 만들었나** — 토대 우선 수직 슬라이스: `P1` 프로토콜(패킷 3 + hitEffect) → `P2` **서버 인프라**(`DeferredDamageSystem` = "N틱 뒤 데미지"를 `RespawnSystem`의 틱 카운트다운 패턴으로 복제 = 헌법 #5 논블로킹 / `FrozenUntilTick` + `EnemyAISystem` freeze 가드, Boss는 가드 없어 면역) → `P3` 평타(`ResolveImpactTargets` 헬퍼 신설 + Mage 분기로 deferred 전환) → `P4` 스킬+썬더볼트(`C_SkillUse` 핸들러 + 쿨다운 tick 기반 + `ResolveImpactTargets` 박스 스캔 = P3 헬퍼 첫 호출) → `P5` 클라(선예측 스폰 폐지 + `ProjectileLaunchHandler`/`SkillCastHandler` + hitEffect VFX 분기) → `P6` 회귀+마감. **핵심 일반화** = `ResolveImpactTargets(origin, halfExtents)`가 평타(단일 대상)와 썬더볼트(박스 N개)를 한 헬퍼로 — AoE는 단일 모델의 자연 확장. **PDL list 미지원**(전 패킷 고정 필드)을 만나 S_SkillCast에 적 목록을 못 싣자, 썬더볼트 타격을 적별 개별 `S_HitResult`(hitEffect=2=낙뢰)로 회피 = 도구 확장 없이 평타·썬더볼트가 도착 처리를 공유.
- 🧪 **테스트 결과** — 최종 회귀(세션28, WSL2 = ADR-029, 메인 직접 실측): 클린빌드 **0 경고/0 오류** + `dotnet test` **508 통과/0 실패/4 skip** + 신규 헤드리스 봇 **4종 전부 PASS**(RangedHitSmoke = 평타 명중→S_ProjectileLaunch+travelTicks 후 S_HitResult hitEffect=1·HP 감소 / RangedWhiffSmoke = 허공 평타 투사체·데미지 0 / ThunderboltAoeSmoke = C_SkillUse→S_SkillCast+박스 적 S_HitResult hitEffect=2·Boss 데미지 / FreezeSmoke = 평타 후 Normal 적 freeze 정지/재개) + 기존 봇(EmergencyCombat hits=2·death·hp 30→-20 / BossFight hits=5·stageClear·boss 100→-10) **v11 회귀 0** + `ProtocolVersion == 11`(enum 시프트 0: 신규 23/24/25, 기존 ≤22 불변) + Unity 클라 컴파일 0err(MCP RunCommand `isCompiling=False` + ReadConsole 0 error) + **reviewer P2~P5 전부 🔴0**. Boss freeze 면역·박스 다수 타격은 보스 FSM Idle dwell·환경 의존이라 dotnet(`Boss_ApplyFreeze_BossBehaviorSystemContinues`·`BoxScan_TargetsInBox_AllEnqueued`)이 결정적 검증, 봇은 통합 흐름만. LagSim integration은 전체 동시 실행 시 1개 flaky(매번 다른 테스트, 단독 green = 코드 무관).
- ➡️ **다음 스텝** — ① **AoE 실 광역기 확장**: `ResolveImpactTargets`가 단일/박스 공용으로 깔렸으므로, 썬더볼트 외 스킬(원형 장판 등)은 모양만 추가하면 된다. ② **텔레포트(블링크)**: M4.8 범위에서 분리(이동 권위/prediction 갈래) = **M4.9**. ③ **밸런스 Play 튜닝**: MageAttackHalfExtent(4.0f)·ProjectileSpeedPerTick(2.0f)·StunTicks(8)·ThunderboltBoxHalfX/Y(6/3)·ThunderboltCooldownTicks(40) 전부 임시값. ④ 백로그(reviewer 🟡, 비차단): 평타↔스킬 클라 쿨다운 게이트 공유(입력 유실 Play 측정 후 분리)·freeze 가드 표현 통일·rewind 매직넘버(4) 상수화·빈 박스 쿨다운 소비 단언·S_SkillCast strikeDelayTicks/projectileType 미사용(연출 단계 hook).

---

## TL;DR (🎯 무엇 / 🤔 왜)

M4.8은 **Mage를 구조상 근접에서 진짜 원거리로** 바꾸고, **평타와 분리된 첫 스킬(썬더볼트 AoE)**을 붙인 v11 마일스톤이다.

**서버 확정 후 발사**: M4.7은 로컬 Mage가 C_Attack 직후 투사체를 선예측 스폰해 "그림은 맞았는데 서버 miss = 데미지 0" 위험이 있었다. 이제 서버가 명중을 즉발 확정한 뒤 `S_ProjectileLaunch`로 통보하고 클라가 그때 스폰한다(로컬/원격 단일 경로 = 중복 0). 클라 선예측은 commit window(이동/캐스팅 잠금)만 유지 — "예측 가능/불가능 비대칭"(자기 입력 시점=예측 / 명중·데미지·freeze=서버 신뢰)의 M4.7 정신 연장.

**지연 데미지 큐 + freeze**: 명중 즉시 HP를 깎는 대신 `DeferredDamageSystem`에 enqueue(impactTick = currentTick + travelTicks)하고, 매 틱 카운트다운으로 도착 틱에 데미지 + `S_HitResult`를 보낸다 — `RespawnSystem`과 같은 패턴이라 `await`/`Sleep` 0(헌법 #5). freeze는 `EnemyEntity.FrozenUntilTick` + `EnemyAISystem` 가드로 서버 권위 정지(Boss는 가드 없어 면역). 영호 결정으로 freeze를 "도착까지"에서 "도착 + StunTicks(8틱)"로 강화 = 진짜 stun-lock.

**AoE-ready 일반화 + PDL list 회피**: `ResolveImpactTargets(origin, halfExtents)`가 평타(단일)와 썬더볼트(박스 N개)를 한 헬퍼로 처리 — 광역기는 단일 모델의 확장. PDL이 가변 길이 list를 지원하지 않아(전 패킷 고정 필드) S_SkillCast에 적 목록을 못 싣자, 썬더볼트 타격을 적별 `S_HitResult`(hitEffect=2)로 회피 = 도구 확장 0 + 평타·썬더볼트가 도착 처리를 공유.

**프로토콜 규율**: ProtocolVersion 10→11(영구). PDL append-only로 `S_ProjectileLaunch`(23)·`C_SkillUse`(24)·`S_SkillCast`(25)를 *맨 아래만* 박고, `S_HitResult`엔 `hitEffect` byte를 *끝에* append(기존 5필드 오프셋 불변) = enum 시프트 0.

---

## Phase 박제 요약

| Phase | 제목 | 핵심 | commit |
|---|---|---|---|
| P1 | 프로토콜 신설 | PDL append-only로 S_ProjectileLaunch(23)+C_SkillUse(24)+S_SkillCast(25) + S_HitResult.hitEffect append, **ProtocolVersion 10→11**, SkillId enum, Shared.dll. enum 시프트 0 확인 [irreversible] | 0d9ae77 |
| P2 | 서버 인프라 | `DeferredDamageSystem`(RespawnSystem 틱 카운트다운 패턴, await/Sleep 0) + `EnemyEntity.FrozenUntilTick`/`ApplyFreeze`(max 중첩) + `EnemyAISystem` freeze 가드(Boss 면역) + GameMap.Tick 끼움. DeferredImpact struct에 AttackerEntityId 포함 [trust-boundary] | 44f8383 |
| P3 | 서버 평타 원거리 | `GetAttackHitbox`에 Mage 사거리 분기 + ProcessAttack Mage(travelTicks+deferred+freeze+S_ProjectileLaunch) + `ResolveImpactTargets`(단일, AoE 진입점) + **aggro 봉합**(deferred 도착 시점 TargetEntityId=attacker, reviewer 🔴). Knight 즉시 경로 유지 | 8ade57e |
| P4 ★ | 서버 스킬+썬더볼트 AoE | `C_SkillUseHandler`(skillId 검증+auth) + `SkillSystem.ProcessThunderbolt`(쿨다운 tick 기반+박스 스캔 ResolveImpactTargets+deferred N개+freeze Normal만+S_SkillCast). 쿨다운 초기값 -(쿨다운+1)로 long.MinValue 오버플로우 회피 | 96f12a4 |
| P5 ★ | 클라 서버확정 투사체+스킬 | 선예측 스폰 폐지 + `ProjectileLaunchHandler`(로컬/원격 단일) + 스킬키 C_SkillUse + `SkillCastHandler` + HitResultHandler hitEffect 분기(0근접/1투사체/2낙뢰). Unity 0err. 새 에셋 0건(VFX=placeholder warn-once) | d770e6a |
| P6 | 회귀 + 마감 | 신규 봇 4종 + 기존 봇 v11 회귀 + **freeze 강화**(도착 후 StunTicks, 영호 결정) + 본 박제 + PR | d854a8d + 본 마감 |

**Phase 06 포함분 (세션28)**:
- **신규 스모크 봇 4종 작성 + 전수 PASS** — RangedHitSmoke / RangedWhiffSmoke / ThunderboltAoeSmoke / FreezeSmoke. 봇 측정 학습 2건: Boss freeze 면역은 보스 FSM Idle dwell(공격 쿨다운 정지)로 position 관측이 비결정적 → dotnet 위임(봇은 데미지만), Normal freeze는 측정 윈도우를 freeze 만료 *전*(travelTicks+4틱)에 끝내 freeze 후 이동이 안 섞이게 안정화.
- **freeze 강화** — 영호 결정으로 freeze를 "도착까지"에서 "도착 + StunTicks(8틱=400ms)"로 = 진짜 stun-lock. CombatSystem(평타)/SkillSystem(썬더볼트) ApplyFreeze에 + StunTicks. 부수 효과로 freeze 윈도우가 길어져 봇 측정도 안정화.
- **마일스톤 전체 회귀 입증** — 클린빌드 0/0 + dotnet test 508/0/4skip + 기존 봇(EmergencyCombat·BossFight) v11 회귀 0. 보스/전투 봇은 fresh 서버 단독(교차오염 회피).

---

## 결정 흐름 (🛠️ 어떻게 — 회고 참고용)

1. **서버 확정 후 발사 (클라 선예측 스폰 폐지)** — M4.7의 로컬 즉시 스폰은 "그림은 맞았는데 서버 miss = 데미지 0" 위험. 서버가 명중을 즉발 확정한 뒤 `S_ProjectileLaunch`로 통보 → 클라가 그때 스폰(로컬/원격 단일 경로). 클라 선예측은 commit window만 유지(헌법 #1). M4.7 carry-over "예측 가능/불가능 비대칭"의 연장.
2. **지연 데미지 = RespawnSystem 패턴 재사용** — "N틱 뒤 데미지 적용"을 await/Task.Delay로 짜면 헌법 #5(틱 루프 논블로킹) 위반. 이미 `RespawnSystem`이 쓰던 틱 카운트다운(List + RemainingTicks-- + 역방향 순회)을 그대로 복제 = `DeferredDamageSystem`. 도착 시 id로 재조회 + null/IsDead 재확인(late binding) = 도착 전 적 사망/respawn(새 id)이면 stale impact가 자동 무효.
3. **freeze = 서버 권위 + Boss 면역** — `EnemyEntity.FrozenUntilTick`(tick thread invariant) + `EnemyAISystem.Update` 진입부 가드(`tick<until이면 continue` = Fsm.Tick·latch 스킵). Boss는 `if Boss continue`가 가드보다 앞이고 `BossBehaviorSystem`엔 가드가 없어 면역 — "Boss는 데미지 지연만, telegraph FSM은 계속". `ApplyFreeze`는 max(기존,신규)로 평타+썬더볼트 중첩 시 조기 해제 방지.
4. **PDL list 미지원 → 적별 S_HitResult 회피** — PacketGenerator는 고정 필드만 지원(전 패킷이 단일 필드, S_Snapshot도 엔티티 1개). 도구를 list 지원으로 확장하는 건 헌법 #2(프로토콜 신성) 도구 변경 = 과한 리스크. 썬더볼트 타격을 발동 시 박스 스캔으로 확정 후 각 적을 개별 DeferredImpact에 넣고, 도착 시 적마다 `S_HitResult`(hitEffect=2)를 보낸다 — `S_HitResult` 끝에 hitEffect byte 1개만 append(오프셋 불변)하면 평타 투사체 도착(1)·낙뢰(2)·근접(0)을 한 패킷으로 구분. 도구 확장 0 + 평타·썬더볼트가 도착 처리를 공유.
5. **최소 스킬 시스템 = 쿨다운만 서버 권위** — 평타(C_Attack)와 분리된 `C_SkillUse`(skillId+attackerClientTick). 핸들러가 skillId 범위 검증(Thunderbolt만)+handshake auth, caster entityId는 session._entityId에서 강제(도용 차단=헌법 #3). 쿨다운은 tick 기반(map.CurrentTick)으로 헌법 #5 정합(ms 시계 의존 회피), 초기값 -(쿨다운+1)로 long.MinValue 오버플로우 회피. 마나는 범위 밖(쿨다운만).
6. **freeze 강화 (도착 후 StunTicks)** — 영호 결정(2026-06-09): "도착까지만" freeze는 투사체/낙뢰 도착 즉시 풀려 stun이 거의 없고(연사 lock 불가) 봇 측정도 어려웠다. 데미지는 도착 시 그대로, freeze만 도착 + StunTicks(8틱=400ms) 더 = 진짜 stun-lock(메이플식 얼림). 부수 효과로 freeze 윈도우가 길어져 봇 측정도 안정화. 전부 임시값(Play 튜닝).
7. **봇 검증 = 통합 흐름, 결정적 검증은 dotnet 위임** — Boss freeze 면역(이동 계속)은 보스 FSM Idle dwell 때문에 봇 position 관측이 비결정적(Idle이면 freeze 아니어도 delta=0). 박스 다수 타격도 적 배치 의존. 이런 결정적 판정은 dotnet 테스트가 격리 검증하고, 봇은 "C_SkillUse→S_SkillCast→S_HitResult hitEffect=2" 같은 통합 흐름만 확인. "봇으로 뭘 측정하지 말지"의 경계.

---

## AC 검증 결과

마일스톤 완료 조건 대조 (2026-06-09 세션28, WSL2 = ADR-029, 메인 직접 실측):

- [x] **Mage 평타 = 서버 확정 투사체** — 명중 즉발 판정 후 `S_ProjectileLaunch` 발사 통보 + travelTicks 후 지연 데미지. RangedHitSmoke로 projectileLaunch=True·travelTicks=2·S_HitResult(hitEffect=1)·HP 30→8 실측
- [x] **사거리 밖 = 캐스팅 스윙만** — 허공 평타 시 투사체 X·데미지 0. RangedWhiffSmoke로 projectiles=0·hitResults=0 실측
- [x] **썬더볼트 박스 AoE** — C_SkillUse→박스 스캔→각 적 낙뢰. ThunderboltAoeSmoke로 skillCast=True·normalHits(hitEffect=2)>=1·bossHit=True 실측 + dotnet `BoxScan_TargetsInBox_AllEnqueued`(박스 안 전원 enqueue·밖 0)
- [x] **freeze (Normal 정지 / Boss 면역)** — FreezeSmoke로 froze=True·resumed=True 실측 + dotnet `Freeze_NormalEnemy_XUnchanged`·`Boss_ApplyFreeze_BossBehaviorSystemContinues`(Boss FrozenUntilTick 세팅돼도 이동) 결정적 검증
- [x] **쿨다운 서버 권위** — tick 기반(map.CurrentTick), 초기값 오버플로우 회피. dotnet `Cooldown_SecondCastDropped`(미경과 재발동 silent drop)
- [x] **trust-boundary** — skillId 검증 + handshake auth + caster entityId session 강제(도용 차단) + rate-limit/rewind 앞단 유지. dotnet handler 3종(happy/invalid/auth)
- [x] **`ProtocolVersion.Current == 11`** — enum 시프트 0(신규 23/24/25, 기존 ≤22 불변, S_HitResult 끝 hitEffect append). 클린빌드 0/0 + PacketRoundTrip 신규 4건
- [x] `dotnet test` green — **508 통과/0 실패/4 skip**(LagSim 1 flaky = 동시 실행 경합, 단독 green = 코드 무관)
- [x] 신규 봇 4종 PASS + 기존 봇 v11 회귀 0 — RangedHit/RangedWhiff/ThunderboltAoe/Freeze 전수 PASS + EmergencyCombat(hits=2·death) / BossFight(hits=5·stageClear) 회귀 0
- [x] Unity 클라 컴파일 0 err — MCP RunCommand isCompiling=False + ReadConsole 0 error. 새 .prefab/.unity/.asset 0건(VFX=Resources.Load placeholder warn-once)
- [x] reviewer P2~P5 전부 🔴0 — P3 aggro 누락(🔴)은 봉합 후 통과
- [x] **freeze 강화 (StunTicks)** — 영호 결정 반영, CombatConstants.StunTicks=8 + ApplyFreeze 2곳 + 테스트 단언 갱신
- [~] **2클라 Play 매트릭스** — 봇 4종(프로토콜 경로 전수 PASS) + dotnet 508이 데미지/freeze/박스/쿨다운 로직을 자동 검증 커버. 2클라 시각 체감(placeholder VFX라 투사체/낙뢰 시각은 유현 에셋 후)은 영호 직접 Play
- [ ] CHANGELOG [M] entry + PR 생성·머지 — **영호 명시 GO 게이트**(본 박제 commit 후)
- [x] work-pin 갱신 — 본 마감 흐름에서 지속 갱신

---

## 이월 명시 (➡️ 다음)

- **AoE 실 광역기 확장**: `ResolveImpactTargets`가 단일/박스 공용으로 깔렸으므로 썬더볼트 외 스킬(원형 장판, 직선 관통 등)은 박스→모양만 추가. 다단 히트도 미구현.
- **텔레포트(블링크) = M4.9**: M4.8에서 분리된 이동 권위/prediction 갈래. 서버 확정 워프 vs 클라 예측, 벽 통과/충돌 = M4.6 이동 동기화 재검토.
- **밸런스 Play 튜닝**: MageAttackHalfExtent(4.0f)·ProjectileSpeedPerTick(2.0f)·MinTravelTicks(2)·MaxTravelTicks(10)·StunTicks(8)·ThunderboltBoxHalfX/Y(6/3)·LightningDelayTicks(4)·ThunderboltCooldownTicks(40) 전부 임시값. 2클라 Play로 감각 조정.
- **백로그 (reviewer 🟡, 비차단)**: 평타↔스킬 클라 쿨다운 게이트 공유(입력 유실 2클라 측정 후 분리 판단, premature 봉합 회피) / freeze 가드 표현 통일(CombatSystem 무조건 호출 vs SkillSystem Kind 분기) / rewind 매직넘버(4) → CombatConstants.RewindMaxTicks 상수화(평타+스킬 rule of three) / 빈 박스 쿨다운 소비 단언 추가 / S_SkillCast strikeDelayTicks·projectileType 미사용(연출 단계 hook) / death-path 헬퍼 추출(CombatSystem ↔ DeferredDamageSystem 중복).
- **마나·MP 시스템**: 현재 쿨다운만. 스킬 코스트는 미래.
- **음수 currentHp 계약**: 즉시 경로(Knight)는 currentHp raw(음수=사망 신호로 LagSim 봇 의존), deferred 경로는 Max(0) floor. 통일은 봇을 S_EntityDeath 판정으로 전환하는 별도 작업.

---

## 학습 일지 후보 키워드

서버 확정 후 발사(클라 선예측 스폰 폐지 = "그림은 맞았는데 데미지 0" 구조적 제거 / 로컬·원격 단일 스폰 경로 = 중복 0) / 지연 데미지 = RespawnSystem 틱 카운트다운 패턴 재사용(await·Sleep 0 = 헌법 #5, id 재조회 late binding으로 stale impact 자동 무효) / freeze 서버 권위 + Boss 면역(FrozenUntilTick + EnemyAISystem 가드, BossBehaviorSystem엔 가드 없음 = 면역, ApplyFreeze max 중첩) / ResolveImpactTargets 단일·박스 공용 = AoE-ready(광역기는 단일 모델의 확장) / PDL list 미지원 → 적별 S_HitResult + hitEffect byte append 회피(도구 확장 0, 평타·썬더볼트 도착 처리 공유, append-only = 바이트 오프셋 안정성) / 최소 스킬 시스템(C_SkillUse 쿨다운만 서버 권위, tick 기반 = ms 시계 의존 회피, 초기값 -(쿨다운+1)로 long.MinValue 오버플로우 회피, caster entityId session 강제) / freeze 강화 = 도착 후 StunTicks(데미지는 도착 시 / freeze는 도착+StunTicks = 진짜 stun-lock, 부수효과로 봇 측정 안정화) / 봇 검증 경계(Boss FSM Idle dwell로 이동 관측 비결정 → dotnet 위임, 봇은 통합 흐름만) / aggro는 deferred 도착 시점에(후공 적의 유일 Chase 트리거 = TargetEntityId, 대칭 경로 누락은 diff 아닌 계약으로 보임) / reviewer 권고도 맥락 누락 가능(floor 통일 권고 → 음수 currentHp 봇 사망 신호 계약 깨 회귀 → 메인 직접 실측이 잡음)
