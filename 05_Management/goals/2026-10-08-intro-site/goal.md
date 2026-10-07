# 게임 소개 페이지(고객용)

## 진척 단계

- [x] 범위 승인과 goal 고정
- [>] 공개 안전 선행 시험
- [ ] 웹용 그림 사본 만들기
- [ ] 페이지 본문 작성
- [ ] 배포 workflow 구현
- [ ] 독립 검증
- [ ] 제품 PR 병합
- [ ] 배포 확인
- [ ] Gardener 점검
- [ ] 종료 기록 PR 병합

PR 번호가 생기면 「제품 PR 병합」 같은 단계 이름을 「PR000 병합」 형식으로 바꾼다.

## 재개 지점

**기록 시점: 2026-10-08 04:3x KST, goal 고정 직후.** 이 문단은 그 시점의 상태다. 그 뒤의 실제 진행은 「진척 단계」, 「결과와 열린 사항」, 그리고 리드가 단계마다 다시 쓰는 이 문단을 따른다.

- **지금 단계:** 범위 초안 v2가 승인됐고(아래 「요청 원천과 승인」) 이 goal을 고정했다. branch `feat/intro-site-20261008`을 최신 main `7086d45b`에서 만들었다. 다음은 신규 `claude-opus-5-5` 선행 시험 작성자의 공개 안전·문구·렌더 harness다.
- **작업 경로:** `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`.
- **Run과 재진입:** Run은 `run_605925c36641`, 리드 handle은 `term_59adfc07-455c-493b-b4bc-1e2583b08ff9`(이 세션의 관측값이며 다음 리드의 실행 권한이 아님)다. 다시 열면 새 handle로 run-use하고 메인에 알린 뒤 우편함 대기를 하나만 연다.
- **작업자·검증자:** 아직 없다.
- **근거 폴더 E:** 저장소 로컬 `.backups/verification/2026-10-08-intro-site/`(Git 제외). 범위 단계 근거는 `.backups/verification/2026-10-08-intro-page-scope/`(E0)다. 리드 맥락 메모는 E/`astra-context.md`다.

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

## 적용 중인 메인 결정

메인 `msg_8a1a882241d3`의 판단(초안 9절 답)이다. 사용자 결정이 아니다.

1. 고객 문장의 원천은 게임 화면에 실제로 보이는 문자열과 게임 데이터다. 근거표에 `파일:행`을 적는다. 코드 열거값·주석은 화면 문자열과 대조하는 보조 근거로만 쓴다. 화면 이름이 없으면 일반 명칭으로 쓴다.
2. README의 낡은 두 문장 정정은 이 PR에서 뺀다. 아래 「후속 후보」에 Rules 후속으로 적는다.
3. 그림 사본 생성 스크립트는 근거 폴더 E에 두고, 사본과 출처 표만 commit한다.
4. v1 판단 중 사이트 폴더 `00_Document/intro-site/`(Management 소유), 문서 지도 한 행(이 PR, Rules에 미리 알림), harness 근거 폴더, R-7 비해당, 새 workflow의 Rules·CodeMap 통지는 그대로다.

같은 메시지의 운영 지시: 우편함 대기는 하나만 연다. 사용자 결정이 필요한 범위 문제가 나오면 멈추고 status로 올린다. workflow 파일 push가 자격 증명으로 막히면 상태를 보존해 올린다. 확인 창이나 auto mode 분류기에 막히면 우회하지 않고 메인에 올린다. 병합 승인 요청에는 PR 번호·정확한 head·검증 판정 원문 경로·Pages 켜기 안내를 넣는다.

## 만들 것

