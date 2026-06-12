#nullable enable
using Dawnholder.Client.UI;
using NUnit.Framework;

namespace Dawnholder.Client.Tests
{
    // SkillHudController.ComputeFill 순수 함수 단위 테스트.
    //
    // fill 방향 = remaining/total (잠금 표시 컨벤션 — 쿨다운 중 fill 만참 → 0 감소).
    // 준비도 flip(1f - ...)은 영호 외관 결정 — 이 테스트는 raw fill 값만 검증.
    public class SkillHudControllerTests
    {
        [Test]
        public void ComputeFill_JustCast_ReturnsOne()
        {
            // 막 시전(remaining == total) → 잠금 표시 최대 fill.
            float fill = SkillHudController.ComputeFill(remaining: 3f, total: 3f);
            Assert.AreEqual(1f, fill, 0.0001f);
        }

        [Test]
        public void ComputeFill_ReadyToUse_ReturnsZero()
        {
            // 준비 완료(remaining == 0) → fill 0 (잠금 없음).
            float fill = SkillHudController.ComputeFill(remaining: 0f, total: 3f);
            Assert.AreEqual(0f, fill, 0.0001f);
        }

        [Test]
        public void ComputeFill_RemainingExceedsTotal_ClampsToOne()
        {
            // remaining > total: Clamp01으로 1f 수렴 — 음수 쿨다운 등 이상 입력 방어.
            float fill = SkillHudController.ComputeFill(remaining: 5f, total: 2f);
            Assert.AreEqual(1f, fill, 0.0001f);
        }

        [Test]
        public void ComputeFill_TotalIsZero_ReturnsZero()
        {
            // total == 0: "한 번도 시전 안 함" — 준비 완료 외관(fill 0).
            float fill = SkillHudController.ComputeFill(remaining: 0f, total: 0f);
            Assert.AreEqual(0f, fill, 0.0001f);
        }
    }
}
