# Management 초기화 목표

> 아래 정본·작업 경로와 권한은 이 목표 수행 당시의 기록이다. 현재 Management 정본 경로는 [README](../../README.md)를 따른다. 과거 실행 경로·결과는 소급 변경하지 않는다.

## 목표와 승인

사용자의 2026-09-30 지시 “05_Management 문서랑 초기화 세팅 진행해줘, 목표 문서 기준으로 먼저 틀을 잡아놓자 SubAgent한테 구현 지시해줘”에 따라 설계 기록과 React·TypeScript·Vite 화면 기반을 만든다. 실제 운영 기능은 후속 목표다.

## 허용 범위와 소유권

- 사용자의 경로 정정에 따라 정본과 작업 루트는 `C:/Dev/DawnHolder_Project/05_Management`다. 기존 Game Dev 관리 checkout 안의 `05_Management/**`만 작업한다. 현재 checkout 브랜치를 이 목표의 소유 브랜치로 취급하지 않는다.
- `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-foundation/05_Management`는 최초 구현·검증 장소다. 당시 브랜치 `bass131/management-foundation`, 기반 main `a3c4e15e7d655511ced06bd1303360761413c5ab`는 과거 작업 맥락으로 보존한다. 사용자의 최신 경로 지시가 우선하며 브랜치 전환·커밋·푸시·병합은 수행하지 않는다.
- 이번 경로 정정에서는 문서/소스/독립 테스트/lockfile 16개와 과거 원시 로그를 복사하고 node_modules/dist는 새 경로에서 재현한다. 최초 작업본은 삭제·이동하지 않고 그 README에 보관/정본 안내만 기록한다.
- README·요구사항·결정·이 목표 문서, 프런트엔드 소스·로컬 설정·의존성/lockfile·검증 기반을 작성한다. 로컬 npm 설치와 프런트엔드 자체 빌드·타입검사는 승인 범위다.
- 구현 작업자 지정 모델 `gpt-6.1-sol`, 확인된 실제 런타임 `unknown`. 독립 테스트 코드·검증은 이후 별도 `gpt-6-astra` 작업자의 소유다. 구현자는 테스트 파일을 작성하지 않는다.

## 제외 범위

게임서버·DB·운영 API·WSL 설정·Windows 보안정책·서명·실제 데스크톱 패키징·배포·CI·루트 설정/공통문서를 변경하지 않는다. 글로벌 설치·업데이트, 기존 프로세스 제어, Management 영역 밖의 shared checkout 수정, 전체 .NET 솔루션 빌드는 수행하지 않는다. 원본 독립 테스트는 내용 그대로 복사하며 이번 구현 작업자는 새 테스트 작성·기존 테스트 수정을 하지 않는다.

## 관찰 가능한 완료조건

1. README에서 요구사항·결정·이 목표를 찾을 수 있고, 확정 요구·제안·미결정·관찰을 구분한다. 현재 작업 상태와 검증 결과는 이 문서만 원본으로 삼는다.
2. `frontend`에 React·TypeScript·Vite, strict 타입검사, 실행/빌드/preview 명령, lockfile, 로컬 ignore 및 독립 테스트 도구 기반이 있다. 기본 dev·preview는 `127.0.0.1`에만 바인딩하며 실제 리스너로 확인한다.
3. 화면에 Dawnholder Management와 서버 운영 → 유저 관리 → 개발 현황 탐색이 있다. 각 영역은 미연결을 표시하고, 실시간 상태·접속 수·저장 결과를 만들어 표시하지 않는다. 제어는 비활성 또는 미연결 안내에 한정한다.
4. 프런트엔드 설치·빌드·타입검사 결과와 사용 도구 버전을 기록하고, 독립 검증자가 요구사항 기준으로 별도 테스트를 작성·실행한다. 자체 점검과 독립 검증을 구분한다.
5. 변경은 `05_Management/**`에 한정되고, 실제 서버/DB/WSL/보안/패키징/배포에 영향을 주지 않는다. 수행하지 않은 통합·게임 실행을 성공으로 보고하지 않는다.

## 현재 상태

