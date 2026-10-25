# 구현자 배정 정본 정렬과 검증 계약 후보 연결

## 재개 지점

**다음 첫 일은 문서 head를 고정한 뒤 병합 전 Opus Gardener를 발행하는 것이다.** 상태는 **독립 NOT PASS 1회 + 메인 한정 처분**이다. 메인 `msg_a98913eb5019`가 승인7문서의 다음 단계 진행을 허용했고 `msg_ccf55ac3c1ff`가 R03 신규 실사 금지를 확정했다. 원판정/91입력·61산출물은 보존한다. Gardener·종료 기록 재실사·PR·사용자 정확 head 병합 승인은 아직이다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: `docs/implementer-routing-canon-20261025`; 최신 origin/main에서 분기한 base/HEAD `d16bd388dcb2650559d9857a6c8be305df7ce169`.
- 사전 맥락: 로컬 Git 제외 `.backups/verification/2026-10-25-implementer-routing-canon/lead-context-v1.md`. 원천 실제 bytes/SHA·시계·허용 경계는 그 메모에 둔다.
- 승인 전 초안은 E/`scope-draft-v1.md`에23582B/SHA256 `4D6A6A923641FA0574E626808303BACF5E2B1D00649D1351D362CD1F71DACC57` 그대로 보존했다. E는 위 로컬 근거 root다. 승인 뒤 메모는 E/`lead-approved-context-v1.md`, 승인 수신은 E/`approval-inbox-v1.json`이다. 이전 goal의 Run/Task/Dispatch는 사용하지 않는다.
- 현재 Run은 `run_372d7a4244be`, 회신 주소 `run:run_372d7a4244be`다. 작성자 신원·Task/Dispatch·실제 pending 상태는 E/lead/implementation-start-v1.json 및 implementation-start-unknown-show-v1.json을 읽는다. 리드 같은 대화 ID는 `01a17287-9b3b-7560-a86d-92dc82021c89`이며 새 복구 때 현재 runtime/handle을 다시 대조한다.
- 위 첫 작성자는 기동 정산/종료한 역사 시도다. 현재 신규02의 Task/Dispatch/신원은 E/lead/implementation-02-start-v1.json, 실제 제출·모델 설정은 implementation-02-submission-audit-v1.json과 thread-settings-v1.json을 읽는다. 발행 계약은 E/contracts/implementation-02-v1.md, 승인 입력은 E/inputs/input-pins-02-v1.json이다.

## 진척 단계

- [x] 원천 대조
- [x] 범위 승인
- [x] 계약 준비
- [x] 작성자 기동
- [x] 정본 작성
- [x] 독립 실사 — NOT PASS 1회, 메인 한정 처분으로 다음 단계
- [>] 종료 점검
- [ ] 문서 PR 제출

실제 PR 번호가 생기면 병합 단계는 `PR번호 병합`으로 적는다. 초안 작성 완료를 독립 통과나 목표 완료로 표시하지 않는다.

## 요구사항 원천과 확인 수준

1. 메인 전달 `msg_4f48b81630e9`(2026-10-25T10:09:36Z)의 사용자 원문은 「대시보드 결정 응답: 1) Rules 다음 일 - 구현자 배정 정본 맞추기를 지금 할지 → A 지금 좁은 정본 정리 goal」다. 메인은 사용자 메인 pane Enter 제출 시각을 19:08 KST로 전달했다. Rules pane의 사용자 직접 입력으로 격상하지 않는다.
2. A의 정확한 범위: 「메인이 Rules 리드를 새로 열고 범위 초안을 받아 너에게 승인을 받는다. 범위는 구현자 배정 정본을 10-19 결정(구현자 Sol max, Astra는 같은 계약 두 번째 실패만)에 맞추는 것과, 검증 계약 허용 목록에 표준 읽기 전용 기동 묶음을 넣는 후보 연결이다. 문서 PR 하나로 끝낸다.」 이는 **초안 작성 허용**이며 구현/문서 착수 승인은 아직 아니다.
3. 10-19 원천은 **Level worktree** `01_Phases/goals/2026-10-24-level-redesign/goal.md:175~236`의 「10-19 라우팅·사용량 결정 원문과 이번 적용」이다. 메인이 부른 「10-19 모델 라우팅 전달 원문」과 같은 블록을 실제 heading으로 확인했다. 다른 worktree의 로컬 파일이므로 이 checkout 상대 링크에 존재한다고 가정하지 않는다. 원문 전달은 Level의 Main `msg_f5284eba4275`와 `E/lead-entry/run-mail-33.json`으로 연결된다.
4. 블록의 최초 사용자 원문: 「OK A로 진행하되, 지금 워크트리로 규칙이 사본 형식 느낌으로 분산되어있어서 전부 적용하려면 시간이 좀 걸릴꺼 같은데」. 근거는 `C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-19/scripts/update-decision-routing-bundle-1400.cjs`다. 이 스크립트는 당시 선택지/전후표이며 실행하지 않았다. 후속 사용자 결정으로 바뀐 크레딧 내용을 최초 A 숫자로 되돌리지 않는다.
5. Core 이탈 근거와 Art 후보 원천은 아래 절에 분리한다. 정본 기준은 base commit의 현재 AGENTS·ORCA·스킬·양식이다. 과거 goal/고정 Git 기록·후보 출처 문장은 역사 자료이며 일괄 교체 대상이 아니다.

## 적용 중인 사용자 결정

