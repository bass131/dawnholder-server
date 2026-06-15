---
owner: youngho
milestone: M5
phase: 01
title: PDL 8패킷 일괄 추가 + ProtocolVersion v14→v15 bump
status: pending
grade: 보통
risk: irreversible
domain: shared
estimated: 1h
---

# Phase 01: PDL 8패킷 일괄 추가 + ProtocolVersion v14→v15 bump

> **상태**: pending
> **마일스톤**: M5 (트랙 A — 파티 시스템 서버)
> **등급**: 보통 + **irreversible**(`ProtocolVersion` bump = wire break)
> **담당**: shared (Sonnet Worker)

---

## 🎯 목표

파티/퀘스트/포탈 기능에 필요한 **8개 패킷을 PDL.xml에 한 번에 append**하고, PacketGenerator로 재생성한 뒤 `Shared.dll`을 갱신하고, `ProtocolVersion`을 v14→v15로 **단일 bump** 한다. M5 전체의 신경계(프로토콜)를 먼저 깔아두는 Phase라 모든 트랙의 최선행이다.

> 왜 한 번에? 패킷을 Phase마다 조금씩 추가하면 마일스톤 내에서 `ProtocolVersion`을 여러 번 올려야 한다(매 bump = 비가역 wire break). 8개를 A0에서 일괄 정의하면 v15 한 번으로 끝난다. 이후 Phase에서 PDL을 또 건드리면 *설계 결함 신호*다.

---

## ⏪ 사전 조건

- [ ] 없음 — **최우선 단독 Phase**. feature 브랜치(`feature/m5-party-quest`) 위에서 시작.
- [ ] 현재 `ProtocolVersion.Current == 14` 확인 (M4.15에서 올라간 값).

---

## 📝 작업 내용

- [ ] `99_Tools/PacketGenerator/PDL.xml` — 아래 8패킷을 **append-only**로 추가(기존 패킷/ID 사이에 끼워넣기 금지, 끝에 붙임). 필드 타입·이름 정확히:
  - `C_PartyInvite { int targetEntityId }`
  - `C_PartyRespond { int inviterEntityId, byte accept }`
  - `C_PartyLeave { byte reserved }`
  - `S_PartyInviteRecv { int inviterEntityId, byte inviterClass }`
  - `S_PartyUpdate { int partyId, int leaderEntityId, int member0EntityId, int member1EntityId, byte member0Class, byte member1Class }`
  - `S_PartyError { byte reason }`
  - `S_QuestUpdate { int currentCount, int targetCount }`
  - `S_PortalLocked { int requiredCount, int currentCount }`
- [ ] PacketGenerator 실행 → `98_Shared/Protocol/Generated/GenPackets.cs` 재생성. 8개 패킷의 직렬화/역직렬화 코드 + stable PacketID 자동 부여 확인.
- [ ] `98_Shared/Protocol/ProtocolVersion.cs` — `Current = 15`로 변경 + **이력 주석 1줄** 추가(예: `// v15: M5 파티/퀘스트/포탈 8패킷`).
- [ ] `Shared.dll`(+`.pdb`) 빌드 후 `03_Client/Assets/Plugins/`로 복사(ADR-010 — Unity가 참조하는 산출물 갱신).

---

## ✅ 완료 조건

- [ ] 8개 패킷이 `GenPackets.cs`에 생성됨(C 3개 + S 5개).
- [ ] `ProtocolVersion.Current == 15` + 이력 주석 1줄 존재.
- [ ] `Dawnholder.slnx` 전체 빌드 통과(WSL2 `dotnet build` 0/0). 서버·봇·테스트 프로젝트 전부 새 패킷 참조 가능.
- [ ] `Shared.dll`이 `03_Client/Assets/Plugins/`에 복사됨.

---

## 🧪 테스트

**자동**:
- `Dawnholder.slnx` 빌드 통과 = 패킷 정의 정합성의 1차 게이트(타입 불일치/중복 ID는 빌드에서 잡힘).

**수동**:
- 생성된 `GenPackets.cs`에서 8패킷 PacketID가 기존 최대값 다음부터 연속 부여되었는지 육안 확인(은퇴 ID 재사용 0).

---

## 📚 학습 포인트

> 학부생 시각 — 처음 만나는 개념 위주.

- **프로토콜 버전 bump는 비가역이다** — 클라/서버가 동일하게 컴파일된 어셈블리를 참조하므로(ADR-010), v15로 올리면 v14로 빌드된 클라이언트(유현/인규)는 접속 불가가 된다. 그래서 헌법은 "패킷 모양 변경 시 `Protocol.Version` bump"를 요구하고, 이건 위험 깃발 `irreversible`로 분류된다.
- **append-only가 왜 신성한가** — 패킷 ID는 정의 순서로 부여된다. 중간에 끼워넣으면 *뒤따르는 모든 패킷의 ID가 밀려서* 같은 ID를 서로 다른 패킷으로 해석하는 동기화 재앙이 난다. 그래서 "은퇴한 ID 재사용 금지 + 끝에만 추가"가 규율이다.
- **PDL은 가변 길이 list를 지원하지 않는다** — 그래서 파티 멤버(정원 2)를 `List`가 아니라 `member0/member1` 2슬롯 고정 필드로 표현한다. 빈 슬롯은 `entityId=0`. trade-off: 고정 슬롯은 단순·빠르지만 정원이 늘면 패킷을 새로 정의해야 한다. 정원 2 고정이 확정이라 이 방식이 더 낫다.

---

## ⚠️ 함정 / 주의사항

- **append-only 위반 금지** — 기존 패킷 사이에 새 패킷을 끼우면 PacketID가 전부 밀린다. 반드시 PDL.xml *맨 끝*에 추가.
- **마일스톤 내 bump는 1회만** — A0에서 8개를 일괄 정의했으므로 이후 Phase에서 PDL을 다시 건드리면 안 된다. 추가 패킷이 필요하다고 느끼면 그건 설계가 틀어졌다는 신호 → 영호와 의논.
- **영호 사전승인 범위 = 브랜치 commit만** (2026-06-14). v15 bump를 feature 브랜치에 commit하는 건 야간 자율 OK(가역). 그러나 **push/PR/merge + 디스코드 wire-break 공지는 아침 영호 게이트** — Worker/AI 자율 진행 금지.
- **Shared.dll co-review** — `Shared.dll`을 commit에 포함하면 `03_Client` CODEOWNERS(정유현) 트리거 → admin bypass(영호 GO). dll 커밋 타이밍은 영호 확인.

---

## ➡️ 다음 Phase

- Phase 02 — PartyState + PartyRegistry actor 코어.

---

## 📋 박제 (완료 후)

- 보통 등급 → work-pin + commit message로 충분(-DONE.md 박지 않음). `ProtocolVersion` 이력 줄이 사실상의 박제.

---

## 작업 로그

- 2026-06-14: 생성.
