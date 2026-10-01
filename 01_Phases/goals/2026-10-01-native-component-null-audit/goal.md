# R-2 내장 컴포넌트 null 검사

상태: **사전 조사 완료·goal 커밋 후 메인 확인 대기**. `cb6f717`의 `03_Client/Assets/Scripts`를 조사한 결과, 내장 컴포넌트 조회 결과를 CLR null 연산으로 처리하는 수정 대상은 발견하지 못했다. 문서 변경 경로를 제안하며 신규 Opus의 독립 실사 전에는 후보 종료를 확정하지 않는다.

## 목표와 범위

- 착수 지시: 메인 Claude의 `msg_40867e846fd7`(2026-10-01). 전달된 사용자 결정은 DB 설계를 큰 목표로 두고 R-2 검사·Management 규칙 적용을 병행하며 사용하지 않는 브랜치를 정리하는 것이다. 메인 전달과 사용자 직접 입력을 구분한다.
- 출발점: [직전 시범](../2026-10-01-hierarchical-routing-pilot/goal.md)의 Opus 원문 `R-2`. 한 줄 검사에서 사용자 컴포넌트만 확인했으나 여러 줄·변수 경유 내장 컴포넌트 검사는 남아 있었다. 원문은 `.backups/verification/2026-10-01-unity-component-null-pilot/verification-1/verdict.md`에 있다.
- 대상: `03_Client/Assets/Scripts/**/*.cs`에서 `GetComponent`, `GetComponentInChildren`, `GetComponentInParent` 및 관련 조회 결과가 `??`, `?.`, `is null`, `ReferenceEquals`에 들어가는 경로. 호출과 소비가 여러 줄·지역 변수·필드로 분리된 경우도 타입과 대입·사용처를 추적한다. `TryGetComponent`·복수 조회·Find 계열도 검색 교차 확인에 포함한다.
- 내장 조회 결과가 해당 연산에 들어가는 곳이 있으면 Sol이 Unity null 비교 또는 `TryGetComponent`로 최소 수정하고 신규 Opus가 독립 테스트한다. 없으면 제품 코드 변경과 Sol 발행 없이 검색 근거를 기록하고 [후속 후보](../2026-10-01-refactor-record-corrections/open-items.md)의 R-2 종료 기록을 추가한다. 문서만 변경한 경우 Opus는 정적 실사를 수행한다.
- 생명주기 전반의 파괴된 Unity 객체, 사용자 MonoBehaviour, 직렬화 필드, `AddComponent`로 생성한 객체의 모든 CLR null 사용까지 안전하다고 판정하는 작업은 아니다. 새로운 수명·표시 정책, 다른 리팩토링 후보, DB 구현·DDL 실행은 범위 밖이다.

## 완료조건과 소유

1. 검색 대상·방법·타입/흐름 분류와 검색 한계를 재검토 가능한 근거로 남긴다. 검색어 0건만으로 전수 검증을 주장하지 않는다.
2. 발견한 수정 대상에 따라 코드 또는 문서 경로를 택하고, 신규 Opus의 원문을 실제 diff·근거와 대조한다. 미실행 플레이·빌드·DB는 성공으로 보고하지 않는다.
3. 새 검증자를 담당 Astra 아래 pane에 지정 모델로 분할하고 최초 `worker-start --terminal` 연결의 성공/거부와 모델 관측을 기록한다. 작업 하나 후 정산·종료하며 재사용하지 않는다.
4. PASS 후 결과 커밋·push·PR 생성과 판정 원문을 메인에게 보고한다. 각 PR 병합은 사용자 명시 승인 대상이며 자동 병합하지 않는다.

| 소유자 | 허용 범위 |
|---|---|
| GameDev Astra | 이 goal, CURRENT 링크, open-items의 R-2 기록, 로컬 검사·세션 근거, 승인된 branch/commit/push/PR |
| 신규 Opus 검증자 | 저장소 읽기·정적 실사, `.backups/verification/2026-10-01-native-component-null-audit/verification-1/` 보고와 원시 근거 쓰기. 제품·문서·Git 쓰기와 추가 위임 금지 |
| Sol | 현재 수정 대상 없음으로 미발행. 새 코드 대상 발견 시 메인에 근거와 최소 파일 범위를 보고하고 소유권을 정한다 |

goal 커밋 후 메인 확인을 받고 독립 검증을 발행한다. 검증 중 저장소 문서 쓰기를 동결하며 판정 후 결과 기록만 갱신한다. [코드 기준](../../../00_Document/conventions/CODE_CONVENTION.md)·[Orca 절차](../../../.agents/skills/dawnholder-goal-loop/references/orca-work.md)를 적용한다.

## 조사 결과 — 독립 실사 전

기준 HEAD/base: `cb6f717de0fc0eea6d1295d3c8c47da7454125a6`, branch `bass131/native-component-null-audit`. C# 파일 112개, `GetComponent` 계열 및 `TryGetComponent` 호출 44개를 열거했다. 호출부 목록과 CLR null 연산 목록을 각각 검색하고 교차 대조했다. 내장 조회의 결과 변수는 선언·대입부터 사용까지 본문을 읽었다.

```powershell
rg --files -g '*.cs' 03_Client/Assets/Scripts
rg -n --glob '*.cs' 'GetComponent|TryGetComponent' 03_Client/Assets/Scripts
rg -n -U --glob '*.cs' '\b(Try)?GetComponents?(InChildren|InParent)?\s*(<[^>]+>\s*)?\(' 03_Client/Assets/Scripts
rg -n -U --glob '*.cs' '\?\?|\?\.|\bis\s+(not\s+)?null\b|ReferenceEquals\s*\(' 03_Client/Assets/Scripts
rg -n --glob '*.cs' 'Find(Object|FirstObject|AnyObject|Objects)|\.Find\(' 03_Client/Assets/Scripts
```

