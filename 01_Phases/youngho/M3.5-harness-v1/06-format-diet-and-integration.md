# Phase 06: 양식 다이어트 + 정합 마감 + 옛 → 새 전환 commit

> **상태**: pending
> **마일스톤**: M3.5 — 새 하네스 v1 문서화 (마감)
> **등급**: 복잡 (정량 4등급 중 3단계)
> **도메인**: 전 영역 (양식 + 정합 + 전환 commit)
> **담당**: 영호 단독
> **예상 소요**: 4~5h
> **산출물 위치**: 옛 영역 직접 갱신 (새 하네스 v1 *발효 시점*)

---

## 🎯 목표

Phase 01~05에서 `New_Harness/` 폴더 안에 박힌 새 하네스 v1 산출물을 옛 영역으로 *일괄 mv* + 옛 자산 *삭제/응축* + 양식 다이어트 정합 검증. 전환 commit 1회로 옛 → 새 발효. 그 후 M4 진입 준비 게이트.

---

## 🤔 왜 *한 commit*인가

- **반쪽 발효 차단**: 옛/새 섞인 상태로 commit 박으면 다음 세션이 *어느 양식 따라야 할지* 혼동 → 자기 발등 찍기
- **롤백 비용 명확**: 전환 commit 자체를 revert하면 옛 운영 100% 복구
- **포트폴리오 가치**: 새 하네스 v1 도입을 *한 git 시점*으로 박음 → 캡스톤 평가 자산
- **머지 PR 가시화**: PR 본문에 "M3.5 새 하네스 v1 발효" 한 줄로 명확

---

## ⏪ 사전 조건

- [ ] Phase 01~05 모두 완료 (`New_Harness/` 안에 풀세트 박힘)
- [ ] 옛 운영 100% 작동 유지 (Phase 01~05 진행 중 깨뜨림 X 확인)
- [ ] Phase 01에서 박힌 옛 → 새 매핑 표 최종본 (어느 파일이 어디로 mv할지 명확)

---

## 📝 작업 내용

### 1. 양식 다이어트 최종 검증

- [ ] **work-envelope 제거**: 옛 `.claude/hooks/check-work-envelope.sh` 삭제 + 옛 정책 `reporting-format.md`의 work-envelope 절 제거
- [ ] **5단계 보고 조건부화**: 옛 모든 코드 응답 끝 → 새 대규모 등급 Phase 완료 시만. policies + 헌법 정합
- [ ] **work-pin 압축**: 옛 60~70줄 → 새 30~40줄 목표. 압축 양식 `pin-injector.sh` 정합
- [ ] **`-DONE.md` 박제 유지**: 캡스톤 평가 자산이라 그대로
- [ ] **5단계 보고 양식 = MD + HTML 이중 박음**: HTML 템플릿 추가 (`.claude/templates/report-html-template.html`)

### 2. 옛 → 새 *일괄 mv* (전환 commit 본 작업)

순서 (의존성 따라):

- [ ] `New_Harness/CLAUDE.md` → `CLAUDE.md` (옛 헌법 삭제 후 mv)
- [ ] `New_Harness/policies/*` → `00_Document/policies/*` (옛 policies/ 삭제 후 mv)
- [ ] `New_Harness/agents/*` → `.claude/agents/*` (옛 agents/ 7개 삭제 후 새 8~9개 mv)
- [ ] `New_Harness/hooks/*` → `.claude/hooks/*` (옛 5개 삭제 후 새 7개 mv)
- [ ] `New_Harness/settings.proposed.json` → `.claude/settings.json` 정합 (deny/ask 절 유지 + hooks 절 갱신)
- [ ] `New_Harness/knowledge/*` → `.claude/knowledge/*` (신설 디렉토리 — 옛 운영에 없던 영역)
- [ ] `New_Harness/commands/*` → `.claude/commands/*` (옛 16개 중 8개 삭제 후 새 10개 mv)
- [ ] `New_Harness/README.md` → 본 Phase 06 -DONE.md 안에 흡수 (별 파일 X)
- [ ] `New_Harness/` 폴더 자체 삭제 (전환 완료)

