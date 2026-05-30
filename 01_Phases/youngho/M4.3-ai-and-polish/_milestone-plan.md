---
owner: youngho
milestone: M4.3
title: Enemy AI + Boss + 애니메이션 상태머신 + Polish (발표용)
status: planned
grade: 대규모
risk: irreversible
estimated: 20~30h (총합, 7 Phase — 애니 상태머신 재편 2026-05-30)
domain: server+shared+client
---

# M4.3 — Enemy AI + Boss Behavior + Polish (발표용)

> **상태**: planned — 2026-05-29 `/work:plan M4.3`로 확정 (placeholder → 본격 분해)
> **시작**: 2026-05-29
> **목표 마감**: 캡스톤 1 발표(2026-06-10) 직전 — 발표 데모에 enemy AI + boss 포함

---

## 🎯 마일스톤 목표

M3에서 응급으로 박은 **고정 위치 더미 enemy/boss를 살아 움직이게** 만든다 — patrol/chase AI + 보스 다단 attack 패턴. 여기에 **움직임 체감 polish**(클래스별 이동속도 + 점프 mispredict 봉합 + reconcile 부드러움)와 **RemotePlayer 외관 봉합**(Animator/prefab)을 더해 **캡스톤 1 발표 데모를 화려하게** 만드는 것이 목표.

### ⚠️ 이 마일스톤은 "M4 마감"이 아니다 (2026-05-29 의논)

M4.3는 **발표용 polish 마일스톤**이지 M4 전체 종료가 아니다. 보안 hardening(cheat-flag/Serilog/PvP ADR)을 통째로 뒤로 미뤘기 때문에, **5단계 마감 의례 + M4 전체 종합 보고는 박지 않는다**. M4.3는 PR 머지 + work-pin 갱신 수준으로 가볍게 닫는다. 진짜 M4 마감 의례는 보안까지 끝난 끝물에.

---

## 📋 Phase 분해 (7개 — 애니 상태머신 재편 2026-05-30)

> **2026-05-30 재편**: 사용자 결정으로 **애니메이션 상태머신(메이플 스타일 — Idle/Walk/Jump/Attack/Hit/Death)** 을 LocalPlayer/RemotePlayer/Enemy 셋에 통일 도입. 기존 08(enemy 클라)을 **08a(프로토콜+서버)/08b(클라 구조)** 로 분리, 11(RemotePlayer 외관)을 **"애니 외관 완성(3객체 6상태)"** 으로 확대. 09는 boss attack을 animState 채널에 정합. → 6 Phase에서 7 Phase로.

