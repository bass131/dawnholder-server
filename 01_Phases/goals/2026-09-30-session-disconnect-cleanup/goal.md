# S3 — 서버 종료 오류 경로의 자원 정리 보장

상태: 구현·독립 검증 완료, [PR #143](https://github.com/bass131/dawnholder-server/pull/143) 통합 대기. branch `fix/session-disconnect-cleanup`, 구현·테스트 commit `217d87a`. 최신 main `a3c4e15e7d655511ced06bd1303360761413c5ab` 기반의 독립 목표이며 [S2 PR #142](https://github.com/bass131/dawnholder-server/pull/142)의 로컬 독립 검증·계약 검토 완료 후 진행한다. 앞선 미병합 소스는 이 브랜치에 포함하지 않는다. 사용자는 2026-09-30 종료 알림 예외 시 **정리 보장 후 기존처럼 예외 전파**를 선택했다. 순수 동작불변 리팩토링과 구분한 좁은 오류 경로 보강이다. PR은 최종 보고서 확인·사용자 승인 전 병합하지 않는다.

## 목표와 선택한 설계

서버 `Network.Session.Disconnect`가 닫힘 플래그를 세운 뒤 endpoint 조회/종료 callback 예외로 socket·송신 큐 정리를 건너뛰는 경우를 막는다. 연결된 테스트 소켓과 throwing callback에서 열린 소켓·잔여 큐·두 번째 Disconnect의 정리 불능이 재현되었다. 운영 발생이나 대규모 누수는 확인하지 않았다.

한 세션이 알림과 전송 자원을 계속 소유한다. 알림 처리와 transport cleanup을 `try/finally` 등 최소 제어흐름으로 분리하며, 정상 callback-before-close와 콜백 한 번, 기존 예외 전파를 유지한다. 정리 실패가 원래 예외를 가리지 않도록 기존 정리 예외 계약을 검토한다. endpoint 조회 실패에도 자원이 정리되어야 한다. 새 lifecycle service·공용 추상화·클라이언트 구현 복제는 하지 않는다.

## 범위와 완료조건

- 생산 변경은 `02_Server/Network/Session.cs`의 종료 경로만. GameSession의 월드 close enqueue, 송수신 프레이밍·PDL·게임 정책·ClientNet·Shared·Unity 자산은 유지한다.
- 독립 테스트는 실제 loopback 임의 포트로 throwing/정상 callback, peer EOF·socket 종료·잔여 송신 큐 해제, 반복 Disconnect 알림 한 번과 기존 예외 전파를 확인한다.
- 기존 GameSession/ServerHost 수명 테스트와 solution build/test를 실행한다. 필요 시 관련 종료 중 전송 순서를 확인하되 pre-Start lifecycle 전체 설계나 부하 성능 작업으로 확장하지 않는다.
- TestCode는 구현자와 분리한 Astra가 작성·실행한다. 생산 수정은 Sol6.1에 반환한다. 테스트 소유 socket/listener만 정리하며 기존7777·다른프로세스·SQL은 조작하지 않는다.

## 조사 판단과 측정

Shared 계산·codec, 양쪽 프레이밍, 연결 시도/리스너 수명은 대표 경계 조사에서 현행 유지 근거가 있었다. 모든 입력·동시성·런타임의 전수 감사는 아니다.

부분 송신 후보는 보류했다. reflection으로 일부 완료를 주입한 결과와 정상 API 도달성을 구분하며, .NET10 실제 TCP 양쪽 각1MiB는 전부 전달됐다. Unity Mono/IL2CPP에서의 도달성은 미검증이다. 이 근거 없이 양쪽 송신 helper를 추가하지 않는다.

사전 근거는 `.backups/reviews/2026-09-30-s3-network-assessment.md`와 이를 정정하는 `.backups/verification/2026-09-30-network-boundary-probes/summary.md`다. 실측은 조건부 정리 실패의 전후 계약과 독립 회귀 결과이며 임의 AI 점수·복잡도 개선을 주장하지 않는다.

## 실행과 기록

DEVELOPMENT에 따라 WSL 격리본에서 검증하고 Windows Unity DLL을 갱신하지 않는다. 메인이 goal·CURRENT·로드맵·PR/보고, Sol이 생산코드·최소 계약 설명, 별도 Astra가 테스트·실행 공간, 또 다른 Astra가 diff 읽기 검토를 맡는다. 실행 결과는 `.backups/verification/2026-09-30-session-disconnect-cleanup/`에 남긴다. 코드/테스트 쓰기 종료를 확인하고 통합한다.

## 결과와 다음 단계

구현·독립 테스트·별도 읽기 리뷰 완료 상태다. Sol은 Session.cs 및 protocol.md만 수정했고 별도 Astra는 신규 `Network/SessionDisconnectCleanupTests.cs`5case를 작성·실행했다. 지정 Sol6.1/Astra와 실제 모델을 구분하며 실제 런타임은 unknown이다.

| 검증 | 실제 결과 |
|---|---|
| WSL solution 강제 빌드 | exit0, 기존 warning6/error0 |
| 전체 solution | 813 total / 808 pass / 기존5 skip / 0 fail |
| 신규·관련 회귀 | 신규5개 포함 관련22/22 통과 |
| 동일 신규 테스트의 기준 소스 실행 | 정상1 pass, 오류 경로4 fail; 회귀 검출 확인 |
| 독립 diff 리뷰 | 수정 필요 결함 없음 |

이 브랜치는 main 기준이므로 S1/S2 신규 테스트가 포함되지 않는다. 단계별 총수를 누적 품질 점수로 해석하지 않는다. 정상·throwing callback은 실제 Start/loopback 경로에서 socket 폐쇄·peer EOF·queue/pending 해제와 동일 예외 객체 전파를 확인했다. endpoint 오류는 실소켓의 protected 필드 연결을 사용해 선행 receive callback race를 제거했다. queue seed는 managed 컨테이너 정리 검증이며 진행 중 OS send 전체 검증이 아니다. 진단 출력 실패는 기존 비병렬 ConsoleSerial에서 검증하고 원래 writer를 복구했다.

초기 한 fixture에서 callback의 직접 Dispose가 EOF 대신 ConnectionReset을 반환했다. 그 변형만 정상 폐쇄 관측 두 종류를 허용하도록 고쳤으며 production Shutdown 경로의 EOF 검증은 유지했다. 첫 로그와 재검증 결과를 모두 보존했다. 생산코드 수정 요청은 없었다. 원시 명령·환경·해시·결과는 `.backups/verification/2026-09-30-session-disconnect-cleanup/summary.md`, 읽기 리뷰는 `.backups/reviews/2026-09-30-s3-disconnect-contract-review.md`다.

Windows 원본 DLL·SQL·실서비스는 변경하지 않았다. Unity/실제 플레이·운영 오류 빈도·성능·전체 전송 수명은 미실행/미측정이다. GitHub exact-head CI는 PR 생성 후 확인한다. [전체 로드맵](../../milestones/2026-09-30-maintainability-rollout/roadmap.md)의 S4는 원격 보간 계산 경계의 검증 가능성을 다룬다.
