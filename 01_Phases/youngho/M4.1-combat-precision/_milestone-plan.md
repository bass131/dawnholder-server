---
owner: youngho
milestone: M4.1
title: Combat Integrity & Portfolio Hardening (Phase 01 Codex 리뷰 + Session 상태 머신 + ClientNet 대칭 + Build 산출물 + Formulas + lag comp)
status: in-progress
grade: 대규모
risk: low
estimated: 10~16h (총합, 6 Phase, Phase 01 ✅ 마감 ~1.5h 제외 = 잔여 8.5~14.5h)
domain: server+shared+client+harness
depends_on: M3.8 Phase 03 (PlayerStats + CharacterClass 박힘) — ✅ 해소 (main `2cd0fbc`)
---

# M4.1 — Combat Integrity & Portfolio Hardening

> **상태**: in-progress (Phase 01 ✅ 마감, Phase 02 진입 대기)
> **시작 예정**: 2026-05-23 (M3.8 마감 후 즉시 진입)
> **마감 목표**: 2026-06-04 (M4.2 진입 6/5 = 6일 여유, 캡스톤 1 발표 6/10 안정화 ↑)
> **사전 조건 (해소)**: M3.8 Phase 03 ✅ PlayerStats 박힘 (`02_Server/GameServer/Combat/PlayerStats.cs` main `2cd0fbc`)

---

## 🎯 마일스톤 목표

**"신뢰도 ≫ 정밀도" 입사 방어 정신**. 옛 plan "Combat Precision" 정신 = lag compensation 같은 *정밀도* 중심 → 본 재구성 = 캡스톤 1 시연 + 입사용 포트폴리오 *방어선* 강화 중심.

**핵심 통찰**: 면접관/시연자가 가장 먼저 의심하는 건 lag compensation의 ms 수치가 아니라 *"이 사람이 만든 게 진짜 작동하나"*. 캐릭터 선택 없이 월드 진입 가능 / 선택한 클래스가 데미지에 반영 X / 클라 framing 검증 비대칭 / 빌드마다 git dirty = *시연 5분 안에 발견되는 구조 결함*. 본 마일스톤 = P0 (신뢰도) → P1 (정밀도) 순서로 박음.

**왜 본 순서가 입사용으로 방어 가능한가** (한 문단):

> 면접/시연 환경에서 P0 결함 한 건이라도 잡히면 *"lag compensation도 같은 수준이겠지"* 일반화가 박힙니다. 반대로 P0 풀세트 봉합 → P1 정밀도 박음 = *"신뢰도 베이스가 있는 사람이 정밀도까지 박았다"* 가닥 박음. lag compensation을 P0보다 먼저 박으면 *"화려한 거 박는데 기본은 안 됐다"* 역효과 위험. M3.8 ★★★ 본인 통찰 "클라 입력 컨트롤 = UX 게이트 ≠ 서버 게이트" 정신 정합 — *기본 신뢰도 = UX의 베이스*.

**우선순위 (P0/P1/P2)**:

- **P0 (면접/시연 신뢰도 즉시 깎는 구조 결함)** — Phase 02·03·04·05 흡수:
  1. 캐릭터 선택 전 월드 입장 가능 (Phase 02)
  2. `C_CharacterSelect` 서버 상태 전이 강제 (Phase 02)
  3. `CharacterClass`/`PlayerStats`가 `PlayerEntity` + 전투 공식 반영 (Phase 05)
  4. ClientNet frame length 검증 부재 (Phase 03)
  5. `dotnet build`/`test`가 Unity `Shared.dll` dirty 만드는 빌드 산출물 (Phase 04)
- **P1 (M4.1 정밀도 직접)** — Phase 05·06 흡수:
  - Formulas.cs 분리 (Phase 05)
  - PlayerStats 기반 damage 적용 (Phase 05)
  - AttackRange 클라/서버 중복 제거 (Phase 06, B1)
  - AABB hitbox (Phase 06)
  - lag compensation 200ms rewind (Phase 06)
- **P2 (M4.2/M4.3 이관)**:
  - 진짜 4맵 분리 (M4.2)
  - enemy/boss AI (M4.3)
  - visual polish, sorting layer, UI theme (M4.3)
  - cheat-flag table, Serilog, runtime config (M4.2/M5+)

---

## 📋 Phase 분해 (6 Phase, 순차)

