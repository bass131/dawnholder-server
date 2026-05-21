---
summary: M3.5 Phase 04 — Knowledge 시스템(5 도메인 _index + _usage + README) + GC Collector(knowledge-gc Specialist 3번째) + 시드 8건을 New_Harness/ 격리 폴더 안에 박음. 옛 운영 영향 0. 풀 8 → 9 자연 확장.
phase: M3.5/04
status: done
owner: youngho
grade: 대규모
---

## TL;DR

새 하네스 v1의 Knowledge 시스템 풀세트를 박음:
- `New_Harness/knowledge/` 5 도메인 (server/shared/client/qa/cross-cutting) `_index.md` + `_usage.md` + `README.md` (7파일)
- `New_Harness/agents/knowledge-gc.md` (Specialist 3번째 신설 — 풀 8 → 9)
- 시드 항목 8건 — cross-cutting 4 / client 2 / shared 1 / server 1 / qa 0(M4 자연 누적)
- 옛 자산 마이그 표 박힘 (memory / CHANGELOG / 학습 일지 → knowledge 시드 흡수)

옛 `.claude/agents/` 7개 + `.claude/CHANGELOG.md` + `~/.claude/memory/`는 **그대로** → 옛 운영 100% 작동. Phase 06 전환 commit 시점에 일괄 mv 예정.

---

## 5단계 보고

> Phase 04 등급 = *대규모* — 새 헌법 v1 기준 5단계 보고 + MD/HTML 이중 박음 필수. HTML 박음은 Phase 06 전환 후 발효(`00_Document/reports/`로 mv). 본 시점은 MD만.

### 🎯 무엇을 만들었나

3 commit 누적 (`a42dbdc` + `c6b1402` + 본 commit), 총 13 파일 / +1100여줄:

**(1/3) `a42dbdc` — Knowledge 골격 (7파일 / 597줄)**:
- `knowledge/README.md` (86줄) — 진입점 + 트랙 A/B 분리 + 박는 양식
- `knowledge/_usage.md` (181줄) — SubAgent/사용자 입출력 가이드 (통독 매핑 + 박는 시점 3종 + GC 시점 + 자율 박제 금지)
- `knowledge/server/_index.md` (65줄) — server SubAgent 캐시 골격 (시드 자리만)
- `knowledge/shared/_index.md` (64줄) — shared SubAgent 캐시 골격
- `knowledge/client/_index.md` (65줄) — client + unity-bridge SubAgent 캐시 골격
- `knowledge/qa/_index.md` (65줄) — qa SubAgent 캐시 골격
- `knowledge/cross-cutting/_index.md` (71줄) — 전 SubAgent 통독 캐시 골격

**(2/3) `c6b1402` — GC Collector + 시드 8건 (5파일 / +368 -20)**:
- `agents/knowledge-gc.md` (212줄, 신설) — Specialist 3번째. 풀 8 → 9 자연 확장. GC 정책 5종(비활성화/완전 삭제/응축/분해/승격) 명세. 자동 호출 X (`/harness-review` 또는 `/session:end` 권유 또는 명시 요청만)
- `knowledge/cross-cutting/_index.md` (+63줄) — 시드 4건: `sac-dotnet-test-block` / `projectsettings-cloud-ping-pong` / `gamma-pre-validation-pattern` / `riot-vanguard-spawn-unknown`
- `knowledge/client/_index.md` (+32줄) — 시드 2건: `prefab-overwrite-untracked-disaster` / `unity-version-hash-pinning`
- `knowledge/shared/_index.md` (+15줄) — 시드 1건: `false-promise-pattern` (Rule of Three 통과 ★★★)
- `knowledge/server/_index.md` (+26줄) — 시드 1건: `lifecycle-race-broadcast-skip`

**(3/3) 본 commit — 정합 갱신 + -DONE.md**:
- `New_Harness/CLAUDE.md` — SubAgent 풀 표 8 → 9 (`knowledge-gc` 행 추가) + 자동 호출 트리거 절에 "knowledge-gc 자동 호출 X" 명시
- `New_Harness/agents/_routing.md` — 헤더 "풀 8 → 풀 9" + 도메인 매핑 / 자동 호출 / 권한 경계 표에 knowledge-gc 행 추가
- `New_Harness/README.md` — Phase 04 산출물 표 (예정 → 완료) + 옛 자산 마이그 표 6행 추가 (memory / CHANGELOG / 학습 일지 → knowledge 시드 흡수 매핑)
- `04-knowledge-system-gc-DONE.md` — 본 박제