| # | Phase | 등급 | 도메인 | 예상 | risk |
|---|---|---|---|---|---|
| 07✅ | enemy AI 서버 (FSM + tick 루프 + S_EntityState) — **merged (PR #56)** | 복잡 | server+shared | — | irreversible (PDL 6→7 완료) |
| 08a | 애니 상태머신 프로토콜+서버 (AnimState enum + animState 필드 append + 서버 상태결정 broadcast) | 복잡 | shared+server | 3~4h | irreversible (PDL 7→8) |
| 08b | 애니 상태머신 클라 구조 (IMotionState + AnimatorDriver + 소스 3종 + enemy 위치 보간) | 복잡 | client | 3~4h | unity-asset |
| 09 | boss behavior (다단 attack + 페이즈 1/2 + S_EnemyAttack + HUD mock 제거 + attack animState 연동) | 복잡 | server+shared+client | 4~6h | irreversible (S_EnemyAttack — Version은 08a의 8에 포함) |
| 10 | 움직임 체감 polish (β10 MoveSpeed dead + jump Y mispredict + reconcile drift) | 복잡 | shared+server+client | 2~3h | — |
| 11 | 애니 외관 완성 (Animator 6상태 클립 × 3객체 + RemotePlayer prefab 정합) | 복잡 | client (본인 외관 **critical path**) | 4~6h | unity-asset |
| 12 | M4.3 회귀 테스트 + 가벼운 마감 (5단계 보고 X) | 보통 | qa+메타 | 1~2h | — |

**총 등급 = 대규모** (마일스톤 자체 — 애니 상태머신이 shared+server+client 3도메인 관통 + PDL bump). irreversible 깃발(PDL 7→8) + unity-asset(Animator) 동시. reviewer Tier 2-A + 신중한 append-only + (큰 PR 머지 전) `/cross-review` 권장.

> **PDL bump 정책 (2026-05-30 애니 재편)**: M4.3 발표 데모를 한 PR로 묶음 머지 가정 → 08a `animState` 필드 추가(S_Snapshot/S_EntityState) + 09 `S_EnemyAttack` 추가를 묶어 **`ProtocolVersion` 7→8 한 번만 bump** (둘 다 Version 8). 07의 6→7은 PR #56에서 이미 머지 완료. 애니 상태머신(08a/08b)과 boss(09)를 *별도 PR로 분리* 머지할 경우에만 09에서 8→9 추가. M3 boss 선례(같은 PR 연속 additive = bump 1회) 정합.

---

## 🔗 의존성 그래프

```
Phase 07✅ (enemy AI 서버 — merged)
   │
   ↓
Phase 08a (애니 프로토콜+서버 — animState 결정·송신)
   │
   ↓
Phase 08b (애니 클라 구조 — AnimatorDriver로 렌더 + enemy 보간)
   │
   ├──────────────┬──────────────┐
   ↓              ↓              ↓
Phase 09       Phase 11       (Phase 10 독립)
(boss + attack  (애니 외관 —
 animState)      본인 Animator 6상태 클립,
                 08b driver 계약 위에 셋업)

Phase 10 (움직임 polish)  ←── 08·09와 독립 (다른 코드 경로)

Phase 12 (회귀 + 마감) ←── 08a~11 전부 완료 후
```

**병렬 가능**: Phase 09 ↔ 11 (boss 서버 로직 vs 본인 Animator 에셋, 영역 다름) / Phase 10 독립. **순차 필수**: 08a→08b (프로토콜 먼저, 클라가 받음), 08b→11 (driver 계약 위에 클립 셋업). 11은 본인 에셋 critical path라 08b 끝나는 대로 병렬 착수 권장.

---

## ✅ 마일스톤 완료 조건

- [ ] enemy Normal patrol/chase FSM 동작 (서버 권위, tick thread — 헌법 #5)
- [ ] enemy 위치가 클라 화면에서 보간되어 움직임 (Play 실측)
- [ ] boss 다단 attack 패턴 + 페이즈 1/2 전환 (HP 임계 기준)
- [ ] 적→플레이어 데미지 = 서버 권위 (헌법 #1), 클라는 표시만
- [ ] 클래스별 이동속도 체감 (Warrior 4 / Ranger 6), 점프 Y mispredict 0, reconcile 부드러움
- [ ] **애니 상태머신**: 서버 animState 권위 결정·broadcast → 클라 `AnimatorDriver` 렌더 (전략 패턴, `IMotionState` 3종 — Local/Remote/Enemy)
- [ ] **3객체 6상태**(Idle/Walk/Jump/Attack/Hit/Death) Play 동작 + RemotePlayer prefab 1개 정합
- [ ] `dotnet test` green (회귀 0 + 신규 테스트 — `AnimStateTests` 등)
- [ ] `ProtocolVersion` 7→8 bump (08a animState 필드 + 09 S_EnemyAttack, 한 PR 묶음 둘 다 v8, append-only, ID 재사용 0). 07의 6→7은 PR #56 머지 완료
- [ ] CHANGELOG entry ([M] — enemy AI 도입 + 패킷 2개 추가, 모든 팀원 영향)
- [ ] (적 MoveSpeed 보수적 — target rewind 미적용 어긋남 회피. 정밀 전투는 M4.4)
- [ ] 캡스톤 1 발표 데모 흐름 정상 (마을 → 사냥터 적 처치 → 보스방 패턴전 → 엔딩)

---

## 🚫 이번에 명시적으로 뺀 것 (M4.4+ 이월)

- **보안 hardening**: cheat-flag table + Serilog + PvP 지원 ADR + β7 reconnect 죽은 세션 + β9/β4 (LOW) → 별도 보안 마일스톤(M4.4 가칭)
- **🔴 정밀 전투(M4.4) — MAX effort 재검토 발견**: **target rewind** (`EnemyEntity` position history 추가 + `ProcessAttack`에서 target도 `GetPositionAtTick`으로 rewind). 적이 움직이면 "클라 본 위치(보간 150ms 지연)"와 "서버 판정 위치"가 어긋나 조준-판정 빗맞음. M4.3는 적 MoveSpeed 보수적으로 *회피*만 → M4.4에서 근본 봉합. (M4.1 이월 "target도 rewind"가 enemy 이동 도입으로 *필수*가 됨)
- **하네스 잡일**: 봇 portal const 공유 헬퍼 + PendingSpawn EditMode 테스트 + 하네스 문서 sweep + β1 PDL explicit-id ADR
- **M4.1 이월**: target rewind + capsule hitbox + 04_ClientNet.Tests + 봇 lag 종단간

---

## ➡️ 발표 후 로드맵 (2026-05-29 의논 — 큰 마일스톤들)

```
M4.3 (지금)  발표용 enemy AI + boss + 움직임/외관 polish
   ↓ [캡스톤 1 발표 2026-06-10]
M4.4(가칭)   보안 hardening (cheat-flag + Serilog + PvP ADR + γ10 잔여)  ← 이번에 미룬 것
M?  컨텐츠     퀘스트 + 컷신 + 인벤토리/아이템 (★ 런타임 메모리 휘발 버전, DB X)
M?  외관       애니메이터 + Sprite + UI 본격 작업
M5  영속화     DB 추가 → 인벤토리/아이템/캐릭터 영속 + 재접속 복원
M?  외부연결   하마치 통한 실제 외부 연결 테스트 (마무리)
```

> ⚠️ 위 로드맵은 **발표 후 PRD 재정합 대상**. 기존 PRD의 M5 영속화/M6 길드/M7 거점과 합쳐 순서·번호를 다시 박아야 함. 인벤토리/아이템은 "휘발 버전(컨텐츠) → DB 영속화(M5)" 2단계로 분리 (서버 권위는 휘발 버전부터 사수 — 헌법 #1).

---

## 갱신 이력

- 2026-05-22 — placeholder 박힘 (M4.1 plan 시점, M4 3토막 분할 정합)
- 2026-05-29 — **본격 분해 확정** (`/work:plan M4.3`). placeholder의 AI+polish+마감 3 Phase → 6 Phase로 확장. enemy AI를 서버/클라 2 Phase로 분리(3도메인+PDL bump), Phase 12를 마감 의례→회귀 테스트로 격하, 보안 통째 M4.4 이월, 발표 후 로드맵 구체화.
- 2026-05-29 — **MAX effort 재검토 봉합** (plan-auditor GO 후 코드 직접 감사). ① 🔴 target rewind 누락 발견 — 적 이동 시 lag comp 비대칭(조준-판정 어긋남) → M4.3는 적 MoveSpeed 보수적 회피 + M4.4 근본 봉합 이월(사용자 결정). ② PDL bump 2회→1회(한 PR 묶음, 보스 선례 정합). ③ Phase 09에 HUD mock 제거 명시(클라 피격 인프라는 이미 준비됨 확인). ④ 보스 공격 telegraph 권장(보스→플레이어 판정 비대칭).
- 2026-05-30 — **애니메이션 상태머신 재편** (사용자 결정, `/work:plan`). 발표 데모에 메이플 스타일 풀 상태머신(Idle/Walk/Jump/Attack/Hit/Death) 도입. **A안 채택**(서버 권위 `animState` byte 통일 — 원격/적 클라 구현이 "byte 읽기"로 통일, 전략 패턴). 기존 08(enemy 클라)→**08a(프로토콜+서버)/08b(클라 구조)** 분리, 11(RemotePlayer 외관)→**애니 외관 완성(3객체 6상태)** 확대. PDL 7→8(`animState` 필드 append + S_EnemyAttack). 6→7 Phase, 마일스톤 등급 복잡→**대규모**. 클라는 `IMotionState`/`AnimatorDriver` 전략 패턴 + enemy 보간은 기존 `RemoteEntity` 컴포넌트 재사용(종속 최소 — 사용자 의논). 본인 Animator 6상태 클립이 발표 critical path.
