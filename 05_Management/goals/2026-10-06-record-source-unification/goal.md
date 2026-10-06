# 운영툴 기록 원본 일원화와 역할 정리

## 진척 단계

- [x] 범위 승인과 goal 고정
- [x] 결정·문서 정정 작성
- [x] PR196 병합
- [>] 색인 선행 시험 작성
- [ ] 색인 전환 구현·검증
- [ ] 색인 PR 병합
- [ ] 백로그 메뉴 구현·검증
- [ ] 백로그 PR 병합
- [ ] Gardener와 종료 기록

PR 번호가 생기면 「문서 PR 병합」 같은 단계 이름을 「PR000 병합」 형식으로 바꾼다.

## 재개 지점

**기록 시점: 2026-10-06 22:5x KST, PR196 병합 뒤 PR2 branch의 첫 goal 갱신 commit.** 이 문단과 아래 순서는 그 시점의 상태와 당시 예정이다. 그 뒤의 실제 진행은 「진척 단계」, 「결과와 열린 사항」, 그리고 리드가 단계마다 다시 쓰는 이 문단을 따른다.

그 시점의 상태는 다음과 같다. PR1은 [PR196](https://github.com/bass131/dawnholder-server/pull/196)으로 병합됐다(아래 「PR196 병합」). PR2 branch `feat/management-record-index-20261006`을 최신 main `a47a0276`(PR196 병합 commit)에서 만들었다. 작업 경로는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`다. 열린 작업자·검증자는 없었다.

당시 예정 순서는 다음과 같다.

1. 리드가 PR2 맥락 메모를 쓰고, TDD 선행 시험 계약을 발행한다. 시험 작성자는 신규 `claude-opus-5-5`이고, 계약 판정 기준에 「적용 중인 메인 결정」 2항의 경로 경계 거절 사례 8종을 넣는다.
2. 실패하는 요구 시험과 그 원시 결과가 생기면 신규 `gpt-6.1-sol`(max)에게 구현을 맡긴다.
3. 구현 뒤 다른 신규 `claude-opus-5-5`가 강 등급으로 독립 검증한다. 실제 Electron 확인은 「설계와 검증 경계」를 따른다.
4. PR 생성 직전 메인 알림 → CI → 메인 R-2 → 사용자 개별 병합 승인.
5. PR3(백로그 메뉴)은 PR2 병합 뒤 최신 main의 새 branch에서 시작한다. PR3 시험 작성자 모델은 계약 전에 메인에 묻는다.

근거 폴더 E는 저장소 로컬 `.backups/verification/2026-10-06-record-source-unification/`, 진입 근거 E0은 `.backups/verification/2026-10-06-management-entry/`다. 리드 맥락 메모는 E/`astra-context.md`다. 이번 Run은 `run_3fa510a50602`다. 2026-10-06 22:4x KST Orca 재시작으로 리드 handle이 `term_d2c6dac2…`에서 `term_7ad342b5-663c-4d6b-980c-bc65ca5b1c26`으로 바뀌었고, 리드가 run-use로 Run을 다시 묶었다(E/`run-use-after-restart.json`). handle은 이 세션의 관측값이며 다음 리드의 실행 권한이 아니다.

## 요청 원천과 승인

메인 `msg_1f81ec8d4d15`(2026-10-06T09:27:02Z, E0/`entry-inbox-all.json`)가 새 Management 리드 진입과 목표 요청서(계획 묶음 4, 항목 18~24)를 보냈다. 리드는 범위 초안 v1 `msg_d10dbe755fdb`(E0/`scope-draft-v1.md`)을 보냈다. 메인은 `msg_4c7e21aeead6`(E0/`main-scope-receipt-and-coordination.json`)으로 초안을 현황판 계획 검토에 올리고 조율 질문에 답했다.

메인 `msg_dbff8df03e07`(2026-10-06T10:13:16Z, E0/`user-approval-relay.json`)이 전달한 사용자 원문(메인 pane 제출, 19:1x KST):

> 대시보드 결정 응답: 1) 계획 검토 - Management 운영툴 기록 원본 일원화와 역할 정리 → A 승인 (초안 msg_d10dbe755fdb)

코멘트는 없다. 메인 정리본에는 「리드 추천대로면 A 승인만 누르면 된다. 다르게 고르려면 코멘트에 「질문 2는 B」처럼 적어 줘.」가 있었다. 그래서 초안 질문의 답은 추천 그대로다. 메인 전달이므로 사용자 직접 입력으로 격상하지 않는다.

| 질문 | 답 | 결과 |
|---|---|---|
| 1 화면의 기록 편집 | A 없앤다 | 화면은 읽기 전용, 색인 수정은 Git PR로만 한다 |
| 2 원문 읽기 기준 | A 실행 중인 checkout의 파일 | 앱·MCP가 그 checkout의 파일을 읽고 branch·HEAD를 표시한다 |
| 3 MCP 실제 세션 연결 | B 이번엔 연결하지 않는다 | MCP는 새 색인과 시스템 카드 자료를 읽도록 고쳐 두고 연결은 다음 계획에서 정한다 |
| 초안 7절 4) 해석 | 이의 없음 | 구현 설명 3건과 카드 소개는 유일한 원문이라 05_Management의 system-guide.json에 둔다 |

