# 개발 현황 시스템 카드 혼합안 r2 자산 명세

추적 번호 M-2. 작성: 디자인 작성자 세션 `[Management 검증자]`(요청·최초 실행 모델 `claude-opus-5-5`, backend `unknown`). 생성·다운로드·가공·manifest는 Astra 소유다. 작성자는 이 명세를 쓰고, 받은 최종 파일을 목업에 data URI로 넣기만 했다. 디자인 비평 뒤에는 신규 수정 세션 `[Management 검증자]`(요청 모델 `claude-opus-5-5`, backend `unknown`)가 다시 만든 클라이언트 전송 그림을 목업에 넣고 1·6절을 고쳤다.

**결과 요약:** 1절의 필수 14종이 모두 예산 안에서 만들어져 목업에 들어갔다. 디자인 비평 뒤 클라이언트 전송 엠블럼 한 장만 정사각에 가까운 구도로 다시 만들어 바꿨다(6절). 선택 항목 `pin.png`는 만들지 않았고 r1 픽셀 SVG 압정을 쓴다. 실제 가공 경로와 파일별 결과는 6절에 있다.

## 0. 공통 규칙

- **도구:** Codex 내장 GPT-Image. Unity·비승인 API·외부 이미지 사이트는 쓰지 않는다.
- **금지 prompt:** 특정 게임 이름, "~ 스타일로", 실제 게임 아트·캐릭터·로고·UI 모사, 사람·동물 캐릭터. 그림 안 글자·숫자·기호 문자도 넣지 않는다. 상태 이름은 HTML 글자로 따로 붙인다.
- **원본과 최종 구분:** tool 원본(실제 1254×1254, 장부 머리 그림 1698×926)은 크기 제한 없이 `assets/source/`에 보존하고 목업에는 넣지 않는다. 아래 크기·예산은 **최종 파일**에만 적용한다.
- **최종 파일 가공 기준:** 논리 격자로 줄인 뒤(면적 평균) 색을 아래 색 수 이하로 줄이고, 최근접 확대로 최종 크기를 만든다. 투명 항목은 알파를 0/255 두 값으로 끊어 반투명 테두리를 없앤다. 실제로 쓴 단계는 6절의 Astra 기록을 따른다.
- **표시:** 목업은 `image-rendering: pixelated`로 최종 파일을 1배 또는 정수배로만 표시한다.
- **기록:** 생성물마다 prompt 원문, 도구, 화면 표시 모델, 생성 일시, backend `unknown`, 가공 단계, 최종 파일 SHA-256을 manifest에 남긴다.

### 공통 팔레트 (sRGB)

| 묶음 | 색 |
|---|---|
| 외곽선 | `#1a120b` |
| 호두나무 | `#24170d` `#3b2a1e` `#4b3222` `#5d4432` `#7a5a3e` |
| 코르크 | `#8a5a2e` `#a8743f` `#c08a4f` `#d9a866` 반점 `#6b4423` |
| 놋쇠·핀 | `#e3b24a` `#b5862a` `#c8473a` |
| 종이 | `#fdf6e6` `#f3e9d2` |
| 도장 잉크 | 코드 있음 `#1f6b3a`, 일부 `#7d5200`, 미완료 `#9b2c22`, 미연결 `#4f5560` |
| 시스템 색 | server `#6f9bd1`, client `#5fb27f`, client-net `#3fa7a7`, shared `#d39a3c`, tools `#a47fd0`, management `#d07560`, automation `#8e98a3` |

각 그림은 위 표에서 고르고, 명암용으로 같은 색의 밝기 변형 2단까지 허용한다.

## 1. 목록