### 🤔 왜 필요한가

5/20 의논에서 박힌 *Knowledge 시스템 풀세트 + GC Collector* 모델을 헌법 산물로 박는 단계. 옛 운영의 *AI 백지 비용* 함정 봉합:

- **옛 운영 함정**: 각 세션이 *백지에서 시작* → 같은 사고 반복 (SAC On dotnet test 차단 / ProjectSettings cloud ping-pong / PacketGenerator noManager 트랩 등). CHANGELOG 박아서 매 세션 재인지 → 시간 비용 ↑
- **트랙 분리 정신**: 학습 일지(트랙 B = 본인 회고)는 AI가 직접 활용 못함. ADR/policies/CHANGELOG는 *도메인별 인덱싱 X* → 검색 비용 ↑
- **NDREAM 패턴 정합**: PDF NDREAM 하네스 5/20 참조 — 도메인별 _index + GC Collector = 한국 게임 회사 백엔드 표준
- **AI 자기 강화 편향 방지**: AI 자율 박제 = 자기가 박은 패턴을 자기가 인용 → 검증 없는 순환. 사용자 확인 게이트 = 사실 검증

### 🛠️ 어떻게 만들었나

**핵심 결정 4개**:

1. **GC = 별 SubAgent (9번째 Specialist)** — Phase 04 정의 73줄에 "별 SubAgent 또는 qa sub-mode" trade-off 박혀있던 거 *별 SubAgent*로 결정. qa는 *바이너리 검증 도메인*이고 GC는 *문서 관리 도메인*이라 본질이 다름. 옛 모호 영역 해소 정신(Phase 02 ★★★ 학습)이랑 정합. 풀 8 → 9 확장은 Specialist 카테고리 안 자연 누적
2. **시드 8건 (cross-cutting 4 / client 2 / shared 1 / server 1 / qa 0)** — *처음부터 풀세트 박지 않음* 원칙. 8건은 ⭐ 강력 5 (sac/cloud/prefab/false-promise/gamma) + 권장 3 (unity-hash/vanguard/lifecycle-race). qa는 M4 진입 후 자연 누적 (헤드리스 봇 / 부하 / 퍼징 시드는 그때 박힘). 시드 박을 항목은 *사용자 명시 선택* — AI 자율 박제 금지 원칙
3. **옛 자산 처리 = *흡수가 아니라 재작성*** — 옛 memory(`sac-dotnet-test-block.md` 등)는 *유지* (개인 영역 보존). 옛 CHANGELOG는 *유지* (시간순 이력 가치). 옛 학습 일지는 *유지* (트랙 B 분리). 새 knowledge 시드는 옛 자산을 *AI 가독성으로 재작성* — 회고체 X, 구조화 패턴 (증상/패턴/봉합/사례/확신도/관련 키워드)
4. **`_gc-policy.md` 별 파일 박지 않음** — 옛 정의 파일 9줄엔 별 파일 박는다고 박혀있었으나, `policies/knowledge-system.md`에 GC 정책 5섹션 이미 박혀있고 + `agents/knowledge-gc.md`에 정책 4종 + 절차 명세 박힘. 중복 회피 = 정책 단일 진실 공급원 정신

**작업 순서 (3 commit 분기)**:

1. (1/3) 5 도메인 + _usage + README 골격 → commit `a42dbdc` (옛 운영 영향 0 검증: dotnet build green)
2. (2/3) knowledge-gc agent + 5 _index에 시드 8건 → commit `c6b1402` (Shared.dll timestamp 변경은 의도 외라 stage 안 함)
3. (3/3) 정합 갱신 (README + CLAUDE + _routing 풀 9 확장) + -DONE.md → 본 commit

### 🧪 테스트 결과

**자동 (옛 운영 sanity check)**:
- `dotnet build Dawnholder.slnx` — green (0 경고 / 0 오류 / 3.56s, commit (1/3) 시점)
- 옛 `.claude/agents/` 7개 + 옛 CHANGELOG + 옛 memory 그대로 → 옛 자동 호출·통독 흐름 영향 0
- 옛 `00_Document/policies/knowledge-system.md`는 *존재 X* (옛 운영엔 Knowledge 시스템 영역 자체 없음, 새 격리 폴더 안 신설). 옛 운영 깨질 가능성 0

