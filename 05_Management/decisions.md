# Management 결정과 이유

요구 내용의 원본은 [requirements](requirements.md)다. 여기에는 선택과 이유를 ID로 연결하며 작업 상태/실적은 [README](README.md)가 연결하는 각 목표에서만 관리한다. 기준일 2026-09-30.

## D-01

**확정:** [R-01](requirements.md#r-01)의 우선순위를 따른다. 현재 라이브 운영 필요를 먼저 다루고 유저/GM와 개발 근거 영역으로 이어간다. 초기 화면 탐색도 이 순서를 사용한다.

이는 장기 제품 우선순위다. PR147 이후 제한된 후속의 확정 순서인 개발 기록 공동 조회 → 서버 등록·로그 조회는 [D-10](#d-10)을 따른다. 우선순위 재논의 예정이라는 전달은 기존 합의의 취소나 새 구현 착수 승인이 아니다.

## D-02

**확정:** [R-02](requirements.md#r-02)의 현재 운영 단위와 로컬 접근 제한을 따른다. 게임 접속 제공과 관리 접근의 권한 경계를 분리하기 위한 선택이다. 장기 다중 서버 요구를 현재 Docker/분산 구현 승인으로 해석하지 않는다.

브라우저 대시보드 중심 구상에서 사용자가 원하는 Windows 독립 앱/창 방향으로 구체화했다. 초기 프런트엔드의 브라우저 실행은 개발 확인 수단이고 최종 제품 포장 결정이 아니다.

## D-03

**확정:** [R-03](requirements.md#r-03)의 화면 기술과 WSL 실행 환경을 채택한다. React·TypeScript·Vite 화면과 WSL 운영 구성의 경계를 나눠 설계한다.

사용자의 최신 초기화 지시로 문서 기록만 하던 범위에서 프런트엔드 최소 기반까지 확대했다. 이번 [목표](goals/2026-09-30-foundation/goal.md)는 백엔드/실제 운영 구현을 포함하지 않는다.

**후속 확정:** 초기 논의의 C# ASP.NET Core 제안과 Tauri/Electron 후보 중, 사용자가 WSL ASP.NET Core 관리백엔드와 Electron을 직접 선택했다. [데스크톱 창 목표](goals/2026-09-30-desktop-shell/goal.md)로 독립 창의 개발 실행 가능성을 먼저 확인하고 상태·로그/실제 백엔드 연동은 그 다음 목표로 나눈다. 서명 방식은 [D-08](#d-08)에 남긴다.

## D-04

**확정:** [R-04](requirements.md#r-04)·[R-05](requirements.md#r-05)에 따라 창 수명과 운영 수명을 구분하고, 제한 복구와 PC 재부팅 후 수동 라이브 시작을 따른다. 사용자는 창을 닫아도 장애 알림을 유지하도록 트레이에 남기는 방향을 선택했다. 이 답변에 따라 데스크톱 셸 범위를 기본 트레이 숨김/열기/명시 종료까지 확대했다. 실제 알림·서버/백엔드 수명 제어는 포함하지 않는다.

**제안/미결정:** 독립 운영 담당 프로세스는 이를 충족하는 구조 제안이다. 완전종료 뒤 알림·자동시작·응답없음 판정·복구 횟수/간격·재부팅 후 예약 정책은 아직 선택하지 않았다.

## D-05

**확정:** [R-06](requirements.md#r-06)의 저장완료를 정상 종료의 선행조건으로 삼는다. 유저 미저장 변경을 남긴 자동 강제종료와 그 뒤 예약 재시작을 막기 위한 선택이다.

게임 틱 밖의 종료조정자가 비동기 완료/실패를 기다려야 한다. GameServer 영속저장이 선행 의존성이며 DB schema/접속 확인을 이 기능의 완료로 사용하지 않는다.

## D-06

**확정:** [R-07](requirements.md#r-07)에 따라 개발 빌드와 라이브 버전을 구분하고, 업데이트와 장애복구의 버전 선택을 구분한다. 개발 중 산출물이 승인 없이 라이브에 반영되지 않도록 한다.

**미결정:** rollback과 데이터/프로토콜 호환 정책은 별도 설계가 필요하다.

## D-07

**확정:** [R-08](requirements.md#r-08)·[R-09](requirements.md#r-09)의 관리/GM 사용과 증거 기반 요구를 따른다. 서버가 판정과 권한 검증을 소유하는 공통 계약을 유지한다.

**제안/미결정:** 게임 GM 권한과 도구 운영자 권한 분리는 책임·접근 범위를 구분하기 위한 제안이다. 개별 명령과 자동 탐지/제재는 확정하지 않았다. [R-10](requirements.md#r-10)·[R-11](requirements.md#r-11)의 에이전트 연결도 라이브 조작권한을 자동 부여하지 않는다.

## D-08

**확정/미결정:** Windows 창은 Electron을 채택했고 서명은 최대한 비용 없이 진행하기를 원한다. 서명 방식·가입 가능 여부는 아직 미결정이다. 로컬 개발 실행을 먼저 관찰하며 기존 [차단 관찰](requirements.md#조사-관찰과-미실행)을 Electron 실패 증거로 대체하지 않는다. 개발 실행 성공도 배포 EXE/설치프로그램이나 SAC 전체 검증으로 확대하지 않는다.

사용자는 **개인 개발자**라고 답했다. 법적 소재지는 별도 명시하지 않았다. 2026-09-30 전달된 서명 조사 기준, Microsoft Artifact Signing Public Trust는 한국 소재 조직이 대상에 포함되며 개인은 미국/캐나다 소재로 제한된다. 따라서 한국 소재 개인이라면 대상이 아니다. 사용자의 소재지나 가입 가능 여부를 확정하지 않았고, 서명서비스 가입·비용·구매 결정은 없다. [공식 가입 안내](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)

Public Trust Test·Private Trust·자체서명을 현재 SAC를 유지하는 공개신뢰 해결책으로 단정하지 않는다. [공식 신뢰 모델](https://learn.microsoft.com/en-us/azure/artifact-signing/concept-trust-models)

## D-09

**확정:** 사용자의 기존 화면이 투박하다는 지적과 웹 디자인 참고 요청에 따라 [R-12](requirements.md#r-12)의 차분한 다크 콘솔·절제된 blue 방향을 선택했다. 가는 사이드바와 낮은 헤더, 상태 요약과 넓은 로그 영역으로 운영 화면의 구성을 정리한다. 미연결·비활성 제어·메뉴 의미를 보존한다.

레이아웃/간격 참고는 [shadcn blocks](https://ui.shadcn.com/blocks)·[sidebar](https://ui.shadcn.com/blocks/sidebar), [Tabler vertical layout](https://preview.tabler.io/layout-vertical.html)·[admin template](https://tabler.io/admin-template)다. 직접 SVG/CSS로 구성하며 템플릿 전체 복사·설치나 패키지 추가를 하지 않는다.

두 프로젝트의 라이선스 원본은 MIT로 확인했다: [shadcn LICENSE.md](https://github.com/shadcn-ui/ui/blob/main/LICENSE.md), [Tabler LICENSE](https://github.com/tabler/tabler/blob/dev/LICENSE). 이는 참고 출처 기록이며 외부 구현 코드를 복사했다는 뜻이 아니다. 실제 변경과 검증 결과는 [데스크톱 창 목표](goals/2026-09-30-desktop-shell/goal.md)에만 기록한다.

## D-10

**후속 확정:** [R-10](requirements.md#r-10)에 따라 개발 기록 공동 조회 → 서버 등록·로그 조회 순서로 진행한다. 현재 구현된 `records/catalog.json` 파일을 공통 원본으로 유지하고, 첫 조회는 로컬 stdio MCP와 공통 조회 모듈로 앱 수명과 분리한다. 사람과 에이전트가 같은 자료를 읽게 하되 첫 MCP에 쓰기·서버 제어를 노출하지 않는다. 합의와 완료조건은 [공동 조회·로그 합의](goals/2026-09-30-system-records/shared-read-agreements.md)에서 찾는다.

일반 로그 최근 10분 조회와 서버당 최대 7일·1GB 보존, 조사 근거의 별도 보존도 확정했다. 자동 수집·갱신 주기, 쓰기/실행 권한, 등록·회전 세부 계약 등 [남은 결정](goals/2026-09-30-system-records/shared-read-agreements.md#다음-결정)은 유지한다. 이 선택은 구현 실적이 아니며 2026-10-01 사실 정정 요청으로 MCP 착수 보류가 해제되지는 않는다.

## 기록 원칙

요구와 미결정은 requirements, 선택 이유와 변경된 방향은 이 문서, 구현 목표·상태·결과는 각 goal에 둔다. 전체 대화·일일 장부·별도 진행률 문서는 만들지 않는다. 새 구현 범위는 기존 Game Dev 메인과 사용자 합의 후 별도 목표로 정한다.