메인 `msg_bd99d237b5ec`(2026-10-25T10:54:27Z)은 다음 두 사용자 Enter 제출 선택을 전달했다. 사용자 제출 시각의 `19:5x KST`는 메인 전언 그대로이며 Rules가 정확 분을 추정하지 않는다.

> 대시보드 결정 응답: 1) 계획 검토 - Rules 구현자 배정 정본 정렬과 검증 계약 후보 연결 → A 승인 (초안 msg_f38b17ac26a5) · 2) Rules 범위 추가 - 검증자 배정도 같은 PR에서 10-19 결정에 맞출지 → A 같은 PR에 넣기

2의 A 원문은 「구현자 배정과 함께 검증자 배정 문구도 10-19 결정대로 맞춘다. 위험 신호와 DB 쪽 검증은 Opus, 그 밖의 문서·강약 검증은 Sol xhigh다. 같은 문서 PR이 조금 커진다.」이며 같은 결정에 「검증 강도·차단 규칙·구현자와 검증자 분리는 바꾸지 않는다. 모델 이름만 맞춘다.」가 있다.

메인은 승인된 초안23582B/hash 일치와 **기본 포함 + 표11~13 및 기동 예시**를 대조했다. 위험 신호/DB 등 Opus의 `--autocompact 250000`, 그 밖의 독립 Sol xhigh를 포함한다. 표14의 기존 예외·강도·차단은 삭제하지 않는다. Gardener/Fable 배정·사용량/한시 예산/파트 우선순위/Opus 시각은 제외다.

같은 메시지의 다음은 **메인 판단**이며 별도 사용자 결정으로 격상하지 않는다.

1. 한 문서 PR 지시에 맞춰 head 고정 PR의 병합 전 Gardener와 결과/종료 기록을 같은 PR에 넣는다. 병합 뒤 tracked 수정이 필요하면 먼저 메인에 보고한다.
2. 이번 문서 실사는 신규 `gpt-6.1-sol` xhigh, 작성자는 신규 `gpt-6.1-sol` max·service_tier fast다. priority 관측은 rollout `thread_settings`에서 확인한다. 첫 화면/실제 backend 모델/요청 설정을 구분한다.
3. Content C9 창을 사용하지 않는 문서 작업이며 Unity·WSL·DB·dotnet은 실행하지 않는다. Run 생성 직후 메인에 회신 주소를 알린다.

메인 범위 판정 `msg_4b0292210393`(2026-10-25T10:58:08Z)은 task-context/SKILL.md의 일반 검증자 고정 「독립 Opus/신규 Opus」 호칭 동기화를 **이미 승인된 범위 안 결함 수정**으로 판정했다. 모델·역할 호칭만 goal-loop 검증자 라우팅 참조로 바꾸며, 검증 강도·현실 시나리오·통과 차단/측정값 위장/제품 계산 복제 차단은 한 글자도 바꾸지 않는다. 위험 신호/DB 등의 Opus 배정은 유지한다. 이 조건 원문을 작성자·검증자 계약에 붙이고 실제 diff가 호칭 밖으로 번지지 않았는지 판정한다.

## 10-19 블록과 현행 정본의 전수 비교

여기서 전수는 **지정 전달 블록의 항목 전체**와 관련 현재 운영 정본/스킬의 대응 관계다. 전체 저장소·모든 과거 goal·실제 런타임 전수를 감사했다는 뜻이 아니다. 아래 표는 승인 초안의 비교표로 보존한다. 「기본 포함」과 표11~13의 「추가 승인」은 아래 적용 결정으로 모두 승인됐다. 그 밖의 제외/보존 경계는 유지한다.

