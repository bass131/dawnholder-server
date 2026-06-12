---
owner: youngho
milestone: M4.9
phase: 02
title: 클래스↔스킬 게이트 + 클래스별 스킬 키 라우팅
status: pending
grade: 복잡
risk: trust-boundary (클래스 자격 검증 서버 권위)
estimated: 3h
domain: cross
---

# Phase 02: 클래스↔스킬 게이트 + 클래스별 스킬 키 라우팅

> **상태**: pending
> **마일스톤**: M4.9
> **등급**: 복잡 (shared+server+client 3 도메인)
> **담당**: shared+server+client Worker(Sonnet) + reviewer
> **의존**: 없음 — Phase 01과 병렬. 단 Phase 03~06의 **선행**(클래스 매핑/키 라우팅이 신규 스킬 토대).

---

## 🎯 목표

"어느 클래스가 어느 스킬을 쓸 수 있는가"를 `98_Shared` 단일 진실로 정의하고, **서버가 클래스 불일치 시전을 거부**(silent drop + cheat-flag)하게 만든다. 동시에 클라가 클래스별로 다른 스킬 키를 송신하도록 라우팅한다. 이 Phase가 끝나면 **전사(Knight)가 Mage 전용 썬더볼트를 시전하면 서버가 드랍하고 클라 입력 단계에서도 막힌다** — M4.8까지 열려 있던 헌법 §3 신뢰 경계 구멍을 봉합한다.

---

## ⏪ 사전 조건

- [ ] M4.8 마감 — SkillId(None=0/Thunderbolt=1), C_SkillUseHandler(HasSelectedClass만 검증), SkillSystem 동작 중
- [ ] PlayerStats에 Class 정보 존재(서버가 caster의 클래스를 알 수 있음) 확인
- [ ] 영호와 **클래스별 스킬 키 매핑 확정** (예: Mage Q=Thunderbolt / E=Teleport, Knight Q=Dash — 키 설계 이 Phase에서 확정)
- [ ] **[Phase 02 첫 액션] enum append wire-safe spike** — SkillId.Dash=2/Teleport=3 빈 append만 → Shared.dll 재빌드 → PacketRoundTrip 테스트 + ProtocolVersion==11 assert로 "bump 불필요" 가정을 초입에 확정(~5분 spike). 깨지면 **즉시 STOP** — plan 재조정(irreversible 깃발 경로). Phase 03/05는 이 spike가 통과한 enum 위에 올라탐.

---

## 📝 작업 내용

> shared(매핑) → server(거부) → client(키 라우팅) 순. 매핑이 단일 진실의 뿌리.

**shared (98_Shared/GameData)**:
- [ ] `SkillCatalog`(또는 동형) 신설 — `SkillId → 요구 CharacterClass` 매핑 단일 진실. **Dash=2 / Teleport=3 자리 미리 포함**해 Phase 03/05가 enum append만 하면 되게 설계 (예: Thunderbolt→Mage, Dash→Knight, Teleport→Mage)
- [ ] 매핑 조회 헬퍼 (예: `bool CanCast(CharacterClass, SkillId)`) — 클라·서버 공용

**server (02_Server)**:
- [ ] `C_SkillUseHandler` 또는 `SkillSystem`에서 **캐스터 클래스 검증** — `SkillCatalog.CanCast(caster.Class, skillId)` 불일치면 **silent drop + cheat-flag 로깅**(헌법 §3, 기존 `[Trust]` 로그 패턴 정합). caster 클래스는 session/PlayerEntity에서 강제(클라가 보낸 값 신뢰 X)
- [ ] 기존 `skillId != Thunderbolt` 하드 분기를 **카탈로그 기반 검증으로 일반화**(Dash/Teleport도 통과하게)
- [ ] **서버 쿨다운 자료구조 단일 확정** — 현 `LastSkillTick` 단일 필드(M4.8 썬더볼트 1개 가정)를 **스킬별 쿨다운**(예: skillId 키 맵 또는 스킬별 필드)으로 전환. Phase 03(Dash)·05(Teleport)가 병렬이라 공유 자원 변경은 **여기서 한 번만** — 03/05는 이 구조에 올라탐(한 스킬 쓰면 다른 스킬도 쿨다운 걸리는 버그 차단).

