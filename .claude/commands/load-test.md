---
description: 헤드리스 봇 부하 테스트 시나리오 실행 + 리포트
argument-hint: <scenario-name> <bot-count> [duration-seconds]
---

부하 테스트 실행. 파라미터: **$ARGUMENTS**

`qa-sim` 서브에이전트에게 위임하세요. 브리프:

1. `tools/headless-bot/Scenarios/`에 시나리오 파일 존재 확인. 없으면
   생성할지 또는 가장 가까운 기존 시나리오를 쓸지 사용자에게 질문.
2. dev 포트에 로컬 서버가 떠 있는지 확인. 없으면 사용자에게 시작 요청
   (또는 `dotnet run --project server/GameServer`로 시작 제안).
3. 주어진 duration 동안 N개 봇으로 시나리오 실행.
4. 수집: 서버 tick time (p50/p95/p99), packets/sec, 메모리, 에러 로그,
   disconnect 발생.
5. 리포트 작성:
   - 예산 대비 pass/fail (tick p99 < 10ms, 에러 0, 누수 0).
   - 프로파일러 데이터가 있으면 hot path top 3.
   - 발견된 이상에 대한 owner agent 추천.
6. Tear down: 모든 봇 프로세스 kill, 서버가 idle로 돌아갔는지 확인.

버그를 발견해도 게임 소스 절대 수정 금지. 최소 repro 테스트만 핸드오프.