| 파일 (`assets/`) | 의미·쓰는 곳 | 그림 | 투명 | 논리 격자 → 최종 파일 | 색 수 | 최종 예산 |
|---|---|---|---|---|---|---|
| `emblem-server.png` | 게임 서버 카드·탭 | 서버 랙 한 대, 점등 표시등 | 예 | 32×32 → 64×64 | ≤12 | ≤4 KB |
| `emblem-client.png` | Unity 클라이언트 | 작은 모니터, 화면에 언덕과 해 하나 | 예 | 32×32 → 64×64 | ≤12 | ≤4 KB |
| `emblem-client-net.png` | 클라이언트 전송 | 대각선으로 맞물린 두 연결 플러그와 위아래로 감긴 굵은 케이블. 실루엣은 정사각에 가깝게(비평 뒤 교체, 처음 그림은 두 플러그와 작은 불꽃) | 예 | 32×32 → 64×64 | ≤12 | ≤4 KB |
| `emblem-shared.png` | 공유 계약 | 말린 양피지와 밀랍 인장 | 예 | 32×32 → 64×64 | ≤12 | ≤4 KB |
| `emblem-tools.png` | 개발·검증 도구 | 교차한 망치와 렌치 | 예 | 32×32 → 64×64 | ≤12 | ≤4 KB |
| `emblem-management.png` | 운영툴 | 펼친 장부와 깃펜 | 예 | 32×32 → 64×64 | ≤12 | ≤4 KB |
| `emblem-automation.png` | 자동화·저장소 규칙 | 맞물린 톱니바퀴 두 개 | 예 | 32×32 → 64×64 | ≤12 | ≤4 KB |
| `stamp-present.png` | 상태 도장: 코드 있음 | 실선 이중 원 + 체크 | 예 | 24×24 → 48×48 | 잉크 1색 | ≤3 KB |
| `stamp-partial.png` | 상태 도장: 일부만 있음 | 실선 원 + 왼쪽 절반 채움 | 예 | 24×24 → 48×48 | 잉크 1색 | ≤3 KB |
| `stamp-absent.png` | 상태 도장: 미완료 | 파선 원 + 가운데 가로 막대 | 예 | 24×24 → 48×48 | 잉크 1색 | ≤3 KB |
| `stamp-disconnected.png` | 상태 도장: 미연결 | 점선 원 + 끊긴 사슬 고리 | 예 | 24×24 → 48×48 | 잉크 1색 | ≤3 KB |
| `tex-cork.png` | 게시판 바탕(카드 뒤) | 코르크판 반점 질감 | 아니오 | 64×64 → 128×128, 이음매 없이 반복 | ≤16 | ≤16 KB |
| `tex-wood.png` | 게시판 틀·이름판, 사이드바 옅은 깔개 | 가로 결 호두나무 판자 2–3장, 이음새 | 아니오 | 64×64 → 128×128, 이음매 없이 반복 | ≤16 | ≤16 KB |
| `ledger-head.png` | 사이드바 머리 그림(브랜드 자리) | 상점 계산대 위 펼친 장부, 깃펜과 잉크병, 동전 더미, 작은 등잔 | 예 | 88×48 → 176×96 | ≤24 | ≤16 KB |
| `pin.png` (선택) | 카드 압정 | 붉은 머리 놋쇠 압정, 위에서 본 모습 | 예 | 12×12 → 24×24 | ≤6 | ≤1 KB |

- 시작 목록의 "나무판·코르크 텍스처 1장"을 두 장으로 나눴다. CSS는 그림 일부만 반복할 수 없어서다. 이음매가 남으면 좌우·상하 거울 반전 2×2로 만들어도 된다. 3×3 반복 미리보기에서 이음새가 띠로 보이면 불합격이다.
- `ledger-head.png`는 1280×720 세로 예산 때문에 페이지 머리가 아니라 사이드바 머리(폭 192px)에 둔다. 사이드바 `#3b2a1e` 위에 놓이므로 바깥 외곽선이 그 색에서 보여야 한다.
- `pin.png`가 없으면 r1의 픽셀 SVG 압정을 그대로 쓴다. 다른 항목은 작성자가 SVG로 대신 그리지 않는다.
- 최종 이미지 합계 예산은 120 KB 이하, base64 포함 후 약 160 KB다.

## 2. 항목별 판정 기준

- **엠블럼 7:** 같은 광원(왼쪽 위), 1논리px `#1a120b` 외곽선, 주색은 해당 시스템 색. 64px에서 서로 실루엣만으로 구분돼야 한다. 테두리 배지·바탕판 없이 물체만 둔다.
- **도장 4:** 고무 도장 찍힌 자국. 잉크 한 색, 가장자리 약간 거친 잉크 번짐은 논리 격자 안에서만 허용. 색을 빼도 바깥 원(이중 실선·실선·파선·점선)과 안쪽 기호(체크·반 채움·막대·끊긴 고리)로 넷이 구분돼야 한다.
- **코르크:** 평균 밝기가 `#a8743f` 근처다. 카드 종이 `#fdf6e6`와 비글자 대비 3:1 이상을 지키고, 반점 밝기 변화는 작게 둬 카드 글자보다 눈에 띄지 않게 한다.
- **나무:** 평균 밝기 `#4b3222` 근처, 결 대비는 낮게. 이름판 글자 `#fff1d6`가 위에서 4.5:1 이상 읽혀야 한다.
- **장부 머리 그림:** 정면 약간 위 시점, 가로로 긴 정물. 사람·글자·간판 문구 없음.

