# Electron 데스크톱 창 목표

## 목표와 합의

사용자의 “순차적으로 진행해보자, 나한테 물어볼 거 충분히 물어봐줘” 지시와 Electron 선택에 따라 기존 React 화면을 Windows 독립 창에서 개발 실행할 수 있는지 확인한다. WSL ASP.NET Core 관리백엔드도 채택했으나 이번 목표에 구현하지 않는다. 서명은 최대한 비용 없이 진행하기를 원한다.

## 허용 범위와 소유권

- 유일 정본 `C:/Dev/DawnHolder_Project/05_Management/**`만 수정한다. 기존 Game Dev checkout의 브랜치를 이 목표의 소유 브랜치로 취급하지 않으며 브랜치 전환·커밋·푸시·PR·병합은 수행하지 않는다. 이전 worktree 보관본은 읽거나 수정하지 않는다.
- 메인의 후속 계획 승인에 따라 기존 frontend package에 Electron `44.5.0`을 로컬 설치·lock 고정하고 main TS·빌드/실행 설정·실제 Windows 개발 실행을 진행한다. 기존 화면 기능과 `App.test.tsx`는 변경하지 않는다.
- 구현 작업자 지정 모델 `gpt-6.1-sol`, 확인된 실제 런타임 `unknown`. 추가 위임하지 않는다. 독립 검증자는 이후 `gpt-6-astra`로 별도 지정한다. 구현자는 테스트 파일을 작성하지 않으며 기존 `App.test.tsx` 내용은 보존한다.
- 이번 구현 범위는 기존 frontend package 안의 Electron main TS·별도 tsconfig·runtime 산출물·스크립트·package/lock·필요한 Vite 경로 설정·로컬 ignore와 목표 결과 기록이다. 자기 앱 프로세스만 식별해 정리한다.
- 후속 사용자 지시로 웹의 공식 디자인 참고를 조사해 화면의 시각 구성을 개선하는 범위를 추가했다. 별도 디자인 작업자가 `App.tsx`·`styles.css`·renderer assets를 소유하며 이 작업자는 해당 파일을 수정하지 않는다. 확정 방향과 출처는 [R-12](../../requirements.md#r-12)·[D-09](../../decisions.md#d-09)에 연결한다.

## 제외 범위

관리백엔드/WSL 기동·실제 서버/DB·상태/로그 연동·운영 명령·공통 코드/문서/CURRENT·CI·Windows 보안정책·전역 설치·유료 서비스 가입/인증서 구매·서명·배포 EXE/설치형 패키징·자동 업데이트·실제 장애/Windows 알림은 이번에 수행하지 않는다. 필요 없는 preload/Node 노출/IPC와 프레임워크·패키지 구조 중복도 추가하지 않는다. 도구 완전종료 후 알림 정책은 별도 합의 범위다.

## 관찰 가능한 완료조건

사용자 답변으로 기본 트레이 수명을 추가했다. 이번 셸은 자기 창/앱 수명만 관리하며 실제 장애 알림·WSL/서버 동작으로 해석하지 않는다.

1. Windows Electron 독립 창에 기존 Dawnholder Management 화면을 실제로 표시한다. 미연결 안내·우선순위 탐색·비활성 제어와 기존 DOM 계약을 보존하고 운영 API를 호출하지 않는다.
2. Electron main은 TypeScript로 작성하고 기존 package/lock을 사용한다. production bundle을 로컬 로드할 때 상대 asset 경로를 확인한다. 브라우저 dev/preview의 loopback 설정과 기존 빌드/타입검사를 보존한다.
3. `nodeIntegration: false`, `contextIsolation: true`, `sandbox: true`, `webSecurity: true`를 유지한다. 임의 외부 navigation/window.open을 차단하고 불필요한 renderer 권한·Node API·preload/IPC를 제공하지 않는다.
4. 공식 안정판 Electron을 로컬 설치·정확한 버전/lock으로 고정하고 현재 SAC/Defender 설정을 유지한 실제 실행 결과를 남긴다. 자기 visible 창/프로세스만 실행·정리하며 보안 우회·이름 바꾸기·기존 프로세스 종료를 하지 않는다.
5. 실행 차단이면 정확한 실패와 확인 가능한 해당 시점 이벤트 근거를 보고하고 실행 성공으로 표시하지 않는다. 과거 DLL 차단을 새 실패 근거로 사용하지 않는다. 개발 실행 관찰을 배포/설치프로그램/SAC 전체 통과로 확대하지 않는다.
6. 구현자 자체 빌드/실행 관찰과 독립 검증자가 작성·실행한 계약 테스트/리뷰를 구분하고 결과·로그·미실행을 이 문서에 모은다.
7. 로컬 비어 있지 않은 트레이 아이콘과 tooltip을 제공한다. 창 X=hide/트레이 유지, 메뉴 열기=show/focus, 명시 종료=이 앱만 quit를 수행하고 종료 플래그/참조를 정리한다. 실제 장애를 가장한 알림을 만들지 않는다.
8. [R-12](../../requirements.md#r-12)의 다크 콘솔·절제된 blue·반응형 시각 구성을 기존 미연결/탐색/비활성 제어 계약과 함께 보존한다. [D-09](../../decisions.md#d-09)의 공식 참고와 직접 SVG/CSS 구현 근거를 기록하고 실제 화면을 독립 검증한다.

## 현재 상태

정본 경로의 Electron 셸·기본 트레이·다크 콘솔 구현과 독립 계약/실행/렌더 검증을 완료했다. 실제 메뉴 객체의 callback 경로로 창 복원·명시 종료를 확인했으며 Windows 트레이 메뉴의 직접 마우스 클릭은 미실행이다. 자기 앱과 검증 앱의 잔여 소유 프로세스/관측 리스너는 0이다. 로컬 미커밋 상태이며 실제 운영 연동·장애 알림·배포/서명이나 Git 통합 완료를 뜻하지 않는다.

## 결정과 미결정

- 기술 선택의 원본은 [R-03](../../requirements.md#r-03), 선택 이력/이유는 [D-03](../../decisions.md#d-03)·[D-08](../../decisions.md#d-08)에 있다. backend/game 연결은 후속 목표다.
- 사용자는 X 닫기 시 트레이에 남아 장애 알림을 유지하는 방향을 선택했다. 이에 따라 기본 트레이 숨김/열기/종료를 이번 범위에 추가했다. 실제 서버 운영 수명 분리와 향후 알림 요구는 [R-04](../../requirements.md#r-04)를 유지하고, 실제 알림은 이번에 구현하지 않는다.
- 서명서비스·법적 소재지·가입 자격은 미결정이다. 개인 개발자라는 답변과 무비용 선호만 확인했으며 무료 공개신뢰 서명 확보를 주장하지 않는다.

## 조사와 구현 계획

- 기존 frontend는 ESM package와 strict TS, Vite/React, lockfile을 갖고 있다. main TS를 별도 NodeNext 설정으로 컴파일하고 `desktop-dist`에 출력하는 안으로 package 중복을 피한다.
- `desktop:build`/`desktop:start`로 화면 build + main compile + 로컬 Electron 실행을 연결한다. frontend의 dev/preview 호스트를 유지하고 로컬 bundle에는 Vite `base: './'`를 적용했다. 사용자 답변에 따라 기본 트레이 hide/show/quit 수명을 적용한다.
- `BrowserWindow.loadFile`은 이번 신뢰된 로컬 화면의 최소 개발 실행 안이다. 공식 보안 문서는 custom protocol을 권장하므로 file URL의 범위와 제한을 후속 구현 계획에서 명시한다. [BrowserWindow API](https://www.electronjs.org/docs/latest/api/browser-window), [보안 안내](https://www.electronjs.org/docs/latest/tutorial/security)
- main ESM 지원과 renderer의 Node 접근 분리를 확인했다. [공식 ESM 안내](https://www.electronjs.org/docs/latest/tutorial/esm)
- 2026-09-30 `npm view electron@latest version engines --json`: 안정판 `44.5.0`, 설치용 Node engine `>=22.12.0`. 기존 Node `v24.15.0`은 이 조건에 맞는다. [공식 릴리스](https://github.com/electron/electron/releases/tag/v44.5.0). 이는 설치/실행 결과가 아니다.

## 결과와 미실행

- 로컬 `npm install --engine-strict` exit 0, Electron `44.5.0` 고정/lock 반영, 당시 audit 0 vulnerabilities. 공식 패키지에는 postinstall이 없으므로 `node node_modules/electron/install.js`로 Windows x64 바이너리를 별도 확보했다. [설치 로그](../../.verification/desktop-install.log), [바이너리 확보 로그](../../.verification/desktop-binary-install.log), [공식 설치 안내](https://www.electronjs.org/docs/latest/tutorial/installation)
- 실행 전 `electron.exe` 크기/버전/SHA-256/서명을 기록했다. ProductVersion `44.5.0`, Authenticode `NotSigned`. 공식 npm/릴리스 URL과 정확한 해시는 [바이너리 메타데이터](../../.verification/desktop-binary-metadata.json)에 있다. 서명 상태를 실행 실패나 새 앱의 공개신뢰 인증 성공으로 해석하지 않는다.
- `npm run desktop:build` exit 0: 기존 strict TS/UI build와 별도 main NodeNext compile 통과. `base: './'`, production build 전용 CSP(meta), 기존 dev/preview loopback을 보존했다. CSP 삽입 hook은 dev에 적용하지 않아 Vite/HMR 정책을 바꾸지 않는다. [빌드 로그](../../.verification/desktop-build.log)
- 보안정책을 변경하지 않고 자기 Electron 앱 PID `17356`을 실행했다. `local Management bundle loaded`, `visible=true` 로그와 실제 Windows 창의 미연결 화면을 확인했다. 이는 디자인 변경 전 관찰이다. [소유 기록](../../.verification/desktop-owner.json), [실행 로그](../../.verification/desktop-runtime.log), [변경 전 screenshot](../../.verification/desktop-first-window.png)
- 자기 창에 WM_CLOSE를 보내 close→hide, 같은 PID 생존/보이는 창 handle 0과 tray 유지 로그를 확인했다. [hide 관찰](../../.verification/desktop-hide.json). 로컬 32px PNG는 SVG 원본과 Windows 기본 Drawing API로 만든 정적 아이콘이며 새 이미지 패키지/외부 다운로드를 사용하지 않았다.
- 구현자 단계의 Orca Windows provider taskbar/menu 접근은 `windowNotFound`였다. OS 메뉴 직접 조작 한계는 유지하며 아래 독립 검증의 실제 객체/callback 관찰과 구분한다.
- 기록한 자기 프로세스 트리 4개의 경로/시작시각을 확인해 정리했다. 정리 전 소유 PID의 listen socket 0, debug flag 없음, 정리 후 소유 프로세스 0이다. 메뉴 quit 통과로 해석하지 않는다. [디버그/리스너](../../.verification/desktop-debug-listeners.json), [정리](../../.verification/desktop-cleanup.json)
- 디자인 작업자 보고: `App.tsx`·`styles.css` 두 파일의 직접 SVG/CSS 구성으로 [R-12](../../requirements.md#r-12)의 방향을 반영했고 자체 `npm run typecheck`가 통과했다. 템플릿 전체 복사/설치 없이 미연결·비활성 제어·메뉴 의미를 유지했다는 보고다. 이 기록은 최종 독립 렌더 검증을 대신하지 않는다.
- 창의 초기 배경색만 root/body의 `#10151e`와 맞췄다. 이 한 줄 수정 뒤 구현자는 새 빌드/실행을 반복하지 않았으며 디자인 종료 후 최종 빌드/렌더는 아래 독립 검증에서 확인했다.

### 최종 독립 검증

- 검증자 지정 `gpt-6-astra`, 확인된 실제 런타임 `unknown`. [독립 보고](verification.md)를 읽고 테스트/정리 원시 근거와 최종 1280 screenshot을 확인했다. 기존 UI 테스트는 보존했고 검증자가 main 계약 테스트와 별도 test tsconfig를 작성했다.
- UI 3 + main 6, 총 9계약 테스트 통과. strict main/독립 테스트 타입검사와 `desktop:build`의 strict UI 타입검사·Vite production·main NodeNext compile 통과. 마지막 `color-scheme: dark` 반영 뒤 재빌드/재캡처했다. [테스트](../../.verification/desktop-independent-all-tests.log), [최종 빌드](../../.verification/desktop-independent-build.log)
- 실제 Windows Electron의 renderer Node/Electron/IPC 부재와 격리 설정, 로컬 bundle/CSP, 외부 navigation/window.open 차단을 확인했다. Return/char로 세 영역 탐색과 Tab의 2px 초점을 확인했고 관측된 네트워크 요청 시도는 0이었다. 이는 전체 OS 트래픽 캡처 검증은 아니다.
- 실제 BrowserWindow.close→같은 창 hide/Tray 생존, 등록된 열기 callback→같은 창 show/focus, 종료 callback→tray 파괴/window 0/정상 exit 0을 확인했다. OS 트레이 메뉴 직접 클릭은 수행하지 않았다. [실제 객체 런타임 근거](../../.verification/desktop-independent-runtime.json)
- 기본 1040×800 창과 1280/960 폭의 renderer 캡처에서 가로 잘림이 없고 미연결/미수집·비활성 제어와 다크 scrollbar가 명확했다. [최종 1280 화면](../../.verification/desktop-independent-1280.png). 내용 높이에 따른 세로 스크롤은 남으며 모든 접근성 기준의 인증 검사는 아니다.
- 최종 PID `21376` exit 0, 잔여 소유 프로세스 0, 관측 리스너 0. CDP/Node debug 포트·새 Vite 서버·npm ci/바이너리 재다운로드는 사용하지 않았다. [실행 결과](../../.verification/desktop-independent-process-result.json), [최종 정리](../../.verification/desktop-independent-final-cleanup.json)

Windows 트레이 메뉴의 직접 클릭, 실제 장애/Windows 알림, WSL ASP.NET Core 관리백엔드·게임서버·DB·운영 명령, 서명서비스·배포/설치형 패키징/SAC 전체 검증은 미실행이다. 개발 실행의 성공과 `NotSigned` 바이너리 관찰을 배포 신뢰 검증으로 확대하지 않는다. 다음 설계 대상은 서버 등록·상태/로그·관리백엔드지만 새 목표를 시작하지 않았다. 로그/PNG는 정본의 ignore된 `.verification/desktop-*`이며 구현자는 테스트 파일을 작성·수정하지 않았다. 커밋·푸시·PR·병합은 수행하지 않았다.
