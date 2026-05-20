# Phase 01: 새 헌법 + docs/ 다이어트 (초안 박기)

> **상태**: pending
> **마일스톤**: M3.5 — 새 하네스 v1 문서화
> **등급**: 복잡 (정량 4등급 중 3단계)
> **도메인**: 문서 (CLAUDE.md / 00_Document/policies/)
> **담당**: 영호 단독 (새 하네스 v1 단독 통제 약속)
> **예상 소요**: 2~3h
> **산출물 위치**: `01_Phases/youngho/M3.5-harness-v1/New_Harness/` 폴더 안 (옛 CLAUDE.md / policies/ 직접 수정 X — Phase 06 전환 시점에 일괄 mv)

---

## 🎯 목표

5/20 의논 결과 박힌 *새 하네스 v1 모델*의 헌법 초안과 정책 묶음을 `New_Harness/` 폴더 안에 *별 파일*로 박는다. 옛 운영(`CLAUDE.md` + `00_Document/policies/`)은 그대로 두고, Phase 06 정합 마감 시점에 옛 → 새 전환 commit으로 일괄 교체.

---

## 🤔 왜 *별 파일*인가 (산출물 격리 결정)

- **옛 운영 깨뜨림 금지**: Claude Code가 매 세션 자동 로드하는 `CLAUDE.md`를 작업 중에 *반쪽 갱신*하면 본인이 다음 호출에서 깨진 하네스로 작업하게 됨 (자기 발등 찍기)
- **점진적 검토 가능**: 새 헌법 초안을 옛 헌법과 *나란히 두고* diff 비교 → 빠뜨린 절대 원칙 / 과도한 다이어트 / 누락된 정책 자동 점검
- **롤백 비용 X**: Phase 06 전환 *전*까지는 옛 운영 100% 작동. 새 하네스가 마음에 안 들면 New_Harness/ 폴더만 버리면 끝
- **Phase 06 = 전환 commit 1회**: 옛 파일 삭제 + 새 파일 mv + import 경로 정정 = 한 commit으로 깔끔히 시점 분리

---

## ⏪ 사전 조건

