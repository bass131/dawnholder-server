---
owner: youngho
milestone: M4.7
phase: milestone-closeout
title: HP 동기화 + 공격 모델 정비 — 전용 S_PlayerHp/S_PlayerAttack 이벤트 + 허공 스윙 분리
status: done
completed: 2026-06-09
grade: 대규모
summary: M4.7 완전 마감 (6 Phase + Play 봉합 2건, PR #83 HP갈래 + #84 공격갈래·Play봉합 + 본 마감 PR). 두 구조급 결함을 한 묶음으로 봉합 — ① 플레이어 HP를 변할 때만 송신하는 전용 S_PlayerHp(on-change) 이벤트로 권위 동기화하고 클라 표시 미러(PlayerStats maxHp 추론)를 은퇴(사망 후 HUD 0 고착 근본 봉합), ② 공격 스윙↔명중을 분리해 타겟 없이도 허공 스윙이 나가고 다른 플레이어의 공격이 순간 이벤트 S_PlayerAttack으로 보이게 함. 핵심 = EnterAttackState를 AABB 명중 앞으로 옮기되 rate-limit/rewind 검증은 앞단 유지(trust-boundary 불변, 스팸 차단 유지). 예측 가능/불가능 비대칭(자기 공격 시점=선예측 / 명중·HP·lunge=서버 신뢰)으로 rubber-band 0. Play 피드백 봉합 2건 — 공격 입력 쿨다운 게이트(AttackCooldownTicks=10 98_Shared 단일, commit window보다 길게 잡아 유령 스윙 차단) + Knight 전방 lunge(넉백 ExternalVelX 채널 재활용 + force-adopt 렌더 = 서버 권위). Item 2(Mage 진짜 원거리)는 구조상 마일스톤급이라 M4.8로 승격(메이플식 서버확정 투사체+지연데미지+freeze, 썬더볼트 AoE 토대). ProtocolVersion 9→10(S_PlayerHp 21 + S_PlayerAttack 22, enum 시프트 0). 최종 회귀 = 클린빌드 0/0 + test 479/0/4 + 신규 봇 3종 PASS + 기존 봇 회귀 0 + Unity 컴파일 0err. 5단계 보고 시각판 = _milestone-DONE.html.
---

# M4.7 — HP 동기화 + 공격 모델 정비 마일스톤 박제

**마감 일자**: 2026-06-09 (세션27, Phase 06 회귀 + 마감)
**Phase 수**: 6/6 완료 (01~05 + Play 봉합 동승, PR #83~#84 + 본 마감 PR)
**등급**: 대규모 (shared 프로토콜 bump = irreversible + server 신뢰 경계 + client 미러 은퇴 + qa = 4도메인 관통)
**WORK-ID**: m4.7-hp-sync-and-combat-events
**시각 보고서**: [`_milestone-DONE.html`](_milestone-DONE.html) — 대규모 5단계 보고 HTML 박제

---

## 5단계 보고

- 🎯 **무엇을 만들었나** — 두 구조급 결함을 한 묶음으로 봉합했다. ① **플레이어 HP 권위 동기화** — HP가 변할 때만 송신하는 전용 `S_PlayerHp`(on-change) 이벤트(PacketID 21)를 신설하고, 클라가 `PlayerStats.ForClass(class).MaxHp`로 maxHp를 추론하던 **표시 미러**를 은퇴(M4.5 임시 봉합 철거). ② **공격 스윙↔명중 분리** — 공격 연출(스윙)을 명중 판정에서 떼어내 타겟이 없어도 허공 스윙이 나가고, 다른 플레이어의 공격을 순간 이벤트 `S_PlayerAttack`(PacketID 22)으로 화면에 보이게 했다. ProtocolVersion 9→10. + Play 피드백 봉합 2건 — 공격 입력 쿨다운 게이트(유령 스윙 차단) + Knight 전방 lunge.
- 🤔 **왜 필요한가** — 주기 `S_Snapshot`(ID 6)엔 HP 필드가 없어 플레이어 자기 HP는 피격 시 `S_EnemyAttack.targetCurrentHp`로만 흘러왔고(maxHp도 없음), 사망→부활·회복은 채널이 없어 클라가 표시 미러로 때웠다 — **"사망 후 HUD 0 고착"의 근본 원인**. 또 공격은 **4겹 직렬 게이트가 전부 타겟 존재에 묶여** 사거리 내 적이 없으면 클라가 `C_Attack` 송신 자체를 안 했고(로컬 Attack 예측도 없음) → 빗나가면 로컬조차 자기 스윙을 못 봤다. 원격 공격은 `animState=Attack`(지속상태 미러)로만 보여 투사체 발사 같은 *순간 이벤트*를 표현할 수 없었다(원격 Mage 투사체 표시 불가).
- 🛠️ **어떻게 만들었나** — 수직 슬라이스 **2갈래**: `P1` 프로토콜 신설 후 HP갈래(`P2` 서버 송신 → `P4` 클라 신뢰)와 공격갈래(`P3` 서버 스윙분리 → `P5` 클라 예측·원격 연출)가 병렬로 떨어진다. 핵심은 **스윙↔명중 분리** — `EnterAttackState`를 AABB 명중 *앞으로* 옮겨 명중 무관하게 스윙·`S_PlayerAttack`이 나가되, rate-limit/rewind 검증은 그대로 앞단에 두어 스팸 차단을 지켰다(데미지·`S_HitResult`는 AABB 명중만 게이트). **예측 가능/불가능 비대칭**: 클라가 아는 것(자기 공격 시점)은 선예측, 모르는 것(명중·HP·lunge)은 서버 신뢰. 갈래별 중간 상태도 헌법 정합이라 PR1(HP) → PR2(공격+Play봉합) → PR3(마감) 스택 머지.
- 🧪 **테스트 결과** — 최종 회귀(세션27, WSL2 = ADR-029): 클린빌드 **0 경고/0 오류** + `dotnet test` **479 통과/0 실패/4 skip** + 신규 헤드리스 봇 **3종 전부 PASS**(HpSyncSmoke = 보스룸 피격→S_PlayerHp 4단계 초기full/감소/사망0/부활full 관측 / RemoteAttackSmoke = 2봇 except 가드 B수신·A미수신 / WhiffSwingSmoke = 허공 스윙 S_HitResult 0건) + 기존 봇(EmergencyCombat·BossFight) **회귀 0** + `ProtocolVersion == 10`(enum 시프트 0: `S_EnemyAttack 20`·`C_Attack 11` 불변, 신규 21/22) + Unity 클라 컴파일 0 err(`PlayerHpHandler`·`PlayerAttackHandler`·`ProjectileSpawner` 로드). 2클라 Play 매트릭스는 Item 1/Item 3을 사용자가 직접 Play 확인(쿨다운·lunge 감각).
- ➡️ **다음 스텝** — Item 2(Mage 진짜 원거리)는 구조상 마일스톤급이라 **M4.8로 승격**: 메이플식 서버 확정 투사체 + 도착 시 지연 데미지 + 도착까지 몬스터 freeze — 추후 썬더볼트 광역기(AoE)의 토대(memory `future-m4.8-maple-ranged-combat`). 백로그(reviewer 🟡, 비차단): `CombatSwingTests` auth 거부 테스트 / facing round-trip / `PlayerAttackHandler` Resources.Load static 캐시 / lunge 단위 테스트. 차기 마일스톤 가닥(외관·연출 또는 M4.8 또는 M5 Persistence)은 사용자 결정.

---

## TL;DR (🎯 무엇 / 🤔 왜)

M4.7은 **플레이어 HP를 권위적으로 흘려보내고**, **공격이 명중과 무관하게 항상 스윙을 내며**, **다른 플레이어의 공격이 화면에 보이게** 한 v10 구조급 마일스톤이다.

**HP = 전용 on-change 이벤트**: 옛 클라는 HP 채널이 없어 `PlayerStats`로 maxHp를 추론하는 표시 미러로 때웠다(사망 후 HUD 0 고착). 이제 HP가 변할 때만(진입+피격+부활) 보내는 전용 `S_PlayerHp`(entityId/currentHp/maxHp)로 서버가 권위 HP를 흘리고, 클라는 받은 값만 표시한다(헌법 #1). TCP 신뢰 전송이라 on-change 손실 약점이 무효이고, `entityId` 필드를 미리 박아 미래 파티/원격 HP 바로 **패킷 모양 불변** 확장을 열었다.

**스윙(연출) ↔ 명중(데미지) 분리**: 옛 코드는 4겹 게이트(클라 송신/로컬 예측/서버 진입/데미지)가 전부 타겟 존재에 묶여 빗나가면 스윙조차 안 났다. 이제 `EnterAttackState`를 AABB 명중 앞으로 옮겨 rate-limit/rewind만 통과하면 명중 무관하게 스윙 상태 진입 + `S_PlayerAttack` broadcast(except: attacker.Owner). 데미지·`S_HitResult`는 AABB 명중일 때만 — "연출만 분리, 권위 판정 불변"이라 trust-boundary가 안 흔들린다.

**예측 가능/불가능 비대칭으로 rubber-band 0**: 클라는 자기 공격 시점을 아니까 commit window 동안 Attack을 선예측한다(서버도 유효 시도 시 진입 → 일치 = 보수적). 명중·HP·lunge처럼 서버가 정하는 것은 신뢰(force-adopt). 낙관적으로 위치를 먼저 움직이면 틀릴 때 튕기지만, 보수적(먼저 멈춤/서버 채택)이면 틀려도 rubber-band가 0이다.

**프로토콜 규율**: ProtocolVersion 9→10(영구). PDL append-only로 두 패킷을 *맨 아래만* ID 21/22로 박아 기존 enum 시프트 0(`S_EnemyAttack 20`·`C_Attack 11` 불변). `C_Attack` 모양은 불변 — `targetEntityId` 필드 *의미*만 "필수 타겟" → "선택 힌트(0=허공)"로 바뀌어 추가 패킷 없이 동승.

---

## Phase 박제 요약

| Phase | 제목 | 핵심 | 머지 |
|---|---|---|---|
| 01 | 프로토콜 신설 | PDL append-only로 `S_PlayerHp`(21)+`S_PlayerAttack`(22) 신설, PacketGenerator 재생성, **ProtocolVersion 9→10**, Shared.dll 재빌드+복사. enum 시프트 0 확인 [irreversible] | PR #83 |
| 02 | 서버 HP 송신 | `SendPlayerHp` 헬퍼 + 3트리거(진입 `EnterGameWorld` / 피격·부활 `ApplyBossAttack` / 맵전환 `MapMigration`). 음수는 `Max(0,Hp)` floor 송신 | PR #83 |
| 04 | 클라 HP 신뢰 경로 | `PlayerHpHandler` 신설 + dispatch 등록 + **표시 미러 제거**(`PlayerStats` maxHp 추론 은퇴). 사망 후 HUD 0 고착 근본 봉합. reviewer 🟡 맵전환 HP송신 갭(데미지 캐리 placeholder 고착) 동반 봉합 | PR #83 |
| 03 ★ | 서버 공격 모델 정비 | **스윙↔명중 분리** — `EnterAttackState`를 AABB 명중 앞으로 이동 + `S_PlayerAttack` broadcast(except: attacker.Owner). AABB 명중은 데미지+`S_HitResult`만 게이트. rate-limit(스윙 시도 기준)/rewind 앞단 유지 [trust-boundary] | PR #84 |
| 05 ★ | 클라 공격 입력+예측+원격 연출 | 타겟 게이트 제거(항상 스윙) + `AttackIntent` 타겟 없어도 송신(sentinel 0) + 로컬 Attack 선예측(commit window) + `PlayerAttackHandler` 원격 투사체/스윙(`ProjectileSpawner` 추출). 로컬 중복 0 · rubber-band 0 | PR #84 |
| — | *(Play 봉합) 쿨다운 게이트 + Knight lunge* | Item 1 = 공격 입력 쿨다운 게이트(`AttackCooldownTicks=10` 98_Shared 단일, 클라 `CanAttack` + `LocalPlayerInput` 게이트) — commit window(8틱)보다 길게 잡아 유령 스윙(400~500ms 갭) 차단. Item 3 = Knight 전방 lunge(넉백 `ExternalVelX` 채널 재활용 + `AttackState.Tick` 감쇠, 클라 force-adopt 렌더 = 100% 서버 권위) | PR #84 |
| 06 | 회귀 + 마감 | 신규 봇 3종 + 전체 회귀(빌드/test/기존봇/v10) + 본 박제 + PR | 본 마감 PR |

**Phase 06 포함분 (세션27)**:
- **신규 스모크 봇 3종 작성 + 전수 PASS** — `HpSyncSmoke`(보스룸 피격→`S_PlayerHp` 4단계 관측: 초기 full → 감소 → 사망 0 → 부활 full. 봇은 보스를 공격하지 않음 = 보스는 리스폰 없어 죽이면 피격 소스 소멸), `RemoteAttackSmoke`(2봇, A 허공공격→B가 `S_PlayerAttack` 수신·A 미수신 = except 가드), `WhiffSwingSmoke`(타겟 없는 허공 스윙→`S_HitResult` 0건 = 데미지 없음).
- **마일스톤 전체 회귀 입증** — 클린빌드 0/0 + `dotnet test` 479/0/4skip + 기존 봇(EmergencyCombat·BossFight) 회귀 0. 보스/전투 봇은 **fresh 서버 단독** 실행(교차오염 회피, 보스 무리스폰이라 서버당 1회).

---

## 결정 흐름 (🛠️ 어떻게 — 회고 참고용)

1. **HP = 전용 on-change 이벤트** — 주기 snapshot에 HP를 끼우면 고빈도 비대화(헌법 #5)이고 위치 reconcile과 엉킨다. HP는 변할 때만(진입+피격+부활) 보내는 전용 `S_PlayerHp`로 분리 — TCP 신뢰 전송이라 on-change 손실 약점이 무효. broadcast 범위는 본인에게만(`Owner.Send` 1:1)이되 `entityId`를 미리 박아 미래 전원 broadcast 승격 시 **패킷 모양 불변 → 추가 bump 불필요**.
2. **스윙(연출) ↔ 명중(데미지) 분리** — 데미지 모델(단일 타겟 AABB)은 그대로 두고 스윙 *이벤트*만 명중에서 뗐다. `EnterAttackState`를 AABB 판정 앞으로 옮기되 **rate-limit/rewind는 앞단 유지** — "연출만 분리, 권위 판정 불변"이라 trust-boundary가 안 흔들린다. rate-limit은 "스윙 시도" 기준이라 빈 스윙도 쿨다운을 소비(스팸 차단 = 헌법 #3 유지).
3. **예측 가능/불가능 비대칭** — 클라는 *자기 공격 시점*을 아니까 commit window 동안 Attack을 선예측(서버도 유효 시도 시 진입 → 일치 = 보수적, 틀려도 commit window 종료로 자연 복구 → rubber-band 0). *언제 맞을지·HP·lunge*는 서버가 정하니 신뢰(force-adopt). 낙관적으로 위치를 먼저 움직이면 틀릴 때 튕긴다. (M4.6 carry-over "보수적 예측 방향" 정신의 연장.)
4. **Knight lunge = 넉백 패턴 재활용 + force-adopt** — 새 이동 채널을 만들지 않고 넉백(`KnockbackVx`)이 흐르던 `ExternalVelX`(Physics.Step 4번째 인자)에 lunge 임펄스를 실어 `AttackState.Tick`이 `KnockbackDecayPerTick`로 감쇠(넉백과 동형). 둘은 상호배타 State(Attack vs Hit)라 `GameMap.Tick`이 합산해 전달. 클라는 force-adopt(`AnimState.Attack`)로 렌더만 = 100% 서버 권위. lunge 세기(`AttackLungeInitialVx=3.0f`)는 서버 전용(헌법 #1) — 98_Shared 불변, Mage 제외.
5. **Play 봉합도 보수적 방향** — 공격 입력 쿨다운 게이트(`AttackCooldownTicks` 10틱=500ms)를 commit window(8틱=400ms)보다 길게 잡아 "한 번 공격은 끝까지 커밋" + 유령 스윙(400~500ms 갭에 재발동) 차단. `AttackCooldownTicks`를 98_Shared 단일 진실로 신설(commit window 선례 정합)하고 서버 `AttackCooldownMs`를 역산 단일화, 클라 `CanAttack`은 권위가 아닌 **예측 거울**(서버 rate-limit과 같은 값).
6. **Item 2 = M4.8 승격 판단** — Mage는 구조상 근접(`GetAttackHitbox`에 class 분기 없음, 투사체 콜라이더 0 = 시각 전용). "진짜 원거리"는 서버 확정 투사체 + 도착 시 지연 데미지 + 몬스터 freeze = 마일스톤급. 단일 타겟 하드코딩 대신 "도착 시 범위 내 N 타겟 판정"으로 일반화하는 토대를 처음부터 깔아야 추후 썬더볼트 AoE가 같은 모델의 확장이 된다 → M4.7에 안 넣고 **M4.8 "원거리 전투 모델"로 승격**.

---

## AC 검증 결과

마일스톤 완료 조건 대조 (2026-06-09 세션27, WSL2 = ADR-029 표준 경로, 메인 직접 실측):

- [x] 플레이어 HP가 **전용 `S_PlayerHp` 이벤트**로 권위 동기화 — 표시 미러(maxHp 추론) 은퇴. HpSyncSmoke로 진입 full / 피격 감소 / 사망 0 / 부활 full **4단계 전부 관측**(events=14)
- [x] HP 송신이 **변경 지점마다 동반** — 진입/피격/부활(+맵전환 갭 봉합). 음수는 `Max(0,Hp)` floor. HpSyncSmoke 사망 시 currentHp==0, 부활 시 currentHp==maxHp(150) 실측
- [x] **허공 스윙** — 타겟 없어도 공격 버튼이 스윙을 냄. WhiffSwingSmoke로 허공 4회 공격→`S_HitResult` **0건**(데미지 없음, 스윙 연출은 유지) 실측
- [x] **원격 공격**이 `S_PlayerAttack` 순간 이벤트로 broadcast — RemoteAttackSmoke로 botB 수신(bReceived=1)·botA 미수신(aReceived=0) = **except 가드** 실측
- [x] 스윙 분리해도 **rate-limit/rewind 검증 앞단 유지** — P3 trust-boundary 점검(rate-limit/rewind가 `EnterAttackState` 앞단 유지·데미지 AABB 게이트 불변·attacker 도용방어 불변). EmergencyCombat rate-limit burst 회귀 0
- [x] 클라 로컬 Attack **선예측** + 서버 유효시도 진입 일치 → **rubber-band 0** — P5 Unity 컴파일 0err + [Reconcile] count=0(이전 세션 실증)
- [x] **`ProtocolVersion.Current == 10`** — enum 시프트 0(`S_EnemyAttack 20`·`C_Attack 11` 불변, 신규 `S_PlayerHp 21`/`S_PlayerAttack 22`). 클린빌드 0/0
- [x] `dotnet test` green — **479 통과/0 실패/4 skip**(Total 483, Duration 1m41s)
- [x] 신규 봇 3종 PASS + 기존 봇 회귀 0 — HpSync/RemoteAttack/WhiffSwing 전수 PASS + EmergencyCombat(hits=2, death=True) / BossFight(hits=5, stageClear=True) 회귀 0. 캐비앗: 전투/보스 봇 fresh 서버 단독, 보스 시나리오 서버당 1회
- [x] Unity 클라 컴파일 0 err — `PlayerHpHandler`·`PlayerAttackHandler`·`ProjectileSpawner` 로드(이전 세션 MCP RunCommand 실증)
- [~] Item 2(Mage 진짜 원거리) — 구조상 마일스톤급이라 **M4.8로 승격**(사용자 결정). M4.7 봉합엔 미포함
- [~] 2클라 Play 매트릭스 — Item 1(쿨다운)·Item 3(lunge) **사용자 직접 Play 확인**(감각 OK). 직업 2종 풀 매트릭스는 봇 3종(프로토콜 경로 전수) + Play 봉합 커버리지로 충분 판정
- [ ] CHANGELOG [M] entry + PR 생성·머지 — **사용자 명시 GO 게이트**(본 박제 commit 후 진행)
- [x] work-pin 갱신 — 본 마감 흐름에서 지속 갱신

---

## 이월 명시 (➡️ 다음)

- **M4.8 "원거리 전투 모델" (승격)**: Mage 메이플식 진짜 원거리 — 서버 확정 투사체 + 도착 시 지연 데미지 + 도착까지 몬스터 freeze. 단일 타겟 대신 "도착 시 범위 내 N 타겟"으로 일반화 → 추후 썬더볼트 광역기(AoE) 토대. 주의: ① 우리 투사체는 호밍이라 freeze는 연출상 *선택*(순수 stun-lock 결정), ② freeze/HP는 서버 권위(클라 단독 잠금 금지), ③ 투사체는 클라 예측이 아니라 **서버 확정 후 발사**여야 "그림은 맞았는데 데미지 0" 재발 방지. 설계 상세 = memory `future-m4.8-maple-ranged-combat`
- **백로그 (reviewer 🟡, 비차단)**: `CombatSwingTests` auth 거부 테스트 / facing round-trip 테스트 / `PlayerAttackHandler` Resources.Load static 캐시 / lunge 단위 테스트(AttackLungeVx 세팅·감쇠·Mage 제외 + GameMap ExternalVelX 합산)
- **HP 확장 (패킷 모양 불변)**: `entityId` 필드를 미리 박았으므로 원격 머리 위 HP 바 / 파티 HP는 추가 bump 없이 broadcast 승격만으로 가능
- **S_EnemyAttack vs S_PlayerHp HP 중복**: 피격 시 둘 다 HP 운반(의도적 — S_EnemyAttack은 이펙트/연출, HP 권위는 S_PlayerHp 일원화). `S_EnemyAttack.targetCurrentHp` deprecated/제거는 별도 bump = 범위 밖
- **다음 마일스톤 가닥 (사용자)**: 외관·연출(배경/컷신/NPC) 또는 M4.8 원거리 전투 모델 또는 M5 Persistence(LocalDB Linux + GenPackets Write 풀링 + Serilog/DI)

---

## 학습 일지 후보 키워드

HP = 전용 on-change 이벤트(주기 snapshot 비대화 회피 + 위치 reconcile 분리 + TCP 신뢰로 손실 약점 무효 + entityId로 패킷 모양 불변 확장) / 표시 미러 은퇴(클라 PlayerStats maxHp 추론 = 사망 후 HUD 0 고착 근본 원인) / 스윙↔명중 분리(EnterAttackState를 AABB 앞으로, rate-limit·rewind는 앞단 유지 = 연출만 분리 권위 판정 불변, trust-boundary 안 흔들림) / 예측 가능·불가능 비대칭(자기 공격 시점=선예측 commit window / 명중·HP·lunge=서버 신뢰 force-adopt) / 보수적 예측 방향(먼저 멈춤·서버 채택이면 틀려도 rubber-band 0, 낙관적 먼저 움직임은 튕김) / 넉백 ExternalVelX 채널 재활용으로 lunge(새 채널 0, force-adopt 서버 권위, 98_Shared 불변 = lunge 세기 서버 전용) / 쿨다운 게이트 = 예측 거울(98_Shared 단일 상수, commit window보다 길게 = 유령 스윙 차단) / PDL append-only ID 21·22로 enum 시프트 0(C_Attack 모양 불변, 필드 의미만 변경 = 추가 bump 회피) / 봇 negative 검증(허공 스윙 = S_HitResult 0건) + except 가드(2봇 A공격→B수신·A미수신) / 보스 무리스폰이라 HpSync 봇은 보스 비공격(죽이면 피격 소스 소멸) / Item을 마일스톤급으로 승격 판단(Mage 구조상 근접 → 진짜 원거리 = M4.8, AoE 일반화 토대 먼저)
