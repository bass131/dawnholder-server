---
name: knowledge-cross-cutting-index
description: 도메인 횡단 (보안/툴 함정/마이그/환경 사고) 학습 캐시 인덱스
domain: cross-cutting
maintainer: youngho
last_updated: 2026-05-20
---

# Cross-Cutting Knowledge — _index.md

> **누가 통독**: **전 SubAgent 통독** (server / shared / client / qa / unity-bridge — 자기 도메인 _index와 *함께* 통독)
> **R only 통독**: coordinator / reviewer / plan-auditor (필요 시)
> **박는 시점**: `-DONE.md` 박제 직후 / CHANGELOG [M]/[H] 직후 / 사용자 명시 요청. **AI 자율 박제 금지**.
> **양식·박는 방법**: [`../_usage.md`](../_usage.md) 4번 섹션

---

## 활성 항목 (최근 3개월)

| 키워드 | 한 줄 요약 | 트리거 | 검증 |
|---|---|---|---|
| _(Phase 04 (2/3)에서 시드 박힘 — 예정: `sac-dotnet-test-block`, `projectsettings-cloud-ping-pong`, `gamma-pre-validation-pattern`, `riot-vanguard-spawn-unknown`)_ | — | — | — |

---

## 디테일 본문

_Phase 04 (2/3)에서 시드 항목별 ~30~50줄 박힘._

### 예정 시드 (cross-cutting은 시드 가장 풍부 — 한국 PC 환경 함정 + 마이그 패턴 + γ 패턴)

- `sac-dotnet-test-block` — Smart App Control On 환경 unsigned dotnet test dll 0x800711C7 차단 (★★★)
- `projectsettings-cloud-ping-pong` — Unity Cloud Services 다인 함정, pre-commit hook으로 cloud 라인 자동 unstage (★★★)
- `gamma-pre-validation-pattern` — Phase 정의 박기 전 외부 검토 (Codex γ 6/7회차 누적, plan-auditor SubAgent 내재화) (★★★)
- `riot-vanguard-spawn-unknown` — 사용자 PC 상주, Node child_process.spawn 차단 (★★)

---

## 비활성 / GC 대기 (3개월+ 무참조)

_(없음 — 본 캐시는 2026-05-20 신설)_

---

## 도메인 경계

이 캐시는 *도메인 횡단 패턴*을 담습니다:

- **포함**:
  - 환경 사고 (한국 PC 함정: SAC / Vanguard / WDAC)
  - 툴 함정 (PacketGenerator noManager / Unity MCP 빈 응답 / Unity 버전 hash)
  - 마이그 패턴 (격리 폴더 → 일괄 mv 전환 / dual-write Phase)
  - 검증 패턴 (γ 사전 검증 / Rule of Three / 옵션 B 응급 모드)
  - 헌법 위반 패턴 (false-promise는 *코드/문서 정합*이라 `shared/`로, 횡단 본질이면 본 캐시)
- **제외**: 단일 도메인 패턴 — 각 도메인 _index로 분리

**판단 기준**: "이 패턴이 *어느 SubAgent에 영향*인가" — 1개면 해당 도메인, 2개 이상이면 cross-cutting.

---

## 관련 자산

- CHANGELOG: [`../../../../.claude/CHANGELOG.md`](../../../../.claude/CHANGELOG.md) — [H]/[M] 박힘 = 본 캐시 후보
- 옛 memory: `~/.claude/projects/C--Dev-ClaudeDev/memory/` — 흡수 후보 (Phase 04 (2/3))
- 정책: [`../../policies/knowledge-system.md`](../../policies/knowledge-system.md)

---

## 갱신 이력

- 2026-05-20 — M3.5 Phase 04 (1/3) 골격 박힘. 시드 항목은 (2/3)에서 채움 — cross-cutting은 시드 가장 풍부 (5건 이상 예상).
