# Phase 09: 데모 리허설 + 헤드리스/수동 체크리스트 + 마지막 fix

> **상태**: pending
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **예상 소요**: 2h
> **담당 에이전트**: qa-sim + client

---

## 🎯 목표

면담 5/20 13:40 직전 풀-쓰루 1회. 알려진 버그 0. Codex β 권장 C 반영 (헤드리스 + 수동 체크리스트).

## ⏪ 사전 조건

- [ ] Phase 08 완료 (Asset 통합 + UI 박힘)

---

## 📝 작업 내용

- [ ] **헤드리스 봇 2개 시나리오 자동화** (`99_Tools/headless-bot/`):
  - connect → handshake → join broadcast 확인
  - move → 보간 부드러움 확인
  - attack → 적 HP 감소 → death 확인
  - boss attack → boss death → StageClear broadcast 확인
  - disconnect → leave broadcast 확인
  - reconnect → initial roster 정상 (기존 entities 다 보임)
- [ ] **수동 체크리스트** (Unity 1 + 서버):
  - [ ] join (두 클라 접속, 서로 보임)
  - [ ] leave (한 클라 종료, 다른 클라에서 despawn)
  - [ ] move (두 클라 같이 움직임, 보간 부드러움)
  - [ ] attack (적 placeholder 처치)
  - [ ] boss death (보스 처치 + StageClear UI)
  - [ ] reconnect (한 클라 끊고 재접속 — initial roster 정상)
- [ ] **알려진 버그 0** — 발견 시 즉시 fix 또는 *데모에서 회피 가능한지* 판단
- [ ] **발표 데모 스크립트 짧게** — 어디 클릭, 어떤 순서, 무엇을 보일지 (1페이지)
- [ ] **`dotnet test` green** 최종 확인 (M3 baseline)

## ✅ 완료 조건

- [ ] 6 수동 시나리오 모두 통과
- [ ] 헤드리스 봇 자동 시나리오 1회 통과
- [ ] `dotnet test` green
- [ ] Unity Editor 풀-쓰루 1회 깔끔 (알려진 버그 0)
- [ ] 발표 스크립트 1페이지 박힘

---

## 🧪 테스트

**자동**: 헤드리스 봇 2개 시나리오 (`99_Tools/headless-bot/`)
**수동**: 위 6 시나리오 + 풀-쓰루 1회

---

## 📚 학습 포인트

- **리허설의 가치** — 첫 시연이 발표 당일이면 *reality 폭발*. 1시간 리허설이 면담 패배 막음
- **헤드리스 봇 + 수동 결합** — 자동은 *protocol 정합* 검증, 수동은 *시각/체감* 검증
- **응급 모드라도 체크리스트 박제** = 안전망. Phase 11 cleanup 정신 = "잘 끝낼수록 다음 진입 빠름"

---

## ⚠️ 함정 / 주의사항

- **리허설 시간 부족** — Phase 08까지 시간 늘면 1h라도 박을 것. *데모 당일 첫 시연 = 면담 패배*
- **헤드리스 봇 작성에 시간 폭발** — 응급은 *수동 체크리스트만으로 OK*. 헤드리스는 *기존 코드 활용*
- **알려진 버그 0 vs fix 시간** — 작은 버그 (UI 깜빡임 등)는 데모에서 회피, 큰 버그 (broadcast 깨짐)는 즉시 fix
- **발표 스크립트 누락** — 즉흥은 *학부생에게 위험*. 1페이지라도 박을 것

---

## ➡️ 다음 Phase

(M3 마감) → 면담 5/20 → 면담 후 1순위 = **M5 Persistence (DB 영속화)** *또는* **M3 다듬기** (캡스톤 1까지 6/10)

---

## 작업 로그

- 2026-05-18: pending (γ 방식 3회차 Codex β 권장 C 반영)
