# Harness Review Follow-up 1/5 — MessagePack 잔재 정정

- 기준 리뷰: `Dawnholder-harness-review-2026-05-19.md`
- 작업 일시: 2026-05-19
- 범위: markdown 문서 정정만
- 기준 결정: `00_Document/ADR/tech-stack/ADR-002-tcp-pdl.md`

## 결론

헌법/체크리스트/구조 문서에 남아 있던 MessagePack 기반 규칙 표현을 자체 PDL XML + C# 코드 생성기 기준으로 정정했다.

정정 후 본 follow-up 문서를 제외하고 `CLAUDE.md`와 `00_Document/`에서 `messagepack|[MessagePackObject]|[Key(`를 검색하면 ADR-002 본문만 남는다. ADR-002는 과거 대안과 trade-off를 설명하는 기준 문서이므로 의도적으로 유지했다.

## 정정 목록

| 파일 | 옛 문구 | 새 문구 |
|---|---|---|
| `CLAUDE.md` | `자체 PDL ... (MessagePack 아님)` | `자체 PDL(Packet Definition Language) XML + C# 코드 생성기 — [ADR-002]` |
| `CLAUDE.md` | `패킷 struct는 [MessagePackObject] + 명시적 [Key(N)] 인덱스` | `PDL.xml append-only 정의 + PacketGenerator가 stable PacketID/필드 직렬화 코드 생성` |
| `00_Document/REVIEW_CHECKLIST.md` | `패킷 struct에 [MessagePackObject] 또는 명시적 [Key(N)] 누락` | `PDL.xml이 아닌 수동 패킷 struct 작성 또는 PacketID/필드 순서 임의 지정` |
| `00_Document/REVIEW_CHECKLIST.md` | `MessagePack / protobuf / System.Text.Json 등 대체 직렬화 사용` | `자체 PDL XML + 코드 생성기 외 대체 직렬화 사용` |
| `00_Document/ARCHITECTURE.md` | `MessagePack은 ADR-002 v1에서 채택했으나...` | `ADR-002 v2 기준 자체 PDL XML + C# 코드 생성기가 단일 직렬화 경로` |
| `00_Document/ARCHITECTURE.md` | `MessagePack 의존성 제거` | `외부 직렬화 의존성 제거` |
| `00_Document/ADR_History.md` | `MessagePack → 자체 PDL + 코드 생성기` | `외부 직렬화안 → 자체 PDL + 코드 생성기` |
| `00_Document/reviews/2026-05-18-pre-m3-claude-review.md` | `[MessagePackObject] 사용 X(자체 PDL)` | `자체 PDL XML + 코드 생성기 사용` |

## 검증

검색:

```text
rg -n -i "messagepack|\[MessagePackObject\]|\[Key\(" -g "*.md" -g "!00_Document/reviews/2026-05-19-harness-review-followup-1of5.md" CLAUDE.md 00_Document
```

결과: `00_Document/ADR/tech-stack/ADR-002-tcp-pdl.md` 4건만 남음. ADR-002 본문은 정확한 기준 문서라 수정하지 않았다.

빌드:

```text
dotnet build Dawnholder.slnx --nologo
```

문서 변경만이라 코드 의미 변경은 없다. 빌드 결과는 작업 응답에 별도 기록한다.

## Diff 요약

정정 대상:

- `CLAUDE.md`
- `00_Document/ARCHITECTURE.md`
- `00_Document/ADR_History.md`
- `00_Document/REVIEW_CHECKLIST.md`
- `00_Document/reviews/2026-05-18-pre-m3-claude-review.md`

동시 작업 보존:

- `01_Phases/youngho/M3-first-multiplayer/06-server-combat-emergency.md` 및 서버/Shared 변경은 Claude Phase 06 진행분으로 보고 본 작업에서 수정하지 않았다.
- `99_Tools/headless-bot/*` 변경은 직전 Codex 병렬 작업분이다.
