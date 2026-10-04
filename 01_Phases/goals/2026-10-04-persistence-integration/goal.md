# 실제 SQL 설치·엔진 판정

상태: **두 번째 PC 크래시 후 고정 입력 보존을 확인했고 신규 Opus 강 독립 검증을 다시 준비한다.** 수정·검증 비교 기준은 `2372ba4bc10d932ce52aeb032cea60707d911c1b`다. 첫 PR 종료 범위·G4는 메인 `msg_b7074e6a1c4a` 승인 상태다. 마지막 Sol 자체 시험은 7 suite 962 PASS/0 FAIL/9 OBSERVED로 남았지만 최종 보고·정산 전에 크래시로 중단됐다. 이는 독립 PASS가 아니다. 실제 DB·G2·U-01·D:·서비스는 미실행/사용자 행동 대기다. 직전 독립 판정의 INSTALL-05 잔여·06은 새 판정 전까지 해소로 확정하지 않는다.

## 2026-10-05 01:15 KST 두 번째 크래시와 복구

메인 `msg_01dcc2f82c5c`(2026-10-04T16:40:31Z)가 두 번째 블루스크린과 사용자 원문 **「둘 다 하자, 안랩 세이프 트랜잭션도 지우고, Orca도 이전버전으로 다운그레이드하자」**를 전달했다. 보안 제품 제거·덤프 해석·데이터 손실 없음은 메인 보고이며 이 세션 수행이나 사용자 직접 입력으로 격상하지 않는다. 실제 CLI는 1.4.217로 확인하고 해당 버전 가이드를 다시 읽었다. 죽은 검증자 `task_be5ae41c496f` / `ctx_6ae9b6f4b789` / `term_2cb15f32-1ae9-40cb-b3e2-0e72adc8bd44`는 자동 abandoned/failed terminal_missing·capability revoked이고 최종 판정이 없다. **크래시로 중단된 부분 결과**로 남기며 중간 기록을 판정 근거로 승계하지 않는다. `crash2-input-audit.json`에서 이전 고정 입력79개가 모두 일치하고 검증 evidence 폴더/최종 판정/테스트 변경이 없음을 확인했다. 기존 원시는 보존하고 제품 확정 실패에 더하지 않는다. 새 자기 pane `term_5eb9b1ed-99af-4ffa-a845-f11d4df76ab1`, runtime `c37fa9b2-410f-4791-ac59-9ad67570b6ef`, 현 Run **`run_6ba3f644755b`**이며 메인 `term_00d42610-7480-4fad-be06-ac8f4d9b131d`에 `msg_1f3acbd73c0d`로 새 주소를 보냈다. 이전 Run·handle은 역사이고 옛 inbox는0건이다. 메인 허용에 따라 종료 확인된 제품2·goal만 로컬 checkpoint에 남기고, 같은 검증 범위의 신규 `claude-opus-5-5`를 새 계약/입력/근거 폴더로 발행해 처음부터 판정한다. push·PR·병합 및 실제DB·D:·서비스/UAC는 별도 기존 경계를 유지한다.
## 2026-10-05 00:45 KST 크래시와 복구(이전 Run 역사)

- 메인 `msg_89a81699367a`(2026-10-04T16:00:09Z)가 PC 블루스크린·전체 프로세스 종료와 사용자 결정 **「지금 재개하고 재발을 지켜봄」**을 전달했다. 사용자 직접 입력으로 격상하지 않는다. 죽은 Sol `task_dcc811b02548` / `ctx_0e647dde42ff` / `term_53a75ba3-3a34-4e84-8c68-b6791773f80a`는 Orca에서 abandoned/failed terminal_missing이다. 크래시는 같은 계약·번호의 제품 확정 실패에 더하지 않는다. Sol 재기동 없이 디스크를 인수하고 신규 Opus로 진행하라는 메인 지시를 적용한다.
- 제품 쓰기 종료 status `msg_39d452061d25`(15:35:12Z)는 남아 있고 제품2파일은 당시 전체 실행 전후/부모 hash와 일치한다. **최종 report.md·worker_done·context 실제 준수 갱신은 없으며 보고 조각은 「크래시로 중단된 부분 결과」다.** 부모가 실제 diff·고정 입력·원시7파일을 대조한 `crash-recovery-settlement.md`는 Sol 완료 보고나 독립 판정을 대신하지 않는다. 최종 시험962/0/9와 Unity3/S3/stash2 보존은 `crash-recovery-source-audit.json`에서 확인했다. 비교48개 중 부모 맥락 메모의 복구 절 추가1개만 달랐고 제품·테스트·goal은 크래시 전 실측과 같았다.
- Sol 변경은 Environment.Common의 안전 사유 exact/template 분류와 New-TestDatabase의 내구 상태 미확정 안내다. 초기 시험62/16/1 중 환경 유래14건은 자식 PSModulePath만 빈 값으로 보정해 제품/테스트 무변경76/2/1로 복원됐고, 제품 수정 뒤78/0/1이 됐다. 전역 설정 변경은 없었다. wrapper 호출 편차·heartbeat300초 기준 초과3건(ask 제외22/12/8초)·최종 정산 누락은 부모 인수 기록에 보존하며 절차 전체 PASS로 바꾸지 않는다.
- 새 자기 pane `term_0448526f-a712-4397-a802-6d001d3ac077`, runtime `7dedef4a-9af4-4cb2-bd73-2da55712cc94`, **당시 Run `run_3038fbaaa770`**. 메인 새 pane `term_f4b4d463-b205-4f2a-96f6-3ff1b7f61598`에 `msg_d23ca5304cfb`로 회신 주소를 알렸다. 이전 Run `run_495ed90b4d12`·handle은 역사다. 과거 inbox의 미확인 Content 주소 안내는 이미 합의한 경계이며 새 작업 지시가 아니었다. 옛 외부 자원은 retained/nextAction none·terminal null로 남아 있어 임의 다른 pane 종료나 reset을 하지 않았다.
- 다음은 신규 `claude-opus-5-5` 한 세션의 처음부터 실사·독립 테스트·오프라인 진입 실행이다. 메인 지시로 부모 인수 문서를 제공하지만 부족한 필수 근거를 PASS로 완화하지 않는다. 이번 판정 뒤 잔여 결함은 메인에 반환하고 추가 수정 회차를 자동 열지 않는다. 실제 DB/서비스/UAC는 별도 승인 대기를 유지한다.
- Content 추가 경계는 `resume-content-symbol-boundary.json` / `resume-content-dead-guard-candidate.json`에 보존했다. Content가 전달한 메인 원천 대조에 따라 처치 receipt는 철회했고 기존 death gate의 회귀를 시험한다. GameDev 후속 후보 `enemy-hit-dead-guard`는 ApplyImmediateEnemyHit의 IsDead 사전검사 보강이며 기존 호출자 guard가 있다는 **Content 전달 관측**이다. 현재 제품 변경/직접 검증 실적으로 쓰지 않고 BACKLOG 등록은 메인/Rules에 조율한다.
## 정본 반영 전 적용 중인 사용자 결정