**수동 (본인 눈 통독)**:
- 시드 8건 본인 눈으로 통독 — *양식 검증*: 증상 / 패턴 / 봉합 / 사례 / 확신도 / 관련 키워드 6요소 모두 박힘 확인
- 가상 시나리오: `server` SubAgent가 작업 시작 → `server/_index.md` + `shared/_index.md` + `cross-cutting/_index.md` 통독 → `lifecycle-race-broadcast-skip` + `false-promise-pattern` + `sac-dotnet-test-block` 발견 → 작업 진행 시 *백지 비용 ↓* 확인 (시뮬레이션 통과)
- GC 시나리오 3건 점검 — 비활성화 / 완전 삭제 / 응축. 4단계 절차(통독 → 분류 → 보고 → 사용자 OK → 실행) 합리적 확인 통과
- `_routing.md` / `CLAUDE.md` 풀 9 확장 정합 — knowledge-gc 자동 호출 X 명시 / 권한 경계 (`../knowledge/` R/W, 다른 영역 R only) / 도메인 매핑 (수동 트리거만) 3축 모두 박힘 확인

### ➡️ 다음 스텝

**Phase 05 — 슬래시 정리 + 신규 2개** (복잡, 3~4h):
- `/harness-review` 신규 (하네스 자체 점검 + knowledge-gc 호출 트리거)
- `/cross-review` 신규 (γ 방식 정합)
- 옛 슬래시 16개 → 새 10개 정리 (학습 5 + 일지 3 트랙 B 이관)
- `/session:end` 흐름에 knowledge-gc 자동 권유 단계 추가
- Phase 04 산출물 (knowledge + knowledge-gc) 이 슬래시 호출 대상

**Phase 06 — 정합 마감 + 일괄 mv** (복잡, 2~3h, M3.5 ↔ M4 게이트):
- 새 자산 → 옛 영역 일괄 mv (knowledge/ → `.claude/knowledge/`, knowledge-gc.md → `.claude/agents/`)
- 옛 자산 삭제 또는 응축
- ADR-022 박음 + CHANGELOG [H]
- 본 폴더 삭제

**(별 시점) Phase 04 학습 키워드 ★★★ 흡수** — 본 Phase에서 박힌 4건 (track-a-vs-b-split / seed-restraint-pattern / ai-self-reinforcement-bias-prevention / specialist-pool-organic-expansion)을 별 시점에 트랙 A로 메타-박제 (또는 본인 트랙 B Notion).

---

## AC 검증 결과

Phase 04 정의 완료 조건 6개 실측 검증:

```bash
# 1. New_Harness/knowledge/ 5 도메인 폴더 + 각자 _index.md 박힘
$ find 01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge -name "_index.md" | sort
01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge/client/_index.md
01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge/cross-cutting/_index.md
01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge/qa/_index.md
01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge/server/_index.md
01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge/shared/_index.md
# → 5/5 PASS

# 2. 항목 양식 + 파일 크기 한도 명세 (200줄 한도)
$ wc -l 01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge/**/_index.md \
        01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge/*.md
  97 client/_index.md
 134 cross-cutting/_index.md
  65 qa/_index.md
  91 server/_index.md
  79 shared/_index.md
 181 _usage.md
  86 README.md
# → 7/7 PASS (모두 200줄 한도 안, _usage 최대 181줄)

# 3. GC Collector SubAgent 정의 박음
$ ls -la 01_Phases/youngho/M3.5-harness-v1/New_Harness/agents/knowledge-gc.md
-rw-r--r-- 1 bass1 197609 8.5K May 20 ... knowledge-gc.md
$ wc -l 01_Phases/youngho/M3.5-harness-v1/New_Harness/agents/knowledge-gc.md
212 knowledge-gc.md
# → PASS (Specialist 3번째, 풀 8 → 9 자연 확장)

# 4. 시드 항목 5~10개 박음 (실측 8건)
$ grep -E "^\| \`[a-z-]+\` \|" 01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge/**/_index.md | wc -l
8
# → PASS (cross-cutting 4 / client 2 / shared 1 / server 1 / qa 0)

# 5. 옛 자산 마이그 표 갱신 (README.md Phase 04 산출물 표)
$ grep -A 8 "Phase 04 산출물" 01_Phases/youngho/M3.5-harness-v1/New_Harness/README.md | head -10
### Phase 04 산출물 (완료 — commits `a42dbdc` + `c6b1402` + (3/3) 본 commit)
| 옛 | 새 `New_Harness/knowledge/` + `agents/knowledge-gc.md` | 변경 |
# → PASS (옛 memory / CHANGELOG / 학습 일지 → knowledge 시드 매핑 6행)

# 6. 옛 운영 100% 작동
$ dotnet build Dawnholder.slnx --nologo 2>&1 | tail -3
빌드했습니다.
    경고 0개
    오류 0개
경과 시간: 00:00:03.19
# → PASS (옛 .claude/agents/ 7개 + CHANGELOG + memory 그대로, 빌드 green)
```

