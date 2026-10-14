# 리드 Sol·작업자 xhigh 모델 라우팅 핫픽스

## 재개 지점

- 메인 `msg_440213ef0ff7`의 사용자 지시 전달과 범위 v1 승인 `msg_7b06b4c0cae3`에 따른 별도 문서 goal이다. 승인 출처는 사용자 밤 판단 위임에 따른 메인 판단이며 새 사용자 결정이 아니다.
- 작업 공간 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-sol-xhigh-hotfix`, branch `docs/sol-xhigh-routing-hotfix`, 최신 main/base `20ce59ddd72d0cb72b6e230e386a62587b9bfe1b`.
- 리드 `[Rules 리드 Sol]`, 현재 handle `term_9763f046-8be8-4d0b-aaa8-5214fb800be3`, 회신 `run:run_54319375ed08`(consumer_generation3). worker는 별도 Task/Dispatch로 추적한다. Main은 `term_e2961048-266e-4503-b52b-dfb986d8c9b5`이며 사용할 때 현재 동일성을 확인한다.
- 근거 E는 `.backups/verification/2026-10-14-sol-xhigh-routing-hotfix/`(Git 제외). 리드 사전 메모 `E/lead-context.md`, 원문과 승인·기계 검색 목록·위임 계약·기동·판정·Git 영수증을 여기에 둔다.
- 현재 차례: **[PR238 - 리드 Sol·작업자 xhigh 모델 라우팅 정합](https://github.com/bass131/dawnholder-server/pull/238)을 생성했고 아침 사용자 승인·메인 병합을 기다린다.** 신규 Astra xhigh의 독립 문서 실사 PASS와 worker_done을 정산했으며 두 외부 세션을 닫았다. 이번 밤의 PR/head 보고·종료 점검/기록 정리를 마치고 다음 goal 초안/작업자 없이 멈춘다. PR2 가드는 보류한다.
- PR2 가드는 `rules-active`의 `feat/codex-merge-guard-20261013`에서 동결까지 보류한다. 이전 기록 head `a2703d079491859d0767c6effc768d42a3ea5f46`과 제품/시험 등16변경 경로는 보존했다. 구조 대응·실제 Codex 진입·Linux CI 미완을 이 goal의 문서 판정으로 덮지 않는다.
- 다음 행동: 아침 사용자가 PR238의 당시 정확40자 head로 병합 승인하고 메인이 병합 관문을 통과시켜야 한다. 최종 head는 PR 메타데이터와 E/final-handoff.json에 기록해 자기참조 commit을 만들지 않는다. 다음 Rules 목표와 PR2/Art 재개 여부는 아침 사용자가 정하며 자동 착수하지 않는다.

## 진척 단계

- [x] 범위 승인
- [x] 문서 수정
- [x] 독립 문서 실사
- [x] PR 생성
- [ ] PR238 병합
- [x] 종료 기록

## 범위

### 만들 것

1. 기본 다섯과 승인된 추가 파트 리드는 `gpt-6.1-sol` xhigh 및 `[<파트> 리드 Sol]`로 정합한다. CodeMap의 Architecture 경로/이름은 유지한다.
2. Sol 배정 작업자는 `gpt-6.1-sol` xhigh로 정합한다. Astra 배정 작업자는 `gpt-6-astra` xhigh, 독립 검증자 모델 시범·R-7·확정 실패 뒤 Advisor는 유지한다.
3. 같은 파트의 전환기 리드 Astra/Sol 태그 수용과 현재 from_handle·Task·Dispatch 대조를 유지한다. 역사 당시 태그·모델·원문은 보존한다. 문서 변경과 런타임 전환을 구분한다.
4. 이전 한 리드·목표 하나 Sol max 시범 후보는 이번 사용자 결정으로 대체됐음을 처분한다. 시범 시행 완료로 보고하지 않는다.

### 건드릴 곳

| 경로 | 쓰는 사람 | 경계 |
|---|---|---|
| AGENTS.md | 외부 Sol xhigh | 현행 모델/effort/태그 및 전환 설명만 |
| 00_Document/operations/ORCA.md | 외부 Sol xhigh | R-1/R-5/R-8/capacity의 해당 모델/effort/태그, 현 기본 배정 참조. 예외/권한은 보존 |
| .agents/skills/dawnholder-goal-loop/SKILL.md | 외부 Sol xhigh | 현 Sol effort/리드 지칭 정합. 독립 검증 시범과 역사 근거 유지 |
| .agents/skills/dawnholder-goal-loop/references/implementer-routing.md | 외부 Sol xhigh | Sol xhigh. Astra 선택 신호/배정 유지 |
| 05_Management/RESUME.md | 외부 Sol xhigh | 현 Management 리드 태그 |
| CLAUDE.md | 메인만 | 27행 Architecture 리드 태그. 다른 모델 명령은 현재 AGENTS 참조를 유지 |
| 이 goal·CURRENT의 Rules 줄·BACKLOG 후보 | 리드 | 범위/결정/실제 결과·새 링크 및 이전 시범 폐기 처분 |
| E/doc-write/ | 외부 Sol xhigh | 사전 메모·보고·기존 도구 원시만 |
| E/doc-review/ | 신규 Astra xhigh | 사전 메모·독립 문서 판정·원시만 |

초기 latest main의 기계 검색에서 RESUME·helper-session·session-handoff에는 활성 모델 명령이 없었다. orca-work의 Astra는 capacity 예외였다. 추가 쓰기가 실제로 필요하면 이 승인 범위의 현재 라우팅 문장인지 리드에게 경로/이유를 보고하고 소유를 먼저 확정한다. 역사 goal/보고와 테스트 fixture는 수정하지 않는다.

### 하지 않을 것

- PR2 제품/시험·기존16변경, 역사 goal·보관 기록·고정 보고·테스트 fixture를 바꾸지 않는다.
- Astra 구현자·독립 검증자·선행 시험·R-7/Advisor 배정·capacity 조건·쓰기/Git/병합 권한·파트 생성·판을 바꾸지 않는다.
- 전역 설정·업데이트·무거운 실행·새 코드/시험/하네스·시간 계산 준비 장치를 만들지 않는다.
- 이 문서 goal을 근거로 시연 밖 후보나 Art 문서 PR을 자동 착수하지 않는다. 사용자 정확 head 승인 전 병합/자동 병합하지 않는다.

### 관찰 가능한 완료조건

1. 활성 정본의 리드/Sol 작업자 모델·effort·태그와 R-1/R-5/R-8 명령이 일치한다. 남은 Astra/max 표본은 구현/검증/역사 등 유지 이유가 있다.
2. 구현자와 분리된 신규 Astra xhigh가 실제 최종 diff·원문·참조·권한 및 리드 기동/작업자 기동/동일 대화 재개/전환기 수신의 문서 시나리오를 독립 실사한다. 메인 CLAUDE 쓰기 종료/실제 diff를 포함한다.
3. 새 하네스 없이 기존 후보 검사·diff check와 기계 검색 원시를 보존한다. 미실행 제품 검증·런타임 모델을 문서 판정으로 성공 주장하지 않는다.
4. lead-sol-model-pilot-canon의 최초7필드/원문 출처를 보존해 이번 결정으로 폐기한다. PR2 보류·다른 파트 진행은 보존한다.
5. 문서 PR 하나를 최신 main 기반으로 만들고 변경 요약·판정 원문·미실행·정확40자 head를 메인에 보낸다. 사용자 승인 뒤 메인만 병합한다.

## PR 경계와 검증

- 1 - 모델 라우팅 문서 핫픽스: 이 goal의 허용 문서/기록 하나의 PR. TDD 비해당, 검증은 독립 문서 실사1회다. Sol xhigh 작성·쓰기 종료 후 신규 Astra xhigh 검증이며 R-7 비해당이다.
- 시연 우선 모드에 따라 비동작 지적은 번호/근거와 함께 동결 뒤 정리에 남긴다. 제품 계산 복제·측정 위장·금지 구간 실행·메모/결과 불일치·결론/본문 모순은 메인에게 올린다. 독립 실사에서 다시 막히면 새 검증을 열지 않고 메인 판단을 받는다.
- 문서 모델 라우팅의 교정 층은 문서다. 사용자 운영 선택을 코드·설정으로 강제하거나 전역 기본값을 바꾸는 것은 승인 범위 밖이므로 더 높은 기술 층을 구현하지 않는다.

## 적용 중인 사용자 결정

메인 전달 원문이며 Rules pane의 직접 사용자 입력으로 격상하지 않는다.

1. `msg_440213ef0ff7`(2026-10-14T14:21:18Z), 메인 pane23:2x KST: 「음 그리고 리드급 아무래도 Sol Max로 전체적으로 확장해야 될 거 같아, Astra 토큰 사용량이 감당이 안되네」. 이어 「음 Sol 그리고 정정사항은 작업자랑 리드 전부 Xhigh로 변경, 핫픽스 적용 지시해줘」. 최종 xhigh 정정이 앞 Max를 대체한다. 이전 `msg_928a88962458`의 한 리드 Sol max 시범을 대체한다.
2. 시연 우선 모드 `msg_1074f0ee17f7`(14:11:27Z)의 사용자 원문: 「대시보드 결정 응답: 1) 10-28 시연 - 동결까지 시연 경로 우선 모드로 검증 절차를 줄일지, 지금 절차를 유지할지 → A 시연 우선 모드」. 10-28 기능 동결까지 적용, 동결 뒤 원래 절차로 복귀한다. 이번 명시 사용자 요청 핫픽스만 시연 밖 작업 보류의 예외이며 PR2는 보류한다. 전달 전체 원문은 E/user-demo-priority-original.json에 보존한다.
3. 메인 승인 `msg_7b06b4c0cae3`(14:26:41Z)에 담긴 밤 판단 위임 원문: 「대시보드 결정 응답: 1) 밤샘 진행 - 자는 동안 생기는 판단을 메인 추천대로 정하게 할지, 그 작업만 아침까지 멈출지 → A 메인 추천대로 진행」. 메인은 이 위임으로 범위 v1을 승인했으며 새 사용자 결정으로 격상하지 않는다.
4. 정리·멈춤 지시 전달 `msg_cee73c25f37d`(14:46:39Z)의 사용자 원문: 「그리고 영속성 부분은 너가 더 전문가니까 Goal 초안 작성하고 진행까지 계속 진행해줘, Core쪽 진도를 좀 빠르게 뺴보자 / 나머지 세션은 Taste에 민감한 부분이니까 그 파트들은 목표 끝나면 마무리하고 정리하는 식으로하자」. 사용자 실제 제출 시각은 **2026-10-14T14:44:08Z(23:44 KST)**이며 메인 정정 `msg_faa0cc22666f`(14:53:00Z)를 따른다. 첫 전달의 「2026-10-15 00:0x KST 무렵」 표기는 오류다. 전달 내용은 그대로며 Core 권한을 Rules에 확대하지 않는다. Rules는 이 핫픽스의 PR번호/정확 head 보고·병합 대기·종료 점검/기록 정리 뒤 멈추고 다음 goal 초안/작업자를 열지 않는다. 다음 일은 아침 사용자가 직접 정하며 PR2는 보류한다. 원 전달·정정 전체는 E/user-close-current-goal-original.json 및 user-close-current-goal-time-correction.json에 별도로 보존했다. 작업자 고정 입력을 바꾸지 않기 위해 쓰기 종료 뒤 이 절에 기록했다.

## 메인 적용 판단과 승인

- `msg_440213ef0ff7`의 해석: 리드 기본/승인 추가 모두 Sol xhigh, Sol 배정 작업자 xhigh, Astra 배정 xhigh와 독립 검증 시범/R-7/Advisor는 유지. 전환기 Astra/Sol 리드 태그 수용·출처 대조 유지. 런타임 전환은 문서 병합을 기다리지 않는다.
- `msg_7b06b4c0cae3`는 공식 질문 `msg_1445da7fe23e`의 범위 v1을 그대로 승인했다. CLAUDE 변경은 실제 목록의 줄/문장을 Main에게 보내고 쓰기 종료를 받는다. 신규 Astra xhigh 문서 실사1회, 그 뒤 다시 막히면 Main에 올린다. 정확40자 head 보고·아침 사용자 승인 뒤 Main 병합이다.

## 현재 결과

- 같은 Run을 consumer_generation3으로 인수했고 화면 GPT-6.1-Sol xhigh·Codex0.162.1·문맥23%를 관측해 READY `msg_15c81b58ef02`로 보냈다. backend 실제 모델 unknown, 경고1 원문 미확인이다. bounded stream에 원문이 없었으며 모델 전환 원인으로 단정하지 않는다.
- latest main 기계 검색과 기존 문서 줄 대조를 끝냈다. 새 checkout은 독립 최신 main 기반이며 repo setup을 skip했다. PR2 제품 쓰기·독립 재검증은 실행하지 않았다.
- 메인 CLAUDE 쓰기 종료 `msg_00be2b4eb42f`(14:30:01Z): 실제1파일1줄에서 `[Architecture 리드 Astra]`를 `[Architecture 리드 Sol]`로 바꿨다. 수정 뒤 SHA256 `c93f968b45b87d58421da8db247109082d90566bb3b4e0a141d4f559c8060585` 일치, 나머지 줄 보존. 리드는 파일 쓰기를 대신하지 않고 branch 소유권에 따라 commit만 한다.
- 문서 Sol Task `task_76455f9e1cac` / Dispatch `ctx_10e808987732`: 14:53:47Z 쓰기 종료 `msg_115c30e89fa2`, 14:53:57Z worker_done `msg_79882ccf606b`의 현재 from_handle/ID/명시 succeeded/허용5파일을 대조했다. report SHA256 `51c66cd2546b4ad50ef237cabaf0dfd4a6ebc497f4c0f0f16c00212bc35827a8`, context SHA256 `0b028db923d4f1a8ac20463479a69883ae530a7ca1c8df38f31fdd9cde775458` 전체와 실제 diff를 읽었다. E/doc-write-lead-audit.json에서 입력23의 허용5변경/허용 밖0·Git dirty5/신규0·근거60파일 hash를 확인했다. 작성 보고는 독립 PASS가 아니다.
- 운영 미준수는 기존 helper의300초 기준에서 heartbeat 사이309초·328초 두 구간과 초기 읽기/메일 UTC·별도 원시 일부 미기록이다. 원문/기계 결과는 E/doc-write/report.md 및 doc-write-liveness.json에 공개했고 시연 우선의 운영 공개 처분을 따른다. 내용/권한 모순까지 면제하지 않는다.
- release retained/external_terminal·processAction none 뒤 동일 pane의 완료 idle·draft 없음·현재 identity를 확인해 정확 close ptyKilled=true·reclaimable0으로 정산했다. 원문은 E/doc-write-lead-settlement.md와 각 영수증에 보존하며 세션을 재사용하지 않는다. 사용자 정리·멈춤 및 시각 정정은 고정 입력 해제 뒤 위 결정4로 반영했다.
- 신규 Astra 독립 실사 Task `task_cb261897559f` / Dispatch `ctx_bac385c2d32e`는 verifies `task_76455f9e1cac`를 실제 연결했고 검증 HEAD `bcebf24aadb2e5dbd5ed95a3ef8851935d79a45f`의 최종9파일을 문서 PASS로 판정했다. 판정 원문 E/doc-review/verdict.md SHA256 `4ff8541de9b6093e108b562610c0524a40b321cc47127f937811e208f0fb8886`, context SHA256 `7231b13ed31fbb09715e665f48ed4c70db135a8c6e244259b36e3acee5cc1445` 전체를 리드가 읽었다. 고정91입력/HEAD 불변·Git clean·근거51파일 hash를 대조했다. Sol5파일 완료 raw/실제 커밋 diff163행 동일·독립 검색314행/65파일·후보 대상0 경계를 판정과 연결했다.
- 쓰기 종료 `msg_94b6fea3091c`(15:18:54Z)와 worker_done `msg_d227ea72780d`(15:19:02Z)의 현재 from_handle/두 ID/명시 succeeded를 대조해 정산했다. release retained/external_terminal 뒤 정확 identity의 완료 idle·draft 없음·새 일 없음으로 close ptyKilled=true·reclaimable0을 확인했다. 독립 실사는1회이며 검증자를 재사용하거나 새 재검증/다음 goal을 열지 않았다.
- D1은 리드 계약의 path-check 접두 생략 오기였다. 공식 질문 `msg_a90abb930de3`와 답 `msg_9b99b53114e6`가 실제 경로/hash를 확정해 해소했으며 작성 Sol 결함으로 귀속하지 않는다. 첫 발생을 여기 기록하고 새 검사/반복 규칙을 만들지 않았다. 검증자 초기 원시 일부 미기록은 판정 운영 절에 공개했다. 기존 helper의 검증자 신호7은300초 기준 최대277초·초과0이다. 공식 질문/답 원문 시각은15:05:35Z→15:07:34Z이며 중간 heartbeat로 helper의 questionWaits가 비어 있는 것을 실제 질문 대기 없음으로 해석하지 않는다.
- PR238 생성 당시 head는 독립 검증 HEAD와 같았다. 이후 이 goal의 결과/PR번호/중단 상태 기록만 리드가 통합하며 나머지8변경 문서의 hash는 독립 검증 입력과 대조해 보존한다. 판정의 입력을 최종 기록 head로 소급하지 않는다. 현재 제품·실제 런타임 전환·빌드·Unity·DB·WSL/CI 실행 판정은 없고 Git 병합도 하지 않았다.
- Content 자원 조율은 중량 실행/예약/예정 창0으로 회신했다. 처음 `--run Rules`와 상대 Run 수신을 함께 쓴 요청은 run_not_found였고 request-show absent·상대 inbox의 일치 회신 부재를 확인한 뒤 현재 handle에 한 번 답해 실제 Content Run으로 정상 접수됐다. 도구 인자/수신 경로 정정이며 새 운영 규칙·후속 후보를 만들지 않았다. Content가 Unity1회 허가/예약1·시작 미관측을 보고한 원문은 E에 보존했고 Rules 실행 허가로 확대하지 않는다.

## 이번 밤 종료 점검

- 문서 작성·독립 문서 실사·PR 생성과 판정/원시 보존을 마쳤다. 작성/검증 세션은 모두 정산·정확 close, reclaimable0이다. Rules 중량 실행/예약0, 제품/시험 쓰기0이며 PR2 보류16변경과 기존 원시는 별도 checkout에 보존한다.
- 아침 사용자 승인이 남았으므로 전체 goal의 병합 완료/완전 종료로 표시하지 않는다. 현재 밤의 작업은 PR/head 보고·병합 대기·정리 지점에서 멈춘다. 전체 병합 뒤 Gardener/완전 종료/R-8의 필요 순서는 메인이 당시 사용자 결정과 대조하며 지금 추가 세션을 만들지 않는다.
- 메인 최종 status는 PR238·정확40자 head·판정 원문/R-2 표본·동결 뒤 정리·아침 결정 항목을 포함한다. 최종 head와 원격 CI 상태는 실제 GitHub 관측을 E/final-handoff.json에 남긴다. 다음 goal 초안/작업자 기동은 하지 않는다.

## 동결 뒤 정리

- 현재 새 미해결 문서 결함 없음. D1은 공식 답으로 해소됐다. O1 고정 anchor와 O2 당시 비용/조건부 후보는 유지 관찰이며 새 작업으로 채택하지 않았다. 작성/검증의 운영 공개는 원 판정·이 goal에 보존한다. 기존 PR2/Art/시연 우선 정본 후보는 해당 원천의 처분을 유지한다.

## 다음 계획 후보

현재 새 후보 없음.
