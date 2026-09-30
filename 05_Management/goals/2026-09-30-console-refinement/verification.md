# 운영 콘솔 재정비 독립 검증

2026-09-30. 정본 `C:/Dev/DawnHolder_Project/05_Management`의 미커밋 renderer 변경을 검증했다. 지정 모델 `gpt-6-astra`, 확인된 실제 모델 `unknown`. 목표·상태·최종 결과의 원본은 [goal.md](goal.md)다. 이전 desktop-shell 근거를 덮어쓰지 않았다.

## 결과

기존 계약 보존과 이번 정보 위계·가독성 검증을 통과했다. 독립 검증에서 작은 활성 메뉴 번호의 낮은 대비를 발견해 구현자에게 수정 요청했고, 해당 색상 한 줄 수정 후 최종 build와 실제 Electron 캡처를 다시 확인했다. 남은 제품 결함은 발견하지 않았다.

| 검증 | 결과 | 근거 |
|---|---|---|
| 독립 UI 테스트 보완 및 `npm test` | 2파일/10테스트 통과: UI 4 + 기존 main 6 | [tests](../../.verification/console-refinement-tests.log) |
| 최종 `npm run desktop:build` | strict UI 타입검사·Vite production build·main NodeNext compile exit 0 | [build](../../.verification/console-refinement-build.log) |
| 실제 Electron renderer | 기본 1040 창, 960/1280 content 폭 렌더와 세 영역 키보드 탐색 통과 | [runtime](../../.verification/console-refinement-runtime.json) |
| 외부 요청·정리 | 관측 외부 요청 시도 0, 최종 PID `34356` exit 0, 소유 프로세스 잔존 0 | [process result](../../.verification/console-refinement-process-result.json), [cleanup](../../.verification/console-refinement-final-cleanup.json) |

[App.test.tsx](../../frontend/src/App.test.tsx)에 미연결 요약값을 숫자로 표시하지 않고 미수집 이유와 접근 가능한 이름으로 설명하는 계약 하나를 추가했다. 기존 미연결·탐색/ARIA·disabled/네트워크 미호출 테스트를 보존했다. 구현 문구 전체나 장식 구조를 복제하는 테스트는 추가하지 않았다.

## 실제 화면과 키보드

이전 경험을 바탕으로 [검증 전용 하니스](../../.verification/console-refinement-runtime.mjs)가 실제 product main을 import하고 Electron `capturePage()`와 DOM 상태를 관찰했다. 외부 브라우저·새 의존성·CDP/debug 포트·제품 renderer 권한 추가는 없다. 하니스 main 권한을 제품 renderer 권한으로 해석하지 않는다.

- [기본 1040×800 창](../../.verification/console-refinement-default-1040.png): 실제 content `1024×761`. 요약 → 원본 로그 → 비활성 제어가 한 화면에 보인다. 제어 하단 약 676px로 내용 높이 안에 들어온다.
- [960×800 content](../../.verification/console-refinement-960.png), [1280×800 content](../../.verification/console-refinement-1280.png): 가로/세로 overflow 없이 내용이 표시된다.
- [유저 관리](../../.verification/console-refinement-users-1280.png), [개발 현황](../../.verification/console-refinement-development-1280.png): 영역에 맞는 구조와 미수집/미연결 안내를 유지하며 키보드 초점이 보인다.
- 실제 Return/char 입력으로 유저 관리 → 개발 현황 → 서버 운영 전환을 확인했다. Tab 순서와 2px solid focus outline도 확인했다.
- 서버 상태·버전·접속 현황은 미수집 이유가 있는 대시로 표시되고 시작/종료는 disabled다. 미연결 상태를 정상·접속자 0·저장완료로 가장하지 않는다.

PNG는 실제 Electron renderer 내용 캡처이며 OS 제목줄/작업표시줄까지 포함한 전체 데스크톱 캡처는 아니다. 각 화면을 직접 보고 가로 잘림·겹침·상태 오해 여부를 확인했다. 마지막 숫자 색 보정 후 동일 경로의 최종 캡처를 갱신하고 기본 창을 다시 확인했다.

## 발견 사항과 수정 확인

활성 메뉴의 11px 번호 `#7e899a`와 배경 `#252d3a`의 명암비가 약 `3.92:1`로 일반 텍스트 기준 `4.5:1`보다 낮았다. [수정 전 실제 관찰](../../.verification/console-refinement-before-contrast-runtime.json), [수정 전 기본 창](../../.verification/console-refinement-before-contrast-1040.png)을 보존했다. 검증자는 production을 고치지 않고 구현자에게 환류했다.

구현자가 `.priority`를 `#a0adbf`로 바꾼 뒤, 세 폭의 관측 텍스트/불투명 배경 최소 명암비는 약 `5.57:1`로 통과했다. 이는 computed CSS 기반 관측이며 비활성 버튼·아이콘·모든 접근성 항목의 인증 검사는 아니다.

## 범위와 미실행

검증자는 `App.test.tsx`, 이 보고와 `.verification/console-refinement-*`만 작성했다. 허용된 build 산출물은 갱신했다. production·main·config·목표 문서·Git·공통 파일·보안정책·다른 프로세스는 변경하지 않았다. 이전 main 6테스트는 실행했지만 변경 없는 native tray 메뉴/UI를 다시 조사하지 않았다. 최종 검증 앱은 `app.quit()`로 종료했으며 이번 결과를 새 트레이 검증으로 보고하지 않는다.

하니스는 요청 시도를 기록하고 외부 전송 전 취소하도록 구성했으며 관측 요청 시도는 0이었다. 실제 백엔드·WSL·게임서버·DB·운영 API·알림·서명/배포·실제 플레이는 미실행이다. 원시 근거와 이미지는 ignore된 정본 로컬 `.verification`에 있다.
