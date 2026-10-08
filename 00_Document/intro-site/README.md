# 게임 소개 페이지 원본

게임을 처음 보는 방문자를 위한 소개 페이지 한 장의 원본이다. GitHub Pages 주소는 <https://bass131.github.io/dawnholder-server/>다. 목표·범위·결정은 [소개 페이지 goal](../../05_Management/goals/2026-10-08-intro-site/goal.md)에 있다. 이 폴더의 소유는 Management다.

## 공개 방식

[intro-site workflow](../../.github/workflows/intro-site.yml)가 `publish-files.txt`에 적힌 파일만 묶어 배포한다. 기본 branch에 병합된 변경만 공개된다. PR에서는 묶는 단계까지만 돌고 배포하지 않는다. 이 README와 `publish-files.txt`는 목록에 없어서 공개되지 않는다.

로컬에서는 이 폴더를 로컬 정적 서버로 열어 본다. `file://`로 열면 브라우저 보안 규칙 때문에 글꼴이 막힐 수 있다. 외부 글꼴·스크립트·CDN을 쓰지 않아 인터넷 없이도 같은 모습이다.

## 공개 허용 목록

`publish-files.txt`가 공개 파일의 유일한 목록이다. 파일을 더하거나 빼는 일도 PR과 병합 승인을 거친다.

