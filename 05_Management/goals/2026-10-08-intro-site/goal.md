# 게임 소개 페이지(고객용)

## 진척 단계

- [x] 범위 승인과 goal 고정
- [x] 첫 판 제작과 검증
- [x] 디자인 범위 개정 승인
- [x] 무드보드와 선행 시험
- [x] 방향 시안 고르기
- [>] E2E 시험과 시안 버그
- [ ] D1 완성과 E2E 통과
- [ ] 사용자 중간 확인
- [ ] 비평과 독립 검증
- [ ] 사용자 최종 확인
- [ ] PR207 병합·배포
- [ ] Gardener·종료 PR

PR 번호가 생기면 「제품 PR 병합」 같은 단계 이름을 「PR000 병합」 형식으로 바꾼다. 「첫 판 제작과 검증」은 상한 12개를 지키려고 첫 판의 완료 단계 다섯(공개 안전 선행 시험, 웹용 그림 사본 만들기, 페이지 본문 작성, 배포 workflow 구현, 독립 검증)을 접은 것이다(메인 수용 `msg_80ddaefce11a`). 그 기록은 「결과와 열린 사항」에 그대로 있다. 시안 선택 뒤 E2E·사용자 중간 확인이 더해져(「시안 선택과 E2E·밀도 요구」), 「PR207 병합」과 「배포 확인」, 「Gardener 점검」과 「종료 기록 PR 병합」을 하나씩 접었다(메인 수용 `msg_d0b739ca391a`). 「무드보드와 선행 시험」은 시안 선택 뒤 T2가 끝나 정산된 2026-10-08 16:06 KST에 닫았다.

## 재개 지점

**기록 시점: 2026-10-08 17:36 KST, Orca 재시작 준비(메인 `msg_49163ec7a137`, 사용자 요청: 터미널 렉). T3·F 작업 중.** 사용자는 첫 판 PR207을 보류하고 디자인을 다시 하기로 했고(아래 「디자인 재작업 범위 개정」), 시안 D1을 골랐다(「시안 선택과 E2E·밀도 요구」). 실제 진행은 「진척 단계」, 「결과와 열린 사항」, 그리고 리드가 단계마다 다시 쓰는 이 문단을 따른다.

- **지금 단계:** 선행 시험 T2를 정산하고 닫았다(「선행 시험 결과(T2)」). 리드가 D1용 사이트 그림·글꼴 사본과 README 표 넷을 사이트 폴더에 넣었다(「사이트 그림·글꼴 사본과 README 표(리드)」). E2E 시험 작성자 T3과 완성 세션 F를 16:17 KST에 병렬로 열었다(「E2E 시험 T3과 완성 세션 F 발행」). 다음은 T3의 D1 버그 목록 status와 F의 멈춤 지점 1 status다. 임시 서버 127.0.0.1:8750은 T3의 시안 E2E가 끝나면 닫는다. PR207(branch `feat/intro-site-20261008`, base `7086d45b`)은 열린 채 보류이고, 디자인 완성 뒤 새 head로 병합 승인을 요청한다. GitHub Pages는 꺼진 채다.
- **재시작 준비 상태(17:36 KST):** 새 작업자 기동은 보류 중이다. 작업자에게 멈춤 지시는 보내지 않았다.
  - T3(E2E 시험, 신규 `claude-opus-5-5`): pane `term_312e1a08…`, Task `task_30e21975778a`, Dispatch `ctx_5c65fd855ef2`, 계약 E/`contracts/t3-task.md`. 마지막 heartbeat 08:28Z `implementing`. 쓴 것: E/`harness-v2/`의 `check-e2e.mjs`·`e2e/`, E/`t3/`(`test-writer-context.md`, `inputs`·`raw`·`runs`·`chrome`·`superseded`). D1 버그 목록·보고는 아직 없다.
  - F(완성 세션, 신규 `claude-opus-5-5`, 태그 `[Management Sol]`): pane `term_40086209…`, Task `task_aa27bccd465e`, Dispatch `ctx_cd721f44ed4f`, 계약 E/`contracts/f-task.md`, 접점 보충 1. 마지막 heartbeat 08:17Z `self-check`(멈춤 지점 1 직전). 쓴 것: 사이트 `index.html`·`styles.css`(수정, 미커밋)와 새 `script.js`(미추적), E/`f/`(`designer-context.md`, `design-plan.md`, `report.md`, `shots`·`frames`·`raw`·`tools`). 멈춤 지점 1 status는 아직 없다.
  - 리드 미리보기 서버 127.0.0.1:8750은 리드 Claude 세션의 background 작업이라 재시작으로 꺼진다. T3이 시안 E2E에 쓰면 리드가 다시 띄운다.
  - 리드 Claude 세션 ID `090e4c0e-03b6-4f71-bf11-8c419407e534`, Run `run_605925c36641`. 재시작 뒤 첫 할 일: run-use로 Run을 인수하고 T3·F pane이 살아 있는지 `terminal list`로 본 뒤, 죽었으면 각 산출물을 보존한 채 메인에 알리고 크래시 정본대로 새 세션 대체를 준비한다.
- **작업 경로:** `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`.
- **Run과 재진입:** Run은 `run_605925c36641`이다. 이번 리드 handle은 `term_436c20d1-eacc-4335-ab85-027f65e69f4f`이고, 이전 리드 handle `term_59adfc07-455c-493b-b4bc-1e2583b08ff9`와 함께 관측값일 뿐 다음 리드의 실행 권한이 아니다. 다시 열면 새 handle로 run-use하고 메인에 알린 뒤 우편함 대기를 하나만 연다.
- **작업자·검증자:** T1·S1·V1·V2, 시안 D1·D2·D3, 선행 시험 T2는 정산하고 닫았다. 지금 열린 작업자는 T3과 F다(pane·Task·Dispatch는 「E2E 시험 T3과 완성 세션 F 발행」).
- **근거 폴더 E:** 저장소 로컬 `.backups/verification/2026-10-08-intro-site/`(Git 제외). 범위 단계 근거는 `.backups/verification/2026-10-08-intro-page-scope/`(E0)다. 첫 판 리드 맥락 메모는 E/`astra-context.md`, 디자인 범위 개정 근거와 메모는 E/`design-scope/`다.

## 요청 원천과 승인

- 사용자 요청(메인 전달): 「메인 보조세션 추가로 할당해서 Dawnholder Repo에 Github Pages로 소개용 페이지 만들어보고 싶은데 가능할까?」, 「… → B goal 범위부터」.
- v1 코멘트(메인 전달 `msg_04011b2fc5d8`): 「말 그대로 게임의 소개에 대한 사이트여서 고객용 사이트 느낌을 제작할거라 기술설명은 안해도 될 거 같음, 계획 초안에 이 부분 다시 반영해서 선택지랑 세부내용 계획 다시 짜줄래?」.
- 리드가 범위 초안 v2를 보냈다(`msg_d84653aed763`, E0/`scope-draft-v2.md`, SHA256 `34c0e57fb0c0e61eb5db47191b756ec3d75daefc6eec05ed6c64ade7cd30823e`). 이 goal의 범위는 그 초안이 정본이다.
- 승인(메인 `msg_8a1a882241d3`, 2026-10-07T19:27:24Z, E/`entry-inbox.json`이 전달한 사용자 원문, 메인 창 Enter 제출): 「대시보드 결정 응답: … 2) 계획 검토 - Management 소개 페이지 범위 초안 v2 - 고객용 재구성, 질문 5개(추천 전부 A) → A 승인 (초안 msg_d84653aed763)」. 계획 검토의 A는 「메인이 이 답을 그대로 파트에 전달하고, 파트는 이 초안대로 goal을 시작한다」는 뜻이다.

모두 메인 전달이며 사용자 직접 입력으로 격상하지 않는다.

## 적용 중인 사용자 결정

초안 0절 질문 다섯은 모두 A다.

| 질문 | 답 | 결과 |
|---|---|---|
| Q1 페이지 성격 | A 게임 소개만 | 맨 아래에 캡스톤 프로젝트 크레딧(팀장 이름)과 소스 코드 링크 한 줄만 둔다 |
| Q2 이야기 문장 | A 게임 안 문장만 | 게임 화면에 보이는 대사·퀘스트 문구로 두세 줄을 쓴다. 지어낸 설정은 없다 |
| Q3 아직 없는 콘텐츠 | A 「앞으로」 절과 배지 | 「개발 중」·「계획」 배지로 따로 모은다. 출시일은 쓰지 않는다 |
| Q4 그림 | A 저장소 그림만 | 저장소 그림의 웹용 사본만 쓴다. 플레이 캡처는 없다 |
| Q5 배포 | A 사이트 폴더 + 배포 workflow | main에 병합된 변경만 공개된다 |

질문에서 뺀 기본값도 그대로다. 언어는 한국어만, 페이지는 한 장이다.

**추가 그림 생성 허용(goal 고정 뒤):** 메인 `msg_f48d2798a0f6`(2026-10-07T19:35:25Z)이 전달한 사용자 원문(메인 창 Enter 제출)은 「만약에 Management한테 추가 아트워크가 필요하면 우리 게임 리소스 참고해서 GPT Sol한테 만들어달라고 그래」다. 메인 해석은 다음과 같다. Q4 A에 「필요하면 Sol 생성 그림 추가」가 더해진다. 작업자는 신규 `gpt-6.1-sol`(max)이다. 참고 원천은 프로젝트 자체 그림만 쓰고 유료 Asset Store 패키지는 참고 입력으로도 쓰지 않는다. 생성 그림에는 글자를 넣지 않고 실존 게임·캐릭터를 흉내 내지 않는다. 생성 그림은 사이트 폴더의 새 파일로만 두고 출처 표에 생성 모델·참고 원본·생성 날짜·SHA256을 적는다. 리드는 Sol 보고를 믿지 말고 실제 파일 위치와 SHA256을 직접 확인한다. **리드 판단(2026-10-08 04:4x KST): 지금은 만들지 않는다.** 저장소 그림 사본 6장으로 첫 화면·직업·몬스터 절이 모두 채워진다. 그림이 없는 절은 이야기·플레이·앞으로·소식인데, 「앞으로」의 길드 거점은 아직 게임에 없어서 그림을 만들면 있는 것처럼 보일 위험이 있다.

### 디자인 재작업 범위 개정(2026-10-08)

모두 메인 전달이며 사용자 직접 입력으로 격상하지 않는다. 원문 사본은 E/`design-scope/`에 있다.

- **교정과 보류(진입 메시지 `msg_331e84a02264`):** 메인이 PR head와 같은 로컬 index.html을 열어 보인 뒤 사용자 원문은 「음 홈페이지 이거 Sol이 만들었지?, 퀄리티가 너무 낮아, 앞으로 디자인은 Opus 5.5한테 시켜야겠는데」다. 결정 응답은 「1) PR207 - 게임 소개 페이지 디자인 다시 할지 → B 보류, 디자인 다시 (head 7cd4232247176376896708554fa24300654af85d)」이고, B는 「공개를 미루고 같은 goal에서 디자인을 다시 한다. 네 디자인 확인을 받은 뒤 새 head로 승인 요청이 온다.」였다. 거슬린 점은 배치·여백, 색·분위기, 글꼴·글자 크기, 그림 활용 넷과 「프론티어급 동적디자인이 세련되게 들어간 느낌을 원했는데, 딱 기본 CSS 느낌이라 더 그런거같은데」다.
- **v1 수정 요청(`msg_449e095ece04`):** 「… → B 수정 요청 (초안 msg_21b65cf410f3), 코멘트 「음 프론트엔드로 최고점으로 결과물 찍을 방법이 더 있는지 찾아보고 더 고려해봐줄래?」」.
- **v2 승인(`msg_f5cb634b7576`, 2026-10-08T03:10:42Z):** 「대시보드 결정 응답: 1) 계획 검토 - Management 소개 페이지 디자인 범위 v2 (Q1~Q3, Q3은 사용량 판단) → A 승인 (초안 msg_231d995453d7)」. 메인 해석은 Q1·Q2·Q3 모두 A다. 범위 정본은 v2 초안 E/`design-scope/scope-design-draft-v2.md`(SHA256 `b2da998017bd4fceee8d5a9105fb52198850a199c17d9037532ad61c6b340a81`)다.

