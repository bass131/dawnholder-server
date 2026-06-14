---
owner: youngho
milestone: M4.15
phase: _milestone (Energy Bolt + 히트박스 + freeze + 텔레포트 4방향/v14)
title: M4.15 마일스톤 마감 — Energy Bolt 다듬기 + 전 스킬 히트박스 재정비 + freeze 제거 + 텔레포트 4방향/v14
status: done
completed: 2026-06-14
grade: 대규모
summary: M4.15 완전 마감. 영호 Play-test 기반 4 워크스트림 — (A) 투사체 일정 속도(travelTicks 상한 artifact 제거→ceil(2D÷2.0), 클라 무변경 캐스케이드) (B) 히트박스 정사각→비정사각 X/Y 분리(Mage X11/Y1.0, Knight X1.5/Y1.0, Thunderbolt Y3→1.5, Dash Y1.5→1.0 — 층 분리) (C) freeze 적용 제거(ApplyFreeze 2곳, 인프라 보존) (D) 텔레포트 4방향/v14(C_SkillUse.verticalDir append + ProtocolVersion v13→v14, 영호 Option B GO / 수직=지형 인식 발판 snap / 도착 이펙트 S_SkillCast 수신시점 arming + 캐릭터 글루). 최종 텔레포트 튜닝 X3.5·Y3.0·이펙트Y-0.5(영호 Play 2차). 회귀 WSL2 580/0/5 + 봇 16/16(fresh) + Unity 0err + reviewer P02·P07 🔴0 + plan-auditor 2회 GO. commit 11건 미push(PR=영호 GO 대기, v14 비가역+Shared.dll co-review). 시각판=_milestone-DONE.html.
---

# M4.15 마일스톤 마감: Energy Bolt + 히트박스 + freeze + 텔레포트 4방향/v14

**기간/구성**: 2026-06-14 단일 세션. Phase 01~10 (워크스트림 A/B/C 6 Phase + D 텔레포트 4 Phase). 영호 Play-test 2회 반영. commit 11건, 브랜치 `feature/m4.15-energybolt-skill-range` **미push (PR = 영호 GO 대기)**.
**시각 보고서**: [`_milestone-DONE.html`](_milestone-DONE.html) — 워크스트림 카드 + Phase 타임라인 + before/after 값 (대규모 HTML 박제, ADR-031)

---

## 5단계 보고

- **무엇을 만들었나** — 4 워크스트림. (A) Mage 에너지 볼트 투사체 일정 속도화. (B) 전 스킬 히트박스 X/Y 분리(정사각→비정사각, 층 분리). (C) Mage 평타·Thunderbolt freeze 제거(인프라 보존). (D) 텔레포트 4방향(위/아래) + 지형 인식 수직 + 도착 이펙트 결정론/글루 + Protocol v14.
- **왜 필요한가** — 영호 직접 Play에서 발견한 거칠음: 투사체가 멀수록 순간이동처럼 빨라짐(상한이 결과 속도를 폭증), 정사각 히트박스라 위아래 층 다 맞음, 메이플 에너지 볼트엔 없는 freeze, 텔레포트가 좌우만·고정거리로 바닥 파고듦·도착 이펙트 엉뚱한 위치.
- **어떻게 만들었나** — (A) `travelTicks=max(2,ceil(2D÷2.0))` 상한 제거(P04), 클라는 서버 역산이라 무변경 자동 정합(P05). (B) `GetAttackHitbox` 정사각`(half,half)`→클래스별`(halfX,halfY)`(P02). (C) `ApplyFreeze` 호출 2곳 제거, 필드/가드/Boss면역 인프라 보존(P03). (D) `C_SkillUse.verticalDir`(byte) append + v14(P06) → 서버 whitelist 정규화 3진 + `TryFindVerticalTeleportTarget` 지형 쿼리(P07·P09) → 클라 위/아래 입력 + 이펙트 arming을 S_SkillCast 수신시점으로 + 캐릭터 parenting(P08·P09).
- **테스트 결과** — WSL2 `dotnet test` 580/0/5, 헤드리스 봇 16/16(fresh 재검), Windows+WSL2 빌드 0/0, Unity 컴파일 0err, reviewer P02·P07 헌법 🔴0, plan-auditor 2회 GO(🔴5 전부 사전 봉합).
- **다음 스텝** — PR 게이트(v14 비가역 + Shared.dll co-review = 영호 명시 GO). 후속 후보: 수직 텔레포트 terrain-aware 정밀화, 히트박스·거리 추가 Play 튜닝, 미래 빙결 스킬(보존 인프라 재사용).