1. **소개 페이지 한 장** `00_Document/intro-site/index.html`과 `styles.css`. 정적 HTML·CSS만 쓴다. 빌드 도구·npm 의존성·외부 폰트·CDN·분석 스크립트는 쓰지 않는다. JavaScript가 꺼져 있어도 모든 내용이 보인다. 절 구성은 초안 2절 1)의 표다.
   - 첫 화면: 게임 이름, 한 줄 소개, 「개발 중인 프로토타입」 표시, 페이지 안 이동 버튼. 배경은 인게임 배경 그림이다. 배너 그림은 쓰지 않는다.
   - 이야기: 마을 주민의 대사와 퀘스트 「마을의 위협」을 바탕으로 두세 줄. 마을 가게 주인들의 화면 대사도 같은 원천 범주라 쓸 수 있다.
   - 플레이 특징, 직업(기사·마법사), 모험의 흐름(마을 → 사냥터 → 보스 → 결과 화면 → 마을, 슬라임·골렘·보스), 앞으로(개발 중·계획 배지와 「계획은 바뀔 수 있다」 한 줄), 개발 소식(README 「일정」의 두 사실, 공개 빌드 없음), 맨 아래(크레딧·소스 코드 링크·AI 생성 그림 표기·all rights reserved·기준 날짜와 commit).
   - 문장 규칙: 기술 용어를 본문에 쓰지 않는다(맨 아래 소스 코드 링크 줄만 예외). 과장 광고와 다른 게임 비교를 쓰지 않는다. 측정하지 않은 품질을 실적처럼 쓰지 않는다. 보스 고유 이름과 「Dawnholder」 뜻풀이는 원천에 없어 쓰지 않는다.
2. **웹용 그림 사본** `00_Document/intro-site/images/`. 원본은 README 그림(`00_Document/assets/`)과 적 idle 시트(`03_Client/Assets/Art/Enemy/`)다. 첫 칸 자르기와 축소만 한다. 생성 스크립트는 E/`tools/`에 둔다(메인 결정 3).
3. **공개 허용 목록** `00_Document/intro-site/publish-files.txt`. 배포할 파일의 사이트 폴더 기준 상대 경로를 한 줄에 하나씩 적는다. workflow와 harness가 같은 목록을 읽는다.
4. **사이트 안내** `00_Document/intro-site/README.md`. 로컬 미리보기, 허용 목록, 공개 금지 정보, 그림 출처 표(사본·원본 경로·원본 SHA256·자른 영역·사본 크기·사본 SHA256), 사실 근거표(문장·원천 `파일:행`·기준 commit)를 둔다. 허용 목록 밖이라 배포되지 않는다.
5. **배포 workflow** `.github/workflows/intro-site.yml`. 조립 job은 허용 목록의 파일만 산출물에 넣고 `contents: read`만 가진다. 배포 job만 `pages: write`·`id-token: write`를 가지며 main push와 수동 실행에서만 돈다. PR에서는 조립까지만 돈다.
6. **진입 링크와 기록:** README 페이지 주소 한 줄, `00_Document/INDEX.md` 사이트 원본 한 행(Rules에 미리 알림), 이 goal, CURRENT Management 행.

## 건드릴 곳과 소유권

| 소유자 | 파일과 책임 |
|---|---|
| Management 리드 | 이 goal, `00_Document/operations/CURRENT.md`의 Management 행과 경로 줄, `00_Document/intro-site/`(index.html·styles.css·images/·README.md·publish-files.txt), README 링크 한 줄, `00_Document/INDEX.md` 한 행, E/`tools/`의 그림 스크립트, 위임 계약·근거·Git. 근거: AGENTS 「모델 라우팅」의 「보고서 자료의 … 본문·HTML·전용 생성 스크립트는 Astra가 작성한다」 |
| 신규 Sol(`gpt-6.1-sol` max) | `.github/workflows/intro-site.yml` 하나 |
| 신규 선행 시험 작성자(`claude-opus-5-5`) | E/`harness/`의 공개 안전·문구·그림 출처·링크·렌더 점검 harness |
| 신규 독립 검증자(`claude-opus-5-5`) | E/`verify/`의 시험·판정 파일만 |

리드와 Sol의 파일이 겹치지 않아 본문 작성과 workflow 구현은 병렬로 맡긴다. 작업자 한 명의 쓰기 종료·정산 뒤 다음 소유자에게 넘긴다. Sol·시험 작성자·검증자의 commit/push는 금지다.

## 하지 않을 것

