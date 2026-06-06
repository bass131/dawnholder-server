---
owner: youngho
milestone: M4.3
phase: milestone-closeout
title: AI + Polish — 마일스톤 경량 마감
status: done
completed: 2026-06-06
grade: 복잡
summary: M4.3 경량 마감 (PR #62 → main 954e028). Enemy AI 서버 FSM(07) + 애니 상태머신 풀세트(08a 프로토콜 v8 / 08b 클라 구조 / 11 외관+wiring 5종) + 이동감 polish(10 입력 큐 FIFO / 10b 점프 버퍼). 09 보스는 마일스톤 재편(2026-06-06)으로 M4.5 이월, prefab 연결+Play 실측은 M4.4-05 흡수. 회귀 풀 green(빌드 0/0 + 테스트 349/0/4skip + 봇 6/6 + ProtocolVersion 8 불변). 5단계 보고 X — 발표 데모 풀 리허설은 M4.5 마감이 진짜.
---

# M4.3 — AI + Polish (경량 마감)

**마감 일자**: 2026-06-06 (PR #62 머지, main `954e028`)
**Phase 수**: 7 마감 + 1 이월 (07/08a/08b/10/10b/11/12 ✅, 09 보스 → M4.5)
**등급**: 복잡 (마일스톤 마감 의례 + PR 머지 irreversible 깃발 — M4.2 정합. 보고는 경량, 5단계 의도적 생략)

---

## TL;DR

고정 더미였던 enemy가 patrol/chase FSM으로 살아 움직이고(07), 서버 권위 `AnimState`가 프로토콜(v8)을 타고 클라 Animator까지 흐르는 풀 체인이 완성됐다(08a→08b→11). 이동감은 rubber-band(10, 입력 큐 one-per-tick FIFO)와 점프 snap(10b, 서버 jump buffer) 두 근본 결함을 서버 쪽에서 봉합. **2026-06-06 마일스톤 재편**으로 09 보스 행동은 M4.5로, prefab 연결+Play 실측은 M4.4-05로 이월하고 본 마일스톤은 경량 마감 — 지형·직업 수요가 M4.3 입자를 초과해서 M4.4(world-and-player)/M4.5(content-and-boss)로 분리한 결정이 핵심(plan-auditor GO).

---

## Phase 박제 요약

| Phase | 제목 | 핵심 | 마감 |
|---|---|---|---|
| 07 | Enemy AI 서버 | patrol/chase FSM + `S_EntityState`(ID 19) + **v6→7** + 5초 respawn | ✅ -DONE 참조 |
| 08a | 애니 상태 프로토콜+서버 | `AnimState` enum + snapshot append + **v7→8** + 서버 권위 상태결정 + latch 8틱 | ✅ `47b59d5` |
| 08b | 애니 클라 구조 | `IMotionState` + `AnimatorDriver` + 소스 3종 + enemy 보간 재사용 | ✅ `c22c6d3` |
| 09 | 보스 행동 | — **M4.5-04 이월** (마일스톤 재편) | ⏭️ |
| 10 | 이동감 — rubber-band | 서버 입력 큐 bounded FIFO(cap 6) + 틱당 Physics.Step 1회 + ack=적용 시점 | ✅ `8ad8840` |
| 10b | 이동감 — 점프 snap | 착지 직전 점프 보관 jump buffer(서버) | ✅ `324dfb3` |
| 11 | 애니 외관 완성 | 본인 5종 클립 제작 + 자산 머지(PR #58~60, `b4ff1b2` 221파일) + **Animator wiring 5종**(`2940f2f` — AttackVariant 랜덤/directed 진입) | ✅ -DONE 참조 |
| 12 | 회귀 + 경량 마감 | 회귀 풀 green + CHANGELOG [M] + PR #62 머지 | ✅ 본 문서 |

**머지 이력**: PR #58(08b+10+10b+아트) / #59(유현 타일맵) / #60(Phase 11 자산 1차) / **#62(마감 — b4ff1b2+2940f2f+재편+회귀)**. 전부 사용자 GO 게이트 + admin bypass 절차(솔로 운영 사유 코멘트).

---

## AC 검증 결과

Phase 12 회귀 (2026-06-06 세션12 실측):

- `dotnet build Dawnholder.slnx --no-incremental` → **경고 0 / 오류 0** (경과 4.4s)
- `dotnet test --no-build` → **349 통과 / 0 실패 / 4 skip** (skip 4건 = 기존 장기 통합 시나리오: MapTransition_TenRuns / Hundred_runs / LagSim 2종 — M4.3 무관 기존 박제)
- 헤드리스 봇 **6 시나리오 전부 PASS** (로컬 서버 기동 실측): smoke / M2BasicMovement(desync 0.00) / MultiRosterSmoke / EmergencyCombatSmoke(rate-limit drop 확인) / BossStageClearSmoke(stage clear + 중복 억제) / EnemyAiSmoke(patrol+chase 동작)
- `ProtocolVersion.Current == 8` 확인 (07: 6→7, 08a: 7→8. M4.4 전체 bump 0 예정, 다음 bump는 M4.5-04 8→9 유일)
- 서버 tick 메트릭 정상 (p99 ≤ 2.56ms, 20 TPS 50ms 예산 대비 여유)

---

## 결정 흐름 (회고 참고용)

- **마일스톤 재편 = 입자 초과 시 분리** — 지형(언덕·단차·공중·상호작용)+직업 전반 분리+몬스터 prefab+UI 수요가 M4.3 입자를 초과 → M4.3는 잔여물만 경량 마감, M4.4/M4.5 신설 (plan-auditor 조건부 GO → 봉합 후 GO).
- **마감 무게도 trade-off** — 모든 마일스톤에 5단계 보고를 박지 않는다. 재편으로 입자가 줄었으면 마감 의례도 같이 줄이는 게 정합 (헌법 등급별 보고 정신).
- **admin bypass 절차 재검증** — 정유현 장기 부재 솔로 운영에서 PR 사유 코멘트 + 사용자 GO + 환경변수 절차가 PR #60에 이어 #62에서도 무사고 (보안 키워드 literal은 body에 안 박음).
- **"fallback 회귀 0"이 기존 미연결 값의 실연결을 가릴 수 있음** — 의도된 정정(5.0→4/6)은 plan에 명시적으로 박기 (plan-auditor 학습, M4.4-04 적용 예정).

---

## 이월 명시

- **M4.4-05**: prefab 연결 + Play 실측 + Knight 점프체인/공격랜덤 관측 (Phase 11 -DONE 박제 — Art prefab 단독 조작 불가가 정상)
- **M4.5-04**: 보스 행동(옛 09) + `S_PlayerJoin` characterClass append (**8→9 유일 bump**)
- **의도된 Missing 2건**: BossRoom/HuntingGround 배경(본인 새 컨셉 예정) + Gameplay.unity 죽은 오브젝트 2개
- 백로그(target rewind/보안 hardening/NetworkService SRP/.editorconfig 등)는 `_milestone-plan.md` 참조

---

## 학습 일지 후보 키워드

마일스톤 입자 초과와 재편 판단 기준 / 마감 의례 무게의 trade-off / Any State 전이 지속 평가 vs directed 진입 / 서버 입력 큐 bounded FIFO와 ack 시점 계약 / 시각 전용 랜덤(AttackVariant)과 서버 권위의 경계 / admin bypass 절차의 정당 사유 박제

---

## 다음 마일스톤

- **M4.4 — world-and-player** (타일맵 지형 충돌 bake + 직업 조작 분리). 첫 블로커 = 본인+유현 타일맵 레이어 약속(Tilemap_Solid/Tilemap_OneWay 분리).