---

## TL;DR

영호 Play-test로 드러난 Mage/스킬 거칠음 4종을 정비했다. 투사체 속도 폭증(상한 artifact)·정사각 히트박스(층 오판정)·불필요한 freeze는 서버에서 봉합(wire v13 불변), 텔레포트는 4방향+지형인식+이펙트 결정론으로 재작업하며 `C_SkillUse.verticalDir` 필드 추가로 ProtocolVersion v14 bump(영호 Option B GO). WSL2 580/0/5 + 봇 16/16 + Unity 0err로 거동 검증. 미push — PR은 영호 GO 대기.

## 박제 사실 (워크스트림별 최종값)

- **(A) 투사체**: `ProjectileSpeedPerTick=2.0`, `MinTravelTicks=2`, `MaxTravelTicks`(10) 제거. 클라 `ProjectileVisual` 무변경.
- **(B) 히트박스**: Mage `X=11/Y=1.0`, Knight `X=1.5/Y=1.0`, Thunderbolt `X=13/Y=1.5`(3→1.5), Dash `X=2.5/Y=1.0`(1.5→1.0). `GetAttackHitbox` 클래스별 비정사각.
- **(C) freeze**: `ApplyFreeze` production 호출 0. `FrozenUntilTick`/`ApplyFreeze` 메서드/`EnemyAISystem` 가드/Boss 면역 보존. `StunTicks` 주석화.
- **(D) 텔레포트**: `TeleportDistance=3.5`(수평), `TeleportVerticalRange=3.0`(수직 발판 탐지), `TeleportEffectYOffset=-0.5`. `verticalDir` 0=수평/1=위/2=아래(whitelist). `ProtocolVersion.Current=14`.

## AC 검증 결과

```
# WSL2 (ADR-029) — rsync ~/dawnholder-poc → build → test
$ ~/.dotnet/dotnet build Dawnholder.slnx
  Build succeeded. 0 Warning(s) / 0 Error(s)
$ ~/.dotnet/dotnet test Dawnholder.slnx --no-build
  Passed! - Failed: 0, Passed: 580, Skipped: 5, Total: 585

# 헤드리스 봇 16 시나리오 (run_bot_regression.sh)
  연속런: 14 success=True, BossFight/HpSync/Freeze success=False (보스상태 누적 flaky)
  fresh 단독 재검(run_bot_fresh_recheck FreezeSmoke HpSyncSmoke BossFightSmoke):
    FreezeSmoke: success=True / HpSyncSmoke: success=True / BossFightSmoke: success=True
  → 16/16 (carry-over "연속 FAIL ≠ 회귀, fresh 단독 재검이 판정")

# Unity 클라 컴파일 (메인 MCP)
  reimport(LocalPlayerInput/LocalPlayerMovement/SkillCastHandler) + RunCommand probe
  → scriptCompilationFailed=False (0 err)

# 프로토콜
  ProtocolVersion.Current == 14 (C_SkillUse.verticalDir append-only, 기존 필드 오프셋 불변)
```

## 결정 흐름 (회고 참고용)