| # | Phase | 등급 | 도메인 | 예상 | 담당 | P 분류 |
|---|---|---|---|---|---|---|
| 01 | Codex β 크로스 리뷰 + M3 응급 하드코딩 추가 발본 (✅ 마감) | 복잡 (자동 상향) | qa+cross | ~1.5h | 메인 + 본인 Codex | 마감 |
| **02** | **Session State Machine Hardening** (P0-1 + P0-2) | 복잡 | server (+ client wiring 최소) | 2~3h | server SubAgent (Sonnet) | **P0** |
| **03** | **ClientNet Trust Boundary Symmetry** (P0-4) | 보통 | client + shared | 1~2h | client SubAgent + shared SubAgent | **P0** |
| **04** | **Build Artifact Hygiene** (P0-5) | 보통 | harness + infra | 1~2h | 메인 직접 (build script 영역) | **P0** |
| 05 (옛 02) | Formulas.cs 분리 + PlayerStats *진짜* 전투 반영 (P0-3 + P1) | 복잡 (자동 상향, 옛 보통) | shared + server | 2~3h | server SubAgent + shared SubAgent | **P0 + P1** |
| 06 (옛 03) | lag compensation 200ms + AABB hitbox + PDL 4→5 + B1/B3 sweep | 복잡 | server (trust-boundary + irreversible) | 3~5h | server SubAgent (Sonnet) + reviewer Tier 2-A | **P1** |

**총 등급 = 대규모** (마일스톤 자체, 6 Phase 풀세트, ~12~17h, server+shared+client+harness 4 도메인) — 옛 = 복잡 (3 Phase). 사유 = P0 신규 3 Phase + Phase 05/06 자동 상향.

**박제 정합**:
- Phase 02·05·06 = 복잡 = -DONE.md 박음
- Phase 03·04 = 보통 = work-pin + commit message 충분 (단 P0 봉합이라 자동 상향 트리거 ON 가능 — 발견 결함 양에 따라 결정)
- 마일스톤 마감 = M4.1-마감 별 -DONE.md (**대규모 등급 = 5단계 보고 MD/HTML 이중 박음** + ADR-024 cadence false-promise 점검 결과 섹션 의무)

---

## 🔗 의존성 그래프

```
Phase 01 (Codex β 크로스 리뷰) ✅ 마감
   │
   │  발견 결과 = B1/B3 → Phase 06 흡수
   ↓
Phase 02 (Session State Machine) — P0-1 + P0-2 봉합
   │
   │  서버 상태 머신 강제 → Phase 05 PlayerStats 진짜 흡수 진입 정합
   ↓
Phase 03 (ClientNet Trust Boundary Symmetry) — P0-4 봉합
   │
   │  framing 검증 대칭 → Phase 06 PDL 변경 시 클라/서버 동형 안전
   ↓
Phase 04 (Build Artifact Hygiene) — P0-5 봉합
   │
   │  Shared.dll dirty 봉합 → Phase 05/06 잦은 빌드 시 git 노이즈 ↓
   ↓
Phase 05 (Formulas.cs 분리 + PlayerStats 진짜 전투 반영) — P0-3 + P1
   │
   │  Formulas.cs `ComputeDamage(attackerStats, targetStats, baseDamage)` → Phase 06
   │  damage apply 분기에서 호출
   ↓
Phase 06 (lag comp + AABB + PDL bump + B1/B3 sweep) — P1
```

**의존성 사유**:
- Phase 02 *먼저* = P0 신뢰도 베이스 봉합 (다른 P0/P1 위에 박힘)
- Phase 03 = Phase 06 PDL 변경 시 클라/서버 framing 동형 의무 (옛 위치 = Phase 06 직전 박을 가닥 있지만 P0이라 *베이스*에 박음)
- Phase 04 = 옛 Phase 02·03 시점부터 잦은 빌드 = 본 봉합 *베이스*에 박음이 잦은 git 노이즈 ↓
- Phase 05 = P0-3 (PlayerStats 진짜 반영) + P1 (Formulas 분리) 묶음, 옛 Phase 02 정신 + 강화
- Phase 06 = 옛 Phase 03 정신 + B1/B3 sweep 흡수

**병렬 가능 = 없음** (6 Phase 순차, P0 베이스 → P1 정밀도 순서).

---

## ✅ 마일스톤 완료 조건