출처는 메인 `msg_ac202aff9b99`(2026-10-04T14:56:49Z)와 읽기 전용 `C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-04/`의 아래 파일이다. 이들은 메인이 전달한 사용자 결정이며 이 세션의 사용자 직접 입력으로 격상하지 않는다. Rules가 운영 정본으로 옮기기 전까지 이 goal과 다음 계약에 적용한다.

- `plan-scopes-draft.md` 사용자 원문: **“1) GameDev: 영속화 goal을 첫 PR에서 닫고 마감용 「게임 저장 고리」로 → A 첫 PR에서 닫음”**. 첫 PR 뒤 저장소·제한 복구 두 번째 PR은 이번 goal에서 제외한다. 다음 순서는 인스턴스 맵 수명→게임 저장 고리이며 자동 착수하지 않는다.
- `HANDOFF.md` 결정1 사용자 원문 **“OK 그렇게 가자”**: 범위를 만들 것·건드릴 곳·하지 않을 것·관찰 가능한 완료조건 및 PR 경계로 적고 착수 전 메인이 확인한다. 범위 안 완료조건을 막는 결함만 수정하며 같은 산출물 수정이 3회를 넘으면 메인 체크포인트 알림을 보낸다.
- `routing-draft.md` 사용자 원문: **“1) 작업 유형별 표와 모델 평가 권고 16개 배치 → A 승인 · 2) 설계 4범주 작업은 구현 전에 Fable이 불변식 목록 작성(시범) → A 시범 도입 · 3) Sol effort 시험은 보류하고 max 유지 → A 보류 · 4) 작업별 자동 기록 범위 → A 둘 다 · 5) 검증 강도 2등급 4주 시범 → A 시범 도입 · 6) 규칙 문서는 지금 늘지 않게만, 가지치기는 10-31 평가 때 → A 증가만 멈춤 · 7) 사람용 코드 따라읽기 문서는 지금 만들지 않음 → A 지금 안 함”**.
- 위 라우팅의 즉시 적용: 계약 경로 실존 확인, 예문 대신 요구 항목/판정 기준, 관련 규칙 절 원문만 포함하고 제외 이유 표시, 검증 harness 파일화/판단별 Assert 변수, 설계 결정 차단 시 한 줄 대안, 보고 수치의 원시 파일 추적. 판정에는 `verifies` 대상 Task 및 결함 번호·심각도·차단·귀속과 「설계 관찰(비차단)」를 둔다. 설치·I/O 진입 경로는 stub 뒤에서라도 1회 실행해야 하며 함수 정의 점검만으로 PASS를 주지 않는다.
- 이번 goal의 검증 등급은 **강**(DB·설치·I/O·실패 수명)이다. 신규 Opus의 실사·독립 테스트·실제 진입 경로 실행이 필요하고 오프라인 진입과 실제 SQL/U-01을 구분한다. 등급 시범은 10-31 재평가다. `HANDOFF.md` TDD 결정 원문 **“A”**는 새 goal부터 적용하므로 진행 중이던 이번 goal은 기존 방식이다. 구현 Sol `gpt-6.1-sol` max, 검증 신규 `claude-opus-5-5`를 유지한다.
- `deadline-roadmap-draft.md` 사용자 원문 **“1) 필수선 → A 필수선 승인 · 2) Docker 분산 → A 마감 뒤 · 3) 게임을 두 파트로 → A 둘로 나눔 · 4) 도구·운영 파트는 마감까지 게임 속도를 돕는 일만 → B 오늘 순서대로”**. 11월 첫째 주 졸업작품 전시회 평가에 PPT·플레이 녹화로 마을 광장→길드 거점→파티 인스턴스 던전→보상→성장→재접속 유지를 보인다. 10-28 기능 동결 목표, 상점·연구 2순위다.
- Fable 사전 설계 시범은 Content 첫 goal 및 GameDev의 **다음** 인스턴스 맵 수명·게임 저장 고리에 한정한다. 구현 전 불변식/전제 출처를 goal-review.md에 쓰고 메인이 원문 확인한 뒤 구현한다. 이번 05/06 마지막 수정에 새 사전 Fable 관문을 소급하지 않는다. ORCA의 같은 계약·번호 확정 실패 3회 규칙은 유지한다.

## 현재 범위·PR 경계

| 항목 | 승인 초안에 맞춘 범위 |
|---|---|
| 만들 것 | INSTALL-05/06 수정·독립 재검증, 승인 시험 DB의 최종 설치와 실제 엔진 U-01 판정, 첫 PR과 G4 정산·종료 기록 |
| 건드릴 곳 | `99_Tools/database/`의 설치 수명·검증 도구와 해당 테스트, 이 goal·필요한 설치/실행 근거. 마지막 Sol의 제품 쓰기는 `test-environment/Environment.Common.ps1`와 `New-TestDatabase.ps1` 두 파일로 한정 |
| 하지 않을 것 | 001 변경, 저장소/제한 복구 두 번째 PR, `PersistenceRecovery`, 실제 recovery Windows principal 최소권한 실증, D1a 시험 행렬 전체, crash 복구, 게임/Unity 연동·PDL·다른 파트 구현, 서비스/UAC/SQL 무승인 실행 |
| 관찰 가능한 완료조건 | 05/06 독립 PASS와 01~04 보존, 승인된 실제 엔진의 최종 설치·U-01 원시, 필요한 CI·문서 실사, 정확 head에 대한 사용자 병합 승인, G4 인수 및 goal 결과·Gardener·종료 점검 |

