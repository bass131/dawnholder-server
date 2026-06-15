---
owner: youngho
milestone: M6
phase: 05
title: 통합 플레이테스트 + 클로즈아웃
status: pending
grade: 복잡
risk: irreversible
estimated: 3~5h
domain: cross
summary: 6개 폴리시 항목 통합 검증 + WSL2 회귀 게이트 + -DONE.md/HTML 박제 + (영호 GO 시) PR/머지
---

# Phase 05: 통합 플레이테스트 + 클로즈아웃

> **상태**: pending
> **마일스톤**: M6
> **등급**: 복잡 (Opus plan-auditor 권고 보통→복잡 상향: irreversible 깃발 + Phase 01~04 외관 부채 일괄 회수 + -DONE.md/HTML 박제 의무 + push/PR/머지 = 보통이 정의-frontmatter 불일치)
> **담당**: cross + 영호 육안

---

## 🎯 목표

Phase 01~04의 변경을 통합 플레이테스트로 함께 검증하고, 회귀가 없는지 게이트를 통과한 뒤
M6를 박제(-DONE.md/HTML)하고 영호 GO 시 main에 머지한다.

---

## ⏪ 사전 조건

- [ ] Phase 01~04 전부 완료 (Animator / Sorting / HUD / Dialog)

---

## 📝 작업 내용

- [ ] 6개 항목 통합 플레이테스트 (영호 + 서버 기동): C 보스 모션 / D 일반몹 모션 / E 렌더 순서 / A 파티 HUD 위치 / B 퀘스트 텍스트 / F NPC 대화
- [ ] 발견된 미세 이슈 보정 (있으면 해당 Phase로 회귀하지 말고 작은 fixup)
- [ ] WSL2 sync+build+test 회귀 게이트 (baseline 644/0/5)
- [ ] 클라이언트 빌드 (`C:\Dev\Build\Client\03_Client.exe`) + 영호 빌드 육안
- [ ] `_milestone-DONE.md` + HTML 시각화 박제 (복잡 이상 마일스톤)
- [ ] **영호 명시 GO 후**: push → PR 생성 → (코드 PR이면 CODEOWNERS admin 예외) → main 머지
- [ ] 머지 후 work-pin을 다음 마일스톤(M7-sound) 전환 상태로 갱신

---

## ✅ 완료 조건

- [ ] 6개 항목 모두 영호 육안 OK
- [ ] WSL2 회귀 게이트 green
- [ ] -DONE.md/HTML 박제 완료
- [ ] (GO 시) main 머지 완료 + ProtocolVersion 상태 명시

---

## 🧪 테스트

**수동 (영호 육안)**: 6개 항목 통합 시나리오
**자동**: WSL2 회귀 644/0/5

---

## 📚 학습 포인트

- 폴리시 마일스톤의 클로즈아웃은 "신규 기능 데모"가 아니라 "체감 품질 before/after 확인"이 핵심.
- 여러 작은 변경을 한 번에 머지할 때 회귀 게이트가 안전망인 이유.

---

## ⚠️ 함정 / 주의사항

- push/PR/머지는 비가역 — **영호 명시 GO 의무** (헌법 PR 게이트). AI 자율 진행 X.
- PR 본문에 bypass 보안 키워드 literal 박지 않기 (Auto Mode classifier 정합 — 풀어쓰기).
- 클라 로컬 퀘스트 텍스트 선택이면 ProtocolVersion 무변경 — 머지 메시지에 정확히 기재.

---

## ➡️ 다음 마일스톤

- M7 — 사운드 (전면 사운드 적용)
