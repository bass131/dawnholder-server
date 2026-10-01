# 리팩토링 기록 정정과 설계 이유 복원

상태: **정정 작성·독립 검토 완료, PR 준비 중**. 사용자 재개 지시(2026-10-01)에 따라 C1–C5를 수행했다. 최종 문서 갱신·PR·CI 결과는 아래에 기록하며 병합은 해당 PR의 사용자 명시 승인을 기다린다.

## 목표와 기준선

완료한 작은 개선을 보존하면서 현재 병합 상태, 대표 적용 범위, 미해결 후보와 미실행 검증을 구분한다. 삭제된 유효한 설계 이유를 현재 소유 코드 가까이에 복원한다. 제품·게임 정책이나 실행 동작을 변경하는 작업은 아니다.

- 작업 경로: `C:/Dev/DawnHolder_Project`, branch `bass131/refactor-record-corrections`.
- 최신 원격 main 확인·fetch 기준: `ef5f1023fe3353ee9eec5da04422232a33855233`.
- 기존 준비 branch `bass131/menu-probe-lifetime-p1b`와 인계 commit `b3cf78a50f8e2ed7570ffd0727a86b339495ae33`을 보존했다. 새 branch는 main에서 생성하고 인계 문서 3파일을 `147ef1c`로 cherry-pick했다.
- 시작 시 유일한 미추적 파일은 Claude 소유 `CLAUDE.md`였고 SHA256은 `A357F76597DAAC0378501A2B700A3D57636B817D241E2138BAB66ECD0D68CCA0`였다. 정정 작업자는 이를 수정·삭제하지 않았다. 이후 소유자는 사용자 승인으로 공유 사본을 제거하고 별도 [PR149](https://github.com/bass131/dawnholder-server/pull/149)·commit `e6156b699d9684f4b0b66586f1ccdb9cd7a40b91`에 보존했다고 회신했다(`msg_f057655b5cc7`, `msg_445e2b98f16f`). 메인은 해당 단일 파일 PR과 Git 본문을 읽기 확인했다. raw hash 동치를 주장하지 않으며 이 정정 PR에 포함하거나 사본을 재생성하지 않는다.
- 협의 근거: Orca 질문 `msg_251fb584b074`, 사실 확인 답변 `msg_7daecde0a547`, 조정안 `msg_e1becf053dbd`, 수용·착수 및 원자료 `msg_7856cfb59ba3`. 메시지는 당시 협의 근거이며 사용자 요청 원문 전체를 대신하지 않는다.

## 범위와 설계

1. **C1 병합 상태:** S1–S5 goal의 현재형 통합 대기를 실제 merge 기록으로 정정한다. 당시 검증 head·수치와 과거 경과를 새 실행으로 바꾸지 않는다.
2. **C2 적용 범위:** 유지보수 로드맵의 대표 변경을 구체적으로 표시하고 정적 세 진단의 error 적용이 8파일이라는 사실을 명확히 한다. 원래 전 영역 개선 의도나 합의된 후속 목표를 소급 축소하지 않는다. 전역 규칙·CI 설정은 변경하지 않는다.
3. **C3 근거 목록:** [확인한 개선과 후속 후보](open-items.md)는 고정 기준선의 짧은 감사·인계 자료다. 기술 후보, 정책 미결정, 검증 공백, 명시 보류, 합의된 후속 작업, 의도적 유지를 구분한다. 각 목표의 실제 상태를 대체하는 지속 장부로 만들지 않는다. 이미 한 검증과 이번 재실행을 혼동하지 않는다.
4. **C4 설계 주석:** 리스폰 시간·배치의 당시 이유는 해당 값의 현재 소유자에, 서버 tick 기반 보간 이유는 `RemoteInterpolationState`에 둔다. 당시 데모 판단을 현재 최적값·새 측정 결과로 표현하지 않는다. 기존 유효 설명을 반복하거나 실행 토큰·문자열·전처리·직렬화·meta를 변경하지 않는다.
5. **C5 인계 보존:** 기존 checkpoint를 포함하고 CURRENT와 RESUME를 실제 현재 목표로 연결한다. P1a 완료와 P1b 미착수 기록을 보존한다.

새 결함 수정, 초대·쿨다운·메뉴 정책 선택, DB/Management 구현, 전역 error 확대, 새 AI 평가·전수 감사, 대형 HTML 보고서, CLAUDE 설정 반영은 범위 밖이다. 추가 원자료는 필요성을 확인한 후보로만 분류하고 이번 구현 범위를 늘리지 않는다.

## 역할과 파일 소유

| 역할 | 쓰기 소유 | 경계 |
|---|---|---|
| 메인 Astra | 이 goal, open-items, CURRENT, RESUME, PR·결과 | 목표·판단·통합과 Claude 협의 |
| 문서 Astra | S1–S5 goal 5개, 유지보수 roadmap, CODE_CONVENTION의 적용 범위 설명 | C1/C2만; 과거 승인·실적의 원문을 새 주장으로 만들지 않음 |
| 구현 Sol6.1 | `02_Server/GameServer/Combat/EnemyCatalog.cs`, 필요 시 `Maps/EnemyRespawnPlacement.cs`, `03_Client/Assets/Scripts/State/RemoteInterpolationState.cs` | 일반 주석만; 코드 동작·테스트·Git 쓰기 금지 |
| 독립 Astra | 자신의 검토/검증 근거 | 작성자 writer-end 뒤 문서·코드 의미·링크·보존 확인, 대상 수정은 작성자에게 반환 |
| Claude | 별도 세션의 감사 근거·사용자 정책 논의, 기존 CLAUDE.md 소유 유지 | 정정 파일 동시 쓰기 없음; 설정 PR은 별도 |

지정 모델은 구현 `gpt-6.1-sol`, 문서·검토·메인 `gpt-6-astra`; 정확한 실제 runtime 확인 불가는 `unknown`이다. native 작업자를 사용하며 Orca Run/Task/Dispatch를 만들지 않는다. 일반 작업자의 추가 위임은 금지한다. 새 CLI가 필요할 때 `--no-daemon`을 적용한다.

## 검증과 완료조건

- [x] C1 merge SHA·시각·범위가 Git/PR 기록과 일치하고 낡은 현재형 상태가 정정됐다.
- [x] C2 대표 적용과 전체 목표, 8파일 error 적용과 기본 warning을 구분했다.
- [x] C3의 각 후보가 구체 근거·다음 행동·사용자 결정 필요 여부를 갖고 사실/추정/미실행을 구분한다.
- [x] 별도 Astra가 C4의 유효한 설명과 실행 코드 불변, 변경 경계·문서 링크를 확인했다.
- [x] 기존 checkpoint·Unity 자산/공유 DLL을 보존하고 CLAUDE.md의 별도 소유자 처리·보존 위치를 확인했다.
- [ ] 결과·미실행을 기록하고 PR을 생성해 최종 head의 필수 CI를 확인했다. 병합은 별도 승인 대기다.

일반 주석·문서 정정에는 새로운 동작 테스트나 전체 Unity/서버 회귀를 추가하지 않는다. 독립 검증은 고정 base/head의 diff와 실행 토큰·문자열·지시문 동일성, 주석 의미, 상대 링크·기록 일관성을 확인한다. 비주석 변화나 컴파일 우려가 발견되면 범위를 재확인하고 필요한 검증만 추가한다. Windows solution build의 DLL 복사 부작용을 피하며 PR의 기존 필수 CI는 유지한다. 과거 테스트 수치는 링크된 해당 goal의 실적이며 이번 재실행이 아니다.

## 결과와 인계

문서 Astra와 Sol6.1이 쓰기를 종료한 뒤 별도 Astra가 독립 정적 검토하여 PASS했다. 생산 변화는 3파일에 일반 `//` 주석을 각 2줄 추가한 것뿐이다. `git diff --unified=0` 및 삽입 위치 대조로 기존 행·실행 토큰·문자열·전처리 지시문 불변을 확인했고, `.meta`·DLL 변경은 없다. PR140/142/143/144/145의 merge SHA·UTC 시각은 `gh pr view`와 대조했고 모두 기준 main의 조상이다.

변경·신규 Markdown의 상대 파일 링크 누락은 0건이며 새로 바뀐 anchor와 `git diff --check`도 확인했다. checkpoint 3문서의 cherry-pick blob 동일성과 기존 준비 branch의 `b3cf78a` 보존을 확인했다. 검토 당시 입력은 변경 14파일의 SHA256으로 고정했으며 이후 이 goal과 RESUME의 소유자 현황·결과 갱신은 별도 작은 delta로 재확인한다.

원시 명령·hash·결과는 `.backups/verification/2026-10-01-refactor-record-corrections/{checks.json,pr-merge-records.json,summary.md}`, 독립 검토는 `.backups/reviews/2026-10-01-refactor-record-corrections.md`다. 로컬 빌드·동작 테스트·Unity·서버·SQL·AI 재평가는 미실행이며 이번 정정의 성공으로 주장하지 않는다. PR의 .NET CI는 이 정적 검토와 별도로 기록한다. 후속 게임 정책·DB/Management 구현은 시작하지 않았다.
