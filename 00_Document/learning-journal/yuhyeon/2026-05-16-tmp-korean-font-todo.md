# 학습 대기: TMP 한글 폰트 도입 (2026-05-16)

> **작성일**: 2026-05-16
> **work-id**: phase02-main-menu-buttons (트리거 사건)
> **상태**: 🟡 stub — 사건 발생 시점 박제. 본격 해결은 별 Phase로 분리 (헌법 원칙: scope 늘면 새 Phase).
> **소요 시간 추정**: 30~45분 (한글 도입 작업) + 회고 30분 (이 파일을 troubleshoot 일지로 확장)

이 파일은 "Phase 02 진행 중 발생한 한글 폰트 사건 + 향후 해결 계획 + 학습 자료 stub"입니다.

---

## 사건 요약

**증상 (한 줄)**: Phase 02에서 StartButton/QuitButton에 `시작` / `종료` 텍스트 입력 → Game 뷰에 `□□` (빈 박스) 표시 + Console에 폰트 누락 경고 다수.

**Console 경고**:
```
The character with Unicode value 작 was not found in [LiberationSans SDF]   ← "작"
The character with Unicode value 종 was not found in [LiberationSans SDF]   ← "종"
The character with Unicode value 료 was not found in [LiberationSans SDF]   ← "료"
```

**진단**:
- TMP(TextMeshPro)는 SDF (Signed Distance Field) 기반 폰트 렌더링
- Phase 01에서 자동 임포트된 TMP Essentials의 기본 폰트 = LiberationSans SDF (Latin 글리프만)
- 한글 글리프 미포함 → fallback 폰트도 없음 → `.notdef` glyph (□) 표시

**임시 해결** (Phase 02 진행 우선):
- 버튼 텍스트 영문 변경 (`시작` → `Start`, `종료` → `Quit`)
- Welcome 텍스트는 이미 영문 ("Dawnholder — Welcome") 그대로 유지

**진짜 해결 (향후 별 Phase)**:
- 한글 .ttf 다운 → Unity 임포트 → TMP Font Asset Creator로 SDF Atlas 생성 → Text (TMP) 폰트 슬롯 교체
- 적합 후보: Google Noto Sans CJK Korean (무료, OFL 라이선스)
- 한글 범위: `AC00-D7A3` (가~힣 11,172자) + 기본 ASCII

---

## 학습 후보 키워드 (검색용)

- **TextMeshPro Font Asset** — SDF 폰트 구조, Atlas Generation
- **Unicode Range Hex** — 한글 범위 `AC00-D7A3`, 한자 `4E00-9FFF` 등
- **Font Fallback** — TMP의 fallback 폰트 체인 (한글 → 영문 → emoji 순차 시도)
- **SDF (Signed Distance Field)** — 벡터 폰트를 텍스처로 미리 렌더링하는 기법, 확대/효과에 강함
- **.notdef glyph** — 폰트에 없는 문자가 표시되는 박스 (□) 기호
- **OFL (Open Font License)** — 무료 폰트 라이선스 (Google Fonts 다수 이 라이선스)

## STAR 박제 후보 (면접 무기)

- **S**: Phase 02 메뉴 버튼 만들면서 한글 텍스트 깨짐 발견
- **T**: Phase 02 scope 안 늘리면서 진행 + 향후 영구 해결 계획 박제
- **A**: 임시 영문화로 Phase 02 진행 + 학습 일지 stub으로 분리 (헌법 "scope 늘면 새 Phase" 원칙 적용)
- **R**: Phase 02 완료 + 한글 폰트 도입은 별 작업으로 큐잉. 면접 답: "초보 단계에서 흔히 '지금 다 해버리자' 함정에 빠지는데 scope 분리로 마일스톤 페이스 유지"

## 향후 작업

- [ ] 별 Phase 또는 작은 PR로 한글 TMP 폰트 도입 (M1 마감 후 또는 M2 어딘가)
- [ ] 도입 완료 시 본 파일을 `troubleshoots/2026-05-16-tmp-korean-font.md`로 확장 (`/journal:bug`)
- [ ] 임시 영문 텍스트 → 한글로 복원 ("Start" → "시작", "Quit" → "종료")

---

## 참고 — TMP Font Asset 생성 절차 (미리 박제)

```
1. 한글 .ttf 다운 (예: NotoSansKR-Regular.ttf)
2. Unity → Assets/Fonts/ 폴더에 임포트 (드래그)
3. Window → TextMeshPro → Font Asset Creator
4. 다음 입력:
   - Source Font File: NotoSansKR-Regular
   - Sampling Point Size: Auto Sizing
   - Padding: 5
   - Packing Method: Optimum
   - Atlas Resolution: 4096 x 4096 (한글 11,172자 들어가려면 큼)
   - Character Set: Unicode Range (Hex)
   - Character Sequence (Hex): 0020-007E,AC00-D7A3
     (0020-007E = 기본 ASCII / AC00-D7A3 = 한글 가~힣)
5. Generate Font Atlas → 몇 분 소요
6. Save → NotoSansKR SDF.asset 생성
7. MainMenu 씬 → Text (TMP) 컴포넌트 → Font Asset 슬롯에 새 Asset 드래그
8. Console 경고 사라짐 + 한글 텍스트 정상 표시 확인
```

Atlas 4K → 빌드 사이즈 영향 (~10MB 추가). 운영 시점에 *실제 사용 글자만* 추리는 Dynamic SDF 모드도 있음 (학습 후 검토).
