# 운영 콘솔 시각 재정비 목표

> 아래 정본·유일 정본·작업 경로와 권한은 이 목표 수행 당시의 기록이다. 현재 Management 정본 경로는 [README](../../README.md)를 따른다. 과거 실행 경로·결과는 소급 변경하지 않는다.

## 목표와 승인

사용자의 “디자인 더 업그레이드” 요청과 기존 결과의 시각 수준에 대한 피드백에 따라 운영 콘솔의 정보 위계와 밀도를 재정비한다. 차분한 다크·절제된 포인트 방향은 유지하면서 반복 연결 안내와 동등한 카드 박스를 줄이고 요약 → 로그 → 제어 순서를 드러낸다.

## 범위와 소유권

- 유일 정본 `C:/Dev/DawnHolder_Project/05_Management/**` 안에서만 작업한다. 기존 Game Dev checkout의 브랜치 전환·커밋·푸시·PR·병합과 루트/공통 문서/CURRENT 변경은 수행하지 않는다. 이전 보관 worktree를 수정하지 않는다.
- renderer 구현 작업자가 `frontend/src/App.tsx`·`styles.css`를 소유한다. 문서 작업자는 이 목표와 README 연결만 작성하며 renderer를 수정하지 않는다. 독립 검증자가 테스트/실제 runtime 근거를 별도로 작성한다.
- 구현 지정 모델 `gpt-6.1-sol`, 독립 검증 지정 모델 `gpt-6-astra`. 확인된 실제 런타임은 각각 `unknown`이다. 추가 위임 권한이나 persistent goal을 만들지 않는다.
- 이번 범위·상태·결과·미실행은 이 문서가 단일 원본이다. 이전 [desktop-shell 완료 기록](../2026-09-30-desktop-shell/goal.md)과 검증 근거는 보존한다.

## 제외 범위

추가 패키지·UI 프레임워크·외부 asset 도입, Electron main/격리/트레이 수명 변경, 백엔드·WSL·게임서버·DB·운영 API·실제 서버 제어, 알림·서명·패키징·배포·보안정책 변경은 수행하지 않는다. 미연결 영역에 가짜 운영 수치·로그·성공 동작을 추가하지 않는다.

## 관찰 가능한 완료조건

1. 차분한 다크와 절제된 포인트를 유지하고, compact sidebar/header와 읽기 쉬운 텍스트·간격·명암으로 운영 콘솔을 구성한다.
2. 반복 연결 안내와 동등한 카드 박스를 줄인다. 화면의 요약 → 로그 → 제어 정보 위계가 드러나고 불필요한 중복 문구/장식이 주 내용을 밀어내지 않는다.
3. 실제 미연결·미수집 상태를 정직하게 표시하며 disabled 제어를 유지한다. 서버 운영 → 유저 관리 → 개발 현황의 탐색 의미와 keyboard/ARIA 계약을 보존한다.
4. 외부 asset/운영 API 호출 없이 기존 로컬 bundle을 사용한다. 기존 Electron renderer 격리와 X→트레이 숨김/열기/명시 종료 수명을 보존한다.
5. 기존 1040×800 기본 창과 960/1280 폭에서 실제 Electron 렌더를 확인한다. 가로 잘림·겹침·읽을 수 없는 텍스트가 없고 필요한 내용은 정상 스크롤로 접근할 수 있다.
6. 구현자의 자체 타입검사와 독립 검증자의 계약 테스트·strict 타입검사·빌드·실제 runtime/화면 확인을 구분해 결과와 근거를 기록한다. 이전 목표의 성공을 이번 변경 검증으로 대신하지 않는다.

## 현재 상태

renderer 시각 재정비와 독립 계약/빌드/실제 화면 검증을 완료했다. 기본 창에서 요약·로그·제어를 한 화면에 표시하고 세 폭에서 overflow가 없음을 확인했다. 로컬 미커밋 상태이며 실제 운영 연동이나 Git 통합 완료를 뜻하지 않는다. 새 Git 작업이나 persistent goal은 시작하지 않았다.

## 결정과 보존 근거

- [R-12](../../requirements.md#r-12)의 다크 운영 콘솔 방향과 [D-09](../../decisions.md#d-09)의 직접 SVG/CSS·공식 참고 원칙을 유지한다. 이번에는 정보 위계와 중복 축소를 우선하며 새 기능으로 범위를 넓히지 않는다.
- 실제 미연결 화면과 Electron/트레이 보존 계약은 이전 목표의 [독립 보고](../2026-09-30-desktop-shell/verification.md)를 참고한다. 이는 보존 기준이며 이번 변경 결과가 아니다.

## 결과와 미실행

- 구현 작업자는 `App.tsx`·`styles.css`의 정보 위계를 재설계하고 자체 타입검사를 통과했다. 기존 공식 자료를 참고한 직접 구현이며 템플릿 추가 조사·설치나 패키지 추가는 없었다.
- 독립 검증자가 UI 계약 1개를 보완해 main 6 + UI 4, 총 10테스트 통과. strict 타입검사와 최종 `desktop:build` 통과. [독립 보고](verification.md), [테스트](../../.verification/console-refinement-tests.log), [최종 빌드](../../.verification/console-refinement-build.log)
- 최초 메뉴 번호의 명암비 3.92:1 실패를 구현자에게 돌려 `.priority`를 `#a0adbf`로 수정한 뒤 독립 재검증했다. 최종 관측 일반 텍스트의 최소 명암비는 약 5.57:1이며 접근성 인증 검사가 아니다.
- 실제 1040 기본 창에서 제어 하단 676px/content 높이 761px로 요약·로그·제어가 한 화면에 들어왔다. 1040/960/1280의 가로·세로 overflow 0, 세 영역 키보드 탐색과 외부 요청 0을 확인했고 미연결/disabled·기존 격리/권한 계약을 보존했다. [기본 창](../../.verification/console-refinement-default-1040.png), [960](../../.verification/console-refinement-960.png), [1280](../../.verification/console-refinement-1280.png), [runtime](../../.verification/console-refinement-runtime.json)
- 최종 PID `34356` exit 0, 잔여 소유 프로세스 0. [실행 결과](../../.verification/console-refinement-process-result.json), [최종 정리](../../.verification/console-refinement-final-cleanup.json). 문서 작업자는 코드·테스트·검증 보고서를 수정하거나 빌드/앱 실행을 반복하지 않았다. 로그/이미지는 정본의 ignore된 `.verification/console-refinement-*`에 보존한다.

실제 서버 등록·상태/로그 관리백엔드·WSL/게임서버·운영 API·장애 알림·서명/배포는 이번 목표에서도 미구현/미실행이다.