| 번호 - 항목 | 10-19 전달 블록 | 현재 정본 위치·차이 | 이번 제안·이유 |
|---|---|---|---|
| 1 - 기본 구현자 | gpt-6.1-sol **max** | implementer-routing:3·21, AGENTS:35·48은 Sol xhigh | **기본 포함**. 모델/effort와 기본 조건을 정렬 |
| 2 - 위험 신호 구현자 | 셸·보안·입력/시간·순서·동시성·수명/런타임·언어 경계도 Sol max | routing:3·13~15의 신호1~3은 Astra 배정 | **기본 포함**. 이 신호만으로 Astra를 고르는 경로를 없앰; 위험의 기술적 의미는 지우지 않음 |
| 3 - 새 다파일·애매함 | 설계 판단이 남은 새 다파일 신호와 「애매하면 Astra」를 쓰지 않음 | routing:3·16은 둘 다 Astra 근거 | **기본 포함**. 두 자동 배정 조건 폐기, 역사 근거 보존 |
| 4 - 두 번째 독립 불합격 | 같은 계약 두 번째 독립 불합격 뒤 다음 새 구현자는 Astra xhigh | routing:17~19 신호5의 기준/절차·계약귀속 제외는 이미 있음 | **기본 포함**. 이 승격과 조건·제외를 분명히 유지. 결함 번호 무관 집계와 동일 번호 확정 실패3회는 구분 |
| 5 - 기동 명령 | 구현자는 Sol max, 리드는 xhigh 유지 | ORCA R-5:170의 Sol 명령이 xhigh | **기본 포함**. 역할별 명령을 구분하고 리드/R-8 resume는 보존 |
| 6 - effort 확인 | 첫 화면에 max가 없으면 대체하지 않고 메인 보고; 지정/관측을 계약에 기록 | AGENTS:38/R-5는 모델 지정/화면/backend unknown 구분; max의 관측 조건은 없음 | **기본 포함**. max 부재 처리와 지정 모델·effort를 기존 기록에 연결 |
| 7 - capacity | Sol 30분 실패 뒤 새 Astra xhigh, Claude 첫 화면 처리 유지 | ORCA:179는 Sol **xhigh**라고 고정; AGENTS:38은 capacity 링크 | **기본 포함**. Sol max 명칭만 일치시키고 1→2→5→10분/30분·정산·새 세션·검증자 대체 금지 보존. 「두 번째만」의 기존 capacity 예외를 지우지 않음 |
| 8 - 계약 배정 근거 | 모델·effort·정확 명령·화면값을 기록 | routing:25와 task-context templates:53은 「신호 번호 또는 해당 없음」; AGENTS:38은 노력 표기 부족 | **기본 포함**. Sol max 기본/두 번째 불합격의 실제 근거/capacity 예외를 기록하는 방식으로 연결 |
| 9 - 현행 요약 문구 | 현재 구현자 max | goal-loop:50·ORCA R-7:220의 「기본배정·Sol xhigh」가 구현자까지 가리킬 수 있음 | **기본 포함**. 구현자 정본 링크로 연결/역할 명확화만. 검증 강도·R-7 절차 변경 없음 |
| 10 - 파트 리드 | Core 포함 Sol xhigh | AGENTS:35·48, ORCA R-1/R-8과 일치 | **제외·현행 보존**. 리드 모델/문맥 설정을 바꿀 이유 없음 |
| 11 - 위험 신호 독립 검증 | Opus 5.5, --autocompact 250000 | goal-loop:58~61은 일반 Astra/DB·보안 Opus, 시간·수명·일반 언어 경계 Opus 연결이 없음. ORCA R-5도 옛 예시 | **추가 승인 권고**. 구현자를 위험 신호로 Astra에 보내던 경로와 검증 경로의 구분을 완결하려면 필요하나 A의 중심보다 넓음 |
| 12 - DB 등 독립 검증 | DB·영속·프로토콜·DLL·보안은 Opus 5.5 + 250000 | Opus 모델은 일치, goal-loop/R-5의 기동 예시에 압축 옵션 없음 | **추가 승인 권고**. 이번에는 문구 정렬 여부만 제안; DB 실행/압축 설정 실제 변경은 없음 |
| 13 - 일반 독립 검증 | 문서·강/약·로컬 DB 카드 = 새 Sol xhigh | goal-loop:58·61은 Astra xhigh, R-5도 Astra/Opus 예시 | **추가 승인 권고**. 다른 새 Sol 검증 허용을 해당 시범 문단에 동기화. 실제 이번 문서 실사 배정은 이미 적용 중인 10-19를 따름 |
| 14 - 검증 예외와 역할 | 구현/검증 분리·하나 뒤 종료·생략/리드 구현 금지 유지; 카드 누락 결함1회면 Opus 복귀 | AGENTS/goal-loop와 해당 보존 규칙은 일치. Astra 구현→Opus라는 기존 예외는 10-19 블록에서 별도 폐기하지 않음 | **기본 보존**, 모델 동기화 문구는 추가 승인 항목. 기존 예외·강도/차단을 조용히 삭제하지 않음 |
| 15 - 선행 시험·Gardener | 신규 Opus 5.5, 변화 없음 | AGENTS:35와 ORCA Gardener 기본 및 goal-loop 평가 대상으로 유지 | **제외·현행 보존**. 배정 변경 없음; 이번 종료 Gardener 면제도 아님 |
| 16 - Fable 자문·설계 시범 | Fable 5.1, 변화 없음 | ORCA 확정 실패3회/R-7 기본과 일치. 기존 Advisor의 사유 있는 Astra 대안도 있음 | **제외·현행 보존**. 이번 결정이 기존 대안을 폐기한다는 근거 없음; 실패3회 규칙 재설계 없음 |
| 17 - 사용량 지표·속도 | 주간 플랜 기준 약14%p/일, 효과도 주간 환산으로만 평가; max 세션 예200 초과 보고 | 현재 routing:27은 회차·결함·절차 실패 기록만, 주간 pacing/보고 기준 없음 | **제외·별도 후보 검토**. 측정/메인 운영 범위. 당시 예시를 새 실측·보편 상한으로 만들지 않음 |
| 18 - 비상 크레딧 | 후속 결정으로 평소 비상만; 당시 10-25 05:40까지 비상 사용, 상한18000·16000 재질문 | 원 CJS의 A 절반/하루3500과 후속 블록 내용이 다름; 정본에 현재 한시 예산 없음 | **제외·역사 보존**. 이미 지난 리셋 구간을 현재 예산 허가로 재발행하지 않음 |
| 19 - 우선순위·두 트랙 | 당시 Core→World, Content, Art, Rules/Management 뒤; 메인이 보류/재개 | 현재 R-1은 배치 정본, 별도 실시간 순서/현황은 메인·각 goal | **제외**. 한시 우선순위이며 이번 Rules 지금 착수 결정이 따로 옴. 다른 파트 스케줄/goal은 수정하지 않음 |
| 20 - Opus 시각 제한 | 2026-10-20 20:00 전 새 Opus 금지 | 현재 정본에 해당 과거 시각 제한 없음 | **제외·역사 보존**. 경과한 제한을 재활성화하지 않음 |
| 21 - 전환·각 리드 기록 | 사용자 결정은 정본 병합을 기다리지 않고 적용; 각 goal에 원문/출처·계약/화면 기록; 타 worktree 정본을 임의 수정하지 않음 | AGENTS는 10-14만 현재 결정처럼 연결, routing:5도 10-14 신호 유지 | **기본 포함**은 이번 구현자 정본의 10-19 원천 연결/범위. 타 파트 goal·배포/런타임 전환은 제외 |