- UTF-8(BOM 없음)과 LF로 쓴다. 한 줄에 경로 하나다. 빈 줄과 `#`으로 시작하는 줄은 무시한다. 앞뒤 공백은 쓰지 않는다.
- 경로는 이 폴더 기준 상대 경로이고 구분자는 `/`다. 절대 경로·드라이브 문자·`\`·`..`·`.` 구간·`.`으로 시작하는 구간(숨김 파일·폴더)·앞머리 `-`·중복 줄은 쓸 수 없다.
- 이 폴더 안의 일반 파일만 넣을 수 있다. 디렉터리·symlink는 안 된다. `README.md`와 `publish-files.txt` 자신은 넣지 않는다.
- 넣을 수 있는 파일은 `index.html`, `styles.css`, 스크립트 파일 하나(`*.js`), `images/` 아래 그림(`.png`·`.jpg`·`.webp`), `fonts/` 아래 글꼴(`.woff2`)과 라이선스 전문 `fonts/LICENSE.txt`다. 글꼴이 하나라도 있으면 `fonts/LICENSE.txt`도 넣는다.

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
| `images/layer-1-sky.webp` | `03_Client/Assets/Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/01_Sky_SunGlow.png` | `aedec3341678c82e008b4abe4f86fb57c46c1a58c1f09d9751beecee727272f4` | `0,0,2172,724` | `2172x724` | `78c7c244ae10ba1751e9d9eb796117f73de4bae15783b904e51a061a2b9fd2a0` |
| `images/layer-2-clouds.webp` | `03_Client/Assets/Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/02_Far_Clouds.png` | `961f5ae805169898bd61c853102ffe92390bf0fb64e893fc37cd57689fd4f44e` | `0,0,2172,724` | `2172x724` | `7fe57151519e2afbecd06c223f0f2e9de30c64979052a6895d104215c7bc0683` |
| `images/layer-3-far-mountains.webp` | `03_Client/Assets/Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/03_Distant_Mountains.png` | `27126a40a6580bac03345aa8c017d26f18908e19510630f41ca2d63aa28bbb32` | `0,0,2172,724` | `2172x724` | `90596ba73d815fed7652da21f2ff20970fda5d640029972de12d546015d6db4f` |
| `images/layer-4-hills.webp` | `03_Client/Assets/Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/04_Mid_Mountains_Hills.png` | `ec67b31a7fce5ef8218212a2965e2646f32fb1fb96a9112db8a941f29ddf8ad5` | `0,0,2172,724` | `2172x724` | `9187610e0f5d63a7a426856f4ef488c3a819dc40fb2bd7f05ebd9230766c0aa7` |
| `images/layer-5-castle.webp` | `03_Client/Assets/Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/05_Castle_City.png` | `ba9726dac966dbe5bfb98d63949be3352af1de58dedfd581645e2440f534db25` | `0,0,2172,724` | `2172x724` | `327d1536095fef0913519f127850e0e037f46873cf53839bb386d6b6d8aec7f0` |
| `images/layer-6-valley.webp` | `03_Client/Assets/Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/06_Valley_Fields_Ruins.png` | `129f8d4dcd061afdc86bdb38570af1bec2e52d4c55875a553c615c5596beccc2` | `0,0,2172,724` | `2172x724` | `1f459cad96c09a717bb5b3a66ba7563e312924f5b6175b5645fa48bdfb8a6fb9` |
| `images/logo.webp` | `03_Client/Assets/Art/UI/BackGround/Dawnholder_MainTitle_Banner_keyed.png` | `b7fb7659c35778bffe2f6d84f39186695a34cf6e69c89cf44e5ecc8a41ba8c9c` | `0,0,1024,424;1024,0,1024,424;2048,0,1024,424;3072,0,1024,424;0,424,1024,424;1024,424,1024,424;2048,424,1024,424;3072,424,1024,424;0,848,1024,424;1024,848,1024,424;2048,848,1024,424;3072,848,1024,424;0,1272,1024,424;1024,1272,1024,424;2048,1272,1024,424;3072,1272,1024,424` | `10240x265` | `2a314fa239a801c1369ec2f4224a5562693eaa79fad122962198f6cf87f6f909` |
| `images/knight-idle.webp` | `03_Client/Assets/Art/Characters/Playable/Knight/Knight_Idle.png` | `8267c5a16f1d96d0eef905749cf4b842a0e62bd022513a6d8e8f604f8f3d41b4` | `0,0,512,370;512,0,512,370;1024,0,512,370;1536,0,512,370;0,370,512,370;512,370,512,370;1024,370,512,370;1536,370,512,370;0,740,512,370;512,740,512,370;1024,740,512,370;1536,740,512,370;0,1110,512,370;512,1110,512,370;1024,1110,512,370;1536,1110,512,370` | `3648x165` | `f70863256367ba8bca69922fd3d18c6843781c0df13e5961a8f4ff97b8ff2b6e` |
| `images/knight-move.webp` | `03_Client/Assets/Art/Characters/Playable/Knight/Knight_Move.png` | `27cded85f6e5c4026f64312c48fb75dfaa528c1963c5ab80ba590ee60eef65d1` | `0,0,512,370;512,0,512,370;1024,0,512,370;1536,0,512,370;0,370,512,370;512,370,512,370;1024,370,512,370;1536,370,512,370;0,740,512,370;512,740,512,370;1024,740,512,370;1536,740,512,370;0,1110,512,370;512,1110,512,370;1024,1110,512,370;1536,1110,512,370` | `3648x165` | `10ac2420992206ba3c479f0223c15f667c6015c68fe1ebdb1209410da97f808e` |
| `images/knight-attack.webp` | `03_Client/Assets/Art/Characters/Playable/Knight/Knight_Attack0.png` | `5e03c132b9fb2f32af8cc9df86d53f27c8ff8011dcb665c00bf213ce999b465d` | `0,0,512,281;512,0,512,281;1024,0,512,281;1536,0,512,281;0,281,512,281;512,281,512,281;1024,281,512,281;1536,281,512,281;0,562,512,281;512,562,512,281;1024,562,512,281;1536,562,512,281;0,843,512,281;512,843,512,281;1024,843,512,281;1536,843,512,281` | `5712x196` | `1c0f2ba1f34ef561972aeff8191a35e1c580e8e167762654f350f07760ec93d2` |
| `images/knight-dash-effect.webp` | `03_Client/Assets/Art/Characters/Playable/Knight/Skill_Effect/Sprites/Dash/Knight_Dash_Skill_Effect.png` | `f3e5c708c1a874fe6a6b561fc50c231b4aaae8cef038a8033ff0bf9cc8c3585a` | `0,0,600,280;600,0,600,280;1200,0,600,280;1800,0,600,280;2400,0,600,280;0,280,600,280;600,280,600,280;1200,280,600,280;1800,280,600,280` | `4320x224` | `71e6612831a75c9d99fa9aaa93fa43f1688d69941a8eaf919cc3941733880d5f` |
| `images/mage-idle.webp` | `03_Client/Assets/Art/Characters/Playable/Mage/Mage_Idle.png` | `7f5c8dfdf2223201e23f7f2a86d1a933094d78df73b725d09f7607f19fb8cbac` | `0,0,512,401;512,0,512,401;1024,0,512,401;1536,0,512,401;0,401,512,401;512,401,512,401;1024,401,512,401;1536,401,512,401;0,802,512,401;512,802,512,401;1024,802,512,401;1536,802,512,401;0,1203,512,401;512,1203,512,401;1024,1203,512,401;1536,1203,512,401` | `3456x169` | `a47c660bc2a4d3ab4b41748183ff30788bef72cb83f96c0bc4640a5c91e68e27` |
| `images/mage-move.webp` | `03_Client/Assets/Art/Characters/Playable/Mage/Mage_Move.png` | `07c7d905c41314b0eb8f6f3f19f4edde09530444c88b254d247e3c931cd6bed5` | `0,0,512,401;512,0,512,401;1024,0,512,401;1536,0,512,401;0,401,512,401;512,401,512,401;1024,401,512,401;1536,401,512,401;0,802,512,401;512,802,512,401;1024,802,512,401;1536,802,512,401;0,1203,512,401;512,1203,512,401;1024,1203,512,401;1536,1203,512,401` | `3456x169` | `40f94bca918eabfd1f7da92b6d479c23efd9065ec43d619e6098e59984bb4eea` |
| `images/mage-cast.webp` | `03_Client/Assets/Art/Characters/Playable/Mage/Mage_Cast_Channeling.png` | `6a873c1d03b4d4194cbe35ce0587de668ed891c8564a7c317e523c9878dad010` | `0,0,512,447;512,0,512,447;1024,0,512,447;1536,0,512,447;0,447,512,447;512,447,512,447;1024,447,512,447;1536,447,512,447;0,894,512,447;512,894,512,447;1024,894,512,447;1536,894,512,447;0,1341,512,448;512,1341,512,448;1024,1341,512,448;1536,1341,512,448` | `4240x232` | `7f458fa29160b2ea908db89b286e33f54ffa51b0629c3c2a68b88ad88fe26bb4` |
| `images/mage-thunder-effect.webp` | `03_Client/Assets/Art/Characters/Playable/Mage/Skill_Effect/Sprites/ThunderVolt/Mage_ThunderVolt.png` | `80e5a3d051dd6e7d33fb57e77e84ad6e0a231b7bd0a66e9aa9e87645e6bf762a` | `0,0,465,911;465,0,465,911;930,0,465,911;1395,0,465,911;1860,0,465,911;0,913,465,911;467,913,465,911;930,911,465,911;1395,911,465,911;1860,911,465,911` | `1620x316` | `dc76c929fef4610d9b34e09166cfcacc35f8f142a47d9f3d688fcd1425c0cd3b` |
| `images/mage-teleport-effect.webp` | `03_Client/Assets/Art/Characters/Playable/Mage/Skill_Effect/Sprites/Teleport/Mage_Teleport.png` | `edf7795e980c0c3876bd5cb9b41425e6d3768267045e490b3e01a510ae617c95` | `1,0,377,568;379,0,377,568;754,0,377,568;1133,0,377,568;1508,0,377,568` | `755x228` | `22aa564a80882dddcd693e54139b9a9f9af60da12ff996e63aabe53ccfc3fa0b` |
| `images/slime-idle.webp` | `03_Client/Assets/Art/Enemy/Slime/Slime_Idle.png` | `490925cdae79a2e63498d284c90ad51dc1c37813843ed263f63aa2607d6e707d` | `0,0,512,371;512,0,512,371;1024,0,512,371;1536,0,512,371;0,371,512,371;512,371,512,371;1024,371,512,371;1536,371,512,371;0,742,512,371;512,742,512,371;1024,742,512,371;1536,742,512,371;0,1113,512,371;512,1113,512,371;1024,1113,512,371;1536,1113,512,371` | `2624x119` | `a073b19447a190f17ee8d3f9c2cb2dd02ab8c41e6cf84dfcf4416d822aa712a1` |
| `images/golem-idle.webp` | `03_Client/Assets/Art/Enemy/Golem/Golem_Idle.png` | `906c8f86ad54eaba5b79604a7c9a0d7eab183d8fd8d8c6e470b943a0ad68c62a` | `0,0,512,314;512,0,512,314;1024,0,512,314;1536,0,512,314;0,315,512,314;512,314,512,314;1024,314,512,314;1536,314,512,314;0,628,512,314;512,628,512,314;1024,628,512,314;1536,628,512,314;0,942,512,314;512,942,512,314;1024,942,512,314;1536,942,512,314` | `5712x219` | `8ff1dcf0f330e5a2a2d6678fce14b281d25c3a433631320c20269b6b0b7c59c9` |
| `images/boss-idle.webp` | `03_Client/Assets/Art/Enemy/Boss_Vampire/Boss_Idle.png` | `5986c073247f52e0b8e36993c74e261bb9187618d680817c58823a8bd94c6d04` | `0,0,512,370;512,0,512,370;1024,0,512,370;1536,0,512,370;0,370,512,370;512,370,512,370;1024,370,512,370;1536,370,512,370;0,740,512,370;512,740,512,370;1024,740,512,370;1536,740,512,370;0,1110,512,370;512,1110,512,370;1024,1110,512,370;1536,1110,512,370` | `4384x198` | `15b80a99ef04fca5137fcf67a1347d006fef87fcc37f0ba00f806a68b11d7a74` |
| `images/blacksmith-idle.webp` | `03_Client/Assets/Art/Characters/NPC/BlackSmith/BlackSmith_Idle.png` | `1f426c4c33f1b106c5d2597dc44749e17acfb24c1c272dede0021162fd4e6791` | `0,0,512,411;512,0,512,411;1024,0,512,411;1536,0,512,411;0,411,512,411;512,411,512,411;1024,411,512,411;1536,411,512,411;0,822,512,411;512,822,512,411;1024,822,512,411;1536,822,512,411;0,1233,512,411;512,1233,512,411;1024,1233,512,411;1536,1233,512,411` | `3648x183` | `94d04930e4984070de828fcb45290331fec014fe760b90d98a8e6c3865287de9` |
| `images/blacksmith-portrait.webp` | `03_Client/Assets/Art/Characters/NPC/BlackSmith/Portrait/blacksmith_base.png` | `dd9fdb2a9a0d2dfad8fbcabd77730e191b6c77735110409eb1f3afaea09a3282` | `0,0,1400,1123` | `700x562` | `2c0545e5d762344430e918de96a4db94500f14b60c2c43da6557f764cdd3dacd` |
| `images/icon.png` | `03_Client/Assets/Art/UI/Character_Icon/knight_icon.png` | `c3ec4a9fd4b05d8bae77541b1cd41d17399a476b2d7c81aaee2c9fb4d4c2bfea` | `0,0,1090,2048` | `34x64` | `baa9b48b40ebe0ceaf0b48161583c9c8f758822f75def9bb2addd3c0a140f7b0` |

모든 그림은 저장소에 이미 있는 그림의 웹용 사본이다. 원본과 `.meta`는 바꾸지 않았다. 확대한 사본은 없다. 사본 생성 스크립트는 저장소에 두지 않았다(goal 결정). 원본 일부에는 AI 생성 도구의 출처 표식이 있지만 사본에서는 사라질 수 있어서, 페이지 맨 아래의 「그림 일부는 AI 생성 도구를 활용해 만들었어요」 문장과 이 표가 그 표기를 대신한다.

자른 영역은 원본 왼쪽 위 원점의 `x,y,폭,높이` 픽셀이다. 칸이 여럿인 동작 띠는 원본 `.meta`의 칸 영역을 원본 순서대로 `;`로 이었다.

첫 판 사본(PNG·JPEG 6개)은 원본의 첫 칸을 잘라 투명 여백을 걷어 냈고, 배경만 폭 1600으로 줄여 JPEG(품질 85)로 저장했다. 적 그림의 첫 칸은 각 원본 `.meta`의 `*_Idle_0` 영역이고, 기사·마법사는 512px 칸 스트립의 첫 칸이다. 디자인 개정 페이지가 쓰지 않게 되면 목록과 이 표에서 뺀다.

디자인 개정 사본(WebP 21개와 `icon.png`)은 게임 화면의 크기 비율을 지키도록 게임 1단위가 사본 80px이 되게 줄였다. 원본마다 게임의 PPU(1단위당 픽셀)와 prefab 배율이 달라 축소율도 다르고, 그 결과는 「동작 배율과 기준점」 표에 있다. 동작 띠는 칸마다 원본 칸을 같은 비율로 줄여 칸 안 아래 가운데에 놓고 가로로 이었다. 원본 칸의 크기와 pivot 설정이 칸마다 같아서 기준점도 모든 칸에서 같은 자리다. 겹 배경은 원래 크기 그대로이고, 로고는 칸 폭 640px, 대장장이 초상은 폭 700px, 아이콘은 높이 64px로 줄였다. WebP는 cwebp 1.6.0으로 만들었고 손실(그림 품질 90, 배경 85)과 무손실 중 작은 쪽을 골랐다(무손실이 1.15배 이내면 무손실). 이번 사본은 모두 손실이 골라졌다.

## 글꼴 출처

| 사본 | 배포처 | 판 | 원본 파일 | 원본 SHA256 | 사본 SHA256 |
|---|---|---|---|---|---|
| `fonts/Pretendard-Regular.subset.woff2` | `npm:pretendard@1.3.9` | `1.3.9` | `dist/web/static/woff2-subset/Pretendard-Regular.subset.woff2` | `01dd73155fdfab7ce9b25224523e85a96927e21aef97f21957d41f1bfa7e3878` | `01dd73155fdfab7ce9b25224523e85a96927e21aef97f21957d41f1bfa7e3878` |
| `fonts/Pretendard-Bold.subset.woff2` | `npm:pretendard@1.3.9` | `1.3.9` | `dist/web/static/woff2-subset/Pretendard-Bold.subset.woff2` | `78eb71c33101ee7d4f8d1b777d193a12d00f8a296e712a8c417cf27abe946397` | `78eb71c33101ee7d4f8d1b777d193a12d00f8a296e712a8c417cf27abe946397` |
| `fonts/LICENSE.txt` | `npm:pretendard@1.3.9` | `1.3.9` | `dist/LICENSE.txt` | `b04538c9abec39a3db75108cf0af0fd9c77032fe8aa2cf38345b4d250e98e38e` | `b04538c9abec39a3db75108cf0af0fd9c77032fe8aa2cf38345b4d250e98e38e` |

글꼴은 Pretendard 공식 배포 파일을 바꾸지 않고 쓴다. 그래서 원본과 사본의 SHA256이 같다. 라이선스는 SIL Open Font License 1.1이고 전문을 `fonts/LICENSE.txt`로 함께 공개한다. 공식 부분 집합이라 모든 한글을 담지는 않는다(글자표 3,728자). 페이지 글자를 바꾸면 이 파일에 없는 글자가 생기지 않는지 다시 확인한다.

## 동작 배율과 기준점

| 사본 | 칸 수 | 칸 크기 | 사본 1px당 게임 단위 | 기준점 | 붙는 곳 | 재생 | 근거 |
|---|---|---|---|---|---|---|---|
| `images/layer-1-sky.webp` | `1` | `2172x724` | `0.0125` | `1086,362` | `-` | `정지` | `03_Client/Assets/Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/01_Sky_SunGlow.png.meta:54; 03_Client/Assets/Prefabs/Environment/Parallax_CastleValley.prefab:574` |
| `images/layer-2-clouds.webp` | `1` | `2172x724` | `0.0125` | `1086,362` | `-` | `정지` | `03_Client/Assets/Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/02_Far_Clouds.png.meta:66; 03_Client/Assets/Prefabs/Environment/Parallax_CastleValley.prefab:574` |
| `images/layer-3-far-mountains.webp` | `1` | `2172x724` | `0.0125` | `1086,362` | `-` | `정지` | `03_Client/Assets/Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/03_Distant_Mountains.png.meta:54; 03_Client/Assets/Prefabs/Environment/Parallax_CastleValley.prefab:574` |
| `images/layer-4-hills.webp` | `1` | `2172x724` | `0.0125` | `1086,362` | `-` | `정지` | `03_Client/Assets/Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/04_Mid_Mountains_Hills.png.meta:54; 03_Client/Assets/Prefabs/Environment/Parallax_CastleValley.prefab:574` |
| `images/layer-5-castle.webp` | `1` | `2172x724` | `0.0125` | `1086,362` | `-` | `정지` | `03_Client/Assets/Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/05_Castle_City.png.meta:54; 03_Client/Assets/Prefabs/Environment/Parallax_CastleValley.prefab:574` |
| `images/layer-6-valley.webp` | `1` | `2172x724` | `0.0125` | `1086,362` | `-` | `정지` | `03_Client/Assets/Art/Environment/BackGround/Parallax/CastleValley_Sunset_Parallax_Layers/Assets/Art/Backgrounds/CastleValley_Sunset/06_Valley_Fields_Ruins.png.meta:54; 03_Client/Assets/Prefabs/Environment/Parallax_CastleValley.prefab:574` |
| `images/logo.webp` | `16` | `640x265` | `-` | `320,132.5` | `-` | `반복 12` | `03_Client/Assets/Scenes/00.Menu/MainMenu.unity:528; 03_Client/Assets/Prefabs/UI/MainTitle_Clip.anim:60` |
| `images/knight-idle.webp` | `16` | `228x165` | `0.012476` | `114,165` | `-` | `반복 8.4` | `03_Client/Assets/Art/Characters/Playable/Knight/Knight_Idle.png.meta:56; 03_Client/Assets/Art/Characters/Playable/Knight/Animator/Knight.controller:218-219` |
| `images/knight-move.webp` | `16` | `228x165` | `0.012476` | `114,165` | `-` | `반복 27.6` | `03_Client/Assets/Art/Characters/Playable/Knight/Knight_Move.png.meta:56; 03_Client/Assets/Art/Characters/Playable/Knight/Animator/Knight.controller:454-455` |
| `images/knight-attack.webp` | `16` | `357x196` | `0.012506` | `178,196` | `-` | `한 번 24` | `03_Client/Assets/Art/Characters/Playable/Knight/Knight_Attack0.png.meta:56; 03_Client/Assets/Art/Characters/Playable/Knight/Animator/Knight.controller:10-11` |
| `images/knight-dash-effect.webp` | `9` | `480x224` | `0.0125` | `240,112` | `images/knight-idle.webp@1,0.9` | `한 번 21.6` | `03_Client/Assets/Art/Characters/Playable/Knight/Skill_Effect/Sprites/Dash/Knight_Dash_Skill_Effect.png.meta:96; 03_Client/Assets/Prefabs/Characters/ClassVisuals/KnightVisual.prefab:175; 03_Client/Assets/Scripts/Network/Handlers/Skill/SkillCastHandler.cs:153-158; 03_Client/Assets/Art/Characters/Playable/Knight/Skill_Effect/Animator/Dash/Knight_Dash_Skill_Effect.controller:56` |
| `images/mage-idle.webp` | `16` | `216x169` | `0.012476` | `108,169` | `-` | `반복 7.2` | `03_Client/Assets/Art/Characters/Playable/Mage/Mage_Idle.png.meta:56; 03_Client/Assets/Art/Characters/Playable/Mage/Animator/Mage.controller:59-60` |
| `images/mage-move.webp` | `16` | `216x169` | `0.012476` | `108,169` | `-` | `반복 31.8` | `03_Client/Assets/Art/Characters/Playable/Mage/Mage_Move.png.meta:56; 03_Client/Assets/Art/Characters/Playable/Mage/Animator/Mage.controller:287-288` |
| `images/mage-cast.webp` | `16` | `265x232` | `0.012512` | `132,232` | `-` | `한 번 16.8` | `03_Client/Assets/Art/Characters/Playable/Mage/Mage_Cast_Channeling.png.meta:56; 03_Client/Assets/Art/Characters/Playable/Mage/Animator/Mage.controller:547-548` |
| `images/mage-thunder-effect.webp` | `10` | `162x316` | `0.012516` | `80.5,261.2` | `images/slime-idle.webp@0,0` | `한 번 12` | `03_Client/Assets/Art/Characters/Playable/Mage/Skill_Effect/Sprites/ThunderVolt/Mage_ThunderVolt.png.meta:56; 03_Client/Assets/Resources/Effects/LightningStrike.prefab:32; 03_Client/Assets/Scripts/Network/Handlers/Combat/HitResultHandler.cs:76-90` |
| `images/mage-teleport-effect.webp` | `5` | `151x228` | `0.012483` | `75.5,217.1` | `images/mage-idle.webp@0,-0.5` | `한 번 12` | `03_Client/Assets/Art/Characters/Playable/Mage/Skill_Effect/Sprites/Teleport/Mage_Teleport.png.meta:104; 03_Client/Assets/Scripts/Network/Handlers/Skill/SkillCastHandler.cs:34` |
| `images/slime-idle.webp` | `16` | `164x119` | `0.012488` | `82,119` | `-` | `반복 12` | `03_Client/Assets/Art/Enemy/Slime/Slime_Idle.png.meta:56; 03_Client/Assets/Art/Enemy/Slime/Animator/Slime.controller:327-328` |
| `images/golem-idle.webp` | `16` | `357x219` | `0.012506` | `178,219` | `-` | `반복 4.8` | `03_Client/Assets/Art/Enemy/Golem/Golem_Idle.png.meta:56; 03_Client/Assets/Art/Enemy/Golem/Animator/Golem.controller:54-55` |
| `images/boss-idle.webp` | `16` | `274x198` | `0.012503` | `136.5,198` | `-` | `반복 8.4` | `03_Client/Assets/Art/Enemy/Boss_Vampire/Boss_Idle.png.meta:56; 03_Client/Assets/Art/Enemy/Boss_Vampire/Animator/Boss_Animator.controller:229-230` |
| `images/blacksmith-idle.webp` | `16` | `228x183` | `0.012476` | `114,183` | `-` | `반복 6` | `03_Client/Assets/Art/Characters/NPC/BlackSmith/BlackSmith_Idle.png.meta:101; 03_Client/Assets/Art/Characters/NPC/BlackSmith/Animator/BlackSmith.controller:55-56` |

사본 1px당 게임 단위가 모두 0.0125 근처인 것은 게임 1단위를 사본 80px로 맞췄기 때문이다. 정수 픽셀로 반올림한 칸 크기 때문에 끝자리가 조금씩 다르다. 그래서 같은 장면에서 같은 배율로 그리면 게임 화면과 같은 크기 비율이 된다. 로고는 메뉴 화면 UI라 게임 단위가 없다. 재생 속도는 게임 클립의 12fps에 애니메이터 상태 속도 배수를 곱한 값이다. 붙는 곳의 대시 효과 (1, 0.9)는 기사 몸의 효과 기준점이고, 낙뢰는 적의 발밑, 순간이동 도착 효과는 마법사 발밑에서 0.5단위 아래다.

## 동작 맞물림

| 동작 | 단계 | 시작 ms | 끝 ms | 캐릭터 사본 | 캐릭터 칸 | 캐릭터 위치 | 방향 | 효과 사본 | 효과 칸 | 효과 위치 | 효과 방향 | 근거 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `knight-dash` | `1` | `0` | `400` | `images/knight-attack.webp` | `0-9` | `0→4` | `오른쪽` | `images/knight-dash-effect.webp` | `0-8` | `1,0.9` | `캐릭터` | `03_Client/Assets/Scripts/Prediction/PlayerAbilityTimers.cs:116-121; 98_Shared/GameData/Constants.cs:97; 98_Shared/GameData/Constants.cs:101; 03_Client/Assets/Scripts/Network/Handlers/Skill/SkillCastHandler.cs:153-158` |
| `knight-dash` | `2` | `400` | `767` | `images/knight-idle.webp` | `0-3` | `4` | `오른쪽` | `images/knight-dash-effect.webp` | `8-8` | `1,0.9` | `캐릭터` | `03_Client/Assets/Resources/Effects/DashSkill.prefab:130` |
| `knight-dash` | `3` | `767` | `1000` | `images/knight-idle.webp` | `3-5` | `4` | `오른쪽` | `-` | `-` | `-` | `-` | `03_Client/Assets/Resources/Effects/DashSkill.prefab:130` |
| `knight-dash` | `4` | `1000` | `2000` | `images/knight-move.webp` | `반복` | `4→0` | `왼쪽` | `-` | `-` | `-` | `-` | `98_Shared/GameData/Combat/PlayerStats.cs:41` |
| `mage-thunder` | `1` | `0` | `200` | `images/mage-cast.webp` | `0-3` | `0` | `오른쪽` | `-` | `-` | `-` | `-` | `03_Client/Assets/Scripts/Prediction/PlayerAbilityTimers.cs:108-113; 98_Shared/GameData/Constants.cs:46` |
| `mage-thunder` | `2` | `200` | `400` | `images/mage-cast.webp` | `3-6` | `0` | `오른쪽` | `images/mage-thunder-effect.webp` | `0-2` | `대상` | `고정` | `02_Server/GameServer/Combat/CombatConstants.cs:84; 03_Client/Assets/Scripts/Network/Handlers/Combat/HitResultHandler.cs:76-90` |
| `mage-thunder` | `3` | `400` | `1033` | `images/mage-idle.webp` | `0-4` | `0` | `오른쪽` | `images/mage-thunder-effect.webp` | `2-9` | `대상` | `고정` | `98_Shared/GameData/Constants.cs:46; 03_Client/Assets/Resources/Effects/LightningStrike.prefab:130` |
| `mage-thunder` | `4` | `1033` | `1100` | `images/mage-idle.webp` | `4-5` | `0` | `오른쪽` | `images/mage-thunder-effect.webp` | `9-9` | `대상` | `고정` | `03_Client/Assets/Resources/Effects/LightningStrike.prefab:130` |
| `mage-teleport` | `1` | `0` | `417` | `images/mage-idle.webp` | `계속` | `3.5` | `오른쪽` | `images/mage-teleport-effect.webp` | `0-4` | `-3.5,-0.5` | `고정` | `03_Client/Assets/Scripts/Prediction/PlayerAbilityTimers.cs:124-127; 02_Server/GameServer/Combat/CombatConstants.cs:96; 02_Server/GameServer/Maps/Actions/TeleportAction.cs:47-56; 03_Client/Assets/Scripts/Network/Handlers/Skill/SkillCastHandler.cs:34; 03_Client/Assets/Scripts/Network/Handlers/Skill/SkillCastHandler.cs:177-181` |
| `mage-teleport` | `1` | `0` | `417` | `images/mage-idle.webp` | `계속` | `3.5` | `오른쪽` | `images/mage-teleport-effect.webp` | `0-4` | `0,-0.5` | `고정` | `03_Client/Assets/Scripts/Network/Handlers/Skill/SkillCastHandler.cs:224-230` |
| `mage-teleport` | `2` | `417` | `517` | `images/mage-idle.webp` | `계속` | `3.5` | `오른쪽` | `images/mage-teleport-effect.webp` | `4-4` | `-3.5,-0.5` | `고정` | `03_Client/Assets/Resources/Effects/TeleportDepart.prefab:130` |
| `mage-teleport` | `2` | `417` | `517` | `images/mage-idle.webp` | `계속` | `3.5` | `오른쪽` | `images/mage-teleport-effect.webp` | `4-4` | `0,-0.5` | `고정` | `03_Client/Assets/Resources/Effects/TeleportArrive.prefab:130` |
| `mage-teleport` | `3` | `517` | `1500` | `images/mage-idle.webp` | `계속` | `3.5` | `오른쪽` | `-` | `-` | `-` | `-` | `98_Shared/GameData/Constants.cs:76` |
| `mage-teleport` | `4` | `1500` | `1917` | `images/mage-idle.webp` | `계속` | `0` | `왼쪽` | `images/mage-teleport-effect.webp` | `0-4` | `-3.5,-0.5` | `고정` | `98_Shared/GameData/Constants.cs:76; 02_Server/GameServer/Maps/Actions/TeleportAction.cs:47-56` |
| `mage-teleport` | `4` | `1500` | `1917` | `images/mage-idle.webp` | `계속` | `0` | `왼쪽` | `images/mage-teleport-effect.webp` | `0-4` | `0,-0.5` | `고정` | `03_Client/Assets/Scripts/Network/Handlers/Skill/SkillCastHandler.cs:224-230` |
| `mage-teleport` | `5` | `1917` | `2017` | `images/mage-idle.webp` | `계속` | `0` | `왼쪽` | `images/mage-teleport-effect.webp` | `4-4` | `-3.5,-0.5` | `고정` | `03_Client/Assets/Resources/Effects/TeleportDepart.prefab:130` |
| `mage-teleport` | `5` | `1917` | `2017` | `images/mage-idle.webp` | `계속` | `0` | `왼쪽` | `images/mage-teleport-effect.webp` | `4-4` | `0,-0.5` | `고정` | `03_Client/Assets/Resources/Effects/TeleportArrive.prefab:130` |

시각은 게임 서버의 틱(초당 20번, 한 틱 50ms)과 효과 수명 값에서 계산했다. 게임에는 애니메이션 이벤트가 없어서 캐릭터 상태와 효과는 코드가 정한 순간에 바뀐다. 효과가 서버 왕복만큼 늦게 나오는 지연은 페이지에서 0으로 둔다(로컬 플레이에서 한 칸 미만).

- **기사 대시:** 게임에는 대시 전용 동작이 없고 기사는 공격 동작으로 400ms 동안 초당 10단위를 달려 4단위를 간다. 공격 시트는 둘 중 하나를 무작위로 쓰는데 페이지는 첫 번째를 쓴다. 대시 효과는 기사 몸에 붙어 같이 움직이고, 마지막 칸을 767ms까지 유지한 뒤 사라진다.
- **마법사 낙뢰:** 시전 동작 200ms 뒤 사거리 안의 적마다 발밑에 하나씩 떨어진다. 사거리 안에 적이 없으면 낙뢰가 없으므로 장면에 적을 둔다. 낙뢰는 마법사 방향과 관계없이 뒤집지 않는다.
- **마법사 순간이동:** 마법사 동작은 바뀌지 않고, 보는 방향으로 3.5단위를 바로 옮겨 간다. 떠난 자리 효과는 그 자리에 남고 도착 효과는 마법사에 붙는다. 같은 단계의 두 효과는 효과 위치로 구별한다. 둘 다 뒤집지 않는다.
- **페이지에서 더한 단계:** 기사는 대시 효과가 사라진 뒤 233ms 쉬고 걸어서(초당 4단위) 제자리로 돌아온다. 마법사는 순간이동 재사용 대기 1500ms 뒤 왼쪽을 보고 한 번 더 순간이동해 돌아온다. 둘 다 게임에서 할 수 있는 동작이다. 마지막 단계 뒤에는 오른쪽을 보고 대기 동작으로 돌아간다.

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
