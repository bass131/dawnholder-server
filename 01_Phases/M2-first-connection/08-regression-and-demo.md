# Phase 08: 회귀 안전망 + 데모 영상 + p99 측정

> **상태**: pending
> **마일스톤**: M2 First Connection
> **예상 소요**: 2시간
> **담당 에이전트**: qa-sim

---

## 🎯 목표

M2 도달을 **증명**한다. 자동 회귀 시나리오 + 1분 데모 영상 + 서버 tick p99 측정. PRD 성공 기준 일부(tick p99 < 10ms) 달성 보고. 캡스톤 1 옵션 B fallback이 안전하게 손에 들어옴.

---

## ⏪ 사전 조건

- [ ] Phase 07 완료 (중력 + 점프 동작, M2 기능 셋 완성)

---

## 📝 작업 내용

- [ ] **headless-bot 시나리오** (`99_Tools/headless-bot/Scenarios/M2BasicMovement.cs`):
    - Connect → S_EnterMap 대기 → intent 시퀀스 1000개(좌/우/점프 섞기) 전송 → 1초 후 leave.
    - 봇 측에서도 Shared/Physics.Step으로 자체 위치 시뮬 → 종료 시점 서버 snapshot과 위치 일치 확인.
    - 100회 반복 안정성 (한 번도 desync/disconnect 없음).
- [ ] **서버 측 메트릭** (`02_Server/GameServer/Loop/TickMetrics.cs`):
    - 매 tick 소요 시간 히스토그램 (1ms bucket).
    - 종료 시 또는 30초마다 콘솔에 `tick p50 / p95 / p99 / max` 한 줄 출력.
    - 봇 시나리오 + 1명 Unity 클라 환경에서 측정 → p99 < 10ms 목표.
- [ ] **데모 영상**:
    - 시나리오 스크립트: spawn → 좌우 이동 (5s) → 연속 점프 (5s) → cheat 시도 (강제 텔레포트, 즉시 보정 — 화면에 콘솔 띄워서 snap 로그 보이게) → 종료. 총 ~60s.
    - Unity Recorder 패키지 또는 OBS로 캡처. mp4 또는 webm.
    - 영상 파일은 git ignore (저장은 노션 또는 로컬). 경로는 `-DONE.md`에 박음.
- [ ] **/work:review 실행**:
    - 헌법/ADR/구조 위반 자동 점검.
    - 0건이어야 통과.

---

## ✅ 완료 조건

- [ ] `dotnet test` 전체 통과 (M1 회귀 + M2 신규)
- [ ] headless-bot 시나리오 100회 반복 안정 (한 번도 실패 X)
- [ ] tick p99 < 10ms (PRD 성공 기준)
- [ ] 60초 데모 영상 파일 존재 — 좌우 + 점프 + cheat 보정이 시각적으로 명확
- [ ] `/work:review` 위반 0건
- [ ] `08-regression-and-demo-DONE.md` 작성 + Post-flight 게이트 통과

---

## 🧪 테스트

**자동 테스트:**
- `99_Tools/headless-bot/` CI-friendly 모드 (콘솔 출력 + exit code).
- `GameServer.Tests/Loop/TickMetricsTests.cs` — 메트릭 계산 검증.

**수동 테스트:**
- 데모 영상 재생 확인 (마이크/배경 잡음 없는지, 화면 가독성)
- 봇 + Unity 클라 동시 실행 시 서버 안정

---

## 📚 학습 포인트

- **회귀 안전망의 가치**: M2 끝난 뒤 M3 작업하다 M2가 깨져도 자동 감지.
- **p99의 의미**: 평균(p50)이 아니라 *느린 1%*가 사용자 체감을 결정. 게임 서버는 tail latency가 핵심 KPI.
- **데모 영상의 위력**: 면접관 30초 시청 → "이거 만들 줄 안다" 검증. README보다 강함.
- **bot 자동화 패턴**: M8 부하 테스트의 기초. 1대 봇이 1000대로 확장.
- **/work:review의 역할**: 사람 리뷰어 대신 헌법/ADR을 기계적으로 강제 — 1인 개발의 안전망.

---

## ⚠️ 함정 / 주의사항

- 봇이 너무 빠르게 패킷 보내면 Phase 04 rate-limit 트리거 → 정상 페이스 (50ms 간격 권장).
- 봇 측 Physics.Step과 서버 Physics.Step이 *같은 인자*면 같은 결과. 입력 시퀀스 미세 차이 주의.
- p99 측정은 환경 의존 — 노트북 + 디버거 attach 상태면 부정확. Release 빌드 + 디버거 떼고 측정.
- 데모 영상에 본인 디스코드/개인 정보 노출 주의. 깨끗한 작업창에서 캡처.
- `/work:review`가 잡아내는 위반은 fix 후 재실행. "넘기고 다음 Phase 가자" 함정 차단 (헌법 자체 위반).

---

## ➡️ 다음 Phase

- **M2 완료** → M3 First Multiplayer(두 명 같은 맵에 보이기) 진입.
- 그 전에 `/journal:phase M2` 권유 (Phase 단위 회고는 본인 페이스).

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
