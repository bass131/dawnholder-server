# 정본 경로 독립 재검증

2026-09-30. 정본: `C:/Dev/DawnHolder_Project/05_Management`.
검증자 지정 모델 `gpt-6-astra`, 확인된 실제 모델 `unknown`.
목표·상태·최종 결과의 원본은 [goal.md](goal.md)다. [최초 검증](verification.md)은 이전 작업공간의 역사적 근거로 보존한다.

## 결과

경로 변경 후 기존 계약 테스트 3개, strict 타입검사, production build가 모두 통과했다. 코드·설정·테스트·lockfile은 이전 작업본과 동일하며 새 경로로 인한 결함을 발견하지 않았다. README와 목표 문서는 정본 경로와 이전 작업본의 보관 상태를 명시한다.

| 독립 수행 | 결과 | 근거 |
|---|---|---|
| 정본 frontend의 `npm test` | exit 0, 기존 1파일/3계약 테스트 통과 | [tests](../../.verification/relocation-independent-tests.log) |
| `npm run typecheck` | exit 0, 테스트 포함 strict TS | [typecheck](../../.verification/relocation-independent-typecheck.log) |
| `npm run build` | exit 0, 타입검사와 Vite production build | [build](../../.verification/relocation-independent-build.log) |
| 원본/정본/복사 당시 manifest SHA-256 대조 | 경로 안내를 수정한 README/goal 제외 35개 일치: 프로젝트 14개와 역사적 로그 21개 | [hashes](../../.verification/relocation-independent-hashes.json) |
| 기존 문서 로컬 파일·헤딩 참조 | README/requirements/decisions/goal/최초 verification의 61개 유효 | [links](../../.verification/relocation-independent-links.log) |
| 설치 의존성 `npm ls --depth=0` | exit 0, 직접 의존성 누락/불일치 없음 | [dependencies](../../.verification/relocation-independent-dependencies.log) |
| Management 범위 상태·ignore | Management 파일은 미추적 상태, node_modules/dist/검증 로그 제외 | [scope](../../.verification/relocation-independent-scope.log), [ignore](../../.verification/relocation-independent-ignore.log) |

환경은 Node `v24.15.0`, npm `11.13.0`다. 구현자가 정본에서 수행한 `npm ci --engine-strict`의 [새 경로 설치 로그](../../.verification/relocated-ci.log)를 확인했다. 독립 검증자는 lockfile SHA 일치와 설치된 의존성 및 실행 결과를 확인했으며, 새 설치 우려가 없어 `npm ci`를 중복 실행하지 않았다. 구현자 설치를 독립 재설치 실적으로 보고하지 않는다.

## 범위와 한계

이번 검증은 기존 테스트를 내용 변경 없이 재사용했다. 검증자의 문서/근거 쓰기는 이 파일과 `.verification/relocation-independent-*`에 한정되며, 빌드는 허용된 ignore 산출물 `frontend/dist`를 생성했다. production·기존 테스트·패키지·config·goal·공통 파일을 수정하지 않았다. Git 상태는 `05_Management`로 한정해서 읽었으며 다른 세션의 checkout 변경을 이 작업의 변경으로 해석하지 않았다. 브랜치 전환·커밋·푸시·병합은 수행하지 않았다.

이번 경로 변경에서 dev/preview 프로세스나 브라우저 탭을 새로 시작하지 않았다. 코드/config가 동일하므로 추가 리스너 실행을 반복하지 않았으며 이전 worktree의 loopback/HTTP 관찰을 정본 경로에서 재실행한 결과로 주장하지 않는다. 이번 검증으로 종료해야 할 자체 서버·탭도 없다.

기존 Orca 연결 실패에 따른 실제 시각 품질 미확인 상태는 유지한다. 외부 문서 내용 재조사, 실제 Windows 독립 앱·SAC·알림, WSL/게임서버/관리 API/DB·저장·예약·복구·GM 검증은 미실행이다. `.verification` 원시 근거는 정본 작업공간의 ignore된 로컬 파일이다.
