# 실행 배치 독립 검증

2026-09-30. 정본 `C:/Dev/DawnHolder_Project/05_Management`. 지정 모델 `gpt-6-astra`, 확인된 실제 모델 `unknown`. 목표·상태·최종 결과는 [goal.md](goal.md)가 원본이다.

## 결과

배치 분기 테스트 12개, 기본 창 크기 계약을 포함한 main 테스트 7개, 테스트 strict 타입검사와 desktop build가 통과했다. 이번 범위에서 수정할 제품 결함은 발견하지 않았다.

| 수행 | 결과 | 근거 |
|---|---|---|
| `node --test tests/launcher.test.mjs` | 12개 통과 | [launcher tests](../../.verification/launcher-independent-tests.log) |
| frontend의 `npm test -- tests/desktop-main.test.ts` | 7개 통과 | [main tests](../../.verification/launcher-independent-main-tests.log) |
| `node node_modules/typescript/bin/tsc --project tests/tsconfig.json --noEmit` | exit 0 | [test typecheck](../../.verification/launcher-independent-test-typecheck.log) |
| `npm run desktop:build` | strict UI 타입검사 + Vite build + main NodeNext compile exit 0 | [build](../../.verification/launcher-independent-build.log) |

[독립 배치 테스트](../../tests/launcher.test.mjs)는 실제 cmd.exe에서 공백이 있는 임시 프로젝트/도구 경로와 무관한 호출 cwd를 사용했다. Node/npm/frontend, tsc/vite/React/React DOM, Electron 바이너리 누락 시 오류·pause·nonzero와 npm/앱 미호출을 확인했다. 빌드 실패 코드 7과 실행 요청 실패 코드 9 보존, 성공 시 올바른 cwd의 `npm run desktop:build` 후 올바른 로컬 실행경로와 `.` 인자 전달, 모든 반환 경로의 pushd/popd 복원을 확인했다. ASCII/CRLF와 원본의 인용된 `%~dp0frontend`, 빈 START 제목도 확인했다.

테스트는 배치 복사본의 START 한 곳만 기록용 .cmd stub 호출로 대체했다. 원본 START 구문은 정적으로 대조했으며 나머지 cmd 분기/where/pushd/call/실제 pause/exit는 실행했다. npm은 동작을 기록하는 stub, node.exe·로컬 의존성·electron.exe는 존재 확인용 파일이다. 실제 설치·다운로드·Electron 실행은 일어나지 않았다. **성공 분기의 OS detached 실행이나 실제 창 생성 성공을 확인한 검증은 아니다.** 임시 fixture는 검증 영역의 직접 자식 경로임을 확인한 뒤 정리했다.

[main 테스트](../../frontend/tests/desktop-main.test.ts)에 새 BrowserWindow의 `width:1280`, `height:720`, `useContentSize:true` 부재 계약을 추가했다. 다음 실행의 외곽 크기 설정과 컴파일을 확인했으며 실제 새 크기의 창/클리핑은 이번에 관찰하지 않았다.

## 범위와 보존

검증자는 테스트 2파일, 이 보고와 `.verification/launcher-independent-*`를 작성하고 허용된 build 산출물을 갱신했다. 배치·README·goal·production main은 수정하지 않았다. 변경 없는 UI 전체 테스트·추가 보안/트레이 runtime 검증을 반복하지 않았다.

사용자가 보고 있는 PID `12744`는 조회·제어·종료하지 않았다. 새 실제 앱/프로필/디버그 포트를 만들지 않았고, 네트워크·새 의존성·보안정책·Git·다른 세션 파일을 변경하지 않았다. 실제 배치 더블클릭과 새 `1280×720` 창 확인은 미실행이다. 이전 메인이 띄운 앱의 관찰을 이 배치 테스트 결과로 대신하지 않는다.
