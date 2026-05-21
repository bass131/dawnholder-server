---
owner: youngho | yuhyeon | inkyu
milestone: M1 | M2 | M3 | M3.5 | M4 | ...
phase: NN
title: Phase 제목
status: pending | in-progress | done | blocked
grade: 단순 | 보통 | 복잡 | 대규모
risk: (옵션) trust-boundary | irreversible | unity-asset
estimated: 1~3h (단순/보통) | 2~5h (복잡) | 5~12h (대규모)
domain: server | shared | client | qa | cross
---

# Phase NN: [Phase 제목]

> **상태**: pending | in-progress | done | blocked
> **마일스톤**: M3.5 / M4 / ...
> **등급**: (4 정량 — 본 frontmatter 정합)
> **담당**: (frontmatter `owner:` 정합)

---

## 🎯 목표

> 이 Phase가 끝나면 무엇이 동작해야 하는가? 한두 문장으로.

_(예: 서버가 TCP 포트 7777에서 클라이언트 연결을 받아 Session 객체로 관리할 수 있다.)_

---

## ⏪ 사전 조건

- [ ] _(예: Phase 01 — 서버 프로젝트 부트스트랩 완료)_
- [ ] _(예: Shared 라이브러리 csproj 참조 가능)_

---

## 📝 작업 내용

> 의미 있는 단위로. 등급별로 분량 다름:
> - 단순: 1~3 체크리스트, 1 파일
> - 보통: 3~7 체크리스트, 2~3 파일
> - 복잡: 7~15 체크리스트, 다 도메인
> - 대규모: TaskCreate로 내부 분해 권장 (Team SubAgent 동원)

- [ ] _(예: TcpListener를 server/Network/에 구현)_
- [ ] _(예: Session 클래스 정의 (ID, 소켓, 연결 시각))_

---

## ✅ 완료 조건

> 어떻게 "끝났다"를 객관적으로 판단할지.

- [ ] _(예: `dotnet run` 으로 서버 시작 시 "Listening on :7777" 로그 확인)_
- [ ] _(예: 단위 테스트 N개 모두 통과)_

---

## 🧪 테스트

**자동**:
- _(예: SessionManagerTests — 동시 100개 연결 처리)_

**수동**:
- _(예: 서버 켜고, 별도 터미널에서 `nc localhost 7777` 5회 반복)_

---

## 📚 학습 포인트

> 이번 Phase에서 새로 만나는 개념. 본인 노션 트랙 B에 박을 후보.

- _(예: TcpListener vs Socket — 추상화 레벨)_
- _(예: async/await가 왜 네트워크 코드에 적합한가)_

---

## ⚠️ 함정 / 주의사항

- _(예: AcceptTcpClientAsync를 await하지 않으면 한 명만 받고 멈춤)_

---

## ➡️ 다음 Phase

- _(예: Phase 03 — Packet Framing)_

---

## 📋 박제 (완료 후 -DONE.md)

> 등급별 분기 (정책 = [`00_Document/policies/reporting-format.md`](../00_Document/policies/reporting-format.md)):
> - **단순/보통**: work-pin + commit message만 충분 (-DONE.md 박지 않음)
> - **복잡**: -DONE.md 박음 (요약 + 사실 박제 + 학습 키워드 후보)
> - **대규모**: -DONE.md + 5단계 보고 (🎯 무엇 / 🤔 왜 / 🛠️ 어떻게 / 🧪 테스트 / ➡️ 다음) MD + HTML 이중 박음 (캡스톤 평가 자산)

박제 시점에 `phase-gate-validator.sh` Hook이 frontmatter + 등급별 의무 섹션 자동 검사.

---

## 작업 로그

> Phase 진행하면서 발견된 이슈, 결정, 메모.

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
