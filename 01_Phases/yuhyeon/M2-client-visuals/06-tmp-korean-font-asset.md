# Phase 06: TMP 한글 Font Asset 생성 + Fallback

> **상태**: pending
> **마일스톤**: M2-client-visuals
> **예상 소요**: 1.5~2시간
> **담당 에이전트**: client

---

## 🎯 목표

무료 한글 폰트 1개(SIL OFL 또는 동등 라이선스)를 받아 TMP Font Asset으로 만들고, TMP Settings의 Default Font Asset 또는 Fallback 체인에 박아 모든 TMP_Text에서 한글이 정상 렌더되게 한다.

**끝나면 데모 가능한 것**: 임의의 TMP_Text에 "안녕하세요 한글 테스트" 입력 시 깨진 사각형(tofu) 없이 또렷이 보임.

---

## ⏪ 사전 조건

- [ ] M1 Phase 03 HUD 셋업 완료 (TMP_Text 사용 중)
- [ ] TextMeshPro 패키지 활성 (M1에서 이미 설치된 상태)
- [ ] 한글 무료 폰트 1개 선정 — 권장:
  - **Pretendard** (OFL, https://github.com/orioncactus/pretendard) — 깔끔한 모던
  - **Noto Sans KR** (OFL, Google Fonts) — 무난한 본문
  - **Nanum Gothic** (OFL, Naver) — 한국에서 친숙

---

## 📝 작업 내용

- [ ] **폴더 신설**: `03_Client/Assets/Fonts/`
- [ ] **폰트 파일 임포트**: `.ttf` 또는 `.otf`를 위 폴더에 드래그 (Pretendard-Regular.otf 같이 1개)
- [ ] **Font Asset Creator 열기**: Window → TextMeshPro → Font Asset Creator
- [ ] **설정**:
  - Source Font File = 임포트한 폰트
  - Sampling Point Size = **Auto Sizing** (또는 32)
  - Padding = 5 (SDF 외곽선 여유)
  - Atlas Resolution = **2048×2048** (한글은 음절 11,172자라 1024로 부족할 수 있음)
  - Character Set 정적/동적 결정 ↓
- [ ] **Character Set 결정 (학습 trade-off)**:
  - **Static (정적)**: Custom Characters → "Range" 옵션 → 한글 음절 범위 `0xAC00-0xD7A3` + 영문 `0x20-0x7E` + 한글 자모 `0x3131-0x318E`. 메모리 큼(~10MB+), runtime 안정.
  - **Dynamic (동적)**: Atlas Population Mode = Dynamic. runtime에서 필요할 때 생성. 초기 메모리 작음, 처음 만나는 글자 0.X초 hitch.
  - **권장**: 학부생 학습 단계 + 한국어 단일 언어 = **Dynamic** (Pretendard.otf 자체를 폰트로 깔고 동적 atlas)
- [ ] **Generate Font Atlas** → "Save" 또는 "Save as..." → `Assets/Fonts/Pretendard-Regular SDF.asset`
- [ ] **TMP Settings 갱신**: Project → Assets/TextMesh Pro/Resources/TMP Settings.asset 선택 → Inspector
  - **Default Font Asset** = 방금 만든 Pretendard SDF (또는 기존 LiberationSans 유지 + Fallback에 한글 추가)
  - **Fallback Font Assets** 리스트에 위 Asset 추가 (Default 안 바꾸는 경우)
- [ ] **시각 검증**: 임의 GameObject에 TMP_Text + "안녕하세요 ABC 123" 입력 → 모두 또렷이 보임

---

## ✅ 완료 조건

- [ ] `Assets/Fonts/{폰트}.otf` + `Assets/Fonts/{폰트} SDF.asset` + `.meta` 모두 git 추적
- [ ] 폰트 라이선스 파일(`LICENSE.txt` 또는 `OFL.txt`) 동봉 — `Assets/Fonts/` 안에 같이 commit
- [ ] TMP Settings의 Default Font Asset 또는 Fallback 리스트에 한글 폰트 박힘
- [ ] 새 TMP_Text에 "안녕하세요 한글" 입력 시 깨진 사각형(tofu) 0개

---

## 🧪 테스트

**수동 테스트:**
1. 빈 Canvas + TMP_Text 신설 → "안녕하세요" 입력 → 한글 정상 표시
2. 동적 Atlas인 경우 처음 보는 글자("이런 글자도 다 됨") 입력 → 0.1~0.5초 후 추가 렌더
3. 매우 큰 폰트 사이즈(72) → SDF 덕에 안 깨짐
4. Scene 저장 + 재로드 → 폰트 그대로

**자동 테스트:** 없음

---

## 📚 학습 포인트

- **TextMeshPro (TMP)**: Unity 표준 텍스트 렌더러. 옛 UI.Text의 후속. SDF rendering 덕에 크기 변경 시 안 깨지고 외곽선·그림자도 무료.
- **SDF (Signed Distance Field) Rendering**: 각 픽셀이 *글자 경계까지 거리*를 저장. 셰이더가 그 거리값으로 가장자리 그리기 → 크기 변경에 깨짐 없음. 옛 비트맵 폰트 대비 큰 발전.
- **Font Atlas**: 한 텍스처에 모든 글리프(글자 모양) 모아둔 것. GPU가 텍스처 1장만 sample 하면 됨 (draw call 효율).
- **Atlas Population Mode**:
  - **Static** — 미리 정해진 글자 집합만. 메모리 예측 가능.
  - **Dynamic** — runtime에 필요한 글자 추가. 메모리 최소 시작 + 첫 만남 hitch.
- **Character Set 범위 (Unicode)**:
  - 한글 음절(완성형) `0xAC00 - 0xD7A3` (11,172자)
  - 한글 자모(조합용) `0x3131 - 0x318E`
  - 기본 라틴 `0x20 - 0x7E`
- **Fallback Chain**: TMP가 한 글자 그릴 때 Font Asset에 없으면 Fallback 리스트를 순회. 다국어/이모지 처리에 표준 패턴.
- **SIL OFL (Open Font License)**: 폰트 라이선스 표준. 자유 사용·임베드·번들 OK, 단 폰트 자체를 *팔* 수 없음. 학습/게임 임베드 안전.
- **Sampling Point Size**: SDF 만들 때 원본 폰트 렌더 해상도. Auto Sizing = TMP가 알아서. 너무 크면 atlas 메모리 폭증, 너무 작으면 큰 텍스트가 흐림. Auto가 보통 합리적.

---

## ⚠️ 함정 / 주의사항

- **Atlas Resolution 너무 작음**: 1024×1024로 한글 정적 11,172자 박으면 generation 실패 ("Atlas full"). 2048 또는 동적 권장.
- **TMP Settings 안 바꾸기**: Default Font Asset 그대로 두면 새 TMP_Text는 기존 LiberationSans만 → 한글이 tofu. Default를 한글 폰트로 바꾸거나 Fallback 박아야 함.
- **Fallback 순서**: 위에서 아래로 검색. 자주 쓰는 한글을 *위*에 두면 lookup 빠름.
- **폰트 라이선스 미동봉**: OFL은 *임베드 OK + LICENSE 동봉 의무*. `Assets/Fonts/`에 OFL.txt 같이 commit. 안 하면 라이선스 위반.
- **한자 / 일본어 / 특수 기호**: 한글 폰트엔 보통 없음 → Fallback에 Noto Sans CJK 같은 거 추가 필요. M2 범위는 한글 + 영문만.
- **동적 Atlas의 hitch**: 첫 만남 0.1~0.5초 지연. 메뉴 떴을 때 처음 보는 글자가 한 박자 늦게 그려지는 현상. 면담 데모에 신경 쓰이면 자주 쓰는 글자 정적으로 미리 박기.
- **SDF Bold/Italic은 별개 Font Asset**: Pretendard Bold는 Regular과 다른 SDF 만들어야 함. 학습 단계는 Regular 1개로 충분.

---

## ➡️ 다음 Phase

- **Phase 07 — 메뉴 + HUD 한글화**: 만들어 둔 한글 폰트로 모든 영문 텍스트 한글 교체.

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