### 3. import 경로 정정

- [ ] CLAUDE.md 안의 모든 `00_Document/policies/*` reference 정정 (새 파일 이름 정합)
- [ ] ADR `00_Document/ADR.md` + `00_Document/ADR/` 안의 reference 정정 (제거된 정책/슬래시 안내)
- [ ] `00_Document/commands-index.md` 갱신 (옛 16 → 새 10)
- [ ] `.claude/CHANGELOG.md`에 [H] entry 박음 (새 하네스 v1 발효 — 모든 팀원 영향)

### 4. Phase 폴더 namespace 정합

- [ ] `01_Phases/yuhyeon/` 폴더 신설 (현재 없음 또는 비어있음 점검)
- [ ] `01_Phases/inkyu/` 폴더 신설 (6월 말 합류 대비)
- [ ] `01_Phases/_template.md`에 새 frontmatter `owner:` 박음
- [ ] 옛 `learning-journal/` `<영호|유현|인규>/` 그대로 (트랙 B 영역, 변경 X)

### 5. 팀 셋업 가이드 갱신

- [ ] `00_Document/team-guide.html` — 새 하네스 v1 발효 한 줄 + 슬래시 카탈로그 갱신 + "막혔을 때" 표 정합
- [ ] `.claude/setup-steps/` 1~5 — 새 SubAgent 8 + Hook 7 + Knowledge 시스템 안내 박음
- [ ] README.md — L1 헌법 표 셀 갱신 + 폴더 구조 `.claude/knowledge/` 추가

### 6. ADR 박제

- [ ] 새 ADR 박음: **ADR-022 새 하네스 v1 (5/20 의논 결과)**
  - 옛 헌법 운영 패턴 → 새 모델 전환 배경
  - 8개 SubAgent + 7개 Hook + Knowledge + 슬래시 10개 결정 근거
  - PDF NDREAM 참조 + 팀 합류 흡수 + 작업 KPI 전환 명시
- [ ] 옛 ADR (ADR-019 Tier 2 등) 정정 (새 reviewer 정합 — *대체 X*, 보강만)

### 7. *최종* 테스트 — 옛 운영 깨뜨림 검증

- [ ] `dotnet build Dawnholder.slnx --nologo` green
- [ ] `dotnet test Dawnholder.slnx --nologo` 170+ PASS (M3 baseline 유지)
- [ ] 새 슬래시 1개 실제 호출 (예: `/harness-review all` — 본 Phase 산출물 자체를 점검)
- [ ] 새 reviewer SubAgent 호출 1회 (Tier 2 자동 호출 흐름 검증)
- [ ] 새 plan-auditor SubAgent 호출 1회 (`_milestone-plan.md` 검토 시뮬레이션)
- [ ] work-pin 압축 양식 1회 갱신 (실 측정 30~40줄 확인)

### 8. PR + 머지

- [ ] PR 본문 = "M3.5 새 하네스 v1 발효 (PDF NDREAM 패턴, 5/20 의논 결과)"
  - 변경 요약: 옛 헌법 175줄 → 새 N줄 / SubAgent 7 → 8 / Hook 5 → 7 / Knowledge 신설 / 슬래시 16 → 10
  - 호환성: 옛 슬래시 8개 제거 — 트랙 B Notion 이관 (학습 5 + 일지 3)
  - 검증: dotnet test 170+ PASS + 새 슬래시/SubAgent 실측 1회씩
- [ ] **영호 셀프 머지** (main 보호 정책)
- [ ] 머지 후 CONTEXT.md 갱신 (M3.5 마감 박제 + M4 진입 게이트 박음)

---

## ✅ 완료 조건

- [ ] `New_Harness/` 폴더 삭제됨 (전환 완료)
- [ ] 옛 운영 자산 정합 (옛 hook 5 → 새 7 / 옛 agents 7 → 새 8 / 옛 commands 16 → 새 10 / 옛 policies 4 → 새 N)
- [ ] `dotnet test` 170+ PASS
- [ ] 새 슬래시/SubAgent/Hook *최소 1회 실측* 통과
- [ ] ADR-022 박힘
- [ ] CHANGELOG [H] entry 박힘
- [ ] PR 본문 정합 + 머지 완료

