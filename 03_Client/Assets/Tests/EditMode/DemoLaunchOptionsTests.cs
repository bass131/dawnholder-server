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

        [Test]
        public void CharacterArgument_PreservesRawNameAndExistingLaunchOptions()
        {
            // PR268 RED contract v1 R1: whitespace is significant; the server alone judges character names.
            const string rawName = "  데모 기사 ";
            DemoLaunchOptions? options = DemoLaunchOptions.Parse(new[]
            {
                "Dawnholder.exe", "-demo-role", "knight", "-demo-character", rawName,
                "-demo-account", "demo_extra", "-demo-mute", "-demo-no-bgm", "-demo-zoom", "1.4"
            });

            Assert.IsNotNull(options, "R1: the role enables the demo");
            Assert.AreEqual(rawName, options!.Character);
            Assert.AreEqual(DemoRole.Knight, options.Role);
            Assert.AreEqual("demo_extra", options.Account);
            Assert.IsTrue(options.Muted);
            Assert.IsTrue(options.NoMusic);
            Assert.AreEqual(1.4f, options.CameraZoom, 1e-5f);
        }

        [Test]
        public void WithoutCharacterArgument_CharacterIsNull()
        {
            // R1: a matching saved character may be selected without a name argument.
            DemoLaunchOptions? options = DemoLaunchOptions.Parse(new[] { "Dawnholder.exe", "-demo-role", "knight" });

            Assert.IsNotNull(options);
            Assert.IsNull(options!.Character);
        }

        [Test]
        public void CharacterArgumentWithoutTrailingValue_CharacterIsNull()
        {
            // R1: a dangling switch is absent, rather than an empty or invented name.
            DemoLaunchOptions? options = DemoLaunchOptions.Parse(new[]
            {
                "Dawnholder.exe", "-demo-role", "knight", "-demo-character"
            });

            Assert.IsNotNull(options);
            Assert.IsNull(options!.Character);
        }

        [Test]
        public void CharacterArgumentWithoutDemoRole_StaysOff()
        {
            // R1 / goal B1: the character switch cannot enable the autopilot by itself.
            DemoLaunchOptions? options = DemoLaunchOptions.Parse(new[]
            {
                "Dawnholder.exe", "-demo-character", "  데모 기사 "
            });

            Assert.IsNull(options);
        }
    }
}
