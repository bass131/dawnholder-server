# ADR — Architecture Decision Records

> **이 문서의 역할**: "왜 이렇게 만들었는지" 결정의 기록. 6개월 뒤에
> 본인이 봐도, AI가 봐도 "왜 이 선택을 했지?"를 알 수 있게.
>
> **포맷**: 결정마다 3줄. **결정 / 이유 / 트레이드오프**. 길게 쓰지 말 것.
>
> **언제 쓰나**: 되돌리기 어려운 결정을 할 때마다. 작은 코드 결정은 ADR
> 안 씀. "이걸 바꾸려면 며칠 걸리겠다" 싶은 것만.

---

## 본문은 카테고리별로 외부화됨

본 파일이 220줄 임계 도달(ADR-014 정책 (b))로 채택된 ADR 본문을 카테고리 폴더로 외부화. 전체 목록 + 1줄 요약 + 카테고리 분류는 **[`ADR/INDEX.md`](ADR/INDEX.md)** 참조.

빠른 탐색:
- **[ADR/tech-stack/](ADR/tech-stack/)** — 스택·도구체인·환경 (9개: 001 002 003 004 005 010 011 012 017)
- **[ADR/gameplay/](ADR/gameplay/)** — 게임 디자인·스코프 (4개: 006 007 008 009)
- **[ADR/harness/](ADR/harness/)** — 작업 흐름·문서·훅 (8개: 013 014 015 016 018 019 020 021)

---

## ADR 템플릿

```markdown
### ADR-NNN: [결정 제목]
**날짜**: YYYY-MM-DD
**상태**: 채택됨 | 폐기됨 | 대체됨(ADR-NNN으로)
**결정**: [무엇을 선택했는지 한 줄]
**이유**: [왜 선택했는지 한두 줄]
**트레이드오프**: [무엇을 포기했는지 한두 줄]
```

새 ADR 추가 절차:
1. 적절한 카테고리 폴더(`ADR/{tech-stack|gameplay|harness}/`)에 `ADR-NNN-slug.md` 생성
2. [`ADR/INDEX.md`](ADR/INDEX.md)에 한 줄 추가
3. 본 파일의 후보 표에서 채택분 제거(또는 번호 shift)
4. [`ADR_History.md`](ADR_History.md)에 변경 이력 한 줄

---

## 채워질 ADR 후보들 (예시)

> 본인이 진행하다가 다음 결정들을 할 때 ADR로 기록하세요. 번호는 채택 순서대로 부여 — 아래는 가이드일 뿐. 채택분(ADR-020 = 훅 환경 의존성, 2026-05-14 / ADR-019 = reviewer 에이전트, 2026-05-15 / ADR-021 = 클라이언트 UI Additive Scene 분리, 2026-05-17)은 후보 표에서 제거됨.

- **ADR-022**: 인증 방식 (단순 닉네임 → JWT? 세션?)
- **ADR-023**: 캐릭터 데이터 스키마 (정규화 vs JSONB)
- **ADR-024**: 채팅 시스템 (TCP로 전송 vs 별도 채널)
- **ADR-025**: 로그 저장 (로컬 파일 vs 외부 sink)
- **ADR-026**: 헤드리스 봇의 자동화 방식
- **ADR 후보**: WDAC 미서명 DLL 차단 정책 정리 (Burst 등 도구 활성화 필요 시점에)
- **ADR-028 채택 (2026-05-29)**: Code Convention 수립 (`00_Document/conventions/`). *구조/SRP*(God class 분리 등 판단 영역)는 Convention + reviewer 축 6으로 **지금 실현**. ***.editorconfig + Roslyn(포매팅, 기계적 영역)*은 CODE_CONVENTION §4로 M4.4 이월** — 옛 'Reviewer 후속 Roslyn' 후보 중 도구 위임이 *가능한* 포매팅 부분만 남긴 것(구조/SRP는 도구로 자동 감지 불가라 사람/reviewer 판단).
- ...

---

## 변경 이력

> 이력은 [`ADR_History.md`](ADR_History.md) 참조. 새 ADR 추가/갱신 시 본 파일이 아니라 거기에 한 줄씩 추가 ([`00_Document/policies/doc-thresholds.md`](policies/doc-thresholds.md) — 누적 섹션 외부화).