- [ ] M3 마감 완료 (PR #41 main `ac2d302` 머지)
- [ ] `youngho/harness-v1` 브랜치 (from main `ac2d302`)
- [ ] `_milestone-plan.md` 통독 — 6 Phase 분해 + 의존성 그래프 인지

---

## 📝 작업 내용

### 1. New_Harness/ 폴더 신설 + 기본 골격

- [ ] `01_Phases/youngho/M3.5-harness-v1/New_Harness/` 디렉토리 생성
- [ ] `New_Harness/README.md` — 폴더 목적 + 옛 → 새 매핑 표 (어느 옛 파일이 어디로 가는지)
- [ ] `New_Harness/CLAUDE.md` — 새 헌법 초안 (절대 원칙 5개 유지 + 운영 양식 절 다이어트)
- [ ] `New_Harness/policies/` — 새 정책 묶음 (옛 policies/ 응축 + 5/20 의논 결과 흡수)

### 2. 새 헌법 초안 박기 (`New_Harness/CLAUDE.md`)

- [ ] **절대 원칙 5개 그대로** (서버 권위 / 프로토콜 신성 / 신뢰 경계 / 공유 코드 규율 / 틱 블로킹) — 옛 헌법에서 그대로 복사
- [ ] **운영 양식 절 다이어트**:
  - work-envelope 양식 *삭제* (5/20 의논에서 죽이기로 결정)
  - 5단계 보고 = *대규모 등급 Phase 완료 시만* (옛 모든 코드 응답 끝 → 신규 조건부)
  - work-pin = *유지하되 압축* (옛 60줄 → 새 30~40줄 목표)
  - `-DONE.md` 박제 = *유지* (포트폴리오/캡스톤 평가 자산)
- [ ] **새 절: 정량 4등급** — 단순/보통/복잡/대규모 정의 + 위험 Hook 자동 상향 (trust-boundary/irreversible/unity-asset)
- [ ] **새 절: 모델 분담** — Sonnet (Worker) / Opus (Coordinator + Reviewer) PDF NDREAM 패턴
- [ ] **새 절: SubAgent 풀 8** — server/shared/client/qa + reviewer/plan-auditor/unity-bridge/coordinator (각 정의는 Phase 02 산출물 reference)
- [ ] **새 절: Phase 폴더 namespace** — `<영호|유현|인규>/M{N}-...` + frontmatter `owner:`
- [ ] **새 절: 보고서 양식** — MD + HTML 이중 박음 (캡스톤 평가 자산)
- [ ] **사용자 컨텍스트 절 유지** — 학부생 멘토링 톤은 그대로

### 3. 새 정책 묶음 (`New_Harness/policies/`)

- [ ] **옛 policies/ 통독 후 응축**:
  - `reporting-format.md` → 조건부 5단계 보고 + work-envelope 삭제 반영
  - `pin-and-done.md` → work-pin 압축본 양식 + -DONE.md 박제 유지
  - `doc-thresholds.md` → 새 등급 체계 정합 (옛 220줄 임계 유지 검토)
  - `review-tiering.md` → 새 SubAgent reviewer + plan-auditor 정합 재작성
- [ ] **신규 정책 추가**:
  - `grade-and-risk.md` — 정량 4등급 + 위험 Hook 자동 상향 패턴
  - `subagent-routing.md` — SubAgent 풀 8 라우팅 (옛 도메인 6 매핑 → 8개로 확장)
  - `knowledge-system.md` — Phase 04 산출물 reference (knowledge GC + 도메인 _index)

### 4. 옛 → 새 매핑 표 (`New_Harness/README.md`)

- [ ] 옛 파일 한 줄 / 새 파일 한 줄 / 변경 사유 한 줄 = 표 형식
- [ ] 삭제 대상 명시 (work-envelope 양식, 옛 도메인 6 라우팅 표 등)
- [ ] *유지* 대상 명시 (절대 원칙 5개, 사용자 컨텍스트 절, ADR 우선순위 표)

---

## ✅ 완료 조건

- [ ] `New_Harness/` 폴더 + README.md + CLAUDE.md + policies/ 4~6 파일 박힘
- [ ] 옛 헌법과 새 헌법 *나란히 두고 diff* 가능 (이름 충돌 X — 새 헌법은 `New_Harness/CLAUDE.md`)
- [ ] 옛 운영 100% 작동 (이 Phase 진행 중 옛 슬래시/훅/SubAgent 호출 모두 정상)
- [ ] 옛 → 새 매핑 표가 *Phase 06 전환 commit 시 어느 파일을 어디로 mv할지* 명확
- [ ] Phase 02~06 정의 .md가 이 Phase 01 산출물에 정합 (헌법이 SubAgent/Hook 권한 정의)

---

## 🧪 테스트

**자동**: 옛 운영 sanity check
- `dotnet build Dawnholder.slnx --nologo` green 유지 (헌법 변경이 빌드에 영향 X)
- `dotnet test Dawnholder.slnx --nologo` 170 PASS 유지 (M3 baseline)

**수동**:
- 옛 슬래시 1개 호출해 정상 작동 확인 (예: `/learn:recap`)
- 옛 reviewer SubAgent 호출 시 정상 응답 (Phase 02 산출물이 옛 reviewer를 *대체*하는 건 Phase 06에서)
- `New_Harness/CLAUDE.md`와 옛 `CLAUDE.md`를 본인 눈으로 diff — 빠진 절 / 과도한 다이어트 / 의도 안 맞는 부분 점검

---

## 📚 학습 포인트

- **점진적 마이그 패턴 — 격리 폴더**: 옛 운영 깨뜨림 없이 새 모델 박는 한국 게임 회사 표준 패턴 (DB 마이그의 dual-write phase와 유사)
- **헌법 부분 갱신의 함정**: 절대 원칙은 절대 갱신 X. 운영 양식만 다이어트. *경계 명확화*가 핵심
- **work-envelope 죽이기 결정**: 옛 양식이 매 코드 응답에 봉투 첨부 → AI/사용자 둘 다 노이즈 부담 → 5/20 의논에서 죽이기 결정. *양식이 가치를 만드는지 노이즈를 만드는지*가 헌법 운영 결정의 핵심 기준
- **새 등급 체계 도입 이유**: 옛 운영은 등급 없이 *모든 Phase를 같은 무게로* 처리 → 단순 변경에도 과도한 양식 부담. 새 등급은 *작업 무게 → 양식 부담* 1:1 매핑

---

## ⚠️ 함정 / 주의사항

- **옛 `CLAUDE.md` 절대 직접 수정 금지** — Claude Code 다음 세션 자동 로드 시 반쪽 갱신본 잡아서 사고. 모든 새 정의는 `New_Harness/` 안
- **절대 원칙 5개를 응축한답시고 표현 바꾸기 금지** — 글자 그대로 복사. 의미 미세 변경이 보안/동기화 구멍 만듦 (서버 권위 / 프로토콜 신성 / 신뢰 경계 / 공유 코드 규율 / 틱 블로킹)
- **다이어트 → 누락 검증**: 옛 헌법 175줄을 새 헌법으로 응축 시 *빠진 게 없는지* 매핑 표로 reverse check
- **새 SubAgent 정의는 Phase 02 산출물 — 헌법엔 *이름만* 박음**: 헌법에 SubAgent 디테일 박으면 Phase 02 산출물과 충돌. 헌법 = "SubAgent 풀 8개: [목록]" 한 줄 + Phase 02 산출물 reference

---

## ➡️ 다음 Phase

- **Phase 02 — SubAgent 풀 8 정의** (등급:대규모)
- 의존성: 본 Phase 01의 헌법이 SubAgent 권한·도메인 본질 정의

---

## 📋 박제 (완료 후 -DONE.md)

- 옛 → 새 매핑 표 최종본
- 다이어트로 *지운* 절 목록 + 사유
- 새로 *추가*한 절 목록 + 사유
- 옛 운영 sanity check 결과 (테스트 PASS 유지 확인)
- 학습 키워드 후보 (점진적 마이그 / 헌법 부분 갱신 / 양식 비용 평가 etc)
