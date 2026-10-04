# 제3자 소프트웨어·글꼴과 그림 출처

이 문서는 아래 Management 도식·UI 빌드 구성에 연결된 원래 저작권과 라이선스 고지를 보존합니다. 구성이나 의존성을 바꿀 때 실제 빌드 출력과 내부 포함 코드를 대조해 함께 갱신합니다.

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

## 소프트웨어 구성과 원문

기준 도식 산출물은 `diagram-renderer.js` 2,539,114 bytes, SHA-256 `401a923cd87e6db2dc8a3d5e07e882e6acf68022309f1513731957c0d978a95c`입니다. 도식 output module의 패키지 소유 45개와 별도 UI 3개, 출력에 나타나는 생성 helper 2개를 아래에 나눴습니다. 패키지 소유 목록은 각 패키지의 모든 함수가 남았다는 뜻이 아닙니다. 이미 묶여 배포된 내부 구성은 다음 절에 따로 기록합니다.

구성 대조 graph SHA-256: 도식 `36864b6ecc3e1943cc7dd856fde30856d3739555484d284ed1023e0bc17d3daf`, UI `b6cc9057d5472ab0785e8c1e2461f9144c8eb2a96fa9f65897f26536dc06bfd0`. 원래 설치 파일은 `frontend/`를 기준으로 표기하며, 고지 전문은 이 문서 안에 포함합니다.