**결과**: 완료 조건 6/6 PASS. Phase 04 done.

---

## 결정 흐름 (학습 일지 쓸 때 참고용)

### 1. GC Collector 위치 — 별 SubAgent vs qa sub-mode

- **갈래**:
  - A) 별 SubAgent (knowledge-gc 신설, 풀 8 → 9 확장)
  - B) qa SubAgent의 sub-mode (풀 8 유지, 책임 추가)
- **채택**: A (별 SubAgent)
- **이유**: qa = *바이너리 검증 도메인*, GC = *문서 관리 도메인* — 본질이 다름. Phase 02 학습(`subagent-pool-expansion-pattern` ★★★)이랑 정합 = 옛 모호 영역 해소 정신 + 풀 8 → 9 자연 확장이 Specialist 카테고리 견고함 검증

### 2. 시드 항목 수 + 분포 — *처음부터 풀세트* vs *시드만*

- **갈래**:
  - A) 옛 학습 일지 패턴 = 처음부터 풀세트 박음 (15~20건 예상)
  - B) 시드 5~10건만, 유기적 누적 (5~10건)
- **채택**: B (8건 균형점 — cross-cutting 4 / client 2 / shared 1 / server 1 / qa 0)
- **이유**: 옛 운영의 *학습 일지 처음부터 풀세트*는 가짜 학습 누적 사고 (회고체 박혀서 AI 활용 X). 시드 + 유기적 누적이 *AI 활용 가치*가 검증된 항목만 캐시화. qa는 M4 진입 후 자연 누적

### 3. 옛 자산 처리 — 복사 vs 재작성 vs 유지

- **갈래**:
  - A) 옛 memory / CHANGELOG / 학습 일지를 *복사* (옛 톤 그대로 신 캐시 흡수)
  - B) *재작성* (회고체 → 구조화 패턴, AI 가독성)
  - C) *유지* (옛 자산 그대로, 신 캐시는 완전 신규)
- **채택**: B + C 혼합 — 옛 자산 *유지* + 신 캐시는 *재작성*
- **이유**: 옛 자산은 *개인 영역 / 시간순 이력 / 트랙 B 회고용*으로 가치. 새 캐시는 *AI 직접 활용*이 본질 → 회고체 X 구조화 패턴 (증상 / 패턴 / 봉합 / 사례 / 확신도 / 관련 키워드). 복사 마이그는 두 톤 섞임 → 가치 ↓

### 4. `_gc-policy.md` 별 파일 박지 않음 — 단일 진실 공급원

- **갈래**:
  - A) Phase 04 정의대로 `knowledge/_gc-policy.md` 별 파일 박음
  - B) `policies/knowledge-system.md` (Phase 01) + `agents/knowledge-gc.md` (Phase 04)로 통합
- **채택**: B (중복 회피)
- **이유**: GC 정책은 *정책 영역* → `policies/knowledge-system.md` 5섹션 이미 박힘. GC 실행 명세는 *SubAgent 영역* → `agents/knowledge-gc.md` 4종 절차 박힘. 별 파일은 중복 → 정합 갱신 비용 ↑. 정책 단일 진실 공급원 정신 (constitution-partial-update-trap 학습 ★★★ 정합)

---

## 학습 일지 후보 키워드

### ★★★ (3건)

- **`knowledge-system-track-a-vs-b-split`** — AI 캐시(트랙 A) vs 본인 회고 일지(트랙 B) 분리 정신. 양쪽 양식 분리 — 트랙 A = 구조화 패턴(증상/패턴/봉합), 트랙 B = 회고체. 한 사건이 양쪽에 박힐 수 있으나 *시각이 다름*. 가짜 학습 방지의 핵심 인프라. 한국 게임 회사 면접 *AI 활용 의사결정* 어필 결정타.
- **`seed-with-restraint-pattern`** — *처음부터 풀세트 박지 않음* 정신. 시드 5~10건만 박고, 실제 누적은 *유기적*. M3.5 시점 8건 (cross-cutting 4 / client 2 / shared 1 / server 1 / qa 0). 옛 운영의 *학습 일지 처음부터 풀세트 박음*과 대비 — 시드 + 유기적 누적이 가짜 학습 누적 차단.
- **`ai-self-reinforcement-bias-prevention`** — AI 자율 박제 = AI가 박은 패턴 → 자기가 인용 → 검증 없는 순환. 봉합 = 사용자 확인 게이트 (`-DONE.md` 직후 / CHANGELOG 직후 / 명시 요청 3 트리거). 본 Phase에서 본 약속을 *모든 _usage.md 박는 절차*에 박음. 한국 게임 회사 면접 *AI 검증 의사결정* 어필.

