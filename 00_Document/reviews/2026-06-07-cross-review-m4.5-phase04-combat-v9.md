# Cross-Review — 2026-06-07 — M4.5 Phase 04 양방향 전투 + v9 bump (마일스톤 마감 전)

## 변경 범위

- **점검 대상**: PR #71 (merge `ef0082d`, 21파일) — 보스 FSM + S_EnemyAttack ID 20 + S_PlayerJoin.characterClass + ProtocolVersion 8→9. 후속 `897a90d` (보스 모션 봉합) 포함, 현 HEAD 기준
- **등급**: 대규모 (위험 깃발: irreversible — v9 bump, M4.5 마일스톤 PR 선행 검증)
- **실행**: α = reviewer SubAgent / β = Codex CLI (본인 별 세션, 프롬프트 분담) / γ = 본 문서

## α — Claude reviewer 결과

🔴 0 / 🟡 1. 축별: §1 🟢 (판정 = 서버 권위 위치 only) / §2 🟢 (append-only + v9 한 묶음 적정 + 옛 "v8 포함" 깨진 약속 정정 확인) / §3 🟢 (S_EnemyAttack 단방향, C_* 신규 0) / §5 🟡 (GenPackets Write 64KB 할당 — 생성기 전역 패턴, Phase 04 신규 아님, M5 부하 단계 풀링 후보) / false-promise 🟢 / 클라 계약 🟢 (telegraph=전원 broadcast vs impact=피격자만 — 2채널 분리로 misfire 없음).

## β — Codex 결과 (1차)

6건: P1×1, P2×3, P3×2 + NO-GO 판정.

1. [P1] BossBehaviorSystem:172 — 사망 HP 송신 후 full 리셋되지만 S_Snapshot에 HP 없음 → 클라 HUD 0 고착 (확신)
2. [P2] BossBehaviorSystem:56 — telegraph 도중 페이즈2 전환 시 잔여 telegraph 미clamp, 주석과 불일치 (확신)
3. [P2] Boss_Animator.controller — Start 0.8s 고정 vs P2 판정 0.5s (확신)
4. [P2] BossFightSmoke:152 — 성공 조건이 리스폰 관측 미요구 (확신)
5. [P3] BossBehaviorTests:497 — 직렬화 음수 HP/attackPattern=255 경계 미검증 (확신)
6. [P3] ClientPacketHandlers:169 — S_PlayerJoin 주석 ID 5 (실제 9) (확신)

## γ 비교 분석

- **양쪽 다 잡음**: 0
- **α만 잡음**: GenPackets 64KB Write 할당 (M5 이월)
- **β만 잡음 — 검증 후 실제 결함 확정**: ① P1 HUD 0 고착 (`UpdateHP` 경로 전수 추적으로 실증 — α는 "사망 HP 순서"까지만 보고 *리스폰 후 복구 경로 부재*를 놓침) ② P3 경계값 테스트 갭 ③ P3 주석 ID 드리프트
- **β만 잡음 — 주석 드리프트만 (동작은 의도)**: P2 telegraph 미clamp — 진행 중 telegraph 단축은 예고 공정성 위반이라 동작 유지가 정답, 주석만 정정
- **β만 잡음 — 기지 이월**: P2 0.5s 정합 (work-pin 이월 기존재)
- **양쪽 통과**: §1/§2/§3 헌법 축, 직렬화 왕복 기본, FSM 무블로킹

## 봉합 (1차) + β 재실측 (2차)

봉합 1차: ① 리스폰 시 HUD를 PlayerStats.ForClass.MaxHp로 복구 (v9 불변 제약 — HP 동기화 패킷은 v10 구조급 이월) ② BossBehaviorSystem 주석 정정 ③ S_EnemyAttack 경계값 왕복 테스트 추가 (21/21) ④ 주석 ID 9 정정.

**β 2차가 봉합 1차의 신규 결함 발견** (Step 4-A 의무 가치 실증, M4.2 γ 9회차 패턴 재현): 같은 콜백 프레임에서 0 → full 덮어쓰기 = 사망 피드백(HP 0 표시) 소실.

봉합 2차: `SceneTransition.PlayRespawnFade(Action onCovered)` — 화면 완전 암전 시점 콜백으로 HUD 복구 이동 + 시작 불가 시 false 반환 → 호출자 즉시 복구 폴백 (0 고착 방지 우선).

## β 재실측 (3차) — 수렴

기능 결함 0. "사망 HP 먼저 표시 → 암전 후 복구" 경로 정합 확인. 잔여 지적 2건은 모두 커밋 제외 금지물 영역 (Cainos 데모씬 trailing whitespace / M_TerrainStylize_Test 오버라이드 — 의도된 미커밋 잔여물, work-pin 박힘). BossBehaviorTests 21/21.

## 결정 권유

🟢 **GO — 마일스톤 마감 진행.** 양쪽 다 잡음 0 + β 단독 결함 전부 봉합 + 재실측 수렴. 남은 게이트 = 사용자 실측 (Step 4-B): 발표 데모 풀 루프 Play에서 사망→리스폰 "HP 0 표시 → 암전 → 부활 시 full" 확인.

## 이월 박힘 (본 리뷰 산출)

- **플레이어 HP 동기화 전용 패킷** (구조급, v10 후보) — "공격 이벤트 패킷 부재"와 한 묶음. 현 표시 미러는 임시
- **BossFightSmoke 리스폰 단언 보강** (qa) — 봇은 HUD 관측 불가라 서버측 리스폰(position/후속 동작) 단언으로
- GenPackets Write 64KB 풀링 (M5 부하 단계)

## 옛 학습 정합

- `false-promise` 변종: 주석 ID 5 드리프트 + telegraph 주석 불일치 — 문서/주석 약속 vs 코드 격차 패턴 2건 봉합
- γ 9회차 패턴 재현: **봉합이 새 결함 도입** (β 2차에서 실증) — Step 4-A 재실측 의무가 두 마일스톤 연속으로 값어치 함
- α/β 상호 보완 실증: α = 헌법/계약 시각 (2채널 분리 정합 확인), β = 데이터 흐름 끝까지 추적 (리스폰 *후* 상태 복구 부재 발견)