| 구성요소 | 배포 버전 | 출력 경계 | 라이선스와 고지 범위 | 원문 |
|---|---|---|---|---|
| mermaid | 12.0.0 | 도식 | MIT | [LICENSE](#notice-ec9fb67dcb25eccc) |
| dayjs | 1.11.23 | 도식 | MIT | [LICENSE](#notice-5faab7526d055651) |
| khroma | 2.1.0 | 도식 | MIT — 동봉 license 원문 기준 | [license](#notice-66b333b0f66759a0) |
| dompurify | 3.4.16 | 도식 | Apache-2.0 선택; 원래 이중 라이선스 표기는 아래 보존 | [LICENSE](#notice-cfc7749b96f63bd3) |
| katex | 0.16.47 | 도식 | MIT | [LICENSE](#notice-766ccc1f306c885a) |
| d3-array | 3.2.4 | 도식 | ISC | [LICENSE](#notice-3e6849627f74ff73) |
| d3-axis | 3.0.0 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-dispatch | 3.0.1 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-selection | 3.0.0 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-drag | 3.0.0 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-color | 3.1.0 | 도식 | ISC | [LICENSE](#notice-faa682e3e430941f) |
| d3-interpolate | 3.0.1 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-timer | 3.0.1 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-transition | 3.0.1 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-ease | 3.0.1 | 도식 | BSD-3-Clause | [LICENSE](#notice-b989d9085148d21f) |
| d3-brush | 3.0.0 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-path | 3.1.0 | 도식 | ISC | [LICENSE](#notice-a09b0d6d76e768fc) |
| d3-chord | 3.0.1 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-contour | 4.0.2 | 도식 | ISC | [LICENSE](#notice-e51cba0e4fa1d8c0) |
| d3-delaunay | 6.0.4 | 도식 | ISC | [LICENSE](#notice-582c3022bd019423) |
| d3-dsv | 3.0.1 | 도식 | ISC | [LICENSE](#notice-7842a82012622e5a) |
| d3-fetch | 3.0.1 | 도식 | ISC | [LICENSE](#notice-ffdc17dc9040006b) |
| d3-quadtree | 3.0.1 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-force | 3.0.0 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-format | 3.1.2 | 도식 | ISC | [LICENSE](#notice-2a6d2d5f32ba0b75) |
| d3-geo | 3.1.1 | 도식 | ISC; 동봉 GeographicLib MIT 고지 | [LICENSE](#notice-3e3edc1224eec9c3) |
| d3-hierarchy | 3.1.2 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-polygon | 3.0.1 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-random | 3.0.1 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-time | 3.1.0 | 도식 | ISC | [LICENSE](#notice-faa682e3e430941f) |
| d3-time-format | 4.1.0 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-scale | 4.0.2 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3-scale-chromatic | 3.1.0 | 도식 | ISC; 동봉 ColorBrewer Apache-2.0 고지 | [LICENSE](#notice-0986d93944902cca) |
| d3-shape | 3.2.0 | 도식 | ISC | [LICENSE](#notice-faa682e3e430941f) |
| d3-zoom | 3.0.0 | 도식 | ISC | [LICENSE](#notice-e008c5e25a6be382) |
| d3 | 7.9.0 | 도식 | ISC | [LICENSE](#notice-3e6849627f74ff73) |
| @iconify/utils | 3.1.7 | 도식 | MIT | [license.txt](#notice-a86793ac023162ce) |
| @braintree/sanitize-url | 7.1.2 | 도식 | MIT | [LICENSE](#notice-0984740e0c3d725c) |
| es-toolkit | 1.52.0 | 도식 | MIT | [LICENSE](#notice-51f3674294d988d8), [NOTICE](#notice-11ee5e3bce9b0a4d) |
| marked | 16.4.2 | 도식 | MIT; 동봉 Markdown BSD-3-Clause 고지 | [LICENSE.md](#notice-8e3a3f82f59a6095) |
| ts-dedent | 2.3.0 | 도식 | MIT | [LICENSE](#notice-eff13095d9d762c3) |
| roughjs | 4.6.6 | 도식 | MIT | [LICENSE](#notice-dca9a392272606ac) |
| lodash-es | 4.18.1 | 도식 | MIT; 문서 prose의 예시 코드에 대한 CC0 구분 보존 | [LICENSE](#notice-f71e8ed126b46346) |
| dagre-d3-es | 7.0.14 | 도식 | MIT | [LICENSE.md](#notice-d01eb84ccce437db) |
| stylis | 4.4.0 | 도식 | MIT | [LICENSE](#notice-88c579c6ee44b411) |
| react | 19.3.0 | UI | MIT | [LICENSE](#notice-da6d3703ed11cbe4) |
| scheduler | 0.28.0 | UI | MIT | [LICENSE](#notice-da6d3703ed11cbe4) |
| react-dom | 19.3.0 | UI | MIT | [LICENSE](#notice-da6d3703ed11cbe4) |
| vite | 8.3.1 | 생성 helper | MIT | [LICENSE.md](#notice-444c5ec3bf7f798a) |
| rolldown | 1.2.11 | 생성 helper | MIT | [LICENSE](#notice-23ecfff35a5a2e80), [THIRD-PARTY-LICENSE](#notice-a877291d800ed436) |

GeographicLib·ColorBrewer·Markdown·es-toolkit의 Lodash 파생 고지는 해당 배포 원문과 함께 보존합니다. Lodash의 CC0 문구는 문서 prose 안 예시 코드에 대한 구분이며 실행 코드 전체의 라이선스로 바꾸지 않습니다. Vite는 출력의 modulepreload helper, Rolldown은 runtime helper에 연결하며 두 빌드 도구의 모든 의존성이 앱에 포함됐다는 뜻은 아닙니다.

### DOMPurify 선택과 실제 배포 header

DOMPurify 3.4.16의 package metadata는 MPL-2.0 또는 Apache-2.0 선택을 제공하며 이 제품에서는 Apache-2.0을 선택합니다. 실제 배포 파일의 원래 문구와 별도 helper 고지는 다음과 같이 보존합니다.


~~~~text
/*! @license DOMPurify 3.4.16 | (c) Cure53 and other contributors | Released under the Apache license 2.0 and Mozilla Public License 2.0 | github.com/cure53/DOMPurify/blob/3.4.16/LICENSE */
/*! regenerator-runtime -- Copyright (c) 2014-present, Facebook, Inc. -- license (MIT): https://github.com/babel/babel/blob/main/packages/babel-helpers/LICENSE */
~~~~

### Mermaid 내부 구성

Mermaid 12.0.0의 배포 주석과 source map은 js-yaml 4.3.0 및 FastDom 1.0.12의 패치된 원천을 가리킵니다. js-yaml의 [MIT 전문](#notice-a07bc24468b9654c)과 FastDom README에 동봉된 [MIT 전문](#notice-df2ae14f12f31864)을 보존합니다. FastDom에는 별도 LICENSE 파일이 없으며, 패치된 내부 파일을 무수정 upstream 파일과 같다고 표시하지 않습니다.

js-yaml 고지 원천: [npm 4.3.0 archive](https://registry.npmjs.org/js-yaml/-/js-yaml-4.3.0.tgz), registry gitHead `33d05b5d29a8c21360f620f7e1c1706e24522eda`, LICENSE 1,084 bytes/SHA-256 `a07bc24468b9654ce76a547d47a2db282d07733b715db4c73a98bd63961f9550`. FastDom 고지 원천: [npm 1.0.12 archive](https://registry.npmjs.org/fastdom/-/fastdom-1.0.12.tgz), registry gitHead `a7b9044d58952b970c8b918dcf0e7c8824ba0ff5`, README 7,525 bytes/SHA-256 `cc5669ab6f7741bb061c8a7244addc8e3af1f84ec7d2445744b59d0169a87d11`. 두 archive는 registry의 SHA-512 integrity와 대조했습니다.

### 정확한 내부 버전을 확인하지 못한 구성

DOMPurify 배포 header는 Babel helper/regenerator-runtime의 MIT 출처를 명시합니다. 내부 helper의 정확 버전은 **unknown**입니다. 현재 설치된 Babel runtime 버전으로 대신 표시하지 않습니다. header가 가리키는 [Babel helpers MIT 전문](#notice-4be9d87b56a30629)을 원래 저작권과 함께 보존합니다. 고지 원문 commit `a0690e39ea63cdcc3d9282ece739e6677c83ad6e`, 1189 bytes/SHA-256 `4be9d87b56a306293223b490c0d0b245e9e94f39884147bf051a6c7b825aeb30`는 라이선스 원천의 고정이며 코드 버전의 확정이 아닙니다.

RoughJS 4.6.6의 공급 원천 bin은 아래 네 패키지를 import합니다. 실제 배포된 rough.esm.js에는 인접 source map이 없어 각각의 정확한 내부 버전은 **unknown**입니다. 고지 확보에 사용한 설치 버전과 내부 버전을 구분하고 관련 MIT 원문을 보수적으로 보존합니다. 이 표를 네 패키지의 모든 함수가 최종 산출물에 남았다는 의미로 사용하지 않습니다.

| 원천 import 관계 | 고지 확보 설치 버전 | 정확한 내부 버전 | 라이선스 | 원문 |
|---|---|---|---|---|
| hachure-fill | 0.5.2 | unknown | MIT | [원문](#notice-5e807b516a9be922) |
| path-data-parser | 0.1.0 | unknown | MIT | [원문](#notice-e9754a00aebde654) |
| points-on-curve | 0.2.0 | unknown | MIT | [원문](#notice-e9754a00aebde654) |
| points-on-path | 0.2.1 | unknown | MIT | [원문](#notice-b6fc1f7f1d96a48b) |

고정 배포 원천: DOMPurify purify.es.mjs 84,443 bytes/SHA-256 `c44274a7959cfdd4da871fa78a5d5fbbef55db68d118c5c0833bc4b5cf9633ad`; RoughJS rough.esm.js 27,748 bytes/SHA-256 `e921535f216ebc93d422489d614da955b5286812220b3149fc70978f4cb5bd45`.

### Electron 배포 runtime

Electron 44.5.0의 원래 runtime 고지는 `frontend/node_modules/electron/dist/LICENSE`와 `LICENSES.chromium.html`에 별도로 동봉돼 있습니다. 앱의 JavaScript 구성 고지를 이 runtime 고지의 대체로 사용하지 않습니다.

## 원래 라이선스·고지 전문

동일 SHA-256의 원문은 한 번 싣고 해당 구성요소를 함께 연결했습니다. 아래 원문 속 저작권·조건·면책 문구는 원래 배포의 표기를 보존합니다.

<a id="notice-ec9fb67dcb25eccc"></a>

### mermaid 12.0.0

- 원천: node_modules/mermaid/LICENSE (전문; 원본 SHA-256 ec9fb67dcb25eccc416ed56e1aab819222c805a2a4bfe4cb19e7556bf2ffde80)

~~~~text
The MIT License (MIT)

Copyright (c) 2014 - 2022 Knut Sveidqvist

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-5faab7526d055651"></a>

### dayjs 1.11.23

- 원천: node_modules/dayjs/LICENSE (전문; 원본 SHA-256 5faab7526d055651be3aab769d58897be6bd91f3d39d137f25f12dba1b31d5dc)

~~~~text
MIT License

Copyright (c) 2018-present, iamkun

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-66b333b0f66759a0"></a>

### khroma 2.1.0

- 원천: node_modules/khroma/license (전문; 원본 SHA-256 66b333b0f66759a0b710459e03f7029abe17f4358114a128d2c972e642961b49)

~~~~text
The MIT License (MIT)

Copyright (c) 2019-present Fabio Spampinato, Andrew Maney

Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
~~~~

<a id="notice-cfc7749b96f63bd3"></a>

### dompurify 3.4.16

- 원천: node_modules/dompurify/LICENSE (전문; 원본 SHA-256 cfc7749b96f63bd31c3c42b5c471bf756814053e847c10f3eb003417bc523d30)

~~~~text

                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "[]"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.

   Copyright [yyyy] [name of copyright owner]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
~~~~

<a id="notice-766ccc1f306c885a"></a>

### katex 0.16.47

- 원천: node_modules/katex/LICENSE (전문; 원본 SHA-256 766ccc1f306c885aa45542a9846bbd0a505b27a0374f146778171c2254ce18e3)

~~~~text
The MIT License (MIT)

Copyright (c) 2013-2020 Khan Academy and other contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-3e6849627f74ff73"></a>

### d3-array 3.2.4 / d3 7.9.0

- 원천: node_modules/d3-array/LICENSE (전문; 원본 SHA-256 3e6849627f74ff73c257a3ae1efb574015d94fc1035c05ec3c15805165efcbc4)
- 원천: node_modules/d3/LICENSE (전문; 원본 SHA-256 3e6849627f74ff73c257a3ae1efb574015d94fc1035c05ec3c15805165efcbc4)

~~~~text
Copyright 2010-2023 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
~~~~

<a id="notice-e008c5e25a6be382"></a>

### d3-axis 3.0.0 / d3-dispatch 3.0.1 / d3-selection 3.0.0 / d3-drag 3.0.0 / d3-interpolate 3.0.1 / d3-timer 3.0.1 / d3-transition 3.0.1 / d3-brush 3.0.0 / d3-chord 3.0.1 / d3-quadtree 3.0.1 / d3-force 3.0.0 / d3-hierarchy 3.1.2 / d3-polygon 3.0.1 / d3-random 3.0.1 / d3-time-format 4.1.0 / d3-scale 4.0.2 / d3-zoom 3.0.0

- 원천: node_modules/d3-axis/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-dispatch/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-selection/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-drag/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-interpolate/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-timer/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-transition/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-brush/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-chord/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-quadtree/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-force/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-hierarchy/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-polygon/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-random/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-time-format/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-scale/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)
- 원천: node_modules/d3-zoom/LICENSE (전문; 원본 SHA-256 e008c5e25a6be382593089c29bfabbc553c6378eee02895aec46ce396cc404ee)

~~~~text
Copyright 2010-2021 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
~~~~

<a id="notice-faa682e3e430941f"></a>

### d3-color 3.1.0 / d3-time 3.1.0 / d3-shape 3.2.0

- 원천: node_modules/d3-color/LICENSE (전문; 원본 SHA-256 faa682e3e430941f958d26180458f5934a62f58dac4d70ccdd15608c15d0f884)
- 원천: node_modules/d3-time/LICENSE (전문; 원본 SHA-256 faa682e3e430941f958d26180458f5934a62f58dac4d70ccdd15608c15d0f884)
- 원천: node_modules/d3-shape/LICENSE (전문; 원본 SHA-256 faa682e3e430941f958d26180458f5934a62f58dac4d70ccdd15608c15d0f884)

~~~~text
Copyright 2010-2022 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
~~~~

<a id="notice-b989d9085148d21f"></a>

### d3-ease 3.0.1

- 원천: node_modules/d3-ease/LICENSE (전문; 원본 SHA-256 b989d9085148d21ff64a75e14462aefa06b2589a55553b07b1460c70081c7d08)

~~~~text
Copyright 2010-2021 Mike Bostock
Copyright 2001 Robert Penner
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

* Neither the name of the author nor the names of contributors may be used to
  endorse or promote products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
~~~~

<a id="notice-a09b0d6d76e768fc"></a>

### d3-path 3.1.0

- 원천: node_modules/d3-path/LICENSE (전문; 원본 SHA-256 a09b0d6d76e768fca2085fad05743ee81ddd11946bcda160a3cf5e1081a479aa)

~~~~text
Copyright 2015-2022 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
~~~~

<a id="notice-e51cba0e4fa1d8c0"></a>

### d3-contour 4.0.2

- 원천: node_modules/d3-contour/LICENSE (전문; 원본 SHA-256 e51cba0e4fa1d8c078e15319868f2b90188f413f9ce9dac1043b62db32901e93)

~~~~text
Copyright 2012-2023 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
~~~~

<a id="notice-582c3022bd019423"></a>

### d3-delaunay 6.0.4

- 원천: node_modules/d3-delaunay/LICENSE (전문; 원본 SHA-256 582c3022bd01942336095f92b58a90b1be624dc547d987555c6c956512dd24c1)

~~~~text
Copyright 2018-2021 Observable, Inc.
Copyright 2021 Mapbox

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
~~~~

<a id="notice-7842a82012622e5a"></a>

### d3-dsv 3.0.1

- 원천: node_modules/d3-dsv/LICENSE (전문; 원본 SHA-256 7842a82012622e5a37bfe4f6c1b67cd3bae6350716b5bf87f36cea0bdc1319dc)

~~~~text
Copyright 2013-2021 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
~~~~

<a id="notice-ffdc17dc9040006b"></a>

### d3-fetch 3.0.1

- 원천: node_modules/d3-fetch/LICENSE (전문; 원본 SHA-256 ffdc17dc9040006bddeb891c95bfbb492e86d6bc64a4a4233666febab1be35e0)

~~~~text
Copyright 2016-2021 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
~~~~

<a id="notice-2a6d2d5f32ba0b75"></a>

### d3-format 3.1.2

- 원천: node_modules/d3-format/LICENSE (전문; 원본 SHA-256 2a6d2d5f32ba0b755ddbc1c833f766e30cdbe6ebe9a6d4e3e24427721f0b63d3)

~~~~text
Copyright 2010-2026 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
~~~~

<a id="notice-3e3edc1224eec9c3"></a>

### d3-geo 3.1.1

- 원천: node_modules/d3-geo/LICENSE (전문; 원본 SHA-256 3e3edc1224eec9c39cd26491a21304a62883c1e5b6a65c5283ccc7a6cc94baee)

~~~~text
Copyright 2010-2024 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.

This license applies to GeographicLib, versions 1.12 and later.

Copyright 2008-2012 Charles Karney

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
~~~~

<a id="notice-0986d93944902cca"></a>

### d3-scale-chromatic 3.1.0

- 원천: node_modules/d3-scale-chromatic/LICENSE (전문; 원본 SHA-256 0986d93944902cca9af86824b6a641b4b7c2d10602fc510c433a8bcd443919fe)

~~~~text
Copyright 2010-2024 Mike Bostock

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.

Apache-Style Software License for ColorBrewer software and ColorBrewer Color Schemes

Copyright 2002 Cynthia Brewer, Mark Harrower, and The Pennsylvania State University

Licensed under the Apache License, Version 2.0 (the "License"); you may not use
this file except in compliance with the License. You may obtain a copy of the
License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
CONDITIONS OF ANY KIND, either express or implied. See the License for the
specific language governing permissions and limitations under the License.
~~~~

<a id="notice-a86793ac023162ce"></a>

### @iconify/utils 3.1.7

- 원천: node_modules/@iconify/utils/license.txt (전문; 원본 SHA-256 a86793ac023162ce98b91899601c15f6712f44943163c4f92a0a4bf89152b3bf)

~~~~text
MIT License

Copyright (c) 2021-PRESENT Vjacheslav Trushkin

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-0984740e0c3d725c"></a>

### @braintree/sanitize-url 7.1.2

- 원천: node_modules/@braintree/sanitize-url/LICENSE (전문; 원본 SHA-256 0984740e0c3d725c8044dec7edcefe1dbce180ef5a7bc710c251e19607000158)

~~~~text
MIT License

Copyright (c) 2017 Braintree

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-51f3674294d988d8"></a>

### es-toolkit 1.52.0

- 원천: node_modules/es-toolkit/LICENSE (전문; 원본 SHA-256 51f3674294d988d89dbebd31273baaeb3c697a15482d9699b14d1fded2e132a7)

~~~~text
MIT License

Copyright (c) 2024 Viva Republica, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-11ee5e3bce9b0a4d"></a>

### es-toolkit 1.52.0

- 원천: node_modules/es-toolkit/NOTICE (전문; 원본 SHA-256 11ee5e3bce9b0a4dde7f73811caf98978e710228d891c3111ad8ef8d7d2a916f)

~~~~text
es-toolkit

Parts of the test suite and compatibility layer in es-toolkit/compat are
derived from Lodash (https://github.com/lodash/lodash).

Lodash copyright notice and MIT permission notice:

Copyright OpenJS Foundation and other contributors <https://openjsf.org/>

Based on Underscore.js, copyright Jeremy Ashkenas,
DocumentCloud and Investigative Reporters & Editors <http://underscorejs.org/>

This software consists of voluntary contributions made by many
individuals. For exact contribution history, see the revision history
available at https://github.com/lodash/lodash

The following license applies to all parts of this software except as
documented below:

====

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
~~~~

<a id="notice-8e3a3f82f59a6095"></a>

### marked 16.4.2

- 원천: node_modules/marked/LICENSE.md (전문; 원본 SHA-256 8e3a3f82f59a60958f56ca08f445647c32a4733dc7ca6c2c46f6eb898471ab9c)

~~~~text
# License information

## Contribution License Agreement

If you contribute code to this project, you are implicitly allowing your code
to be distributed under the MIT license. You are also implicitly verifying that
all code is your original work. `</legalese>`

## Marked

Copyright (c) 2018+, MarkedJS (https://github.com/markedjs/)
Copyright (c) 2011-2018, Christopher Jeffrey (https://github.com/chjj/)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

## Markdown

Copyright © 2004, John Gruber
http://daringfireball.net/
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
* Neither the name “Markdown” nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

This software is provided by the copyright holders and contributors “as is” and any express or implied warranties, including, but not limited to, the implied warranties of merchantability and fitness for a particular purpose are disclaimed. In no event shall the copyright owner or contributors be liable for any direct, indirect, incidental, special, exemplary, or consequential damages (including, but not limited to, procurement of substitute goods or services; loss of use, data, or profits; or business interruption) however caused and on any theory of liability, whether in contract, strict liability, or tort (including negligence or otherwise) arising in any way out of the use of this software, even if advised of the possibility of such damage.
~~~~

<a id="notice-eff13095d9d762c3"></a>

### ts-dedent 2.3.0

- 원천: node_modules/ts-dedent/LICENSE (전문; 원본 SHA-256 eff13095d9d762c3bd9dda74370c81e2130c28f2691bc455603abf973bac25b9)

~~~~text
MIT License

Copyright (c) 2018 Tamino Martinius

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-dca9a392272606ac"></a>

### roughjs 4.6.6

- 원천: node_modules/roughjs/LICENSE (전문; 원본 SHA-256 dca9a392272606ac748ac0976a2a1133f14eef841c27beaa51a844d53c56a09d)

~~~~text
MIT License

Copyright (c) 2019 Preet Shihn

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-f71e8ed126b46346"></a>

### lodash-es 4.18.1

- 원천: node_modules/lodash-es/LICENSE (전문; 원본 SHA-256 f71e8ed126b46346494aad5486874cd8f0aafe95092ed67d2e3cb6110f939abc)

~~~~text
Copyright OpenJS Foundation and other contributors <https://openjsf.org/>

Based on Underscore.js, copyright Jeremy Ashkenas,
DocumentCloud and Investigative Reporters & Editors <http://underscorejs.org/>

This software consists of voluntary contributions made by many
individuals. For exact contribution history, see the revision history
available at https://github.com/lodash/lodash

The following license applies to all parts of this software except as
documented below:

====

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

====

Copyright and related rights for sample code are waived via CC0. Sample
code is defined as all source code displayed within the prose of the
documentation.

CC0: http://creativecommons.org/publicdomain/zero/1.0/

====

Files located in the node_modules and vendor directories are externally
maintained libraries used by this software which have their own
licenses; we recommend you read them, as their terms may differ from the
terms above.
~~~~

<a id="notice-d01eb84ccce437db"></a>

### dagre-d3-es 7.0.14

- 원천: node_modules/dagre-d3-es/LICENSE.md (전문; 원본 SHA-256 d01eb84ccce437dbf5fd40853bceea0f287930920acea066597a4a25ead63858)

~~~~text
Original dagre-d3 copyright: Copyright (c) 2013 Chris Pettitt
Original dagre copyright: Copyright (c) 2012-2014 Chris Pettitt
Original graphlib copyright: Copyright (c) 2012-2014 Chris Pettitt

Copyright (c) 2022-2024 Thibaut Lassalle, David Newell, Alois Klink, Sidharth Vinod and dagre-es contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
~~~~

<a id="notice-88c579c6ee44b411"></a>

### stylis 4.4.0

- 원천: node_modules/stylis/LICENSE (전문; 원본 SHA-256 88c579c6ee44b41137a21321190d177982e5b17d4fa1b83a4a2e3ea325a8fc9a)

~~~~text
MIT License

Copyright (c) 2016-present Sultan Tarimo

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-da6d3703ed11cbe4"></a>

### react 19.3.0 / scheduler 0.28.0 / react-dom 19.3.0

- 원천: node_modules/react/LICENSE (전문; 원본 SHA-256 da6d3703ed11cbe42bd212c725957c98da23cbff1998c05fa4b3d976d1a58e93)
- 원천: node_modules/scheduler/LICENSE (전문; 원본 SHA-256 da6d3703ed11cbe42bd212c725957c98da23cbff1998c05fa4b3d976d1a58e93)
- 원천: node_modules/react-dom/LICENSE (전문; 원본 SHA-256 da6d3703ed11cbe42bd212c725957c98da23cbff1998c05fa4b3d976d1a58e93)

~~~~text
MIT License

Copyright (c) Meta Platforms, Inc. and affiliates.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-444c5ec3bf7f798a"></a>

### vite 8.3.1

- 원천: node_modules/vite/LICENSE.md (Vite core MIT 절 — 빌드 도구 전체 의존 목록과 구분; 원본 SHA-256 387dd7baa307083401a27c58c362c30832f5ba1dba84f10cc22c33401523f45c)

~~~~text
# Vite core license
Vite is released under the MIT license:

MIT License

Copyright (c) 2019-present, VoidZero Inc. and Vite contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

~~~~

<a id="notice-23ecfff35a5a2e80"></a>

### rolldown 1.2.11

- 원천: node_modules/rolldown/LICENSE (전문; 원본 SHA-256 23ecfff35a5a2e80d92142f75228912c3b1abc4b5a8337a821ff4397e2f9f734)

~~~~text
MIT License

Copyright (c) 2024-present VoidZero Inc. & Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

end of terms and conditions

The licenses of externally maintained libraries from which parts of the Software is derived are listed [here](https://github.com/rolldown/rolldown/blob/main/THIRD-PARTY-LICENSE).
~~~~

<a id="notice-a877291d800ed436"></a>

### rolldown 1.2.11

- 원천: node_modules/rolldown/THIRD-PARTY-LICENSE (전문; 원본 SHA-256 a877291d800ed43692f3f9ae09d8e01cc6f7293ad39d43896059c188ffbb8b7c)

~~~~text
The MIT License (MIT)

Copyright (c) 2017 [these people](https://github.com/rollup/rollup/graphs/contributors)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

---

MIT License

Copyright (c) 2020 Evan Wallace

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-a07bc24468b9654c"></a>

### js-yaml 4.3.0 (Mermaid 내부)

- 원천: npm js-yaml 4.3.0 package/LICENSE; SHA-256 a07bc24468b9654ce76a547d47a2db282d07733b715db4c73a98bd63961f9550

~~~~text
(The MIT License)

Copyright (C) 2011-2015 by Vitaly Puzrin

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
~~~~

<a id="notice-df2ae14f12f31864"></a>

### FastDom 1.0.12 (Mermaid의 패치된 내부 구성)

- 원천: npm fastdom 1.0.12 package/README.md License 절; README SHA-256 cc5669ab6f7741bb061c8a7244addc8e3af1f84ec7d2445744b59d0169a87d11

~~~~text
## License

(The MIT License)

Copyright (c) 2016 Wilson Page <wilsonpage@me.com>

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the 'Software'), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
~~~~

<a id="notice-4be9d87b56a30629"></a>

### Babel helper / regenerator-runtime (DOMPurify 내부; 정확 버전 unknown)

- 원천: https://raw.githubusercontent.com/babel/babel/a0690e39ea63cdcc3d9282ece739e6677c83ad6e/packages/babel-helpers/LICENSE; 1189 bytes; SHA-256 4be9d87b56a306293223b490c0d0b245e9e94f39884147bf051a6c7b825aeb30

~~~~text
MIT License

Copyright (c) 2014-present Sebastian McKenzie and other contributors
Copyright (c) 2014-present, Facebook, Inc. (ONLY ./src/helpers/regenerator* files)

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
~~~~

<a id="notice-5e807b516a9be922"></a>

### hachure-fill (RoughJS 원천 import 관계; 내부 버전 unknown)

- 원천: 고지 확보 원천 hachure-fill 0.5.2/LICENSE; 1068 bytes; SHA-256 5e807b516a9be9229ed459880bf353a336e2d947867247290680d437017be391

~~~~text
MIT License

Copyright (c) 2023 Preet Shihn

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-e9754a00aebde654"></a>

### path-data-parser (RoughJS 원천 import 관계; 내부 버전 unknown) / points-on-curve (RoughJS 원천 import 관계; 내부 버전 unknown)

- 원천: 고지 확보 원천 path-data-parser 0.1.0/LICENSE; 1068 bytes; SHA-256 e9754a00aebde654e80f40bcf41dab667d6a41dfbbd2912bcf14fb07d468bf71
- 원천: 고지 확보 원천 points-on-curve 0.2.0/LICENSE; 1068 bytes; SHA-256 e9754a00aebde654e80f40bcf41dab667d6a41dfbbd2912bcf14fb07d468bf71

~~~~text
MIT License

Copyright (c) 2020 Preet Shihn

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

<a id="notice-b6fc1f7f1d96a48b"></a>

### points-on-path (RoughJS 원천 import 관계; 내부 버전 unknown)

- 원천: 고지 확보 원천 points-on-path 0.2.1/LICENSE; 1062 bytes; SHA-256 b6fc1f7f1d96a48bc97176f3dcef959e364c5c17ba44167511de421500a86c6c

~~~~text
MIT License

Copyright (c) 2020 Preet

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~
