# Phase 03: 핸들러 Layer 분리 + 02_Server/CLAUDE.md 정합

> **상태**: pending
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **예상 소요**: 2h
> **담당 에이전트**: netcode

---

## 🎯 목표

핸들러 dispatch를 layer 모델로 봉합 + `02_Server/CLAUDE.md` Layout 표 *동시* 갱신. **헌법 #4 (Shared Code Discipline) 가짜 약속 2번째 봉합** — 문서와 코드 동시 진행 정신. 기존 모든 핸들러에 invalid + auth 테스트 페어 채움.

## ⏪ 사전 조건

- [ ] Phase 02 완료 (Handshake 핸들러 박힘)

---

## 📝 작업 내용

- [ ] 현재 dispatch 형태 점검 (if-else? switch? Dictionary?) — 어디가 분산돼있는지
- [ ] `02_Server/GameServer/Handlers/` 폴더 정합 — C2S 패킷별 핸들러 파일 분리 (`MoveHandler.cs`, `HandshakeHandler.cs`, ...)
- [ ] dispatch 테이블 = `Dictionary<PacketId, IHandler>` (if-else 체인 제거) → 새 핸들러 = 한 줄 등록
- [ ] `02_Server/CLAUDE.md` Layout 섹션 실제 구조와 정합 (Handlers/ 폴더 안 구조 명시)
- [ ] 기존 핸들러(C2S_Move, C2S_Handshake, ...) 각각 invalid input 테스트 1 + auth 테스트 1 페어 (없으면 신설)
- [ ] `dotnet test` green

## ✅ 완료 조건

- [ ] `Handlers/` 폴더 깔끔 분리, 새 핸들러 추가 = 한 줄 등록
- [ ] `02_Server/CLAUDE.md` Layout 표가 실제 구조와 1:1 (*코드 변경과 동시 commit* — 가짜 약속 봉합 패턴)
- [ ] 모든 기존 핸들러에 invalid/auth 테스트 페어 존재
- [ ] `dotnet test` green

---

## 🧪 테스트

**자동**: 각 핸들러별 happy + invalid + auth = 3건씩
**수동**: 서버 켜서 정상 패킷 1회 + invalid 패킷 1회 (drop 또는 disconnect 확인)

---

## 📚 학습 포인트

- **Dispatch 패턴** — if-else / switch / Dictionary 비교, Dictionary가 *확장성 + 테스트 용이*
- **헌법 #4 가짜 약속 봉합 패턴** — `02_Server/CLAUDE.md` Layout이 실제 구조와 mismatch면 약속만 있고 진짜 없음. 코드 변경 + 문서 갱신 *동시*가 정답
- **invalid + auth 페어 = 신뢰 경계 minimum** — 헌법 #3 정합 (`02_Server/CLAUDE.md` "새 packet handler 추가 시" 4번 조항)

---

## ⚠️ 함정 / 주의사항

- dispatch가 if-else 체인이면 새 핸들러 추가 시 누락 위험 → Dictionary 강제
- `02_Server/CLAUDE.md` 안 고치면 #4 봉합 X (코드만 바꾸고 문서 안 바꾸면 *반쪽 봉합*)
- invalid 테스트 = 단순 "잘못된 데이터 보내고 reject 확인", auth 테스트 = "권한 없는 sender가 보내고 reject 확인"
- 핸들러 단위 테스트는 *세션 mock* 필요 (full integration X)

---

## ➡️ 다음 Phase

Phase 04 — 서버 Broadcast 인프라

---

## 작업 로그

- 2026-05-18: pending (헌법 #4 가짜 약속 2번째 봉합 = work-pin 박힌 약속)
