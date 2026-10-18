# HUD 그림 출처와 조립

승인된 브라운 B1 공통 조각을 기존 HUD의 5개 PNG에 합성했다. 원천은 인접 [Components 사용 안내](../Components/README.md)와 [import-spec.json](../Components/import-spec.json)의 `assetFile`·PNG SHA256이다. 새 원화·아이콘·색상 변환 없이 크롭, 9-slice, 등비 확대와 alpha mask만 사용한다.

## sprite별 조합

| PNG / 기존 sprite | 승인 조각과 변환 |
|---|---|
| Minimap_Frame / 3_minimap_frame_0 | Panel_Default·Inset_Default의 9-slice 테두리, Header 이름 칸, Ornament_Corner 등비 축소·좌우/상하 반전. 원본 alpha를 상한으로 적용해 지도 구멍을 유지한다. |
| Status_Frame / 1_status_hud_frame_0 | Panel_Default 몸체, ItemSlot_Empty 초상 뒤판, Inset_Default 상태바 3칸, NumberBadge 등비 확대. 원본 낮은 alpha 영역과 초상/배지 위치를 유지한다. |
| Skill_Slot / 6_skill_slot_frame_0·1 | ItemSlot_Empty·ItemSlot_Selected를 각 atlas rect 안에서 독립 9-slice. 기존 내부 alpha와 두 조각 사이 여백을 유지한다. |
| Quest_Panel / 4_quest_panel_frame_0 | Panel_Default·Header, ItemSlot_Empty 3칸, Divider_Horizontal 4줄. 기존 위치에 조립하고 원본 alpha를 상한으로 사용한다. |
| Goldbar / Goldbar_0 | Header 몸체와 Inset_Default 안쪽을 각각 9-slice한다. 원 rect는 이미지 좌표 (1134,224), 2760×584 px다. |
| Goldbar / Goldbar_1 | PortraitFrame_128을 742×742 px로 등비 확대한다. 767×742 px rect에 좌12/우13 px 여백을 두고 링 개구부를 유지한다. 원형을 타원으로 늘리거나 새 재화 아이콘을 그리지 않는다. |

## 단위와 보존

조립 좌표·테두리·여백의 단위는 PNG 픽셀이다. Components의 PPU200·2배 밀도는 원천 명세이며, 이 HUD의 기존 PPU100·canvas 크기·sprite rect/name/internalID/spriteID·GUID·pivot·border·mesh와 모든 기존 meta bytes는 유지한다. 이미지 top-left 좌표와 Unity atlas bottom-left 좌표는 구분한다. 코드·씬·런타임 크기·입력 영역은 이 그림에서 변경하지 않는다.

기존 둥근 외곽의 투명 영역을 유지하기 위해 몸체는 rect 안쪽 1px에 배치하고 원천 9-slice 모서리를 확대했다(Minimap 2배, Status 2.5배, Slot 3배, Quest 2배). 원본 RGB는 사용하지 않는다. 처음 네 PNG는 원본 alpha보다 불투명해지지 않으며, Goldbar는 기존 두 rect 밖의 불투명 체크 배경을 투명하게 정리하고 승인 조각의 alpha를 사용한다. Divider는 원본 12px 높이에서 조립본 4px로 축소했고 가로 몸체를 늘렸다. 이는 Components 런타임 Image 설정 변경이 아니다.

## 사용한 조각의 고정 출처

| Components PNG | SHA256 |
|---|---|
| Divider_Horizontal.png | c81aed6fd72542080814a87c2189df9d39523ef4b59b67b5fc466a0e6a11d8b9 |
| Header.png | b102ddcb312d5471b7117a9dab26ab74a5e10091332ba04f16578192a9894cb8 |
| Inset_Default.png | e6c0507eb16efd633011b73eb10ef743b100f27630e47998f3e040686ad02220 |
| ItemSlot_Empty.png | 55de5d67d500ab866e63ee71fe2616818f2293c75e626ffa53b7cc61ffec3a93 |
| ItemSlot_Selected.png | 20e4fca9f37f43029d827e31ff1737600e23cc30a7bcb8f6e85eb582f87269b5 |
| NumberBadge.png | fc01fb893835e3a9d6bb17efd246e24d7f75e035be4f637fe4315e549b201a5e |
| Ornament_Corner.png | 1ec4f967578031ff41ed3faffc52987ab494db32f613a40ca94abb8984598ec8 |
| Panel_Default.png | 0e91be5fc7d15467fc9cf1b2c98fe621a4dacc255fc6f107cd81f96158a3e167 |
| PortraitFrame_128.png | 9b60f36a348cfd88a62508bcfa0f426a39591b2e3c23ecb4d2bf1582db23870a |

실제 PNG의 크기·RGBA·atlas 경계·투명 영역과 source hash를 자체 대조했다. Unity import·실제 게임 표시/클릭·현재 화면 촬영·독립 검증·사용자 미관 승인은 별도 후행 확인 대상이다.