## 적용 중인 사용자 결정

모두 메인 전달이며 사용자 직접 입력으로 격상하지 않는다. 원문은 `msg_1f81ec8d4d15`에서 옮겼다.

- **기록 원본 일원화(18:1x KST):** 「대시보드 결정 응답: … 4) 문서와 운영툴 기록의 이중 원본 - 9-30 링크 합의를 확정할지 → A 9-30 링크 합의로 확정 …」. 원본은 프로젝트 문서 하나로 못 박는다. 9-30에 열어 둔 「항목별 서술 소유권 전환」을 닫는다. 운영툴 기록의 요약·상태 사본은 원문 링크(commit·경로·구간)와 색인으로 바꾼다. 9-30 합의 원문은 [이주 협의안](../2026-09-30-system-records/migration-consultation.md)과 [재배치 대응표](../2026-09-30-system-records/migration-map.md), Core checkout의 로컬 회신 `.backups/handoffs/2026-09-30-management-records-gamedev-reply.md`다.
- **처리 계획 승인(18:2x KST):** 「OK A로 가자, 현황판도 업데이트 해줘」. 직전 질문은 「규칙·운영 미반영 38건 - 항목별 목록과 처리 순서」, A는 「이 묶음·순서로 시작」이다. 이 goal이 그 계획의 묶음 4다.
- **검증자 모델 시범(09:1x Z 제출):** 「대시보드 결정 응답: 3) 검증자 모델 - Astra 검증을 시범으로 시작할지 → A 10-31까지 시범으로 시작」. 정본(AGENTS 모델 라우팅·ORCA R-5) 반영 전까지 다음을 적용한다.
  - 문서 실사와 코드 검증(강·약)의 독립 검증자는 신규 `gpt-6-astra`(reasoning effort xhigh)다. 기동은 리드 pane 아래 vertical split `codex --model gpt-6-astra -c model_reasoning_effort=xhigh`다.
  - DB·영속 데이터, 프로토콜·공유 DLL, 보안 경계를 바꾸는 작업은 신규 `claude-opus-5-5`가 검증한다. 애매하면 Opus다.
  - Gardener, 확정 실패 뒤 Advisor, R-7 설계 검토는 지금 배정 그대로다.
  - 구현자·검증자 분리, 작업 하나 뒤 정산·종료, 파트당 검증자 동시 하나, 시험 파일만 쓰기, 판정 양식과 차단 사유, 태그 `[Management 검증자]`는 그대로다.
  - 모델 부재·capacity 장기 실패는 Opus로 임의 대체하지 않고 메인에 보고한다. Codex 컨텍스트는 273k이므로 고정 입력이 크면 계약을 나누고 나눈 기준을 계약에 적는다.
  - 판정 원문의 지정 모델·관찰 모델 칸을 반드시 채운다. 10-31 평가 자료로 검증자 모델별 판정 수와 뒤에 드러난 놓친 결함을 센다.
