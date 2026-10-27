#nullable enable
using System.Linq;
using Dawnholder.Client.Demo;
using NUnit.Framework;

namespace Dawnholder.Client.Tests
{
    // The demo autopilot must stay off for every launch the shipped build sees; only -demo-role turns it on.
    public class DemoLaunchOptionsTests
    {
        [Test]
        public void ShippedLaunch_WithoutDemoRole_StaysOff()
        {
            Assert.IsNull(DemoLaunchOptions.Parse(new[] { "Dawnholder.exe" }));
            Assert.IsNull(DemoLaunchOptions.Parse(new[] { "Dawnholder.exe", "-screen-width", "960", "-screen-height", "540" }));
            Assert.IsNull(DemoLaunchOptions.Parse(new[] { "Dawnholder.exe", "-demo-account", "demo_knight" }));
        }

        [Test]
        public void UnknownRoleOrMissingValue_StaysOff()
        {
            Assert.IsNull(DemoLaunchOptions.Parse(new[] { "Dawnholder.exe", "-demo-role", "archer" }));
            Assert.IsNull(DemoLaunchOptions.Parse(new[] { "Dawnholder.exe", "-demo-role" }));
        }

        [Test]
        public void DemoRole_SelectsRoleAndDefaultAccount()
        {
            DemoLaunchOptions? knight = DemoLaunchOptions.Parse(new[] { "Dawnholder.exe", "-demo-role", "Knight" });
            DemoLaunchOptions? mage = DemoLaunchOptions.Parse(new[] { "Dawnholder.exe", "-demo-role", "mage", "-demo-account", "mage_two" });

            Assert.AreEqual(DemoRole.Knight, knight!.Role);
            Assert.AreEqual("demo_knight", knight.Account);
            Assert.AreEqual(DemoRole.Mage, mage!.Role);
            Assert.AreEqual("mage_two", mage.Account);
        }

        [Test]
        public void SoundSwitches_AreOptInPerPlayer()
        {
            DemoLaunchOptions plain = DemoLaunchOptions.Parse(new[] { "Dawnholder.exe", "-demo-role", "knight" })!;
            DemoLaunchOptions knight = DemoLaunchOptions.Parse(new[] { "Dawnholder.exe", "-demo-role", "knight", "-demo-no-bgm" })!;
            DemoLaunchOptions mage = DemoLaunchOptions.Parse(new[] { "Dawnholder.exe", "-demo-role", "mage", "-demo-mute" })!;

            Assert.IsFalse(plain.Muted || plain.NoMusic);
            Assert.IsTrue(knight.NoMusic && !knight.Muted);
            Assert.IsTrue(mage.Muted && !mage.NoMusic);
        }

        [Test]
        public void CameraZoom_IsOptIn_AndIgnoresValuesOutsideOneToThree()
        {
            float ZoomOf(params string[] extra) =>
                DemoLaunchOptions.Parse(new[] { "Dawnholder.exe", "-demo-role", "knight" }.Concat(extra).ToArray())!.CameraZoom;

            Assert.AreEqual(1f, ZoomOf());
            Assert.AreEqual(1.4f, ZoomOf("-demo-zoom", "1.4"), 1e-5f);
            Assert.AreEqual(1f, ZoomOf("-demo-zoom", "0.5"));
            Assert.AreEqual(1f, ZoomOf("-demo-zoom", "5"));
            Assert.AreEqual(1f, ZoomOf("-demo-zoom", "1,4"));
            Assert.AreEqual(1f, ZoomOf("-demo-zoom"));
        }
    }
}
