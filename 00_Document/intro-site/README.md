# 게임 소개 페이지 원본

게임을 처음 보는 방문자를 위한 소개 페이지 한 장의 원본이다. GitHub Pages 주소는 <https://bass131.github.io/dawnholder-server/>다. 목표·범위·결정은 [소개 페이지 goal](../../05_Management/goals/2026-10-08-intro-site/goal.md)에 있다. 이 폴더의 소유는 Management다.

## 공개 방식

[intro-site workflow](../../.github/workflows/intro-site.yml)가 `publish-files.txt`에 적힌 파일만 묶어 배포한다. 기본 branch에 병합된 변경만 공개된다. PR에서는 묶는 단계까지만 돌고 배포하지 않는다. 이 README와 `publish-files.txt`는 목록에 없어서 공개되지 않는다.

로컬에서는 이 폴더의 `index.html`을 브라우저로 열면 된다. 외부 글꼴·스크립트·CDN을 쓰지 않아 인터넷 없이도 같은 모습이다.

## 공개 허용 목록

`publish-files.txt`가 공개 파일의 유일한 목록이다. 파일을 더하거나 빼는 일도 PR과 병합 승인을 거친다.

- UTF-8(BOM 없음)과 LF로 쓴다. 한 줄에 경로 하나다. 빈 줄과 `#`으로 시작하는 줄은 무시한다. 앞뒤 공백은 쓰지 않는다.
- 경로는 이 폴더 기준 상대 경로이고 구분자는 `/`다. 절대 경로·드라이브 문자·`\`·`..`·`.` 구간·`.`으로 시작하는 구간(숨김 파일·폴더)·앞머리 `-`·중복 줄은 쓸 수 없다.
- 이 폴더 안의 일반 파일만 넣을 수 있다. 디렉터리·symlink는 안 된다. `README.md`와 `publish-files.txt` 자신은 넣지 않는다.

## 공개하지 않는 정보

페이지와 공개 파일에는 다음을 넣지 않는다.

- 작업 세션·실행 기록의 식별자, 로컬 절대 경로, 이메일·연락처, 토큰.
- 내부 운영 도구와 작업 방식, PR·branch 진행 기록.
- 기술 설명(서버 구조, 통신 규격, 개발 환경 같은 것). 맨 아래 소스 코드 링크 줄만 예외이고, 그 줄에는 `data-tech-exempt` 속성을 둔다.
- 원천에 없는 세계관·설정·이름, 아직 게임에 없는 것을 있는 것처럼 쓰는 문장, 출시일 약속.
- 유료 에셋 패키지 그림.

## 문장 규칙

- 사실을 말하는 문장에는 `data-fact="F01"` 꼴의 표식을 두고, 아래 「사실 근거」 표에 원천을 적는다.
- 고객 문장의 원천은 게임 화면에 실제로 보이는 문자열과 게임 데이터다. 코드 주석은 보조 근거로만 쓴다. 화면 이름이 없으면 일반 명칭(기사, 보스 같은)으로 쓴다.
- 「지금 게임에 있는 것」, 「개발 중」, 「계획」은 [제품 범위](../PRD.md)의 구분을 따른다.
- 게임 문자열이 바뀌면 페이지와 어긋난다. 아래 표의 기준 commit에서 원천을 다시 대조한다.

## 그림 출처

| 사본 | 원본 경로 | 원본 SHA256 | 자른 영역 | 사본 크기 | 사본 SHA256 |
|---|---|---|---|---|---|
| `images/castle-valley.jpg` | `00_Document/assets/readme-art-castlevalley.png` | `f9f12cb152635b714917da1b771811b443a2d360ca6e118b2ef4a068f31ac0ca` | `0,0,2172,724` | `1600x533` | `541fa5f9394d7b14f478b099615faa48cb95587a42d5ba13dd87c0d4f390faad` |
| `images/knight.png` | `00_Document/assets/readme-art-knight.png` | `746a02682686643b4cd592b99b508b78322db0e2e320ebdb8ae45944a3e143d4` | `161,87,241,283` | `241x283` | `41f2c72f841dda4b35cda9a98d8b0e6f2eb1755775fe258acd52433b52904ee7` |
| `images/mage.png` | `00_Document/assets/readme-art-mage.png` | `a70b23e72caba6a12688d8bc8b9c3cb84995c4306a0a6ee0f10ce17f863b8eba` | `149,125,224,276` | `224x276` | `b31acbb5ddb50452226fa22d302dc63be4f05c3ddf8704e88f4f146ade6d8be6` |
| `images/slime.png` | `03_Client/Assets/Art/Enemy/Slime/Slime_Idle.png` | `490925cdae79a2e63498d284c90ad51dc1c37813843ed263f63aa2607d6e707d` | `168,231,176,140` | `176x140` | `852788c8b18c4baaebf8433e47907033cb138223eaca817acdf2965395d94e43` |
| `images/golem.png` | `03_Client/Assets/Art/Enemy/Golem/Golem_Idle.png` | `906c8f86ad54eaba5b79604a7c9a0d7eab183d8fd8d8c6e470b943a0ad68c62a` | `140,105,209,209` | `209x209` | `b44dcf750fbb3fda0684612e5578da9dcbd83cfe938db13d17a1aad429ad5d0d` |
| `images/boss.png` | `03_Client/Assets/Art/Enemy/Boss_Vampire/Boss_Idle.png` | `5986c073247f52e0b8e36993c74e261bb9187618d680817c58823a8bd94c6d04` | `84,79,278,291` | `278x291` | `137e134319f4d011518851d5b26e4434d5eb6388a23ac4e6860966fcc0573a3e` |

모든 그림은 저장소에 이미 있는 그림의 웹용 사본이다. 원본과 `.meta`는 바꾸지 않았다. 사본은 원본의 첫 칸을 잘라 투명 여백을 걷어 냈고, 배경만 폭 1600으로 줄여 JPEG(품질 85)로 저장했다. 확대한 사본은 없다. 사본 생성 스크립트는 저장소에 두지 않았다(goal 결정). 원본 일부에는 AI 생성 도구의 출처 표식이 있지만 사본에서는 사라질 수 있어서, 페이지 맨 아래의 「그림 일부는 AI 생성 도구를 활용해 만들었어요」 문장과 이 표가 그 표기를 대신한다.

자른 영역은 원본 왼쪽 위 원점의 `x,y,폭,높이` 픽셀이다. 적 그림의 첫 칸은 각 원본 `.meta`의 `*_Idle_0` 영역이고, 기사·마법사는 512px 칸 스트립의 첫 칸이다.

## 사실 근거

| 번호 | 페이지 문장 | 원천 | 기준 commit |
|---|---|---|---|
| F01 | 친구들과 함께 사냥을 떠나는 2D 사이드스크롤 RPG | `00_Document/PRD.md:3` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F02 | 개발 중인 프로토타입 | `00_Document/PRD.md:3` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F03 | 한국어로 즐기는 PC 게임을 목표로 만들고 있다 | `00_Document/PRD.md:18` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F04 | 마을 대장장이 대사 「어이, 모험가! 무기 손질이 필요하면 들러. ...가게는 아직 준비 중이지만 말이야.」 | `03_Client/Assets/Scenes/01.PlayArea/Town.unity:5986-5988`; `03_Client/Assets/Scripts/Gameplay/NpcInteractable.cs:84` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F05 | 퀘스트 이름 「마을의 위협」, 목표 「사냥터의 몬스터 처치」 | `03_Client/Assets/Scripts/UI/QuestProgressHud.cs:31-32` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F06 | 옆으로 펼쳐진 맵을 달리고 점프한다 | `00_Document/PRD.md:3`; `00_Document/PRD.md:9` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F07 | 근접 공격·원거리 공격, 직업마다 다른 스킬 | `00_Document/PRD.md:10`; `98_Shared/GameData/Combat/SkillCatalog.cs:34-36` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F08 | 친구를 파티에 초대하고 수락하면 함께 사냥한다 | `00_Document/PRD.md:11`; `00_Document/FEATURE_MAP.md:17`; `03_Client/Assets/Scripts/UI/PartyInvitePopup.cs:194`; `03_Client/Assets/Scripts/UI/PartyInvitePopup.cs:212` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F09 | 몬스터를 정해진 수만큼 처치하면 보스에게 가는 길이 열린다 | `00_Document/PRD.md:11`; `00_Document/FEATURE_MAP.md:18`; `03_Client/Assets/Scripts/Network/Handlers/Zone/PortalLockedHandler.cs:32` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F10 | 쓰러지더라도 다시 일어나 도전할 수 있다 | `00_Document/PRD.md:10`; `00_Document/FEATURE_MAP.md:15` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F11 | 기사는 근접 직업이고 체력과 방어력이 높다 | `03_Client/Assets/Scripts/UI/PartyMemberHud.cs:99`; `98_Shared/GameData/Combat/PlayerStats.cs:41`; `98_Shared/GameData/Combat/PlayerStats.cs:45`; `02_Server/GameServer/Combat/CombatConstants.cs:21` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F12 | 기사 스킬: 짧은 거리를 빠르게 돌진하는 대시 | `98_Shared/GameData/Combat/SkillCatalog.cs:35`; `98_Shared/GameData/Enums/SkillId.cs:15`; `98_Shared/GameData/Constants.cs:101` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F13 | 마법사는 원거리 직업이고 체력은 낮지만 더 빠르다 | `03_Client/Assets/Scripts/UI/PartyMemberHud.cs:99`; `98_Shared/GameData/Combat/PlayerStats.cs:41`; `98_Shared/GameData/Combat/PlayerStats.cs:45`; `02_Server/GameServer/Combat/CombatConstants.cs:58`; `02_Server/GameServer/Combat/CombatConstants.cs:62` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F14 | 마법사 스킬: 적들 위로 떨어지는 낙뢰, 원하는 방향으로 옮겨 가는 순간이동 | `98_Shared/GameData/Combat/SkillCatalog.cs:34`; `98_Shared/GameData/Combat/SkillCatalog.cs:36`; `98_Shared/GameData/Enums/SkillId.cs:14`; `98_Shared/GameData/Enums/SkillId.cs:16` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F15 | 마을 → 사냥터 → 보스 → 결과 화면 → 다시 마을 | `02_Server/GameServer/Maps/PortalTable.cs:68-74`; `02_Server/GameServer/Maps/PortalTable.cs:81-94`; `02_Server/GameServer/Maps/PortalTable.cs:101-115`; `02_Server/GameServer/Maps/PortalTable.cs:120-126` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F16 | 만나게 될 몬스터: 슬라임, 골렘, 보스 | `98_Shared/GameData/Enums/EnemyKind.cs:8-10`; `03_Client/Assets/Resources/EnemyVisualTable.asset:16-21` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F17 | 개발 중: 아이템과 인벤토리 | `00_Document/FEATURE_MAP.md:19`; `00_Document/PRD.md:14` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F18 | 개발 중: 캐릭터 저장과 다시 접속했을 때 이어하기 | `00_Document/PRD.md:13`; `00_Document/operations/CURRENT.md:7` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F19 | 계획: 사냥으로 얻은 자원으로 길드 거점 발전, 길드 창고와 거점 시설 | `00_Document/PRD.md:3`; `00_Document/PRD.md:14` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F20 | 2026년 6월 첫 시연을 마쳤다 | `README.md:22`; `README.md:84` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F21 | 2026년 11월 19일 졸업작품 마감 | `README.md:85` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F22 | 지금은 내려받아 플레이할 수 있는 공개 버전이 없다 | `README.md:12`; `00_Document/PRD.md:18` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F23 | KNUT 4인 캡스톤 프로젝트, 팀장 유영호 | `README.md:6` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F24 | 재사용 권한을 부여하지 않는다(all rights reserved) | `README.md:97-98` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F25 | 그림 일부는 AI 생성 도구를 활용해 만들었다 | `README.md:34` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |
| F26 | 「앞으로」 절의 항목은 아직 게임에 없다 | `00_Document/PRD.md:13-14`; `00_Document/FEATURE_MAP.md:19` | `7086d45b92b76742d8c11facbf8a1031b81d0853` |

기준 commit은 모두 `7086d45b92b76742d8c11facbf8a1031b81d0853`이다. 행 번호는 그 commit의 파일 기준이다.

F04의 대사는 빌드 장면 `Town.unity`에 배치된 대장장이(`Npc_BlackSmith`)의 대화 문자열이고, 상호작용하면 대화 창에 그대로 보인다(`NpcInteractable.cs:84`). 장면에 배치되지 않은 prefab의 문자열은 화면에 나오지 않으므로 원천으로 쓰지 않는다. 대장장이의 화면 이름은 없어 일반 명칭으로 쓴다. F11·F13의 근접·원거리는 공격 범위 값(`CombatConstants.cs:21`의 기사 1.5, `:58`의 마법사 11.0)과 마법사 투사체 속도(`:62`)로, 체력·속도는 `PlayerStats.cs`의 기사 150·4와 마법사 80·6으로 대조했다. F12·F14의 스킬 이름은 화면 문자열이 없어 직업 제한 표(`SkillCatalog.cs`)와 열거값 주석을 대조한 일반 명칭이다. F16의 슬라임은 일반 몬스터(`EnemyKind` 0)에 연결된 그림(`EnemyVisualTable.asset` → `Prefabs/Enemies/Enemy_Normal.prefab` → `Art/Enemy/Slime/Slime_Idle.png`)이다. 보스의 고유 이름은 원천에 없어 쓰지 않는다.