- 기술 설명 절: 서버 구조, 틱·TPS, 통신 규격·PDL, CI, 개발 운영 방식, 기술 스택, 구조 도식.
- 다운로드·플레이 버튼, 게임 빌드 배포, 공개 빌드가 있는 것처럼 읽히는 문구.
- 원천에 없는 세계관·설정·이름. 「Dawnholder」 뜻풀이와 보스 고유 이름도 포함한다.
- 「개발 중」·「계획」 콘텐츠를 「지금 게임에 있는 것」으로 쓰기, 출시일·완성 시점 약속.
- 원본 그림·`.meta` 수정, 유료 에셋 폴더(`Art/Environment/Others/Cainos/`) 그림, 콘셉트 그림의 배경 제거 가공, 배너 사용.
- 저장소 설정 변경: Pages 켜기(사용자 손), environment·ruleset 변경, 사용자 지정 도메인.
- 분석·추적 스크립트, 외부 폰트·CDN, 문의 폼, 댓글, 소식 구독. 영어판, 여러 페이지.
- 기존 workflow 4개 수정, 공개 안전 검사의 CI 편입(후속 후보).
- README의 낡은 운영 문장 두 개 정정(메인 결정 2).
- 서버·Unity·운영툴 코드, BACKLOG·ORCA·AGENTS·`.agents`·Codex 설정.
- 내부 운영 정보와 개인 정보 게재: Orca handle·Run·msg ID, 로컬 절대 경로, 이메일·연락처, 토큰, 내부 PR·branch 진행 기록. 크레딧의 팀장 이름은 README 6행에 이미 공개된 범위만 쓴다.
- 새 도구 설치(필요하면 의존성 승인으로 멈춘다), 다음 goal 자동 착수.

## 관찰 가능한 완료조건

1. 사이트 원본·웹용 그림 사본·허용 목록·workflow·README 링크·INDEX 행이 main에 병합돼 있다.
2. 페이지의 모든 사실 문장이 근거표(원천 경로·`파일:행`·기준 commit)와 연결된다. 독립 검증자 대조에서 불일치가 0건이다. 「지금 게임에 있는 것·개발 중·계획」 구분이 PRD·FEATURE_MAP 원문과 같다. 원천에 없는 설정 문장이 0건이다.
3. 산출물 본문에서 기술 용어 목록의 등장이 0건이다. 맨 아래 소스 코드 링크 줄만 예외다. 근거는 harness 원시 출력이다.
4. 조립 산출물의 파일 목록이 `publish-files.txt`와 정확히 같다. 산출물 텍스트에서 내부 운영 정보 패턴(`term_`·`run_`·`msg_`, Windows·WSL 로컬 경로, 이메일, 토큰 접두)이 0건이다. 근거는 PR CI 조립 산출물에 돌린 harness 원시 출력이다.
5. 모든 그림 사본이 사이트 README 출처 표의 원본과 SHA256으로 연결되고, 원본 그림·`.meta`의 변경이 0건이다. 그림 합계 크기를 harness가 원시 출력으로 기록한다. 목표는 3MB 이하다(목표값이며 측정값 아님).
6. 실제 브라우저 렌더에서 데스크톱 폭과 좁은 화면(375px 안팎)에 가로 스크롤이 없다. 모든 그림이 보이고 모든 링크가 응답한다. JavaScript를 꺼도 모든 절이 보인다. 근거는 검증자 실행 기록이다.
7. PR에서 새 workflow의 조립 job이 성공하고 배포 job은 돌지 않는다. 쓰기 권한이 배포 job에만 있다. 기존 workflow 4개가 이 PR에서 통과한다.
8. 병합과 Pages 켜기 뒤 main push run의 배포 job이 성공한다. `https://bass131.github.io/dawnholder-server/`가 페이지 제목을 반환하고, 배포본 파일 목록이 허용 목록과 같다. 이 항목은 병합 뒤 관찰이라 PR 통과와 따로 기록한다.

## 설계와 검증 경계

