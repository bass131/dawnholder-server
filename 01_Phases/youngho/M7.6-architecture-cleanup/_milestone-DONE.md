---
owner: youngho
milestone: M7.6
phase: milestone-closeout
title: Architecture Cleanup — 아키텍처 논리 감사 8건 정리 (M8 영속화 전 토대)
status: done
grade: 대규모
summary: UltraCode 34에이전트 감사(38발견→13통과)에서 영호가 GO한 리팩토링 8건을 동작 불변 원칙으로 정리. preM8 2건(QuestRegistry 분리·치트 #if DEBUG)이 M8 스키마 오염 차단. 서버 6건(P01~P05+#8서버) = WSL2 663/0/5(657→+6)·봇16/16·reviewer🔴0 검증. 클라 2건(#8클라 사망페이드 S_PlayerHp 이동·#6 EffectSpawnService 통합) = 컴파일0err·reviewer🔴0·영호 Play 육안 통과. ProtocolVersion 불변(와이어 0변경). SendBuffer는 영호 제동("0참조≠삭제")으로 삭제 아닌 정직라벨로 전환(GC최적화 미wiring=JobQueue동급). 12커밋 로컬·미push(PR/push=영호 GO 대기).
---

# M7.6 마일스톤 마감 — Architecture Cleanup (감사 8건 정리)

**마감 일자**: 2026-06-19 (loop-driven 1세션 — 서버 자율분 + 영호 의논(SendBuffer)·Unity 육안 검증)
**Phase 수**: 6 (P01 QuestRegistry / P02 치트게이트 / P03 PartyFlow / P04 게이트선언화+사망진입점 / P05 deadcode+봇 / P06 이펙트통합)
**등급**: 대규모 (3+ 도메인 [서버 게임플레이·네트워킹 + 클라 연출 + 도구/문서] + trust-boundary 다수 + 300줄+)
**브랜치**: `feature/m7.6-arch-cleanup` (12커밋, 로컬·미push — PR/push는 영호 GO 게이트)

---

## TL;DR

UltraCode 34에이전트 감사(`w5oioyjmf`, 38발견→13통과)가 짚은 **책임이 엉뚱한 계층에 굳은 자국 8건**을, M8(DB 영속화) 진입 전 *동작 불변*으로 바로잡았다. 게임 외부 행동은 1비트도 안 바꾸고 구조만 정리 — 각 Phase를 WSL2 회귀(테스트 수 비감소)·reviewer🔴0·봇 회귀로 증명했다. preM8 2건(QuestRegistry를 PartyRegistry에서 분리 / 치트 서브시스템 `#if DEBUG` 빌드 배제)이 M8 스키마·신뢰경계 오염을 사전 차단하는 핵심. 클라 2건(사망 페이드를 권위 HP 채널로 이동 / 이펙트 스폰 4중복제 통합)은 영호가 Unity Play 육안으로 시각 동치 확인(bucket-b).

**이번 마일스톤의 교훈 = SendBuffer 사건**: 감사가 "dead code 삭제"로 묶은 걸 AI가 그대로 추천했으나, 영호가 *"패킷 어떻게 보내는데? 0참조라고 삭제 아니지?"*로 제동 → 실측 결과 **버려진 쓰레기가 아니라 안 끼운 GC 최적화**(Rookiss 패턴)였고, 삭제 대신 정직 라벨로 전환했다.

| # | 발견 | Phase | 커밋 | 도메인 |
|---|---|---|---|---|
| #1 | QuestRegistry를 PartyRegistry에서 분리 | P01 | `5bbaf88` | 서버 |
| #2 | 치트 서브시스템 `#if DEBUG` 빌드 배제 | P02 | `c4864fa` | 서버(trust) |
| #3 | 파티 오케스트레이션 GameSession → PartyFlow | P03 | `1eb7ef1` | 서버 |
| #5 | 전제조건 게이트 선언화 (RequiresSelectedClass) | P04 | `592ac5d` | 서버(trust) |
| #4 | dead PriorityQueue 삭제 + JobQueue/SendBuffer 정직라벨 | P05 | `a1b159c`·`3f8aed2` | 서버/문서 |
| #7 | 봇 6개 standalone 수정 (하네스 drift) | P05 | `e5db185` | 도구 |
| #8 | HandlePlayerDeath 추출(서버) + 사망 페이드 S_PlayerHp(클라) | P04·P06 | `eadf47d`·`1c9770d` | 서버+클라 |
| #6 | 이펙트 스폰 4중복제 → EffectSpawnService 통합 | P06 | `08e627d` | 클라 |

토대 2커밋: `78204e6`(감사 리포트) `e9f0590`(마일스톤 플랜, plan-auditor CONDITIONAL GO 봉합).

---

## 5단계 보고

**① 무엇을 만들었나** — 감사 통과 8건을 동작 불변으로 정리: QuestRegistry 분리 · 치트 빌드배제 · PartyFlow 추출 · 게이트 선언화 · 사망 진입점(서버 `GameMap.HandlePlayerDeath` + 클라 권위 HP 채널) · dead code/false-promise 정정 · 봇 16/16 복원 · 이펙트 스폰 통합.

**② 왜 필요한가** — M8 = DB 영속화. 책임이 엉뚱한 계층에 있으면 영속화 경계가 더러워진다. 퀘스트 진행도가 PartyRegistry에 얹혀 있으면 M8 스키마 오염(#1), 치트가 Release에 살아있으면 신뢰경계 구멍(#2), 사망이 특정 데미지 소스 부산물이면 영속화 훅을 못 건다(#8). preM8 2건(#1·#2)이 깨끗한 영속화 경계 확보의 핵심.

**③ 어떻게 만들었나** — Opus/Sonnet Worker 위임 → 각 Phase WSL2 회귀 + reviewer (trust-boundary/대규모 = Opus). 실측 정정 2건: ① work-pin 가정 "BossStates 사망/부활 중복"은 *사실이 아니었다* — ApplyBossAttack은 ApplyMeleeDamage에 위임만 해 사망/부활 site는 단일. ② death-guard(GameMap:236-239)는 즉시부활로 *도달 불가능한 방어 코드* → 범위 밖 잔류(plan-auditor 코드 검증). #8 클라↔서버 시너지: 서버가 S_PlayerHp(0)→(MaxHp) 순으로 보내므로 클라 페이드를 권위 HP 채널(currentHp≤0)에서 도출 = 모든 사망 소스에 robust. #6은 flip 규칙·TeleportArrive 위치·warn-once 비트 동치로 4 사이트 통합.

**④ 테스트 결과** — 서버: WSL2 **663 passed / 0 failed / 5 skipped**(baseline 657 → +6) · 봇 **16/16** · reviewer🔴0 · P02 Release drop 별도 증명(CheatBuildGateTests). 클라(bucket-b): Unity 강제 재컴파일 **CS 에러 0**(MCP) · reviewer🔴0 · **영호 Play 육안 "전부 이상 무"**. ProtocolVersion **불변**.

**⑤ 다음 스텝** — PR/push(영호 GO, 단독 normal merge) → M8 DB 영속화 재개(`feature/m8-persistence`) → (백로그) M8후 부하 프로파일링+최적화 패스(memory `future-perf-profiling-pass`).

---

## AC 검증 결과

마일스톤 완료 조건(`_milestone-plan.md`) 대비 실측:

- **8건 감사 항목 적용** — ✅ 8/8 (커밋 표 참조). 적용 불가 0.
- **WSL2 회귀 green (테스트 수 657 비감소 + build 0err)** — ✅ `~/.dotnet/dotnet build Dawnholder.slnx` = Build succeeded / 0 Error. `dotnet test --no-build` = **`Passed! - Failed: 0, Passed: 663, Skipped: 5, Total: 668`** (657→663, +6: #5 게이트테스트 +1, #8 HandlePlayerDeathTests +5).
- **게임 동작 회귀 0 (봇 시나리오 통과)** — ✅ `run_bot_regression.sh` = **`REGRESSION SUMMARY: PASS=16 FAIL=0`** (시나리오당 fresh 서버, HpSyncSmoke가 사망→부활 e2e 증명).
- **reviewer 🔴 0 (각 Phase)** — ✅ 서버 6건 + 클라 2건 전부 🔴0.
- **Protocol.Version 불변** — ✅ 와이어 포맷 0 변경 (전부 서버/클라 내부 구조 정리).
- **preM8 2건 완료 (M8 경계 정리)** — ✅ QuestRegistry 분리(#1) + 치트 #if DEBUG(#2).
- **P02 위반 봉합 별도 증명** — ✅ CheatBuildGateTests가 Release 구성에서 C_CheatCommand 미등록(unknown PacketID drop) 확인. "회귀 green ≠ 위반 봉합" 함정 회피.
- **클라 2건 (bucket-b)** — ✅ Unity MCP 강제 재컴파일 CS 에러 0 + EffectSpawnService 타입 resolve. 영호 Play 육안: 사망 페이드 1회 / 대쉬 좌우 flip / 텔레포트 출발·도착 위치 / 보스·슬라임·골렘 피격 이펙트 = "전부 이상 무". 이펙트 프리팹 8중 7 존재(썬더볼트 캐스터 시전VFX만 의도적 미배치 — pre-existing, 적 낙뢰 본체는 별개·존재).

---

## 결정 흐름

**SendBuffer 사건 (이번 마일스톤 핵심 의논)**:
1. 감사 #4가 JobQueue/PriorityQueue/SendBuffer를 "dead code 삭제"로 묶음. PriorityQueue(0참조+0테스트+잠재버그)는 삭제, SendBuffer도 같은 묶음으로 AI가 "삭제" 추천.
2. **영호 제동**: *"패킷 어떻게 보내는데? 그냥 안 쓰니까 삭제 이런 건 아니지?"* — 미참조 코드를 갈래 분류 없이 지우려던 것을 멈춤.
3. **실측**: 송신은 `Session.cs`가 패킷당 `new byte[]` → `_sendQueue` → `SendAsync(BufferList)` 직접 경로(SendBuffer 미경유). SendBuffer(청크 재사용 = GC 압력 최소화, Rookiss/한국MMO 패턴)는 *안 끼운 최적화 참조 구현* = JobQueue 동급.
4. **결론**: 삭제 X → 정직 라벨 전환(`3f8aed2`, 02_Server/CLAUDE.md + 양쪽 SendBuffer.cs 헤더 + PacketFormat.cs 주석 4곳). 교훈 = memory `sendbuffer-not-dead-code-gc-optimization`("0참조≠삭제, 갈래 4종 분류 먼저"). 실제 wiring은 M8후 perf 백로그.

**최적화 철학 의논**: 영호 "지금까지 최적화 신경 안 쓴 거냐" → 정정. 아키텍처는 perf 의식(액터/lock0·Flyweight 틱-new0·RecvBuffer 링버퍼·gather-write 송신·코드생성 직렬화). 의도적 연기 = "측정 후 병목만"(Knuth, GSP-09 §9.9). 갭 = 부하 프로파일링 데이터 부재 → M8후 perf 패스 백로그 박제(영호 승인).

**#8 분리 판단**: 서버 추출(권위 사망/부활, AI 자율) vs 클라 페이드(시각, bucket-b). 클라↔서버 패킷 순서(S_PlayerHp 0→MaxHp)가 권위 채널 도출을 가능케 함.

**append-only 준수**: 감사 리포트(`reviews/`)·옛 ADR 동결. 정정은 새 기록으로.

---

## 학습 일지 후보 키워드

(knowledge 캐시 승격 후보 — 자율 박제 X, 영호 승인 후)

- **"0참조 ≠ 삭제"** — 미참조 코드 갈래 4종(버려진쓰레기/버그대안/의도된미wiring/숨은사용) 분류 먼저. ①②만 삭제 안전. (✅ memory 박제됨: `sendbuffer-not-dead-code-gc-optimization`)
- **권위 채널 일원화** — lifecycle 사건(사망)은 lifecycle 권위 채널(S_PlayerHp)에서 도출. 특정 데미지 소스 부산물 X → DoT/함정/투사체에 robust.
- **동작 불변 리팩토링 = 위치만 옮기고 시점 불변** — 원자적 부활(같은 함수 내 Hp 0→MaxHp)이 death-guard 도달성을 보존. 시점 쪼개면 동작 깨짐.
- **회귀 green ≠ 위반 봉합** — 헌법 위반 봉합 Phase는 위반 제거를 *별도* 정량 증명(P02 Release drop). 회귀는 동작 불변만 증명.
- **transient vs durable 상태 소유권** — Velocity/OnGround는 물리 스텝이 소유 → 진입점 직접 출력 검증은 물리 덮기 전 캡처.
- **perf 철학** — "측정 후 병목만"(Knuth/GSP-09). 아키텍처 perf 의식 ≠ 무신경. (✅ memory: `future-perf-profiling-pass`)

---

## 잔여 / 범위 밖

- HitResultHandler도 같은 "Load+Instantiate+warn" 패턴 보유(LightningStrike/투사체/대쉬 임팩트) — 감사 #6이 SkillCastHandler만 지목해 이번 범위 밖. EffectSpawnService 합류 후속 후보.
- PacketFormat.cs `Write()` 템플릿 내 emit되는 주석(SendBuffer 언급)은 wiring 시점에 참이 되므로 그대로 — perf 백로그서 함께 갱신.
- 01_Phases frozen -DONE 깨진링크(pre-existing append-only) 미수정.
