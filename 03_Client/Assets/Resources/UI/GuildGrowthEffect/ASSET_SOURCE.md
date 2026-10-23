# Guild growth effect atlas

Generated once with the built-in `image_gen.imagegen` tool on 2026-10-21 for the approved guild growth effect.
No retry, edit call, external SDK or image transformation was used. The original alpha is preserved byte for byte.
The style reference was the previously inspected guild-base concept (gold sun/stronghold, royal blue pennants,
warm gold light); it was a visual reference, not an edit target.

- Original: `C:/Users/bass1/.codex/generated_images/01a15e99-86fc-7272-b1f1-708ac7711b87/exec-8d0da1cb-b5d9-461f-9b2e-538df04ea84d.png`.
- Original and consumed PNG SHA256: `6c12e1535a8ae904f0d546d8a925bcaa88ee7517206c3fb708ba6440bc56c5d7`.
- Actual size: 1254 x 1254, Format32bppArgb. Requested size was 1024 square; the actual output is authoritative.
- Alpha census: 641319 fully transparent, 930931 translucent, 266 fully opaque pixels (System.Drawing read only).
- Resources key: `UI/GuildGrowthEffect/GuildGrowthEffectAtlas`.
- PNG GUID: `eac8e1489206432e8840aa53f00c9d66`; folder GUID: `ae115b0109644d17a04ca82e662e7b44`.
- Full prompt and tool/copy receipt: `growth-effect-pr/implementation-01/imagegen-prompt.md`
  and `growth-effect-pr/implementation-01/raw/imagegen-receipt.json` in the verification evidence tree.

The generated atlas was directly inspected. It has no letters, numbers or watermark. Its four materials are
recognizably different, but the requested uniform gutters were not produced and the lower halo reaches the
image edge. No pixels were repaired. The consumer uses these inspected source rectangles instead of inventing
an exact quadrant grid; low-alpha edge/crop quality still needs the later actual Unity captures.

| Runtime sprite | Top-left origin x,y,w,h | Unity bottom-left x,y,w,h |
|---|---|---|
| SunStronghold | 32,24,600,584 | 32,646,600,584 |
| WeaponCompletion | 672,24,552,584 | 672,646,552,584 |
| AmberHalo | 0,608,672,646 | 0,0,672,646 |
| GoldenSparks | 688,656,544,568 | 688,30,544,568 |

GuildGrowthEffectPresentation creates four full-rect runtime sprites from the atlas per view. That presentation
destroys only these runtime sprites when the scene-owned view is destroyed. Resources retains the original
texture and project font. The PNG importer disables mipmaps/compression/resizing and retains input alpha.
