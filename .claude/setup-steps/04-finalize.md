# 04-finalize — 개인 자산 초기화 + 다음 액션 안내

> role 무관 모두 거치는 마지막 단계.
> 02, 03 통과 상태 전제. 7 단계.

---

## 1. CONTEXT.md 템플릿 복사

```
1단계: 본인 작업 공간을 초기화할게요.
먼저 본인의 CONTEXT.md를 만듭니다. 이건 본인 1인칭 워킹 메모로,
git 공유 안 됩니다 (본인 컴퓨터에만 있음).

레포 루트에서 다음 실행:

  cp .claude/templates/CONTEXT-template.md CONTEXT.md
  (PowerShell의 경우: Copy-Item .claude/templates/CONTEXT-template.md CONTEXT.md)

복사 후 알려주세요.
```

복사 완료 후:
```
좋아요. 이제 본인 정보 채워야 해요. CONTEXT.md 열어서 다음 부분 채워주세요:

1. '사용자 컨텍스트' 섹션
   - 신분: 학부생
   - 이름: {name_kr}
   - 역할: {role 기반 한국어 표기. backend면 "백엔드 코어", unity-client면 "Unity 클라 UI·입력" 또는 "Unity 클라 아트"}
   - 합류 시점: {오늘 날짜 YYYY-MM-DD}
   - 목표: 본인이 정한 학습 목표 (예: 게임 회사 백엔드 포지션 포트폴리오)
   - 본인의 학습 일지 위치: 00_Document/learning-journal/{slug}/

2. '⏸️ 현재 진행 상황' 섹션
   - 합류 시점 게임 진행 상태는 팀장(유영호)에게 확인하거나 그냥 비워두기 OK
   - 본인이 합류한 직후 첫 작업: 일단 "학습 마라톤 시작" 정도

3. 그 외 placeholder들은 일단 그대로 두세요. 작업하면서 채워나갑니다.

저장한 후 알려주세요.
```

저장 완료 응답 → 다음 단계.

---

## 2. CONTEXT_History.md 템플릿 복사

```
2단계: 갱신 이력 파일도 만듭니다.

  cp .claude/templates/CONTEXT-History-template.md CONTEXT_History.md
  (PowerShell: Copy-Item .claude/templates/CONTEXT-History-template.md CONTEXT_History.md)

복사 후 파일 열어서 갱신 이력 표의 첫 줄을 다음으로 채워주세요:

  | {오늘 날짜 YYYY-MM-DD} | {name_kr} ({slug}) 합류 + CONTEXT 시스템 초기화. 역할: {role}. |

저장 후 알려주세요.
```

---

## 3. learning-journal 본인 폴더 생성

```
3단계: 본인 학습 일지 폴더를 만듭니다.

  mkdir 00_Document/learning-journal/{slug}
  mkdir 00_Document/learning-journal/{slug}/concepts

완료 후 다음 명령으로 README 골격 만들어주세요:

  echo "# {name_kr} ({slug}) — 학습 일지" > 00_Document/learning-journal/{slug}/README.md

이 폴더에 본인이 학습한 내용을 박아나가요 (옛 자산 보존, 잔존 사용 OK).
새 트랙 B는 본인 노션 "Dawnholder 학습 일지" DB — 자유 양식 + Claude
자연어 인터뷰 도움 가능 ("회고 박을 거 인터뷰 도와줘"). 옛 /journal:* 슬래시는
M3.5 새 하네스 v1에서 제거됨 (ADR-022).

완료 후 알려주세요.
```

---

## 4. current-pin.txt 빈 핀으로 초기 시드

```
4단계: 본인 작업 좌표 핀을 초기화할게요.
헌법 ADR-018의 작업 좌표 시스템이에요. 첫 작업 정해지면 그때 진짜 좌표 박힘.

다음 내용으로 .claude/state/current-pin.txt 파일 생성/덮어쓰기:

WORK-ID: not-set
PHASE: (셋업 완료, 첫 작업 대기 중)
현재 작업: 셋업 완료. 첫 Phase 또는 학습 마라톤 시작 대기 중.
완료 조건: 첫 작업 결정 후 갱신
다음 액션: /work:plan 호출해서 첫 Phase 분해 받기 (팀장과 의논 후)
주의할 약속:
  - 봉투는 매 코드 응답, 5단계 보고는 Phase 완료 시만
  - 헌법/ADR/하네스 변경은 .claude/CHANGELOG.md에서 확인
학습 보존: (첫 작업 시작 시 추가)
마지막 갱신: {오늘 날짜 YYYY-MM-DD} (셋업 완료)
```

저장 후 알려주세요.
```

---

## 5. 본인 노션 페이지 안내

```
5단계: 본인 노션 페이지를 만들어둘 거예요. 이건 본인 포트폴리오용 자산입니다.

본인 노션 계정(없으면 무료 가입: notion.so)에 새 페이지 또는 데이터베이스
하나 만드세요. 이름 예: "Dawnholder 협업 히스토리".

