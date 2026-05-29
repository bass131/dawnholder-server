---
owner: youngho
milestone: M4.3R
phase: 07
title: 네트워크 레이어 m_ prefix 일괄 (서버 Network/ + ClientNet 자매)
status: done
grade: 보통
domain: cross
estimated: 1~2h
---

# Phase 07: 네트워크 레이어 m_ prefix 일괄 (rank 7)

> **상태**: done (2026-05-29 — m_ 필드 → _camelCase + 파라미터 → camelCase 정규화, build 0/0 test 322/0/4)
> **마일스톤**: M4.3R
> **등급**: 보통 (기계적 rename, cross 도메인 자매 동시변경)
> **담당**: server SubAgent + client SubAgent (ADR-012 자매 — 동시 변경, Coordinator 조율)

---

## 🎯 목표

ServerCore/네트워크 레이어(Rookiss 강의 유산)의 `m_` 헝가리안 prefix를 `_camelCase`로 통일한다(§3.3 — Phase 01에서 "서버 적용" 명문화 완료 전제). `02_Server/Network/`와 `04_ClientNet/`는 **ADR-012 자매 구현**이라 한쪽만 바꾸면 안 됨 — **동시 변경**. 기계적 rename, 동작 완전 보존.

---

## ⏪ 사전 조건

- [ ] **Phase 01 완료 필수** — CODE_CONVENTION §3.3 "서버 적용" 명문화 (현재 §3.3은 클라 섹션이라 해석 갭)

---

## 📝 작업 내용

- [ ] `02_Server/Network/` — `Session.cs`/`Listener.cs`의 `m_` 필드 → `_camelCase` (m_Socket→_socket, m_disconnected→_disconnected, m_recvBuffer→_recvBuffer, m_lock→_lock, m_SendQueue→_sendQueue, m_PendingList→_pendingList, m_SendArgs→_sendArgs, m_RecvArgs→_recvArgs, m_listenSocket→_listenSocket, m_SessionFactory→_sessionFactory)
- [ ] `04_ClientNet/` — `ClientSession.cs`/`SendBuffer.cs`/`RecvBuffer.cs`/`Connector.cs`의 동형 `m_` 필드 → `_camelCase` (서버 자매와 동시)
- [ ] `99_Tools` 헤드리스 봇이 `Connector` 재사용 → 빌드 영향 확인
- [ ] `dotnet build Dawnholder.slnx` 후 Shared.dll/ClientNet.dll → Plugins 복사 확인 (DLL stale 함정)

### rank 9 포함 — 매개변수 `_` prefix 제거 (§3.3 재분류, 2026-05-29 사용자 결정)
- [ ] `Listener.cs` 매개변수 `_endPoint`/`_sessionFactory`/`_register`/`_args` → `camelCase`(밑줄 제거). params에 `_`는 §3.3 prefix 위반(밑줄=field 전용)
- [ ] grep로 네트워크 레이어 내 다른 `_`-prefix 매개변수/지역변수 잔여 확인 (있으면 동반 제거)
- [ ] casing 변환(Pascal↔camel)·중괄호 스타일은 §4 `.editorconfig`(M4.4) 유지 — 본 Phase는 *밑줄 prefix 제거*만 (casing은 이미 정상)

---

## ✅ 완료 조건

- [ ] `m_` prefix 0건 (grep `m_` in 02_Server/Network/ + 04_ClientNet/ = 0)
- [ ] 매개변수 `_` prefix 0건 (Listener.cs `_endPoint` 류 → `camelCase`)
- [ ] `dotnet build Dawnholder.slnx` green + `dotnet test` 회귀 0
- [ ] 헤드리스 봇 빌드 green (Connector 재사용 영향 0)
- [ ] Shared.dll/ClientNet.dll Plugins 복사 (DLL stale 방지)
- [ ] 서버 Network/ ↔ ClientNet/ 자매 동시 변경 확인 (한쪽만 변경 아님 — ADR-012)
- [ ] reviewer §3.3 + ADR-012 정합 점검

---

## 🧪 테스트

**자동**: `dotnet test` 회귀 0 (rename은 동작 불변 — 컴파일만 통과하면 됨).
**수동**: 봇 1대 접속 → 핸드셰이크/이동/snapshot 정상 (네트워크 레이어 rename이 직렬화/소켓에 영향 0 확인).

---

## 📚 학습 포인트

- **자매 구현 동시 변경(ADR-012)**: 같은 socket 코드를 서버(`02_Server/Network/`)와 클라(`04_ClientNet/`)가 거의 동형으로 가짐. 한쪽만 바꾸면 두 구현이 갈라져 유지보수 부담 — 동시 변경이 규율.
- **`m_` 헝가리안 vs `_camelCase`**: `m_`는 옛 C++ 관습(member). 현대 C#는 `_camelCase`가 표준(Microsoft). 한 코드베이스 두 표기 = 인지 부담.
- **기계적 rename은 도구가 더 안전**: 본 Phase는 §3.3 필드 prefix(우리 규칙)라 수동 처리하지만, §4 포매팅(매개변수/중괄호)은 .editorconfig가 일괄 — "사람이 판단할 게 아니라 도구가 강제"(§4).

---

## ⚠️ 함정 / 주의사항

- **자매 동시 변경 필수(ADR-012)** — 서버 Network/만 또는 ClientNet/만 바꾸면 안 됨. 같은 구조라 동시에.
- **DLL stale 함정**(work-pin 습관 a): ClientNet 수정 후 빌드 1회 + Plugins 복사. 안 하면 Unity가 옛 DLL 참조 → 소리 없는 어긋남.
- **rank 9 매개변수 prefix 손대지 말기** — §4 도구 영역. 여기서 수동으로 하면 .editorconfig 도입 시 충돌/중복.
- **순수 rename — 시그니처/동작 불변**: 필드 이름만. public 메서드/직렬화/소켓 동작 0 변경.

---

## ➡️ 다음 Phase

- M4.3R 마일스톤 마감 — 회귀 종합 + 5단계 보고 (MD/HTML) + CHANGELOG [M]

---

## 📋 박제 (완료 후)

- **보통 등급** — work-pin + commit. 단 마일스톤 마지막 Phase라 마감 시 5단계 보고는 마일스톤 레벨에서.

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan`)
