---
description: 헤드리스 봇 부하 테스트 시나리오 실행 + 리포트
argument-hint: <scenario-name> <bot-count> [duration-seconds]
---

부하 테스트 실행. 파라미터: **$ARGUMENTS**

---

### 등급 판정

- 시나리오 실행 = **보통** 등급 (1 도메인 × 테스트만, 게임 코드 R only)
- 새 시나리오 작성 동반 = **복잡** 등급 (시나리오 코드 작성 + 실행)

---

### 작업 흐름

`qa` SubAgent에게 위임 ([`../../agents/qa.md`](../../agents/qa.md)).

브리프:

1. **시나리오 파일 존재 확인**
   ```
   99_Tools/headless-bot/Scenarios/<scenario-name>.cs
   ```
   - 없으면 사용자에게 *생성할지 / 기존 가장 가까운 시나리오 쓸지* 질문

2. **dev 포트 로컬 서버 떠 있는지 확인**
   - 없으면 사용자에게 시작 요청 또는 `dotnet run --project 02_Server/GameServer` 시작 제안

3. **봇 N개로 시나리오 실행**
   - 주어진 duration 동안 (기본 60초)

4. **수집 지표**
   - 서버 tick time (p50/p95/**p99**)
   - packets/sec
   - 메모리 (RSS / GC heap)
   - 에러 로그 수
   - disconnect 발생 수

5. **리포트 작성**
   ```
   📊 부하 테스트 리포트 — <scenario> × <N> bots
   ─────────────────────────────────────────
   
   예산 대비:
     - tick p99: <X ms> (예산: 10ms) <PASS/FAIL>
     - 에러 수: <N> (예산: 0) <PASS/FAIL>
     - 메모리 누수: <증가율 %/분> <PASS/FAIL>
   
   Hot path (있으면 top 3):
     1. <함수> (<누적 ms>)
     ...
   
   이상 발견:
     - <시나리오 의도와 다른 동작 발견>
     - owner agent 추천: <server / shared / client>
   ```

6. **Tear down**
   - 모든 봇 프로세스 kill
   - 서버 idle 복귀 확인

---

### 자동 발동 Hook

- **`circuit-breaker.sh`**: 봇 시나리오 안 fuzzing 반복은 정당한 반복 — false positive 위험. Bash 제외 매처라 발동 X (Phase 03 함정 봉합 결과)
- **`risk-detector.sh`**: 게임 코드 *수정 X* — 깃발 발동 안 함

---

### Reviewer 호출 조건부

- 시나리오 *실행만* = 게임 코드 변경 0 → Reviewer 호출 X
- 새 시나리오 작성 = `99_Tools/` 변경 = 조건부 호출 (등급 ≥ 보통 충족)

---

### 절대 금지

- **버그 발견해도 게임 소스 수정 X** — `qa` SubAgent는 게임 코드 R only (헌법 §4 정합)
- 발견 시 최소 repro 테스트만 작성 + 적절한 SubAgent에 핸드오프 (server / shared / client)

---

### 옛 슬래시와 차이

- **옛 `/work:load-test`**: `qa-sim` SubAgent 위임 (이름 옛 명칭)
- **새 `/work:load-test`**: `qa` SubAgent (이름 일관, 옛 qa-sim에서 rename — Phase 02). 책임 동일