- **방향 채널 (D)**: Option A(facing 바이트 재활용, no-bump) vs **Option B(전용 verticalDir 필드, v14 bump)** → 영호 B 선택(의미 분리 — facing은 좌우 스프라이트, verticalDir이 상하). 비가역 감수.
- **수직 텔레포트**: 거리 기반 MVP(옵션②, clamp+물리 resolve) → Play에서 바닥 파고듦(-1.05) → **지형 인식(옵션①)** 으로 전환(발판 탐지 snap, 최대 사거리 안이면 이동, 없으면 이펙트만).
- **도착 이펙트 타이밍**: 송신 시점 arming(레이스 — 텔레포트 반영 전 스냅에 콜백 새어 옛 위치 발동) → **S_SkillCast 수신 시점 arming**(원격 모델 정합, 도착지 발동).
- **도착 이펙트 위치**: `EffectAnchor.ResolvePosition`(방향성 +오프셋·flipX 미러로 옆에 찍힘) → **캐릭터 transform 자식 localPosition 직접 지정**(글루).
- **freeze**: 통째 제거 vs 인프라 보존 → **호출만 제거, 인프라 보존**(미래 빙결 스킬 = 한 줄 부활).

## 막혔던 지점 / 이월

- **verticalDir 3진 정규화 함정**: facing의 2진 패턴(`==1?1:-1`) 모방 시 2(아래)가 0(수평)으로 뭉개짐 → whitelist 술어(`is 1 or 2`) + 경계값{3,99,255} 테스트 (plan-auditor 사전 봉합).
- **broadcast early-return 함정**: 이동 불가 분기에서 `return false` 하면 S_SkillCast 누락 → "이펙트 안 뜸"을 서버에서 재현. 무조건 broadcast 후 return true.
- **봇 연속런 flaky**: BossFight/HpSync/Freeze 연속 FAIL = 보스상태 누적 → fresh 단독 재검으로 판정(전례 정합).
- **이월**: 수직 텔레포트 terrain-aware 정밀화 / 히트박스·거리 임시 시작값 추가 튜닝 / 미래 빙결 스킬.

## 학습 일지 후보 키워드

- 상한(clamp)이 결과 변수(속도)를 폭증시키는 숨은 비선형 — 캡은 입력이 아니라 결과를 봐야
- 신뢰 경계 정규화의 정의역 형태(2진 vs 3진) — whitelist 술어 + 경계값 테스트
- 클라 이펙트 타이밍 = 정보 출처(S_SkillCast 수신 시점) 의존, 송신 시점 arming은 레이스
- parenting(transform 자식) = 위치 추적 / EffectAnchor 방향성 앵커 vs 중심 글루 구분
- append-only 프로토콜 진화 + ProtocolVersion bump 규율
- "연속 FAIL ≠ 회귀" — 공유 서버 상태 누적 flaky의 fresh 단독 재검 판정

## 다음 마일스톤

- PR 머지(영호 GO) 후 M4.16+ — 후속 밸런싱/스킬 디테일 또는 영호 우선순위.

---

## 📦 Phase 커밋 맵

| Phase | 워크스트림 | 커밋 |
|---|---|---|
| 01 baseline + 시작값 | 메타 | `5beda1f` |
| 02 히트박스 X/Y 분리 | B (reviewer🔴0) | `baff561` |
| 03 freeze 제거 | C | `40b6290` |
| 04 투사체 서버 모델 | A | `a0a1589` |
| 05 투사체 클라 정합 | A (무변경) | — |
| 06 Protocol v14 verticalDir | D (irreversible) | `f9cb574` |
| 07 서버 4방향 텔레포트 | D (trust-boundary·Opus) | `1161e3c` |
| 08 클라 입력 + 출발 이펙트 | D | `66f1e27` |
| 09 지형 수직 + 도착 이펙트 폴리시 | D (Play 2차) | `10ea28b` + `ed2bd3b` |
| 10 봇 정합 + 마감 | D/qa | `ac46760` + 마감 docs |

> 재계획 docs: `ad9c77c`.
