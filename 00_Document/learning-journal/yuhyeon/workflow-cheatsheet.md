# 정유현 워크플로우 치트시트

> 작업할 때 옆 창에 켜두는 빠른 참조. 본인이 편집·확장 자유.
> 최초 작성: 2026-05-15

---

## 🌅 하루 흐름

```
아침 (작업 시작)
├─ git pull origin main           ← 최신 받기
├─ /session:start                  ← CHANGELOG 변경 확인 + 좌표 복귀
└─ git checkout -b feature/yuhyun-{작업명}

낮 (Phase 진행)
├─ /work:plan "<오늘 목표>"        ← (첫 작업이면) Phase 분해
├─ "Phase 1부터 시작하자"          ← 수동 이동
├─ ... 협업 (envelope 받으면서) ...
└─ Phase 완료 (5단계 보고 자동)

저녁 (마감)
├─ /work:review                    ← 사전 검증
├─ 🔴 위반 있으면 수정, 🟡는 판단
├─ /session:end                    ← 자동 마감 (commit + PR + 노션)
└─ 다음 날 /session:start 로 이어감
```

---

## 🎮 슬래시 커맨드 빠른 참조

### work — 작업
| 커맨드 | 언제 |
|---|---|
| `/work:plan <목표>` | 새 작업 시작 시 (Phase 분해) |
| `/work:review` | Phase 끝났을 때 (헌법/ADR 점검) |
| `/work:new-packet` | 새 패킷 추가 (양쪽 wiring 한 번에) |
| `/work:new-monster` | 새 몬스터 추가 (데이터만) |
| `/work:load-test` | 봇 부하 테스트 |

### session — 세션
| 커맨드 | 언제 |
|---|---|
| `/session:start` | 매일 작업 시작 |
| `/session:end` | 작업 마감 (commit + PR + 노션) |
| `/session:log` | 노션 박제만 별도 호출 |

### learn — 학습 (모를 때)
| 커맨드 | 언제 |
|---|---|
| `/learn:why <주제>` | 왜 필요한지 처음부터 |
| `/learn:concept <개념>` | 개념 자체 학부 수준 설명 |
| `/learn:explain <코드>` | 코드 한 줄씩 풀어서 |
| `/learn:dumb-it-down` | 직전 답변 더 쉽게 |
| `/learn:recap` | 진행 상황 + 다음 할 일 정리 |

### journal — 일지 (Phase 끝, 사건 풀린 후)
| 커맨드 | 언제 |
|---|---|
| `/journal:phase` | Phase 통째 회고 |
| `/journal:bug` | 막혔다 풀린 사건 (면접 무기) |
| `/journal:concept` | 깊이 학습한 개념 본인 말로 |

⚠️ **슬래시 발동 vs 자연어 질문**:
- `/work:plan 진짜목표` → *발동* (파일 생성됨)
- `"/work:plan 어떻게 써?"` → *질문* (설명만)

---

## 🌳 Git / PR 빠른 참조

### 브랜치 시작
```bash
git checkout main
git pull origin main
git checkout -b feature/yuhyun-{작업명}
```

### 작업 중 확인
```bash
git status     # 어떤 파일 수정됐는지
git diff       # 구체적 변경 내용
```

### Commit
```bash
git add <파일 또는 폴더>
git commit -m "feat(client): 한 줄 요약"
```

**Commit 메시지 컨벤션**:
```
타입(영역): 한 줄 요약
```
- 타입: `feat` / `fix` / `docs` / `refactor` / `test`
- 영역: `client` / `server` / `shared` / `harness`
- 50자 이내, 한국어 OK

### Push + PR
```bash
git push -u origin feature/yuhyun-{작업명}    # 첫 push만 -u
```

→ 출력된 URL 클릭 → GitHub에서 PR 생성

**또는** `/session:end` 호출 시 자동.

### 머지 후 정리
```bash
git checkout main
git pull origin main
git branch -d feature/yuhyun-{작업명}
```

---

## 🛑 STOP 떴을 때 (session:start git 게이트)

`/session:start`는 CONTEXT 읽기 **전에** git 상태부터 점검. 위험하면 막아줌 (작업물 유실 방지가 목적).

### 판정 3가지

| 상태 | 의미 | 대응 |
|---|---|---|
| ✅ feature 브랜치 + 깨끗 | 안전 | 그대로 진행, 필요시 `git pull origin main` |
| ⛔ main 브랜치 | 작업 위치 잘못 (main은 "공식 줄기") | 어제 브랜치로 `checkout` 또는 새 `feature/` 생성 |
| ⛔ uncommitted 변경 있음 | 세이브 안 된 진행 | commit / stash / 버리기 중 하나 |

### Commit = 게임의 "세이브 포인트"

```
[워킹 디렉토리]  →  [Staging Area]  →  [Repository]
   파일 수정         git add            git commit
   (지금 여기)      "이거 담을게"      "기록 박제"
```

