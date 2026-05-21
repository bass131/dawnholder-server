# Phase 04: Knowledge 시스템 + GC Collector

> **상태**: pending
> **마일스톤**: M3.5 — 새 하네스 v1 문서화
> **등급**: 대규모 (정량 4등급 중 4단계)
> **도메인**: `.claude/knowledge/` (신설 인프라)
> **담당**: 영호 단독
> **예상 소요**: 5~7h
> **산출물 위치**: `01_Phases/youngho/M3.5-harness-v1/New_Harness/knowledge/` + GC Collector SubAgent 정의
> **병렬 가능**: Phase 03 (Hook)과 의존성 X — 병렬 진행 OK

---

## 🎯 목표

5/20 의논 결과 박힌 *Knowledge 시스템 풀세트*를 박는다. 각 SubAgent의 도메인별 *학습 캐시*를 `.claude/knowledge/<domain>/` 폴더로 박고, GC Collector 에이전트가 *오래되거나 중복된 항목 정리*. PDF NDREAM 패턴 그대로.

---

## 🤔 왜 Knowledge 시스템인가 (옛 X → 새 풀세트)

**옛 운영의 한계**:
- 각 세션이 *백지에서 시작* → 같은 사고를 반복 (예: SAC On dotnet test 차단, ProjectSettings cloud ping-pong) → CHANGELOG 박아서 매 세션 재인지 → 시간 비용 ↑
- 학습 일지는 *본인 회고용* → AI가 직접 캐시 못함
- ADR/policies/CHANGELOG에 박힌 결정 = 메인 세션 컨텍스트에 들어오긴 하나 *도메인별 인덱싱 X* → 검색 비용 ↑

**새 운영의 가치**:
- 도메인별 _index.md 박혀 SubAgent가 *작업 시작 시 자동 조회* → 백지 비용 ↓
- GC Collector가 *주기적으로* (예: 주 1회 또는 큰 마일스톤 끝) 오래된/중복 항목 정리 → Knowledge 비대 차단
- 본인 학습 일지는 *그대로 유지* (트랙 B 분리) — Knowledge는 AI 작업 캐시

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (`New_Harness/CLAUDE.md`에 Knowledge 시스템 풀세트 한 줄 박힘)
- [ ] Phase 02 완료 (SubAgent 정의 — Knowledge 입출력 주체)
- [ ] (Phase 03과 병렬 OK)

---

## 📝 작업 내용

### 1. Knowledge 폴더 골격 (`New_Harness/knowledge/`)

도메인별 폴더 + 각자 `_index.md`:

- [ ] `New_Harness/knowledge/server/_index.md` — server SubAgent 캐시
- [ ] `New_Harness/knowledge/shared/_index.md` — shared SubAgent 캐시
- [ ] `New_Harness/knowledge/client/_index.md` — client SubAgent 캐시
- [ ] `New_Harness/knowledge/qa/_index.md` — qa SubAgent 캐시
- [ ] `New_Harness/knowledge/cross-cutting/_index.md` — 도메인 간 공통 (예: SAC dotnet test, Unity 6.4 함정)

각 `_index.md` 양식:
```markdown
# {domain} Knowledge Index

## 항목 (최근 박힌 게 위)

| 날짜 | 제목 | 출처 | 사용 빈도 | GC 후보 |
|------|------|------|-----------|---------|
| YYYY-MM-DD | ... | -DONE.md / ADR / 학습 일지 / ad-hoc | N회 | 옛/유지/응축 |
```

### 2. 파일 크기 한도 + 항목 양식

- [ ] 각 knowledge 항목 = 별 `.md` 파일 (예: `server/sac-dotnet-test-block.md`)
- [ ] 항목 크기 한도: **200줄** (옛 doc-thresholds 220줄과 정합)
- [ ] 항목 양식: frontmatter (`title` / `domain` / `created` / `last_used` / `source`) + 본문 (배경 / 증상 / 진단 / 봉합 / 학습)
- [ ] 220줄 초과 시 응축 또는 분해 (GC Collector 책임)

### 3. GC Collector 에이전트 정의 (`New_Harness/agents/knowledge-gc.md`)

- [ ] 별 SubAgent로 박음 (Phase 02의 8개 + 1 = 9개? — 또는 `qa` SubAgent의 sub-mode로 흡수 검토)
- [ ] 호출 트리거: 수동 슬래시 `/knowledge-gc` (Phase 05 산출물) 또는 마일스톤 끝 자동 권유
- [ ] GC 정책:
  - **삭제**: 1년 이상 사용 X + 다른 항목으로 *완전 대체됨*
  - **응축**: 200줄 초과 + 핵심만 추출 가능
  - **승격**: 사용 빈도 ↑↑ + 학습 가치 ★★★ → ADR 박제 권유 (사용자 결정)
  - **분해**: 한 항목에 도메인 여러 개 섞임 → 분리

### 4. SubAgent의 Knowledge 입출력 패턴 (`New_Harness/knowledge/_usage.md`)

