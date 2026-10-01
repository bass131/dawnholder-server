# 시스템 기록·런타임 편집 독립 검증

2026-09-30. 지정 검증 모델 `gpt-6-astra`, 확인된 실제 runtime 모델 `unknown`. 검증자는 제품·데이터 작성자와 분리되어 테스트 4파일을 작성/보완하고 실행했다. 상태 원본은 [goal.md](goal.md)다. 검사 대상은 `feat/management-system-records`, base/HEAD `c27b03e888986f2ec8c593cd6c626a9c515595e1` 위의 아직 커밋하지 않은 Management 변경이다.

## 판정

이번 기록 조회·편집·파일 저장 범위는 **PASS**다. 독립 코드 테스트 27개, 테스트 strict 타입검사, UI/Electron build, 실행 배치 테스트 12개가 통과했다. 동일 제품 산출물의 시험용 복사본에서 실제 Electron 저장을 검증했고, 새 정본 경로의 실제 main은 별도로 읽기 전용 실행했다. 두 최종 실행 모두 관찰한 외부 네트워크 요청 0, 소유 앱 정상 종료, 정본 catalog 해시 보존을 확인했다.

Game Dev의 최초 조건부 의미 검토에서 지적한 전투 조건은 정정된 catalog와 고정 소스로 독립 재확인했다. **Game Dev의 정정본 최종 수용 회신은 이 문서 작성 시 대기 중**이다. 이를 이미 수신한 승인으로 표시하지 않는다. PR 병합 승인·main 통합과도 별개다.