| 질문 | 답 | 결과 |
|---|---|---|
| Q1 연출 코드 | A 직접 쓴 HTML·CSS·JavaScript | 외부 라이브러리·WebGL·빌드 도구 없이 겹 배경·등장 연출·캐릭터 동작·빛 입자·움직임 정지 버튼을 만든다. JavaScript가 꺼져도 모든 내용이 보인다 |
| Q2 외부 파일 받기 | A 받는다 | 사이트에는 Pretendard 공식 웹 글꼴 부분 집합 보통·굵게와 OFL 전문을 싣는다. 근거 폴더에만 `cwebp`, Lighthouse, Playwright와 Firefox·WebKit 브라우저를 둔다. 저장소·전역 설치는 없다 |
| Q3 시안과 완성 작성 | A 디자인 전용 세션 | 신규 `claude-opus-5-5` 시안 세션 셋이 서로 다른 방향 시안을 따로 만들고, 사용자가 고른 시안을 신규 `claude-opus-5-5` 완성 세션이 완성한다 |

**Q3 라우팅 예외의 범위(메인 적용 조건 1).** AGENTS 「보고서 자료의 … HTML … 은 Astra가 작성한다」의 이 goal 한정 예외다. 시안 세션 셋은 E/`drafts/`만, 완성 세션은 사이트 `index.html`·`styles.css`·스크립트 파일만 쓴다. 다른 goal로 넓히지 않는다.

### 시안 선택과 E2E·밀도 요구(2026-10-08)

모두 메인 전달이며 사용자 직접 입력으로 격상하지 않는다. 사용자는 메인이 Chrome 탭으로 띄운 임시 서버의 세 시안을 직접 봤다.

- **시안 선택과 요구(`msg_4b9e90366381`, 2026-10-08T06:29:07Z, 메인 창 Enter 제출):** 「음 페이지 만든거 3개다 버그가 좀 심하네, 각가 E2E 테스트를 해야겠는걸, 그리고 각각 페이지들 너무 허전해
그래도 느낌상 제일 좋은건 D1이 좋은거같네」
- **사용자가 본 버그(`msg_d0b739ca391a`, 06:31:22Z, 메인 창 Enter 제출):** 「버그 같은 경우는 애니메이션 실행시 제대로 출력이 안된다거나, 이펙트랑 캐릭터 크기가 적절하게 안맞는다거나 하는 문제가 있어」
- **메인 해석(사용자 결정 아님):** (1) 완성은 D1 「노을 풍경 시네마틱」 방향이다. (2) 실제 브라우저에서 사람처럼 조작하는 E2E(end-to-end) 시험을 둔다. 먼저 지금의 세 시안에 돌려 버그 목록을 만든다. D2·D3 목록은 기록용이다. 완성본은 E2E가 모두 통과한 뒤에만 사용자에게 보인다. (3) 「너무 허전해」는 절 안의 화면 밀도 지적이다. 채울 재료는 근거표 안의 사실과 저장소 그림뿐이다. (4) 사용자가 본 버그 둘은 E2E와 완성 계약의 명시 항목이다. 하나는 등장 연출·동작·스킬 효과의 재생이다. 칸 빠짐·겹침·엉뚱한 칸·깜빡임·빈 프레임·중간 끊김·대기 미복귀가 0이어야 하고, 칸 영역은 원본 `.meta`와 대조한다. 다른 하나는 크기 비율이다. 같은 장면의 캐릭터·효과·몬스터가 같은 픽셀 배율로 그려지고, 효과의 크기·붙는 기준점은 게임 원천(prefab·pivot·pixels per unit)에서 가져와 근거를 계약에 적는다.
- **로고·캐릭터 최신본 요청(`msg_b6e9bfa259ff`, 06:35:48Z, 메인 창 Enter 제출):** 「그리고 추가적으로 캐릭터나 로고가 예전꺼를 쓰는거 같은데, 수정해달라고 해줘, 최신본 있을거야」. 메인 지시는 씬·prefab·애니메이터·코드 참조와 커밋 이력으로 「지금 게임이 실제로 보여 주는 판」을 정하고, 비교 그림을 근거 폴더에 두고, 근거가 한 판으로 모이면 바꾸고 갈리면 메인에 올리는 것이다.
- **리드 조사 결과(근거 E/`design/latest-art-research.md`, 비교 그림 E/`design/compare-latest.png`):** 로고는 시안의 `Main_Title_Banner.png`가 06-15 이전 판이고 지금 메뉴 장면은 `Dawnholder_MainTitle_Banner_keyed.png`(16칸, 12fps 반복)를 쓴다. 이 판으로 바꾸고, 게임처럼 반복 재생하며 정지 수단·움직임 줄이기에서는 칸 0에 멈춘다(E2E 동작 재생 대상). 캐릭터는 지금 애니메이터가 쓰는 시트가 시안과 같은 06-05 판이고, 저장소에 더 새 캐릭터 그림이 없다. 다르게 생긴 그림은 HUD 얼굴 초상 둘뿐이라 메인에 확인을 요청했다. 효과는 지금 판(대시 06-11 재출력·속도 1.8, 낙뢰, 순간이동)을 쓴다.
- **캐릭터 확인 답(`msg_978398a3a7a3`, 06:52:18Z, 현황판 결정 `mgmt-character-art` 응답, 메인 창 Enter 제출):** 「대시보드 결정 응답: 2) 소개 페이지 - 「최신본」 캐릭터가 HUD 얼굴 초상이었는지 → C 코멘트 「아 캐릭터는 괜찮아 혼란줘서 미안, 로고가 최신본이 아니였고, 캐릭터는 이펙트랑 애니메이션이 잘 맞지않는 버그가 있네」」. 메인 해석: 캐릭터 그림은 지금 게임 시트 그대로, 로고만 교체한다. 「캐릭터 동작과 효과의 맞물림」을 명시 판정으로 둔다. 시간(효과가 시작하는 칸과 캐릭터 동작 칸이 게임과 같은 순간), 위치(붙는 기준점·바라보는 방향), 크기(같은 픽셀 배율)다. 재생 중 프레임을 시간별로 잡아 캐릭터 칸 번호와 효과 칸 번호의 짝을 게임 원천에서 만든 기대 표와 대조하고, 기대값은 원천 파일의 숫자로 고정해 출처를 적는다.
- **Chrome 직접 확인 지시(`msg_10a574bff890`, 06:54:56Z, 메인 창 Enter 제출):** 「만약에 버그 특정하기 힘들면, 크롬으로 직접 사이트 켜져있는걸 기반으로 한번 확인해달라고 해줘」. 메인 해석: 시험 코드나 정지 사진만으로 짚기 어려우면 실제 Chrome에서 임시 서버로 띄운 사이트를 보면서 재현한다(예: Playwright 설치 Chrome 채널을 화면에 띄움, 시간별 프레임, 화면 녹화, 사용자 평소 프로필 미사용). 재현되면 장면(주소·화면 폭·조작·칸)을 버그 목록에 적고 E2E 판정으로 고정한다. 재현이 안 되면 본 것과 함께 메인에 알린다.
- **메인 판단(`msg_d0b739ca391a`, 리드 계획 `msg_c240580b33ee` 확인):** E2E 시험 작성자 T3을 하나 더 연다(세션 상한 +1, 합계 8). 완성 세션은 T3와 병렬로 열고, 멈춤 지점에서 T3의 D1 버그 목록과 E2E 결과를 반영하며 E2E가 모두 통과해야 멈춘다. E2E 화면 폭에 1536×864를 더한다. 완성과 E2E 통과 뒤 비평·독립 검증 전에 사용자 중간 확인을 한 번 둔다. 임시 서버는 T3의 시안 E2E가 끝나면 닫고, 중간 확인 때 다시 띄운다.

**메인 적용 조건 2·3(`msg_f5cb634b7576`).** npm 도구(Lighthouse, Playwright)는 버전을 고정하고 `--ignore-scripts`로 설치 스크립트를 끈다. 근거 폴더 안에만 두고 전역 설치·PATH 변경은 하지 않는다. 받은 파일·버전·SHA256 또는 lockfile을 근거 폴더에 기록하고, Playwright 브라우저가 사용자 프로필 아래에 받아지는 위치와 크기도 기록한다. 권한 확인에 막히면 우회하지 않고 메인에 보고한다. 세션 수는 시안 셋·완성 하나·비평 하나·선행 시험 하나·검증 하나를 넘기지 않는다. 시안 반려로 세션이 더 필요하면 열기 전에 메인에 알린다.

## 적용 중인 메인 결정

메인 `msg_8a1a882241d3`의 판단(초안 9절 답)이다. 사용자 결정이 아니다.

1. 고객 문장의 원천은 게임 화면에 실제로 보이는 문자열과 게임 데이터다. 근거표에 `파일:행`을 적는다. 코드 열거값·주석은 화면 문자열과 대조하는 보조 근거로만 쓴다. 화면 이름이 없으면 일반 명칭으로 쓴다.
2. README의 낡은 두 문장 정정은 이 PR에서 뺀다. 아래 「후속 후보」에 Rules 후속으로 적는다.
3. 그림 사본 생성 스크립트는 근거 폴더 E에 두고, 사본과 출처 표만 commit한다.
4. v1 판단 중 사이트 폴더 `00_Document/intro-site/`(Management 소유), 문서 지도 한 행(이 PR, Rules에 미리 알림), harness 근거 폴더, R-7 비해당, 새 workflow의 Rules·CodeMap 통지는 그대로다.

메인 `msg_f1ea2864cafe`(2026-10-07T19:33:35Z)의 통지 주소: Rules는 `run:run_573214a00f1b`이고 답을 기다리지 않는다. CodeMap은 지금 리드가 없어 PR 본문과 이 goal에 「새 workflow 추가, CodeMap 통지 대기」로 적고 메인이 다음 CodeMap 세션에 전달한다. 리드는 Rules에 문서 지도 한 행과 새 workflow를 통지했다(`msg_81a1748512d3`).

진입 메시지 `msg_8a1a882241d3`의 운영 지시: 우편함 대기는 하나만 연다. 사용자 결정이 필요한 범위 문제가 나오면 멈추고 status로 올린다. workflow 파일 push가 자격 증명으로 막히면 상태를 보존해 올린다. 확인 창이나 auto mode 분류기에 막히면 우회하지 않고 메인에 올린다. 병합 승인 요청에는 PR 번호·정확한 head·검증 판정 원문 경로·Pages 켜기 안내를 넣는다.

**디자인 재작업의 메인 판단(사용자 결정 아님).**

- `msg_80ddaefce11a`: (1) 미커밋 로컬 기록은 범위 개정과 함께 PR207 branch에 commit한다. (2) 디자인 재작업은 사용자 범위 변경에 따른 새 작업이라 사이트 산출물 수정 횟수에 넣지 않는다. 이후 사용자 반려와 독립 검증 결함 수정부터 센다. (3) 시안은 메인이 로컬 index.html을 브라우저로 열어 보이고, 리드가 화면 사진·프레임 묶음 경로를 같이 보낸다. (4) 진척 단계 12개와 첫 판 다섯 단계 접기를 수용한다.
- `msg_f5cb634b7576`: 시안 세션 셋을 먼저 열고 선행 시험 작성자는 그 뒤에 연다. split 뒤 기준 pane이 살아 있는지 매번 확인한다. 다른 게임 사이트 화면 사진은 근거 폴더에만 두고, 시안 계약에는 URL과 관측 문장만 넣는다.