- [ ] SubAgent 작업 시작 시 `_index.md` 조회 → 관련 항목 1~3개 자동 Read
- [ ] 작업 종료 후 *새 학습이 있으면* knowledge 박을지 판단 (사용자 확인 받음 — AI 자율 박제 X, 가짜 학습 방지)
- [ ] knowledge 박힘 = `_index.md`에 한 줄 추가 + 별 `.md` 박힘

### 5. 옛 자산 마이그 (옛 → 새 Knowledge 흡수)

- [ ] 옛 CHANGELOG의 [M]/[H] 사고 박제 항목 → cross-cutting/ 흡수 후보
- [ ] 옛 `~/.claude/.../memory/` 항목 → cross-cutting/ 흡수 후보 (예: `sac-dotnet-test-block.md` / `unity-version-hash-pinning.md`)
- [ ] 옛 학습 일지 ★★★ 항목 중 *AI 작업에 직접 영향* 있는 것 → 도메인 knowledge로 흡수 (예: γ 방식 사전 검증 → plan-auditor knowledge)
- [ ] 옛 학습 일지 ★★★ 중 *본인 회고용*은 그대로 유지 (트랙 B)

### 6. 옛 → 새 매핑 표 갱신 (`New_Harness/README.md`)

- [ ] CHANGELOG / memory / 학습 일지 → 새 knowledge 매핑 행 추가

---

## ✅ 완료 조건

- [ ] `New_Harness/knowledge/` 5 도메인 폴더 + 각자 `_index.md` 박힘
- [ ] 항목 양식 + 파일 크기 한도 명세
- [ ] GC Collector SubAgent 정의 (또는 qa SubAgent sub-mode 결정)
- [ ] 옛 자산 마이그 표 (CHANGELOG/memory/학습 일지 → knowledge 매핑)
- [ ] *시드 항목* 5~10개 박음 (옛 CHANGELOG/memory에서 핵심만 추출 — Phase 06 전환 후 SubAgent가 실제 조회)
- [ ] 옛 운영 100% 작동

---

## 🧪 테스트

**자동**: 옛 운영 sanity check
- 옛 학습 일지 그대로 작동 (정책 변경 X)

**수동**:
- 시드 항목 5건 본인 눈으로 통독 (양식 검증)
- *가상 시나리오*: server SubAgent가 작업 시작 시 `knowledge/server/_index.md` 조회 → 관련 항목 Read → 작업 진행 → 새 학습 박을지 판단. 흐름 시뮬레이션
- GC 시나리오 3건 본인 눈으로 점검 (삭제/응축/승격 판단 기준 합리적인지)

---

## 📚 학습 포인트

- **Knowledge = AI 캐시, 학습 일지 = 본인 회고**: 트랙 B 분리 정신. 가짜 학습 방지 + AI 백지 비용 ↓
- **GC Collector 필요성**: 캐시는 *비대해지면 가치 ↓* → 주기적 정리 필수. PDF NDREAM 패턴의 *Knowledge 위생*
- **사용 빈도 추적**: 어느 항목이 자주 조회되는지 기록 → GC 판단 + ADR 승격 후보 자동 식별
- **시드 항목의 가치**: 본 Phase는 *인프라*만 박고 시드는 5~10개만. 실제 캐시 채움은 M4+ 작업 진행하며 *유기적* 누적. 옛 학습 일지 처럼 처음부터 풀세트 박지 않음
- **knowledge-gc 슬래시화**: Phase 05에서 박힐 신규 슬래시 후보 (마일스톤 끝 자동 권유)

---

## ⚠️ 함정 / 주의사항

- **knowledge 양식이 학습 일지와 *과도하게 유사*하면 트랙 B 분리 의미 X**: knowledge는 *AI 직접 활용용* — 한국어 설명보다 *구조화된 패턴/조건/해결* 위주. 학습 일지는 회고/인터뷰 스타일
- **시드 항목 박을 때 옛 학습 일지 *복사 X***: 본인 회고체 그대로 박으면 SubAgent가 못 활용. *AI 가독성*으로 재작성
- **GC 자동 실행 금지**: GC = *사용자 확인 후* 실행. 자동 삭제는 학습 자산 유실 위험
- **승격 판단 = 사용자 결정**: GC가 "ADR 승격 후보" 표시만, 박는 건 사용자 (가짜 ADR 방지)

---

## ➡️ 다음 Phase

- **Phase 05 — 슬래시 정리 + 신규 2개** (`/knowledge-gc` 슬래시 후보 박힘)
- 의존성: 본 Phase 04의 knowledge 시스템이 슬래시 호출 대상

---

## 📋 박제 (완료 후 -DONE.md)

- knowledge 도메인 5 + 시드 항목 5~10개 박힘
- GC 정책 4종 (삭제/응축/승격/분해) 명세
- 옛 → 새 자산 마이그 표 최종본
- 학습 키워드 후보 (knowledge vs 학습 일지 분리 / GC 패턴 / 시드 + 유기적 누적 etc)
