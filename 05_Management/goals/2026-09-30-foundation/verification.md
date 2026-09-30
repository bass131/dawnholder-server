# 초기화 독립 검증 근거

2026-09-30. 검증자 지정 모델 `gpt-6-astra`, 확인된 실제 런타임 모델 `unknown`.
목표·상태·최종 결과의 원본은 [goal.md](goal.md)이며, 이 문서는 그 판단에 쓰는 독립 검증 근거다.

## 대상과 판정

- 대상: `bass131/management-foundation`, 기반 `a3c4e15e7d655511ced06bd1303360761413c5ab` 위의 미커밋 `05_Management/**` 초기화 변경.
- 환경: Windows, Node `v24.15.0`, npm `11.13.0`; 기존 Orca `1.4.216`.
- 문서 정합성, 미연결 UI 계약, 설치 재현성, strict 타입검사, production build, 실제 dev/preview loopback 리스너 확인은 통과했다. 이 범위에서 수정이 필요한 제품 결함은 발견하지 않았다.
- 실제 브라우저 렌더/시각 검증은 Orca 도구 연결 실패로 미완료다. UI 시각 품질 통과로 보고하지 않는다. 실제 서버 운영·관리 API·데스크톱 앱 검증 결과도 아니다.

## 독립 테스트와 실행

검증자가 [App.test.tsx](../../frontend/src/App.test.tsx)를 작성했다. 구현자의 자체 테스트를 재사용하지 않았다.

1. 기본 서버 운영 진입, 우선순위 탐색 순서, 관측하지 않은 상태를 미연결로 표시하며 가짜 접속 수/저장완료/운영 상태를 표시하지 않는 계약.
2. 키보드로 유저 관리 → 개발 현황 → 서버 운영을 탐색할 때 활성 영역·접근성 연결과 미연결 안내가 일치하는 계약.
3. 전체 탐색과 비활성 제어 클릭 시 실제 제어가 불가능하고 fetch/XHR/WebSocket/EventSource/beacon 호출이 발생하지 않는 계약. 폼 전송·외부 링크 동작도 없다.

| 검증 | 결과 | 원시 근거 |
|---|---|---|
| `npm ci --engine-strict` | exit 0, lock 기반 106개 설치, 당시 audit 0 vulnerabilities | [ci](../../.verification/independent-ci.log) |
| `npm test` | exit 0, 1파일/3테스트 통과 | [tests](../../.verification/independent-tests.log) |
| `npm run typecheck` | exit 0, 테스트 파일 포함 strict TS | [typecheck](../../.verification/independent-typecheck.log) |
| `npm run build` | exit 0, 타입검사 + Vite production build | [build](../../.verification/independent-build.log) |
| 로컬 파일·헤딩 링크 | README/requirements/decisions/goal의 32개 참조 유효 | [links](../../.verification/independent-doc-links.log) |
| Git 변경 경계와 ignore | 공통/기존 tracked 파일 diff 없음, 신규 파일은 Management 안에 한정; node_modules/dist/검증 로그 제외 | [status](../../.verification/independent-root-status.log), [ignore](../../.verification/independent-ignore.log) |

테스트의 네트워크 미호출 확인은 렌더와 제공된 상호작용 경로의 관찰이다. 미래 관리백엔드의 인증/보안 또는 모든 가능한 통합 경로 검증으로 확대하지 않는다. audit 결과 역시 보안 전체 검증을 뜻하지 않는다.

## 실제 리스너와 자원 정리

`package.json`의 dev/preview가 실행하는 로컬 Vite CLI를 각각 `node node_modules/vite/bin/vite.js`, `node node_modules/vite/bin/vite.js preview`로 직접 실행했다. npm 래퍼 자체를 띄운 검증은 아니며 config나 host/port를 덮어쓰지 않았다. 사전 포트가 비어 있음을 확인하고 본인 프로세스만 시작했다.

- dev PID `38464`: `127.0.0.1:5173`만 listen, HTTP 200과 Management 진입 HTML 확인.
- preview PID `42760`: `127.0.0.1:4173`만 listen, HTTP 200과 Management 진입 HTML 확인.
- OS가 보고한 이 두 프로세스의 리스너에는 wildcard/LAN/IPv6 주소가 없었다. `strictPort: true`, `cors: false`, dev 파일 허용범위는 frontend로 제한된 config도 정적으로 확인했다.
- 종료 전 PID의 Node 실행경로·Vite 명령·생성시각을 확인하고 두 프로세스만 종료했다. 종료 후 소유 PID의 리스너가 남지 않았음을 확인했다.
- 근거: [프로세스](../../.verification/independent-processes.json), [리스너](../../.verification/independent-listeners.json), [HTTP](../../.verification/independent-http.log), [정리](../../.verification/independent-cleanup.log).

이 관찰은 현재 Windows frontend dev/preview의 기본 실행에 한정된다. 다른 머신에서의 접근 시도, 향후 WSL 관리 API, 인증·데스크톱 통신, 사용자 CLI override에 대한 보안 검증은 수행하지 않았다.

## 문서와 범위 검토

README가 requirements/decisions/goal로 안내하고, 사용자 확정·설계 제안·미결정·과거 조사 관찰·향후 완료조건을 구분한다. React/TS/Vite 및 WSL은 확정, ASP.NET Core는 제안, 포장/서명/법적 소재지는 미결정이며 개인 개발자라는 최신 응답을 반영했다. 창과 운영 수명 분리, 제한 장애복구·재부팅 후 수동 시작, 저장완료 대기, 사용자 적용 버전, GM·이상행동 근거 요구를 이번 구현 실적으로 서술하지 않는다.

화면은 전역 미연결 안내와 영역별 연결 후 확인 설명, 비활성 운영 제어로 초기 틀임을 표현한다. 특정 '개발 미리보기' 문구 자체를 요구하지 않고 실제 연결 상태와 행동을 기준으로 검증했다. 독립 검증에서 production 파일·설정·패키지·goal 문서는 변경하지 않았다.

로컬 문서 링크를 확인했으며 외부 공개 참고 URL의 현재 응답/내용과 과거 조사 관찰은 이번 검증에서 재조사하지 않았다.

## 브라우저 한계와 미실행

대상 worktree의 기존 브라우저 탭이 없음을 확인하고, production preview용 전용 탭 `06fc391d-e4ae-4b1c-a1e5-63f85375a5a0`만 생성했다. snapshot과 screenshot은 모두 `runtime_unavailable` 및 `The Orca runtime closed the connection before responding. Restart Orca and try again.`을 반환했다. 캡처 이미지는 얻지 못했다. 이는 검증 도구 연결 실패이며 제품 렌더 결함을 입증하지 않는다. 추가 브라우저 설치·앱 재시작·보안설정 변경은 하지 않았다.

정리 요청은 동일 page ID를 지정했고 `closed: true`를 받았다. 기존 탭·프로세스는 건드리지 않았다. [브라우저 정리 응답](../../.verification/independent-browser-cleanup.log)

실제 시각 품질, 반응형 화면, 데스크톱 포장/SAC 실행, Windows 알림, WSL/게임서버/DB, 저장·예약·복구·GM·실제 관리 API는 미실행이다. Git 커밋/푸시/PR/병합도 이 검증자가 수행하지 않았다. `.verification`의 원시 로그는 ignore된 이 작업공간 로컬 근거이며 저장소에 포함되지 않는다.
