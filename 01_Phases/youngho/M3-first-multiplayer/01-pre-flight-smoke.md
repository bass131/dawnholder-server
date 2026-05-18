# Phase 01: Pre-flight Smoke Check

> **상태**: pending
> **마일스톤**: M3 — Multiplayer & Demo Stage
> **예상 소요**: 1h
> **담당 에이전트**: client + netcode

---

## 🎯 목표

응급 데모 본 작업 진입 전 함정 3개 사전 봉합. (1) 유현 Asset 실제 임포트 가능 확인, (2) PacketGenerator 기본값 안전 보장(`--no-manager` 또는 기본값 fix), (3) `98_Shared/CLAUDE.md:19` 옛 문구 정정.

## ⏪ 사전 조건

- [ ] M2.5 Hardening 마감 완료 (main `847325c`)
- [ ] 유현 Asset 1개 이상 받음 (sample import용)
- [ ] `feature/youngho-m3-plan` 브랜치

---

## 📝 작업 내용

- [ ] 유현 Asset 1개 sample import → Unity Editor에서 SpriteRenderer 또는 Image로 표시 확인
- [ ] Asset 포맷 mismatch 시 fallback 결정 박기 (placeholder 박스 + zone 색깔)
- [ ] `99_Tools/PacketGenerator/` 기본값 검토 — `--no-manager` 안 쓸 때 manager 파일이 컴파일 깨지는지 dry-run으로 확인 후 기본값 반전 또는 매번 `--no-manager` 명시 규칙 박기
- [ ] `98_Shared/CLAUDE.md:19` 라인 "M2.5 Phase 09 처리 예정" → "M3 Phase 02 처리 예정" 정정
- [ ] `dotnet test Dawnholder.slnx --nologo` baseline 확인 (Codex 검증 = 132/0/1 skip)

## ✅ 완료 조건

- [ ] Unity Editor에서 유현 sample asset 표시되거나 fallback 결정 박힘
- [ ] PacketGenerator dry-run 컴파일 깨짐 X
- [ ] `98_Shared/CLAUDE.md:19` 정정 commit
- [ ] `dotnet test` 통과 (baseline 유지)

---

## 🧪 테스트

**자동**: `dotnet test Dawnholder.slnx --nologo` green (M2.5 baseline 유지)
**수동**: Unity Editor에서 sample asset 표시 확인 / PacketGenerator 1회 실행 후 빌드 확인

---

## 📚 학습 포인트

- **Pre-flight smoke check 패턴** — 본 작업 진입 전 빠른 환경 검증. 응급 모드일수록 가치 ↑
- **도구 기본값 함정** — `--no-manager` 같은 옵션이 *위험한 기본값*을 가리고 있는 패턴 (Phase 11 가짜 약속 봉합과 같은 클래스)
- **옛 문서 stale risk** — `98_Shared/CLAUDE.md:19`처럼 옛 결정 문구가 Codex/Claude 검토 도구 혼동시키는 비용 (γ 방식 3회차에서 Codex가 직접 짚음)

---

## ⚠️ 함정 / 주의사항

- Asset 포맷 mismatch (Sprite 2D vs Texture2D vs UI Image) — Unity Inspector에서 Texture Type 확인
- PacketGenerator manager 생성 활성화된 채 PDL 변경 시 컴파일 깨짐 (Phase 02부터 매번 발생 위험)
- Shared CLAUDE 정정 누락 시 다음 Codex/Claude 검토 시 *옛 박힌 약속*과 *현재 상태* 충돌로 혼동

---

## ➡️ 다음 Phase

Phase 02 — ProtocolVersion 핸드셰이크 (netcode 인프라 시작)

---

## 작업 로그

- 2026-05-18: pending (γ 방식 3회차 Codex β 권장 5번/6번 반영)
