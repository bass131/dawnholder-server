---
owner: youngho
milestone: M4.5
phase: 04
title: 보스 프로토콜 + 서버 행동 — S_EnemyAttack + class append + 8→9 bump + BossBehavior
status: done
completed: 2026-06-07
grade: 대규모
summary: M4.5 Phase 04 완료 (세션21, 15파일 +α/3신설 1156줄). 맞기만 하던 보스가 스스로 공격 — BossBehaviorSystem FSM(쿨다운→telegraph→권위 판정, 페이즈2 50% 1회성) + 플레이어 사망→스폰 리스폰 HP full + ProtocolVersion 8→9 한 묶음(S_EnemyAttack ID20 신설 + S_PlayerJoin characterClass append) + v8 화석 주석 2곳 정정. Coordinator 4구획(shared→server→qa) 순차 분해, 메인 검수 정정 1건(봇 가짜 리스폰 감지). 검증 = WSL2 test 417/0/4skip(+18 신규) + 봇 BossFightSmoke PASS(damage=15 공식 일치) + 기존 스모크 회귀 0 + reviewer 🔴0/🟡3. telegraph 정량 = P1 16틱(0.8s)/P2 10틱(0.5s) 사용자 확정 박제. 5단계 보고 시각판 = 04-boss-protocol-and-server-DONE.html.
---

# Phase 04 박제: 보스 프로토콜 + 서버 행동

**소요**: 세션21 — Coordinator 4구획 분해(CP-1 shared → CP-2/3 server → CP-4 qa) + 메인 검수
**시각 보고서**: [`04-boss-protocol-and-server-DONE.html`](04-boss-protocol-and-server-DONE.html) — 공격 1사이클 타임라인 포함 (대규모 5단계 보고 HTML 박제)

## 5단계 보고