## 3. prompt 초안 (Astra가 다듬는다)

공통 꼬리: `original pixel art, limited palette, crisp 1-pixel dark outline, no text, no letters, no numbers, no logo, no characters, centered single object, flat solid magenta (#ff00ff) background for keying`. 실제 생성은 내장 도구의 투명 배경 기능을 써서 magenta 키잉을 하지 않았다(6절). 텍스처는 마지막 문구 대신 `seamless tileable texture, fills the whole frame, no object, top-down flat lighting`.

- 엠블럼 예: `a small server rack tower with tiny blinking status lights, steel blue (#6f9bd1) panels, walnut and brass accents` + 공통 꼬리.
- 도장 예: `a rubber stamp ink impression, a double solid circle with a check mark inside, single dark green ink (#1f6b3a), slightly uneven ink edges` + 공통 꼬리.
- 코르크: `cork board surface, warm mid brown (#a8743f) with small darker and lighter granules` + 텍스처 꼬리.
- 나무: `dark walnut wooden planks laid horizontally, subtle grain, thin dark seams between planks` + 텍스처 꼬리.
- 장부 머리 그림: `a shop counter still life: an open ledger book, a quill in an ink pot, a small stack of gold coins, a little oil lamp, warm light, wide composition` + 공통 꼬리(centered single object 제외).

## 4. 폰트 (Astra 삽입)

- **파일 하나:** 공식 Galmuri v2.40.4 배포의 `Galmuri11-Bold.woff2`. 그 배포에 이 파일이 없으면 `Galmuri11.woff2`로 하고 이유를 manifest에 적는다.
- **쓰는 곳:** 제목만. 앱 페이지 제목, 게시판 이름판, 2단 시스템 이름, 구현 문서 제목. 크기는 12px 또는 24px로 정수배만 쓴다. 본문·카드 요약·표·입력은 기존 시스템 글꼴을 유지한다.
- **삽입 위치 계약:** `mockup.html`의 CSP `<meta>` 바로 다음 줄에 아래 두 덩어리만 넣는다. 작성자는 큰 data URI 줄을 수정하지 않는다.
  - `<style id="font-galmuri">@font-face{font-family:"M2 Galmuri";src:url(data:font/woff2;base64,…) format("woff2");font-display:block}</style>`
  - `<template id="galmuri-license">`: 출처 URL, 버전, 파일 이름, SHA-256, 받은 일시, OFL-1.1 전문 원문. 작성자가 출처·라이선스 창에서 이 template을 그대로 보여 준다.
- 제품 폰트 도입·CSP 변경·의존성은 이 명세의 범위가 아니다.
- **결과:** Astra가 공식 v2.40.4 배포의 `Galmuri11-Bold.woff2`(166,632 B, SHA-256 `8643094f…4f2d078`, 고정 commit `bdb86ae`)를 위 계약대로 넣었다. 출처와 OFL 전문은 [assets/fonts/source.json](assets/fonts/source.json)·[OFL-1.1.txt](assets/fonts/OFL-1.1.txt)에도 있다. 목업의 출처·라이선스 창은 `galmuri-license` template을 그대로 펼쳐 보여 준다.

## 5. 인계

Astra가 폰트를 넣고 최종 파일 경로·manifest 경로를 회신하면 그 시점부터 작성자가 `mockup.html` 쓰기를 맡는다. 실제 인계는 `msg_f339b2638025`에 대한 Astra 회신 `msg_96889d6f4702`였다. 작성자는 최종 파일을 Node 스크립트로 base64 data URI로 바꿔 목업에 넣고, 그 스크립트와 실행 로그를 `.backups/verification/2026-10-02-management-m2-system-cards/r2/design/`에 둔다. 예산 초과·투명 실패·이음새가 보이면 작성자가 고치지 않고 항목 이름과 관찰을 Astra에게 돌려준다.

## 6. 실제 생성·가공 결과 (Astra 기록 기준)

