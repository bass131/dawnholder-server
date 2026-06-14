---
owner: youngho
milestone: M4.15
phase: 06
title: Protocol v14 — C_SkillUse verticalDir 필드 추가
status: pending
grade: 복잡
domain: shared
summary: C_SkillUse에 verticalDir 바이트 append + PacketGenerator 재생성 + ProtocolVersion v13→v14 bump + Shared.dll 재배포 (4방향 텔레포트 와이어 기반)
---

# Phase 06: Protocol v14 — C_SkillUse verticalDir 필드 추가

> **상태**: pending
> **마일스톤**: M4.15 (워크스트림 D — 텔레포트, 영호 Play-test 중 추가)
> **등급**: 복잡 (irreversible 깃발 = `Protocol.Version` bump — 영호 Option B GO 완료)
> **담당**: shared (Sonnet Worker — 메인 file:line 게이트)

---

## 🎯 목표

위/아래 텔레포트 방향(수직 의도)을 클라→서버로 전달할 **전용 와이어 채널**을 만든다. 영호가 Option B(전용 필드 + v14 bump)를 선택 — `C_SkillUse`에 `verticalDir` 바이트를 **append-only**로 추가하고, PacketGenerator로 재생성, `Protocol.Version`을 v13→v14로 bump, Shared.dll을 양쪽(서버/클라)에 재배포한다. **이 Phase는 텔레포트 워크스트림의 토대** — P07(서버)·P08(클라)가 모두 이 새 필드에 의존.

---

## ⏪ 사전 조건

- [x] 영호 Option B GO (2026-06-14 AskUserQuestion — facing 바이트 재활용(A) 대신 전용 필드(B) 선택, v14 bump 감수).
- [ ] Phase 01~05 완료 (브랜치 동일, wire는 05까지 v13 유지 — 본 Phase가 첫 wire 변경).

---

## 📝 작업 내용

- [ ] `99_Tools/PacketGenerator/PDL.xml` — `C_SkillUse` 패킷에 `<byte name="verticalDir"/>` **append** (반드시 기존 `facing` *뒤*, append-only — 헌법 §2 "은퇴 ID 재사용 금지" + 필드 순서 stable).
  ```xml
  <packet name="C_SkillUse">
      <byte name="skillId"/>
      <int name="attackerClientTick"/>
      <byte name="facing"/>
      <byte name="verticalDir"/>   <!-- 신규: 0=없음(수평), 1=위, 2=아래 -->
  </packet>
  ```
- [ ] PacketGenerator 재실행 → `98_Shared/Protocol/Generated/GenPackets.cs` 재생성. `C_SkillUse`의 Read/Write에 `verticalDir` 직렬화 추가 확인 (수기 편집 X — 생성기 출력).
- [ ] `98_Shared/Protocol/ProtocolVersion.cs:66` — `Current = 13` → `Current = 14`. (헌법 §2: 패킷 모양 변경 = bump 의무.)
- [ ] `98_Shared/CLAUDE.md` — `Current` 언급을 14로 갱신. **별건 stale 동시 정정**: 현재 문서가 "Current=12"로 박혀 실제(13)와 어긋나 있음(M4.15 plan-auditor false-positive의 근원). 이번에 14로 바로잡아 drift 해소.
- [ ] Shared.dll 재빌드 + `03_Client/Assets/Plugins/`로 복사 (ADR-010 — EmbedAllSources PDB 포함). **이 dll commit이 03_Client CODEOWNERS(정유현) co-review 트리거** — 마감(P09) PR에서 admin bypass(영호 GO) 경로 정함.
- [ ] 양쪽 컴파일 확인: WSL2 `dotnet build` 0/0 (서버 + 04_ClientNet) + Unity 컴파일 0err(메인 MCP, 클라가 새 필드 인식).

---

## ✅ 완료 조건

- [ ] `GenPackets.cs`의 `C_SkillUse`에 `public byte verticalDir;` + Read/Write 직렬화 존재 (생성기 출력, 수기 X).
- [ ] `ProtocolVersion.Current == 14` (서버/클라 핸드셰이크 정합 — 같은 dll 참조).
- [ ] 기존 필드(`skillId`/`attackerClientTick`/`facing`) 순서·타입 불변 (append-only 증명).
- [ ] WSL2 `dotnet build` 0/0 + Unity 컴파일 0err.
- [ ] `98_Shared/CLAUDE.md` Current=14 정합 (stale 12 → 14 정정 포함).

---

## 🧪 테스트

**자동**: `PacketRoundTrip` 테스트가 있으면 `C_SkillUse` round-trip에 verticalDir 포함 확인 (Write→Read 동일성). 없으면 기존 핸드셰이크/스킬 테스트 green 유지로 회귀 0 증명.
**수동**: 없음 (와이어 토대 — 거동은 P07/P08에서).

---

## 📚 학습 포인트

- **append-only 프로토콜 진화** — 기존 필드 뒤에만 추가하면 직렬화 오프셋이 stable. 중간 삽입/삭제는 모든 후속 필드 오프셋을 깨 breaking. PDL이 정의 순서대로 코드 생성하므로 *순서가 곧 와이어 레이아웃*.
- **Protocol.Version의 의미** — 핸드셰이크에서 클라/서버 버전 불일치를 거부. 같은 Shared.dll을 양쪽이 참조(ADR-010)하므로 *함께 재빌드*되어 실전 스큐는 없지만, bump은 "와이어 모양이 바뀌었다"는 규율적 박제 (헌법 §2).
- **생성 코드는 손대지 않는다** — `GenPackets.cs`는 PDL의 산출물. 수기 편집하면 다음 재생성에 증발. 진실은 PDL.xml 한 곳.

---

## ⚠️ 함정 / 주의사항

- **append 위치** — `facing` *뒤*에. 앞/중간에 넣으면 기존 필드 오프셋이 밀려 v13 직렬화와 호환 깨짐(이미 v14라 신경 안 써도 되지만, 규율상 append-only).
- **Shared.dll 비결정 재복사** (carry-over) — 서버 Worker가 Windows 빌드 시 dll이 재복사될 수 있음. 본 Phase는 *의도적으로* dll을 바꾸므로 정상. 단, 소스 무변경인데 dll만 바뀐 다른 파일이 섞이지 않게 diff 확인.
- **co-review 트리거** (memory: shared-dll-triggers-client-co-review) — Shared.dll을 commit에 *포함*해야 클라가 새 필드 사용 가능 → 03_Client CODEOWNERS 발동. 이건 의도된 것 (P09 PR에서 처리).
- **regenerate 도구 실행 경로** — PacketGenerator 실행 방식(WSL2 vs Windows) 확인. SAC 차단 가능성(memory) → ADR-029 WSL2 경로 우선.

---

## ➡️ 다음 Phase

- Phase 07 — 서버 4방향 텔레포트 + 거리 + 지형 (이 필드 소비).
- Phase 08 — 클라 4방향 입력 + 이펙트 버그 (이 필드 송신).

---

## 📋 박제 (완료 후)

- 복잡(irreversible) → work-pin + commit message. 마일스톤 `-DONE.md`(P09)에 워크스트림 D 토대로 흡수.

---

## 작업 로그

- 2026-06-14: 생성 (영호 Option B GO 후 — v14 전용 필드 경로).