## 만들 것

1. **소개 페이지 한 장** `00_Document/intro-site/index.html`, `styles.css`, 직접 쓴 스크립트 파일 하나(디자인 개정 Q1 A). 외부 라이브러리·WebGL·빌드 도구·npm 의존성·외부 요청 글꼴·CDN·분석 스크립트는 쓰지 않는다. JavaScript가 꺼져 있어도 모든 내용이 보이고, 스크립트는 움직임 보조만 맡는다. 페이지는 CSP meta로 같은 출처만 허용한다. 글꼴은 사이트 `fonts/`의 Pretendard 공식 웹 글꼴 부분 집합 보통·굵게와 OFL 전문이다(Q2 A). 절 구성은 초안 2절 1)의 표이고, 디자인은 v2 초안 2절 「추천 조합」을 방향으로 시안 셋에서 고른다.
   - 디자인 방향: 각 절을 게임 속 다른 장소·재질로 만들고 게임 스프라이트가 움직인다. 첫 화면은 게임 장면에 쓰이는 겹 배경과 게임 제목 로고의 등장 연출 하나에 힘을 모은다. 고정 메뉴 바에 절 메뉴와 움직임 켜기·끄기 버튼을 둔다. 다른 게임의 고유 표식은 따라 하지 않는다.
   - 첫 화면: 게임 이름, 한 줄 소개, 「개발 중인 프로토타입」 표시, 페이지 안 이동 버튼. 배경은 인게임 배경 그림이다. 배너 그림은 쓰지 않는다.
   - 이야기: 마을 주민의 대사와 퀘스트 「마을의 위협」을 바탕으로 두세 줄. 마을 가게 주인들의 화면 대사도 같은 원천 범주라 쓸 수 있다.
   - 플레이 특징, 직업(기사·마법사), 모험의 흐름(마을 → 사냥터 → 보스 → 결과 화면 → 마을, 슬라임·골렘·보스), 앞으로(개발 중·계획 배지와 「계획은 바뀔 수 있다」 한 줄), 개발 소식(README 「일정」의 두 사실, 공개 빌드 없음), 맨 아래(크레딧·소스 코드 링크·AI 생성 그림 표기·all rights reserved·기준 날짜와 commit).
   - 문장 규칙: 기술 용어를 본문에 쓰지 않는다(맨 아래 소스 코드 링크 줄만 예외). 과장 광고와 다른 게임 비교를 쓰지 않는다. 측정하지 않은 품질을 실적처럼 쓰지 않는다. 보스 고유 이름과 「Dawnholder」 뜻풀이는 원천에 없어 쓰지 않는다.
2. **웹용 그림 사본** `00_Document/intro-site/images/`. 첫 판의 원본은 README 그림(`00_Document/assets/`)과 적 idle 시트(`03_Client/Assets/Art/Enemy/`)다. 디자인 개정은 저장소 그림을 넓혀 쓴다. 성 계곡 겹 배경 레이어 1~6번(게임 장면 prefab이 쓰는 레이어), 게임 제목 로고 `UI/BackGround/Main_Title_Banner.png`, 기사·마법사·몬스터 동작 시트의 칸, 대장장이 초상, 게임 UI 틀이다. 자르기·축소·칸 이어 붙이기만 하고, 동작 칸은 원본 `.meta`의 칸 영역으로 자른다. 같은 장면에 함께 나오는 캐릭터·효과·몬스터 사본은 게임 원천(pixels per unit·prefab 배율) 기준으로 같은 화면 배율이 되게 줄인다(시안 선택 뒤 추가, 사용자 버그 「이펙트랑 캐릭터 크기」). 형식은 `cwebp`의 WebP이고 손실·무손실은 같은 그림으로 재고 고른다. 생성 스크립트는 E/`tools/`에 둔다(메인 결정 3).
3. **공개 허용 목록** `00_Document/intro-site/publish-files.txt`. 배포할 파일의 사이트 폴더 기준 상대 경로를 한 줄에 하나씩 적는다. workflow와 harness가 같은 목록을 읽는다. 디자인 개정에서 스크립트 파일, 글꼴 두 개와 OFL 전문, 새 그림 사본이 더해진다.
4. **사이트 안내** `00_Document/intro-site/README.md`. 로컬 미리보기, 허용 목록, 공개 금지 정보, 그림 출처 표(사본·원본 경로·원본 SHA256·자른 영역·사본 크기·사본 SHA256), 사실 근거표(문장·원천 `파일:행`·기준 commit), 글꼴 출처(배포처·판 번호·SHA256)를 둔다. 여러 칸을 이어 붙인 동작 사본은 「자른 영역」에 칸 목록을 적는다. 허용 목록 밖이라 배포되지 않는다.
5. **배포 workflow** `.github/workflows/intro-site.yml`. 조립 job은 허용 목록의 파일만 산출물에 넣고 `contents: read`만 가진다. 배포 job만 `pages: write`·`id-token: write`를 가지며 main push와 수동 실행에서만 돈다. PR에서는 조립까지만 돈다.
6. **진입 링크와 기록:** README 페이지 주소 한 줄, `00_Document/INDEX.md` 사이트 원본 한 행(Rules에 미리 알림), 이 goal, CURRENT Management 행.
7. **디자인 근거(커밋 대상 아님):** E/`design/`의 무드보드·디자인 요구서, 시안 셋(E/`drafts/`), 디자인 계획과 회차별 화면 사진, 디자인 비평(E/`critique/`). 다른 게임 사이트 화면 사진은 근거 폴더에만 둔다.

## 건드릴 곳과 소유권

첫 판(T1·S1·V1·V2)의 소유권은 다음과 같았다.

| 소유자 | 파일과 책임 |
|---|---|
| Management 리드 | 이 goal, `00_Document/operations/CURRENT.md`의 Management 행과 경로 줄, `00_Document/intro-site/`(index.html·styles.css·images/·README.md·publish-files.txt), README 링크 한 줄, `00_Document/INDEX.md` 한 행, E/`tools/`의 그림 스크립트, 위임 계약·근거·Git. 근거: AGENTS 「모델 라우팅」의 「보고서 자료의 … 본문·HTML·전용 생성 스크립트는 Astra가 작성한다」 |
| 신규 Sol(`gpt-6.1-sol` max) | `.github/workflows/intro-site.yml` 하나 |
| 신규 선행 시험 작성자(`claude-opus-5-5`) | E/`harness/`의 공개 안전·문구·그림 출처·링크·렌더 점검 harness |
| 신규 독립 검증자(`claude-opus-5-5`) | E/`verify/`의 시험·판정 파일만 |

디자인 재작업의 소유권은 다음과 같다(v2 초안 5절, Q3 A 라우팅 예외는 「적용 중인 사용자 결정」).

| 소유자 | 파일과 책임 |
|---|---|
| Management 리드(`claude-opus-5-5` xhigh) | 조율, 무드보드와 디자인 요구서, E/`tools/` 그림 스크립트와 사이트 `images/`·`fonts/`, 그림·글꼴 출처 표와 사실 근거표, `publish-files.txt`, 사이트 README, 이 goal, 위임 계약, Git |
| 신규 시안 세션 셋(`claude-opus-5-5`) | 각자 E/`drafts/<방향>/`의 첫 화면·직업 절 시안. 사이트 폴더는 쓰지 않는다. 방향은 계약에서 서로 다르게 지정한다 |
| 신규 완성 세션(`claude-opus-5-5`) | 사이트 `index.html`·`styles.css`·스크립트 파일. 고른 시안과 사용자 코멘트가 고정 입력이다. 디자인 비평 반영까지 한 작업으로 맡는다. 완성 뒤 쓰기를 멈추고 비평을 기다렸다가, 리드가 전달한 비평을 반영한 뒤 끝낸다(메인 세션 수 상한). 반영에 새 세션이 필요해지면 열기 전에 메인에 알린다 |
| 신규 선행 시험 작성자(`claude-opus-5-5`) | E/`harness-v2/`. 기존 harness 사본에 디자인 기계 검사와 1c·1f 계약 변경을 더하고 지금 head에서 빨간 단계를 기록한다 |
| 신규 E2E 시험 작성자 T3(`claude-opus-5-5`, 시안 선택 뒤 추가) | T2가 닫힌 뒤 E/`harness-v2/`를 이어받아 E2E를 더하고, 세 시안의 버그 목록과 지금 head의 빨간 단계를 E/`t3/`에 남긴다. 시안과 사이트 파일은 쓰지 않는다 |
| 신규 디자인 비평 세션(`claude-opus-5-5`) | E/`critique/`의 비평 파일만. design-critique·accessibility-review 스킬을 쓴다 |
| 신규 독립 검증자(`claude-opus-5-5`) | E/`verify3/`의 시험·판정 파일만 |

workflow는 바꾸지 않아 Sol은 열지 않는다. 시안 세션 셋은 파일이 겹치지 않아 동시에 연다. 작업자 한 명의 쓰기 종료·정산 뒤 다음 소유자에게 넘긴다. 리드 외 모든 세션의 commit/push는 금지다.

## 하지 않을 것

- 기술 설명 절: 서버 구조, 틱·TPS, 통신 규격·PDL, CI, 개발 운영 방식, 기술 스택, 구조 도식.
- 다운로드·플레이 버튼, 게임 빌드 배포, 공개 빌드가 있는 것처럼 읽히는 문구.
- 원천에 없는 세계관·설정·이름. 「Dawnholder」 뜻풀이와 보스 고유 이름도 포함한다.
- 「개발 중」·「계획」 콘텐츠를 「지금 게임에 있는 것」으로 쓰기, 출시일·완성 시점 약속.
- 원본 그림·`.meta` 수정, 유료 에셋 폴더(`Art/Environment/Others/Cainos/`) 그림, 콘셉트 그림의 배경 제거 가공, 배너 사용.
- 저장소 설정 변경: Pages 켜기(사용자 손), environment·ruleset 변경, 사용자 지정 도메인.
- 분석·추적 스크립트, 외부 요청 글꼴·CDN, 문의 폼, 댓글, 소식 구독. 영어판, 여러 페이지. 사이트 폴더 안 Pretendard 공식 OFL 글꼴은 디자인 개정 Q2 A로 허용한다.
- 외부 라이브러리·WebGL·빌드 도구(디자인 개정 Q1 A). 소리·음악, 자동 재생 영상, 플레이 영상 캡처(사용자 결정 Q4 A 유지), 픽셀 글꼴, 동의 배너.
- 근거표 행 없는 새 사실 문장. 디자인 작업은 문장 위치·길이만 다듬고 근거표 번호를 유지한다.
- 그림 파일을 새로 그리거나 합성해 게임에 없는 장면을 만드는 가공. 페이지 배치로 스프라이트를 배경 앞에 세우는 것은 게임 화면에 실제로 같이 나오는 조합만 한다.
- 다른 게임의 고유 표식 모방. 예: 해·달 원형 다이얼, 금박 장식 바, 소용돌이와 CRT 화면 조합.
- 기존 workflow 4개 수정, 공개 안전 검사의 CI 편입(후속 후보).
- README의 낡은 운영 문장 두 개 정정(메인 결정 2).
- 서버·Unity·운영툴 코드, BACKLOG·ORCA·AGENTS·`.agents`·Codex 설정.
- 내부 운영 정보와 개인 정보 게재: Orca handle·Run·msg ID, 로컬 절대 경로, 이메일·연락처, 토큰, 내부 PR·branch 진행 기록. 크레딧의 팀장 이름은 README 6행에 이미 공개된 범위만 쓴다.
- 새 도구 설치. 디자인 개정 Q2 A의 근거 폴더 전용 도구(`cwebp`, Lighthouse, Playwright와 Firefox·WebKit 브라우저)만 메인 적용 조건 2대로 예외다. 그 밖의 도구는 의존성 승인으로 멈춘다.
- 다음 goal 자동 착수.

