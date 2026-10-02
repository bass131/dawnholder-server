# M-2 시스템 카드 디자인 명세 (결정용 초안)

## 결정 요청

사용자가 정할 것은 시스템 카드 화면의 테마 방향 하나와, 제목 픽셀 폰트를 지금 검토할지 여부다. 추천은 **A 퀘스트 게시판**과 **픽셀 폰트 보류**다. "테마 A 또는 B, 폰트 보류 또는 검토"처럼 두 낱말로 답하면 되고, 바꾸고 싶은 부분이 있으면 한 줄로 덧붙이면 된다.

| 안건 | 선택지 | 고르면 일어나는 일 |
|---|---|---|
| 테마 | **A 퀘스트 게시판 (추천)** | 지금의 어두운 운영 화면을 유지하고, 나무 게시판과 종이 의뢰서 카드를 장식 층으로 더한다. |
| | B 상점 장부 | 화면 전체가 밝은 장부 페이지로 바뀌고, 하위 시스템이 한 줄씩 대조되는 품목 행이 된다. |
| 제목 픽셀 폰트 | **보류 (추천)** | 시스템 폰트와 2px 픽셀 그림자로 구현하며, 새 폰트나 CSP(앱이 불러올 수 있는 자원을 제한하는 보안 정책) 변경이 생기지 않는다. |
| | 검토 | 폰트 파일을 받아 목업에 넣은 뒤 다시 보여 준다. 제품 도입은 그 뒤 별도 승인이다. |

A를 추천하는 이유는 세 가지다. 지금 화면의 결정을 가장 적게 바꾸고, "카드를 골라 들어간다"는 이번 목표의 탐색 방식과 그대로 맞고, 사용자가 앞서 말한 "게임 퀘스트 보드 느낌" 방향과 같다. 마지막 근거는 메인의 진단 메모에 적힌 전달 문구이며, 이 세션에서 사용자 원문으로 확인하지는 않았다. B는 하위 카드가 12개인 게임 서버처럼 항목이 많을 때 한 화면에서 비교하기 더 좋다. 폰트 보류를 추천하는 이유는 이번 목업에 픽셀 폰트가 실제로 들어가지 않아 사용자가 판단할 근거가 화면에 없기 때문이다.

## 산출물 상태

- 이 문서와 [목업](mockup.html)은 신규 디자인 작성자 세션 `[Management 검증자]`가 썼다. 요청 모델과 최초 실행 명령은 `claude-opus-5-5`, backend 실제 모델은 `unknown`이다.
- 디자인 목업은 작성을 마쳤고 작성자 자기 점검만 했다. 사용자 승인, 독립 디자인 검증, 제품 화면 검증은 모두 아직이다.
- 목업의 카드 본문과 분할은 샘플이다. 구현 완료나 사실 검증 완료를 뜻하지 않는다.
- R-12/D-09는 고치지 않았다. 10절의 문구는 결정 뒤 갱신할 제안이다.

## 1. 공통 데이터 층과 테마 층

### 1.1 테마가 바꾸지 않는 것

두 안은 같은 DOM, 같은 자료, 같은 화면 흐름을 쓴다. 목업에서 테마를 바꿔도 주소와 focus 위치가 그대로다. 테마 전환은 `html[data-theme]` 값 하나와 CSS만 바꾼다.

- **데이터 층**: 카드 제목·요약·사실 목록, 구현 문서 본문, 코드 매핑 표, 기존 기록 칩. 무늬·질감·그림자 없이 평평한 면에 둔다. 본문은 Segoe UI·맑은 고딕, 문서 본문 15px, 줄 간격 1.75다.
- **상태 의미**: 구현 상태 4종, 자료 상태(로딩·실패·손상·초과), 기준 commit과 자동 갱신 안 됨 표시. 모양·글자·테두리로 구분하고 색은 보조로만 쓴다.
- **흐름**: 1단 카드 → 2단 카드 → 구현 문서 → 상위·검색 복귀, 개발 기록·기록 편집 진입, 키 조작.
- **기존 화면 의미**: 서버 운영·유저 관리의 미수집·미연결·비활성 제어, 개발 기록 탐색, catalog 편집 초안 보존.

### 1.2 테마가 바꾸는 것

색 토큰, 제목 글꼴, 면을 감싸는 프레임, 장식 아이콘, 전환 효과, 하위 카드 목록의 배치(격자 또는 행)다. 장식은 정보 위계를 돕는 곳에만 둔다. 작은 글자나 노이즈 질감, 과한 그림자로 게임 느낌을 대신하지 않는다.

### 1.3 A 퀘스트 게시판

- **장면**: 밤의 길드 홀에 걸린 나무 게시판. 앱 바깥 크롬은 지금 R-12의 어두운 배경을 이어 간다.
- **카드**: 나무판 위에 핀으로 꽂은 종이 의뢰서다. 종이 면은 밝은 단색이고 글자는 어두운 잉크색이라 데이터 층이 가장 잘 읽힌다.
- **신호 절제**: 미완료·미연결·연결 실패처럼 주의가 필요한 카드에만 카드 위에 떠 있는 느낌표와 점선 안쪽 테두리를 단다. 정상 카드에는 신호가 없다.
- **움직임**: 카드에 마우스나 focus가 오면 2px 들리고 그림자가 2px 늘어난다. 핀은 3프레임으로 흔들린다. 둘 다 `steps()` 타이밍이라 픽셀 게임처럼 끊어 움직인다.
- **문서**: 핀 두 개로 고정한 넓은 종이 한 장이다. 종이 안은 평평하다.

### 1.4 B 상점 장부