- **병합 관문(17:19 KST):** 「1안건 A」. 병합은 메인 창에서만 하고 사용자가 제출한 승인 문장을 기록하는 hook으로 통과시킨다. 구현 전까지는 리드 pane에서 병합하되 메인의 병합 신호가 있어야 한다. hook이 구현되면 메인이 알린다(`msg_dbff8df03e07`).
- **운영툴 역할 방향(2026-10-04):** 메인 `msg_3485e21d8942`의 사용자 원문은 [시스템 카드 goal의 역할 방향 절](../2026-10-02-system-cards/goal.md#운영툴과-실시간-현황판의-역할-방향--2026-10-04-메인-전달)에 있다. 운영툴은 완성된 작업 기준과 이후 작업을 정리하고 실시간성은 현황판이 맡는다. 2026-10-05 `msg_b101768ef103`의 「운영툴 「작업 현황」 화면 후보를 폐기할지 → A 폐기」가 후속 확인이다.
- **DB 1단계 과정·의도 기록(2026-10-05):** 메인 `msg_2ecb51e414d5`의 사용자 원문 「그러면 지금까지 한 일련의 과정들이 무슨 의도고 왜 했어야했는데 운영툴 기록에도 작성해줘」. 출처는 [시스템 카드 goal의 다음 계획 후보](../2026-10-02-system-cards/goal.md#현재-목표-우선과-다음-계획-후보)다.
- **「다음 할 일」 정리와 백로그 메뉴(2026-10-03):** 메인 `msg_6e281afe2167`이 전한 「OK 그렇게 진행해줘」. 계약은 [시스템 카드 goal의 백로그 후속 절](../2026-10-02-system-cards/goal.md#후속-백로그--백로그-정본메뉴와-다음-일-정리)이다.
- **「다음 할 일」 애매 두 묶음의 처분(2026-10-06 19:4x KST):** 메인 `msg_dfd3843e9464`(10:40:27Z, E/`wait-8.json`)가 전달한 원문 「대시보드 결정 응답: 1) 원격 플레이 실제 확인 - 백로그로 옮길지 폐기할지 → A 백로그로 옮긴다 · 2) 시스템 지도 3D 표현 - 백로그로 옮길지 폐기할지 → A 백로그로 옮긴다」. 현황판의 A 선택지 글은 1) 「담당 후보는 Core다. 선행 조건은 게임 회귀 단계와 겹치는지 확인하는 것이다.」, 2) 「담당 후보는 Management다. 선행 조건은 색인 전환 뒤 표시할 관계 자료와 필요성을 확인하는 것이다.」였다. 결과는 [분류표의 사용자 결정 절](next-steps-classification.md#사용자-결정)에 있다.
- **새 goal부터 TDD(2026-10-04):** 원문 위치는 [운영 정본 반영 goal](../../../01_Phases/goals/2026-10-05-operating-canon/goal.md)의 85행과 메인의 `HANDOFF.md` 결정 「A」다. 문구 정본화는 Rules의 후속(계획 13번)이다. 이 goal은 Content goal의 적용 방식을 따른다.

## 적용 중인 메인 결정

메인 `msg_4c7e21aeead6`(2026-10-06T09:47:00Z)의 여섯 항목이다. 모두 메인 판단이며 사용자 결정이 아니다.

1. PR2는 TDD 선행 시험 작성자와 독립 검증자 모두 신규 `claude-opus-5-5`다. PR3의 시험 작성자는 PR3 계약을 발행하기 전에 메인에 다시 묻는다. 답이 없으면 현행 정본(AGENTS 모델 라우팅의 테스트 작성 = 신규 `claude-opus-5-5`)을 따른다.
2. R-7 설계 검토 시범은 이 목표에 적용하지 않는다. 대신 PR2 계약의 판정 기준에 경로 경계의 거절 사례를 명시한다: 상위 경로, 절대 경로, 드라이브 문자, 대소문자만 다른 경로, 심볼릭 링크·정션, 허용 밖 확장자, 크기 상한 초과, 등록되지 않은 출처 ID.
3. CURRENT의 Management 줄은 리드가 갱신한다.
4. DB 1단계 문서의 위치와 쓰기·검토 분담은 사용자 승인 뒤 PR1 작업 안에서 Core 리드(`run:run_b36cc92a4cf4`)와 직접 조율한다. 메일을 넣은 뒤 메인에 알리고, 메인이 Core 화면을 보고 안내를 넣는다. Core가 그 파일의 쓰기에 동의하지 않으면 05_Management에 두고 Core 검토를 받는다.
5. Rules로 갈 제안은 메인에 보낸다. 메인이 새 Rules 리드의 goal 입력에 넣는다. 위치 규칙·문서 지도·색인 검사 CI·목표 사이 main 유지 제안 5건은 `msg_026b701e3728`로 보냈다. BACKLOG 이관 행은 27개 분류 확정 뒤 보낸다.
6. 종료 점검 후보 2(vitest CI)는 계획 14번, 후보 1(검증 실행 helper)은 계획 11번에 속하며 둘 다 묶음 2다. 이 goal에 넣지 않는다.

메인 `msg_dbff8df03e07`의 운영 지시: `gh pr create`와 `gh pr merge`는 사용자 확인 창 대상이므로 실행 직전에 메인에 알린다. 병합은 PR마다 메인이 그 head에 대한 사용자 승인을 전달한 뒤에만 한다. 초안과 goal이 달라지는 곳이 생기면 쓰기 전에 메인에 알린다. 우편함 대기는 8개 type `--types` 필터와 `--timeout-ms 900000`을 쓴다(진입 메시지).

## 만들 것

1. **기록 색인 전환(항목 18):** `records/catalog.json`을 schemaVersion 2 색인으로 바꾼다.
   - 남길 필드: 시스템의 id·title·area·sourceIds·relatedSystemIds·recordIds, 기록의 id·type·title·systemIds·sourceIds와 PR 번호·병합 commit 링크, 출처의 id·title·kind·locator(저장소 상대경로)·section(정확한 제목 문구)·availability.
   - 뺄 필드: 시스템의 summary·responsibility·behavior·implementationStatus·integrationStatus·verificationStatus·limitations·nextSteps, 기록의 summary·reason·status·details·limitations·nextSteps, 출처의 note, 최상위 scopeNote와 수기 revision. 버전은 지금처럼 내용 SHA-256으로 식별한다.
   - 9-30 이후 병합된 goal을 기록 색인에 올린다.
2. **화면:** 목록(제목·분야/종류)은 유지한다. 상세는 원문 링크 목록과 선택한 원문 구간의 읽기 전용 표시다. 읽은 checkout의 branch·HEAD를 보인다(Git 메타데이터 파일 읽기, 알 수 없으면 그렇게 표시). 색인에 등록된 저장소 안 Markdown만 읽는다. 없는 파일·구간·크기 초과는 끊긴 링크로 보이고 현재 사실처럼 보이지 않는다. 검색은 색인 필드와 출처 제목 대상으로 줄어든다. 앱은 Git 명령을 실행하지 않는다.
3. **편집 제거:** 기록 편집·JSON 불러오기·저장 화면과 `system-records:save` IPC, 저장소의 저장·백업·충돌 처리 코드를 없앤다.
4. **MCP(항목 21):** DTO를 색인 v2로 바꾸고 system-guide.json의 카드·구현 설명을 같은 원천으로 제공하는 읽기 도구를 더한다. 원문 구간 읽기는 색인에 등록된 출처 ID로만 받는다. 검증 때 새로 빌드한 실행본을 쓰고 README·MCP.md에 재빌드 조건을 적는다. 실제 개발 세션 연결은 하지 않는다.
5. **색인 검사:** 참조 ID·출처 경로·구간 제목의 존재를 검사해 끊긴 링크를 실패로, 색인에 없는 goal을 warning으로 낸다. 백로그 ID·goal 링크 존재 검사도 같은 도구에 둔다. 로컬 실행과 vitest까지 만들고 CI 연결은 묶음 2(계획 14번)로 넘긴다.
6. **읽기 전용 백로그 메뉴(항목 22):** `00_Document/operations/BACKLOG.md` 표를 읽어 「이후 작업」으로 보인다. ID·goal 링크 어긋남은 warning, 표를 읽지 못하면 형식 오류로 표시한다. 기록에서 nextSteps가 빠지는 대신 이 메뉴가 「다음 일」을 맡는다.
7. **「다음 할 일」 27개 분류(항목 22):** catalog nextSteps 27개(시스템 21, 기록 6)를 완료 삭제·BACKLOG 이관·폐기로 나누고, 이미 예정된 로드맵 단계는 「로드맵 연결」로 표시한다. 근거 표는 이 폴더의 `next-steps-classification.md`다. 애매한 항목만 메인을 거쳐 결정 요청으로 올리고 이관 행은 Rules에 보낸다.
8. **결정·요구 반영(항목 19):** requirements·decisions에 기록 원본 일원화와 운영툴 역할 방향을 출처와 함께 넣는다. R-10·D-10의 「catalog 공동 원본」, R-13 소급 정비, R-15의 summary 길이 검사는 대상이 사라지므로 바뀐 이유와 함께 고친다. R-01의 장기 우선순위 문장은 사용자 결정 없이 바꾸지 않고 「후속 작업 순서」만 실제 기록으로 정정한다.
9. **낡은 사실·상태 정정(항목 23):** README(기본 창 1600×900 DIP·125%, CI 워크플로 4개와 vitest 미포함, 「원본 한 곳」, revision 설명, 문서 지도의 빠진 goal), RESUME(현재 goal 안내), 병합됐는데 OPEN·승인 전·[>]로 남은 Management goal 4건의 상태 줄을 고친다. 과거 본문은 소급 수정하지 않는다.
10. **DB 1단계 과정·의도 문서(항목 20):** 영속 연동 goal의 실제 DB 1단계(시험 DB 생성·기본 스키마 001 설치 시도)를 다루는 프로젝트 Markdown 한 편이다. 위치는 Core와 합의한 `01_Phases/goals/2026-10-04-persistence-integration/db-stage1-intent-and-process.md`다(아래 「Core 조율 결과」). 보고서 성격이라 리드가 쓰고, Core가 사실·용어를 검토하며, 독립 문서 실사를 받는다. 기록 색인이 이 문서를 가리킨다.
11. **위치 규칙 제안(항목 24):** 보고서 폴더 두 곳, Management goal 위치, 문서 지도의 운영툴 진입 제안을 메인을 거쳐 Rules에 보낸다(`msg_026b701e3728`). 직접 쓰지 않는다.

## 건드릴 곳과 소유권

| 소유자 | 파일과 책임 |
|---|---|
| Management 리드 | 이 goal과 부속 문서, `00_Document/operations/CURRENT.md`의 Management 줄, `05_Management/` README·RESUME·requirements·decisions·MCP.md, 끝난 Management goal 4건의 상태 줄, `01_Phases/goals/2026-10-04-persistence-integration/db-stage1-intent-and-process.md` 새 파일 하나(Core 동의, 병합 뒤 소유는 Core), 위임 계약·근거·Git |
| 신규 Sol | PR2: `05_Management/records/catalog.json`, `05_Management/frontend/electron/`의 catalog 계약·조회·저장 제거·원문 구간 읽기, `preload.cts`, `05_Management/frontend/src/DevelopmentRecords*`·`src/developmentRecords/`, `App.tsx` 필요 부분, `05_Management/frontend/mcp/`, 색인 검사 스크립트. PR3: 백로그 메뉴 |
| TDD 선행 시험 작성자·독립 검증자 | 해당 PR의 시험 파일만 쓴다. 제품 파일은 읽기 전용이다 |

작업자 한 명의 쓰기 종료·정산 뒤 다음 소유자에게 넘긴다. Sol의 시험 작성/수정과 commit/push는 금지다. 승인 밖 파일이 필요하면 쓰기 전에 메인에 올린다. 시스템 카드 화면과 `system-guide.json` 형식은 MCP 제공 외에는 바꾸지 않는다.

## 하지 않을 것

- 운영 백엔드·서버 등록·로그, 시스템 카드 전체 자료, 창·배율 복원(R-16), 코드 보기(R-17), 매핑 드리프트, 서버 운영 시각화, 결정 보드 MCP, 운영툴 테스트 CI 편입(묶음 2), MCP의 실제 개발 세션 연결.
- 00_Document·01_Phases·AGENTS·BACKLOG·`.agents`·Codex 설정·`.github` 직접 쓰기. 예외는 CURRENT의 Management 줄과, Core가 동의한 DB 1단계 문서 새 파일 하나다. 영속 연동 goal의 `goal.md`는 건드리지 않는다.
- `system-guide.json` 형식 변경, 구현 설명 3건의 이동.
- 무관한 기존 실패 수리. 이번 변경이 영향을 준 시험만 고치고 기존 실패는 전수 분류한다.
- 사용자 산출물(`frontend/dist`, `desktop-dist`, `.verification`)의 삭제·덮어쓰기, 전역 설정 변경, 다음 goal 자동 착수.

## 관찰 가능한 완료조건

1. catalog에 서술·상태 필드가 0개이고, 색인 검사가 끊긴 링크 0건으로 끝나며 색인에 없는 goal 목록을 출력한다. 9-30 이후 병합된 goal이 기록 색인에 있다.
2. 개발 기록 상세에서 원문 구간을 앱 안에서 읽고 읽은 checkout의 branch·HEAD가 보인다. 없는 파일·구간·크기 초과는 끊긴 링크로 표시된다. 위 메인 결정 2항의 거절 사례 8종이 모두 거절된다.
3. 화면에 편집·불러오기·저장이 없고 쓰기 IPC가 없다.
4. MCP가 색인 v2와 시스템 카드 자료를 같은 원천·같은 hash로 반환하고, 원문 구간은 등록된 출처 ID로만 읽는다. 새로 빌드한 실행본으로 SDK 시험을 통과한다.
5. 백로그 메뉴가 BACKLOG 항목을 읽기 전용으로 보이고 ID·goal 링크 어긋남(warning)과 표 형식 오류(오류)를 구분한다.
6. requirements·decisions에 두 결정이 출처와 함께 있고, README·RESUME·끝난 goal 4건의 상태 문장이 병합 사실과 맞는다.
7. nextSteps 27개가 모두 분류되고 근거가 있으며 이관 행이 메인을 거쳐 Rules에 전달됐다.
8. DB 1단계 과정·의도 문서가 저장소에 있고 기록 색인이 가리킨다.
9. 위치 규칙·문서 지도 반영안이 메인을 거쳐 Rules에 전달됐다.
10. 실제 Electron(보조 화면, 앱 125%)에서 목록→상세→원문 구간→뒤로, 끊긴 링크, 백로그 메뉴를 독립 검증자가 확인한다. 실행하지 못한 부분은 통과로 적지 않는다.

## 설계와 검증 경계

설계 원칙은 「낡을 수 있는 서술·상태는 빼고, 원문을 찾는 데 필요한 이름·분류·연결·위치만 남긴다」다. PR 번호와 병합 commit은 바뀌지 않는 사실이라 링크로 남긴다. 색인은 수기로 두고(시스템↔기록 연결은 사람 판단이 필요하다) 어긋남은 검사로 잡는다. 이는 [하네스 원칙](../../../00_Document/conventions/CODE_CONVENTION.md#하네스-원칙) 3번(수기 경로 목록의 존재·드리프트 검사)의 적용이다. 검사 정책은 warning 파일럿부터 시작하고 error 승격은 사용자 판단이다. 끊긴 링크는 실패로 낸다. 9-30 시범 완료조건 (5) 「원문 부재/오래된 색인/유효하지 않은 링크를 현재 사실로 표시하지 않음」을 화면·MCP 모두에 적용한다.

catalog의 시스템 요약 18개는 FEATURE_MAP·ARCHITECTURE의 재구성 사본이라 빼고 원문 구간 링크로 바꾼다. 구현 설명 3건과 카드 소개는 다른 문서의 사본이 아닌 유일한 원문이라 `system-guide.json`에 Management 소유로 둔다(사용자 이의 없음, 위 표). 9-30 시범의 「나머지 시스템 서술의 GameDev 의미 검토 부재」는 서술을 빼면 대상이 사라진다.

| PR | 등급 | 이유 | 선행 시험 작성자 | 독립 검증자 |
|---|---|---|---|---|
| PR1 문서 | 문서 실사 | 문서만 변경 | 해당 없음 | 신규 gpt-6-astra xhigh |
| PR2 색인 전환 | 강 | 저장소 파일을 읽는 새 경계(보안 경계), 제품+시험 50줄 이상 예상(미측정 추론) | 신규 claude-opus-5-5 | 신규 claude-opus-5-5 |
| PR3 백로그 메뉴 | 강 | 제품+시험 50줄 이상 예상(미측정 추론) | 계약 전 메인 질문(답이 없으면 신규 claude-opus-5-5) | PR2의 읽기 경계를 그대로 쓰면 신규 gpt-6-astra, 새 IPC·파일 경계가 생기면 신규 claude-opus-5-5 |

실제 줄 수는 각 PR의 `git diff --numstat`로 남긴다. 구현은 신규 `gpt-6.1-sol`(reasoning effort max)이다. TDD는 구현 전에 실패하는 요구 시험과 그 원시 결과를 먼저 두고, 시험 작성자와 구현자를 분리하며, 같은 파일의 소유를 넘기기 전 쓰기 종료를 확인한다. 구현 뒤 다른 신규 검증자가 보고·diff·실행 원문을 실사하고 필요한 시험을 보완·실행한다. 준비 단계의 실패는 제품 결함 확정 실패로 집계하지 않는다.

Electron 실제 확인은 소유 TEMP 사본·자기 프로필·자기 프로세스만 쓴다. 보조 디스플레이 identity/bounds/scaleFactor와 앱 zoom을 실행 때 관측하고 원본 출력·프로필 보존을 대조한다. OS 합성 입력/전면화는 금지다. DB·게임·Unity·WSL 서버·포트 7777은 실행하지 않는다. 구조 변경과 동작 변경은 서로 다른 커밋으로 남긴다.

낡은 상태 문장이 병합 뒤 남는 문제는 이번에는 문서 정정으로 끝낸다. 더 높은 층(형식·검사)은 묶음 3(계획 15·16번, Rules 형식)의 범위라 이 goal에서 만들지 않는다([교정 층](../../../00_Document/conventions/CODE_CONVENTION.md#교정-층과-반복-규칙)).

## PR 경계와 종료

각 PR은 최신 main의 새 branch에서 만들고, 정확한 head의 CI·메인 R-2 뒤 사용자 개별 병합 승인을 받는다. 자동 병합은 하지 않는다. 순서는 PR1 → PR2 → PR3이다. README를 PR1과 PR2가 함께 고치므로 동시에 진행하지 않는다.

| PR | 내용 |
|---|---|
| PR1 문서 | 이 goal, CURRENT Management 줄, 결정·요구 반영, 낡은 사실·상태 정정, 27개 분류표, DB 1단계 문서(조율 결과가 05 또는 Core 동의 파일일 때) |
| PR2 색인 전환 | 색인 v2·데이터 전환·새 색인 항목, 원문 구간 읽기 화면, 편집 제거, MCP 갱신·카드 자료 제공, 색인 검사, README·MCP.md의 사용 흐름 |
| PR3 백로그 메뉴 | 읽기 전용 메뉴와 ID·goal 링크 warning |

전체 병합·결과 기록 뒤 신규 `claude-opus-5-5` Gardener와 종료 기록을 하고 메인이 R-8로 넘긴다. 같은 산출물 수정이 3회를 넘으면 메인 체크포인트를 알린다. 다음 goal은 자동으로 시작하지 않는다.

## 후속 후보

- 운영툴 테스트 CI 편입과 색인 검사 연결: 계획 묶음 2(14번), Rules·CodeMap.
- 검증 실행 helper 정본화: 계획 묶음 2(11번).
- MCP 실제 세션 연결: 운영툴 라이브 1.0 이후 [결정 보드 MCP 후보](../2026-10-02-system-cards/goal.md#후속-백로그--운영툴-라이브-10-이후-결정-보드의-mcp-관리)와 함께 다음 계획에서 정한다.
- 운영 백엔드·서버 등록·로그·시스템 카드 전체 자료: 메인 계획 35번(BACKLOG 등록 예정).
- 서버 운영 시각화: [BACKLOG `server-operations-view`](../../../00_Document/operations/BACKLOG.md).

## 결과와 열린 사항

### Core 조율 결과

리드가 `msg_a942a2fbbe0f`(E/`core-db-doc-coordination.txt`)로 묻고, Core 리드가 `msg_4fa0930581dc`(2026-10-06T10:20:45Z, E/`core-db-doc-reply.json`)로 네 질문 모두 동의했다. 발신 handle `term_8cc7e174-29a4-4127-97dd-25d933159ecc`는 Core Run `run_b36cc92a4cf4`의 coordinator와 같다(E/`core-run-identity.txt`). 태그 `[GameDev Astra]`는 AGENTS Core 전환 정본에 따라 같은 파트로 인정한다.

- 위치: `01_Phases/goals/2026-10-04-persistence-integration/db-stage1-intent-and-process.md`. 다른 goal 폴더의 부속 문서 관례와 같다.
- 분담: Management가 새로 쓰고 PR1에 넣는다. Core는 사실·용어를 검토하며 병합 뒤 소유는 Core다. Core branch의 같은 폴더 `goal.md` 미커밋 수정과 겹치지 않는다. 초안이 준비되면 Core 우편함에 보내고 Core 터미널에 안내를 넣는다. Core는 지금 우편함 대기를 열지 않는다.
- 결과 서술 조건: 1단계는 Prepare·장치 확인까지 했고 SQL 서비스 시작 실패로 멈췄으며 DB 생성과 001 설치는 실행하지 않았다. master 파일 OS 오류 5는 관측된 증상으로만 쓰고 원인으로 쓰지 않는다. 10-06의 새 사실(Procmon 관측 시도, Windows Update의 SQL GDR 패치 자동 실행과 실패)은 섞지 않고 goal 링크로만 잇는다. goal.md 링크는 파일 머리나 「재개 지점」 같은 안정된 절에 건다.
- 원천: Core checkout의 로컬 `.backups/verification/2026-10-04-persistence-integration/`의 `g2-stage1-main-guide.md`(v2·patch 포함), `g2-stage1-submissions/`, `g2-stage1-execution/`, `g2-stage1-observations/`, 서비스 시작 실패 진단 `g2-service-diagnosis/`를 읽기 전용으로 대조한다. Git 제외 자료라 문서에는 「로컬 증거 경로」라고 밝힌 평문 경로로 적거나 goal의 해당 기록을 가리킨다.
- 검증 배정: 과정 기록이라 DB·영속 데이터를 바꾸는 작업이 아니다. 시범대로 신규 gpt-6-astra 문서 실사다.

### Core 사실 검토

리드가 초안(80행, SHA-256 `4790a873…`)을 `msg_0a48f37abbb2`(E/`core-db-doc-review-request.txt`)로 보냈다. Core 리드가 `msg_aae81338f70f`(2026-10-06T10:26:58Z, E/`core-db-doc-review-reply.json`)로 사실 오류 1건과 정확도 4건을 돌려줬다. 발신 handle은 위 Core coordinator와 같다. 나머지 사실은 원천과 맞다고 했다.

- 사실 오류: 안전장치 표의 `-NoProfile` 이유. 프로필 스크립트만 막고 상속 환경 변수는 막지 못한다. 원천은 Core checkout의 로컬 증거 경로 `.backups/verification/2026-10-04-persistence-integration/g2-preparation-v4/execution-plan.md`(Git 제외) 39·64행이다. 리드가 그 두 행과 대조해 고쳤다.
- 정확도: 승격 마운트 시도의 주체(메인), 모듈 경로 실패 창(비승격 PowerShell 5.1, PID 30952 → 27280), 기록 부재의 검색 범위(01_Phases·00_Document·메인 메모리 0건), 2단계 범위(Complete·U-01·정리).
- 다섯 곳 모두 반영했다. Core는 문서 실사 검증자가 `-NoProfile` 문장을 같은 두 행과 다시 대조해 달라고 했다.

### 「다음 할 일」 27개 분류

[분류표](next-steps-classification.md)를 원천 재대조로 확정했다. 로드맵 연결 10, 폐기(변경 규칙 사본) 8, 폐기(조건부 지침) 1(#13, 1차 실사 W4로 옮김), 폐기(사용자 결정) 1, 완료 삭제 3, BACKLOG 이관(사용자 결정) 2(한 묶음), 나눔 1, 일부 완료+이관 1이다. 초안의 애매 3건 중 평가 실행 여부(#16)는 기준선 goal의 실행·채점 기록이 있어 완료 삭제로 바꿨다. 처음 확정 때 BACKLOG 이관 행은 `management-launcher-real-run` 하나였고, 결정 요청 후보는 실제 원격 플레이 확인과 시스템 지도 3D 표현 두 건이었다.

리드가 이관 행과 결정 요청 후보를 `msg_33365ac784b2`(E/`nextsteps-transfer-and-decisions.txt`)로 메인에 보냈다. 먼저 `decision_gate` 유형으로 보낸 같은 본문은 「No active Dispatch belongs to this message sender」 오류를 받았지만 `msg_54535201beaa`로 도착했다(E/`nextsteps-transfer-and-decisions-rejected.json`). 메인은 `msg_b1bf869e87fd`(2026-10-06T10:38:19Z, E/`wait-7.json`)로 받았다. 결정 요청 후보 2건은 현황판 결정 대기에 올렸고, 사용자 답이 올 때까지 두 행은 「애매」로 둔다. 이관 행은 다음 Rules goal 입력에 넣었다.

사용자는 두 건 모두 BACKLOG 이관을 골랐다(위 「적용 중인 사용자 결정」). 문서 실사를 띄우기 전이라 분류표에 바로 반영하고 이관 행 `remote-play-check`·`system-map-3d`를 더했다. 두 행은 메인을 거쳐 Rules에 보낸다.

### PR1 문서 실사

- **1차(차단):** 신규 `[Management 검증자]`(지정 gpt-6-astra xhigh, 관찰 화면 「GPT-6-Astra xhigh」, Codex v0.160.1, backend unknown)가 고정 HEAD `3fc61360`(13파일 +427/-23)을 실사했다. Task `task_540f21cc5321`, Dispatch `ctx_cd666930972a`, worker_done `msg_942f5fb8060c`(2026-10-06T11:00:54Z). 판정 원문은 E/`pr1-doc-review/verdict.md`(SHA-256 `2598e31a…`)다.
- 차단 네 건과 정정:
  - W1: DB 문서가 실행 당일 관측(`Attached=False`)을 「평소 분리」로, 메인의 제한된 검색을 「어디에도」로 넓혔다. 관측 범위대로 고쳤다.
  - W2: DB 문서가 「이후 블록마다 같은 guard를 반복한다」고 썼으나 장치 확인·관측 종류 선택·기록 종료 블록에는 guard가 없다. guard를 부르는 블록과 부르지 않는 블록을 나누고, 실제 통과는 준비·서비스 시작 블록으로 한정했다.
  - W3: 분류표 이관 행 `management-launcher-real-run`의 출처 칸에 전달자·날짜·msg ID가 없었다. BACKLOG 「항목 계약」대로 채웠다.
  - W4: #13의 근거(protocol.md 30행)는 범위 제외 문장이라 같은 변경 규칙이 아니다. 같은 규칙이 정본에 없음을 확인하고 새 분류 「폐기(조건부 지침)」로 옮겼다. 이 처분은 리드 판단이다.
- 비차단 Q1(#20 근거의 Game Dev 의미 검토 범위)은 「전투 서술」로 좁혔다. Q2(재개 지점의 「아직 검증자는 없다」)는 1차 정정에서 문장을 바꿨으나 2차 실사에서 부분 해소로 판정됐다(아래 X1).
- 함께 반영한 메인 결정: 검증자 질문(`msg_6d3e4c6f8aa0`)에 계획 35번 원천을 답하다가(`msg_2e2464a0fae4`), 이관 행 `management-launcher-real-run`이 메인 계획 35번의 「실행 배치 실물 검증」과 겹침을 찾았다. 리드가 메인에 물었고(`msg_bd113ff74c06`), 메인이 「B 별도 행 유지」로 답했다(`msg_da1c987f7ff0`, 메인의 운영 판단). 분류표 #25 근거에 그 대조 결과를 적었다.
- **2차(좁힌 재실사, 차단):** 신규 `[Management 검증자]`(지정 gpt-6-astra xhigh, 관찰 화면 「GPT-6-Astra xhigh」, Codex v0.160.1, backend unknown)가 정정 delta `3fc61360..3f028104`(3파일 +26/-12)를 실사했다. Task `task_dc807430e86e`, Dispatch `ctx_3b823d43245e`, worker_done `msg_dddd11e439b3`(2026-10-06T11:19:08Z). 판정 원문은 E/`pr1-recheck/verdict.md`(SHA-256 `15993050…`)다. W1~W4·Q1은 해소, Q2는 부분 해소였다.
  - X1(차단): 「재개 지점」의 현재형 상태와 다음 순서에 기록 시점이 없어, PR 생성·병합 뒤 읽으면 아직 재실사를 준비하는 상태로 읽힌다. 첫 문단을 기록 시점의 상태와 당시 예정으로 한정하고, 뒤의 진행은 진척 단계·이 절·리드 갱신을 따른다고 적었다.
  - X2(비차단): 재실사 계약이 메인 결정 `msg_da1c987f7ff0`의 원시를 E/`wait-12.json`으로 잘못 적었다(실제 E/`wait-11.json`). 리드가 `msg_9b1387d94527`로 정정했고 저장소 문서에는 영향이 없다.
  - O1(비차단 관찰): DB 문서 32행 「각자 필요한 확인만 한다」를 실제 행위(장치 확인·관측 종류 지정·기록 종료)로 구체화했다.
- 검증자 절차 한계: 1차 판정 집필 구간의 heartbeat 간격이 5분을 넘었다고 검증자가 판정에 스스로 적었다. 문서 결함과 구분한다.

### PR196 병합

- **3차(좁힌 재실사, 통과):** 신규 `[Management 검증자]`(지정 gpt-6-astra xhigh, 관찰 화면 「GPT-6-Astra xhigh」, backend unknown)가 delta `3f028104..562a0c78`을 통과로 판정했다. X1·Q2·O1은 해소됐고 새 결함은 없었다. Task `task_78855f959ba5`, worker_done `msg_2853f48e37e2`(2026-10-06T11:33:44Z), 판정 원문은 E/`pr1-recheck2/verdict.md`(SHA-256 `4ee5ae46…`)다.
- **PR 생성과 CI:** 리드가 메인에 알린 뒤(`msg_a99807425be9`) PR196을 만들었다(2026-10-06T11:36:21Z). head `562a0c78b781f157e7403899cfaf50ed33378004`에서 module-boundaries·code-rules·architecture-tests·dotnet-tests 4개가 모두 success였다(E/`pr1-ci-final.txt`).
- **승인과 병합:** 리드의 승인 묶음(`msg_581e68a420af`) 뒤 메인 `msg_f6dba519f08c`가 사용자 원문 「대시보드 결정 응답: 1) PR196 - 운영툴 기록 원본 일원화 문서 병합 승인 → A 지금 head로 병합 (head 562a0c78b781f157e7403899cfaf50ed33378004)」을 전달했다(메인 전달, 사용자 직접 입력으로 격상하지 않음). 리드가 직전 head를 다시 조회하고 `gh pr merge 196 --merge --match-head-commit 562a0c78…`로 병합했다. 병합 commit `a47a02765c87d9794933f461a9c71ac7d4369d51`, 2026-10-06T13:48:22Z다(E/`pr1-post-merge-state.json`).
- **Orca 재시작:** PR196 승인 대기 중 Orca가 재시작돼 리드 탭이 복구되지 않았다. 메인이 리드 대화 기록을 `claude --resume`으로 새 탭에 다시 열었고(`msg_92dcde92b30d`), 리드가 run-use로 Run을 다시 묶었다. 이전 세션의 CI 감시와 우편함 대기는 재시작으로 끝났다.

### 진입과 준비

- **진입:** 새 Run `run_3fa510a50602`(E0/`run-create.json`), READY `msg_fa27c1982e46`(E0/`ready-sent.json`). 진입 때 worktree는 종료 기록 branch `docs/management-record-navigation-closeout`(HEAD `e37f261`, origin/main보다 19 뒤, 미커밋 0)이었다.
- **27개 분류 초안:** 승인 전 읽기 조사로 E0/`nextsteps-classification-draft.md`를 만들었다. 확정 전 원천을 다시 대조한다.
- **열린 질문:** PR3 시험 작성자 모델(PR3 계약 전 메인). DB 1단계 문서 위치는 Core 조율로, 27개 중 결정 요청 후보 2건은 사용자 결정으로 닫혔다.