초기화 구현과 정본 경로의 독립 재검증을 완료했다. 유일 정본은 `C:/Dev/DawnHolder_Project/05_Management`이며 최초 독립 작업공간은 보관본이다. 로컬 미커밋이며 실제 데스크톱/서버/DB/서명/시각확인과 Git 통합 완료를 뜻하지 않는다. 커밋·푸시·PR·병합은 수행하지 않았고, 병합 직전 사용자 명시 승인은 공통 지침을 따른다.

## 결정과 선행 의존성

- 이번 구현은 [확정 화면 기술과 초기화 범위](../../decisions.md#d-03)의 개발용 화면이다. Windows 독립 창 포장과 백엔드 기술은 아직 확정하지 않았다.
- 장기 요구와 미결정의 원본은 [requirements](../../requirements.md), 선택 이유는 [decisions](../../decisions.md)에 둔다. 후속 구현 목표를 이번 완료조건으로 확대하지 않는다.
- 안전 종료의 GameServer 영속저장, 운영 권한/인증, 데스크톱 포장/서명은 후속 설계·구현 의존성이다.

## 결과와 근거

### 최초 작업공간의 구현자 자체 점검

- 사전 확인: Node `v24.15.0`, npm `11.13.0`; 새 작업공간의 AGENTS는 기존 현재 지침과 동일했다.
- 2026-09-30 공식 [Vite 안내](https://vite.dev/guide/), [React 안내](https://react.dev/learn/build-a-react-app-from-scratch), [TypeScript 안내](https://www.typescriptlang.org/docs/handbook/tsconfig-json.html)와 `npm view <package>@latest ... --json`으로 안정판·engine·peer 조건을 확인했다.
- 고정 설치: React/React DOM `19.3.0`, Vite `8.3.1`, TypeScript `7.0.2`, plugin-react `6.1.1`, Vitest `5.0.2`, jsdom `30.1.1`. 전체 직접 의존성은 package.json과 [설치 목록](../../.verification/dependencies.log)에 있다.
- `npm install --engine-strict`: exit 0, 106개 패키지 설치, 당시 audit 0 vulnerabilities. [설치 로그](../../.verification/npm-install.log)
- `npm run build`: 내부 `npm run typecheck`(`tsc --noEmit`)와 Vite production build 모두 exit 0. [빌드/타입검사 로그](../../.verification/build.log)
- Vite의 기본 config로 dev/preview 서버를 각각 생성해 HTTP 진입점 200을 확인했다. OS 리스너는 dev `127.0.0.1:5173`, preview `127.0.0.1:4173`뿐이었다. 자기 서버만 finally에서 닫았다. 이는 API 기반 실행 자체 점검이며 실제 브라우저 렌더 검증은 아니다. [리스너 로그](../../.verification/local-listeners.log)
- Git status에서 변경은 `05_Management/**`의 신규 파일뿐이었다. 로컬 로그·node_modules·dist는 ignore 처리한다. 로그는 이 작업공간 전용이며 저장소에 커밋하지 않는다.
- 구현자 점검 당시 문서의 로컬 파일/헤딩 참조 32개 존재를 정적으로 확인했고, `git check-ignore -v`로 로그·node_modules·dist 제외를 확인했다. README 29줄, requirements 93줄, decisions 57줄로 요약했다.

### 최초 작업공간의 독립 검증

아래와 [verification.md](verification.md)는 이전 독립 작업공간에서 수행한 역사적 근거다. 함께 복사한 `.verification` 원본 로그를 보존하며 새 경로의 실행 결과로 해석하지 않는다.

- 검증자 지정 모델 `gpt-6-astra`, 확인된 실제 런타임 `unknown`. [독립 보고](verification.md)의 필요한 부분과 아래 원시 근거를 확인했다. 검증자는 production 파일·설정·패키지·goal을 수정하지 않았다.
- 검증자가 [App.test.tsx](../../frontend/src/App.test.tsx)에 작성한 DOM 계약 테스트 3개 통과: 미연결 기본 진입/가짜 운영 상태 부재, 키보드 영역 탐색/접근성 연결, 비활성 제어/관찰 경로의 운영 네트워크 호출 부재. 실제 브라우저 시각 품질 검증과 구분한다. [테스트 로그](../../.verification/independent-tests.log)
- 독립 `npm ci --engine-strict`, `npm run typecheck`, `npm run build` 모두 exit 0. lock 기반 106개 설치와 당시 audit 0 vulnerabilities, 테스트 포함 strict TS 및 production build를 확인했다. [재설치](../../.verification/independent-ci.log), [타입검사](../../.verification/independent-typecheck.log), [빌드](../../.verification/independent-build.log)
- 기본 config의 Vite CLI로 dev/preview를 실행해 각각 `127.0.0.1:5173`, `127.0.0.1:4173`만 listen 및 HTTP 200을 확인했다. npm 래퍼 자체 실행이나 다른 머신의 접속 시도 검증은 아니다. [OS 리스너](../../.verification/independent-listeners.json), [HTTP](../../.verification/independent-http.log)
- 독립 문서 참조 32개, 변경 경계·ignore 검토 통과. 이 검증 범위에서 수정할 제품 결함은 발견하지 않았다. 상세 범위와 정적 검토 근거는 독립 보고에 있다.
- 검증자의 Vite PID 2개 종료·리스너 소멸과 생성 브라우저 탭의 `closed: true` 응답을 확인했다. [프로세스 정리](../../.verification/independent-cleanup.log), [탭 정리](../../.verification/independent-browser-cleanup.log)
- 실제 snapshot/screenshot은 Orca `runtime_unavailable`로 미완료이며 캡처를 얻지 못했다. 이는 도구 연결 실패 관찰이고 제품 렌더 결함이나 시각 품질 통과를 뜻하지 않는다.

### 정본 경로 배치와 재현

- 사용자 정정으로 `C:/Dev/DawnHolder_Project/05_Management`에 추적대상 16개와 과거 원시 로그 21개를 복사했다. 복사 직후 전체 SHA-256 일치를 확인했다. [복사 목록/해시](../../.verification/relocated-copy-hashes.json)
- README/이 목표의 경로 안내만 갱신하고 과거 verification.md와 테스트·설정·소스·lockfile은 보존한다. `node_modules`/`dist`는 복사하지 않았다. 새 경로 실행 결과는 `relocated-*` 로그로 구분한다.
- 정본 `frontend`의 `npm ci --engine-strict` exit 0: lock 기반 106개 설치, 당시 audit 0 vulnerabilities. `npm run build`의 strict `tsc --noEmit`와 Vite production build 모두 exit 0. [새 경로 설치](../../.verification/relocated-ci.log), [새 경로 빌드/타입검사](../../.verification/relocated-build.log)
- 경로 안내를 수정한 README/goal을 제외한 프로젝트 파일 14개와 과거 원시 로그 21개, 총 35개는 복사 후에도 SHA-256이 원본과 일치한다. 원래 작업본은 README 외 36개 파일이 복사 당시 해시와 일치한다. [최종 해시 확인](../../.verification/relocated-hash-check.json)
- 구현자는 새 경로에서 테스트 실행·서버 시작 없이 자체 재현을 마치고 독립 재검증자에게 전달했다.
- 정본 독립 재검증: 기존 계약 테스트 3개, strict 타입검사·production build, 원본/정본/manifest SHA-256 35개 일치, 당시 로컬 문서 참조 61개 및 scope/ignore 검토가 통과했다. 검증자는 `npm ci`를 재실행하지 않고 구현자 설치 로그·lock/실설치 일치를 확인했으며 새 서버·시각검증은 수행하지 않았다. [정본 독립 보고](relocation-verification.md)

## 미실행과 남은 일

경로 정정 후 새 dev/preview 서버는 띄우지 않았다. 실제 브라우저 렌더/시각 품질·반응형 화면, 데스크톱 포장/SAC 실행·서명·Windows 알림, 게임서버·WSL·DB 연동, 저장/예약/복구/GM·실제 관리 API는 미실행이다. DOM 계약 테스트 통과를 이 범위의 성공으로 확대하지 않는다. 커밋·푸시·PR·병합도 미수행이다. 장기 미결정은 requirements/decisions에 기록한 포장·서명·백엔드 기술·인증/권한·복구/예약 정책 등이며 후속 목표로 다룬다.