적용 기준 후보: 일반·위험 작업은 Sol max이고, Astra xhigh는 **같은 계약에서 Sol 구현의 두 번째 독립 불합격 뒤 다음 신규 수정 시도**에 배정한다. 별도로 이미 승인된 capacity 장기 실패 예외를 유지한다. 절차 이탈/계약·명세 귀속 결함 제외, 같은 번호 실패3회 Advisor, 새 세션·기록·메인 판단 조건은 현행대로다. 성능이 더 좋다는 새 측정 결론은 없다.

## 범위

### 만들 것

1. 위 표의 **기본 포함** 구현자 항목을 현행 운영 문서·계약 양식에 정렬한다. 구현자/리드 effort를 명확히 나누고, 10-19 적용 원천과 기존 예외/실패 집계를 연결한다.
2. BACKLOG에 아래 **표준 읽기 전용 기동 묶음 후보 한 행**을 채택 전 상태로 연결한다. 공용 허용 목록·hook·guard·실행 면제는 만들지 않는다.
3. 승인된 범위·원문/실제 diff·외부 작성/독립 문서 실사·Gardener·미실행 경계를 이 goal과 로컬 근거에 연결한다. 추가 승인된 표11~13의 검증 배정 동기화와 기동 예시도 같은 PR에 포함한다.

### 건드릴 곳

| 파일 | 승인 뒤 허용할 최소 변경 |
|---|---|
| `AGENTS.md` | 모델 라우팅/10-14 전환 문단의 **현재 구현자** 부분과 10-19 원천 연결. 리드 xhigh/병합 권한 보존 |
| `.agents/skills/dawnholder-goal-loop/references/implementer-routing.md` | 기본·조건·예외/원천·계약 기록 정렬. 과거 조사 근거는 당시 관측임을 유지 |
| `00_Document/operations/ORCA.md` | R-5 구현자 명령, capacity의 구현자 effort 명칭, R-7의 기본배정 참조만. 검증자 명령 동기화는 추가 승인 때만 |
| `.agents/skills/dawnholder-goal-loop/SKILL.md` | 기본배정 모호성/라우팅 링크 최소 정렬. 검증자 시범 문단의 모델 동기화는 추가 승인 때만 |
| `.agents/skills/dawnholder-task-context/references/templates.md` | 구현자 배정 근거 한 필드의 조건·effort/관측 연결. 도구 허용 목록 신설 없음 |
| `.agents/skills/dawnholder-task-context/SKILL.md` | 메인 범위 판정 msg_4b0292210393에 따라 일반 검증자 고정 모델/역할 호칭만 라우팅 참조로 연결. 검증 강도·현실 시나리오·차단/측정값 위장/제품 계산 복제 차단 본문은 변경하지 않음 |
| `00_Document/operations/BACKLOG.md` | 새 안정적 후보 ID 한 행. 기존 후보의 과거 신호 출처/선행 조건을 일괄 변경하지 않음 |
| `00_Document/operations/CURRENT.md` | 승인 뒤 Rules 링크/branch만. 타 파트 줄 보존 |
| 이 `goal.md` | 승인 원천·범위·상태·결과. 필요 시 같은 goal의 종료 Gardener 문서만 추가 승인 범위로 연결 |

로컬 evidence/새 계약/보고/원시·허용 TEMP는 **착수 승인 뒤** 이 goal의 근거 root 안 역할별 경로로 고정한다. 지금 허용한 두 파일만 쓰는 단계에서 넓히지 않는다. CLAUDE는 기본 변경 대상이 아니며 사용자 선택 없이 메인 파일을 대신 쓰지 않는다.

### 하지 않을 것

- 제품 코드·테스트·생성물·에셋·설정·hook·guard, 자동 모델 선택/비용 측정 도구를 바꾸거나 실행하지 않는다.
- 다른 파트의 goal·계약·파일·미커밋 변경을 고치거나 과거 판정·배정·원시를 소급 수정하지 않는다. 다른 worktree에 정본을 일괄 복사하지 않는다.
- Unity·WSL·DB·서버·dotnet 실행, 서비스·프로세스 중지, 전역 설정/권한 변경을 하지 않는다.
- 위험 신호만으로 Astra 승격, 추가 실패 조건, 새 Advisor/R-7 범위, 읽기 실행 전면 면제를 만들지 않는다.
- 10-19 당시 비용/사용량·리셋 전 예산·최대2트랙·Opus 시각 제한을 현재 운영 예산/스케줄로 재승인하지 않는다. fast는 별도 결정이므로 이 goal의 변경 대상이 아니다.

### 관찰 가능한 완료조건