---

## 🧪 테스트

**자동**:
- `dotnet build` + `dotnet test` 170+ PASS
- 새 hook 자동 발동 시나리오 (예: 코드 변경 → reviewer 자동 호출)

**수동**:
- `/harness-review all` 호출 → 새 하네스 자체 점검 (드물게 자기 점검 시나리오)
- `/cross-review youngho/harness-v1` 호출 → 본 Phase 06 산출물 외부 시각 점검
- 새 work-pin 양식 실제 박힘 (`.claude/state/current-pin.txt` 30~40줄 확인)
- 새 헌법 통독 — 옛 vs 새 diff 본인 눈으로 점검 (절대 원칙 5개 누락 X / 다이어트 합리적)

---

## 📚 학습 포인트

- **전환 commit 1회 정신**: 큰 마이그 = 한 시점 박음. 반쪽 발효 위험 + 롤백 비용 모두 ↓
- **양식 비용 평가의 가치**: work-envelope 죽이기 결정 = *양식이 노이즈 만드는지* 명시적 평가. 모든 양식은 *가치 vs 비용* 점검 대상
- **새 하네스 v1 ADR 박제**: 결정 *기록*의 가치. 1년 후 본인이 "왜 이렇게 박았더라" 검토 시 ADR-022가 단일 진실 공급원
- **포트폴리오 자산화**: 새 하네스 v1 도입 자체가 *바이브 도메인 시대 인식 + 투트랙 분리 + Zero-based 재구성*의 의사결정 사례. 한국 게임 회사 백엔드 면접에서 *방향성 결정/리팩터링* 어필 결정타

---

## ⚠️ 함정 / 주의사항

- **전환 commit 중 옛 운영 깨지면 본인이 다음 호출 못 함**: mv 작업 중 *atomic* 보장 — `git mv`로 한 번에, *commit 전에 본인 sanity check* (옛 슬래시 1개 + 새 슬래시 1개 호출)
- **`.claude/settings.json` 정합 시 deny 절 절대 건드림 금지**: secrets/curl/wget deny는 보안 가드. 새 hooks 절만 추가/교체
- **CHANGELOG [H] entry = 슬랙/디스코드 동반 안내 권장**: 옛 헌법 ε 결정 [H] (5/18)과 정합 — 모든 팀원 영향 변경은 슬랙 통보
- **PR 머지는 영호 셀프**: main 보호 정책 (5/20 의논 결정). 다른 팀원 PR 승인 강제 그대로 유지
- **CHANGELOG에 박을 때 `/cross-review` 추가 점검 권유**: 본인 한 사람 머리로 한 transition이라 사고 위험 ↑. Codex β 직접 검증 1회 권유 (옵션)

---

## ➡️ M3.5 마감 후 — M4 진입 게이트

- [ ] CONTEXT.md "현재 멈춤 지점" 갱신 = "M3.5 마감 + 새 하네스 v1 발효. M4 진입 준비 완료"
- [ ] work-pin = M4 새 WORK-ID로 갱신
- [ ] `/work:plan M4` 호출 *새 운영 첫 실측* — plan-auditor 자동 호출 흐름 검증
- [ ] M4 Phase 1 진입 — 새 SubAgent + Hook + Knowledge 풀세트 첫 작업

---

## 📋 박제 (완료 후 -DONE.md)

- 옛 → 새 일괄 mv 결과 표 (어느 파일 어디로 갔는지)
- 양식 다이어트 metric (work-pin 줄 수 / CLAUDE.md 줄 수 / 슬래시 수 / Hook 수 / SubAgent 수)
- 실측 결과 (새 슬래시 호출 1회씩 + reviewer 자동 호출 + dotnet test PASS)
- ADR-022 reference + CHANGELOG [H] entry
- 학습 키워드 후보 (전환 commit 정신 / 양식 비용 평가 / 새 하네스 v1 도입 의사결정 / 바이브 도메인 시대 인식 흡수 / 투트랙 분리 박제 etc)
- M4 진입 준비 게이트 명세