**PR은 설치·엔진 판정 한 경계다.** 첫 PR 병합 뒤 G4·결과 기록·Gardener·메인 종료 점검·R-8로 이 goal을 닫는다. 저장소·복구 두 번째 PR을 이어 여는 이전 계획은 철회됐다. 이번 마지막 수정 1회→독립 재검증 1회 뒤 결함이 남으면 원시/번호와 다음 할 일을 메인에 보고하고 추가 회차는 열지 않는다.

## 이전 중간 마감 결정(역사)

이 절의 새 착수 동결·PR 없음·전체 goal 미종료는 당시 결정이다. 현재 실행 범위는 위 재개 결정과 아래 범위가 우선하며, 원문 기록을 당시 성공/승인으로 재해석하지 않는다.

### 사용자 중간 마감 결정

메인 `msg_06ab6853f32e`(2026-10-04T11:10:02Z)가 사용자 원문 **“결정대기 관련에서 현황판에 업데이트가 안됬네, 일단 A긴 해”**를 전달했다. 메인이 제안한 A안은 “지금부터 새 착수 동결: 진행 중인 검증·결함 수정 루프만 끝내고 새 goal·PR 범위는 열지 않는다”다. 메인 전달이며 사용자 직접 메시지로 격상하지 않는다.

- GameDev 마감 단위는 INSTALL-05 잔여·06의 **신규 Sol1회→신규 Opus1회** 수정·재검증이다. 이 마지막 회차 뒤 결함이 남으면 번호·원시·다음 할 일을 이 goal에 기록하고 멈춘다.
- 통과하면 현재 작업 브랜치에서 체크포인트 commit/push까지 한다. 새 PR은 만들지 않는다. 새 goal·계획·PR 범위를 열지 않고 다음 후보는 기록만 한다.
- 사용자 행동이 필요한 D: 연결, 실제 DB/G2, 화면 확인과 서비스 시작은 마감 뒤로 미룬다. 기존 실행 초안은 승인되지 않은 상태를 유지한다.
- 마감 지점에서 메인에 **“중간 마감 도달”**을 보고한다. 상태·branch/HEAD·미커밋 파일 수·잔여 결함/후보·재개 첫 단계를 포함하고, 이후 새 작업 없이 대기한다. 이 중간 마감을 전체 goal 완료나 Gardener/R-8 완료로 표현하지 않는다.

### 분할 시간 초과와 미사용 pane 종료

메인 `msg_6454aa8d5367`(2026-10-04T11:22:37Z)이 CLI 기동 오류 뒤 **현재 작업 트리의 체크포인트 commit/push(PR 없음)**를 지시했다. 이후 `msg_c5c2fc99f8e1`(11:25:24Z)이 실제 pane4개 생성을 정정했고, `msg_8b903351e9f0`(11:26:19Z)이 **사용자 결정 A: 남은 Sol pane4개를 모두 닫고 마지막 수정 회차 없이 중간 마감**을 전달했다. 사용자 결정의 원문 출처는 이 메인 전달 메시지이며 사용자 직접 입력으로 격상하지 않는다. 미해결05·06을 그대로 기록해 마감한다.

- 담당 Astra의 split은 `Timed out waiting for split pane handle`을 반환했다. 원시는 `installation-final-fix-launch.json`(요청 `88aba1a8-cef3-4a71-b095-41cc1646d18d`)과 `final-fix-split-timeout-terminals.json`이다. CLI 오류는 pane 미생성을 뜻하지 않았다. Task·Dispatch·제품 쓰기는 시작하지 않았다.
- 메인이 보고한 대리3회: 11:19:30Z `f217bd79-2459-463c-8fd8-b4c97dfcb103`, 11:20:34Z `28d595f1-73a0-407e-a02a-8cc7a2a08d0c`, 11:20:56Z `4f61ce9b-ab67-454b-9bf3-6782f9f00745`. 모두 같은 timeout을 반환했다. 당시 terminal list7개/새handle 없음 관측은 늦은 등록 전의 순간 상태였으며 잔여pane 없음의 최종 근거가 아니다. 메인 관측의 원문은 `delivery_7e579977e120.json`, 정정과 종료 결정은 `delivery_66e1b721c0e8.json`에 보존했다.
- **4회 모두 실제 pane 생성**: `term_cd61ed89-c7ee-48d2-99a5-bf8725ec32a1`, `term_69745747-339b-4347-927b-91566629d23c`, `term_de69b3c4-dd25-4c07-9eed-7b8f984e5ba7`, `term_061b904e-9cb0-4de1-b7f2-86410851f553`. 메인이 종료 직전에 네 개 모두 GPT-6.1-Sol max 첫 화면의 빈 prompt·draft 없음·Task/Dispatch 미연결을 재확인한 뒤 `terminal close`로 종료했다(4건 모두 ok). 이 생성/종료는 메인 전달 관측이며 Astra가 attach·입력·종료한 것이 아니다.
- 준비 계약 `installation-final-fix-contract.md` SHA256 `45A82670D5A7933A117D77258D3C7AC54659A76CC7DF00B64B1A449FF7FAC10E`, 입력 `installation-final-fix-inputs.json` SHA256 `3B91112EA3EB564F4D1C931DD5357F35444CCF6A1AA742ED8D2076B5EE27FDE8`. 두 파일은 기동 전 역사 입력이며 **미발행**이다. 제품 범위는 Environment.Common/New-TestDatabase 두 파일, INSTALL-05 잔여·06의 마지막 Sol→Opus 한 회차다.
- 새 탭 생성·배치 우회·모델 대체·Astra 직접 구현은 하지 않는다. 메인 현황판 `term_4d1431ae-66ff-4f50-b396-bad517e0672d`는 건드리지 않는다. 분할 실패 원인 확인은 마감 뒤 메인이 따로 맡는다.
- **재개 첫 단계: Sol 기동 경로 확인 → installation-final-fix 계약으로 마지막 회차(새 Sol → 새 Opus).** 재개 승인 후 현재 HEAD·goal/메모 hash를 다시 고정하고 위 역사 입력을 그대로 실행 권한으로 쓰지 않는다. **분할이 시간 초과로 끝나면 바로 재시도하지 않고 기다린 뒤 terminal list를 다시 확인한다.** 마지막 회차 뒤에도 남으면 새 회차를 열지 않고 번호·원시·다음 할 일을 기록한다.
- 최초 체크포인트 `4bdbbcb1179fb83b1bd1af73ca973247a1aad9d8`의 실제 commit/push·보존은 로컬 `mid-closeout-checkpoint.json`, 이 정정 후 최신 결과는 `mid-closeout-final-checkpoint.json`에 둔다. 메인 `msg_398ecf11f1c9`의 요청대로 정정만 후속 커밋으로 남기며 중간 마감은 제품 PASS·G2 승인·실제 DB 성공·전체 goal 종료를 의미하지 않는다.

