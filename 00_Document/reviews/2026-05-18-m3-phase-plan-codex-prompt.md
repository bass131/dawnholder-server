# M3 Phase 분해 — Codex β xhigh 검토 요청 (γ 방식 3회차 / Rule of Three)

> 본인이 이 파일을 Codex CLI에 던지세요. 결과(별도 `*-codex-review.md`)는 같은 폴더에 박은 후 Claude에게 알려주면 분해 표 조정합니다.

---

## 배경

- 본인 = 학부생 백엔드 학습 + Dawnholder Project (2D 사이드스크롤 MMORPG, **6/10 캡스톤 1** + **11/19 본 마감**) 팀장
- **M2 First Connection** 완료 — 1인 권위 movement + prediction/reconciliation + jump
- **M2.5 Hardening** 마감 완료 — Phase 09 trust-boundary fail-closed / 10 session lifecycle race / 11 cleanup. main `847325c`. PR #27/#28/#29/#30
- **M3 First Multiplayer & Demo Stage** 마일스톤 진입. 본 PR이 그 분해.

## 시간 제약 — 응급 모드

오늘 **2026-05-18** → **2026-05-20 13:40 교수 중간 면담**. 약 **48h** (잠/식사/유현 동기/발표자료 별도).

면담 = M3 풀세트 **응급 데모** (시연 위주, 발표자료 병렬 다른 세션 작성).

## 데모 범위 (β+ 묶음 — 사용자 결정 박힘)

- 두 명 같은 맵 broadcast (서로 보임 + 부드러운 보간)
- 단순 전투 시연 (적/보스 placeholder, 단일 맵 3-zone)
- **단일 맵 3-zone 트릭** (마을/전투/보스) — *진짜 4맵 분리는 M4로 분리*
- Stage Clear UI
- 유현 Asset(캐릭터/적/보스 스프라이트) 통합

## 헌법 제약 (NON-NEGOTIABLE — `CLAUDE.md` 참조)

- **#1 Server Authority** — 클라 입력은 서버 검증 후 적용. 응급 *단순화 OK, 위반 X*
- **#2 Protocol is Sacred** — PacketId stable, 은퇴 ID 재사용 X. PDL 변경 시 PacketGenerator 재생성 + Shared.dll commit + 양쪽 정합 (`02_Server` + `03_Client`)
- **#3 Trust Boundary** — 클라 input untrusted. 범위/소유권/rate-limit 검증
- **#4 Shared Code Discipline** — `98_Shared/` 변경 시 양쪽 빌드 확인. ProtocolVersion bump
- **#5 No Blocking in Tick Loop** — 50ms tick(20 TPS), await/Thread.Sleep 금지

## 관련 ADR

- **ADR-016** 분업: Codex 본문 / Claude mcp 박기 / 본인 회고
- **ADR-018** current-pin.txt 입구 안전망
- **ADR-019** reviewer 서브에이전트 (Tier 2 자동 리뷰)
- **ADR-013** -DONE.md = AI(사실) / learning-journal = 본인(회고)

## 현 분해 안 (v1)

> 응급 모드라 Phase 8개로 분해. 폴더 = `01_Phases/youngho/M3-first-multiplayer/`. Prefix는 마일스톤 내 리셋(01부터).