## 관찰 가능한 완료조건

1. 사이트 원본·웹용 그림 사본·허용 목록·workflow·README 링크·INDEX 행이 main에 병합돼 있다.
2. 페이지의 모든 사실 문장이 근거표(원천 경로·`파일:행`·기준 commit)와 연결된다. 독립 검증자 대조에서 불일치가 0건이다. 「지금 게임에 있는 것·개발 중·계획」 구분이 PRD·FEATURE_MAP 원문과 같다. 원천에 없는 설정 문장이 0건이다.
3. 산출물 본문에서 기술 용어 목록의 등장이 0건이다. 맨 아래 소스 코드 링크 줄만 예외다. 근거는 harness 원시 출력이다.
4. 조립 산출물의 파일 목록이 `publish-files.txt`와 정확히 같다. 산출물 텍스트에서 내부 운영 정보 패턴(`term_`·`run_`·`msg_`, Windows·WSL 로컬 경로, 이메일, 토큰 접두)이 0건이다. 근거는 PR CI 조립 산출물에 돌린 harness 원시 출력이다.
5. 모든 그림 사본이 사이트 README 출처 표의 원본과 SHA256으로 연결되고, 원본 그림·`.meta`의 변경이 0건이다. 그림 합계 크기를 harness가 원시 출력으로 기록한다. 목표는 3MB 이하다(목표값이며 측정값 아님). 디자인 개정 뒤에는 3MB 목표 대신 9-12의 전송량 목표를 쓴다. 글꼴 파일도 출처 표의 배포처·판 번호·SHA256과 연결된다.
6. 실제 브라우저 렌더에서 데스크톱 폭과 좁은 화면(375px 안팎)에 가로 스크롤이 없다. 모든 그림이 보이고 모든 링크가 응답한다. JavaScript를 꺼도 모든 절이 보인다. 근거는 검증자 실행 기록이다. 디자인 개정 뒤에는 화면 폭을 9-1의 네 개로 넓힌다.
7. PR에서 새 workflow의 조립 job이 성공하고 배포 job은 돌지 않는다. 쓰기 권한이 배포 job에만 있다. 기존 workflow 4개가 이 PR에서 통과한다.
8. 병합과 Pages 켜기 뒤 main push run의 배포 job이 성공한다. `https://bass131.github.io/dawnholder-server/`가 페이지 제목을 반환하고, 배포본 파일 목록이 허용 목록과 같다. 이 항목은 병합 뒤 관찰이라 PR 통과와 따로 기록한다.

9 ~ 11은 디자인 개정(v2 초안 3절)에서 더한 완료조건이다. 사실 문장·기술 용어·허용 목록·내부 정보 검사(2~4)는 새 스크립트·글꼴·CSS 파일에도 적용한다.

9. **디자인 기계 검사.** 새 harness를 PR CI 조립 산출물에 돌린 원시 출력에서 모두 통과한다. 12와 13은 warning이다.
   1. 화면 폭 1920×1080, 1536×864, 1280×800, 390×844, 360×740에서 가로 스크롤이 없다(1536×864는 시안 선택 뒤 추가, Windows 125% 배율 1920 화면).
   2. 글자 대비가 본문 4.5:1 이상, 큰 제목 3:1 이상이다(WCAG 2.1 AA). 그림 위 글자는 화면 사진에서 글자 뒤 픽셀을 표본으로 잰다.
   3. 본문 글자는 16px 이상, 본문 문단 폭은 42em(한글 약 40자) 이하, 본문 줄 간격은 1.6 이상이다.
   4. 글꼴 파일이 실제로 로드되고, 페이지에 쓴 모든 글자가 글꼴 파일에 있다.
   5. 렌더 중 네트워크 요청이 모두 같은 출처다. 페이지는 CSP meta로 같은 출처만 허용한다.
   6. 5초 넘게 저절로 이어지는 움직임은 화면에 보이는 버튼으로 멈출 수 있고, JavaScript가 꺼져도 버튼이 작동한다(WCAG 2.2.2).
   7. 움직임 줄이기 설정에서 반복 애니메이션과 스크롤 연동 이동이 0개다. 이 설정과 JavaScript 꺼짐 각각에서 모든 절과 그림이 보인다.
   8. 첫 로드의 레이아웃 흔들림 점수(CLS)가 0.1 이하다.
   9. 원래 크기보다 크게 보이는 픽셀 그림은 흐림 없이 그린다(정수 배율, `image-rendering: pixelated`).
   10. 모든 링크·버튼에 키보드 초점 표시가 있다.
   11. 반복 애니메이션은 `transform`·`opacity`·스프라이트 칸 이동만 바꾼다.
   12. 첫 화면과 페이지 전체의 전송 바이트를 기록한다. 목표는 첫 화면 1.5MB 이하, 전체 4MB 이하다(목표값이며 측정값 아님).
   13. Chromium·Firefox·WebKit 세 엔진에서 1번과 7번이 통과한다. Lighthouse 모바일 기준 성능 90 이상, 접근성 100, 권장사항 100을 기록한다(목표값이며 측정값 아님).
   14. **E2E(시안 선택 뒤 추가, 사용자 요구).** 세 엔진 × 9-1의 화면 폭에서 사람처럼 조작한다. 첫 로드와 등장 연출, 스크롤 전 구간, 마우스 이동, 메뉴 링크 이동, 직업 고르기(마우스·키보드), 움직임 켜기·끄기, JavaScript 끔, 움직임 줄이기 설정이다. 콘솔 오류·잡히지 않은 예외가 0이고, 조작 뒤 화면 상태(요소 위치·겹침·잘림·보임)가 기대와 같다. 사용자가 본 버그 둘을 따로 판정한다. 동작 재생은 등장 연출·대기·이동·스킬 동작·스킬 효과마다 시간별 프레임을 잡아 칸 빠짐·겹침·엉뚱한 칸·깜빡임·빈 프레임·중간 끊김·대기 미복귀가 0이고 칸 영역이 원본 `.meta`와 같다. 크기 비율은 같은 장면의 캐릭터·효과·몬스터가 게임 원천 기준의 같은 픽셀 배율과 기준점으로 그려지고 화면 폭이 바뀌어도 비율이 유지된다. 동작 맞물림(사용자 답 `msg_978398a3a7a3`)은 캐릭터 동작과 효과가 게임과 같은 순간·기준점·방향으로 맞는 것이다. 시간별 프레임에서 캐릭터 칸 번호와 효과 칸 번호의 짝이 게임 원천에서 만든 기대 표와 같다. 시험 코드나 정지 사진으로 짚기 어려운 버그는 실제 Chrome 화면으로 재현한 뒤 판정으로 고정한다. 절별 가장 큰 빈 띠(글·그림 없는 세로 구간 높이의 화면 높이 대비)를 warning으로 기록한다. 조작 중 화면 사진·프레임을 남긴다.
10. **디자인 검토.** 신규 독립 검증자가 기준마다 충족·미충족과 근거(화면 사진·프레임 경로, CSS 위치)를 남기고, 미충족이 0건이다.
    1. 배치·여백: 같은 모양 카드의 반복이 기본 구조가 아니다. 절마다 다른 장소·재질이 있고 그 이유가 디자인 계획에 있다. 간격은 정의된 단계 값만 쓴다.
    2. 색·분위기: 색 토큰 4~6개가 게임 그림에서 나왔고 추출 근거가 있다. 디자인 스킬이 꼽는 생성형 기본값 다섯 가지 중 이유 없이 쓴 것이 0개다.
    3. 글꼴·글자 크기: 글자 크기가 정한 비율의 5~6단계 안에 있다. 첫 화면 제목은 게임 제목 로고다. 대문자 라벨, 제목 속 한 단어 강조, 제목 위 꾸밈 라벨이 없다.
    4. 그림 활용: 첫 화면은 게임 장면의 겹 배경과 로고다. 직업·몬스터 절은 실제 동작 스프라이트가 움직인다. 그림이 없는 절은 이유가 계획에 있다.
    5. 동적·세련: 대표 연출 하나(첫 화면)에 힘을 모은다. 방문자 행동에 답하는 움직임이 있다. 모든 절에 같은 등장 효과를 반복하지 않는다.
    6. 이전 판과 비교: 같은 화면 폭의 이전·이후 화면 사진을 나란히 두고, 사용자 불만 넷과 「기본 CSS 느낌」이 각각 어떻게 달라졌는지 판정한다.
    7. 레퍼런스와 비평: 무드보드에서 빌린 패턴과 이유가 계획에 있다. 다른 게임의 고유 표식을 따라 하지 않았다. 디자인 비평 세션의 지적마다 반영 위치나 반영하지 않은 이유가 있다.
    8. 화면 밀도(시안 선택 뒤 추가, 「너무 허전해」): 절마다 게임 그림·장식·정보가 화면을 채운다. 넓은 화면의 빈 띠와 첫 화면 아래 빈 공간이 계획에 이유 없이 남지 않는다. 9-14의 빈 띠 기록과 같은 폭의 화면 사진을 근거로 본다. 더한 내용은 근거표 안의 사실과 저장소 그림뿐이다.
11. **사용자 디자인 확인.** 시안 선택, 사용자 중간 확인, 최종 확인의 사용자 원문(메인 전달)과 확인한 head가 이 goal에 있다. 중간 확인은 완성과 E2E 통과 뒤, 디자인 비평·독립 검증 전에 한다(시안 선택 뒤 추가). 최종 확인한 사이트 파일과 병합 승인 요청 head의 사이트 파일이 같다.

## 설계와 검증 경계