- **장면**: 낮의 상점 카운터에 펼친 장부. 앱 전체가 밝은 크림색 페이지가 되고 사이드바만 호두나무색으로 남는다.
- **목록**: 상위 시스템은 장부 위쪽의 색인 탭, 하위 시스템은 품목 행이다. 한 행에 이름·ID·요약과 상태·매핑 수·기존 ID·문서 상태가 나란히 놓인다.
- **신호**: 상태는 이중 테두리 장부 도장으로 표시한다. 주의 행은 제목 앞에 ※를 단다.
- **움직임**: 행에 마우스나 focus가 오면 제목 아래 금색 밑줄이 왼쪽에서 그어진다. 화면 진입은 오른쪽에서 14px 미끄러지는 페이지 넘김이다.
- **문서**: 왼쪽 여백에 붉은 괘선 한 줄이 있는 장부 페이지다. 제목만 바탕(Batang) 계열 serif다.

### 1.5 레퍼런스 근거와 표현 의도

후보 넷 가운데 두 개의 공식 1차 출처를 확인했다. 개념만 빌렸고 실제 게임 아트·UI·폰트는 가져오지 않았다. 목업의 모든 그림은 문자 격자로 직접 적은 픽셀 SVG다.

| 안 | 레퍼런스 | 확인한 출처 | 빌린 표현 의도 |
|---|---|---|---|
| A | Stardew Valley의 Help Wanted 게시판 (ConcernedApe) | 공식 사이트 [stardewvalley.net](https://www.stardewvalley.net/)의 Wiki 링크가 가리키는 [Quests 문서](https://stardewvalleywiki.com/Quests) | 게시판은 Pierre's General Store 밖에 있다. 새 의뢰가 붙으면 게시판 위에 노란 느낌표가 뜨고, 받은 의뢰는 그날과 다음 날까지 유효하다. 여기서 "벽에 붙은 의뢰서를 훑어보고 하나를 집는다"는 탐색과 "알릴 것에만 느낌표"라는 신호 절제를 빌렸다. |
| B | Moonlighter (Digital Sun 개발, 11 bit studios 배급) | [Steam 공식 상점 페이지](https://store.steampowered.com/app/606150/Moonlighter/) | 낮에는 마을 Rynoka의 상점에서 물건을 진열하고 값을 신중히 정하고 금 보유량을 관리한다. 여기서 "진열·가격·재고를 한 줄에 대조하는 장부"라는 정리 감각을 빌려 하위 시스템을 품목 행으로, 상태를 장부 도장으로 옮겼다. |

Moonlighter 공식 사이트(`moonlighterthegame.com`)는 이 세션의 조회에서 DNS 오류가 나서 Steam 상점 페이지로 대신했다. Recettear와 Sea of Stars는 1차 출처 확인과 반영을 하지 않았다.

## 2. 화면 흐름

### 2.1 진입

개발 현황 안에 보기 탭 세 개를 둔다: **시스템 카드**(기본), **개발 기록**(기존 탐색), **기록 편집**(기존 catalog 편집). 페이지 제목·설명·"관리 기능 미연결" 안내 문구는 지금 App.tsx의 문장을 그대로 쓴다. 시스템 카드 탭 옆에 검색 입력이 붙는다.

### 2.2 1단 → 2단 → 구현 문서

| 단계 | 보이는 것 | 고르면 |
|---|---|---|
| 1단 전체 시스템 | 상위 7개 카드: 이름, 저장소 경로, 요약, 하위 수, 하위 상태 집계, 연결된 기존 ID 수. 자료 정보는 한 줄 요약으로 접혀 있다. | 그 시스템의 2단 목록으로 간다. |
| 2단 하위 시스템 | 상위 시스템 머리말(경로·요약·진입점·하위 상태·기존 기록 ID 접힘 목록)과 하위 카드: 이름, 하위 ID, 요약, 상태, 코드 매핑 수, 기존 ID, 문서 상태. 위에 상위 7개 바로가기 줄이 있다. | 그 하위 카드의 구현 문서로 간다. |
| 구현 문서 | 머리: 상위 이름 › 하위 ID, 제목, 한 줄 설명, 상태, 기준 commit(전체 SHA 펼침), 자동 갱신 안 됨, 목업 표시. 몸: 목차와 절(책임, 입력→처리→출력, 상태·수명주기, 구현 경계, 주요 코드 위치, 검증 범위·한계, 기존 기록·정본 출처). 끝: 같은 상위의 이전·다음 하위 카드. | 목차는 해당 절로 focus를 옮긴다. 기존 기록 칩은 개발 기록 탭의 그 시스템으로 간다. |

미구현·미연결 하위 카드도 문서에 도달한다. 그 문서는 "현재 상태"와 "없는 코드"를 먼저 설명하고, 코드가 없는 부분의 매핑은 비워 둔다. 가짜 경로는 만들지 않는다.

### 2.3 복귀와 focus

| 조작 | 결과 | focus |
|---|---|---|
| 카드·결과 선택 | 한 단계 들어간다. | 새 화면의 제목 |
| `Esc` 또는 "↑ 상위" 버튼 | 문서 → 2단 → 1단으로 한 단계 올라간다. 검색에서 들어왔으면 검색어를 유지한 채 올라간다. | 방금 나온 카드의 링크 |
| "← '검색어' 검색 결과로" | 검색 결과로 돌아간다. | 방금 열었던 결과 링크 |
| 목차 링크 | 같은 문서 안의 절로 스크롤한다. 주소는 바뀌지만 기록은 늘지 않는다. | 해당 절 제목 |
| `/` | 입력 중이 아닐 때 검색 입력으로 간다. | 검색 입력 |
| 브라우저 뒤로 | 목업은 해시 주소라 뒤로가기로도 같은 흐름이 된다. 제품 Electron 창에는 기본 뒤로가기 키가 없으므로 앱 안 버튼과 `Esc`를 주 경로로 둔다. | 위와 같음 |

`Esc`는 입력 필드에 글자가 있으면 입력 지우기에 쓰이고, 빈 검색 입력에서만 상위 이동으로 동작한다. 1280×720에서 2단·문서·검색 화면은 페이지 제목 줄을 한 줄로 줄여 본문이 첫 화면에 더 많이 들어오게 한다.

### 2.4 검색

카드 이름, 하위 ID, 코드 경로와 역할, 기존 기록 ID, 문서 본문을 대상으로 공백으로 나눈 낱말을 모두 포함하는 항목을 찾는다. 결과는 상위 시스템·하위 시스템·문서 본문 세 묶음으로 나누고, 일치 부분을 강조한다. 결과 수는 `role="status"`로 읽어 준다. 결과가 없으면 찾은 범위, 다른 낱말 예시, 개발 기록에서 같은 검색어로 찾는 링크, 전체 시스템으로 돌아가는 버튼을 보여 준다.

### 2.5 개발 기록과 기록 편집

- 구현 문서의 기존 기록 칩은 개발 기록 탭에서 그 시스템을 선택한 상태로 연다. 개발 기록의 시스템 상세에는 반대 방향 링크 "이 기록과 연결된 시스템 카드"를 둔다. 다대다 연결을 양쪽에서 따라갈 수 있다.
- 문서 끝에 "개발 기록에서 보기"와 "기록 편집 열기"를 둔다. 시스템 카드 자료는 앱에서 편집하지 않는다는 문장을 함께 둔다.
- 편집 초안이 있으면 툴바에 "미저장 초안 있음" 배지가, 기록 편집 탭에 "· 미저장 초안"이 붙는다. 카드 탐색·탭 이동·테마 전환은 초안을 지우지 않는다.
- 저장·취소·검증 버튼에 위계를 준다. 저장은 주 버튼, 검증은 보통 버튼, 초안 버리기는 위험 버튼이다. 지금 화면은 세 버튼 모양이 같다.
- 목업의 저장은 흉내만 내고 파일을 쓰지 않는다. 기존 불러오기 2 MiB 제한, 초안이 있을 때 다른 파일 불러오기 차단 문구는 기존 화면대로 보존했다.

### 2.6 서버 운영·유저 관리

사이드바 세 메뉴의 순서와 의미, 미수집 표시(—), 빈 로그·빈 유저 표, 비활성 서버 시작·종료 버튼을 그대로 둔다. 테마는 색과 프레임만 입힌다. 운영 수치나 성공 결과를 만들지 않는다.

## 3. 토큰

### 3.1 색

| 토큰 | 역할 | A 게시판 | B 장부 |
|---|---|---|---|
| `--app-bg` / `--ws-bg` | 앱 바탕 / 작업 영역 | `#0e1118` / `#121620` | `#e9dfc9` / `#ece3cf` |
| `--text` / `--muted` | 크롬 글자 / 보조 | `#e9ebf0` / `#adb5c3` | `#2a231b` / `#5f5241` |
| `--link` | 크롬 링크 | `#ffd58a` | `#155c57` |
| `--side-bg` / `--side-text` | 사이드바 | `#1b140e` / `#efe3cc` | `#3b2a1e` / `#f5ecda` |
| `--paper` / `--paper-ink` / `--paper-muted` | 카드 면 | `#f2e7cc` / `#2a2118` / `#5d4e3b` | 투명(페이지 위) / `#2a231b` / `#5f5241` |
| `--doc-bg` / `--doc-ink` / `--doc-link` | 문서 면 | `#f5eedd` / `#211a12` / `#7a3d12` | `#fffaf0` / `#211b14` / `#155c57` |
| `--accent` / `--accent-ink` | 주 버튼 | `#f0b44c` / `#1d1406` | `#1f6b66` / `#ffffff` |
| `--st-present` | 코드 있음 | `#2a6a37` | `#1f6b3a` |
| `--st-partial` | 일부만 있음 | `#7d5200` | `#7d5200` |
| `--st-absent` | 미완료 | `#8e2f28` | `#9b2c22` |
| `--st-disconnected` | 미연결 | `#4a5262` | `#4f5560` |
| `--warn-bg` / `--warn-ink` | 경고 칩 | `#fff1c9` / `#5a3a00` | 같음 |
| `--focus` | focus 고리 | `#ffd66b` (종이 안은 `#0b57d0`) | `#0b57d0` (사이드바는 `#ffd66b`) |
| 프레임 | 장식 면 | 나무 `#4b3222`, 이음새 `#3e281b`, 틀 `#7c5434`, 테두리 `#24170d` | 페이지 `#fffaf0`, 겹장 `#f3e9d2`, 가장자리 `#cdb994`, 여백 괘선 `#d9998a`, 행 괘선 `#e4d7b9`, 금 `#b8862b` |
| 시스템 색 | 상위 7개 아이콘·탭 표식 (상태와 무관) | server `#6f9bd1`, client `#5fb27f`, client-net `#3fa7a7`, shared `#d39a3c`, tools `#a47fd0`, management `#d07560`, automation `#8e98a3` | 같음 |

### 3.2 대비

WCAG 2 대비비(글자와 배경의 상대 휘도 비율)를 목업 토큰으로 계산했다. 본문 글자는 모두 4.5:1 이상, focus 고리는 3:1 이상이다.

| 조합 | A | B |
|---|---|---|
| 크롬 본문 / 보조 | 15.16 / 8.76 | 12.15 / 5.94 |
| 크롬 링크 | 13.03 | 6.09 |
| 카드 글자 / 보조 | 12.85 / 6.52 | 14.90 / 7.29 |
| 문서 글자 / 보조 / 링크 | 14.87 / 7.26 / 7.25 | — / — / 7.47 |
| 상태 글자 4종 (상태 칩 바탕 위) | 6.05 · 6.32 · 7.52 · 7.27 | 6.26 · 6.56 · 7.27 · 7.21 |
| focus 고리 | 노랑/나무 8.48, 노랑/바탕 12.99, 파랑/문서 5.52 | 파랑/페이지 6.14, 파랑/바탕 5.00, 노랑/사이드바 9.82 |
| 사이드바 글자 / 보조 | 14.34 / 8.44 | 11.65 / 7.89 |
| 주 버튼 글자 | — | 6.26 |

### 3.3 글꼴과 크기

| 용도 | A | B | 크기 |
|---|---|---|---|
| 본문·카드·문서 | Segoe UI, 맑은 고딕 | 같음 | 카드 요약 13.5px, 문서 15px, 메타 12–12.5px |
| 코드·경로 | Consolas, D2Coding | 같음 | 표 13.5px 안의 0.9em |
| 화면 제목 | 시스템 sans 700 + `2px 2px 0 #000` 픽셀 그림자, 금빛 글자 | 바탕(Batang) 계열 serif 700 | 20px, 문서 제목 23px |
| 판 제목(장식) | 같은 sans, 자간 0.08em | serif, 아래 2px 줄 | 13–14px |
| 픽셀 폰트 자리 | `--title-font` 하나만 바꾸면 된다. 지금은 비어 있다. | 같음 | — |

작은 글자로 분위기를 내지 않는다. 가장 작은 글자는 메타 정보 12px이다. 바탕 계열은 Windows 기본 글꼴이라 설치가 필요 없지만, 다른 OS에서는 대체 serif로 바뀐다.

### 3.4 간격

기준 단위는 4px이고 4·8·12·16·20·24·32를 쓴다. 목업에는 10·14·18·22·26·30px 같은 중간값이 남아 있다. 제품에서는 이 표의 단계로 맞춘다.

| 위치 | 값 |
|---|---|
| 작업 영역 안쪽 | 1280 폭 20·28px, 820 이하 18px, 560 이하 12px |
| 카드 격자 간격 (A) | 세로 30px(핀과 느낌표 자리), 가로 20px |
| 카드 안쪽 (A) | 위 20px, 좌우 16px, 아래 14px |
| 장부 행 (B) | 위아래 12px, 열 간격 16px |
| 문서 안쪽 | 24·30px, 820 이하 18–20px, 560 이하 12px |
| 문서 절 간격 | 제목 위 26px, 문단 아래 10px |

### 3.5 프레임

| 요소 | A | B |
|---|---|---|
| 카드 판 | 나무판 58px마다 3px 이음새, 6px 틀 + 안팎 3px 테두리, 위 가운데 이름판 | 페이지 1px 가장자리 + 겹장 두 장, 왼쪽 38px에 2px 여백 괘선, 위 2px 머리줄 |
| 카드 | 2px 테두리 + 4px 단단한 그림자, 위 가운데 핀(20px) | 행 아래 1px 괘선, 오른쪽 사실 열 왼쪽 1px 괘선 |
| 주의 카드 | 카드 위 느낌표(12×24px) + 안쪽 2px 점선 | 제목 앞 ※ |
| 상태 칩 | 2px 실선(코드 있음·일부)·파선(미완료)·점선(미연결) | 3px 이중선(코드 있음·일부)·파선·점선 |
| 문서 | 2px 테두리 + 5px 그림자, 위 양끝 핀 | 위 테두리 없음(탭과 이어짐), 왼쪽 여백 괘선 |
| 바로가기 줄 | 종이 꼬리표 + 단단한 그림자, 현재 항목 아래 4px 시스템 색 | 색인 탭, 현재 탭은 페이지와 이어지고 위 4px 시스템 색 |

### 3.6 움직임

| 효과 | 시간 | 곡선 | 동작 감소 |
|---|---|---|---|
| 화면 진입 | 200ms | ease-out; A 위로 8px, B 오른쪽에서 14px | 0ms, 이동 0 |
| 카드 들림 (A) | 120ms | `steps(2)` | 0ms, 이동 0 |
| 핀 흔들림·느낌표 뜀 (A) | 300·360ms, 1회 | `steps(3)` | 끔 |
| 금색 밑줄 (B) | 200ms | ease-out | 0ms |
| 로딩 자리표시 깜빡임 | 1.2s 반복 | `steps(4)` | 끔 |
| 문서 안 스크롤 | 부드러운 스크롤 | — | 즉시 이동 |

모든 시간은 `--motion` 배수(1 또는 0)를 곱한다. `prefers-reduced-motion: reduce`이면 `--motion`이 0이 되고 반복 효과는 꺼진다. 내용과 기능은 그대로다. 무한 반복 장식은 로딩 자리표시 하나뿐이다.

## 4. 상태 표시

### 4.1 구현 상태

| 상태 | 글자 | 아이콘 모양 | 테두리 | 쓰는 경우 |
|---|---|---|---|---|
| `present` | 코드 있음 | 체크 상자 | 실선(B 이중선) | 기준 commit에 매핑한 코드가 있다. 기능 완성이나 검증 통과를 뜻하지 않는다. |
| `partial` | 일부만 있음 | 반쯤 찬 상자 | 실선 | 일부 경로만 있다. 목업 자료에서는 쓰지 않았다. |
| `absent` | 미완료 | 파선 빈 상자 | 파선 | 설계·형식만 있고 구현이 없다. 예: 저장 경계. |
| `disconnected` | 미연결 | 끊긴 고리 | 점선 | 코드나 화면은 있으나 게임·서버와 연결되지 않았다. 예: DB 준비 도구, 서버 운영·유저 관리. |

1단 카드는 하위 상태를 "코드 있음 11 · 미완료 1"처럼 아이콘과 수로 집계한다.

### 4.2 자료 상태와 실패

| 상태 | 표시 | 기존 기능 영향 |
|---|---|---|
| 카드 자료 읽는 중 | 카드 자리표시와 "자료 하나를 끝까지 읽고 검증한 뒤에만 표시합니다", `aria-busy` | 없음 |
| 카드 자료 읽기 실패 | 경로와 오류, "이전 자료나 사본으로 조용히 바꿔 보여 주지 않습니다", 다시 읽기·개발 기록 열기 | 개발 기록·편집은 계속 쓴다. 초안 보존 문장을 함께 둔다. |
| 카드 자료 손상·검증 실패 | 중복 ID·부모 없음·문서 기준 불일치 같은 문제 목록, 일부만 보여 주지 않음 | 같음 |
| 카드 자료 크기 초과 | 실제 크기와 2 MiB 상한 | 같음 |
| 개발 기록 읽기 실패 | 개발 기록 탭은 기존 실패 문구, 카드의 기존 ID 칩은 "ID · 제목 확인 불가" | 카드·문서는 그대로 읽는다. |
| 기존 ID 연결 실패 | 문서 머리 경고 칩, 해당 칩은 굵은 파선 "연결 실패: ID — 개발 기록에 없는 ID", 2단 카드에 표시 | 다른 연결은 정상 |
| 코드 매핑 기준 불일치 | 문서 머리 경고, 매핑 표를 흐리게·취소선, "같은 버전의 매핑으로 표시하지 않습니다" | 문서 본문은 읽는다. |
| 주소의 ID 없음 | "하위 카드 ID를 찾을 수 없습니다: ID"와 상위로 가는 버튼 | 없음 |
| 검색 결과 없음 | 2.4절 | 없음 |

### 4.3 기준 commit과 자동 갱신

- 문서마다 기준 commit 7자리와 "전체 SHA 보기"를 둔다. 펼치면 40자리 SHA를 선택해 복사할 수 있다.
- "자동 갱신 안 됨 · 현재 HEAD와 비교하지 않음"을 모든 문서 머리에 둔다.
- 1단 자료 줄은 카드 자료와 개발 기록 자료의 경로·버전·기준일을 따로 적고, hash도 자료별로 따로 표시한다. 목업은 카드 자료 hash 자리에 예시 값을 두고 개발 기록 hash는 "따로 셈"이라는 문구만 두었다. 두 자료를 하나의 snapshot으로 보이지 않게 한다.

## 5. 접근성과 반응형

- **의미 구조**: 카드는 `article` 안 제목의 링크 하나를 카드 전체로 늘린다. 지금 화면처럼 `button` 안에 `h4`·`dl`을 넣는 콘텐츠 모델 위반을 피한다. 위치는 `nav aria-label="현재 위치"`의 순서 목록과 `aria-current="page"`로 나타낸다.
- **키보드**: 모든 탐색이 링크·버튼이다. 화면이 바뀌면 제목으로 focus를 옮기고, 올라오면 직전 카드로 돌려준다. 앱 맨 앞에 "본문으로 건너뛰기"가 있다. 단축키는 `/` 검색과 `Esc` 상위 두 개뿐이다.
- **focus 고리**: 3px 실선 + 2px 간격이다. 카드는 카드 바깥 4px에 고리를 그려 들림 효과와 겹치지 않게 한다.
- **읽어 주기**: 검색 결과 수, 기록 결과 수, 편집 알림은 `role="status"`, 편집 오류는 `role="alert"`다. 장식 SVG는 모두 `aria-hidden`이다.
- **한국어 줄바꿈**: 앱 전체에 `word-break: keep-all`과 `overflow-wrap: anywhere`를 준다. 경로는 `/` 뒤에 줄바꿈 기회를 넣고 고정폭 글꼴로 표시한다. 말줄임으로 자르지 않는다.
- **폭 단계**: 창 폭이 아니라 앱 컨테이너 폭으로 판단한다(`@container`).

| 앱 폭 | 바뀌는 것 |
|---|---|
| 1000px 이하 | 문서 목차가 본문 위 상자로 내려온다. 개발 기록 상세가 목록 아래로 간다. |
| 820px 이하 | 사이드바 150px, 툴바 보조 문구 숨김, 검색 입력 한 줄 전체, B의 행이 두 단(이름·요약 / 사실)으로 쌓인다. |
| 560px 이하 | 사이드바가 위쪽 가로 메뉴가 된다. 툴바는 고정하지 않는다(긴 위치 줄이 본문을 가리지 않게). 바로가기 줄은 아이콘만 보이고 이름은 화면 낭독기용으로 남긴다. 매핑 표는 행마다 이름표가 붙은 목록이 된다. |

## 6. 2단 카드 최종 분할 제안

goal의 7개 1단 범위를 모두 덮는 39개다. "전투·스킬·AI·물리"는 기존 기능 ID(movement·combat·skills·enemy-ai)와 코드 위치(`Maps/Systems/` 파일별)가 이미 나뉘어 있어 네 장으로 나눴다. 상태 발행은 기존 `packet-publication` ID가 따로 있어 월드 틱과 분리했다. 서버 회귀 테스트는 goal 표대로 개발·검증 도구에 둔다.

경로는 모두 저장소 루트 기준이며 `333fe20` tree에서 존재와 파일/디렉터리 종류를 확인했다(88개 경로, 불일치 0). 경로의 역할 설명 일부는 파일 이름 기준이며 본문을 대조하지 않았다고 목업에 적었다.

| 1단 | 2단 ID | 표시 | 상태 | 매핑 | 기존 ID |
|---|---|---|---|---|---|
| server | `server.network` | 서버 전송 계층 | 코드 있음 | 1 | transport |
| | `server.session` | 세션·핸들러 | 코드 있음 | 4 | connection, movement, combat, skills, map-entry, party |
| | `server.world-tick` | 월드 루프·맵 틱 | 코드 있음 | 3 | party, quest, enemy-ai |
| | `server.publication` | 상태 발행·스냅샷 | 코드 있음 | 1 | packet-publication, remote-rendering |
| | `server.movement` | 이동·물리 | 코드 있음 | 3 | movement |
| | `server.combat` | 전투·피해 | 코드 있음 | 4 | combat, character-state |
| | `server.skills` | 스킬·행동 조건 | 코드 있음 | 5 | skills |
| | `server.enemy-ai` | 적·보스·리스폰 | 코드 있음 | 4 | enemy-ai, character-state |
| | `server.map-transition` | 맵 이동 | 코드 있음 | 4 | map-entry |
| | `server.party` | 파티 | 코드 있음 | 2 | party |
| | `server.quest` | 처치 진행·보스 해금 | 코드 있음 | 1 | quest |
| | `server.persistence` | 저장 경계 | 미완료 | 1 | persistence, character-state |
| client | `client.bootstrap` | 씬·초기화 | 코드 있음 | 2 | map-entry, connection |
| | `client.network` | 네트워크 수명·수신 적용 | 코드 있음 | 4 | connection, map-entry, transport |
| | `client.prediction` | 입력·예측·재조정 | 코드 있음 | 2 | movement, skills |
| | `client.state` | 상태·보간 | 코드 있음 | 2 | remote-rendering, party, quest |
| | `client.combat-view` | 전투 표현 | 코드 있음 | 1 | combat, enemy-ai, remote-rendering |
| | `client.ui` | UI | 코드 있음 | 1 | party, quest, character-state |
| | `client.audio` | 음향 | 코드 있음 | 1 | 없음 |
| client-net | `client-net.connect` | 연결 시도 | 코드 있음 | 2 | transport, connection |
| | `client-net.session` | 세션·송수신·프레임 | 코드 있음 | 4 | transport |
| | `client-net.distribution` | 배포·소비 경계 | 코드 있음 | 2 | transport, engineering |
| shared | `shared.protocol` | 프로토콜·버전 | 코드 있음 | 4 | protocol, connection |
| | `shared.gamedata` | 게임 데이터·공식 | 코드 있음 | 1 | character-state, combat, movement, map-entry |
| | `shared.distribution` | DLL 소비 경계 | 코드 있음 | 2 | protocol, engineering |
| tools | `tools.packetgen` | 패킷 생성기 | 코드 있음 | 1 | protocol |
| | `tools.bot` | headless-bot 시나리오 | 코드 있음 | 2 | engineering |
| | `tools.server-tests` | 서버 회귀 테스트 | 코드 있음 | 1 | engineering |
| | `tools.wsl` | WSL 실행 | 코드 있음 | 1 | engineering |
| | `tools.formatting` | 서식 검사 | 코드 있음 | 3 | engineering |
| | `tools.bgm` | 음원 생성 | 코드 있음 | 1 | 없음 |
| | `tools.database` | DB 준비 도구 | 미연결 | 1 | persistence |
| management | `management.desktop` | Electron 셸 | 코드 있음 | 3 | management-desktop |
| | `management.records` | 개발 기록 | 코드 있음 | 4 | management-records |
| | `management.mcp` | 읽기 MCP | 코드 있음 | 3 | management-records |
| | `management.operations` | 서버 운영·유저 관리 | 미연결 | 1 | management-operations |
| automation | `automation.ci` | CI | 코드 있음 | 1 | engineering |
| | `automation.hooks` | 저장소 훅 | 코드 있음 | 1 | engineering |
| | `automation.standards` | SDK·서식 기준 | 코드 있음 | 4 | engineering |

### 6.1 기존 기능 ID 18개 연결표

빠진 ID는 없다. 목업 자료에 없는 ID를 가리키는 연결도 없다.

| 기존 ID | 연결 카드 수 | 연결 카드 |
|---|---|---|
| connection | 5 | server.session, client.bootstrap, client.network, client-net.connect, shared.protocol |
| movement | 4 | server.session, server.movement, client.prediction, shared.gamedata |
| combat | 4 | server.session, server.combat, client.combat-view, shared.gamedata |
| skills | 3 | server.session, server.skills, client.prediction |
| enemy-ai | 3 | server.world-tick, server.enemy-ai, client.combat-view |
| character-state | 5 | server.combat, server.enemy-ai, server.persistence, client.ui, shared.gamedata |
| map-entry | 5 | server.session, server.map-transition, client.bootstrap, client.network, shared.gamedata |
| party | 5 | server.session, server.world-tick, server.party, client.state, client.ui |
| quest | 4 | server.world-tick, server.quest, client.state, client.ui |
| remote-rendering | 3 | server.publication, client.state, client.combat-view |
| packet-publication | 1 | server.publication |
| transport | 5 | server.network, client.network, client-net.connect, client-net.session, client-net.distribution |
| protocol | 3 | shared.protocol, shared.distribution, tools.packetgen |
| persistence | 2 | server.persistence, tools.database |
| engineering | 9 | client-net.distribution, shared.distribution, tools.bot, tools.server-tests, tools.wsl, tools.formatting, automation.ci, automation.hooks, automation.standards |
| management-desktop | 1 | management.desktop |
| management-operations | 1 | management.operations |
| management-records | 2 | management.records, management.mcp |

### 6.2 분할에서 Astra가 확정할 점

- 목업의 연결은 디자인 작성자가 제목과 경로로 고른 초안이다. 기존 기록과의 관계가 맞는지는 Astra가 정하고 독립 검증자가 대조한다.
- `client.audio`와 `tools.bgm`은 기존 ID 연결이 없다. 빈 연결을 허용할지, `engineering` 등에 붙일지 정해야 한다.
- 시스템 카드 기능 자체(이번 M-2)를 `management` 아래 카드로 둘지는 정하지 않았다. 목업에는 넣지 않았다.
- 관찰: `00_Document/FEATURE_MAP.md`의 스킬 행은 `Maps/States/Actions/`를 가리키지만, `333fe20` tree에서 액션 파일은 `02_Server/GameServer/Maps/Actions/`에 있고 `Maps/States/`에는 상태 기계 파일만 있다. 목업 매핑은 실제 tree를 따랐다. FEATURE_MAP 수정은 이 작업 범위가 아니다.

## 7. 구현 파일 책임 제안

새 UI 라이브러리·라우터·폰트·애니메이션 패키지는 쓰지 않는다. 지금 의존성(React 19, 기존 CSS)으로 충분하다. 데이터 계약·Electron IPC·MCP는 goal의 책임 분리를 따르며, 아래는 화면 쪽 제안이다.

| 파일 (제안) | 책임 |
|---|---|
| `src/theme/tokens.css` | 고른 테마 하나의 토큰과 `--motion` 규칙. 다른 CSS는 토큰만 참조한다. |
| `src/theme/PixelIcon.tsx` | 문자 격자를 SVG path로 바꾼 상수와 아이콘 컴포넌트. 색은 class와 토큰으로 준다. 목업의 `<use>` 방식은 문서 CSS가 그림 안쪽에 닿지 않으므로 제품에서는 path를 직접 그린다. |
| `src/systemCards/navigation.ts` | 화면 위치 상태 `{ level, systemId, cardId, search }`, 상위·검색 복귀 계산. 순수 함수라 단위 테스트가 쉽다. |
| `src/systemCards/search.ts` | 검색 색인과 일치 구간 계산. 순수 함수. |
| `src/systemCards/SystemCardsView.tsx` | 자료 상태(읽는 중·실패·정상)에 따라 화면을 고르고, 화면이 바뀔 때 focus를 옮긴다. |
| `src/systemCards/CardList.tsx` | 1단·2단 카드 목록. 같은 마크업을 테마 CSS가 격자나 행으로 배치한다. |
| `src/systemCards/ImplementationDocument.tsx` | 제한된 section 블록(문단·목록·코드·주석·매핑 표)만 그린다. 임의 HTML은 그리지 않는다. |
| `src/systemCards/GuideStatePanel.tsx` | 4.2절의 로딩·실패·손상·초과 패널. |
| `src/systemCards/system-cards.css` | 카드·문서·검색 배치와 `@container` 단계. |
| `src/App.tsx` | 개발 현황 보기 탭 세 개. 지금처럼 개발 기록 컴포넌트를 숨김 상태로 계속 마운트해 편집 초안이 탭 이동으로 사라지지 않게 한다. |
| `src/DevelopmentRecords.tsx` | 기존 동작 유지. 시스템 선택을 밖에서 열 수 있는 진입(예: 선택할 ID)과 "연결된 시스템 카드" 목록 자리만 더한다. |

구현 때 확인할 것:

- 지금 CSP는 `style-src 'self'`, `img-src 'self'`, `default-src 'none'`이다. 묶음 CSS와 React가 CSSOM으로 넣는 style은 문제없다. 생성 이미지를 쓰면 Vite가 4 KiB 미만 파일을 data URI로 넣는 기본값 때문에 `img-src 'self'`에서 막힐 수 있다. 이 경우 파일로 내보내도록 설정을 정하거나 CSP를 바꿔야 하며, 어느 쪽이든 승인 대상이다.
- 목업은 `:has()`, `@container`, `color-mix()`를 쓴다. Chromium 111 이상이 필요하다. Electron 44의 Chromium 판이 이를 넘는지 구현 단계에서 확인한다.
- 화면 위치는 해시 대신 앱 상태로 들고, `Esc`·버튼을 주 경로로 둔다. Electron 창에는 기본 뒤로가기 키가 없다.

## 8. 폰트·이미지·의존성

### 8.1 Galmuri

- 원천 저장소 [quiple/galmuri](https://github.com/quiple/galmuri)의 `ofl.md`에서 SIL Open Font License 1.1과 "Copyright © 2019–2025 Lee Minseo (quiple@quiple.dev)"를 WebFetch로 확인했다. 같은 조회의 요약 기준으로 예약 글꼴 이름(Reserved Font Name) 문구는 없었다. 제품 도입 전에는 원문 파일을 직접 다시 확인한다.
- 글꼴 파일을 받으려던 Bash `curl https://registry.npmjs.org/galmuri/latest`가 권한 확인에서 거부되었다. 우회하지 않았다. 로컬 Python·fonttools도 없어 글자 줄이기(subset)를 할 수 없었다.
- 그래서 목업의 픽셀 폰트 제목 변형은 **미실행**이다. 목업에는 OFL 전문을 넣지 않았다. 글꼴 파일을 넣지 않았으므로 전문 보존 의무가 생기지 않았고, 출처·라이선스 이름·저작권 줄만 출처 창에 적었다.
- 제품에 넣는다면 필요한 것: 글꼴 파일 번들, `font-src 'self'` CSP 추가, OFL 전문과 저작권 표시 동봉, 한글 글자 수에 따른 파일 크기 확인. 모두 사용자 승인 뒤다.

### 8.2 생성 이미지를 요청하지 않은 이유

GPT-Image 생성 요청은 보내지 않았다. 두 테마의 장식(핀·느낌표·동전·상위 7개 아이콘·상태 아이콘)은 2–6색 픽셀 격자 SVG 16종으로 충분했고, 모두 합쳐 수 KB다. 테마 비교에는 그림의 정밀도보다 정보 위계가 중요하다. 생성 대기가 사용자 목업 전달을 늦출 수 있었다.

테마가 정해진 뒤 더 손그림 같은 질감을 원하면 아래 후보를 Astra에게 요청할 수 있다. 모두 투명 배경, 확대 시 nearest-neighbor 기준, 프롬프트에 개인정보·비밀은 넣지 않는다.

| 후보 | 용도 | 크기·프레임 | 파일 예산 | 프롬프트 묘사 |
|---|---|---|---|---|
| A 게시판 틀 | 나무 판 9-slice 테두리 | 48×48px, 정지 1 | ≤ 4 KB PNG | 16색 이하 픽셀아트 판타지 길드 게시판 나무 틀, 어두운 갈색 판자와 놋쇠 못, 가운데는 비움 |
| A 핀 | 카드 핀 | 16×16px, 흔들림 3 | ≤ 2 KB 스프라이트 | 붉은 머리 놋쇠 압정, 픽셀아트, 좌우로 살짝 기우는 3프레임 |
| A 느낌표 | 주의 표식 | 8×16px, 뜀 4 | ≤ 1 KB 스프라이트 | 노란 느낌표 말풍선, 픽셀아트, 위아래로 1–2px 뜨는 4프레임 |
| B 동전 | 장부 머리 장식 | 16×16px, 회전 4 | ≤ 2 KB 스프라이트 | 금화 회전 4프레임, 픽셀아트, 테두리 어두운 갈색 |
| 공통 아이콘 | 상위 7개 시스템 | 16×16px 7장, 정지 | 장당 ≤ 1 KB | 서버 랙·모니터·연결 단자·두루마리와 인장·망치·장부·톱니바퀴, 같은 팔레트의 픽셀아트 |

생성물을 쓰면 프롬프트·도구·표시 모델·생성 일시·backend `unknown`을 이 목표 산출물에 기록한다. 스프라이트 애니메이션도 3.6절의 동작 감소 규칙을 따른다.

## 9. 목업 사용법과 한계

- 파일 하나를 브라우저로 열면 된다. 외부 요청은 문서 머리의 CSP가 막는다(`connect-src 'none'`, `img-src data:`, `font-src data:`). 링크는 누를 때만 열린다.
- 위쪽 회색 막대는 목업 제어용이며 제품 화면이 아니다. 테마, 자료 상태 8종, 긴 글·긴 경로, 창 폭(1280×720·760·420·브라우저 폭), 동작 감소, 바로 가기(대표 문서·미완료 카드·미연결 카드·결과 없음), 출처·라이선스 창이 있다.
- 주소 뒤에 `?theme=ledger&frame=760&state=guide-fail&motion=reduce&stress=1&dirty=1`처럼 붙이면 같은 상태로 바로 열린다. 테마·창 폭·동작 선택은 브라우저 저장소에 남는다.
- 본문이 있는 대표 문서는 9개다: `server.session`, `client.network`, `client-net.session`, `shared.protocol`, `tools.bot`, `management.mcp`, `automation.ci`, 미완료 예시 `server.persistence`, 미연결 예시 `management.operations`. 나머지 30개는 요약·매핑·기존 ID와 "목업: 본문 생략" 표시만 있다.
- 개발 기록 탭은 catalog의 시스템 18개·기록 18개 제목과 상태만 옮겼다. 근거 35개 목록과 동작 상세는 생략했다.
- 기록 편집의 저장은 흉내만 낸다. 검증은 JSON 구문과 최상위 필드만 본다.
- 사용자 화면은 Electron이 아니라 브라우저에서 확인했다. Electron 실행, 실제 데이터 파일, MCP는 이번 범위가 아니다.

## 10. 결정 뒤 갱신할 규칙 (제안, 지금 수정하지 않음)

- **R-12 (A를 고를 때)**: "기본 크롬은 차분한 다크 운영 콘솔을 유지하고, 개발 현황의 시스템 카드에 픽셀아트 퀘스트 게시판 장식 층(나무 판·종이 카드·핀·주의 느낌표)을 더한다. 데이터 층은 평평한 종이 면에 두고, 상태는 모양·글자·테두리로 표시한다."
- **R-12 (B를 고를 때)**: "앱 전체를 밝은 상점 장부 화면으로 바꾸고, 시스템 카드는 색인 탭과 품목 행으로 배치한다." 이 경우 지금의 다크 콘솔 결정과 `#10151e` 배경 조항을 바꿔야 한다.
- **D-09**: 레퍼런스로 고른 게임과 확인한 공식 출처, 실제 아트를 복사하지 않는다는 문장, 픽셀 SVG를 직접 그린다는 문장, 폰트 결정을 더한다. 기존 shadcn·Tabler 레이아웃 참고는 크롬 구조 근거로 남긴다.

## 세부 근거

- 승인 범위와 계약: [goal.md](goal.md), 특히 카드 분류 초안 표, 디자인 산출물과 승인 경계, 완료조건 4·5.
- 현재 화면: `05_Management/frontend/src/App.tsx`(메뉴·미연결 문장), `DevelopmentRecords.tsx`(편집 초안·2 MiB 제한·알림 문장), `styles.css`(R-12 토큰), `vite.config.ts`(production CSP).
- 기존 기록: `05_Management/records/catalog.json` revision `2026-10-01-r2`, asOf `2026-09-30T09:05:09Z`, sourceCommit `c27b03e`.
- 구조 대조: `00_Document/ARCHITECTURE.md`, `00_Document/FEATURE_MAP.md`. 메인의 시스템 지도 HTML과 진단 메모는 분류 후보와 방향을 읽는 데만 썼고, 사실로 옮기지 않았다.
- 경로 확인: 매핑 경로 88개를 `git cat-file -t 333fe20:<path>`로 확인했다. 실제 키 입력·폭·상태 점검 결과와 스크린샷은 작성자 자기 점검 보고에 있다.
