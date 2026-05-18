# Phase 02: ProtocolVersion 핸드셰이크

> **상태**: done (2026-05-18, 박제 → `02-protocol-version-handshake-DONE.md`)
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **예상 소요**: 1.5h
> **담당 에이전트**: netcode

---

## 🎯 목표

클라/서버 ProtocolVersion mismatch 시 *즉시* disconnect. **헌법 #2 (Protocol is Sacred) 가짜 약속 1번째 봉합** — `98_Shared/CLAUDE.md`에 "핸드셰이크 코드 미구현" 박혀있던 상태를 코드로 봉합.

## ⏪ 사전 조건

- [ ] Phase 01 완료 (PacketGenerator 기본값 안전, Shared CLAUDE 정정)

---

## 📝 작업 내용

- [ ] `98_Shared/Protocol/` 또는 GameData에 `Protocol.Version` 상수 정의 (`const ushort Version = 1`, breaking change 시 bump)
- [ ] PDL XML — `C2S_Handshake { ushort clientVersion }` + `S2C_HandshakeResult { bool ok, ushort serverVersion, string reason }` 신설 (또는 기존 첫 패킷에 version 필드 추가)
- [ ] PacketGenerator 재생성 (`--no-manager`) + Shared.dll 빌드 + commit
- [ ] 서버 핸들러 — 첫 패킷이 handshake 아니면 거절, 일치 시 진입 / mismatch 시 reason 박고 즉시 Disconnect
- [ ] 클라 첫 패킷 = handshake 전송, mismatch 응답 시 사용자 안내 + 종료
- [ ] handler 단위 테스트: happy / mismatch / 비-handshake 첫 패킷

## ✅ 완료 조건

- [ ] 버전 일치 시 정상 진입
- [ ] 버전 mismatch 시 서버가 *즉시* disconnect (timeout 기다리지 않음 — 헌법 #3 정합)
- [ ] 핸들러 단위 테스트 3건 모두 통과
- [ ] PDL 변경 의무 3종 다 박힘 (regen / build / commit)

---

## 🧪 테스트

**자동**: `HandshakeHandlerTests` — happy, mismatch, non-handshake 첫 패킷 3건
**수동**: 클라 빌드 1 (version=1) + 빌드 2 (version=2) 만들어 서로 접속, mismatch 측 즉시 종료 확인

---

## 📚 학습 포인트

- **Application-layer handshake** — TCP 위에 application 레벨 핸드셰이크가 왜 필요한가 (TCP는 byte stream, version 보장 X)
- **헌법 #2 봉합** — "코드 미구현 + 주석으로 박힌 약속"이 가짜 약속의 첫 번째 케이스 (M2.5 Phase 09에서 본인이 학습한 패턴)
- **첫 패킷 강제 패턴** — handshake가 첫 패킷이어야 *isolation* 보장 (다른 패킷 받기 전 검증)

---

## ⚠️ 함정 / 주의사항

- PDL 변경 후 PacketGenerator 재생성 누락 → 양쪽 Shared.dll mismatch (CHANGELOG 2026-05-17 박힌 룰)
- mismatch 시 disconnect 안 하고 *timeout 기다림* → 헌법 #3 위반 (rate-limit 무효화 가능)
- version 비교 `==`만 하면 호환 가능 minor version도 거절 — 응급은 `==` OK, 본 마감 시 호환표
- handshake 패킷 자체에 size 한도 (Phase 09 length 검증 적용 — 4~4096)

---

## ➡️ 다음 Phase

Phase 03 — 핸들러 layer 분리 + 02_Server/CLAUDE.md 정합

---

## 작업 로그

- 2026-05-18: pending (헌법 #2 가짜 약속 1번째 봉합 = work-pin 박힌 약속)
- 2026-05-18: **완료** — PacketGenerator bool/string 결함 동반 fix(blocker 봉합) + PDL C_Handshake/S_HandshakeResult 신설 + GameSession first-packet 강제 + 04_ClientNet/Unity wrapper/헤드레스 봇 handshake 전송 + HandshakeHandlerTests 3건 신설. dotnet test 135/0/1 green. 박제 → `02-protocol-version-handshake-DONE.md`.
