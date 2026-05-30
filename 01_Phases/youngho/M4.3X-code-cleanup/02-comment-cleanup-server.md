---
owner: youngho
phase: 02
status: pending
grade: 복잡
summary: 02_Server + 98_Shared 서버측 전체 주석을 §6 기준으로 95% 노이즈 제거(코드 0변경) + §3.3 naming 위반 rename. 빌드/테스트 green 가드
---

# Phase 02: 주석+naming 정리 — 서버측

> **등급**: 복잡 (대량 파일이나 코드 0변경 — 안전)
> **담당**: server SubAgent (workflow 실행)
> **선행**: Phase 01 (§6 기준 확정)

---

## 🎯 목표

`02_Server/`(Network + GameServer + Tests) + `98_Shared/` 서버측의 **주석을 §6 기준으로 정리**한다. 자명한 재진술·역사 박제·폐기된 사고과정을 제거하고, 비자명한 안전 결정 근거(5%)만 남긴다. 동시에 §3.3 naming 위반(`m_` 헝가리안, bare camelCase, `_`-prefix 매개변수)을 rename한다.

**코드 로직은 0 변경** — 주석 제거 + rename만. self-documenting 코드로.

---

## ⏪ 사전 조건

- [ ] **Phase 01 완료** — §6 주석 정책 + naming 규칙 (정리 기준)
- [x] 322 테스트 baseline (정리 후 회귀 0 비교 기준)

---

## 📝 작업 내용

### 주석 정리 (§6 적용 — 코드 0변경)
- [ ] `02_Server/GameServer/` 전체 — God class(GameMap/GameSession 등) 포함, 자명/역사/사고과정 주석 제거
- [ ] `02_Server/Network/` (ServerCore) — socket 인프라 주석 정리
- [ ] `98_Shared/` 서버측 (Protocol/GameData/Formulas/Physics) — 단 PDL.xml 주석은 **보안·프로토콜 결정 근거 비중 높음 → 5% 예외 신중 판단**
- [ ] `02_Server/GameServer.Tests/` — 테스트 주석 정리
- [ ] **5% 예외 보존**: 헌법 함정(`C_Attack` attacker 없음 = 도용방지), tick 스레드 invariant, append-only 등 *안 적으면 사고나는* 것만

### naming 정리 (§3.3 — rename)
- [ ] `m_` 헝가리안 / bare camelCase field → `_camelCase`
- [ ] `_`-prefix 매개변수(`_endPoint` 류) → `camelCase`
- [ ] rename이 직렬화/공개 API 안 깨는지 확인 (서버측은 Inspector 무관)

### 검증
- [ ] `dotnet build Dawnholder.slnx --no-incremental` 0 error/warning
- [ ] `dotnet test --no-build` — **322 그대로 green** (주석/rename은 동작 불변)

---

## ✅ 완료 조건

- [ ] 02_Server + 98_Shared 서버측 주석 노이즈 95% 제거 — EnemyEntity 류가 코드~동일 + 주석 최소
- [ ] §3.3 naming 위반 0 (서버측)
- [ ] **`dotnet test --no-incremental` 322 green (회귀 0)** — 동작 보존 증명
- [ ] git diff가 *주석 삭제 + rename 위주* (로직 줄 변경 0 — review로 확인)
- [ ] 5% 예외가 실제로 비자명 안전 근거인지 reviewer 점검

---

## 🧪 테스트

**자동**: 기존 322 테스트 전부 green (주석/rename = 동작 불변 증명)
**리뷰**: git diff에서 "로직 변경 0, 주석/naming만" 확인 (reviewer)

---

## 📚 학습 포인트

- **주석 삭제는 가장 안전한 리팩토링**: IL에 영향 0 → 테스트가 그대로 통과하면 동작 100% 보존 증명. 구조 리팩토링(04/05)의 위험과 대조.
- **rename의 함정**: 직렬화 필드(`[SerializeField]`)나 공개 API rename은 외부 깨짐 — 서버측은 안전하나 클라(03)는 `[FormerlySerializedAs]` 필요.

---

## ⚠️ 함정 / 주의사항

- **로직 줄을 건드리지 말 것**: 주석 지우다 코드 한 줄 실수로 수정 = 회귀. diff를 "삭제 위주"로 유지.
- **PDL.xml 주석 신중**: 프로토콜 주석엔 보안 결정 근거 多 → 5% 예외 비중 높음. "왜 이 필드 없는지/순서인지"는 보존.
- **workflow 분할**: 파일 多 → 디렉토리별 fan-out. 각 묶음 빌드 가드.
- **DLL churn**: 98_Shared 변경 시 Shared.dll 재생성 → 03_Client Plugins 복사 정합 (단 주석만이면 dll 바이트 거의 동일).

---

## ➡️ 다음 Phase

- Phase 04 — GameMap God class 분리 (깨끗한 주석 위에서 추출)

---

## 📋 박제 (완료 후)

- **복잡 등급** — `02-comment-cleanup-server-DONE.md`.

---

## 작업 로그

- 2026-05-30: 계획 수립 (`/work:plan` 코드베이스 정리)
