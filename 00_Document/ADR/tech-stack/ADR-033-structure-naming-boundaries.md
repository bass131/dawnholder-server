# ADR-033 — 구조 네이밍·경계 기준 (Structure Naming & Boundaries)

- **Status**: 🟢 **Accepted** (2026-06-20 영호 승인) — **P6 범위 = A(전체): D2·D3·D4·D5·D6 전부 집행.** 이 승인이 M7.7 P6(파일 이동) 게이트 통과.
- **Date**: 2026-06-20
- **Context milestone**: M7.7 구조 리팩토링 (behavior-invariant, pre-M8)
- **입력**: `01_Phases/youngho/M7.7-structure-refactor/_diagnosis.md` + Codex 검토 #4·#5 + `00_Document/FEATURE_MAP.md`
- **관련**: ADR-012(소켓 Y2 자매구현), ADR-026(엔티티 ID), `future-maps-namespace-restructure` memory

## Context

M7.7 진단에서 "이름이 거짓말하는" 곳이 다수 발견됐다 — 폴더명이 내용과 다르고, 폴더 ≠ 네임스페이스. 코드를 읽는 사람의 가장 비싼 비용은 "내가 잘못 이해했나?"라는 의심이다(Codex #5). 그러나 파일 *이동*은 frozen `-DONE`·CODEOWNERS·Unity serialized 참조를 깨뜨린다(memory `project-reorg`). 따라서 **대량 이동 전에 이름/경계 기준을 먼저 확정**한다(Codex #4). 본 ADR이 그 기준이며, P6이 이를 *집행*한다.

## Decision (제안 — 핵심 원칙 2개)

> **원칙 A. 폴더 = 개념 = 네임스페이스 일치.** 사람이 *이름을 보고 위치를 예측*할 수 있어야 한다.
> **원칙 B. 이동은 ADR 승인 + frozen 참조 grep 통과 후에만, 최소 범위로(P6).**

### 개별 경계 결정 (제안 — 각 항목 영호 승인 대상)

| # | 대상 | 현 상태(거짓말) | 제안 | 근거/트레이드오프 |
|---|---|---|---|---|
| D1 | `02_Server/Network/` | 깨끗(저수준 전송, 게임로직 0참조) | **유지 + 규칙 명문화**: low-level transport *only*, 게임 로직 참조 0 | 이미 정합. 규칙화로 미래 오염 방지 |
| D2 | `02_Server/GameServer/Network/GameSession.cs` | 폴더=Network, NS=`...Sessions` | **`Sessions/`로 이동** (폴더=NS 정합) | NS가 이미 Sessions라 NS 변경 0, 폴더만 이동. trust-boundary 파일이라 신중(P4와 P6 충돌 주의 — P4 후 이동) |
| D3 | `02_Server/GameServer/Network/MapMigration.cs` | 폴더=Network, *실제=존 이동 로직* | **`Maps/Transitions/`로 이동** + NS `...Maps.Transitions` | 네트워크 아님. FEATURE_MAP B3가 이미 "이동 대상" 표기. M8 영속화가 붙는 곳이라 위치 정합 중요 |
| D4 | `02_Server/GameServer/Maps/Systems/*` (6) | 폴더=Systems, NS=`...Maps` | **NS를 `...Maps.Systems`로 정합** (폴더 유지) | NS 텍스트만, 파일 이동 0 → 저위험. (대안: 폴더 평탄화 — 비추천, Systems 개념 유의미) |
| D5 | `02_Server/GameServer/Combat/` | "Combat"인데 전투 *로직* 없음(EnemyEntity·CombatConstants·Hitbox만) | **방향만 제안, 실행은 신중**: (a) `Entities/`로 의미 정렬 (b) 유지+CLAUDE.md 설명 | 엔티티 분산(D6)과 묶임. *과한 이동 경계* — 영호 결정 필요 |
| D6 | 엔티티 분산 (`PlayerEntity`=Maps/, `EnemyEntity`=Combat/) | 자매 엔티티 두 폴더 | **제안: `Entities/` 폴더로 통합** (NS `...Entities`) — *단 공통 베이스는 보류*(Codex: 이른 추상화 부채) | M8 저장 추상화 시 한곳이 유리. 그러나 이동 광범위(PlayerEntity in-coupling 23) → **P6 optional, 영호 판단** |
| D7 | 클라 핸들러 NS 평면 | 7서브폴더, NS 평면 `Dawnholder.Client.Network` | **NS=폴더 정합** (`...Network.Handlers.Combat` 등) — *파일 이동 0, NS 텍스트만* | P1에서 실행(이동 0이라 .meta 무관). 서버 핸들러와 대칭 |
| D8 | `98_Shared/GameData/` 평면 14파일 | 개념 혼재 | **하위폴더 `Enums/`·`Map/`·`Combat/` + 루트 코어** (NS 유지) | P3에서 실행. 소스 이동은 DLL 출력 불변(Unity 무영향) |
| D9 | `CharacterClass` NS | 도메인 enum인데 혼자 `Shared.Protocol` | **이동 보류** (24 사용처 sweep + append-only 영향) | Deferred. 사유 박제 |

### Consequences

- **P1** 집행: D7(클라 핸들러 NS), PacketGenerator NS. (이동 0)
- **P3** 집행: D8(98_Shared 그룹화). (소스 이동, DLL 불변)
- **P6** 집행: D2·D3·D4 (이동/NS 정합). **D5·D6은 영호 승인 시에만**(과한 이동, 미승인 시 Deferred). 이동마다 frozen 참조 grep 0 dangling 게이트(미통과 시 해당 이동 보류).
- **Deferred**: D9, 엔티티 공통 베이스, Maps→World/Simulation 대재편(memory `future-maps-namespace-restructure`, post-M8 SOLID 패스).

## 영호 결정 (2026-06-20)

- **D5/D6 범위**: **A(전체) 채택** — D5·D6을 이번 M7.7 P6에 *포함*한다 (보수안 B 미채택). 사유: M8(DB 영속화)이 엔티티에 붙기 *전에* `Entities/`로 통합해 두는 게 깨끗 — "이동 defer 거부, real reorg가 목표"(영호). D6(PlayerEntity 23참조)이 最高위험이라 **이동마다 frozen `-DONE`/CODEOWNERS grep 0 + WSL2 708 비감소 게이트, 미통과 시 해당 이동만 보류**.
- **승인 항목**: D1(규칙화)·D2·D3·D4·D5·D6 집행. D7·D8은 P1·P3에서 이미 완료. D9(CharacterClass NS)·엔티티 공통 베이스·Maps→World 대재편은 Deferred 유지.

> **이 ADR은 Accepted다 (영호 2026-06-20).** P6이 D2·D3·D4·D5·D6을 집행한다 — 이동마다 frozen 참조 grep 0 dangling 게이트 통과 시에만.