## 재개 지점

- 작업 경로 `C:/Dev/DawnHolder_Project`, branch `feat/persistence-integration-20261004`. 최신 main 기준 `3f0cb5e2861574ea1e6b092875de27694897b21d`에서 시작했다.
- 이전 종료 checkpoint `f137bbb6ca2a7b5bc424769f8083d2d9814dc32c`를 `2c3522e075cd3f68804f80dec1735f368669eb0e`로 cherry-pick했다. 이전 goal만 +9/-2이며 이 목표의 첫 PR에 포함한다. 원본 branch/commit은 유지한다.
- Unity 실물3 SHA·skip-worktree S3·stash2는 전환 전후 일치한다. 근거는 `.backups/verification/2026-10-04-persistence-integration/branch-checkpoint.json`이다. 상태가 깨끗하다는 Git 출력만으로 사용자 파일 보존을 판단하지 않는다.
- 현재 재개는 위 크래시 복구 절을 따른다. 이전 Run `run_495ed90b4d12`와 `msg_ceabbe68669d`는 크래시 전 주소 이력이다. 실제 DB/G2는 D:·서비스·실행자/명령/시간 창 승인 대기이며 검토 초안의 ExecutionApproved=false/G2=null을 유지한다.
- 이전 Run `run_da60626aa8de`는 역사다. 새 세션은 재사용하지 않고 새 Run 바인딩 직후 메인에 회신 주소를 알린다. 첫 Opus Task `task_19f35f0a16b0`/Dispatch `ctx_7aa1fb01a3ce`는 작업 완료·정산·정확 pane 종료했다. task outcome=succeeded는 실사 완료이며 제품 판정은 FAIL이다.
- 수정 Sol Task `task_10ee107139ee`/Dispatch `ctx_4fb9710dc625`도 쓰기 종료·원문 대조·release·정확 pane 종료했다. 자기 점검은 독립 판정으로 쓰지 않고 위 신규 검증자에게 넘겼다.
- 첫 재검증 Opus Task `task_32164d72f32a`/Dispatch `ctx_2a0a1c3853f1`는 `msg_1a5fa823d2ce`로 쓰기 종료했고 원문/원시 대조·release·정확 pane 종료했다. 해당 Task 성공은 실사 완료이며 제품 판정은 통과 아님이다.

## 현재 실사 결과와 보류

