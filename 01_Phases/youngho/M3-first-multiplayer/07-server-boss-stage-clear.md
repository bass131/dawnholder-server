# Phase 07: 서버 보스 + Stage Clear 트리거

> **상태**: pending
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