1. 실제 diff의 모든 현재 구현자 안내가 Sol max 기본과 두 번째 독립 불합격 조건에 일치한다. 리드 xhigh·Astra xhigh·capacity 예외·절차/계약 귀속 제외·동일 번호 실패3회 조건·권한이 보존된다.
2. 10-19 원천을 이 goal에서 추적하고, 현재 정본에서 이 goal/단일 구현자 라우팅을 따라 찾을 수 있다. 과거 조사/goal·한시 사용자 수치를 현재 재측정으로 쓰지 않는다.
3. BACKLOG 새 행은 출처 세 메시지·Art 계약59행·담당 후보·선행 사용자 판단·상태 `대기`를 갖춘다. 후보만으로 허용 목록이 확대되지 않는다.
4. 승인 범위에 맞춘 신규 외부 작성자의 쓰기 종료 뒤 **신규 독립 검증자**가 내용·참조·권한·실제 diff·사전 메모/규칙 원문/실제 준수 위치를 문서 실사한다. 스킬/양식 변경은 현실적 계약 작성 시나리오를 대조한다. 실제 파일/줄 수 원시와 미실행을 기록한다.
5. 메인이 최종 판정 원문과 R-2 원천을 대조한다. 문서 정렬이 기존 runtime 모델 전환·자동 준수·실제 게임 실행 성공을 증명한다고 보고하지 않는다.
6. 문서 PR은 **하나**이며 현재 head의 사용자 명시 병합 승인은 메인만 받는다. 한 PR에 종료 기록/Gardener를 포함하는 방식은 아래 승인 항목을 먼저 확정한다. Codex는 병합/자동 병합 예약을 하지 않는다.

## Core 이탈과 교정 층

읽기 원천 C는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/core-active/.backups/verification/2026-10-24-character-storage-restart-load/joint-runtime-02`다.

| 번호 - 근거 | 직접 읽은 사실 | 이번 해석·보존 |
|---|---|---|
| 1 - execution-04 계약 | `implementation-contract-v1.md:7`은 JSON/PS 타입·직렬화 경계 신호3만으로 신규 Astra xhigh. 이전 Sol FAILED1건을 연결 | 정본의 옛 신호를 배정에 사용한 사례. Rules가 실제 실행을 재검증한 것은 아님 |
| 2 - execution-05 계약 | `implementation-contract-v1.md:5`는 동시성·process/stream 수명·PS/.NET 신호2·3으로 신규 Astra xhigh. OBS-RETURN-01 수용 실패1 | 두 번째 독립 불합격 조건 충족으로 볼 수 없음 |
| 3 - 메인 교정 | `execution-05/main-routing-correction-authority-v1.json`의 `msg_fa1a619957e4`(2026-10-25T09:56:15Z). 발신자는 현재 메인. E01~03은 Sol max, E04/E05 불일치를 표본 대조하고 E05 쓰기 종료·정산 후 새 Sol max를 지시 | 메인 판단이며 새 사용자 결정이 아님. E04 판정/원시를 무효로 돌리지 말라는 경계도 유지 |

교정 층은 **현재 모델 배정 정본과 기동 예시의 문서 층**을 우선 고치는 안이다. 같은 옛 신호 사용이 두 번 나왔다는 메인 원문을 연결한다. 자동 도구가 판단할 독립 불합격/계약 귀속/실제 세션 상태를 이 문서 goal만으로 안전하게 강제할 수 없고 설정·가드는 승인 범위 밖이다. 미래 자동 선택·hook이 필요하면 별도 구체안/사용자 판단이 필요하다. 정본의 낡은 문구가 원인이라는 설명은 메인 추정과 관측된 배정 기록을 구분하며 인과를 독립 확정하지 않는다.

## Art 후보 연결

원천 A는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/art-active/.backups/verification/2026-10-28-stone-terrain-environment-replacement`다.

- `msg_b7c38a869b7a`(09:31:12Z)는 보고v2 검증자의 목록 밖 도구/신호/base64 이탈을 보존하고, v2 차단/기존 판정 유효와 새 v3 실사를 구분한 **메인 판단**이다.
- `msg_4d70e6e605cb`(09:49:50Z)는 v3 계약59행 B가 cmdlet을 허용하면서 `env -u PSModulePath pwsh -NoProfile -Command`와 console UTF-8 준비를 빠뜨린 점을 직접 표본 대조했다. 검증자 목록 미대조와 리드 계약 누락을 구분한다. 네 형식의 **해당 계약 남은 단계 보충**과 공용 Rules 후보 이관을 구분했다.
- `msg_53a9c731aefe`(10:02:00Z)는 추가 tee/grep/basename을 기존 한정 처분에 넣고 교정 전/교정 수신 뒤~보충 ACK 전/보충 ACK 뒤를 나눴다. 소급 면제나 공용 허용 채택이 아니다.
- 후속 메인 `msg_825e2dc63467`(10:15:16Z)은 Art 리드 `msg_c407d34cd265`의 보충 수신 뒤 `tee /dev/null` 리다이렉트1회·프로세스 치환 `<(...)`1회를 후보 근거로 전달했다. 이 두 실행을 Rules가 독립 재현/전수 감사한 것은 아니다. 도구 이름 열거와 **효과 기준(읽기 전용·자기 raw 아래 쓰기만) 허용**의 비교를 채택 전 질문으로 더하며 어느 쪽도 채택하지 않는다. 기존 보충 뒤 재발이라는 시점과 개별 메인 처분을 보존한다.
- 실제 v3 계약59행과 `contracts/static-preview-report-independent-review-v3-runtime-supplement-v1.md`를 읽었다. 네 형식은 해당 pwsh 기동·그 자식 console UTF-8·git branch --show-current·Get-Command/$PSVersionTable 읽기다. 그대로 전 파트에 허용할지, 그 밖의 도구를 넣을지는 이번에 결정하지 않는다.

승인 뒤 BACKLOG에 넣을 행의 초안:

| ID | 제목 | 이유 | 출처 | 선행 조건 | 담당 후보 | 상태 |
|---|---|---|---|---|---|---|
| `verification-readonly-bootstrap-bundle` | 검증 계약 허용 목록의 표준 읽기 전용 기동 묶음 | Art 보고 v2·v3의 목록 밖 읽기 호출이 반복됐다. v3는 허용 cmdlet의 기동/인코딩 누락도 있었으며, 보충 뒤 tee 리다이렉트·프로세스 치환 이탈이 추가 전달됐다. 검증자 이탈과 계약 누락을 구분한다. | 메인→Rules `msg_4f48b81630e9`·후속 `msg_825e2dc63467`(2026-10-25); 원 판단 `msg_b7c38a869b7a`·`msg_4d70e6e605cb`·`msg_53a9c731aefe`, Art 계약59행/한정 보충 및 리드 `msg_c407d34cd265`의 메인 재전달. 사용자 채택 아님 | 도구 이름 열거 vs 효과 기준(읽기 전용·자기 raw 아래 쓰기만) 허용을 비교하는 채택 전 질문. 실제 기동/읽기·새 runtime·쓰기/원시·구문/부작용 판정·남은 단계 보충/기존 관문을 구체화한 뒤 사용자 별도 채택. 현행 개별 계약/금지·과거 이탈 유지 | Rules·Art·메인 | 대기 |

## 독립 검증과 역할

- 등급은 **문서 실사**다. 코드 변경이 없어 강/약 코드 시험·TDD·게임 실행은 해당 없음이다. 실제 변경 파일/줄 수는 승인 뒤 diff의 원시로 계약에 적는다.
- 승인 뒤 구현 문서 작성은 신규 외부 Sol max 세션 한 작업에 맡긴다. 리드는 goal·계약·통합/Git을 소유하며 구현자와 검증자를 분리한다. 범위 초안 작성은 리드의 설계 업무다.
- 이번 문서 실사 신규 검증자는 10-19 우선 적용 블록의 **그 밖의 독립 검증 = gpt-6.1-sol xhigh**를 제안한다. 현재 스킬 기본 Astra와 task-context의 신규 Opus 표현은 상위 사용자 결정과 충돌하는 부분으로 계약에 근거를 명시한다. 모델 문구를 이번 PR에서 동기화할지는 별도 범위 선택이다. 스킬/양식의 현실적 시나리오 대조 요구 자체는 유지한다.
- 시나리오는 일반 문서 작업, 위험 신호 구현자 선택, 같은 계약 독립 불합격1회/2회·절차/계약귀속 제외, capacity 예외, 리드 xhigh·실제 모델 미확인의 구분이다. 허용한 문서 읽기와 계약 작성으로만 대조하며 세션을 각 모델로 실제 띄워 성능을 측정하지 않는다.
- 신규 외부 독립 세션 생략·제품 직접 수리·현재 검증자 재사용은 없다. 승인 전 세션 발행도 없다. 검증자 모델 해석이 메인과 다르면 발행 전 메인이 확정한다.

## PR 경계와 결정 항목

**승인된 기본 범위:** 구현자 정렬·출처 연결·후보 한 행·필요한 goal/CURRENT 기록을 한 문서 PR에 담는다. 표11~13은 사용자 별도 A로 추가 승인됐다.

**승인된 추가 범위:** 위험 신호→Opus250000·일반 문서/강약/로컬DB카드→Sol xhigh 및 기동 예시까지 같은 PR에서 동기화한다. Gardener/Fable 배정·사용량/한시 예산은 제외한다.

**확정된 한 PR 종료 방식:** 이번 A는 문서 PR 하나다. 메인 `msg_bd99d237b5ec`의 head 고정 PR 판단에 따라 병합 전 Gardener를 수행하고 같은 PR에 문서 변경·검증/Gardener 결과·종료 기록을 싣는다. 두 번째 종료 기록 PR이나 Gardener 면제를 자동 생성하지 않는다. 병합 뒤 원격 상태 확인·로컬 종료 점검/R-8 정산은 남으며, 추가 tracked 수정이 필요하면 먼저 메인에 보고한다.

착수 관문인 메인의 A 원문 대조와 사용자 범위 승인은 충족했다. 중간 점검은 작성 종료 실제 diff/범위 대조, 최종은 독립 원문·R-2·정확 head 병합 승인이다. 승인 전 Run/Task/Dispatch·commit/push·PR 생성0은 초안 당시 기록으로 보존한다.

## 결정과 단계 결과

