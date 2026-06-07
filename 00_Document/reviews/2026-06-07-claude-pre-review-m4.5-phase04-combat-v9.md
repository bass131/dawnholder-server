# Pre-Review for Codex β — 2026-06-07 — M4.5 Phase 04 양방향 전투 + v9 bump 묶음 (마일스톤 마감 전 재검증)

## 변경 범위

- **점검 대상**: PR #71 (merge commit `ef0082d`) — main에 이미 머지됨. 21파일
- **후속 포함**: 현 브랜치 `feature/m4.5-06-regression-and-close`의 `897a90d` (ComputeBossAnimState 우선순위 Death > Attack > Hit 변경 + 테스트 2개)
- **등급**: 대규모 (위험 깃발: irreversible — ProtocolVersion 8→9 bump, M4.5 유일 bump)
- **핵심 파일**:
  - `02_Server/GameServer/Maps/Systems/BossBehaviorSystem.cs` — 보스 FSM (쿨다운 40/24틱 → telegraph 16/10틱 → AABB 판정 → 플레이어 사망/리스폰)
  - `02_Server/GameServer/Combat/CombatConstants.cs` + `EnemyEntity.cs` — 보스 상수 6종 + EnemyStats.Attack
  - `98_Shared/Protocol/`: PDL.xml + GenPackets.cs (S_EnemyAttack ID 20 신설 + S_PlayerJoin.characterClass byte append) + ProtocolVersion 8→9
  - `99_Tools/headless-bot/Scenarios/BossFightSmoke.cs` 신설
- **diff 요약**: 맞기만 하던 보스가 스스로 공격. 서버 권위 FSM이 telegraph 예고를 S_EntityState(animState=Attack)로 전원 broadcast, 16/10틱 후 보스 중심 AABB ∩ 플레이어 권위 위치 판정, 히트 시 S_EnemyAttack(데미지/잔여HP/패턴) 발송. 사망 → 스폰 재배치 + HP full. characterClass는 늦은 합류자 원격 직업 표시용 append.

## α (Claude reviewer) 결과 요약

🔴 0 / 🟡 1 / 나머지 🟢 — 마일스톤 마감 GO 무리 없음 판정.

| 축 | 판정 | 비고 |
|---|---|---|
| §1 Server Authority | 🟢 | 판정 = player.Position(권위) only, 데미지 = Formulas 단일 공식, 클라 계산 0줄 |
| §2 Protocol Sacred | 🟢 | ID 20 + characterClass 모두 append-only, v9 한 묶음 적정, 옛 "v8 포함" 깨진 약속 정정 확인 |
| §3 Trust Boundary | 🟢 | S_EnemyAttack = 서버→클라 단방향, C_* 신규 0, characterClass = 서버 entity cast (echo 아님) |
| §5 No Blocking | 🟡 | GenPackets Write()가 패킷당 new byte[65535] — **생성기 전역 패턴 (Phase 04 신규 아님)**, M5 부하 단계 풀링 후보 |
| false-promise | 🟢 | BossBehaviorTests 7항목이 문서 약속을 코드로 검증 (ProtocolVersion==9 assert 포함) |
| 클라 계약 정합 | 🟢 | 2채널 분리 (telegraph = 전원 S_EntityState / impact = 피격자 시만 S_EnemyAttack) — misfire 없음, 사망 HP 순서 정합 |

## Codex β 점검 가닥 (본인 직접 호출 시 참고)

- 헌법 §1~§5 위반 여부 (특히 trust-boundary — 보스 전투 경로에 클라 입력 유입 0인지)
- 보스 FSM 엣지: 페이즈 2 전환이 telegraph *도중* 일어날 때 카운터 정합 / 보스 사망 틱과 telegraph 겹침 / 플레이어 전원 사망 시 FSM 진행
- PDL append-only + ProtocolVersion 9 정합 (GenPackets 재생성 drift 여부)
- S_EnemyAttack 직렬화 왕복 (음수 HP 케이스 포함)
- 897a90d 후속: Attack > Hit 우선순위 변경이 일반 적 경로(EnemyAISystem)에 새는지 (보스 한정이어야 함)
- 옛 사고 패턴 잠복: false-promise 변종 / 부정 조건 분기(!= X) 함정 / M3 응급 하드코딩 잔존

## 본인 Codex 호출 명령어 (별 세션 터미널에서)

```bash
# Phase 04 머지 commit 검토 (주 대상)
codex review --commit ef0082dfd73e212c2f501ce638f98bbbc4a88e50

# (선택) 현 브랜치 후속분 — 897a90d 보스 모션 봉합
codex review --base main
```

자료 입력은 본인이 직접 — 본 MD를 첨부하거나 prompt에 핵심만 박기.

## 회귀 실측 현황 (β 입력용 참고)

- WSL2 클린빌드 0/0 + dotnet test 419/0/4 + 봇 7 시나리오 전부 PASS (2026-06-07, 세션23)
- 캐비앗: 보스 무리스폰 설계라 보스 시나리오 2종은 같은 서버에서 연달아 불가 (fresh 서버 필요)
- 보스 모션 Start→End 봉합 Play 실측 통과 (P1 정합, P2는 이월)
