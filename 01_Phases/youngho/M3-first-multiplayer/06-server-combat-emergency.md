# Phase 06: 서버 응급 전투 인프라 (Combat state + 적 placeholder + 공격 패킷)

> **상태**: pending
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **예상 소요**: 2.5h
> **담당 에이전트**: gameplay

---

## 🎯 목표

서버 권위 단순 전투. Codex β 권장 *강한 단순화* — **적 AI 없음, 고정 위치 HP dummy, 공격은 서버 range + cooldown만**. 헌법 #1 서버 권위 + #3 신뢰 경계 *단순화 OK, 위반 X*.

## ⏪ 사전 조건

- [ ] Phase 05 완료 (클라 remote entity registry)

---

## 📝 작업 내용

- [ ] `02_Server/GameServer/Combat/` 폴더 신설
- [ ] `PlayerEntity.cs:11`에 `HP` 필드 추가 (기본값 100)
- [ ] `EnemyEntity` 신설 — 고정 위치 (맵 중간 zone x 좌표), AI 없음, HP 30
- [ ] PDL — `C2S_Attack { uint targetEntityId or direction }`, `S2C_HitResult { uint attacker, uint target, int damage }`, `S2C_EntityHpUpdate { uint entityId, int currentHp, int maxHp }`, `S2C_Death { uint entityId }` 신설
- [ ] 서버 공격 처리:
  - rate-limit (cooldown, 500ms) — 헌법 #3 정합. 초과 시 drop (이미 패턴 Phase 09)
  - 서버 반경 hit 판정 (`dist² < range²`, lag compensation 없음 — 응급)
  - 고정 데미지 10. enemy HP 감소, ≤0 시 death broadcast + entity 제거
- [ ] Enemy spawn = 서버 시작 시 맵 중간 zone에 1마리
- [ ] PacketGenerator 재생성 (`--no-manager`) + Shared.dll commit
- [ ] 핸들러 단위 테스트: happy / invalid (range 밖) / auth / rate-limit 초과 페어

## ✅ 완료 조건

- [ ] 클라 공격 패킷 → 서버 hit 판정 → enemy HP 감소 → broadcast → 클라 표시
- [ ] enemy HP 0 → death broadcast → 클라에서 사라짐
- [ ] rate-limit 위반 (500ms 안에 2회) → 거절 (헌법 #3)
- [ ] 공격 range 밖 → no-hit (damage 0)
- [ ] handler 단위 테스트 페어 통과

---

## 🧪 테스트

**자동**: AttackHandlerTests — happy, out-of-range, rate-limit violation, auth failure
**수동**: Unity 클라 + 서버 = 적 placeholder spawn → 공격 → HP 감소 → death

---

## 📚 학습 포인트

- **Combat state 분리** — `PlayerEntity` HP는 *전투 상태*. 이동 상태와 분리 가치 (Maintenance + 직무 응집도)
- **서버 권위 hit 판정** — 클라 절대 사형 보고 X. 클라 = "I attempted attack X", 서버 = "you hit/missed Y, damage Z"
- **응급 단순화 정신** — `lag compensation` / `정밀 hitbox` / `데미지 공식 풀세트`는 M4(본 마감용). 응급은 *덜 박더라도 권위·신뢰경계는 지킴*
- **`dist² < range²` 패턴** — sqrt 회피 (성능 + 정밀도)

---

## ⚠️ 함정 / 주의사항

- **클라 데미지 직접 계산** → #1 위반. 클라는 시각 표시만 (서버에서 받은 HpUpdate 그대로 표시)
- **rate-limit 누락** → #3 위반 (1초에 1000번 공격 가능)
- **공격 sender 검증 누락** → 다른 플레이어 entityId 도용 공격 가능 (#3 위반)
- **lag compensation 안 한 게 헌법 위반은 X**. 단 면담에서 "본 마감엔 lag comp 박을 것" 메모
- **enemy spawn 시점** — 서버 시작 시 1회 또는 첫 플레이어 접속 시. 응급은 *서버 시작 시 1회*

---

## ➡️ 다음 Phase

Phase 07 — 서버 보스 + Stage Clear

---

## 작업 로그

- 2026-05-18: pending (Codex β 발견 2 = 전투 과소추정, 강한 단순화로 봉합)