**client (03_Client)**:
- [ ] `LocalPlayerInput` 클래스별 스킬 키 매핑 — 현재 Q=Thunderbolt 하드코딩(94~115줄)을 **클래스 조회 후 키→skillId 라우팅**으로 교체. Mage면 Q=Thunderbolt/E=Teleport, Knight면 Q=Dash
- [ ] 클라 입력 게이트도 `SkillCatalog.CanCast` 거울 — 자기 클래스가 못 쓰는 스킬 키는 송신 자체를 억제(서버 silent drop과 불일치 방지)

**qa / 테스트**:
- [ ] 클래스 불일치 시전 거부 단위 테스트 (Knight→Thunderbolt drop / Mage→Dash drop, happy: Mage→Thunderbolt 통과)

---

## ✅ 완료 조건 (정량)

- [ ] `dotnet test` green — 기존 회귀 **0**
- [ ] 신규 테스트: Knight→Thunderbolt **silent drop**(cheat-flag 로그) + Mage→Thunderbolt **통과** + (자리 잡힌) Mage→Dash drop
- [ ] 클라: Knight 캐릭터로 Thunderbolt 키 눌러도 **C_SkillUse 송신 안 됨**(입력 차단)
- [ ] 서버: 클래스 불일치 C_SkillUse 수신 시 `[Trust]` 드랍 로그 출력 + mutation 0
- [ ] Shared.dll 재빌드 → `03_Client/Assets/Plugins/` 갱신 + Unity 콘솔 error CS 0

---

## 🧪 테스트

**자동**:
- `SkillCatalogTests` — CanCast 매트릭스(클래스×스킬 전 조합)
- `C_SkillUseHandlerTests` — Knight→Thunderbolt drop / Mage→Thunderbolt happy / 미정의 skillId drop / handshake 미완료 drop

**수동**:
- 2클라: Knight 클라에서 모든 스킬 키 눌러보기 → Thunderbolt 안 나감 확인. Mage 클라에서 Q=Thunderbolt 정상.

---

## 📚 학습 포인트

- **cheat-flag = 신뢰 경계의 핵심 패턴**: 클라가 "Knight인데 Mage 스킬 줘"라고 보내도 서버는 *조용히* 버리고 로깅한다. 에러를 클라에 돌려주지 않는 이유 = 치터에게 "왜 막혔는지" 힌트를 안 주려고. silent drop + 서버 측 로깅이 정석.
- **단일 진실(single source of truth)**: 클래스↔스킬 매핑을 클라/서버 양쪽에 따로 박으면 둘이 어긋나는 순간 "클라는 보냈는데 서버가 버리는" 유령 입력이 생긴다. `98_Shared`에 한 번 정의 → DLL로 양쪽 공유 = 헌법 §4 물리적 강제.
- **클라 게이트 vs 서버 게이트의 역할 분리**: 클라 게이트 = UX(헛입력 방지) + 트래픽 절감. 서버 게이트 = 진짜 보안(헌법 §1, 클라는 못 믿음). 클라 게이트를 뚫어도 서버가 최종 거부 → 클라 게이트는 "있으면 좋은" 거울일 뿐 신뢰 근거 아님.

---

## ⚠️ 함정 / 주의사항

- **trust-boundary 위험 깃발** → 등급 상향. caster 클래스는 **반드시 서버 측 PlayerEntity/session에서 가져옴** — C_SkillUse 페이로드에 클래스를 담아 클라가 보내면 도용 가능(절대 금지).
- 기존 `skillId != Thunderbolt` 하드 분기를 지우고 카탈로그로 일반화할 때, **미정의 skillId(0/4+)는 여전히 drop**돼야 한다(카탈로그에 없으면 거부).
- 키 매핑 변경 시 기존 평타(Attack 액션, Space/좌클릭)와 충돌 안 나게 — 스킬 키는 별개 채널.

---

## ➡️ 다음 Phase

- Phase 03 (Knight Dash 서버) / Phase 05 (Mage Teleport 서버) — 둘 다 이 카탈로그·키 라우팅 위에 올라간다.

---

## 📋 박제 (완료 후 -DONE.md)

- 복잡 등급 → `-DONE.md` 박음 (클래스 게이트 = 헌법 §3 봉합, 사실 박제 + cheat-flag 키워드).

---

## 작업 로그

- 2026-06-10: 계획 작성 (M4.8까지 열려 있던 클래스 게이트 구멍을 Dash/Teleport 도입 계기로 M4.9 회수)