- 승인과 범위 판정 원문/불변 입력을 보존하고 신규 Run·메인 회신 주소를 연결했다. `lead-approved-context-v1.md`에 승인 전/후 읽기·쓰기 경계를 구분했다.
- 미발행 입력목록이 live goal의 Windows 경로를 빼지 못한 자체점검 실패1건은 `lead/contract-preflight-failure-v1.md`와 실패 원시로 보존했다. 새 v4는 live goal 대신 불변 snapshot을 pin하며 실제14입력 존재/bytes/SHA를 대조했다. 이전 실패를 소급 성공/제품 실패로 바꾸지 않는다.
- 발행 계약은 `contracts/implementation-01-v4.md`22010B/SHA256 `FDB6754FEF997989B02AF068BD5160F5A2999ADD94416E53079673784F53BB17`이며 이전 v1~v3는 미발행 상태다. 관련 CODE6절 원문·허용7문서·수동/provider TEMP·실제 경로/단계/원시·호칭 한정 조건을 붙였다.
- 신규 작성자의 split/준비 satisfied·빈 prompt/모달없음·Sol max fast 화면은 확인했다. 최초 연결 receipt는 input_accepted 뒤 turn_start_unobserved/outcome_unknown이고 현재 agent liveness는 live다. JSON draft는 placeholder가 아닌 가시 후반624자라 전체 입력 크기/전문을 증명하지 못한다. 무응답/unknown만으로 종료하거나 새 작업자를 발행하지 않는다.
- 첫 rollout/session 보고는 아직 없어 priority·backend 실제 모델은 미확인이다. `lead/implementation-start-unknown-main-question-v1.json`으로 메인 판단을 요청했다. 문서 제품 변경·독립 PASS·commit/push/PR·Unity/WSL/DB/dotnet 실행은0이다.
- 메인 복구 판단 `msg_68153c471a4e`(11:06:41Z)는 이번 spec752자 포인터 계약에 한해 사전 literal 대조→Enter1회→rollout의 live preamble/spec 전문 대조를 지시했다. 앞부분 유실/전문 불일치면 쓰기 전에 즉시 abandon/정확pane종료·신규 짧은 주입이며 같은pane 재전송/추가Enter는 금지다. 일반 R-5 정책이나 사용자 결정으로 확대하지 않는다.
- `lead/recovery-draft-comparison-v1.json`은 raw literal=false, 표시 줄바꿈만 제거=false, 비공백 문자 suffix=true다. 화면 줄바꿈이 단어를 나누고 공백을 대체한 관측을 문자 유실로 단정하지 않으며, 반대로 literal 일치라고도 보고하지 않는다. 동일성은 새 show로 대조했고 Enter0으로 메인에 보고했다. `lead/recovery-live-preamble-v1.json`에 공식 preamble 원천을 별도로 보존했으며 아직 제출 전문/priority는 미확인이다.
- 메인 `msg_3dc8e53c9c69`는 표시 줄바꿈이 spec의 공백/빈 문자로만 대응함을 직접 대조하고 Enter 전 조건 충족을 판정했다. 「글자 그대로」를 화면 추출에 적용한 메인 문구 결함은 첫 발생으로 기록한다. 동일 runtime/handle/incarnation·모달없음을 재확인한 뒤 텍스트 없는 Enter1회, draft소멸/Working을 관측했다.
- 첫 raw 제출은 spec752자와 실제 Task/Dispatch를 포함했지만 최초 비교 원천5539자와 실제5533자가 달라, 당시 사후 literal 관문에 따라 worker 쓰기0에서 abandon/정확pane종료/Task cancelled를 수행했다. `msg_242c4cb0e39f`의 쓰기0·계약/pin대조, Git diff, 정산/close 원시를 보존했다. 제품/독립실패0·기동 시도1회로 메인이 `msg_215eb517f519`에서 인수했다.
- 리드 원천 확인의 결함도 구분한다. 최초 `dispatch-show --preamble --from worker`는 coordinator 문장을 worker handle로 생성했으므로 실제 입력 유실 근거가 아니다. parent --from으로 별도 조회한 원천과 실제 제출의 LF제거 비교는 **전부 동일**, LF수는81/75다(`lead/recovery-correct-parent-LF-audit-v1.json`). 초기 잘린 worker-read preview/ReadLines 공유위반 시도도 원시로 보존하고, 새 공유읽기 raw대조와 구분했다. 정상 설정은 native event_msg.payload.thread_settings에서 gpt-6.1-sol/max/priority로 확인했다.
- 메인 `msg_215eb517f519`는 재발행 사후 관문을 parent기준 preamble/spec의 **LF만 제거한 문자열은 글자 그대로 동일 + spec/Task/Dispatch·계약hash 첫 보고 + LF차이/합쳐진 줄 원시**로 정했다. 앞선 정규화 미예상을 메인 지시의 결함으로 기록했다. 이 LF 관찰은 첫 발생 기록이며 R-5 공용 규칙/후보로 즉시 승격하지 않는다.
- 신규02는 선행시도와 연결한 새 계약/231자 포인터·새 세션으로 발행했다. 실제 receipt는 input_accepted/turn_started observed, 수동Enter0이다. raw rollout의 제출5018자는 parent기준 preamble+spec5018자와 **공백/줄바꿈까지 exact 일치**, LF차이0이다. 현재 Task/Dispatch·spec전문·session_meta ID/cwd와 첫 thread_settings gpt-6.1-sol/max/priority를 직접 대조했다. backend 실제모델unknown을 유지한다.
- 신규02 첫 heartbeat `msg_18d6fd34a571`은 실제 신원/ID·태그와 계약hash일치·쓰기0을 보고했다. 원입력 전체대조 뒤 사전메모부터 진행하도록 `msg_4dbafbc0dcf8`로 제출 보류를 해제했고 메인에게 `msg_136161b488b6`로 보고했다. 실제 계약23226B/SHA256 `2F9B13FFF58AC7D7B0BBAA9FF60F94B5BE213B3BCF73C626B83819CA9E0E9645`, 근거는 E/lead/implementation-02-*.json이다. 모델 관측/기동 성공을 독립 문서 통과로 확대하지 않는다.
- 신규02 완료 status `msg_f2b5ef912ca0`·worker_done `msg_b16b6904dd4a` succeeded를 현재 신원/Task/Dispatch·7파일·reportPath와 대조했다. report/context/write-end 전문·실제diff 및65inventory hash/67전체파일을 읽고 집합/불일치0을 확인했다. 기준선17/변경후51 자체점검 실패0은 원시 결과와 일치하며 독립 PASS는 아니다. 원문은 E/implementation-02/worker/report.md11478B/SHA256 `C64964CCFBCD977EEA05F856E9F8AA4111352027CB5561DB44B5C48BE2774B94`, 인수는 lead/implementation-02-intake-audit-v1.json이다.
- 최초 완료status의 「Git 실행0」은 실제 Git 읽기와 불일치했고 작성자가 `msg_f47053823df3`에서 「Git 변경0」으로 정정했다. 보고/수행 불일치로 `msg_ccf6698c63a2`를 메인에 즉시 보냈다. report/write-end는 처음부터 읽기/변경·미실행을 구분했다. 정정 전/후 원문을 보존하고 독립 실사에 제공한다.
- 작성자는 release retained/external 뒤 idle·동일inc/leaf를 확인해 정확pane만 종료했다(ptyKilled=true). 범위내reclaimable0이며 생존 helper는 현재 작성자7신호/최대204초/300초초과0을 냈다. 상세는 lead/implementation-02-release/pane-close-v1.json, after-implementation-reclaimable-v1.json과 liveness-implementation-02-v1.json이다. 미실행 게임·Unity·WSL·DB·dotnet/CI는 그대로다.