여기에 본인이 Claude와 협업한 기록을 STAR 형식으로 박제해요:
- S(Situation): 어떤 상황이었나
- T(Task): 무슨 작업이었나
- A(Action): Claude와 본인이 어떻게 협업했나
- R(Result): 결과는

⚠️ 이건 팀 공유 페이지가 아니에요. 각자 자기 노션에 자기 협업 기록.
   협업의 의미가 "팀원 간 협업"이 아니라 "Claude와의 협업"이라
   각자의 학습 마라톤 증거로 활용됩니다.

이 자산이 나중에 본인 자기소개서/포트폴리오의 핵심 자료가 돼요.
팀장(유영호)의 보고서가 이 자료의 예시 — 본인도 그런 자료가 본인 명의로
쌓이는 거예요.

가벼운 시작 권유: 일단 페이지만 만들어두고, 첫 Phase 끝날 때부터
한 줄씩 박아나가세요. 처음부터 완벽한 DB 구조 잡으려 하지 마세요.

페이지 만들었어요? (또는 "나중에 만들게요" 답해도 OK)
```

응답 받으면 다음 단계.

---

## 6. 헌법 / CHANGELOG / 다음 액션 안내

```
6단계: 셋업 거의 다 끝났어요. 마지막으로 한 가지 중요한 약속.

본인(팀장 유영호)이 작업 중 헌법/ADR/하네스/공유 파일 변경할 때마다
.claude/CHANGELOG.md에 한 줄씩 박제됩니다. 본인은 매일 작업 시작 시:

1. git pull (main 최신 받기)
2. /session:start (자동으로 CHANGELOG 최근 줄 보여줌)
3. 변경 인지 후 작업 시작

이게 본인 결정이 팀원에게 전파되는 방식이에요. 안 보면 옛 결정 기반으로
작업하다 충돌 발생할 수 있어요.

그리고 작업 흐름:

- 작업 시작 전: git pull + git checkout -b feature/{slug}-{작업명}
- 작업 끝나면 (그 날 안에): git push + GitHub에서 PR 생성
- 팀장이 PR 승인 → main에 머지 → 다시 git pull

⚠️ main 브랜치에 직접 push 안 됨. 무조건 PR 통해 머지. 본인 승인 강제됨.

PR 만드는 법 모르면 알려주세요. 첫 작업 시작할 때 같이 안내할게요.
```

응답 OK → 마지막 단계.

---

## 7. 셋업 완료 보고 + 다음 액션

다음 메시지를 사용자에게 전체 보고:

```
─────────────────────────────────────────
🎯 셋업 완료 보고
─────────────────────────────────────────

🎯 무엇을 했나
   {name_kr} ({slug}, {role})의 ClaudeDev 협업 환경 셋업 완료.
   환경 검증 8단계 + 역할별 셋업 {5 또는 8}단계 + 개인 자산 초기화 7단계.

🤔 왜 필요했나
   본인은 학부생 백지에서 시작했지만, 본인 환경에서 백엔드/Unity 클라 작업이
   바로 작동하는 상태로 만들어진 상태. 본인 헌법의 자동 검증 훅도 정상 작동.

🛠️ 어떻게 했나
   /setup 진입점이 자기소개 → 환경 검증 → 역할 분기 → 자산 초기화 순서로
   차근차근 안내. 막힐 때마다 STOP + 안내. 학부생 멘토링 톤.

🧪 검증
   - 한글 경로 없음, Bash PATH OK, .NET 10 SDK OK, MSSQL LocalDB OK,
     VS Code + 확장 OK, 빌드 OK
   - {role == backend ? "MSSQL 접속 + PacketGenerator + 서버 부팅 + C# Dev Kit OK" : ""}
   - {role == unity-client ? "Unity Hub + 6000.4.7f1 (hash f3c3c4248748) + 라이선스 + Cloud + AI + MCP + Play 모드 OK" : ""}
   - CONTEXT.md, CONTEXT_History.md, learning-journal/{slug}/, current-pin.txt 박힘

➡️ 다음 액션
   1. CLAUDE.md 한 번 정독 (셋업 중에 대충 훑은 거 깊이 읽기)
   2. 본인 노션 "Dawnholder 협업 히스토리" 페이지 셋업 (아직 안 했으면)
   3. 팀장(유영호)과 첫 작업 의논
   4. 첫 작업 결정 후: /work:plan {마일스톤} 호출해서 Phase 분해 받기

   막히거나 모르겠으면 그때 저한테 물어보세요. 학부생 멘토링 모드 유지됩니다.
```

### 즉석 변환 사후 보고 (해당하는 경우만)

`is_new_member == true` 였던 경우 추가로:

```
⚠️ 팀장에게 보고 드릴 것:
   "{name_kr} → {slug} / {role} 변환 — 팀장 사전 정식 등록 부탁"

   본인 정보가 현재 사전(.claude/setup-steps/01-intro.md)에 박혀있지 않아
   즉석 변환됐어요. 팀장이 정식 박을 거예요.
```

---

## 셋업 종료

이 메시지로 `/setup` 흐름이 완전히 끝남. 사용자는 이제 자유롭게 다음 액션 진행.
