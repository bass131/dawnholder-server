---
owner: youngho
phase: 01
status: pending
grade: 복잡
summary: CODE_CONVENTION에 §6 주석 정책 + §2.2 데이터엔티티/God class 구분 + naming·폴더 계층 규칙 신설 + §5 강제(reviewer 축/SubAgent) 반영 — 전체 정리의 기준 확정
---

# Phase 01: CODE_CONVENTION 보강 (정리 기준 확정)

> **등급**: 복잡 (문서 + `.claude/` self-mod 스테이징)
> **담당**: 메인 세션 (문서) — `.claude/` 변경은 스테이징 → 본인 적용
> **선행**: 모든 정리 Phase(02~05)의 *기준*. 이게 먼저 확정돼야 정리가 일관됨.

---

## 🎯 목표

CODE_CONVENTION에 **주석 정책(§6)**과 **데이터 엔티티 vs God class 구분(§2.2 보강)**, **naming+폴더 계층 규칙**을 명문화한다. 현재 Convention은 God class·패턴·포매팅만 다루고 주석 밀도는 공백 → server SubAgent 주석 범벅이 안 걸린 근본 원인. 기준을 못 박아 02~05 정리가 "이상적 도착점"을 갖게 한다.

---

## ⏪ 사전 조건

- [x] ADR-028 + CODE_CONVENTION v4 존재 (보강 대상)
- [x] EnemyEntity 주석 범벅 사례 + GameMap God class(부록 A) 확인

---

## 📝 작업 내용

### CODE_CONVENTION.md 보강
- [ ] **§6 주석 정책 신설**:
  - 6.1 코드가 말하게 — "무엇/어떻게"는 코드, 주석은 "왜"만
  - 6.2 금지 — (a) 자명한 재진술 (b) 역사·Phase 박제(`M3 Phase 06 Step 2` → git blame) (c) 폐기된 사고과정·대안검토(→커밋/DONE) (d) backlog/TODO 남발 (e) internal 멤버 기계적 XML doc
  - 6.3 허용(5%) — 코드만 봐선 *왜*인지 모르고 안 적으면 사고나는 비자명 결정 근거(보안·프로토콜·헌법 함정). 예: `C_Attack`에 attacker 없는 이유. 바로 위 1~2줄
  - 6.4 위치 — "왜"는 해당 코드 바로 위. 파일 상단 대형 주석 블록 지양
- [ ] **§2.2 보강** — "데이터 엔티티(상태 가방, 필드 多 OK — 로직 없음) ≠ God class(로직 다도메인 — 분리)" 구분 명문화. 필드 수 ≠ God class 신호
- [ ] **naming + 폴더 계층 규칙 신설** (§2.x 또는 §3 확장) — 기능별 분리 클래스는 폴더 계층으로(`Maps/Systems/CombatSystem.cs`), 폴더명이 역할 표현. "나뉘니 어려우면 한 클래스에 넣기"는 안티패턴 — naming+계층으로 탐색성 확보

### 강제 메커니즘 (§5 반영 — `.claude/` self-mod = 스테이징)
- [ ] **reviewer `REVIEW_CHECKLIST`** — "Code Convention" 축(§5.2)에 "주석 노이즈(§6 위반)" 점검 추가. 스테이징 → 본인 적용
- [ ] **SubAgent 정의**(server/client/shared) — "코드 작성 시 §6 주석 정책 준수" 한 줄. 스테이징 → 본인 적용
- [ ] CODE_CONVENTION 변경 이력 v5 + ADR-028 본문에 "주석 정책 추가" 한 줄

---

## ✅ 완료 조건

- [ ] `CODE_CONVENTION.md`에 §6(주석) + §2.2 보강(데이터/God 구분) + naming/계층 규칙 박힘, 변경 이력 v5
- [ ] reviewer REVIEW_CHECKLIST 주석 축 + SubAgent 주석 규칙 = **스테이징 디렉토리에 cp** (`.claude/staging/`), 본인 `! cp` 적용 안내
- [ ] §6 적용 시 EnemyEntity가 어떻게 되는지 **before/after 1파일 시범** (코드 40 / 주석 70 → 코드 40 / 주석 ~5) — 02의 기준점
- [ ] 문서만 변경, 코드 0 변경 (빌드 영향 없음)

---

## 📚 학습 포인트

- **선언 ≠ 강제**: ADR-028 핵심 — ADR/문서에 "잘 쓰자" 적어도 안 지켜짐. *자동 점검 메커니즘*(reviewer 축/hook)이 있어야 산다. 주석 정책도 §5 강제 없으면 또 범벅.
- **결정적 규칙**: "되도록 간결히"가 아니라 "(a)(b)(c)는 금지 / (5%)만 허용"처럼 *못 박아야* 판단이 흔들리지 않음 (§0.1 기반 부채 방지).

---

## ⚠️ 함정 / 주의사항

- **`.claude/` self-modification 하드차단**: agent/command 정의 직접 편집 X → `.claude/staging/`에 cp 후 사용자 `! cp` 적용 (work-pin baseline). 적용 안 하면 강제 무력(ADR-028이 경고한 갭).
- **§6을 과하게 만들지 말 것**(§0.3): 주석 정책 자체가 장황하면 모순. 결정적 + 간결.
- **5% 예외 남용 경계**: "이건 비자명해"를 핑계로 다 남기면 정리 의미 X. 보안·프로토콜·헌법 함정만 — 그 외는 코드로.

---

## ➡️ 다음 Phase

- Phase 02·03 — 이 기준으로 서버/클라 주석 정리 (병렬)

---

## 📋 박제 (완료 후)

- **복잡 등급** — `01-convention-augment-DONE.md`.

---

## 작업 로그

- 2026-05-30: 계획 수립 (`/work:plan` 코드베이스 정리)