- 최신 판정 `installation-recheck-2/verdict.md` SHA256 `1A85FE1DB53A7F450DC3BB8CC9703682C3A3BFCDD8EB682A1158E3C5A038FD69`: 01~04 해소, 05는 Install의 DB identity 변경 사유가 Unclassified로 사라지는 잔여, 06은 완료 journal 쓰기 실패 뒤 디스크 Pending인데 not Pending이라고 안내하는 새 결함이다. 둘 다 fail-closed와 자원 보존은 유지하지만 미해결이다. 마지막 수정 회차는 CLI 시간 초과 후 사용자 결정 A에 따라 미사용 pane4개를 정리하고 미발행으로 보류했다. 05는 첫 재검증 실패1회, 06은 최초 발견이다.
- 부모 `recheck-source-audit.json`에서 최종 전체960/2/9·기존 lifecycle53개 전부 유지·제품35와 기타 고정 입력 불변·변경 테스트1개·Unity3/S3/stash2 보존을 대조했다. lifecycle76/2/1이며 새 OBSERVED1은 비-int SqlNumber 합성 입력의 journal 잔존이다. 실제 비-SqlException fallback 정수는 Closed 연결 helper에서 관측했고 SqlException 분기는 소스 계약만 확인했다. 실제 SQL은 미실행이다.
- 재검증 절차 관측: baseline 실행과 첫 편집의 겹침은 원본 harness hash/53행 동일 근거로 범위를 확인했다. 단계별 inbox check 누락으로 부모 지시 확인이 늦어 추가 재실행이 필요했고, 무효 launch2/parser2·자기 시험 정규식 오류1도 원문 §11에 있다. `msg_7c384bec95e7`로 즉시 메인에 보고했다. 부모 서버 시각 대조의 heartbeat 최대 간격221초/300초 초과0과 별개이며 **절차 전체 PASS로 표현하지 않는다**. 원문은 보존하고 `recheck-settlement.md`에 인수 범위를 남긴다.
- 판정 원문 `.backups/verification/2026-10-04-persistence-integration/installation-review-1/verdict.md`, SHA256 `80C03344EBB19D9C93974030497B1C5C5F7F956F3531DE8B8FDF6BC75B4E825F`. 새 시험17 PASS/36 FAIL(상류 실패 연쇄29건 포함), 기존6 suite 전후884 PASS/0 FAIL/8 OBSERVED다. 실제 DB·U-01·OS/ACL/DPAPI는 미실행이다.
- 최초 실사에서는 INSTALL-01 연결 문자열 builder 대입과 INSTALL-02 두 번째 journal 교체가 차단이었다. 03 동등 경로 lock, 04 cleanup의 계정명 상수, 05 안전한 중단 이유 소실/실제와 다른 기록 안내를 포함해 메인 `msg_16b3c72e78a6`가01~05 신규 Sol 수정을 지시했다. 외부 예외 원문 대신 비밀 없는 고정 사유와 실제 journal 기록 여부만 전달한다는 요구를 적용했다. 이후 해소/잔여는 위 최신 판정을 따른다.
- 실제 DB 보류: 읽기 전용 OS 관측에서 `MSSQL$SQLEXPRESS`는 Stopped/Manual, 등록 실행파일은 `D:\MSSQL2025\MSSQL17.SQLEXPRESS\MSSQL\Binn\sqlservr.exe`, 현재 D:는 DriveNotFound다. 단순 서비스 시작 요청은 철회했고 메인 `msg_d3a5681379d0`가 D: 원래 환경을 사용자에게 확인 중이다. 볼륨·서비스/UAC·재설치·경로 변경은 승인되지 않았다. DB 폐기 승인과 서비스 종료를 구분하며 원래 Stopped 복귀 여부는 G4 별도 결정이다.
- 신규 Opus 보류 이력: 메인 `msg_b4cec1593dd6`가 잠시 보류했으나 **`msg_e7e773056b87`로 해제**했다. 메인이 전달한 사용자 원문은 “1) Claude 주간 사용량 96% - 화요일 20:00 초기화 전까지 우선순위 → C 코멘트 「Max 20으로 플랜 방금 업그레이드 완료」”다. 신규 `claude-opus-5-5`로 재검증하며 파트당 동시 검증자1명·기존 범위는 유지한다.
- D: 사용자 결정: 메인 `msg_da0d5b05b80e`가 “5) SQL Express - 설치된 D: 드라이브가 없어 DB 단계가 막힘 → A D: 드라이브 다시 연결”을 전달했다. 사용자가 직접 연결하며 이후 메인이 읽기 전용 재확인 후 서비스 시작을 별도 사용자 판단으로 올린다. 이 답은 서비스/UAC/DB 접속 승인이 아니다. 전달 시점에는 여전히 C:/F:만 관측됐다.
- 수정 결과: `installation-fix-1/report.md` SHA256 `339F6254EF5DA2391D152D99E39326FFE2275D7F9B9C197D7605D3576E1F0A6B`. lifecycle17/36→Sol 최초51/2→진단 보완53/0, 기존6 suite884/0/8 유지. 부모 `fix-source-audit.json`에서 실제 집계937/0/8·제품4변경/31불변·테스트2 및 Unity3/S3/stash2 보존을 대조했다. OBSERVED8은 검사 대상 제외4건과 실제 엔진 미관측4건이며 성공으로 합산하지 않는다. 정확 Name/Detail은 audit와 report에 있다.
- 수정 절차 정산: `fix-settlement.md`에 표식 교정1건과 cadence5건(336/345/353/313/436초, 최대초과136초)을 개별 관측으로 보존했다. 메인 `msg_7cf40e2e0771`, `msg_d0eae81bb1a1`, `msg_9624ef1d523c`를 적용하며 절차 전체 PASS로 바꾸지 않는다. 신규 검증에 이 이력과 자체 시험의 범위/미실행을 전달한다.
- 절차 기록: heartbeat 임시 기준 `msg_9cdcc96cf620`과 개별 결정 정본 `msg_c9552132e2f7`을 적용했다. 1,656/803/764초 세 공백과 판정문 기록 누락은 **미준수**로 보존한다. 메인 `msg_16b3c72e78a6` 결정에 따라 원문을 덮지 않고 부모 `review-settlement.md`에 보충했다. 절차 전체 PASS로 바꾸지 않으며 제품 FAIL 목록만 수정 근거로 인수한다. 초기 입력의 helper3개 누락/보충과 종료35개 hash 실측도 이 부록에 있다.

## 결정 출처와 선행 결과

메인 Claude `msg_5322a941f836`(2026-10-04T07:54:31Z)가 사용자 원문 **“3) 다음 목표 - 시험 DB에 SQL 설치·엔진 판정·저장소 연결 → A 진행”**을 전달했다. 메인 전달이며 이 세션의 사용자 직접 입력으로 격상하지 않는다. 이전 [SQL 구조·오프라인 goal](../2026-10-02-persistence-repository/goal.md)은 PR169/171 병합·Gardener·메인 정산 뒤 종료했다. 새 목표는 이 승인 범위만 수행한다.

메인 `msg_837c528baf27`(2026-10-04T08:04:54Z)는 초안 SHA256 `E3EA2AFB65767B97AADB16B422E1DDF24EC54F3D121D4E49727FBFF50BBCACE0`의 범위 일치와 DB 접속 없는 첫 위임을 확인했다. G2 executor·명령·시간 창 및 관리자/UAC 단계는 **실행 전에 메인에 결정 요청**으로 보내고, 사용자가 권한 창을 확인할 수 있도록 메인이 대시보드에 올린다. 현재 비승격 `YYH_DESKTOP\bass1`(SID 끝1001)을 관측했으며 승격·UAC 실행은 하지 않았다.

