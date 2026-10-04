# 제3자 소프트웨어·글꼴과 그림 출처

이 문서는 Management 제품에 포함하는 구성요소의 원래 저작권과 라이선스 고지를 보존합니다. 현재 대표 도식 첫 통합 단계로, 소프트웨어의 최종 포함 목록은 빌드 그래프 대조 후 추가할 예정입니다. 이 작성 중 상태를 배포용 고지 완료로 사용하지 않습니다.

## Galmuri11 Bold

- 제작자: Lee Minseo (quiple).
- 배포: Galmuri v2.40.4, `Galmuri11-Bold.woff2`.
- 원천: [공식 릴리스](https://github.com/quiple/galmuri/releases/tag/v2.40.4), commit `bdb86ae89466a361eb8df861222736b81ff975ef`.
- 파일: 166,632 bytes, SHA-256 `8643094f395aa2dbad6bc4385cc043314801eaec2e759d549435ec8ac4f2d078`.
- 라이선스: SIL Open Font License 1.1. 아래는 동봉 고지 원문입니다.
- 원천 기록: [source.json](goals/2026-10-02-system-cards/assets/fonts/source.json), [원문](goals/2026-10-02-system-cards/assets/fonts/OFL-1.1.txt).
Copyright &copy; 2019–2025 Lee Minseo (quiple@quiple.dev)

This font software is licensed under the SIL Open Font License, Version 1.1.
This license is copied below, and is also available with a FAQ at:
<https://openfontlicense.org>

# SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007

## PREAMBLE

The goals of the Open Font License (OFL) are to stimulate worldwide
development of collaborative font projects, to support the font creation
efforts of academic and linguistic communities, and to provide a free and
open framework in which fonts may be shared and improved in partnership
with others.

The OFL allows the licensed fonts to be used, studied, modified and
redistributed freely as long as they are not sold by themselves. The
fonts, including any derivative works, can be bundled, embedded,
redistributed and/or sold with any software provided that any reserved
names are not used by derivative works. The fonts and derivatives,
however, cannot be released under any other type of license. The
requirement for fonts to remain under this license does not apply
to any document created using the fonts or their derivatives.

## DEFINITIONS

"Font Software" refers to the set of files released by the Copyright
Holder(s) under this license and clearly marked as such. This may
include source files, build scripts and documentation.

"Reserved Font Name" refers to any names specified as such after the
copyright statement(s).

"Original Version" refers to the collection of Font Software components as
distributed by the Copyright Holder(s).

"Modified Version" refers to any derivative made by adding to, deleting,
or substituting -- in part or in whole -- any of the components of the
Original Version, by changing formats or by porting the Font Software to a
new environment.

"Author" refers to any designer, engineer, programmer, technical
writer or other person who contributed to the Font Software.

## PERMISSION & CONDITIONS

Permission is hereby granted, free of charge, to any person obtaining
a copy of the Font Software, to use, study, copy, merge, embed, modify,
redistribute, and sell modified and unmodified copies of the Font
Software, subject to the following conditions:

1. Neither the Font Software nor any of its individual components,
in Original or Modified Versions, may be sold by itself.

2. Original or Modified Versions of the Font Software may be bundled,
redistributed and/or sold with any software, provided that each copy
contains the above copyright notice and this license. These can be
included either as stand-alone text files, human-readable headers or
in the appropriate machine-readable metadata fields within text or
binary files as long as those fields can be easily viewed by the user.

3. No Modified Version of the Font Software may use the Reserved Font
Name(s) unless explicit written permission is granted by the corresponding
Copyright Holder. This restriction only applies to the primary font name as
presented to the users.

4. The name(s) of the Copyright Holder(s) or the Author(s) of the Font
Software shall not be used to promote, endorse or advertise any
Modified Version, except to acknowledge the contribution(s) of the
Copyright Holder(s) and the Author(s) or with their explicit written
permission.

5. The Font Software, modified or unmodified, in part or in whole,
must be distributed entirely under this license, and must not be
distributed under any other license. The requirement for fonts to
remain under this license does not apply to any document created
using the Font Software.

## TERMINATION

This license becomes null and void if any of the above conditions are
not met.

## DISCLAIMER

THE FONT SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
OF COPYRIGHT, PATENT, TRADEMARK, OR OTHER RIGHT. IN NO EVENT SHALL THE
COPYRIGHT HOLDER BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
INCLUDING ANY GENERAL, SPECIAL, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL
DAMAGES, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF THE USE OR INABILITY TO USE THE FONT SOFTWARE OR FROM
OTHER DEALINGS IN THE FONT SOFTWARE.

## 프로젝트 생성 그림

엠블럼 7종(`emblem-*.png`), 상태 도장 4종(`stamp-*.png`), 코르크·나무 질감 2종, 장부 머리 그림 1종은 2026-10-02 Codex 내장 GPT-Image로 생성하고 프로젝트에서 픽셀 격자·팔레트·크기를 가공한 자산입니다. 원래 게임의 아트·UI 파일을 복사한 것이 아닙니다. 화면에 노출되지 않은 생성 백엔드 모델은 `unknown`으로 기록했습니다. 이 출처 설명을 새 오픈소스 라이선스 부여로 해석하지 않습니다.

최종 14개 파일은 합계 13,586 bytes입니다. 클라이언트 전송 엠블럼은 비평 후 정사각형에 가까운 구도로 교체했습니다. prompt·생성 관측 시각·파일별 SHA-256·이전 판은 [생성 manifest](goals/2026-10-02-system-cards/assets/generation-manifest.json), 가공·표시 범위는 [자산 명세](goals/2026-10-02-system-cards/asset-spec.md)에 보존합니다.

## 도식 소프트웨어 — 최종 빌드 대조 대기

Mermaid 12.0.0의 ESM core를 빌드 입력으로 사용하며, 실제 포함 패키지 목록과 원래 LICENSE/NOTICE 전문을 빌드 결과에 맞춰 이 절에 추가합니다. 설치 목록과 제품 포함 목록은 다르므로 node_modules 전체를 제품에 포함했다고 표시하지 않습니다.