### 독립 NOT PASS 1회 + 메인 한정 처분

검증 Task `task_4f43b4d3a6d6`/Dispatch `ctx_b0b6c424553a`는 신규 Sol xhigh·요청 fast였고 native `thread_settings`에서 xhigh/priority를 관측했다. backend 실제모델은 unknown이다. 독립 검증자는 문서 직접 실사·현실적 계약 시나리오14건 뒤 **NOT PASS**를 반환했다. 원문은 E/`review-01/reviewer/verdict.md`14607B/SHA256 `7D8AA357428C7905F0997A3C8D85772AD7EE801F8283CEA03E2D7022A2E39054`다. 쓰기종료·`msg_7938b63019b5`의 failed·61파일을 대조하고 정산/정확pane종료했다. 생존12신호 최대189초/300초초과0은 `lead/liveness-review-01-v1.json`의 표본이다.

| 번호 - 결함 | 귀속과 실제 근거 | 메인 최종 처리·남은 한계 |
|---|---|---|
| R01 - 실행 언어 원문 누락 | 리드 계약이 보조 PS harness를 허용하면서 CODE SQL·PowerShell 원문을 제외 | 원누락 유지. 구현자 승격 실패 집계에서 제외 |
| R02 - 보조 harness 가독성 | 작성자 PS 27/47/114행의 긴 metadata/result 조립과 위치인자 등 | 작성자 실패1 유지. 이번 goal에서 수리하지 않고 같은 harness 재사용 전 수리 후보로 보존 |
| R03 - 최초 기동 명령 전문 누락 | 리드의 작성/검증 두 계약에 정확 최초 codex 명령 필드가 없음 | 원누락 유지, 구현자 승격 집계에서 제외. 실제 max/fast·xhigh/fast 기동은 메인 R-2 표본 대조와 일치 |

메인 `msg_a98913eb5019`(2026-10-25T11:54:53Z)는 원문을 직접 읽고 R01/R02/R03 원천을 표본 대조했다. 판정이 승인7문서의 배정 의미·보존·후보에 추가 결함을 찾지 않았고, R01/R02 보조 harness는 Git 제외 근거이며, R03 실제 기동은10-19와 맞으므로 **승인7문서만 Gardener/PR로 진행**하도록 한정 처분했다. 사용자 결정·소급 PASS·면제·원계약 치유가 아니다. PR 본문과 메인 병합 요청에도 위 굵은 상태 문구를 그대로 싣는다.

메인 `msg_ccf55ac3c1ff`(11:57:05Z)는 기존 검증자 종료 뒤 새 R03 후속 세션을 열지 않도록 했다. 새 세션 생성0이며 미발행 초안은 원형보존한다. **계약 누락을 고치는 문서가 아니라 실제 최초 기동 관측 자료**는 E/`lead/launch-command-observation-v1.md`와 `lead/review-01-R03-native-launch-calls-v1.json`이다. 원91입력 불변/61산출물 hash 대조는 `lead/review-01-post-end-pin-audit-v1.json`·`review-01-intake-audit-v1.json`, 변경 전9문서 snapshot은 `inputs/review-01-final-tracked-snapshot/`과 `lead/review-01-tracked-snapshot-v1.json`에 보존한다.

R01·R03의 필수 계약 항목 누락 반복은 **미채택 후보** `delegation-contract-preflight`로 BACKLOG와 Gardener에 연결한다. 대상은 쓰거나 실행할 언어 작성절 원문·지정 모델/effort·최초 실행 명령 전문의 발행 전 점검이다. 새 규칙/guard/helper 채택·구현은 하지 않으며 사용자 판단을 기다린다. R02 harness 수리 후보는 이 goal에만 남긴다.

## 다음 계획 후보

- BACKLOG `verification-readonly-bootstrap-bundle`: Art에서 전달된 허용 목록·기동 준비·효과 기준 비교. 후보 등록 완료이며 채택·구현은 미승인이다.
- BACKLOG `delegation-contract-preflight`: 메인 msg_a98913eb5019·msg_ccf55ac3c1ff가 추가한 계약 필수 항목 누락 반복 후보. Gardener의 검사화 방법 검토와 사용자 채택 전이다.