- 선행 제품 merge `3e07e1b0b318881701b5b7bab8adbe087b596420`, 종료 기록 merge `bca2adf0c1b16ef467212151a9dc9bd3680bff79`. 이전 검증/CI는 새 실제 SQL 실행 근거가 아니다.
- G0 `msg_78c5c1647b7c`, G1 `msg_21ae101a52db`의 정확 범위는 이전 goal의 [승인 구간](../2026-10-02-persistence-repository/goal.md#사용자-세-안건--추천안-승인g1)을 따른다. 최종 배포는 001~004와 modules/manifest이며 과거 002~013 계획은 사용하지 않는다.
- 메인 `msg_78db7cfb8c15`가 승인한 로컬 `successor-goal-draft.md`(SHA256 `DB20282EEC7089025C0BDCBE34436DD8196DC48AF9CA03E1AF1C756DD138D578`)의 조기 엔진 게이트를 승계한다. 오래된 초안의 첫 PR마다 Gardener/R8 문구는 최신 [R-8](../../../00_Document/operations/ORCA.md#r8-astra-lifecycle)의 **전체 goal 종료** 기준으로 적용한다.
- [영속성 로드맵](../../milestones/2026-09-30-contracts-persistence/roadmap.md)의 저장소·격리 검증 단계 후속이다. Q-1B는 이 목표의 DB 통합 뒤 다음 계획으로 다루며 자동 착수하지 않는다.

## 범위와 보존 동작

| 책임 | 할 일 | 보존·제외 |
|---|---|---|
| SQL 설치·엔진 | 승인된 시험 DB에 최종 001 및 migration002~004/modules 설치, 검증기·설치 수명 도구의 실제 결함 수정, 최초 빈 scalar5개 U-01 실증 | 001 원문/checksum, 공개9RPC·29열·결과/오류/grant·tx 계약. 설치 불가 과거 중간 tree 사용 금지 |
| 독립 검증 | database/tests fixture/fault 및 Test-Database의 이번 설치·U-01 관련 계약 | 제품은 Sol만, 독립 테스트/판정은 신규 Opus만. 저장소·WSL 연결·최소권한 전체 행렬은 이번 완료조건에서 제외 |
| 통합 문서 | 설치/실행 안내·판정/미실행·첫 PR/자원 정산 | D2 소비 계약·운영 정책·다른 파트 파일의 무관한 정리 제외 |

저장소 및 `Microsoft.Data.SqlClient 6.1.7` 추가/restore는 이번 scope에서 제외한다. 제품 SQL의 9RPC·29열·JSON·결과/오류/grant·transaction 계약은 보존한다. 새 operation ID 자동 생성·DB/메모리 fallback·부분 결과 성공 판정을 추가하지 않는다. 게임 틱에서 I/O 완료를 기다리지 않는 조건은 후속 게임 저장 고리에도 유지한다.

## DB 접속 경계 G0·G1·G2

| 경계 | 고정 내용 |
|---|---|
| G0 범위/설계 | 첫 PR의 설치·엔진 판정만. 제품/테스트/판정 소유를 Task별 명시. 새 라이브러리/범용 도구 권한을 만들지 않음 |
| G1 대상/수명 | 로컬 `.\SQLEXPRESS`, 시험 DB **Dawnholder_Dev_D1b_20261002 하나의 신규 생성→최종 설치→시험→최종 폐기 수명**. 기존 동명 자원이 있으면 중단하며 채택/DROP/재생성하지 않음. 첫 PR 검토 중 보존하고 아래 G4 확인 뒤 정산 |
| G1 principal | runtime SQL login **dh_d1b_runtime_20261002**, 해당 DB dh_runtime만. recovery **YYH_DESKTOP\dh_d1b_recovery** 비관리자 Windows 계정, 해당 DB dh_recovery만. 동명 계정 채택 금지 |
| G1 연결/TLS | 관리 local shared memory, Windows/WSL `tcp:127.0.0.1,14330`. Encrypt Mandatory(기존 도구 true), 해당 fixture만 TrustServerCertificate=true. 저장소 기본 false·자동 fallback 금지 |
| G1 binding | slot1, AccountId `828e39df-ba5d-4209-86ea-4e9ec1a43ed5`, CharacterId `686e8071-daa1-404d-af7d-d4bb2442748c` |
| G2 설치 경로 | 실제 실행할 provision/install/cleanup 최종 diff·hash, 비밀 없는 승인 계획/manifest, 정확 executor/명령/시간 창·ACL/identity·부작용을 신규 Opus가 **최초 접속 전에** 정적 실사 |
| G2 복구 경로 | 이번 goal에서 제외. 미구현 launcher/저장소나 recovery 실증이 초기 설치 실사로 승인됐다고 하지 않음 |

기존 승인에는 DB 존재기간 만료가 정해져 있지 않다. Astra가 실제 작업자/명령에 대한 시간 창을 따로 고정하며 자원의 한 번 수명과 구분한다. G2 전에는 SQL 접속·비밀 읽기·외부 구성을 하지 않는다. 관리자/UAC 단계는 정확 명령·실행자·시간 창을 메인에 미리 전달한다. 사용자 직접 실행이 필요한 경우 메인이 요청한다.

G2 통과 후 첫 접속은 DB_NAME/instance/endpoint·ProductVersion/patch·collation/RCSI·schema/module hash·binding·SQL SID/ORIGINAL_LOGIN/role/group·legacy writer 부재를 확인한다. 실제 관측값을 기대값으로 자동 채택하지 않는다. runtime 양성 시험은 실제 권한 대조 뒤에만 진행한다. patch가 바뀌면 golden vector를 다시 검증한다.

비밀은 채팅·goal·argv·로그에 넣지 않는다. 목표 전용 ACL+DPAPI와 자식 stdin/pipe를 쓰며 실제 recovery Windows identity를 parent 관리자 성공으로 대체하지 않는다. 서비스 설치/설정/재시작·registry·방화벽·인증모드·인증서 저장소·SAC·전역 설정·기존 principal 변경은 범위 밖이다. 접속 조건이 없으면 상태·영향을 메인에 보고한다.

## 작업 순서·소유·PR 경계

1. **범위 확인·마지막 수정:** 메인에 새 범위와 G4안을 전달한다. 확인 뒤 새 고정 입력/계약으로 신규 Sol(gpt-6.1-sol max)의 05/06 수정 1회와 쓰기 종료 뒤 신규 Opus(claude-opus-5-5)의 독립 재검증 1회를 진행한다. 등급은 강이며 설치 진입 경로를 stub 뒤에서라도 실행한다. 잔여 결함은 다음 회차를 자동 발행하지 않고 메인에 보고한다.
2. **조기 엔진 판정:** G2 통과한 명령만 한 실행자·한 DB writer 시간 창으로 수행한다. 승인 DB를 한 번 만들고 최종001~004/modules를 설치한다. 신규 Opus가 schema/설치와 빈 scalar5개 U-01을 실제 엔진에서 먼저 판정한다. 실패는 raw 보존→새 Sol 좁은 수정→새 Opus 재검증으로 해소한다.
3. **첫 PR — 설치·엔진 판정:** 필요한 설치/검증 도구 현행화, 실제 schema/U-01 근거, goal과 이전 종료 checkpoint를 묶는다. 제품 SQL 외부 계약은 유지한다. 정확 PR 번호·head·CI·미실행·G4 상태를 메인에 보고하고 해당 PR의 사용자 명시 병합 승인을 받는다. DB는 검토와 재현이 끝날 때까지 보존한다.
4. **전체 goal 정산:** 첫 PR 병합 뒤 메인이 확인한 아래 G4를 수행하고 결과를 기록한다. 신규 Opus Gardener→메인/사용자 종료 점검→R-8로 종료한다. 같은 goal의 저장소·복구 두 번째 PR은 열지 않는다.

실제 DB 대기 중에는 메인이 허용한 다음 「인스턴스 맵 수명」 goal 초안·Fable 검토 준비만 최신 main의 별도 branch에서 할 수 있다. 현재 checkout의 쓰기/검증 중 branch를 전환하지 않는다. 새 공간·기동이 필요하면 메인과 배치를 확인한다. 구현은 다음 goal 범위와 Fable 원문을 메인이 확인한 뒤이며, 이 문구만으로 다음 goal을 자동 착수하지 않는다.

PR 분할은 줄 수 상한이 아니라 독립 판정 가능한 경계에 따른다. 엔진 결과가 완료조건/범위를 바꾸면 의존 작업 전에 메인 판단을 받는다. 범위 안 제품 결함은 승인 반복 없이 고치며 최신 ORCA의 같은 계약/번호 확정 실패 집계 규칙을 따른다.

Astra는 goal/위임 계약/비밀 없는 실행 계획/결과/Git를 소유한다. Sol은 할당된 설치 제품 파일만, Opus는 할당된 테스트·판정만 쓴다. `Test-Database.ps1`의 검증 동작과 독립 시험 장치는 Opus 소유로 배정한다. 동시 DB writer와 같은 파일 동시 쓰기는 금지한다. Content와의 `GameSession`·`GameWorld`·`HandlerRegistry` 소유 및 PDL/생성물 단일 writer 조율은 유지하되 이번 설치 Task에 게임 파일 쓰기를 넣지 않는다.

## 관찰 가능한 완료조건

이번 필수 범위는 아래 설치·U-01이다. [D1a verification-plan](../2026-10-01-persistence-technical-design/verification-plan.md) 및 [technical-spec](../2026-10-01-persistence-technical-design/technical-spec.md)의 관련 SQL 계약을 유지하되 S01–S11/C01–C13/O01–O07/U01–U11/R01–R05 **전체 행렬을 이번 통과로 주장하지 않는다**. 제외는 사용자 범위 결정이며 시험 기대값을 고쳐 통과시키지 않는다.

| 완료조건 | 필수 관측 |
|---|---|
| 실제 엔진 U-01 | scalar FOR JSON5개의 빈 집합/SQL NULL/명시 `[]` 결과를 실제 엔진에서 수집. 최초 설치/catalog read, malformed/unknown/drift 거부. D1a 시험 ID U01과 별개이며 source/fake/대상0으로 대체 불가 |
| schema/설치 | 001 checksum·기존 게임 행 보존, 최종001~004/modules 설치·재실행 무변경·중간 실패 원자성·catalog 변조 거부, CHECK parent_column_id/definition 실제 대조 |
| 설치 도구 수명 | 05/06 독립 PASS, 01~04 및 비밀 억제·identity 대조·한 번 수명·journal 보존. 오프라인/실제 SQL·권한 실행을 구분하고 entrypoint 실행 근거를 제시 |
| 환경 구분 | 실제 엔진의 OS·SQL version/patch·연결 경로와 실행자를 기록. 오프라인 stub 성공을 DB·OS계정/ACL/DPAPI 성공으로 치환하지 않음 |
| 문서/규칙/CI | 사전 메모·관련 규칙 원문·실제 준수 위치·독립 판정 연결, 가독성/책임/배치·30초 탐색 실사, 해당 Changed/서식/빌드/테스트/CI의 실제 입력과 원시 근거 |
| G4 정산 | 메인이 확인한 아래 정산안에 따라 실제 생성 자원과 미생성 자원을 구분하고, 정확 identity/manifest·실행 원시·잔여를 기록. 미실행/미확정 정리를 완료로 기록하지 않음 |

각 엔진 시나리오에 명령·UTC·exit·OS/사용 도구·SQL patch·코드 SHA·비밀 없는 DB identity·before/after·fault 위치·commit 여부를 남긴다. 미실행은 환경과 영향을 적고 메인 판단 없이 완료로 합치지 않는다. 저장소/복구·실제 Ready/actor/Host/Unity·L01–L04·E01–E04는 이번 성공에 넣지 않는다.

## G4 정산안(메인 확인 완료)

메인 `msg_b7074e6a1c4a`가 아래 정산안을 승인했다. 실제 SQL/서비스 실행은 별도 승인 경계를 유지한다. 다음 게임 저장 고리에서 기존 시험 자원을 이어 쓰지 않고 검증된 설치 도구로 새 승인 환경을 설치한다.

- **첫 PR 검토 중 보존:** 승인된 하나의 DB와 이번 설치에 실제 생성한 자원은 재현에 필요한 동안 보존한다. 이력이 없는 동명 자원은 채택하지 않는다. 다음 goal용으로 연장 보존하거나 같은 이름으로 재생성하는 권한은 만들지 않는다.
- **병합 뒤 정리:** 실행 중 operation/connection·임시 trigger/권한·자기 소유 프로세스를 정산한 뒤 DB identity를 다시 확인하여 승인된 단일 시험 DB를 최종 폐기한다. 이번에 생성된 login·Windows 계정·credential은 SID/manifest를 대조해 기존 승인 cleanup 경로로 정리한다. 미생성 자원은 정리 불필요로 기록한다. identity 불일치·활성 owner·정리 실패에는 강제 DROP/rollback·타 세션 kill·profile 임의 삭제 없이 잔여를 메인에 보고한다.
- **종료 뒤 보존:** 비밀 없는 승인 계획/manifest·journal·schema/U-01/정리 원시·판정·PR/CI/결정 기록은 로컬 근거와 goal에 보존한다. 비밀 값·credential 파일을 검증 자료로 복제하거나 Git에 넣지 않는다. 이후 게임 저장 고리는 새 범위/자원 승인을 사용한다.
- **별도 사용자 판단:** D: 연결은 사용자 직접 행동이다. SQL 서비스 시작·UAC·G2의 정확 실행자/명령/시간 창은 실행 전에 메인을 통해 승인받는다. DB 폐기는 서비스 종료 승인이 아니므로 원래 Stopped 상태로 복귀할지도 별도로 확인한다. 정산안 승인만으로 외부 실행을 시작하지 않는다.

## Content와의 기술 경계

- Content `msg_47ffacf96779`와 GameDev 회신 `msg_58eaccfd4a15`의 합의: 기존 PDL 1~34의 순서·ID를 보존하고 Content PR1은 C_InventoryRequest / S_InventorySnapshot / C_ItemUse / S_ItemUseResult를 35~38로 append한다. Content PR1 병합 뒤 GameDev는 39부터 사용한다. 패킷 수가 바뀌면 양쪽 goal의 범위를 다시 대조한다.
- PDL·생성물·ProtocolVersion·등록의 현재 단일 writer는 Content다. GameDev는 이번 DB 도구 단계에 게임 파일을 쓰지 않는다. Content checkout에서 GameSession·GameWorld·HandlerRegistry의 최소 등록·처치 콜백·세션 정리 연결을 순차 작성하도록 허용했으며 예정 심볼·변경 경계와 쓰기 종료/head를 요청했다. 기존 quest/party 순서·상태 소유는 보존한다.
- 저장 인터페이스는 GameDev 소유다. 새 DTO/API는 아직 미확정이므로 Content는 임의 확장 hook 대신 메모리 상태의 소유/읽기/변경 경계를 정리하고 다음 게임 저장 고리에서 조율한다. Content 현재 Run `run_add8d9f825f4`, 우리 회신은 `run:run_495ed90b4d12`다. 이 합의는 게임 저장 구현 착수 승인이 아니다.

## 범위 밖과 점검 지점

- 중간 점검: 마지막 수정/독립 재검증, 실제 DB 행동 대기, 첫 엔진 판정/PR 검토. 전체 점검: 첫 PR 병합·G4·결과·Gardener 뒤. Q-1B/ADR-035는 첫 PR 뒤 다음 계획에서 검토하며 자동 착수하지 않는다.
- 마감 뒤 후보 `persistence-recovery-post-deadline`: 기존 저장소·제한 복구 두 번째 PR 묶음, PersistenceRecovery, Windows principal 최소권한 실증, D1a 시험 행렬 전체·crash 복구. 원천 `msg_ac202aff9b99` 및 plan-scopes-draft.md. 공용 [BACKLOG](../../../00_Document/operations/BACKLOG.md) 등록은 소유자와 조율 중이며 아직 등록 완료로 보고하지 않는다. 마감용 저장소·확장 스키마·간단 로그인/게임 저장은 승인된 다음 「게임 저장 고리」 범위에서 다시 설계하며 이 보류 묶음의 자동 재개와 구분한다.
- C1(PS·SQL 120자 초과 식), C2(참조0 SQL 지역 선언)는 **검사 후보 채택/구현을 보류**한다. 기존 작성 규칙을 면제한다는 뜻은 아니다. 근거는 이전 goal의 Gardener와 메인 msg_5322a941f836. 공용 BACKLOG 등록/ID는 담당 파트와 조율하며 여기에는 상세 후보 장부를 만들지 않는다.
- Rules의 SQLFluff 재시범/구조 CLI·tests CI 연결은 기존 예정 후속과 소유를 확인하며 이 목표에 자동 포함하지 않는다. 새 기능·정책·도구·다른 영역 정리·게임 연동은 다음 계획 후보로 남긴다.

## 원문 근거

- 이번 로컬 근거 root: `.backups/verification/2026-10-04-persistence-integration/`. 초기 사전 메모 `astra-context.md`, 후속 `astra-followup-context.md`, 진입/이관 `branch-checkpoint.json`, 독립 `installation-review-1/verdict.md`, 부모 원시 대조 `review-source-audit.json`, 정산 `review-settlement.md`. 후속 기록은 초기 고정 입력을 덮어쓰지 않는다.
- 최신 수정/판정은 `installation-fix-1/report.md`와 `installation-recheck-2/verdict.md`, 부모 원천 대조는 `fix-source-audit.json`/`recheck-source-audit.json`이다. 중간 마감 맥락은 `astra-closeout-context.md`, 최신 Git 정산은 `mid-closeout-final-checkpoint.json`이다.
- 이번 재개 사전 메모는 `astra-resume-scope-context.md`다. 새 계약/고정 입력은 중간 마감 당시 미발행 계약과 구분해 보존한다.
- 이전 승인 초안/승인 전달과 종료 checkpoint: `.backups/verification/2026-10-03-persistence-repository/{successor-goal-draft.md,successor-goal-review-delivery.json,closeout-pr171-checkpoint.json}`. 원문은 덮어쓰지 않는다.