- [x] Phase 01 = ✅ 마감 (산출물 3건 + DONE.md 박힘, Phase 03 옵션 A 흡수)
- [ ] Phase 02 = `HandshakeHandler.cs`에서 `EnterGameWorld()` 호출 제거 + `CharacterSelectHandler`가 `EnterGameWorld()` 호출 박음 + class 선택 전 `C_MoveIntent`/`C_Attack` drop 또는 disconnect + 단위 테스트 6건+ 통과
- [ ] Phase 03 = `04_ClientNet/ClientSession.cs` 또는 `RecvBuffer.cs` framing 검증 분기 박힘 + `98_Shared/FrameValidator.cs` (또는 helper 재활용) + 단위 테스트 4건+ 통과 + 헤드리스 봇 fuzz 통과
- [ ] Phase 04 = `dotnet build`/`test` 후 `git status -s | grep -E "(Shared.dll|ProjectSettings.asset)"` 빈 출력 (5회 연속 검증) + Shared.dll *실제 변경* 시 dirty OK 회귀 검증
- [ ] Phase 05 = `98_Shared/GameData/Formulas.cs` 신설 + `ComputeDamage(PlayerStats, EnemyStats, int) → int` 순수 함수 + `PlayerEntity.Stats` = `GameSession.Stats` 연결 + `GameMap.ProcessAttack` Formulas 위임 + 단위 테스트 6건+ (전사/원거리 분리 검증 포함)
- [ ] Phase 06 = `PlayerEntity.PositionHistory` ring buffer (4 tick) + `C_Attack.attackerClientTick` 필드 PDL append-only + ProtocolVersion 4→5 bump + `GameMap.ProcessAttack` rewind + AABB hitbox (`Hitbox.cs` + `Intersects` 메서드) + B1/B3 sweep + 단위 테스트 8건+ 통과 + 부하 봇 lag 시뮬 검증
- [ ] M4.1-마감 별 -DONE.md 박음 (**대규모 등급 = 5단계 보고 MD/HTML 이중 박음 의무**) + false-promise 점검 결과 섹션 (ADR-024 cadence)
- [ ] CHANGELOG [H] entry 박음 (PDL 변경 + ProtocolVersion bump + P0 5건 봉합 + 마일스톤 대규모 등급)
- [ ] CONTEXT.md "⏸️ 현재 멈춤 지점" = M4.1 완료 + M4.2 진입 대기로 갱신
- [ ] commit + push + PR 게이트 (사용자 명시 GO 후 머지, --admin bypass 사유 박힘 가닥 — 정유현 영역 침범 commit 가능)

---

## ⚠️ 주의할 약속 (헌법 절대 원칙 충돌 가능 항목)

1. **헌법 #1 (Server Authority)** — Phase 02 서버 상태 머신 강제 = 서버 권위 *강화* (handshake → CharacterSelect → EnterGameWorld 강제 흐름). Phase 05 Formulas.cs는 *서버 권위 판정용*, 클라가 *스탯 hint 표시*는 OK / *권위 판정 호출 X*. Phase 06 lag compensation = 서버 권위 그대로 (rewind는 서버 측만).
2. **헌법 #2 (Protocol is Sacred)** — Phase 02 = **신규 패킷 박지 X 가닥** (사용자 보정 1 정합, `S_CharacterSelectRequired` 같은 새 패킷 = PDL bump + 클라 dispatch 새 표면 위험 회피). 서버는 *기존 패킷 drop/disconnect로 강제*. Phase 06 = `C_Attack.attackerClientTick` 필드 추가 + ProtocolVersion 4→5 bump (M3.8 3→4 박은 후 본 Phase 4→5 별 bump).
3. **헌법 #3 (Trust Boundary)** — Phase 03 ClientNet framing 검증 = 서버/클라 *대칭* (옛 = 서버만, 새 = 둘 다). Phase 06 lag compensation rewind 범위 제한 (≤ 4 tick) silent drop.
4. **헌법 #4 (Shared Code Discipline)** — Phase 03 `98_Shared/FrameValidator.cs` (또는 helper 재활용) 신설 = 양쪽 공유. Phase 05 Formulas.cs + Phase 06 PDL 변경 모두 *Shared.dll commit* 의무 (CHANGELOG 2026-05-17 학습 정합). **Phase 04 = `.gitignore` 옵션 A 위험** (사용자 보정 2 정합 — 옛 Shared.dll 미commit 사고와 충돌 가능, hash 비교/SkipUnchangedFiles 봉합 권장).
5. **헌법 #5 (틱 블로킹 금지)** — Phase 02 상태 머신 검사는 GameSession 측 = tick thread 아님 (network thread + main dispatch). Phase 06 ring buffer 갱신은 tick thread 안에서만, lock 없음.

---

## 📚 학습 포인트 (마일스톤 차원)

