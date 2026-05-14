### ADR-016: Notion 협업 3자 분업 (Claude / Codex / 본인)
**날짜**: 2026-05-11
**상태**: 채택됨
**결정**: Notion "Dawnholder 협업 히스토리" DB는 3자 분업으로 운영. **Claude** = 사실 박제 (1차 페이지 생성 + 용어 풀이). **Codex** = Notion 재편집 + 면접 답변 보강 (`codex exec` CLI, create는 `-s workspace-write`, update는 `--dangerously-bypass-approvals-and-sandbox`, stdin은 `< /dev/null`로 차단). **본인** = 회고/학습 일지. 자세한 원칙(8단 구조·STAR 출력·용어 처리·핸드오프 트리거)은 [`.claude/templates/done-md-template.md`](../../../.claude/templates/done-md-template.md) "Notion 협업 분업 원칙" 섹션 영속화.
**이유**: AI 단일 작성은 사실/회고/면접 답변 톤이 섞여 어느 것도 강하지 않음. 3자 분업으로 각 역할 톤 명확화 + 본인 학습/면접 무기 분리. CONTEXT 응축 시 원칙 유실 위험은 template 영속화로 해소.
**트레이드오프**: 협업 도구 추가(Codex) → 환경 의존성 증가. Codex CLI 옵션이 create/update에 따라 다름(혼동 위험 → 핸드오프 절차에 박힘). 본인이 회고 안 쓰면 3자 중 1자가 비어있음 (의도된 분업).
