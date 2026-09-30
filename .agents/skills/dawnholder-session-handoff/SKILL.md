---
name: dawnholder-session-handoff
description: Dawnholder의 Orca 신규 세션 준비를 확인하고 최소 맥락을 인계한다. 빈 신규 prompt에서 준비 대기가 만료되면 허용된 no-op 일반 메시지 한 번으로 응답을 확인한다. 감독형 작업 추적은 goal-loop의 Orca 지침을 따른다.
---

# Dawnholder 세션 준비와 인계

사용자가 허용한 신규 세션·인계 범위에서만 적용한다. 이 스킬은 세션 생성·외부 변경 권한을 추가하지 않는다. 스킬 설정만 요청받았다면 실제 터미널을 조작하지 않는다.

## 실행 기준

- 설치된 `orca-cli` 스킬로 실행 파일을 선택하고 그 실행 파일의 `skills get orca-cli` 버전 일치 가이드를 읽는다. 정확한 문법·지원 기능은 이 가이드가 기준이다.
- 현재 runtime, 저장소·worktree 경로와 전체 ID, terminal handle, incarnation을 식별한다. 제공되지 않은 incarnation은 미확인으로 기록한다. runtime/handle을 하드코딩하거나 재시작 전 handle을 재사용하지 않는다.
- `terminal show`와 제한된 `terminal read`로 준비 상태를 확인한다. 긴 출력은 cursor와 limit으로 필요한 구간만 읽는다. busy·모달·사용자가 작성 중인 prompt는 건드리지 않는다.
- [AGENTS](../../../AGENTS.md)의 모델 라우팅과 새 Codex CLI의 `--no-daemon`을 따른다. 요청 모델과 확인 모델을 구분하고 확인할 수 없으면 `unknown`으로 남긴다.

## 신규 prompt 준비 확인

1. bounded readiness wait를 실행하고 `satisfied` 값을 확인한다. 출력이나 timeout만으로 준비됐다고 판단하지 않는다.
2. wait가 만료됐고 읽기 결과가 **빈 신규 prompt**임을 확인한 경우에만, 권한 있는 준비 확인 no-op 일반 메시지를 딱 한 번 보낸다. 예: “작업은 시작하지 말고 준비되었으면 READY와 현재 작업 경로만 답해주세요.”
3. busy·모달·작성 중 prompt이거나 상태가 불명확하면 보내지 않고 상태를 보고한다. 준비 확인 메시지의 실제 응답을 bounded read/wait로 확인한 뒤에만 handoff를 보낸다.
4. READY 응답 뒤 실제 handoff 직전에 target·runtime·incarnation을 새로 조회해 준비 확인 때의 값과 대조한다. 값이 바뀌거나 동일성을 확인할 수 없으면 이전 READY·receipt를 재사용하지 않고 보류한다. 새 대상에 자동 전송하지 않는다.
5. `input_accepted`는 입력 접수, `turn_started`는 턴 시작 증거, 실제 준비 응답은 준비 확인이다. 이 셋을 같은 성공으로 보고하지 않는다.
6. accepted 뒤 침묵에는 새 메시지를 보내지 않는다. 동일 request replay 전에도 runtime·target·incarnation 동일성을 새로 확인하고, 버전 일치 가이드가 중복 입력 없는 receipt replay를 보장할 때만 동일 request ID와 동일 payload로 receipt를 확인한다. replay 미지원·request 불명확·준비 응답 부재이면 handoff를 보류하고 관찰 상태를 보고한다.

준비 확인 실패는 무한 대기·강제 restart·권한 변경·세션 종료의 근거가 아니다. 일반 메시지로 응답을 확인했더라도 이전 무응답의 원인이 밝혀졌다고 주장하지 않는다.

반복 실패 시 관측 단계·최소 증거 경로·미확정 원인만 durable 인계 맥락에 갱신한다. 별도 장부나 추측 원인을 늘리지 않으며, 이미 accepted된 요청의 payload를 바꿔 재전송하지 않는다.

## 최소 맥락 인계

- durable handoff에는 목표·완료조건, 확정 결정·보존 계약, 작업 경로·branch/base/HEAD, 허용 수정 파일, 실행 자원 소유권, 검증 근거·미실행 범위, 결과 기록 경로와 막힐 때 보고 대상을 남긴다. 전체 대화·로그를 복제하지 않는다.
- 같은 checkout이면 수정 파일 범위와 branch 전환 담당자를 명시하고 기존 작성자의 쓰기 종료를 확인한다. 수신자가 임의로 branch를 전환하거나 동시에 같은 파일을 쓰게 하지 않는다.
- 인계 메시지의 durable request/receipt를 보존하고 접수·턴 시작 확인 여부를 각각 보고한다. accepted 뒤 침묵은 중복 전송하지 않고 위 동일 request 확인 절차를 따른다.
- 원래 메인이 유지되는 좁은 위임이면 복귀 대상과 소유권을 명시한다. 전체 handoff이면 새 worktree ID·agent handle과 접수 receipt를 보고한 뒤 원래 에이전트는 멈추며 완료까지 기다리지 않는다.
- 단순 handoff에 orchestration task-create/dispatch/check 추적을 만들지 않는다. 사용자가 감독·결과 대기·DAG 조정을 요청한 경우에만 [Orca 감독 지침](../dawnholder-goal-loop/references/orca-work.md)과 `orchestration` 스킬을 적용한다.