- **"신뢰도 ≫ 정밀도" 입사 방어 정신** — 본 마일스톤 핵심. M3.8 ★★★ 본인 통찰 "UX 게이트 = 서버 게이트와 분리" 정합. 면접/시연 환경 결함 영향 = lag comp ms 수치보다 *작동 신뢰도* 베이스 ↑.
- **서버 상태 머신 vs 새 패킷 trade-off** (Phase 02 정신) — 새 패킷(`S_CharacterSelectRequired`) 박으면 PDL bump + 클라 dispatch 새 표면 = scope ↑. 기존 패킷 drop/disconnect 박음 = scope ↓ + 헌법 #3 정합 (untrusted input 차단). 학부생 정신 = "추가보다 차단".
- **클라/서버 framing 검증 대칭** (Phase 03 정신) — 서버만 검증 = 비대칭 = 클라 측 우회 입력 (악성 서버 시뮬) 대응 부족. 헌법 #3 정신 *양쪽 적용*. M2.5 Phase 09 학습 패턴 클라 측 확장.
- **빌드 산출물 위생** (Phase 04 정신) — `Shared.dll` 영구 .gitignore = 미commit 사고 트라우마 재발 위험. hash 비교 / SkipUnchangedFiles = "진짜 변경이면 dirty OK, 같은 소스 재빌드면 dirty X" 정합. 옛 사고 학습 + 본 봉합 = *방어 진화 패턴*.

---

## ➡️ 다음 마일스톤

- **M4.2 — Map Transition** (진짜 4맵 분리 + portal handoff + 클라 측 dispatch + cheat-flag table + Serilog). 캡스톤 1 후반 = 6/5~6/9. 발표 데모 = M4.1 + M4.2 결과 종합.
- **M4.3 — AI + Polish** (enemy AI + boss behavior + jump Y mispredict 봉합 + PvP ADR + 마감 의례). 캡스톤 1 후 7월~10월.

---

## 갱신 이력

- 2026-05-22 — 본 세션 사용자 의논 후 박힘. M3 응급 코드 전수조사 필요 인지 → Phase 01 Codex 크로스 리뷰 흡수. M4 전체 7~9 Phase 추정을 3토막(M4.1/M4.2/M4.3) × 3 Phase로 분할.
- 2026-05-22 — M3.8 Capstone-1 Demo Infrastructure 신설 흡수 갱신. 사전 조건 = "M3.8 Phase 03 마감 (PlayerStats 박힘)" 추가. ProtocolVersion bump 3→4 → 4→5 정정. status `in-progress` → `pending`. 시작 5/22 → 5/27, 마감 6/3 → 6/2.
- 2026-05-23 — M3.8 ✅ 완전 마감 + PR #49 머지 완료 (`2cd0fbc`) 흡수 갱신. Phase 01 분담 정정 (본인 직접 Codex 호출). `/cross-review.md` 슬래시 정정 + memory 2건 갱신 동반. (false-promise 23번째 변종 발본)
- 2026-05-23 — 일정 갱신 (옵션 A). 시작 5/27 → 5/23, 마감 6/2 → 6/1. status `pending` → `in-progress`. M3.8 사전 조건 ✅ 해소 표기.
- 2026-05-23 — M3.8 ★★★ 본인 통찰 2건 흡수 (Phase 03 학습 포인트 + 함정 각 한 줄).
- 2026-05-23 — Phase 01 ✅ 마감 (산출물 3건 + DONE.md). β 발견 B1/B3 Phase 03 옵션 A 흡수.
- 2026-05-23 — **본 plan 풀세트 재구성 (옵션 A' = 풀세트 GO + 두 보정)**. 사용자 가닥 = "포트폴리오 방어선" 정신. 제목 변경 = `Combat Precision` → `Combat Integrity & Portfolio Hardening`. Phase 풀세트 3 → 6 (신규 Phase 02 Session State Machine + Phase 03 ClientNet 대칭 + Phase 04 Build 산출물, 옛 Phase 02·03 → 새 Phase 05·06로 rename). 등급 자동 상향 복잡 → 대규모 (6 Phase + server+shared+client+harness 4 도메인). 마감 6/1 → 6/4 (3일 추가, M4.2 진입 6/5). **보정 1** = Phase 02 신규 패킷(`S_CharacterSelectRequired`) 박지 X, 기존 패킷 drop/disconnect로 강제 (PDL bump + 클라 dispatch 새 표면 회피). **보정 2** = Phase 04 `.gitignore` 옵션 A X, hash 비교/SkipUnchangedFiles 봉합 권장 (옛 Shared.dll 미commit 사고와 충돌 회피).
