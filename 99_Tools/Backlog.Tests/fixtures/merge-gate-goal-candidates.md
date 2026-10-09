# 병합 관문과 운영 정본 현행화
## 다음 계획 후보

이 goal 밖으로 둔 일이다. PR2에서 BACKLOG에 같은 규칙으로 기록한다.

- Codex 세션의 병합 차단: 저장소 `.codex/` 프로젝트 hook으로 같은 판정을 거는 방법. 신뢰한 프로젝트에서만 읽고 hook 내용이 바뀔 때마다 사용자 검토가 필요하다(초안 세부 근거 4). 사용자 질문 1 A로 이번에는 하지 않는다.
- 에이전트용 GitHub 계정 분리와 ruleset 보강: 서버 쪽에서 모든 세션을 막는 대안. 비용은 계정·classic 토큰·이 PC의 gh·git 로그인 전환이다(초안 세부 근거 3). ruleset 관리자 우회를 「PR로만」으로 바꾸는 더 싼 중간안은 문서 확인 전이다.
- 우편함 대기 `&`·`/dev/null` 차단 hook: 묶음 2 계획 12에서 같은 PreToolUse 층으로 다룬다.
  - 두 번째 발생: Management 리드가 06:38:36Z에 `check --wait`를 다른 명령 끝에 붙여 `&`와 `> /dev/null 2>&1`로 띄웠다(원문 `msg_9f655bb20a2c`, 근거 management-active E/check-after-stray-wait.json·E/inbox-after-stray-wait.json).
  - 메인 판단 `msg_cec953dd3fdc`(06:40:23Z): 같은 리드·같은 실수의 두 번째라 교정 층 정본에 따라 문서보다 높은 층이 맞다. 후보는 병합 관문 hook과 같은 PreToolUse 자리의 검사다.
  - Rules 리드 첫 발생: 06:57Z에 `--wait` 없는 `check --ack`의 출력을 `/dev/null`로 버렸다. 06:59:10Z에 다시 조회해 0건을 확인했다(E/session/wait62.raw.txt). 검사 범위를 `--wait`에 한정할지 `check` 전체로 할지 정할 근거다.
- 병합 관문 후속(세 번째 재검증 비차단): T1 그물 조건 3의 `+main`(동작 계약 「맨 앞 `+`를 뗀 낱말」 보정), `merge-policy.mjs`의 한국어 이유 주석 세 줄을 파일 관례인 영어로, O-T3 정식 단독 병합이 그물을 건너뛰는 이유 주석. 원천은 E/reverify3/verdict.md다.

<a id="orca-moved-history"></a>

## ORCA에서 옮긴 서술
