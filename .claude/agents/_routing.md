# Agents Routing — SubAgent 풀 9 도메인 매핑

> 본 문서는 *작업 → SubAgent* 빠른 매핑 표. **WHY/원칙은 [`../policies/subagent-routing.md`](../policies/subagent-routing.md), 본 문서는 HOW**.

---

## 도메인 → SubAgent 매핑

| 작업 도메인 | 위임 대상 | 비고 |
|---|---|---|
| 패킷 모양 / 직렬화 / 프레이밍 / 연결 라이프사이클 | `shared` + `server` | PDL은 `shared`, 핸들러 + 세션 상태는 `server` |
| 전투 / 스킬 / 스탯 / 공식 / AI / 영속화 | `server` | 게임플레이 + 네트워킹 + DB 통합 (옛 4 도메인 흡수) |
| Unity 씬 / 렌더링 / 입력 / UI / prediction | `client` | `.cs` 스크립트 본문 |
| Unity prefab / asset / scene YAML | `unity-bridge` | MCP 도구 전담 |
| 헤드리스 봇 / 부하 / 퍼징 / 테스트 | `qa` | 게임 코드 R only |
| ComfyUI 자산 / 2D 스프라이트 import | `unity-bridge` | unity-asset 깃발 자동 발동 (인규 영역 보조) |
| 콘텐츠 데이터 값 (몬스터 stat 등) | `qa` | 스키마 자체는 `shared` |
| Knowledge 캐시 정리 (비활성화/응축/승격/분해) | `knowledge-gc` | 수동 트리거만 (`/harness-review` / `/session:end` / 사용자 명시) |
| 헌법 / ADR / policies / 하네스 자체 | (위임 X, 영호 단독) | M3.5 약속 |

---

## 등급 → 처리 패턴

| 등급 | 처리 패턴 | SubAgent 동원 |
|---|---|---|
| **단순** | 메인 세션 직접 | 없음 (위임 비용 > 작업 비용) |
| **보통** | Worker 1개 | server / shared / client / qa / unity-bridge 중 1 |
| **복잡** | Coordinator + Worker 1~2개 | + reviewer (조건부) |
| **대규모** | Coordinator + Team | Worker 3~4개 + plan-auditor 사전 + reviewer 통합 |

---

## 자동 호출 트리거

### Coordinator 자동 호출 (등급 결정 직후)
- 복잡 / 대규모 → 무조건 호출
- 보통 + 도메인 2개 영향 → 권장 (메인 판단)
- 단순 → 호출 X

### Reviewer (Tier 2-A) 자동 호출 (Worker 코드 변경 후)

**무조건 호출**:
- `98_Shared/` 변경 포함
- 새 핸들러 / 패킷 / 공식 추가
- 위험 깃발 (trust-boundary / irreversible / unity-asset) 발동
- 사용자 "리뷰 돌려줘" 명시

**조건부 호출**: 실질 변경 ≥10줄 + 등급 ≥ 보통

**스킵**: 테스트만 / 주석·rename만 / 사용자 "리뷰 스킵 + 사유"

### Plan-auditor (Tier 2-B) 자동 호출

**무조건 호출**:
- `_milestone-plan.md` Write/Edit
- `01_Phases/**/NN-{slug}.md` Write/Edit (Phase 정의)

**스킵**: 주석·오타만 / 사용자 "점검 스킵 + 사유"

### Knowledge-GC 자동 호출 (없음)

- **자동 호출 X** — knowledge 자율 변경은 가짜 학습 누적 위험
- 발동 트리거: `/harness-review` 슬래시 (Phase 05 산출물) / `/session:end` 마일스톤 마감 권유 / 사용자 "knowledge-gc 호출해줘" 명시

---

## 위임 입력 약속 (필수 5항목)

Coordinator → Worker 위임 시:

```
@<worker-name>

작업: <한 줄>
입력 자산: <Phase 정의 / 의존 -DONE.md / 관련 파일 경로>
변경 대상: <폴더 또는 파일 목록>
완료 조건: <측정 가능한 조건>
출력: work-pin 갱신 + (필요 시) -DONE.md 또는 진행 보고
```

5항목 중 하나라도 누락 시 Worker가 *추측 없이 즉시 종료* + coordinator에게 입력 부족 알림.

---

## 권한 경계 (위반 시 거부)

| SubAgent | R/W | R only | 절대 X |
|---|---|---|---|
| `server` | `02_Server/**` + `98_Shared/**` | `03_Client/**` + `04_ClientNet/**` + `99_Tools/headless-bot/` | 헌법 / 정책 / Unity asset |
| `shared` | `98_Shared/**` + `99_Tools/PacketGenerator/**` | `02_Server/**` + `04_ClientNet/**` + `03_Client/**` | 헌법 / 정책 / Unity asset |
| `client` | `03_Client/Assets/Scripts/**` + `04_ClientNet/**` | `98_Shared/**` | `02_Server/**` + Unity asset (`.prefab/.unity/.asset`) |
| `qa` | `99_Tools/**` + `*Tests/**` + 콘텐츠 데이터 값 | 게임 코드 전체 | 게임 소스 본문 |
| `unity-bridge` | `03_Client/Assets/Scenes/**` + `Prefabs/**` + Unity asset + Unity MCP | `.cs` 스크립트 + `98_Shared/**` | 서버 코드 + 헌법 |
| `reviewer` | (없음) | 전체 | 코드 편집 X (Tier 2-A) |
| `plan-auditor` | (없음) | 전체 | 코드 편집 X (Tier 2-B) |
| `coordinator` | (없음, 위임 권한 보유) | 전체 | 코드 편집 X / 다른 coordinator 호출 X |
| `knowledge-gc` | `../knowledge/**/*.md` | 정책 + CHANGELOG | 코드 / 헌법 / ADR — 사용자 확인 게이트 통과 후만 정리 |

권한 위반 시 즉시 거부 + coordinator에게 다른 SubAgent 요청 보고.

---

## 재귀 차단 (절대)

- **Coordinator → Worker 1단계만**. Worker가 다른 Worker 직접 호출 X
- **Worker가 다른 도메인 작업 필요 발견** 시: 결과에 *분해 요청* 표기 → coordinator가 재분해
- **Coordinator → 다른 Coordinator 호출 X**: 분해가 너무 깊으면 Phase 자체 잘못 추정 신호

자세히 → [`../policies/subagent-routing.md`](../policies/subagent-routing.md) "위임 경계" 절.

---

## 변경 시 동기화 책임

본 문서 수정 시 *반드시* 함께 갱신:
- [`../policies/subagent-routing.md`](../policies/subagent-routing.md) (원칙 정책)
- [`../policies/grade-and-risk.md`](../policies/grade-and-risk.md) (등급 → 처리 패턴 매핑)
- [`coordinator.md`](coordinator.md) (분해 패턴 카탈로그)
- 각 SubAgent의 *권한 경계* 절

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 02 (3/3) 신설. 옛 헌법 본문에 박혔던 6 도메인 라우팅 표를 8 SubAgent로 확장 + 자동 호출 트리거 + 권한 경계 통합.
- 2026-05-20 — M3.5 Phase 04 (3/3) 풀 9 확장. `knowledge-gc` Specialist 3번째 추가 (수동 트리거만, 자동 호출 X) — 도메인 매핑 + 자동 호출 트리거 + 권한 경계 행 추가.