- **등급 강.** workflow가 새 토큰 권한(`pages: write`·`id-token: write`)과 공개 범위를 다루는 보안 경계다. 본문만 보면 문서 실사지만 같은 PR이라 「애매하면 강」을 따른다. 실제 줄 수는 PR 때 `git diff --numstat`로 남긴다.
- **모델.** 선행 시험 작성자와 독립 검증자는 각각 신규 `claude-opus-5-5`다(검증자는 보안 경계라 시범 규칙의 Opus). workflow 구현은 신규 `gpt-6.1-sol`(max)이다. R-7은 메인 결정 4대로 비해당이다.
- **TDD.** 선행 시험 작성자가 사이트 파일이 없는 상태에서 harness를 돌려 실패를 먼저 기록한다. 리드 본문·그림 사본과 Sol workflow가 끝나면 같은 harness로 통과를 본다.
- **공개 허용 목록 하나.** 배포 파일은 `publish-files.txt` 한 곳에서 정한다. workflow는 이 목록만 복사하고, harness는 목록과 사이트 폴더·조립 산출물을 대조한다([하네스 원칙](../../../00_Document/conventions/CODE_CONVENTION.md#하네스-원칙) 3번). 목록에 파일을 더하는 일도 PR과 병합 승인을 거친다.
- **실제 경로 실행.** 강 등급의 실제 경로는 PR CI의 조립 job 1회와 그 산출물(`github-pages` artifact)에 돌린 harness다. 배포 job은 병합 전에 실행할 수 없어 PR 판정에서는 「미실행·판정 보류」로 두고 완료조건 8에서 확인한다.
- **고객 문장의 사실 대조.** 검증자는 사이트 README 근거표의 원천을 직접 열어 문장마다 대조한다. 특히 「지금 게임에 있는 것」으로 쓴 항목이 PRD 「구현」 행과 게임 데이터에 실제로 있는지 본다.
- **그림.** 사본은 Windows PowerShell 5.1의 System.Drawing으로 만든다(scratchpad 시험 exit 0, E/`astra-context.md`). 확대는 하지 않는다. 사본에서 원본의 AI 생성 표식이 사라질 수 있어 페이지의 「그림 일부는 AI 생성 도구를 활용해 제작」 표기와 출처 표가 대신한다.
- **언어 규칙.** HTML·CSS·workflow YAML에는 CODE_CONVENTION 언어 절이 없다. 임시 기준은 REPORTING 「HTML 구성」과 기존 workflow 관례(최상위 `contents: read`, `persist-credentials: false`, `timeout-minutes`, 태그 고정)다. 정본으로 만들지 않는다.

## PR 경계와 종료

각 PR은 최신 main의 새 branch에서 만들고, 정확한 head의 CI·메인 R-2 뒤 사용자 개별 병합 승인을 받는다. 리드는 병합하지 않고 준비 보고만 한다.

| PR | 내용 | 등급 |
|---|---|---|
| 제품 PR | 이 goal, 사이트 폴더, workflow, README 링크, INDEX 행, CURRENT Management 행 | 강 |
| 종료 기록 PR | 배포 확인 결과, Gardener 결과, goal 결과 절 | 문서 실사 |

사용자 손은 Pages 켜기(Settings → Pages → Source를 「GitHub Actions」로)와 두 PR의 병합 승인이다. Pages 켜기는 제품 PR 병합 승인 직전이 좋다. 안 켠 채 병합하면 첫 main run의 배포 job이 실패한다. 공개 시점은 병합과 Pages 켜기가 둘 다 끝난 때다. 배포 확인에서 실패하면 수정 PR을 하나 더 두고 메인에 알린다. 점검 지점은 전체 goal 종료 한 번이다. 같은 산출물 수정이 3회를 넘으면 메인 체크포인트를 알린다. 다음 goal은 자동으로 시작하지 않는다.

## 후속 후보

- README의 낡은 운영 문장 두 개 정정: 메인 결정 2에 따라 Rules 후속이다. 초안 v1 위험 7(E0/`scope-draft-v1.md` 130행)이 짚은 문장은 README 14행(「현재는 Codex 메인 세션이 목표와 결정을 관리」)과 65행(`.agents/skills/`를 「Codex 스킬」로 부름)이다. AGENTS 현행은 「메인 Claude」다.
- 공개 안전 검사의 CI 편입: harness의 허용 목록·내부 정보 패턴·기술 용어 검사를 workflow 조립 단계에 넣는 일. 이번 범위 밖이다.
- 범위 밖 기존 위험(초안 9절 5, 이번에 고치지 않음): 유료 Asset Store 패키지 원본이 공개 저장소에 있다. 폰트 NOTICE가 임시 출처 표기 상태다. README 34행의 「ComfyUI 기반 AI 파이프라인」 표기와 일부 그림의 다른 생성 표식이 맞지 않는다.

## 결과와 열린 사항

아직 없다.