`git status` 표시 읽기:
- `M` = Modified (수정됨)
- `??` = Untracked (git이 모르는 새 파일)
- `A` = Added to stage (스테이징됨)

세이브(commit) 안 한 변경은 `git pull` 시 **충돌 또는 덮어씀 위험** → 그래서 게이트가 막음.

### 변경 정리 3옵션

| 옵션 | 명령 | 언제 |
|---|---|---|
| 버리기 | `git checkout -- <파일>` | 의미 없는 변경 (예: IDE 캐시 `.lscache`) |
| 임시 보관 | `git stash push -m "<메모>"` | 나중에 다시 꺼낼지도 → `git stash pop`으로 복구 |
| commit | `git add <파일>` → `git commit -m "..."` | 의미 있는 변경 |

판단 어려우면 **stash가 가장 안전** (버리지도 commit하지도 않고 보관).

### 🚨 절대 자동으로 치지 마세요 (작업물 증발 1번 원인)

- `git reset --hard` — 워킹 디렉토리 + 스테이지 다 날림
- `git checkout .` (전체) — 워킹 디렉토리 변경 다 버림
- `git clean -fd` — untracked 파일 다 삭제

→ 파괴적 명령은 **파일 단위로 명시**, 전체 적용 금지. Claude도 자동 실행 금지로 못 박혀 있음.

### Unity 프로젝트의 흔한 가짜 변경

| 파일 | 정체 | 보통 처리 |
|---|---|---|
| `*.csproj.lscache` | IDE(Rider/VS) 캐시 | 버리기 (`.gitignore` 후보) |
| `03_Client/ProjectSettings/ProjectVersion.txt` | Unity 켤 때 자동 갱신 가능 | `git diff`로 의미 확인 후 결정 |
| `03_Client/Packages/packages-lock.json` | 패키지 자동 lock | 본인이 패키지 추가 안 했으면 보통 버리기 |
| `03_Client/ProjectSettings/*.asset` | Unity 설정 | ⚠️ 의도적 변경만 commit (팀원 설정 덮을 위험) |

---

## ⚠️ 자주 마주칠 함정

| 함정 | 증상 | 해결 |
|---|---|---|
| main에 직접 commit | push 거부됨 | `git checkout -b feature/...` |
| 브랜치 만들기 전 작업 시작 | main에 변경 쌓임 | `git stash` → 브랜치 → `git stash pop` |
| envelope 빠짐 | 코드 응답인데 4줄 안 박힘 | "봉투 빼먹었어" 한마디 |
| 슬래시 실수 발동 | 의도와 다른 파일 생성 | 따옴표 쓰거나 자연어로 질문 |
| ProjectSettings.asset commit | 팀원 Unity Cloud ID 덮음 | git status 확인 후 add 제외 |

---

## 🔴 절대 어기면 안 되는 5

1. 클라(`03_Client/`)에서 게임 상태(HP, 위치, 인벤토리) 변경 X
2. 데미지 / 히트 / 루팅 공식 = 서버 전용
3. `98_Shared/`는 클라가 *읽기만*
4. 서버 보낸 값은 임의 보정 X (prediction은 reconcile 필수)
5. 의심나면 묻기 (프로토콜·DB·공식은 추측 금지)

---

## 📦 envelope 검토 4줄

코드 응답 끝에 *반드시* 박혀야 함:

```
<!-- work-envelope: <WORK-ID> -->
변경: <건드린 파일 + 핵심 변경>
검증: <빌드/테스트 결과 또는 미실행 사유>
남은 것: <TODO / 리스크 / 다음 액션>
학습 포인트: <개념 1줄>
<!-- /work-envelope -->
```

빠지면 "봉투 빼먹었어".

---

## 📝 STAR 노션 박제 양식

각 항목 본문:

```
🎯 S — Situation
[어떤 상황이었나]

📋 T — Task
[무슨 작업, 왜 필요]

🛠️ A — Action
[Claude와 어떻게 협업, 본인 결정, trade-off]

✅ R — Result
[결과 + 측정값 + 배운 것]

🎓 학습 포인트
[면접에서 1줄로 말할 수 있는 핵심]

📎 첨부
[PR 링크 / 코드 / 스크린샷]
```

---

## 🆘 막혔을 때

1. **개념 모름** → `/learn:why <주제>` 또는 "이거 왜 이래?"
2. **에러 안 풀림** → 에러 메시지 그대로 붙여넣기
3. **방향 헷갈림** → `/learn:recap` 으로 어디까지 했는지 정리
4. **시간 더 필요** → 막힌 거 새 Phase로 떼어내기 (현 Phase에 끼우지 말기)
5. **헌법 위반 같음** → Claude한테 거부 의사 명확히. 안전이 속도보다 우선