| # | Phase 제목 | 영역 | 예상 | 끝나면 데모할 수 있는 것 |
|---|----------|------|------|------------------------|
| 01 | ProtocolVersion 핸드셰이크 | netcode | 1.5h | mismatched 클라 즉시 거절 / 일치 시만 진입 (헌법 #2 가짜 약속 1번째 봉합) |
| 02 | 핸들러 layer 분리 + `02_Server/CLAUDE.md` Layout 정합 | netcode | 2h | `Handlers/{C2S_*}` 분리 + dispatch 테이블 + handler 단위 invalid/auth 테스트 (헌법 #4 가짜 약속 2번째 봉합) |
| 03 | Broadcast 인프라 (PlayerJoin/Leave + multi-target Snapshot) | gameplay/netcode | 2.5h | 두 봇 접속 시 서로 PlayerJoin 받음 / disconnect 시 PlayerLeave / Snapshot 같은 맵 전원 |
| 04 | 두 명 movement 동기 (본인 reconcile + 타인 순수 보간) | client/gameplay | 2.5h | 두 명이 같이 부드럽게 움직임 |
| 05 | 응급 전투: 적 placeholder + 데미지 흐름 (단순화) | gameplay | 2h | 적 1마리 spawn / 클라 공격 패킷 / 서버 반경 hit 판정 / HP 감소 / 사망 broadcast (헌법 #1 #3 단순화 OK 위반 X) |
| 06 | 보스 placeholder + Stage Clear 트리거 | gameplay | 1.5h | 우측 zone 보스 / HP 큼 / 사망 시 StageClear 패킷 broadcast → 클라 UI 표시 |
| 07 | 유현 Asset 통합 + 단일 맵 3-zone 시각화 | client | 2.5h | 캐릭터/적/보스 스프라이트 + zone 배경 + Stage Clear UI |
| 08 | 데모 리허설 + 마지막 fix + Codex β 검토 1회 | qa-sim/client | 1.5h | 풀-쓰루 1회 깔끔 / 알려진 버그 0 |

**총 ~16h 순수 작업 + 디버깅 buffer 50% = 24h**

## 의존성

- 01 → 02 → 03 (netcode 인프라 차례)
- 03 → 04 (broadcast 깔린 후 클라 동기)
- 03 → 05 → 06 (broadcast 깔린 후 적/보스)
- 04, 06 → 07 (서버 흐름 후 시각화)
- 07 → 08 (리허설은 마지막)

## 짚어둔 약속

1. **γ 방식 3회차** (Rule of Three) — Phase 08 = 마일스톤 끝에 1회만 (Codex β xhigh 검토). 각 Phase마다는 48h 응급 모드라 시간 부족
2. **PDL 변경 의무 3종** — Phase 01/03/05/06이 PDL 손댐. 매번: PacketGenerator 재생성 + Shared.dll 빌드/commit + 양쪽 정합 (`.claude/CHANGELOG.md` 2026-05-17 박힌 룰)
3. **테스트는 happy + invalid/auth 페어** — Phase 02에서 모든 기존 핸들러 테스트 페어 채움 (`02_Server/CLAUDE.md` "새 packet handler 추가 시" 4번 조항)

## 검토 요청 (xhigh)

다음 6개 축으로 검토:

1. **헌법 위반 risk** — 각 Phase에서 #1~#5 위반 가능성. 특히 *#1 서버 권위 / #3 신뢰 경계*에서 응급 단순화 시 빠뜨릴 위험. (직전 γ 감사 위반 4건 봉합 직후라 5번째 발생 시 본 마감 전 다시 봉합 부담)
2. **의존성 누락** — Phase A → B 사이 숨은 의존성 (e.g., broadcast 인프라 없이 멀티 캐릭터 렌더 X 등)
3. **시간 분배** — 학부생 + AI 페어 기준 예상 시간 현실성. 너무 작거나 큰 Phase
4. **누락된 작업** — broadcast/전투/UI 흐름에서 빠진 인프라 (e.g., 카메라 멀티 캐릭터 추적, 적 AI 단순화 정도, hit 판정 lag 감안 등)
5. **데모 안정성 risk** — 48h 응급 모드에서 깨지기 쉬운 지점 (lifecycle race, packet ordering, 통합 누락, 유현 Asset 포맷 mismatch 등). M2.5에서 봉합한 lifecycle race가 multi-player에서 *다시 표면화*할 가능성 1순위 의심
6. **PDL 변경 의무 3종 명시 누락** — 어느 Phase 작업 내용에 명시해야 하는지

## 산출물 형식 (산출 파일 = `2026-05-18-m3-phase-plan-codex-review.md`, 본 폴더에 박기)

- Phase별 코멘트 (필요한 것만)
- 추가/삭제/병합 제안 (우선순위 + 이유)
- 48h 안에 완주 가능성 평가 (낙관 / 현실 / 비관 — 각 케이스의 가정)
- 가장 큰 risk 1~2건 명시
- *(선택)* 본인이 모르거나 누락된 작업 후보 짚기

---

**컨텍스트 파일 (필요시 Codex가 직접 읽음)**:
- `CLAUDE.md` — 헌법
- `00_Document/PRD.md` — 무엇을 만들지 (방금 갱신, M3 = Multiplayer & Demo Stage)
- `00_Document/ARCHITECTURE.md` — 어떻게 만들지
- `00_Document/ADR/INDEX.md` — ADR 카테고리
- `01_Phases/youngho/M2.5-hardening/*-DONE.md` — 직전 마일스톤 박제 (M3가 이걸 어떻게 이어가야 하는지)
- `02_Server/CLAUDE.md` — 서버 Layout + 핸들러 추가 규칙 (Phase 02 정합 대상)
- `.claude/CHANGELOG.md` — 하네스 변경 이력 (PDL 의무 3종 박혀있음)
