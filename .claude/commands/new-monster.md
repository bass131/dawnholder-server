---
description: 새 몬스터 추가 (데이터만, 엔진 코드 변경 없음)
argument-hint: <name> <level> <map>
---

"몬스터 추가" 워크플로우 시작. 사용자 요청:
**$ARGUMENTS**

`content` 서브에이전트에게 위임하세요. 브리프:

1. `shared/GameData/Tables/monsters.json`을 읽어 기존 스키마 확인 후
   다음 빈 monster id 선택.
2. 다음 필드로 entry 추가: id, name, level, hp, attack, defense,
   move speed, attack speed, sprite ref, sound ref, drop table id, AI type.
3. 요청된 동작에 존재하지 않는 AI type이 필요하면 STOP하고
   gameplay 에이전트에게 먼저 라우팅.
4. `server/GameServer/Maps/Definitions/<map>.json`의 해당 맵 정의 파일에
   spawn entry 추가.
5. 서버 컨텐츠 로더의 schema 체크가 여전히 통과하는지 확인:
   `dotnet test server/GameServer.Tests --filter Category=ContentSchema`.
6. 참고: 클라이언트는 `client/Assets/Resources/Content/Monsters/`에
   sprite가 존재하기만 하면 됨. 없으면 사용자에게 제공 요청.

엔진 코드 절대 수정 금지. 필요하면 gameplay로 라우팅.