| 확인한 경로 | 분류와 근거 |
|---|---|
| `DamageFlash._sr`, `EffectAnchor.sr`, `ProjectileVisual._sr`, `AnimatorDriver._anim/_sr`, `ParallaxLayer._renderer` | 내장 `SpriteRenderer` 5곳·`Animator` 1곳의 조회 결과. 필드/지역 변수로 옮겨 사용하지만 소비 전에 Unity `== null`/`!= null` 비교를 사용하며 대상 CLR null 연산 없음 |
| `PartyMemberHud`·`QuestProgressHud` | 내장 `CanvasGroup`은 이미 `TryGetComponent` 후 필요 시 추가하고 필드에 저장 |
| `MapNameDisplay`, `PartyInvitePopup`, `MinimapMarkers` | `TMP_Text`, `Button`, `Image` 조회. 네이티브 내장 타입과 구별하며 해당 결과에 대상 CLR null 연산 없음 |
| `LocalPlayerSpawner`, `ClassVisualMount`, `SnapshotHandler` | 조회 직후 `?.`인 3곳 중 SnapshotHandler는 줄바꿈 형태. 각각 사용자 `LocalPlayerInput`, `AnimatorDriver`, `LocalPlayerMotion` |
| `EnemyRegistry`의 `entry.Motion`/`entry.Driver` | 조회 결과를 엔트리에 보관한 뒤 `?.` 사용. 사용자 `EnemyMotion`/`AnimatorDriver`이며 내장 타입 아님 |
| `ClassVisualMount.root.Find`, `MinimapTerrainTint.FindObjectsByType` | Transform 단일 조회는 Unity 비교, TilemapRenderer/SpriteRenderer 복수 조회는 배열 순회·목록 보관 뒤 Unity 비교. 대상 CLR null 연산 없음 |
| `EffectAnchor.FindRecursive(...) ?? ...` | 사용자 재귀 탐색은 `GetChild`로 실재 자식을 순회하고 미발견 때 명시적 CLR null 반환. GetComponent 미발견 sentinel을 전달하는 경로가 아님 |
| `AudioManager.src?.`, `StageClearUI._animator?.`, 투사체의 `target?.` | 각각 AddComponent로 생성한 AudioSource, 직렬화/AddComponent Animator, 호출자가 전달한 Transform. 조회 API 미발견 결과라는 현재 R-2 조건과 구분하며 파괴 수명 전반의 안전성은 미판정 |

원시 근거: `.backups/verification/2026-10-01-native-component-null-audit/`의 `script-files.txt`, `component-queries.txt`, `component-calls-multiline.txt`, `component-call-inventory.json`, `clr-null-patterns.txt`. 정규식 기반 열거와 수동 타입/대입/사용처 실사이며 컴파일러 데이터 흐름 증명은 아니다. 검색 과정의 EnemyMotion 최초 경로 오기는 `Rendering/EnemyMotion.cs`로 바로잡아 타입을 확인했다.

Unity·EditMode·PlayMode·수동 플레이·솔루션 빌드·서버/DB는 이번에 실행하지 않았다. 직전 시범의 특성화 결과는 검사 계기이며 이번 새 실행 결과가 아니다.

## 브랜치 정리와 보존

원격 `main`을 `ls-remote`로 확인하고 최신 `origin/main`에서 작업 branch를 만들었다. 메인 지시에 따라 `bass131/hierarchical-routing-handoff`(`8f777aba281022e391e3cc4da11226259df57435`)와 `bass131/unity-component-null-pilot`(`26406a338aec0ce4fa1148470d4af2126930fde2`)는 `git branch -d`로 삭제했다.

`bass131/menu-probe-lifetime-p1b`의 `b3cf78a50f8e2ed7570ffd0727a86b339495ae33`는 upstream `147ef1c`와 stable patch-id `dd93abf67ef22d6ac28ba13c3bedc912bbbed4cb`가 같고 `git cherry origin/main`의 유일한 결과가 `-`임을 확인한 뒤 승인된 `-D`로 삭제했다. 원격 삭제는 수행하지 않았다.

archive·main·Management branch·stash 2개를 보존했다. manifest의 skip-worktree `S`와 SHA256 `3E194274509B32D18F4BE03F2D9462B5CDBB721C14EEE0ED0B6A17B1360AD781`도 동일하다. 근거는 같은 로컬 폴더의 `cleanup-before.json`, `cleanup-after.json`이다. main은 Management worktree 소유이며 직접 전환·갱신하지 않았다.

## 세션 관측과 다음 경계

GameDev Astra 화면은 `GPT-6-Astra xhigh`, 백엔드 실제 모델은 `unknown`이다. 현재 CLI runtime은 `8a673084-6819-45b9-a551-347226cdce9b`이며 메인 전달값과의 차이를 회신했다. 메인 handle과 incarnation은 직접 조회해 일치함을 확인했다. 신규 Opus의 요청 모델은 `claude-opus-5-5`이며 최초 실행 명령·화면 표시·실제 모델을 구별해 후속 기록한다.

DB D1a는 읽기 전용 조사·메인에게 초안/질문 전달까지만 병행한다. DB 문서 쓰기와 구현은 이 branch 작업 종료 뒤 별도 branch에서 진행하며 D0 결정을 다시 열지 않는다.