- 🎯 **무엇을 만들었나** — 보스 페이즈 1/2 패턴 FSM + 서버 권위 적→플레이어 데미지 + 사망 시 스폰 리스폰(HP full) + ProtocolVersion 8→9 한 묶음(S_EnemyAttack 신설 + S_PlayerJoin characterClass append). 상세 = TL;DR.
- 🤔 **왜 필요한가** — 전투가 플레이어→적 단방향이었음. 양방향 모두 서버 판정이면 전투의 진실이 완전히 서버에 모임(헌법 #1). bump 묶음은 팀원 재빌드 비용 최소화(M4.2 학습) + v8 화석 약속 정정.
- 🛠️ **어떻게 만들었나** — Coordinator 4구획 순차(shared가 dll 먼저 → server FSM/리스폰 → qa 검증). BossBehaviorSystem 신설 + EnemyAISystem Boss 분기 완전 이관. 상세 = 박제 사실 표.
- 🧪 **테스트 결과** — WSL2 test 417/0/4skip(+18) + 봇 BossFightSmoke PASS(damage=15 = Formulas 일치) + 기존 회귀 0 + reviewer 🔴0. 상세 = AC 검증 결과.
- ➡️ **다음 스텝** — Phase 05 보스 클라 연출 + 원격 직업 표시(본 Phase 패킷 소비). telegraph 0.8s/0.5s가 클라 이펙트 타이밍의 진실.

## TL;DR (🎯 무엇 / 🤔 왜)

맞기만 하던 보스가 **스스로 공격하는 보스**가 됐다. 핵심 갈래 셋:

1. **양방향 권위 전투 (헌법 #1)** — 적→플레이어 데미지 판정도 서버가 `player.Position`(권위 위치)만으로 수행. 보스 공격 범위(±2.5f AABB) ∩ 권위 위치, 범위 밖 = 0. 데미지는 `Formulas.ComputeDamage` 단일 공식의 EnemyStats→PlayerStats 오버로드 — prediction으로 피한 플레이어가 맞을 수 있는 비대칭은 telegraph(예고)가 체감 공정성으로 보완.
2. **bump 한 묶음 (헌법 #2)** — `S_EnemyAttack`(ID 20: attackerId/targetId/damage/targetCurrentHp/attackPattern) 신설 + `S_PlayerJoin.characterClass` byte 맨 끝 append를 v9 한 번에. 마일스톤 유일 bump — 이후 Phase bump 0. 옛 v8 주석의 "Phase 09 S_EnemyAttack도 v8 포함" 깨진 약속을 `ProtocolVersion.cs` + `PDL.xml` 두 곳에서 같은 commit 정정 (false-promise 봉합).
3. **tick 카운터 FSM (헌법 #5)** — 쿨다운/telegraph/페이즈 전환 전부 int 카운터 감소. `Task.Delay`/ms 타이머/`Thread.Sleep` 0, 루프 내 동적 할당 0 (패킷 생성은 broadcast 시점만).

## 박제 사실 (🛠️ 어떻게)

| 구획 | 산출 |
|---|---|
| CP-1 shared | `PDL.xml` S_EnemyAttack append + S_PlayerJoin characterClass append + 화석 주석 정정 / `ProtocolVersion.cs` 8→9 + v9 이력 + 화석 정정 / GenPackets 재생성 + Shared.dll·ClientNet.dll PostBuild 동반 (헌법 #4 — 3종 산출물) |
| CP-2 server | `BossBehaviorSystem.cs` 신설 206줄 — Boss 전담 stateless System. EnemyAISystem Boss 분기(17줄) 완전 이관 = latch 감소·broadcast 단일화. Tick 순서 physics→Combat→EnemyAI→**Boss**→Respawn. `CombatConstants` 보스 상수 7종 / `EnemyEntity` FSM 필드 3종(IsPhase2/AttackCooldownTicks/TelegraphTicksRemaining, ctor 초기 쿨다운 = 스폰 즉시 공격 방지) / `Formulas.cs` EnemyStats.Attack(Normal 5/Golem 8) + BossDefault(Atk12/Def3/HP100 — EnemyDefaultHp drift 가드 테스트) + ComputeDamage 오버로드(기존 호출처 무변경 = 회귀 면적 0) / GameMap SpawnEnemy에 Boss→BossDefault (누락 시 Attack=0 → 데미지 항상 1이었을 함정) |
| CP-3 server | 플레이어 HP 0 → 스폰 재배치 + **HP full** + `IsDeadAnimState=false` 명시 리셋(누락 시 부활 후 영원히 Death 애니 — 최대 함정). 통지 = 다음 S_Snapshot(≤100ms, kill-plane 선례 정합 + 틱 내 추가 할당 회피). characterClass wiring 4곳(GameSession roster/broadcast + MapMigration 2곳) 전부 `(byte)entity.Stats.Class` — 클라 raw echo 경로 0 |
| CP-4 qa | `BossBehaviorTests.cs` 18 테스트(505줄) + 봇 `BossFightSmoke.cs`(보스방 진입→피격 관측→처치→StageClear) + Program.cs dispatch. `BossStageClearTests` 기대값 강화 — 옛 `default` EnemyStats(Def0) 가짜 기대값 → BossDefault 실스탯 직접 참조(hits 4→5) |
| 메인 검수 | 봇 가짜 리스폰 감지 발본 — `_lastKnownHp=-1` 초기값이 첫 피격을 리스폰으로 오인(데이터 모순: 2회 피격 ×15 = HP 70인데 respawnCount=1) → `_sawDeath` bool 패턴 + `EnemyAttackCount≥1` 성공 게이트 + dead code 제거 후 재실행 검증 |

**telegraph 정량 박제 (Phase 05 소비 — plan-auditor 🟡 봉합)**: 페이즈 1 = **16틱(800ms)**, 페이즈 2 = **10틱(500ms)**. 근거 = 인간 시각 반응 ~250ms + 회피 이동 여유. 쿨다운 = 40틱(2s)/24틱(1.2s), BossBaseDamage=8, 범위 half-extent 2.5f, 페이즈 임계 0.5. 전부 사용자 확정 후 `CombatConstants.cs` 박제 — Warrior 체감 데미지 = Max(1, 8+12−5) = **15**.

## AC 검증 결과

- WSL2(ADR-029) `dotnet build --no-incremental` 0경고/0오류 + `dotnet test --no-build` → **417 통과 / 0 실패 / 4 skip** (신규 18)
- 완료 조건 7항목 전부 green: ① ProtocolVersion==9 + 두 패킷 한 commit 묶음 + 은퇴 ID 재사용 0 ② 페이즈 1→2 전환(51/50/49% 경계 + 1회성) + 쿨다운 틱 정확성(40+16 / 24+10) ③ 범위 내만 데미지·밖 0·서버 계산(Formulas 직접 참조 drift 방지) ④ 사망→스폰 리스폰 HP full + IsDeadAnimState 리셋 ⑤ characterClass fail-closed(기존 CharacterSelectHandler 회귀 + 서버 권위값 검증) ⑥ 봇 BossFightSmoke PASS — S_EnemyAttack 관측 damage=15 공식 정확 일치, EnemyAttackCount≥1 게이트 ⑦ tick p99 < 1ms (50ms 예산 침범 0)
- 기존 회귀: BossStageClearSmoke PASS + StageClear 1회성 불변 + BroadcastTests/MapMigrationTests green (S_PlayerJoin 17바이트 — 크기 assert 없어 자연 호환)
- reviewer 통합 리뷰 **🔴 0 / 🟡 3(선택)** — 6축 통과. plan-auditor는 착수 전 본 Phase 정의 봉합 완료 상태(2026-06-07 plan)

## 결정 흐름 (회고 참고용)

- **ComputeDamage 오버로드 vs 공용 시그니처** — 오버로드 채택. 약간의 중복 < 기존 11개 호출처 무변경(회귀 면적 0) + 호출부 타입으로 방향 자명.
- **EnemyAISystem Boss 분기 유지 vs 완전 이관** — 완전 이관. 두 System이 같은 entity latch를 이중 감소/이중 broadcast할 위험 원천 차단. 단일 책임 정합.
- **리스폰 통지 = 즉시 스냅샷 vs 다음 S_Snapshot** — 다음 S_Snapshot(≤100ms). kill-plane 선례 정합 + 틱 내 추가 패킷 순회 할당 회피. S_EnemyAttack.targetCurrentHp(≤0)로 클라가 사망 시점은 이미 인지.
- **characterClass 신규 검증 추가 안 함** — C_CharacterSelect 수신 시점(CharacterSelectHandler)에서 이미 fail-closed. 송신값 출처는 오직 서버 `Stats.Class` — 검증 위치를 입구 한 곳에 유지(이중 검증 분산 회피).
- **봇 성공 게이트 강화** — "보스에게 맞아 HP 감소 관측"이 시나리오 핵심인데 옛 성공 판정엔 없었음 → EnemyAttackCount≥1 의무화. 가짜 양성(리스폰 오인)은 정직한 음성(respawn=False)으로 전환.

## 막혔던 지점 / 이월 (➡️ 다음)

- **reviewer 🟡 3건 선택 이월** — ① 리스폰 teleport가 그 틱 RecordPosition 이후라 position history(~200ms)에 사망 좌표 잔존: lag-comp rewind 이론적 창, 실해 측정 후 판단 ② 봇 RespawnCount getter lock 비대칭(읽기 원자적이라 무해) ③ 테스트 주석 한 줄 명확화
- **클라 연출 미착수 = Phase 05 영역(의도)** — S_EnemyAttack 수신 핸들러/이펙트/HUD HP 연동/원격 직업 표시 전부 Phase 05. 본 Phase에서 03_Client 코드 변경 0 (dll 산출물 복사만)
- WSL2 서버 백그라운드 기동 시 wsl 호출 한 번으로 `(&) ` 백그라운딩하면 세션 종료와 함께 죽음 — run_in_background + `tail -f /dev/null |` 파이프 패턴 재확인

## 학습 일지 후보 키워드

양방향 권위 전투(진실이 서버에 모임) / telegraph = prediction 비대칭의 체감 공정성 보완 / tick 카운터 FSM(헌법 #5 표준) / bump 묶음 전략(재빌드 비용 ∝ bump 횟수) / 화석 주석 같은 commit 정정(false-promise) / 오버로드 = 회귀 면적 0 선택 / 관측 코드도 거짓말한다(가짜 리스폰 — 데이터 모순으로 발본) / 테스트 기대값은 실스탯 직접 참조(drift 방지)

## 다음 Phase

- **Phase 05 — 보스 클라 연출 + 원격 직업 표시** (본 Phase 패킷 소비. telegraph 16/10틱 = 클라 이펙트 타이밍의 진실. CHANGELOG [M] 줄 2건 동승 — Phase 03 UI.unity + 본 Phase v9)
