# 밝은 UI 조각

인벤토리와 캐릭터 선택 두 창만 쓰는 밝은 양피지 조각 25장이다. 다른 창은 옆 폴더 `Components`의 브라운 조각을 쓴다.
두 묶음을 한 창 안에서 섞지 않는다.

## 출처

- 크기·border·importer 값: 승인된 kit-spec(SHA256 `d89cf84f8eee0f153dad38eaa0a4b4417188dc602b2a620ff7f0e61eacdd97f4`)
- GUID: 미리 정한 표(SHA256 `81c4029169dcf3ea21c20eca297e60ef330832769cdc92abda7b313cf8531304`)
- PNG는 kit-spec의 SHA256과 같은 바이트를 그대로 옮겼다. 그림을 고치려면 kit를 새로 승인받고 다시 반입한다.

## 쓰는 법

- 모든 PNG는 Pixels Per Unit 200, UI 1 단위당 PNG 2 px이다. 창 Canvas의 reference PPU 100에서 multiplier 1로 둔다.
- 9-slice(Image.type Sliced): Panel_Default, Header, Inset_Default, Divider_Horizontal, Button_Default_*, Button_Accept_*,
  ItemSlot_*, CharacterSlot_*, InputField. border보다 작게 줄이지 않는다. Divider_Horizontal은 높이 6 UI로 고정한다.
- 그대로 그림(Image.type Simple, preserveAspect): IconBase_64, NumberBadge, Pedestal, SelectionGlow, Icon_Material,
  Icon_CurrencyPouch, Icon_Coin.
- 색을 입히지 않는다(흰색). 버튼 그림만 raycast를 받고 장식은 받지 않는다.
- 창 조립은 `Resources/UI/BrightComponents/UiWindowSkin.asset`과 `UiWindowAppearance.BrightSkin`을 거친다.