- **등급 강.** workflow가 새 토큰 권한(`pages: write`·`id-token: write`)과 공개 범위를 다루는 보안 경계다. 본문만 보면 문서 실사지만 같은 PR이라 「애매하면 강」을 따른다. 실제 줄 수는 PR 때 `git diff --numstat`로 남긴다.
- **모델.** 선행 시험 작성자와 독립 검증자는 각각 신규 `claude-opus-5-5`다(검증자는 보안 경계라 시범 규칙의 Opus). workflow 구현은 신규 `gpt-6.1-sol`(max)이다. R-7은 메인 결정 4대로 비해당이다.
- **TDD.** 선행 시험 작성자가 사이트 파일이 없는 상태에서 harness를 돌려 실패를 먼저 기록한다. 리드 본문·그림 사본과 Sol workflow가 끝나면 같은 harness로 통과를 본다.
- **공개 허용 목록 하나.** 배포 파일은 `publish-files.txt` 한 곳에서 정한다. workflow는 이 목록만 복사하고, harness는 목록과 사이트 폴더·조립 산출물을 대조한다([하네스 원칙](../../../00_Document/conventions/CODE_CONVENTION.md#하네스-원칙) 3번). 목록에 파일을 더하는 일도 PR과 병합 승인을 거친다.
- **실제 경로 실행.** 강 등급의 실제 경로는 PR CI의 조립 job 1회와 그 산출물(`github-pages` artifact)에 돌린 harness다. 배포 job은 병합 전에 실행할 수 없어 PR 판정에서는 「미실행·판정 보류」로 두고 완료조건 8에서 확인한다.
- **고객 문장의 사실 대조.** 검증자는 사이트 README 근거표의 원천을 직접 열어 문장마다 대조한다. 특히 「지금 게임에 있는 것」으로 쓴 항목이 PRD 「구현」 행과 게임 데이터에 실제로 있는지 본다.
- **그림.** 사본은 Windows PowerShell 5.1의 System.Drawing으로 만든다(scratchpad 시험 exit 0, E/`astra-context.md`). 확대는 하지 않는다. 사본에서 원본의 AI 생성 표식이 사라질 수 있어 페이지의 「그림 일부는 AI 생성 도구를 활용해 제작」 표기와 출처 표가 대신한다.
- **언어 규칙.** HTML·CSS·workflow YAML에는 CODE_CONVENTION 언어 절이 없다. 임시 기준은 REPORTING 「HTML 구성」과 기존 workflow 관례(최상위 `contents: read`, `persist-credentials: false`, `timeout-minutes`, 태그 고정)다. 정본으로 만들지 않는다.

디자인 개정의 설계와 검증 경계는 다음과 같다(v2 초안 4·5절).

- **등급 강 유지.** 공개 파일에 실행 코드·글꼴·CSP가 더해지고 허용 목록이 바뀌어 공개 범위 보안 경계다. 실제 경로는 PR CI 조립 1회와 그 산출물에 돌린 새 harness다. workflow는 바뀌지 않는다.
- **모델.** 시안·완성·선행 시험·검증은 신규 `claude-opus-5-5`다(검증자는 보안 경계라 시범 규칙의 Opus). 디자인 비평은 보안 판정이 아니지만 같은 PR의 리뷰이고 해당 여부가 애매해 Opus로 둔다. R-7은 4범주에 디자인이 없어 비해당이다.
- **TDD.** 선행 시험 작성자가 사이트 파일을 고치기 전 지금 head에서 새 harness의 빨간 단계를 기록한다. 시안은 근거 폴더에만 써서 빨간 단계 순서에 영향이 없다.
- **디자인 스킬과 증거.** 시안·완성 세션은 계획 전과 제작 시작 때 frontend-design 스킬을 호출하고 호출 위치를 보고에 적는다. 계획은 스킬의 두 단계(색 4~6개와 추출 근거·글꼴 역할·배치와 ASCII 와이어프레임·원칙 → 생성형 기본값과 대조해 고친 부분)를 따른다. 회차별 화면 사진과 한 줄 자기 비평을 남기며, 이는 자체 점검이고 독립 판정이 아니다.
- **디자인 품질 검증.** 기계 검사는 harness 원시 출력으로 본다. 검토 기준은 화면 폭 4개 × JavaScript 켬·끔 × 움직임 줄이기 켬·끔 화면 사진과 첫 화면의 시간별·스크롤 위치별 프레임 묶음으로 본다. 검증자는 작성자의 계획을 결론으로 쓰지 않는다. 주관 판단의 최종 관문은 사용자 확인이다.
- **외부 파일.** 모두 공식 배포본만 받고 판 번호·SHA256 또는 lockfile을 근거 폴더에 기록한다. 글꼴은 직접 자르거나 변환하지 않는다(OFL 이름 규정). 부분 집합에 페이지 글자가 빠졌으면 공식 전체 파일로 바꾼다.

## 교정 기록

- **사용자 교정(첫 발생):** 「디자인 재작업 범위 개정」의 원문. 사용자는 Sol이 만들었다고 봤지만, 사이트 파일은 이전 리드(`claude-opus-5-5`)가 썼고 Sol은 workflow만 썼다(메인 확인). goal 완료조건에 디자인 기준이 없었고, 디자인 스킬 호출이 0회였고, 독립 검증은 사실·보안만 판정했다.
- **층 선택:** 잴 수 있는 기준(대비·글자 크기·줄 폭·글꼴 글자 포함·외부 요청·정지 버튼·움직임 줄이기·흔들림·픽셀 선명도·초점)은 테스트 층(harness)으로 막는다. 배치·분위기·세련은 기계 검사로 바꾸면 가짜 통과가 생길 수 있어 문서 층(완료조건 10)과 디자인 비평·사용자 확인으로 막는다.
- **반복 규칙:** 첫 발생이라 이 goal에만 기록한다. 같은 누락(사용자에게 보이는 화면 산출물 goal에 디자인 기준·디자인 판정이 없음)이 다시 나오면 「사용자에게 보이는 화면 산출물에는 디자인 완료조건과 디자인 판정을 둔다」를 goal-loop 범위 절에 넣도록 Rules에 요청한다.

## PR 경계와 종료

각 PR은 최신 main의 새 branch에서 만들고, 정확한 head의 CI·메인 R-2 뒤 사용자 개별 병합 승인을 받는다. 리드는 병합하지 않고 준비 보고만 한다.

| PR | 내용 | 등급 |
|---|---|---|
| 제품 PR | 이 goal, 사이트 폴더, workflow, README 링크, INDEX 행, CURRENT Management 행 | 강 |
| 종료 기록 PR | 배포 확인 결과, Gardener 결과, goal 결과 절 | 문서 실사 |

사용자 손은 Pages 켜기(Settings → Pages → Source를 「GitHub Actions」로)와 두 PR의 병합 승인이다. Pages 켜기는 제품 PR 병합 승인 직전이 좋다. 안 켠 채 병합하면 첫 main run의 배포 job이 실패한다. 공개 시점은 병합과 Pages 켜기가 둘 다 끝난 때다. 배포 확인에서 실패하면 수정 PR을 하나 더 두고 메인에 알린다. 점검 지점은 전체 goal 종료 한 번이다. 같은 산출물 수정이 3회를 넘으면 메인 체크포인트를 알린다. 다음 goal은 자동으로 시작하지 않는다.

디자인 개정으로 사용자 손에 시안 선택과 최종 디자인 확인이 더해진다. 제품 PR은 PR207 하나 그대로이며 같은 branch에 commit을 더해 새 head를 만든다. 로컬 `origin/main`이 base `7086d45b`에서 `3bb2e77a`로 나아갔고 `00_Document/operations/CURRENT.md`가 겹친다. 디자인 완성 단계에서 main을 branch에 병합해 정리하고 harness와 CI를 다시 돌린다. 강제 push는 하지 않는다.

### 디자인 작업 순서

1. goal 개정을 미커밋 로컬 기록과 함께 PR207 branch에 commit한다.
2. 리드가 Q2 파일을 받고, 그림 사본(WebP, 동작 칸)과 무드보드·디자인 요구서를 만든다.
3. 시안 세션 셋을 연 뒤 선행 시험 작성자를 연다. 시안 화면 사진·프레임과 로컬 미리보기 경로를 메인에 보내 사용자가 하나를 고른다. 모두 반려면 요구서부터 다시 하고, 세션이 더 필요하면 열기 전에 메인에 알린다.
4. (시안 선택 뒤 개정) T2를 정산한 뒤 E2E 시험 작성자 T3을 연다. T3은 세 시안의 버그 목록을 만들고 지금 head에서 E2E 빨간 단계를 남긴다. 임시 서버는 T3의 시안 E2E가 끝나면 닫는다.
5. 리드가 D1이 쓰는 그림의 사이트용 WebP 사본(게임 원천 기준 같은 픽셀 배율)과 글꼴 사본, README 출처 표를 만든 뒤 완성 세션을 T3와 병렬로 연다. 완성 세션은 D1 방향으로 모든 절을 채우고 화면 밀도·사용자 버그 둘을 처음부터 따른다. 멈춤 지점에서 T3의 D1 버그 목록과 E2E 결과를 반영하고, E2E가 모두 통과해야 멈춘다. 리드는 근거표·허용 목록을 맞추고 harness로 자체 점검한다.
6. 사용자 중간 확인: 임시 서버로 완성본을 보인다.
7. 디자인 비평 세션이 비평하고, 리드가 비평과 중간 확인 의견을 완성 세션에 전달해 반영시킨다. 리드가 main을 branch에 병합하고 push한다.
8. 신규 독립 검증자가 PR CI 조립 산출물로 강 등급 검증을 한다(E2E 포함).
9. 사용자 최종 디자인 확인 뒤 새 head로 병합 승인을 요청하고 Pages 켜기를 안내한다. 이후는 배포 확인, Gardener, 종료 기록 PR이다.

## 후속 후보

- README의 낡은 운영 문장 두 개 정정: 메인 결정 2에 따라 Rules 후속이다. 초안 v1 위험 7(E0/`scope-draft-v1.md` 130행)이 짚은 문장은 README 14행(「현재는 Codex 메인 세션이 목표와 결정을 관리」)과 65행(`.agents/skills/`를 「Codex 스킬」로 부름)이다. AGENTS 현행은 「메인 Claude」다.
- 공개 안전 검사의 CI 편입: harness의 허용 목록·내부 정보 패턴·기술 용어 검사를 workflow 조립 단계에 넣는 일. 이번 범위 밖이다.
- V1 설계 관찰에서 온 후보(비차단, E/`verify/verdict.md` 「설계 관찰」): 조립 job의 저장소 전체 checkout을 사이트 폴더만 받도록 줄이기(O2, Management), 쓰기 권한 job의 action을 commit SHA로 고정할지와 저장소 전체 action 고정 정책(O3, CodeMap·Rules 판단), workflow 안 조립 bash를 저장소 스크립트로 옮기기(O5, 다음 workflow 수정 때 Management), PRD 14행의 인벤토리 구분 갱신(O8, Rules).
- V2 설계 관찰에서 온 후보(비차단, E/`verify2/verdict.md`): 사이트 README F04 설명에 「장면 파일에는 `\u` 이스케이프로 저장돼 있다」와 빌드 장면 근거 `EditorBuildSettings.asset`을 더하기(P1, Management), 근거 스크립트 리터럴 행 길이 해석(P3, Rules·메인), 검증자의 F04 화면 노출 시험(E/`verify2/tests/f04-onscreen-source.mjs`)을 harness `facts` 검사로 옮기기(V1 #1 검사 전환, Management).
- 디자인 개정 조사에서 온 후보(v2 초안 1절): 플레이 영상 반복 재생(사용자 결정 Q4 A가 캡처를 뺐고, 촬영과 유료 소품 확인이 필요하다), 픽셀 글꼴의 라이선스 확인, 공개 안전·디자인 기계 검사의 CI 편입(기존 CI 편입 후보와 같은 갈래).
- `.gitattributes`에 `*.webp` 줄이 없어 `* text=auto`의 내용 판별에 기대고 있다. 이번 사본 21개는 앞 8000바이트에 NUL이 있어 이진으로 잡힌다(리드 확인). 저장소 공통 설정이라 Rules·CodeMap 판단이다.
- 범위 밖 기존 위험(초안 9절 5, 이번에 고치지 않음): 유료 Asset Store 패키지 원본이 공개 저장소에 있다. 폰트 NOTICE가 임시 출처 표기 상태다. README 34행의 「ComfyUI 기반 AI 파이프라인」 표기와 일부 그림의 다른 생성 표식이 맞지 않는다.

## 결과와 열린 사항

### 작업자 발행

- **T1 선행 시험(신규 `claude-opus-5-5`):** pane `term_dc17bfef…`, Task `task_1ba49ab2bc2b`, Dispatch `ctx_8867a991f3bd`. 계약 E/`contracts/t1-task.md`(SHA256 `e5dde107…63d9`), 발행 기록 E/`contracts/t1-issue.txt`. 화면 표시 `Opus 5.5 with xhigh effort`, backend unknown.
- **S1 workflow(신규 `gpt-6.1-sol` max):** pane `term_fe526980…`, Task `task_6949ecf80996`, Dispatch `ctx_9cc00fe000a9`. 계약 E/`contracts/s1-task.md`(SHA256 `ff714b5e…3c81`), 발행 기록 E/`contracts/s1-issue.txt`. 화면 표시 `GPT-6.1-Sol max`(Codex v0.160.1), backend unknown.
- **S1 계약 보충 v1.1:** Sol 질문 `msg_0a46b180596a`(2026-10-07T19:50:15Z)은 `actions/upload-pages-artifact` v5의 tar가 `.git`·`.github`를 항상, 숨김 구간을 기본값에서 빼서 목록과 산출물이 어긋날 수 있다고 했다(원시 E/`s1/raw/upload-pages-artifact-action-v5.stdout.txt` 17~39행, 리드 확인). 리드는 Sol의 두 선택지(숨김 포함 + `.git`·`.github` 거부 / 한계 기록) 대신 「`.`으로 시작하는 구간은 목록에서 거부하고 `include-hidden-files`는 기본값 유지」로 답했다. 공개할 숨김 파일이 없어 공개 범위를 넓힐 이유가 없고, 목록 단계에서 막으면 tar 제외 규칙과 목록이 항상 같아진다. 접점 정의는 E/`site-interface.md` v1.1(SHA256 `c0dad5f7…396c`)이고, 이미 발행한 T1에는 주입하지 않았다. 보충 규칙은 workflow 자체 점검과 독립 검증이 확인한다.

### 선행 시험 결과(T1)

- `worker_done` `msg_96d483ec3878`(2026-10-07T20:19:47Z, outcome succeeded). 발신 pane·Task·Dispatch가 발행 기록과 일치했다. 보고 E/`t1/report.md`.
- 빨간 단계: 사이트 폴더가 없는 상태에서 `check-site.mjs --mode source`가 exit 1·`site-missing`, `check-render.mjs`가 exit 1·`site-index-missing`이었다(E/`t1/raw/red-check-site-source.json`, 리드가 원시 JSON의 `status`·`exitCode`·진단 코드를 직접 읽음).
- `selftest.mjs`는 exit 0이었다. 사례 39개 중 37개 통과, 실패 0, 온라인 2개는 `--online` 별도 실행에서 통과했다(E/`t1/raw/selftest-summary.json`). harness 사본 변형 6개는 모두 selftest가 실패로 잡았다.
- 작성자 해석 9개와 한계는 보고 「작성자 판단으로 정한 해석」·「미실행과 한계」에 있다. 리드는 기본값대로 받아들였다.
- 정산: `worker-release` 결과 `retained`. pane이 대기(`✳`, tui-idle)인 것을 확인하고 `terminal close`로 닫았다(E/`t1-release.json`, E/`t1-close.json`).

### workflow 구현(S1)

- `worker_done` `msg_4c7b05b3883d`(2026-10-07T20:30:32Z, outcome succeeded, filesModified `.github/workflows/intro-site.yml`). 발신 pane·Task·Dispatch가 발행 기록과 일치했다. 보고 E/`s1/report.md`, 메모 E/`s1/sol-context.md`.
- Sol 자체 점검(독립 검증 아님): workflow에서 기계 추출한 조립 bash 본문을 E/`s1/` 합성 fixture에 돌려 37/37 통과, `bash -n` 통과. v1.1 숨김 구간 거부를 적용했고(`intro-site.yml` 109~112행) 기존 workflow 4개는 바꾸지 않았다. YAML 문법·GitHub 실행·Linux 0644 권한은 미실행이라 PR CI에서 본다(WSL drvfs에서는 권한이 777로 보임).
- action 버전: `actions/checkout@v7`(최신 v7.0.1), `actions/upload-pages-artifact@v5`(v5.0.0), `actions/deploy-pages@v5`(v5.0.1). 근거는 E/`s1/raw/*-latest.*`와 보고 「action 버전과 호환성 근거」다. 기존 workflow는 `checkout@v4`를 쓰므로 관례와 다르다. 리드는 최신 release 근거가 있어 받아들이고 독립 검증의 설계 관찰 대상으로 넘긴다.
- 정산: `worker-release` 결과 `retained`. pane 대기를 확인하고 `terminal close`로 닫았다(E/`s1-release.json`, E/`s1-close.json`). 리드가 commit했다(`f48920be`).

### 사이트 작성(리드)

- 그림 사본은 E/`tools/make-web-images.ps1`로 사이트 폴더에 만들었다. staging 실행과 SHA256이 같아 결정적이다(E/`tools/site-images.sha256`). 합계 571,331 B다(E/`tools/manifest-site.json`의 `outputBytes` 합).
- 사이트 commit `c6fa9d95`, 진입 링크 commit `5a57d8c4`.
- harness 통과(리드 자체 점검이며 독립 판정 아님): 첫 `check-site --mode source --online`은 exit 1이었다. README의 두 표 앞에 설명 문단이 있어 표 머리글 위치가 접점 3절과 달랐다(`fact-table-header`, `image-table-header`). 설명을 표 뒤로 옮긴 뒤 exit 0, 검사 10개 모두 passed였다(E/`lead-run/check-site-2.json`). `check-render`는 exit 0이었다. 데스크톱 clientWidth 1265, 그림 6개 로드, 절 6개 표시였다(E/`lead-run/check-render-1.json`, 화면 사진 E/`lead-run/render-1-shots/`).

### 독립 검증 V1(강)

- 발행: 신규 `claude-opus-5-5`, pane `term_5aef681e…`(split 최초 실행 명령 `claude --model claude-opus-5-5`, 화면 표시 `Opus 5.5 xhigh`, backend unknown), Task `task_4a29e8b8e360`, Dispatch `ctx_b6ee8db0c5dc`. 계약 E/`contracts/v1-task.md`(SHA256 `cb2cb8c3…906d`), 대상 head `a5691d8d`.
- `worker_done` `msg_d7b4ae46a4c1`(2026-10-07T20:59:33Z, outcome succeeded). 발신 pane·Task·Dispatch가 발행 기록과 일치했다. 판정 원문 E/`verify/verdict.md`.
- 판정: **차단(#1·#2·#3).** #1은 이야기 절의 마을 주민 대사(F04)가 현재 게임 화면에 나오지 않는 것이다. 원천 `NpcVillager.prefab`을 참조하는 장면·prefab이 기준 commit에 없고, 2026-06-01 `a8ec485a`부터 어느 장면에도 배치되지 않았다. 사용자 결정 Q2와 메인 결정 1 위반이다. #2는 사이트 README 허용 목록 규칙에 v1.1 숨김 구간 거부가 빠진 것, #3은 근거 폴더 그림 스크립트의 120자 초과 호출 세 줄이다.
- 지적 없음: PR run `37682714836`의 조립 성공·배포 skipped, artifact 8파일과 허용 목록·조립 로그 SHA·head blob 일치, 산출물에 돌린 `check-site --mode assembled --online`·`check-render` exit 0, 조립 본문 독립 시험 42/42(변형 3개 모두 검출), 기존 workflow 4개 성공, 나머지 사실 25개 일치.
- 리드 R-2 표본: #1 원시(E/`verify/raw/f04-villager-placement.txt`)를 다시 확인했다. 기준 commit에서 villager GUID를 `.unity`·`.prefab`·`.asset`·`.cs`로 찾으면 0건이다. Town 장면의 대장장이·잡화점 대사 두 개를 직접 풀어 읽었다(E/`lead-run/decode-dialog.mjs`).
- 정산: `worker-release` 결과 `retained`. pane 대기(`✳`, 빈 프롬프트)를 확인하고 `terminal close`로 닫았다(E/`v1-release.json`, E/`v1-close.json`).

### V1 결함 수정(리드, 사이트 산출물 수정 1회째)

- **#1:** 이야기 절의 인용을 빌드 장면 `Town.unity`의 활성 NPC 대장장이 대사로 바꿨다(`Town.unity:5986-5988`, 대화 창 표시 `NpcInteractable.cs:84`). 「만들 것」 1이 이미 허용한 가게 주인의 화면 대사라 범위 안이다. 「만들 것」 1의 「마을 주민의 대사」는 그 prefab이 장면에 있다는 잘못된 전제였고, 승인 문구는 바꾸지 않고 이 기록으로 남긴다. 화면 이름이 없어 「마을 대장장이」라는 일반 명칭을 쓴다(메인 결정 1). 사이트 README 근거표 F04와 아래 설명 문단도 같이 고쳤다.
- **#2:** 사이트 README 「공개 허용 목록」에 「`.`으로 시작하는 구간(숨김 파일·폴더)」을 더했다.
- **#3:** E/`tools/make-web-images.ps1`의 세 호출을 인자 한 줄씩 나눴다(커밋 대상 아님). 다시 돌린 그림 6장의 SHA256이 커밋된 사본과 같다(E/`lead-run/rerun-images.sha256`, E/`lead-run/committed-images.sha256`).
- 같이 반영한 관찰: O9(「첫 퀘스트」 → 「퀘스트 「마을의 위협」의 목표」, 게임의 퀘스트는 하나), O8(F18 근거에 `00_Document/operations/CURRENT.md:7` 추가), O7(이 goal의 재개 지점 갱신).
- 반영하지 않은 관찰: O1(산출물 루트 디렉터리 0700)은 배포 요구 문서에 권한 조건이 없고(E/`lead-run/upload-readme-v5.md` 「tar 파일 조건」) 완료조건을 막는다는 근거가 없어 고치지 않는다. 완료조건 8 배포 확인에서 보고, 실패하면 수정 PR을 둔다. O2·O3·O5·O8의 PRD 갱신은 「후속 후보」, O4는 기존 후속 후보(CI 편입), O6은 범위 밖이다.
- 리드 자체 점검(독립 판정 아님): 수정 뒤 `check-site --mode source --online` exit 0·진단 0, `check-render` exit 0(E/`lead-run/fix1/`). 데스크톱 화면 사진에서 이야기 절을 직접 봤다.
- 수정 commit `7cd42322`(push 완료). workflow는 바뀌지 않았다. 메인에 진행 상태를 보냈다(`msg_4b61fc75d99d`).

### 독립 재검증 V2(강)

- 발행: 신규 `claude-opus-5-5`(V1과 다른 세션), pane `term_137f69c2…`(split 최초 실행 명령 `claude --model claude-opus-5-5`, 화면 표시 `Opus 5.5 with xhigh effort`, backend unknown), Task `task_74883f99d99f`, Dispatch `ctx_067e6590dbe9`. 계약 E/`contracts/v2-task.md`(SHA256 `35b6b578…2143`), 발행 기록 E/`contracts/v2-issue.txt`, 대상 head `7cd42322`.
- `worker_done` `msg_407bd8508449`(2026-10-07T21:31:13Z, outcome succeeded). 발신 pane·Task·Dispatch가 발행 기록과 일치했다. 판정 원문 E/`verify2/verdict.md`.
- 판정: **통과.** V1 #1·#2·#3 해소, 새 결함 없음. #1은 검증자가 새로 쓴 화면 노출 시험(E/`verify2/tests/f04-onscreen-source.mjs`)이 새 head에서 10/10, V1 head에서 8건 실패였다. 실제 경로는 run `37686834836`의 조립 성공·배포 skipped, artifact 8파일과 목록·조립 로그 SHA·head blob 일치, `check-site --mode assembled --online`·`check-render` exit 0이다. workflow 무변경(V1 시험 재실행 42/42), 그림 스크립트 재실행 6장 SHA 일치, 기존 workflow 4개 성공이다.
- 리드 R-2 표본: F04 시험 두 head의 원시 표(E/`verify2/raw/f04-onscreen-*.tsv`, exit 0과 1), 산출물 세 쪽 SHA 대조(`inventory-compare.txt`의 두 diff exit 0, 그림 571,331 B), 목록 대조 diff exit 0, 그림 재실행 대조 exit 0을 직접 읽었다. `gh pr checks 207`은 deploy skipping, 나머지 모두 pass다.
- 비차단 관찰: P2(이야기 인용이 사용자가 본 범위 초안 v2 Q2의 예시 대사와 다름)는 병합 승인 요청에 한 줄로 알린다. P4(산출물 루트 0700)는 V1 O1과 같고 배포 확인에서 본다. P1·P3과 시험 전환은 「후속 후보」에, P5(이번 checkout 12초)는 O2 후보의 관측으로 남긴다.
- 정산: `worker-release` 결과 `retained`. pane 대기(`✳`, 빈 프롬프트)를 확인하고 `terminal close`로 닫았다(E/`v2-release.json`, E/`v2-close.json`).
- 그 뒤: 메인에 병합 승인 요청을 보냈고, 사용자가 디자인 재작업으로 보류했다(「디자인 재작업 범위 개정」).

### 디자인 범위 개정(새 리드)

- 새 리드(`claude-opus-5-5` xhigh, 화면 표시 `Opus 5.5 xhigh`, backend unknown)가 2026-10-08 11:0x KST에 Run을 인수했다(run-use, consumer_generation 2). READY `msg_bd5b023cd4f5`.
- v1 초안(E/`design-scope/scope-design-draft.md`, SHA256 `1fff9304…4b608`)을 `msg_21b65cf410f3`로 보냈다. 사용자는 「최고점」 방법을 더 찾으라고 수정을 요청했다(`msg_449e095ece04`).
- v2는 표현 기술·빌드 방식·과정·그림·작성 모델 다섯 갈래를 비교했다. 웹 조사는 Claude 읽기 전용 서브에이전트 셋이 했고, 리드가 MDN 호환 데이터(Firefox 스크롤 연동 `"preview"`), npm `gsap` 라이선스 필드, Pretendard Std의 문자 범위를 직접 확인했다. 사례 사이트 기술 관측의 표본 대조 1건은 CSS를 읽지 못해 확정하지 못했다. v2는 `msg_231d995453d7`로 보냈고 승인됐다(`msg_f5cb634b7576`).
- v1 정정: v1과 첫 보고의 「Pretendard Std」는 한글이 없다. v2는 전체 Pretendard 계열 공식 부분 집합을 쓴다.
- 첫 화면 근거(리드 확인): 겹 배경 prefab `Prefabs/Environment/Parallax_CastleValley.prefab`이 마을·사냥터·보스방 장면에 놓여 있고 레이어 1~6번을 쓴다. 7번 전경 풀숲은 쓰지 않는다. 기사·마법사·슬라임 대기 시트의 `.meta`에는 칸 영역이 16개씩 있다.
- 겹 배경 무게 측정(scratchpad, 리드): 레이어 2~7번 6장을 1440px 폭 PNG로 줄이면 합계 2,386,699 B다(E/`design-scope/raw/size-probe/probe.log`).
- 운영 기록: curl로 출처 두 개를 받으려던 명령이 권한 확인에서 거부됐다. 우회하지 않고 WebFetch로 확인했다.

### 무드보드와 시안 발행

- **Q2 외부 파일(근거 폴더 안, 전역 설치 없음):** Pretendard 1.3.9를 `npm pack`으로 받아 공식 부분 집합 woff2 두 개와 OFL 전문을 꺼냈다. Lighthouse 13.5.0·Playwright 1.64.0은 정확한 버전으로 `--ignore-scripts` 설치했고, Firefox·WebKit은 Playwright 기본 위치(사용자 프로필)에 받았다. 판·크기·SHA256·lockfile hash는 E/`tools/ext/RECORD.md`에 있다.
- **cwebp:** 리드의 curl 받기가 권한 확인에서 거부됐다. 우회하지 않고 메인에 올렸고, 사용자가 직접 받았다(메인 `msg_62c7295ac021`, 도착 `msg_acc183f564f8`). zip SHA256 `48886f50…93f1`이 메인 확인 값과 같고, `cwebp -version`은 1.6.0이다. 공식 페이지에 체크섬이 없고 PGP 서명은 검증하지 않았다(한계, RECORD.md).
- **그림 묶음(근거 폴더, 시안용):** 리드 스크립트 E/`tools/make-design-assets.ps1`(SHA256 `52ddd9bd…26b5`)로 겹 배경 1~6번, 로고, 대장장이 초상, UI 틀 셋, 동작 띠 13개를 만들었다. 동작 칸은 각 시트 `.meta`의 칸 영역에서 자르고 칸 안 아래 가운데에 놓았다. 24개 8,328,017 B이며 목록·원본·SHA256은 E/`design/assets/manifest.json`(SHA256 `3ec5e46c…6cff`)이다. 사이트에 넣을 사본과 WebP 변환은 시안 선택 뒤 따로 만든다.
- **무드보드:** E/`design/moodboard.md`(SHA256 `e719dd58…43ee1`). 사례 여섯과 비교 기준 둘, 빌릴 패턴 8, 피할 것 6, 따라 하지 않을 고유 표식, 우리 그림으로 되는 것과 안 되는 것이다. 다른 사이트 화면 사진은 두지 않았다(메인 조건).
- **시안 세션 셋(구현 역할 태그 `[Management Sol]`, 실제 모델 `claude-opus-5-5`):** 2026-10-08 12:29~12:31 KST, 계약 E/`contracts/d-task.md`(SHA256 `c6744e89…d124`), 발행 기록 E/`contracts/d-issue.txt`, 요약 E/`design/launch/summary.md`. split 최초 실행 명령 `claude --model claude-opus-5-5`, 첫 화면 표시 「Opus 5.5 with xhigh effort」, 선택창 없음, backend unknown. 세 세션 모두 `input_accepted`와 `turn_started`를 확인했다.

| 방향 | 쓰기 폴더 | pane | Task | Dispatch |
|---|---|---|---|---|
| D1 노을 풍경 시네마틱 | E/`drafts/d1-cinematic/` | `term_1db550e9…` | `task_90d1faf07484` | `ctx_dfd2c5c656eb` |
| D2 게임 화면 속으로 | E/`drafts/d2-game-ui/` | `term_7ea65798…` | `task_58c51f1940e4` | `ctx_35f34844d490` |
| D3 사이드스크롤 여정 | E/`drafts/d3-side-scroll/` | `term_398176fb…` | `task_6671d4a0b274` | `ctx_d3b82c584ae7` |

- **D1 결과:** `worker_done` `msg_500326db0c2e`(2026-10-08T04:22:12Z, outcome succeeded). 발신 pane·Task·Dispatch가 발행 기록과 일치했다. 보고 E/`drafts/d1-cinematic/report.md`. 자체 점검(독립 판정 아님)은 27회 가로 넘침 0, 같은 출처 요청만, JS 없이 정지 스위치 작동, 움직임 줄이기 반복 0, 대비 최소 4.89:1, CLS 0, 글꼴 빠진 글자 0이다. 첫 화면 6,773,035 B로 목표를 넘는다. 넓은 화면 2배 표시는 0.5배 축소본을 키워 거칠다. 리드는 폴더 밖 쓰기가 없고 화면 사진이 실제 렌더임을 직접 봤다. 정산: `worker-release` `retained`, pane 대기(✳)를 확인하고 닫았다(E/`design/launch/d1-release.json`, `d1-close.json`).
- **D2 결과:** `worker_done` `msg_c64b955f0568`(2026-10-08T04:04:48Z, outcome succeeded). 발신 pane·Task·Dispatch가 발행 기록과 일치했다. 보고 E/`drafts/d2-game-ui/report.md`. 자체 점검(독립 판정 아님)은 세 엔진 가로 스크롤 없음, 다른 출처 요청 0, JS 끔 정지 버튼 작동, 움직임 줄이기 애니메이션 0, CLS 0이다. 첫 화면 4,736,521 B·전체 6,746,534 B는 PNG 원본이라 목표를 넘고, 완성 때 WebP로 줄인다. 리드는 폴더 밖 쓰기가 없고(E의 새 파일은 모두 리드 기록) 화면 사진이 실제 렌더임을 직접 봤다. 정산: `worker-release` `retained`, pane 대기(✳)를 확인하고 닫았다(E/`design/launch/d2-release.json`, `d2-close.json`).
- **D3 결과:** `worker_done` `msg_75ea919fbc06`(2026-10-08T04:21:30Z, outcome succeeded). 발신 pane·Task·Dispatch가 발행 기록과 일치했다. 보고 E/`drafts/d3-side-scroll/report.md`. 자체 점검(독립 판정 아님)은 세 엔진 × 세 조건 × 세 폭 27회 가로 넘침 0, 외부 요청 0, 정지 수단 반복 5→0, 대비 최저 4.88, CLS 0.001 이하다. 첫 화면 5,408,401 B·전체 5,661,134 B로 목표를 넘고, WebKit 스크롤 연동 0px 한 번은 재측정에서 재현되지 않았다. 추가 요청은 WebP, 이음매 맞춘 겹, favicon이다. 리드는 폴더 밖 쓰기가 없고 화면 사진이 실제 렌더임을 직접 봤다. 정산: `worker-release` `retained`, pane 대기(✳)를 확인하고 닫았다(E/`design/launch/d3-release.json`, `d3-close.json`).
- **선행 시험 T2 준비:** 접점 E/`site-interface-v1.2.md`(SHA256 `a6bfe124…33a2`)와 계약 E/`contracts/t2-task.md`(SHA256 `b001e5a7…c18d`)를 썼다. 메인 조건대로 시안 세션 셋을 닫은 뒤 연다.
- **시안 미리보기와 선택 요청:** 리드가 127.0.0.1:8750 임시 서버(E/`tools/preview-server.mjs`, 루트 E, 읽기 전용)를 띄우고 세 주소·화면 사진 경로·리드 의견(D1 추천)을 한 번에 보냈다(`msg_fcfc7a7c089b`). 메인은 세 주소 200과 대표 사진을 확인하고 현황판 결정 `mgmt-draft-pick`으로 사용자에게 올렸다(`msg_ab9cc888a9f9`). 서버는 사용자 선택 알림까지 켜 둔다.
- **T2 발행(신규 `claude-opus-5-5`):** 2026-10-08 13:27 KST, pane `term_8fbdcf72…`(리드 pane split, 최초 실행 명령 `claude --model claude-opus-5-5`, 화면 표시 「Opus 5.5 with xhigh effort」, 선택창 없음, backend unknown), Task `task_3de8543ca4a8`, Dispatch `ctx_674c68f33776`, `input_accepted`·`turn_started`. spec E/`contracts/t2-spec.txt`, 발행 기록 E/`contracts/t2-issue.txt`(HEAD `676cb097`, 작업 트리 깨끗함), 요약 E/`t2-launch/summary.md`. 메인에 알렸다(`msg_32abaa521ade`).
- **리드 실수(T2 소유 경계, 첫 발생):** T2 계약은 「이 작업 동안 리드는 사이트 폴더를 쓰지 않는다」인데, 리드가 2026-10-08 06:55Z 전후 사이트 `images/`에 새 WebP 사본 22개를 썼다(E/`tools/encode-site-images.mjs`). T2 화면의 git 상태에서 미추적 22개를 보고 06:57Z에 근거 폴더 E/`tools/site-encode/hold-images/`로 옮겨 사이트 폴더를 HEAD 상태로 되돌렸다(SHA256 E/`tools/site-encode/hold-images.sha256`). 파일 시각으로 잰 실제 구간은 06:55:2x~06:57:19Z다. 당시 「그 사이 T2는 fixture selftest 단계였다」고 적었으나 틀렸다. T2 정산의 원시 시각(E/`t2/raw/commands.txt` 47·49행)으로는 T2의 빨간 단계 렌더 `check-design`이 06:54:42~06:57:59Z에 지금 사이트를 대상으로 돌아 구간이 겹쳤다. 결과 영향은 없다고 판단한다. `check-design`과 그 하위 파일에 폴더 나열 호출이 없어 페이지가 부르는 자원만 받는다(리드 grep). 실수 전인 06:23Z 실행(E/`t2/raw/superseded/red-check-design.json`)과 겹친 실행의 진단 23건이 같고, 전송량도 두 실행 모두 623,729 B·요청 9개다(리드 node 대조). 정적 `check-site` 빨간 단계는 06:23Z에 끝나 겹치지 않았다. 교정은 이 goal에 기록만 한다(첫 발생). 새 사본은 T2를 닫은 뒤 다시 넣는다.
- **사용자 휴식 중 대기 규칙(메인 전달 `msg_ab9cc888a9f9`, 사용자 원문 메인 창 제출):** 「OK, 지금 너무 피곤해서, 조금만 더 자다 올게, 모든 리드급 작업 끝나고 대기상태 들어갔는데 10분 넘게 메인에 사용자 응답이 없으면 모든 Orca 메세지 대기 풀어줘.」 T2가 끝나 사용자 답만 남으면 리드가 「사용자 대기 진입」을 status로 보낸다. 메인의 「우편함 대기 해제」를 받으면 대기를 끝내고 메인 터미널 안내까지 다시 열지 않는다. pane과 임시 서버는 그대로 둔다.

### 선행 시험 결과(T2)

- `worker_done` `msg_ccbf8b38e574`(2026-10-08T07:04:37Z, outcome succeeded). 발신 pane·Task·Dispatch가 발행 기록과 일치했다(E/`t2-launch/split.json`, `worker-start.json`). 보고 E/`t2/report.md`(64,426 B)는 리드가 끝까지 직접 읽었다. 메모 E/`t2/test-writer-context.md`.
- 산출물: E/`harness-v2/`. 정적 `check-site.mjs`에 새 검사 6개(CSP, 정지 수단, 본문 표식, 글꼴 출처, 글꼴 글자 포함, 움직이는 그림)를 더했다. 렌더는 새 진입점 `check-design.mjs`(Playwright, 127.0.0.1 임시 서버, 4폭, Edge·Firefox·WebKit, Lighthouse)다. 첫 판 `check-render.mjs`는 v2에서 지웠다.
- 빨간 단계(HEAD `676cb097` 첫 판 사이트): `check-site --mode source` exit 1·위반 5(CSP 없음, 정지 수단 없음, 공개 글꼴 없음, 글꼴 출처 표 없음, `@font-face` 없음). `check-design` exit 1·위반 21·warning 2(대비, 본문 규격, 시스템 글꼴, 정지 수단, 픽셀 선명도, Lighthouse 성능 87·접근성 95).
- selftest exit 0(사례 112개 중 110 통과, 온라인 2개는 `--online` 실행에서 통과). 계약 2a~2h·3-1~3-11마다 그 검사 하나만 어기는 위반 fixture가 있다. 변형 7개를 모두 selftest가 exit 1로 잡았다.
- 제품 결함 1: 첫 판 로드맵 「개발 중」 배지 대비 1.3:1(`styles.css:241` `.card p`가 `:182` `.badge-progress` 색을 덮음). 디자인 개정에서 배지를 다시 만들 때 같은 우선순위 구조를 피한다(완성 세션 계약에 전달).
- 다음 단계 참고(보고 원문): 아이콘 선언을 지우면 `/favicon.ico` 404로 Lighthouse 권장사항 warning이 생긴다. 접점 v1.3이 `images/icon.png` 선언을 이미 요구한다.
- 작성자 해석 20개는 보고 「작성자 판단으로 정한 해석」에 있고 기본값대로 받아들인다. 한계: Chrome·Linux 경로와 실제 CI 산출물은 돌리지 않았다(V3에서 본다).
- 정산: `worker-release` 결과 `retained`. pane 대기(✳, tui-idle satisfied)를 확인하고 `terminal close`로 닫았다. 닫은 뒤 리드 pane이 살아 있다(E/`t2-close/`).

### 사이트 그림·글꼴 사본과 README 표(리드)

- **원천 조사(Claude 읽기 전용 서브에이전트 + 리드 표본 대조):** 크기 비율 E/`design/scale-research.md`, 최신 그림 E/`design/latest-art-research.md`, 동작 시점 E/`design/timing-research.md`. 시안의 크기 비율 버그는 PPU가 시트마다 다른데 모든 띠를 같은 0.5배로 줄인 데서 왔다. 로고는 지금 메뉴 장면의 `Dawnholder_MainTitle_Banner_keyed.png`(16칸 12fps 반복)로 바꿨다. 캐릭터 그림은 최신판이 맞다(사용자 답 `msg_978398a3a7a3`).
- **사본:** 리드 스크립트 E/`tools/make-site-images-v2.ps1`(staging PNG)과 E/`tools/encode-site-images.mjs`(cwebp 1.6.0)로 WebP 21개와 `icon.png`를 만들었다. 게임 1단위 = 사본 80px로 맞춰 모든 사본의 사본 1px당 게임 단위가 0.0125 근처다. 22개 2,350,741 B이고 목록은 E/`tools/site-encode/site-images-manifest.json`이다. T2 경계 실수로 보류 폴더에 옮겼던 파일을 T2 정산 뒤 SHA256 대조(22/22 OK)를 거쳐 사이트 `images/`에 다시 넣었다.
- **글꼴:** Pretendard 1.3.9 공식 부분 집합 woff2 두 개와 `LICENSE.txt`를 바꾸지 않고 `fonts/`에 넣었다. 원본과 SHA256이 같다. `LICENSE.txt`에 CR이 없어 `*.txt eol=lf` 정규화로 바뀌지 않는다.
- **README 표:** 「그림 출처」에 22행을 더했고(첫 판 6행은 새 페이지가 쓰지 않게 되면 뺀다), 「글꼴 출처」 3행, 「동작 배율과 기준점」 20행, 「동작 맞물림」 17행(기사 대시·마법사 낙뢰·마법사 순간이동)을 새로 썼다. 맞물림 시각과 칸은 E/`design/timing-research.md`의 원천 값에서 계산했다. 순간이동 재사용 대기 30틱(`Constants.cs:76`)에 맞춰 돌아오는 순간이동은 1500ms에 둔다. 표의 칸 계산 규칙·효과 방향 칸은 접점 v1.3 3절에 정했다.
- **공개 목록:** `publish-files.txt`에 그림 22개와 글꼴 셋을 더했다(34줄). 첫 판 그림 6개는 아직 남긴다. `script.js`는 완성 세션이 파일을 만든 뒤 리드가 더한다.
- **리드 자체 점검(독립 판정 아님):** harness v2 `check-site --mode source --font-package …`는 exit 1, 위반 셋(CSP 없음, 정지 수단 없음, `@font-face` 없음)이다. 셋 다 완성 세션이 쓸 `index.html`·`styles.css`의 몫이다. 그림 출처·글꼴 출처·공개 목록·내부 정보·그림 합계를 포함한 나머지 13개 검사는 통과했다(E/`lead-run/assets-v2/check-site-1.json`).
- **commit:** `0054fe59`(push 완료). 그림·글꼴 blob의 SHA256이 원본과 같다(리드 `git cat-file` 대조 4건).
- **운영 기록(리드 부작용, 첫 발생):** 2026-10-08 07:1xZ에 Chrome 설치 판을 보려고 `chrome.exe --version`을 실행했다. Windows에서는 판을 출력하지 않고 이미 열린 사용자 Chrome 세션으로 넘겨져(「기존 브라우저 세션에서 여는 중입니다」) 사용자 Chrome에 새 창이나 탭이 열렸을 수 있다. 사용자 창을 잘못 닫을 수 있어 닫지 않았다. 그 직후 파일 속성 읽기(PowerShell)와 `date -u`가 auto mode 분류기에 거부됐고 우회하지 않았다. 메인에 보고했다(`msg_d12af9df1a1e`). T3·F 계약에 「Chrome 실행 파일 직접 실행 금지, Playwright와 임시 profile로만」을 적었다.

### E2E 시험 T3과 완성 세션 F 발행

- **접점 v1.3:** E/`site-interface-v1.3.md`(SHA256 `fe4e3b9b…2c91`). v1.2에 동작 그림·장면·붙는 곳·동작 실행 표식, README 「동작 배율과 기준점」·「동작 맞물림」 표, 화면 폭 1536×864, E2E 정의(동작 재생·크기 비율·동작 맞물림·실제 Chrome 재현·빈 띠)를 더했다.
- **T3(신규 `claude-opus-5-5`, 태그 `[Management 검증자]`):** 2026-10-08 16:17 KST, pane `term_312e1a08…`(리드 pane split, 최초 실행 명령 `claude --model claude-opus-5-5`, 화면 표시 「Opus 5.5 with xhigh effort」, 선택창 없음, backend unknown), Task `task_30e21975778a`, Dispatch `ctx_5c65fd855ef2`, `input_accepted`·`turn_started`. 계약 E/`contracts/t3-task.md`(SHA256 `222d7acf…c36f`), 발행 기록 E/`contracts/t3-issue.txt`(HEAD `0054fe59`), 요약 E/`t3-launch/summary.md`.
- **F(신규 `claude-opus-5-5`, 구현 역할 태그 `[Management Sol]`):** 2026-10-08 16:17 KST, pane `term_40086209…`(같은 방식, 화면 표시 「Opus 5.5 with xhigh effort」, 선택창 없음, backend unknown), Task `task_aa27bccd465e`, Dispatch `ctx_cd721f44ed4f`, `input_accepted`·`turn_started`. 계약 E/`contracts/f-task.md`(SHA256 `1b6331b2…78b8`), 발행 기록 E/`contracts/f-issue.txt`, 요약 E/`f-launch/summary.md`. 메인이 받아 둔 일정(17:00 KST 발행)보다 앞당겼다.
- 세션 수: 시안 셋, T2, T3, F까지 6개를 열었다. 남은 상한은 비평 하나와 V3 하나다.
- **F 질문과 접점 보충 1(리드 답, 범위 안 해석):** F가 사이트 파일 쓰기 전에 세 가지를 물었다(`msg_2a462b01ab18`). 리드 답 `msg_090ea40e7769`을 E/`site-interface-v1.3-addendum-1.md`(SHA256 `bf4612cf…691c`)로 고정하고 T3에도 알렸다(`msg_472d3e1084c2`). (1) 좁은 화면에서 띠 요소마다 0.5배로 줄이자는 기본값은 받지 않았다. 홀수 칸 폭 띠의 칸 경계가 반 픽셀에 걸려 옆 칸이 비치기 때문이다(사용자 버그 1과 같은 종류). 띠 상자는 칸 × 정수 배율로 두고 장면을 감싼 요소 하나에 축소 transform을 건다. (2) 정지 수단·움직임 줄이기에서 `data-action`은 고른 표시만 바꾸고 동작 시간표를 재생하지 않는다. (3) 순간이동 떠난 자리 효과에는 `data-anchor-to`를 두지 않는다. 캐릭터는 요소 하나이고 띠가 바뀌면 `data-sprite`도 함께 바꾼다.
