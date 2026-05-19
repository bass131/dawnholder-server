# Phase 07: 서버 보스 + Stage Clear 트리거

> **상태**: server done (5/19) — 클라 UI Phase 08b 후속
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **예상 소요**: 1.5h
> **담당 에이전트**: gameplay

---

## 🎯 목표

우측 zone 보스 placeholder + 사망 시 Stage Clear broadcast. 보스 = EnemyEntity 특수 케이스. 응급 = 1회성 (respawn 안 함).

## ⏪ 사전 조건

- [ ] Phase 06 완료 (Combat 인프라 박힘)

---

## 📝 작업 내용

- [ ] `BossEntity` 신설 — EnemyEntity 상속 또는 isBoss 플래그. 위치 = 맵 우측 zone, HP 100, AI 없음
- [ ] PDL — `S2C_StageClear` 신설 (또는 `S2C_Death`에 `isStageBoss` 플래그)
- [ ] 보스 HP 0 → StageClear broadcast (한 번만, 중복 방지)
- [ ] Boss spawn = 서버 시작 시 1회 (응급 = respawn X)
- [ ] PacketGenerator 재생성 (`--no-manager`) + Shared.dll commit
- [ ] handler / entity 단위 테스트 (보스 사망 시 StageClear 1회만)

## ✅ 완료 조건

- [ ] 우측 zone 보스 spawn (서버 시작 시)
- [ ] 공격 → HP 감소 (Phase 06 흐름 그대로)
- [ ] HP 0 → StageClear broadcast 1회 → 클라 UI 표시 (UI는 Phase 08)
- [ ] 중복 broadcast X (HP 0 후 추가 공격 들어와도 추가 StageClear 없음)
- [ ] handler/entity 단위 테스트 통과

---

## 🧪 테스트

**자동**: BossHandlerTests — 정상 처치, 중복 공격 시 StageClear 1회만
**수동**: 헤드리스 봇 또는 Unity 클라로 보스 처치 후 로그에서 StageClear 1회 확인

---

## 📚 학습 포인트

- **Boss = Enemy 특수 케이스** — 분리할지 통합할지 결정. 응급 = 분리(별 entity), 본 마감 = 통합(EnemyType 필드)
- **StageClear 권위** — 클라가 *보스 죽음 판정* X. 서버가 *판정 + broadcast*, 클라는 패킷 받아 UI 표시만 (헌법 #1)
- **중복 방지 패턴** — `isStageCleared` 플래그 또는 `dispatched = true` flag로 1회 보장

---

## ⚠️ 함정 / 주의사항

- **StageClear 중복 broadcast** — HP 0 직후 추가 공격 도착하면 또 broadcast 가능. flag로 1회 보장
- **Boss respawn 안 시킴** — 응급 1회성. 본 마감엔 cooldown 또는 스테이지 리셋
- **클라가 StageClear 자체 판정 X** — 클라는 패킷 받기만, 자체 HP 추적 X (UI 표시용 HP만, 권위는 서버)

---

## ➡️ 다음 Phase

Phase 08 — 유현 Asset 통합 + 3-zone 시각화 + Stage Clear UI

---

## 작업 로그

- 2026-05-18: pending
- 2026-05-19: **server 5/5 완성** (gameplay 에이전트). 산출물:
  - `GameMap.cs` — `BossSpawnX/Y/MaxHp` 상수 + `SpawnBoss` helper + ctor에서 Boss 1마리(EnemyKind.Boss, (30, 0), HP 100) 추가 spawn. `_stageCleared` flag + `IsStageCleared` getter 신설.
  - `GameMap.ProcessAttack` — Boss HP 0 시 `_stageCleared` flag 체크 + `S_StageClear { bossEntityId }` broadcast 1회 (S_EntityDeath 다음 순서). 이중 안전망 (target Remove + flag) 으로 중복 broadcast 차단.
  - `PDL.xml` — `S_StageClear` ID 15 신설 (additive). **ProtocolVersion v3 유지** (Phase 06이 이미 stale client cutoff 박힘 + 단순 신규 1패킷 → bump 가치 X. Codex 권장 검토 후 v3 유지 결정 — 면담 시간 절약 + HandshakeHandlerTests 변경 0).
  - `02_Server/GameServer.Tests/Network/BossStageClearTests.cs` 신설 — 3건: `Boss_Death_BroadcastsStageClearOnce` / `BossDuplicateAttack_NoExtraStageClear` / `NormalEnemy_Death_NoStageClear`.
  - 회귀 영향: entity id 풀 shift (Normal=1, Boss=2, Player=3) — 기존 6개 테스트 파일의 player id 기대값 갱신 (AttackHandlerTests / MoveIntentHandlerTests / GameSessionRateLimitTests / GameSessionLifecycleTests / BroadcastTests).
  - BroadcastTests.`NewSession_ReceivesActiveEnemyRoster_OnEnter` 갱신 — enemy roster 1마리 → 2마리(Normal + Boss) 검증으로 확장.
  - `dotnet build` PASS 경고 0 / 오류 0. `dotnet test` 170 PASS (기존 167 + 신규 3) / 1 Skip.
- 2026-05-19: **클라 UI는 Phase 08b로 위임**. Unity 측은 v3 클라가 미지 S_StageClear 패킷 자동 silent drop (dispatch 안 박힘) — server-only 박은 본 Phase 산출물은 헤드리스 봇(BossStageClearSmoke.md 명세 → .cs 변환은 Codex 영역)에서 검증 완료 가능.
