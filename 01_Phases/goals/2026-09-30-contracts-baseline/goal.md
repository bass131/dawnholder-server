# P0 — 공통 계약·유지보수·영속성 기준선

상태: **완료·병합됨**. [PR #146](https://github.com/bass131/dawnholder-server/pull/146)의 head `f18fe92c73d85c69b75b38dcfb4b7969299b6ebb`에 대한 CI SUCCESS·독립 검토와 사용자 명시 승인 후 UTC2026-09-30 09:55:32 merge `a2eb65ee663ff127e77481b7ddcc5e3859abc4b8`로 통합했다. P0의 설계·진단 완료는 게임 코드 개선·DB 저장 연결·실제 Unity 플레이 완료를 뜻하지 않는다. 후속은 [P1a](../2026-09-30-party-invite-command/goal.md)다.

## 목표와 승인 범위

유지보수 가능한 책임 분리, 상태 소유권, 검증 가능한 경계를 클라이언트·서버·공유 코드·도구 전반에 정착시키고, 후속 단계에서 합의된 D0 범위의 실제 DB 저장·복원을 연결한다. [로드맵](../../milestones/2026-09-30-contracts-persistence/roadmap.md)에 단계와 선행 관계를 둔다. 기준·현재 상태·결과는 각 단계의 goal에만 기록한다.

사용자는 전체 영역 포함, Management UI의 별도 세션 소유와 필요시 조율, 게임 동작을 보존하는 내부 재설계를 승인했다. 2026-09-30에 남은 PR 정리 → 정식 목표 승격 → 후속 작업을 요청했다. 모호한 제품 정책·범위·완료조건은 생략하지 않고 사용자와 논의한다. 이미 합의한 결정은 다시 묻지 않는다.

전 영역은 기능 지도와 공통 경계의 모든 영역에 변경/유지/추가조사 판단을 남긴다는 뜻이다. 모든 파일 수정이나 모든 에셋의 전수 검증을 약속하지 않는다. 실제 조사 범위·근거·미확인 항목을 드러내고 표본 조사만으로 전체 코드가 검증됐다고 보고하지 않는다.

## P0 범위와 완료조건

- [x] 기존 PR #141/#143/#144/#145의 정확 head·검증·충돌·미실행 범위를 확인하고, 사용자 개별 승인 후 통합한다. #140/#142의 병합 결과를 보존한다.
- [x] 통합 후 main commit을 고정하고 조사·평가의 대상 코드와 문서 상태를 기록한다. 통합 전 결과와 섞지 않는다.
- [x] 기능 지도 각 기능과 공통 경계를 포함하는 owner/request/state 표를 작성한다. 변경/유지/추가조사 이유, 수명과 실패 경로, 테스트 진입점 및 실제 읽은 경로를 연결한다.
- [x] 경계를 넘는 요청·상태·완료 의미와 역할별 적용 기준을 현행 규칙에 연결한다. 불필요한 추상화·중복 장부를 추가하지 않는다.
- [x] AI 탐색 평가의 과제·채점·모델/effort·맥락·열람/시간 기록 방법을 실행 전에 고정하고 독립 검토한다. 기준선 실행과 미측정 항목을 구분한다.
- [x] 고정한 baseline 과제를 실제 수행하고 독립 채점·원시 기록 확인을 마친다. 실행할 수 없으면 미완료 또는 사용자와 합의한 제외로 남긴다.
- [x] P1 첫 구현의 좁은 계약과 독립 TestCode 검증안을 확정한다. 행동 수정이 필요한 분기는 사용자 결정 후 별도 기록한다.
- [x] 문서·경로·권한 일관성의 독립 검토를 마치고 P0 PR을 생성한다. P0 PR도 병합 직전 사용자 승인이 필요하다.

P0는 설계·기준선 단계다. 생산 코드·Unity 에셋·PDL·SQL·Management UI를 여기서 수정하지 않는다. 정적 CI 확대는 실제 적용할 코드 목표에서 구체 규칙·대상·위반 테스트를 함께 추가한다.

## 보존할 결정과 공통 계약

1. 서버가 게임 상태와 판정을 소유한다. 클라이언트의 서버 상태 사본, 예측값, UI 임시값과 영속 identity를 구분한다. 상태마다 생성·변경·종료 책임을 한 곳에 명시한다.
2. 경계를 넘을 때 필요한 값만 전달한다. 읽기 전용 컬렉션과 내부 원소의 불변성을 혼동하지 않는다. 참조를 보유하면 원본 수명·generation/entry·해제 책임을 계약에 적는다.
3. 요청이 있는 UI는 기능별 command 경계를 사용한다. 표시 전용 HUD에는 필요한 작은 query/binding만 둔다. 모든 화면을 통합 Manager나 전역 이벤트 버스로 모으지 않는다.
4. 로컬 접수, 서버 적용, DB commit/release, 화면 반영은 서로 다른 완료 상태다. 없는 ack나 내구성 보장을 이름만으로 만들지 않는다.
5. 도메인 규칙과 DB·Unity·소켓·시간 의존을 구분한다. actor 밖 I/O 결과를 적용할 때 현재 소유권을 다시 확인하며 게임 틱은 I/O 완료를 기다리지 않는다.
6. 실제 구독한 동일 source에서 해제하고 늦은 callback을 요청 수명에 귀속한다. 기존 연결 generation과 맵 entry 경계를 활용한다.
7. 주석은 이유·제약·순서 계약을 설명한다. LOC·주석 수·패턴 개수 감소를 품질 목표로 삼지 않는다.

기존 연결 수명·맵 진입·플레이어 상태·회귀 개선과 D0 설계는 재사용한다. D0는 개발 고정 계정 1개/캐릭터 1개, 최초 class 유지, 재접속 Town·풀 HP, quest/보스 해금은 세션 한정이다. 저장 대상은 identity/class와 안전 checkpoint이며 DB schema 접속 완료는 GameServer 저장·복원 완료가 아니다. D1a에서 acquire/load·fence·operation 증빙·RecoveryRequired를 기술 명세화한다.

Management UI는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active/05_Management`와 해당 세션이 소유한다. Game Dev는 서버 command/query/event·권한·오류·완료 의미를 조율한다. 관리 UI principal과 게임 GM 계정 권한은 구분한다. Docker 분산 배포·정식 인증·다중 캐릭터·경제 시스템은 자동 포함하지 않는다.

## 조사와 평가 절차

조사의 행은 연결/입장, 이동/예측, 스냅샷/보간, 전투/스킬/피격/사망, 적/보스, 맵 이동, 파티, 퀘스트, UI 요청/구독, 네트워크/공유 프로토콜, 도구/검증, DB/저장 수명, Management 서버 경계로 나눈다. 생산 책임이 있는 editor/tool 코드는 관련 행에 포함한다. 에셋·씬·직렬화·shader는 해당 코드 계약에 영향을 주는 범위와 미조사 범위를 명시한다.

각 행은 owner·변경 진입점·요청/결과·상태 종류와 수명·실패/취소·검증 진입점·변경/유지/추가조사·근거를 갖는다. 경로 지도와 역할 설명은 기존 FEATURE_MAP/domains를 재사용하고, 조사 판단·결과만 이 목표에 둔다. 미해결 질문에는 결정을 내릴 단계와 그 전에 가능한 작업을 기록한다.

AI Readiness는 고정된 탐색 과제에서 owner·호출 흐름·실패 처리·검증 진입점을 근거로 설명하는 정확도와 탐색 부담을 비교한다. 채점표와 독립 정답 검토, 읽은 파일·경과시간 기록, 동일 모델/effort와 시작 맥락을 먼저 명세한다. 정답표를 평가 대상에게 제공하지 않는다. 도구가 실제 모델·토큰을 확인할 수 없으면 unknown/미측정으로 남긴다. 이전 제한 평가 수치와 조건이 다른 새 결과를 하나의 추세로 합치지 않는다.

테스트 수·LOC·주석량을 전체 품질 점수로 합산하지 않는다. 상태 직접 변경 경로, 책임 경계 위반, 관련 변경 파일 범위와 실패 계약 coverage를 구체 사례로 함께 본다. 자동 측정·정적 기준의 도입 여부는 실제 문제와 비용으로 판단한다.

## 역할과 파일 소유권

- 메인 Astra: 이 goal·roadmap·CURRENT·공통 정책의 결정과 통합, 사용자 논의, 종합 보고.
- Sol 6.1 작업자: 명시 배정된 PR 충돌 수정과 기계 Git 작업. 검증 결과나 승인 권한을 임의 변경하지 않는다.
- 독립 Astra: 요구 계약을 기준으로 문서 검토와 실제 변경의 독립 TestCode 검증. 생산 코드 writer 종료 뒤 테스트 파일 소유권을 받는다.
- 영역 조사자는 읽을 대표 경로와 산출물을 한정한다. 같은 파일의 동시 쓰기를 금지하며 추가 위임하지 않는다.

요청 모델과 확인된 실제 runtime은 구분한다. 새 Codex는 --no-daemon. Management 파일·보관 Claude worktree·전역 설정·기존 실행 자원은 수정하지 않는다. SQL 실행은 후속 구체 대상·권한·부작용을 확인한 뒤 진행한다.

## 기준선과 통합 결과

작업 branch는 `bass131/game-contracts-p0`, 최초 base는 `be227d147bc593a7bd525a78072d24a1266c31ba`다. 최종 main 기준선 B는 `c27b03e888986f2ec8c593cd6c626a9c515595e1`, tree는 `e5dd66f6a48cdb94822ebda27f091c8505e37558`로 고정했다. P0 계약 문서가 추가된 작업트리와 B의 기존 문서·코드를 구분한다.

#140 merge `e582798cb4c04e8502027c23c50c267d0176af05`, #142 merge `be227d147bc593a7bd525a78072d24a1266c31ba` 이후 아래 PR을 각각 사용자 승인·정확 head CI SUCCESS·독립 통합 검토 후 병합했다.

| PR | 승인·검증한 head | main merge | 보존/검증 범위 |
|---|---|---|---|
| [#141](https://github.com/bass131/dawnholder-server/pull/141) | `4a88b292bdffd5a2c9ce0ed3c4f76e3d8e1dce78` | `4a8700b9abbd04392affd618fa816a2ced426176` | 세션 준비·인계 문서 2파일 정적 검토 |
| [#143](https://github.com/bass131/dawnholder-server/pull/143) | `a83e72f321482822421a9e4e6c23772d718470a0` | `832291d11c243b1f672adc0a0e336a515ca4590d` | 종료 callback 예외 후 정리·예외 전파. 코드/독립 테스트 기존 검증본과 동일 |
| [#144](https://github.com/bass131/dawnholder-server/pull/144) | `159440fd1e09c3fccdc247db4b0efb6db6cbcf87` | `4abeb8b874ada9c7d924f35e4622944f036f6219` | 원격 보간 코드·테스트·meta 7파일이 기존 EditMode 275 통과본과 동일. PlayMode/시각 플레이 미실행 |
| [#145](https://github.com/bass131/dawnholder-server/pull/145) | `09f618ae7f1d6b30095569af0ba455a3d95763d3` | `c27b03e888986f2ec8c593cd6c626a9c515595e1` | 생성기 실패 종료 코드·회귀 테스트·설정 기존 검증본과 동일. 보고서 Astra 전담 규칙 포함 |

이 승인은 해당 PR에만 사용했다. P0 및 후속 PR의 병합 권한으로 전용하지 않는다. 현행 AGENTS에서 과거 S1–S5 일괄 승인 예외를 제거하고 최신 개별 승인 규칙을 유지한다.

## 현재 결과·근거·다음 실행

### 계약과 조사

[영역별 계약](contracts.md)에 대표 15영역의 owner/request/state·수명·실패/완료 의미·변경/유지/추가조사 판단을 작성했다. 서버 신규 16파일+타입 보강3파일, 클라이언트/공통/도구 신규13파일과 이전12파일 조사 재사용에 기반한 제한 조사다. 모든 코드·에셋의 결함 부재를 주장하지 않는다. [평가 방법](evaluation-method.md)은 별도 계약이며 실행 결과는 이 goal에 둔다. [P1 첫 구현 계약안](p1-first-contract.md)은 파티 응답 command 추출과 별도 구독 조각, 현재 동작 보존 및 독립 테스트 계획을 정의한다. 구현 착수나 테스트 실행 결과는 아니다.

PlayerStats 같은 불변 정의 공유는 유지하고 mutable session/entity/배열은 소유권과 수명을 명시한다. 파티 popup의 gate와 SendIntent의 entry-ready gate 차이는 코드상 관찰했으나 실제 UI 재현 전이다. ConnectionProbe의 EndConnect SocketException 경로는 명시적 Close/finally 없이 실패 callback을 등록하고 반환한다. 실제 운영 발생·누수량은 확인하지 않았다. HP coordinator의 epoch API 검증과 wire packet의 과거 entry 식별은 구분한다. 후속 동작 변경은 재현과 정책 논의 후 결정한다.

Management의 Git 기록+재생 가능한 조회 색인·전투 기록 pilot에 의견을 회신했다. 게임 버전별 계약·활성 goal/CURRENT의 원본은 Game Dev가 유지하고, 소유권 이전 전 Management 카드는 출처/관측시점을 가진 투영으로 취급한다. 기존 문서 이동·삭제나 새 API 구현을 수행한 것은 아니다. 상세 회신은 로컬 `.backups/handoffs/2026-09-30-management-records-gamedev-reply.md`다.

### 실제 최종 main 회귀

2026-09-30 UTC 09:07:56–09:09:52, Ubuntu26.04 WSL2/.NET SDK10.0.300에서 B의 입력260파일을 고정해 실행했다.

```text
dotnet build Dawnholder.slnx --configuration Debug --no-incremental --nologo
dotnet test Dawnholder.slnx --configuration Debug --no-build --nologo --logger "trx;LogFileName=final-main.trx" --results-directory <evidence>/test-results
```

빌드 exit0/오류0/경고4, 전체 테스트 1회 **839개 중 834 통과·5 기존 skip·실패0**. 입력 전후 hash와 Git 동치 확인, 추가 source/config0, 원본 Unity DLL2 불변. 테스트 실행 전 runner의 CRLF 파싱 오류는 evidence script만 LF로 고쳐 해결했으며 제품 코드는 바꾸지 않았다. 실제 Unity/독립 bot/SQL/시각 플레이를 이번에 재실행하지 않았다. 원시 로그·TRX·입력 manifest: 로컬 `.backups/verification/2026-09-30-final-main-regression/summary.md`와 같은 폴더.

### AI 진단 기준선

B에서 고정6과제를 각1회, 과제마다 이전 작업 대화를 전달하지 않은 새 native agent 맥락으로 실행했다. 정답 작성자·검토자와 응답자를 분리하고, 응답 시작 전에 정답의 callback 정리·HP epoch·generator fixture 범위를 정정한 뒤 독립 검토·hash 봉인을 마쳤다. 지정 모델/effort는 `gpt-6-astra/high`, 실제 모델/effort는 **unknown**이다. 응답은 모두 기록상10분 안에 도착했고 invalid 또는 정답 노출을 보여주는 기록은 없었다.

| 과제 | 소유/수명 | 정상·실패 | 영향 경계 | 검증/한계 | 계 | 기록상 경과(초, 추정) | 내용 열람 경로(자기기록) |
|---|---:|---:|---:|---:|---:|---:|---:|
| T1 파티 수락 | 2 | 2 | 2 | 2 | 8/8 | 278 | 30 |
| T2 메뉴 연결 확인·화면 종료 | 2 | 1 | 2 | 1 | 6/8 | 166 | 10 |
| T3 첫 맵 진입·HP | 2 | 2 | 1 | 2 | 7/8 | 307 | 29 |
| T4 DB 설계/구현·결과 소유 | 2 | 2 | 2 | 2 | 8/8 | 185 | 6 |
| T5 이동 패킷 필드 변경 영향 | 2 | 2 | 2 | 2 | 8/8 | 300 | 38 |
| T6 생성기 종료·출력 계약 | 2 | 2 | 2 | 1 | 7/8 | 164 | 7 |

별도 Astra의 원시 기록 감사·24항목 채점 재검토 **PASS, 44/48**. 이는 여섯 답안의 적합도이며 전체 코드 품질·PR 합격선·개선율이 아니다. 확정 과잉확신0건은 명시적 거짓 보장에 대한 판정이며 답안이 완전하다는 뜻은 아니다. T2의 실패 socket 정리/구체 검증 접점, T3의 coordinator epoch와 wire HP 한계, T6의 작은 ValidPdl fixture·LF 정규화 hash 범위 설명이 빠졌다. 해당 항목을1점으로 처리했고 다른 항목에 중복 감점하지 않았다. 이후 문서·테스트의 경계 설명을 보강할 구체 출발점이다.

계측은 명령/경로 자기기록과 메인 dispatch 직전 시각·응답자 시각의 조합이다. 실제 prompt 전달·마지막 파일 저장을 외부 관측한 엄밀한 경과시간 상한이 아니다. 초기 기록 일부 재구성, 잘린 출력, 형식 차이를 보존했다. baseline 내용 경로를 source/test/config/document로 정규화한 수는 T1 `23/5/1/1`, T2 `6/1/0/3`, T3 `21/7/0/1`, T4 `3/0/0/3`, T5 `28/4/4/2`, T6 `1/2/1/3`이며 로컬 prompt/metadata는 제외했다. command JSON 행46/16/33/15/17/14는 tool/내부 명령 수와 같지 않아 별도 비교 점수로 쓰지 않는다. 실제 token/cache·독립 전수 열람 계측은 미측정이다. 자동 제공 지침/운영 문서의 정보 노출 가능성이 있어 완전한 맹검·기술적 접근 차단을 주장하지 않는다.

봉인 method SHA256 `E355FE3E30E97CA741E3CBB07494B8595F7763F52E8B6028970356A551700B24`, allowlist SHA256 `B93F026BEA8C710E7197E0A4589F133C76025B35A159B965AF1EC3D27BAFDDD3`. common605파일과 T4 추가D0문서3개의 blob은 B와 일치했다. 제출물18개 hash와 봉인 입력도 일치했다. `.backups/verification/2026-09-30-p0-evaluation/`의 seal·allowlist·task별 prompt/answer/commands/run·parent-runs·measurement-summary·scoring-draft가 원시 근거다. draft라는 원문 이름/봉인 전 상태 문구는 후속 seal·검토 기록과 구분한다. 독립 감사는 `.backups/reviews/2026-09-30-p0-evaluation-audit.md`다. 이 로컬 기록을 공유 환경의 자동 재현 데이터라고 하지 않는다.

이후 비교는 별도 고정 A와 같은6과제/기준을 사용하되 A의 실제 정답을 독립 확인한다. 실제 runtime 확인 불가·단발 진단이라는 한계 때문에 속도·생산성 향상의 인과 효과를 주장하지 않는다. 과거29/32→31/32와 이번 점수를 같은 추세로 합치지 않는다.

### 문서 검토·다음 실행

[P1 첫 계약](p1-first-contract.md)은 파티 응답 command 추출 → 별도 같은-source 구독 조각을 다룬다. 기존 drop/pending/팝업·예외 순서를 먼저 독립 baseline fixture로 고정하며, void SendIntent의 정상 반환을 전송 접수나 서버 확정으로 이름 붙이지 않는다. 실제 UI 도달성/변경 필요성을 재현한 뒤 UX 정책 변경은 사용자와 결정한다. 구현자는 Sol6.1, 독립 TestCode·검증은 Astra이며 P0 병합 후 최신 main의 별도 목표/branch로 착수한다.

독립 문서 검토에서 초기28개·추가75개 상대 링크 누락0, B 대비 생산/테스트/설정 delta0을 확인했다. 전투 표의 raw/clamp를 권위 HP 상태가 아닌 패킷 currentHp 표현으로 명확히 고쳤다. 계약·P1 범위/독립 테스트안과 최종 평가 결과 단락·체크박스의 좁은 재검토도 PASS다. 문서9파일을 PR #146으로 발행했다. 최종 head CI SUCCESS와 사용자 개별 승인을 확인해 병합했다. 자동 병합은 사용하지 않았다. 이후 PR도 별도 명시 승인을 받는다.

Management의 요청 `msg_19fee0fa81cb`도 전투·Game Dev 상태 범위로 검토해 `msg_4397a2310999`로 회신했다. 고정09:05 catalog 상태는 일치하고, 사망 후처리의 조건과 HP packet 표현을 정정 요청했다. 이후 회귀/평가 결과를 이전 snapshot에 소급하지 않으며 Management 파일·UI는 수정/검증하지 않았다. 회신 근거는 `.backups/handoffs/2026-09-30-management-catalog-gamedev-review.md`다.

그 밖의 로컬 근거는 `.backups/reviews/2026-09-30-p0-formal-doc-review.md`, `2026-09-30-p0-curated-inventory-review.md`, `2026-09-30-p0-final-doc-review.md`, 각 `pr143/144/145-integration-review.md`, `.backups/verification/2026-09-30-rollout-report/merge-*.json`이다. 공유 goal의 수치·commit·명령과 코드/CI가 재확인의 출발점이다. PR #146의 검증·개별 승인·병합을 마치고 후속 P1a로 이동했다. 미합의 게임 정책·SQL 실행 권한은 자동 확대하지 않는다.