- **생성:** Codex 내장 이미지 생성 도구(`image_gen__imagegen`, 요청 표기 "Codex built-in GPT-Image"), 화면 표시 모델은 노출되지 않음, backend `unknown`. 투명 항목은 도구의 투명 배경으로 받았고 magenta 키잉은 하지 않았다. prompt 원문·생성 일시는 [generation-manifest.json](assets/generation-manifest.json)에 있다.
- **가공:** 메인 `msg_548ad283088e`가 허용한 Node 내장 기능(fs·zlib·crypto, 새 패키지 없음)으로 Astra가 결정적으로 가공했다. 스크립트와 결과 기록은 로컬 `.backups/verification/2026-10-02-management-m2-system-cards/r2/normalize-assets.mjs`, `normalization-result.jsonl`이다.
  - 엠블럼·도장·머리 그림: 알파 128 이상 영역으로 자르기 → 비율을 지킨 면적 평균 축소(가운데 정렬) → 알파 128 기준 0/255 → 가중 median-cut 팔레트 → 최근접 2배.
  - 텍스처: 원본 전체를 32×32로 면적 평균 → 좌우·상하 거울 반전으로 이음매 없는 64×64 → 팔레트 16색 이하 → 최근접 2배. Astra가 3×3 반복 표본을 확인했다.
- **선택 항목:** `pin.png`는 만들지 않았다. 카드 압정은 r1 픽셀 SVG를 그대로 쓴다.

| 최종 파일 | 원본 → 최종 | 색 수 | 알파 | 크기 / 예산 |
|---|---|---|---|---|
| `emblem-*.png` 7종 | 1254² → 64×64 | 각 12 | 0/255 | 521–711 B / 4 KB |
| `stamp-*.png` 4종 | 1254² → 48×48 | 각 1 | 0/255 | 195–235 B / 3 KB |
| `tex-cork.png` | 1254² → 128×128 | 16 | 불투명 | 3,313 B / 16 KB |
| `tex-wood.png` | 1254² → 128×128 | 15 | 불투명 | 2,768 B / 16 KB |
| `ledger-head.png` | 1698×926 → 176×96 | 24 | 0/255 | 2,433 B / 16 KB |
| 합계 14개 | | | | 13,586 B / 120 KB (교체 전 13,209 B) |

- **목업 반영:** 작성자가 `embed-assets.mjs`로 manifest의 최종 SHA-256과 실제 파일 hash를 대조한 뒤 `r2-assets` 블록에 data URI로 넣었다. 원본(`assets/source/`)은 넣지 않았다. 스크립트와 실행 기록은 `.backups/verification/2026-10-02-management-m2-system-cards/r2/design/`에 있다.
- **처음 작성자가 본 한계:** 처음 `emblem-client-net.png`는 실루엣이 가로로 얇아 64px에서 다른 엠블럼보다 가볍게 보였다. 판정 기준(구분 가능·투명·예산)은 지켰으므로 r2에서는 바꾸지 않았다.
- **비평 뒤 교체:** 독립 디자인 비평이 이 그림을 필수 수정으로 지적했다(64px 칸에서 실제 그림이 약 55×14px 가로 띠). Astra가 정사각 구도를 요구하는 prompt로 같은 도구에서 다시 만들고, 기존 승인 Node 정규화로 가공했다.
  - 새 원본 `assets/source/emblem-client-net-fix-1.png`, 최종 `emblem-client-net.png` 64×64·12색·알파 0/255·711 B, SHA-256 `0f5666988216c5f898414ce9b23e82bea2f2997da4980f3869473016c50288b8`. 이전 최종 파일은 `assets/history/emblem-client-net-r2.png`, 이전 원본은 `assets/source/emblem-client-net.png`에 남아 있다. 새 prompt·관측 일시·이전 판은 manifest 해당 항목의 `previousVersions`에 있다.
  - 나머지 13종 최종 파일은 바뀌지 않았다. 수정 세션이 `embed-assets.mjs` 복사본으로 다시 넣은 뒤 목업 안 13종 data URI가 교체 전과 바이트 단위로 같음을 대조했다.
  - 실제 렌더 면적(그림이 있을 때와 숨겼을 때 화면 차이): 64px 카드 56×14 → 50×56px, 32px 색인 탭 28×7 → 25×28px. 다른 여섯 엠블럼의 높이 중앙값(48px·24px)의 1.17배다. 근거는 로컬 `r2/fix-1/emblem-footprint-{before,after}.json`이다.