위 대기는 작성 당시 관찰이다. 후속 Game Dev 회신 `msg_ccd48e9fdcbc`(2026-09-30 09:57:21 UTC)의 정정본 의미 검토 **PASS**와 PR147의 10:41:59 UTC 병합(`715bff5bfc62ab5c1f1cdf0aabdf7358492bb1d3`)은 이미 [goal의 현재 상태](goal.md#현재-상태)와 [최종 회신 근거](goal.md#결과-근거와-남은-일)에 기록돼 있다. 이번 정정에서 처음 발견한 PASS나 새 실행 결과가 아니다.

## 내용과 고정 근거

최종 catalog SHA256은 `2711E0C1DB3FB581A6B02373E7C6F72EE3761E303628E3A03329758014385A1A`, revision은 `2026-09-30-r1`, 상태 기준시점은 `2026-09-30T09:05:09Z`다. 18개 시스템·18개 공통 기록·35개 출처의 ID 중복, 누락 참조, 시스템↔기록 역방향 연결 오류는 0이다.

이 hash와 아래 출처 분포·실행 결과는 당시 r1의 근거다. 후속 `2026-10-01-r2`는 Management 출처 6개를 같은 내용의 고정 Git 원문으로 연결해 JSON hash가 달라진다. [출처 전환 기록](migration-map.md#출처-고정과-가용성)을 따르며, r1 실행 검증을 r2에서 다시 수행한 것으로 표시하지 않는다.

[정적 감사 원시 결과](../../.verification/system-records-source-audit.json)와 [재실행 스크립트](../../.verification/system-records-source-audit.mjs)는 Git 출처 16건의 고정 commit/path 존재, 로컬 출처 18건의 실제 SHA256 일치, HTML 구간 9건의 존재를 확인한다. 남은 1건은 메인이 전달받은 메시지 시점 관찰이며 검증자가 메시지 IPC나 GitHub를 새 조회한 결과가 아니다. PR140~145가 고정 commit에 병합되어 있음은 로컬 Git 이력에서도 확인했다.

FEATURE_MAP·서버/클라이언트 계약·D0·후속 단계 초안과 고정 보고서의 S1~S5/M3/합성 검증 수치를 대조했다. 과거 실행 수치, 기존 skip, 합성 Unity 미실행, D0 설계 완료와 GameServer 저장·복원 미완료, P0 준비/P1~P7 미착수를 구분한다. 이후 실제 게임 회귀나 이번 앱 검증을 09:05 snapshot 성공으로 소급하지 않는다. 원시 게임 로그를 전수 재수집하거나 게임 원문을 수정하지 않았다.

초기 검토에서는 요약된 전투 문장을 도메인 문서와 일치한다고 판단했으나, Game Dev가 지적한 더 구체적인 조건은 최종본에 보완했다. `GameMap.HandleEnemyDeath`의 보스이며 아직 clear 전인 경우만 StageClear, 비보스만 respawn 등록하는 조건, 별도 보스 재출현 경로를 확인했다. `DeferredDamageSystem`의 clamp는 `S_HitResult.currentHp` 표현이며 권위 `target.Hp` 자체의 보정이 아님을 확인했다. 평타 SubmitAttack/CombatSystem과 Dash SubmitSkillUse/SkillSystem 입구도 구분했다. 이 재확인은 게임 실행 검증이 아니다.

## 코드 검증

| 검사 | 결과 | 근거 |
|---|---|---|
| frontend `npm test` | 4파일, 27/27 통과 | [최종 로그](../../.verification/system-records-tests.log) |
| `node node_modules/typescript/bin/tsc --project tests/tsconfig.json --noEmit` | exit 0 | [strict 테스트 타입검사](../../.verification/system-records-tests-typecheck.log) |
| frontend `npm run desktop:build` | UI strict/Vite/Electron compile exit 0 | [build](../../.verification/system-records-build.log) |
| 05 `node --test tests/launcher.test.mjs` | 12/12 통과 | [launcher](../../.verification/system-records-launcher.log) |

- [App.test.tsx](../../frontend/src/App.test.tsx): 4개. 미연결·미수집, 운영 제어 비활성, 키보드 영역 전환, fetch/XHR/WebSocket/EventSource/beacon 미호출을 보존한다. 기록 조회/편집 버튼까지 비활성이라고 요구하던 이전 테스트를 운영 제어 범위로 좁혔다.
- [DevelopmentRecords.test.tsx](../../frontend/src/DevelopmentRecords.test.tsx): 7개. 검색/분야/기록 종류/결과 없음, 키보드 상세 focus, 관련 시스템과 근거, 저장의 명시성, 영역 전환 시 초안 보존, 외부 변경 후 reload/save 충돌, 명시 취소 후 기준 갱신, 손상/미확인 참조 거부, 저장/bridge 실패, 파일 불러오기와 미연결을 확인한다.
- [records-store.test.ts](../../frontend/tests/records-store.test.ts): 8개. 실제 임시 파일 I/O로 읽기·저장·정확 last-good bytes·재조회, missing 초기화, JSON/schema/중복ID/끊긴 참조/UTF-8 크기 제한, 손상본 복구 시 정상 백업 보존, 동시 writer와 stale 버전, 기존 lock 보존, 백업 쓰기 실패, 임의 path 필드로 대상 변경 불가, read 실패를 확인한다. 정본 파일에는 쓰지 않는다.
- [desktop-main.test.ts](../../frontend/tests/desktop-main.test.ts): 8개. 기존 외곽 창 크기·트레이 수명·종료·실패·navigation/permission 차단을 보존한다. 새 preload 경계를 반영하고 소유 main frame/정확 URL만 read/save를 호출할 수 있음을 검증한다. 다른 sender/frame/URL·파괴된 창은 거부한다.

최초 실행의 4실패는 숨겨진 기록 편집 설명까지 운영 텔레메트리로 검사하던 이전 기대와 반복 표시된 근거의 단일 query 때문에 발생했다. [첫 로그](../../.verification/system-records-tests-first.log)를 보존했고 테스트 범위를 바로잡았다. 전체 suite가 통과한 뒤 같은 suite를 반복하지 않았다.

메인 검토에서 발견한 “편집 초안은 남지만 reload가 저장 기준 hash를 새 값으로 바꾸는” 문제는 제품 작성자가 수정했다. 독립 테스트와 실제 renderer에서 A 기준 초안→외부 B 변경→reload→A 버전 저장 거부를 확인했다. 명시 취소 후에만 B 기준으로 새 편집을 시작한다.

## 실제 Electron 관찰

제품 Electron `44.5.0`을 사용했다. [실행 launcher](../../.verification/system-records-launch-runtime.mjs)는 Electron 자식의 stdout/stderr를 실제 로그 파일에 연결하고 해당 자식의 exit를 기다린다. 디버그 포트나 renderer Node 권한을 추가하지 않았다.

**저장 시험용 복사본:** [runtime 하니스](../../.verification/system-records-runtime.mjs), [결과](../../.verification/system-records-runtime.json). `desktop-dist`, `dist`, tray 자산을 구조가 같은 `.verification/system-records-runtime-fixture-*`에 복사하고 각 파일의 SHA256이 제품 산출물과 같은지 확인한 뒤 실제 main을 import했다. 변경 대상은 이 복사본의 catalog/backup뿐이다. 최종 PID `12040`, exit 0, `app.quit()`/will-quit 관찰.

실제 외곽 1280×720, 격리 bridge의 `readCatalog/saveCatalog` 두 메서드만 노출, renderer require/process/ipcRenderer 부재와 CSP `connect-src 'none'`을 확인했다. Enter 이벤트로 영역과 카드 선택, 상세 focus, 검색/결과 없음/관계 이동, 35개 근거 표시를 확인했다. 실제 textarea 편집→저장→재읽기와 백업 bytes, 잘못된 JSON 보존, renderer File/DataTransfer를 통한 불러오기(초안만 변경), 외부 파일 변경 뒤 충돌과 초안 보존, 명시 취소, 별도 비허용 창의 IPC denied를 확인했다. 임의 path 필드를 보내도 고정 저장 대상은 유지되고 지정한 다른 파일은 바뀌지 않았다.

동시 저장은 Node의 독립 store 두 인스턴스 테스트로 확인했다. renderer 충돌은 외부 writer가 시험 파일을 변경하는 방식이며 **서로 독립된 제품 창 두 개의 동시 편집 UI를 끝까지 자동화한 것은 아니다**. 파일 input 전달은 실제 Chromium File 이벤트를 사용했지만 사람이 OS 파일 선택 대화상자를 조작한 검증은 아니다.

**새 정본 경로 읽기 전용:** [별도 하니스](../../.verification/system-records-canonical-runtime.mjs), [결과](../../.verification/system-records-canonical-runtime.json). `management-active/05_Management/frontend/desktop-dist/main.js`와 실제 dist URL을 실행했다. 정본 catalog를 bridge로 읽은 hash가 최종 원문과 일치했고 저장 메서드는 호출하지 않았다. 최종 PID `12368`, exit 0, `app.quit()`/will-quit 관찰. 실행 전후 정본 hash 동일, 네트워크 요청 0이다.

시각 확인 근거는 [정본 1280 개발 현황](../../.verification/system-records-canonical-development-1280.png), [전투 상세](../../.verification/system-records-combat-detail-1280.png), [960 긴 출처](../../.verification/system-records-canonical-long-evidence-960.png), [430 긴 출처](../../.verification/system-records-canonical-long-evidence-430.png)다. 외곽 1280×720의 renderer는 1264×681로 관찰되며 외곽/콘텐츠 크기를 혼동하지 않는다. 960/430 외곽에서도 document 가로 넘침과 긴 경로 내부 넘침이 없고 경로/hash 줄바꿈을 이미지로 확인했다. 검증자와 메인이 실제 PNG를 확인했다.

초기 하니스 실패도 최종 성공과 구분한다. 직접 PowerShell GUI 실행의 닫힌 출력 pipe에서 제품 console.info가 EPIPE를 내 오류 창이 발생했다. 검증 소유 오류 창만 Computer Use로 확인하고 이후 자체 timeout 종료를 관찰했다. 두 번째는 launcher의 windowsHide가 창 표시 관찰을 막았고, 세 번째는 Enter의 char 이벤트 누락이었다. 각각 실제 파일 stdio, 화면 검증용 visible spawn, 완전한 Enter 입력으로 하니스만 수정했다. [시도1](../../.verification/system-records-runtime-attempt1.json)·[시도2](../../.verification/system-records-runtime-attempt2.json)·[시도3](../../.verification/system-records-runtime-attempt3.json)를 보존한다. 정본 하니스의 반복 전역 const 선언 오류는 IIFE로 수정했고, 스크롤 후 compositor 대기를 넣어 PNG와 측정 위치를 일치시켰다. 이 과정에서 제품 파일은 변경하지 않았다.

## 보존과 한계

서버·WSL·SQL·Unity·게임/봇·MCP·3D를 실행하지 않았다. 새 패키지 설치, 외부 네트워크, 권한/전역 설정 변경, Git 커밋·푸시·병합, 다른 사용자 앱 종료를 하지 않았다. 이전 Game Dev/Management 원문과 정본 catalog를 보존했다. build 산출물과 검증 전용 fixture/profile/log/PNG는 ignored 검증 영역에 남는다.

배치 검증은 기존 cmd fixture 12개를 다시 실행했으며 성공 START는 stub이다. 실제 launcher 더블클릭·detached OS 성공은 이번에도 별도 미실행이다. 실제 앱 기동은 위 하니스의 제품 main 실행으로 한정한다. 원자적 교체와 백업 정상/실패 경로를 확인했으나 전원 차단·프로세스 강제 중단·장시간 스트레스·동시에 쓰는 비협조적 외부 편집기의 모든 경합을 입증하지 않는다. 로컬 근거 경로는 다른 PC에서 없을 수 있고 화면은 해당 제한을 안내한다. `.verification` 원시 근거는 로컬 산출물이며 PR에 포함된 게임 검증 실적으로 취급하지 않는다.

메인의 최종 goal/README를 읽어 이번/과거 실적, runtime 자체기능/MCP 후속, 정본 원문 소유, 미연결 운영, 파일 불러오기/명시 저장 구분과 수치가 위 근거에 부합함을 확인했다. 추가 제품 수정이나 재실행이 필요한 과장은 발견하지 않았다.

검증 뒤 구현자가 정리한 `catalog-contract.ts`, `App.tsx`, `DevelopmentRecords.tsx`, `recordCatalog.ts`, `tsconfig.json`의 unstaged diff는 EOF 빈줄 7개 삭제뿐임을 독립 확인했다. 동작·설정값·데이터 변경은 없으며 `git diff --check` 통과 후 테스트/build/runtime을 반복하지 않았다.
