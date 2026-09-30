# S5 — 도구 실패 결과와 종합 회귀·보고

상태: 최신 main `a3c4e15e7d655511ced06bd1303360761413c5ab`의 `fix/packet-generator-exit`에서 구현 착수. S2–S4 독립 검증·리뷰 완료 후 진행하는 독립 목표이며 앞선 미병합 소스는 이 브랜치에 포함하지 않는다. 2026-09-30 사용자는 PacketGenerator 출력 실패의 nonzero exit 전달과 독립 회귀를 S5에 포함하도록 승인했다. 정상 생성 내용·패킷 형식은 보존한다.

## 목표와 설계

`99_Tools/PacketGenerator/Program.cs`가 파일 출력 예외를 로그에만 남기고 exit0으로 끝내는 경우를 고친다. 격리 실행에서 출력 디렉터리와 일반 파일의 충돌을 만들면 FAIL 로그·생성 파일 없음에도 exit0이 반환됨을 재현했다. 실제 원본 생성물의 손상이나 stale 파일 소비를 관찰한 것은 아니다.

Main의 반환 결과를 명시해 정상 성공은0, 현재 catch가 처리하는 출력 실패는 nonzero로 전달한다. 기존 parse 예외의 전파, 성공·실패 로그, `--no-wait`, 출력 경로·생성 bytes를 보존한다. 새 I/O 인터페이스나 생성기 전면 재설계·atomic write·PDL 형식 변경은 하지 않는다.

## 범위·소유권·완료조건

- Sol6.1: Program의 최소 오류 경로와 관련 짧은 도구 계약 설명. 독립 Astra: 실제 subprocess 정상/실패 테스트 코드와 테스트용 임시 경로·timeout·정리.
- 생성기를 원본 repo PDL 대상으로 실행하지 않는다. 테스트 소유 임시 repo 구조와 sentinel로 출력 경로 충돌을 만들고 정상 생성 bytes를 기준선과 비교한다.
- XML 오류는 기존대로 실패함을 유지하고 출력 I/O 실패가 nonzero인지 확인한다. 다른 packet generator 상태·ID·버전·template·실제 generated source는 수정하지 않는다.
- 관련 회귀와 최종 solution build/test, 필요한 서버·봇·Unity 회귀의 수행 범위와 미실행 범위를 따로 기록한다. 기존 통과를 무조건 반복하지 않고 최종 변경이 영향을 주는 영역에 맞춘다.
- 원래 구현자 외 검증자가 결과를 확인하고, 메인이 S1–S5와 별도 인계 스킬 PR의 정확한 head·의존 순서·검증·남은 위험을 종합 보고한다. 병합은 그 후 사용자의 최종 확인을 기다린다.

## 전체 적용 평가

영역별 대표 책임·상태 소유·실패 경계·검증 진입을 조사하고 개선 근거가 있는 부분에만 적용한다. 프로젝트 전체의 모든 파일·동시성·게임플레이를 전수 검증했다고 표현하지 않는다.

S1 즉시 피해 조정 소유 지점, S2 선택한 wire 표현 조립 지점, S3 실패에도 자원이 정리되는 계약, S4 명시적 시간 입력과 단일 보간 상태, S5 실패 exit와 독립 subprocess 검증을 전후 근거로 비교한다. 코드 줄수·주석수·테스트 건수만으로 품질 또는 AI 점수를 만들지 않는다. 연속 복잡도·결합·AI 탐색 시간을 미계측이면 미측정으로 보고한다.

기존 WSL helper·wrapper·봇 공통 경계는 현행 유지 근거가 있었다. 실행소유·원본변경방지·잠금 책임을 새 runner로 재구성하지 않는다. 완료된 PC/환경 정비를 재개하지 않는다. 근거는 `.backups/reviews/2026-09-30-s5-tools-assessment.md`와 `.backups/verification/2026-09-30-generator-exit-probe/summary.md`다.

## 보고서

사용자가 요청한 HTML 보고서에는 목표·선택 이유·파일/역할별 변경 설명·주요 실제 diff·독립 테스트/CI 근거·미실행 범위·PR 순서와 승인 대기를 담는다. 원문 대화나 거대한 로그는 복제하지 않는다. 보고서와 raw evidence 위치는 결과 절에서 안내한다. 인계 스킬 PR #141은 이번 코드 로드맵과 별도의 main 기반 문서 PR임을 구분한다.

## 결과

개별 S5 구현·독립 TestCode·별도 읽기 리뷰를 완료했다. Sol은 Program.cs와 DEVELOPMENT만 수정했고 별도 Astra는 `Tools/PacketGeneratorExitTests.cs`3case 및 테스트 프로젝트의 build-only 참조를 작성했다. 생산/테스트 쓰기 종료를 확인했다. 요청 모델 Sol6.1/Astra와 실제 런타임 unknown을 구분한다.

| 검증 | 실제 결과 |
|---|---|
| 단독 테스트 프로젝트 실행 | 신규3개·PacketRoundTrip 포함109/109 통과; 도구 clean 후 자동 build 확인 |
| 동일 신규 테스트의 기준선 실행 | 정상/XML2 pass, 출력 충돌1 expected fail |
| 정상 전체 PDL 출력 | 전후92,915bytes 및 SHA256 동일 |
| WSL solution 강제 빌드 | exit0, 기존 warning6 |
| 전체 solution | 811 total / 806 pass / 기존5 skip / 0 fail |
| 독립 읽기 리뷰 | 필수 수정 없음 |

이 수치는 독립 main 기준 S5이며 앞선 미병합 신규 테스트는 포함하지 않는다. 실제 subprocess는 테스트 소유 임시 repo에서만 실행했다. 원본 PDL·생성 코드·PacketFormat 보존을 확인했다. interactive 키 입력 대기는 실행하지 않고 기존 후행 순서의 코드 보존을 검토했으며 자동 검사에는 --no-wait를 사용했다. 환경·명령·hash·원시 결과는 `.backups/verification/2026-09-30-generator-exit/summary.md`, 별도 읽기 리뷰는 `.backups/reviews/2026-09-30-s5-generator-contract-review.md`다.

최종 합성 회귀·HTML 보고서 최종 갱신과 독립 확인은 진행 전이다. 순서·의존성은 [로드맵](../../milestones/2026-09-30-maintainability-rollout/roadmap.md)에 유지한다.
