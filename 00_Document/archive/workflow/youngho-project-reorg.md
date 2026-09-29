# 문서 지도와 링크 정리 — project-reorg

원래 작업 묶음: `01_Phases/youngho/project-reorg`. 주영역은 workflow이며 관련 영역은 cross-cutting이다.

## 문제와 변경

문서 위치보다 깨진 링크와 색인 부재가 탐색을 방해한다는 조사에 따라 색인을 만들고 링크·옛 기록을 정리했다.

## 작업별 근거와 남은 조건

요지는 원문의 요약·목표와 검증·제약 부분을 짧게 뽑은 것이다. 수치와 상태는 해당 문서 시점의 기록이며 현재의 완료 여부를 뜻하지 않는다. 계획을 통과 결과로 바꾸지 않았고, 다른 시점의 결과도 합산하지 않았다. 전체 수치·사용자 인용·후속 갱신은 고정 원문에서 확인한다.

| 원문과 당시 상태 | 내용·검증·제약 |
|---|---|
| <a id="source-351bc612bedf"></a>[project-reorg — 전체 repo 문서 정리 (마감)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/project-reorg/_reorg-DONE.html)<br>결과 기록; 상태: 명시 없음; 2026-06-19 | 🧩 결정 흐름 (영호 게이트) |
| <a id="source-748d5458dd75"></a>[project-reorg — 전체 repo 문서 정리 (마감)](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/project-reorg/_reorg-DONE.md)<br>결과 기록; 상태: done; 2026-06-19 | 전체 repo 문서 정리 — UltraCode 실측 결과 파편화 원인은 폴더위치 아니라 깨진링크+INDEX부재. 깨진링크 ~60건 수정 + 마스터/reviews INDEX 신설 + 미사용(learning-journal 삭제·M4-backlog archive·state 백업) 정리. 게임 코드 0 변경.<br>원문 발췌 [AC 검증 결과]: 잔여 (범위 밖): `01_Phases/` frozen `-DONE.md` 8개의 깨진 링크(옛 `New_Harness/` 경로·삭제된 `journal/phase.md`·산문 오인). pre-existing이고 append-only라 미수정 — 동결 역사 보존.<br>원문 발췌 [🧪 테스트 결과]: 게임 코드 git diff 0 / 01_Phases 미변경 / 건드린 영역 dangling 0 / plan-auditor GO·위반 0 / append-only 보존. (docs-only라 WSL2 게임 회귀 불요.) |