### ★★ (2건)

- **`specialist-pool-organic-expansion`** — 풀 8 → 9 자연 확장. Phase 02에서 박은 카테고리화(Worker/Reviewer/Specialist)가 *Phase 04 신설 SubAgent*를 자연 흡수. 분류 카테고리 견고함 검증 — 새 책임 발생 시 카테고리 안 자연 누적 vs 옛 운영의 *6 도메인 평면 구조*와 대비. 분류 설계가 미래 확장 비용 결정.
- **`legacy-asset-migration-as-rewriting`** — 옛 memory / CHANGELOG / 학습 일지 → 새 knowledge로 *복사가 아니라 재작성*. 회고체 → 구조화 패턴. *복사 마이그*는 옛 톤 잔존 + AI 활용 X. *재작성 마이그*는 AI 가독성 + 트랙 A 정신 정합. 옛 자산은 *유지* (개인 영역 / 시간순 이력 / 트랙 B 분리 정신).

### ★ (1건)

- **`gc-policy-single-source-of-truth`** — `_gc-policy.md` 별 파일 박지 않음. 정책은 `policies/knowledge-system.md` 단일 위치, 실행 명세는 `agents/knowledge-gc.md`. 중복 회피 = 정책 단일 진실 공급원 정신. 옛 정의 파일 9줄의 *별 파일* 가이드가 정합 검토에서 *중복으로 판명* → 정의 변경.

---

## 본인 회고 영역

> 이 섹션은 사용자(영호) 본인이 학습 일지(트랙 B, Notion) 박을 때 채울 자리. AI는 *사실*만 박고 *회고*는 본인이 박음. 가짜 학습 방지 정신.

- (Phase 04에서 *나*는 무엇을 배웠나?)
- (트랙 A/B 분리 결정에서 *왜* 트랙 B는 본인 회고로 두기로 했나?)
- (시드 8건 선택에서 *어떤* 항목을 *왜* 강력 5건으로 골랐나?)

---

## Phase 정의 ↔ 실측 차이

| Phase 정의 박힌 항목 | 실측 | 사유 |
|---|---|---|
| `knowledge/_gc-policy.md` 별 파일 박음 | 박지 않음 — `policies/knowledge-system.md` + `agents/knowledge-gc.md`로 통합 | 중복 회피 (정책 단일 진실 공급원 정신) |
| GC Collector "별 SubAgent로 박음 또는 qa sub-mode" trade-off | 별 SubAgent 결정 (knowledge-gc Specialist 3번째) | qa = 바이너리 검증, GC = 문서 관리 — 본질이 다름. 풀 8 → 9 자연 확장 |
| 예상 5~7h | 본 세션 누적 박음 (소요 측정 안 함 — 본 세션 큰 호흡) | 다음 마일스톤 측정 도입 후보 |
| 시드 5~10건 | 8건 (균형점) | 사용자 명시 선택 |

---

## 산출물 위치

- 격리 폴더: `01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge/` + `agents/knowledge-gc.md`
- 정책 reference: `01_Phases/youngho/M3.5-harness-v1/New_Harness/policies/knowledge-system.md` (Phase 01 박힘)
- 매핑 표: `01_Phases/youngho/M3.5-harness-v1/New_Harness/README.md` (Phase 04 행 갱신)
- 헌법 풀 표: `01_Phases/youngho/M3.5-harness-v1/New_Harness/CLAUDE.md` (풀 8 → 9)
- 라우팅: `01_Phases/youngho/M3.5-harness-v1/New_Harness/agents/_routing.md` (knowledge-gc 행 추가)

3 commit hash:
- (1/3) `a42dbdc` — 골격 (7파일 / 597줄)
- (2/3) `c6b1402` — GC agent + 시드 8건 (5파일 / +368 -20)
- (3/3) 본 commit — 정합 갱신 + -DONE.md
