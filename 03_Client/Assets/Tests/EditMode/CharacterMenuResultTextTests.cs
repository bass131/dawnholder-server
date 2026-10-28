using Dawnholder.Client.UI;
using NUnit.Framework;

namespace Dawnholder.Client.Tests
{
    // Expected wording is the character menu goal's approved table, copied literally. Bytes are World's
    // packet-contract-v1 result (0 Success .. 16 LegacyFlowOnly) and invalidField (0 None .. 4 CharacterId).
    public sealed class CharacterMenuResultTextTests
    {
        [TestCase(1, 2, "이름은 2~12자의 한글·영문·숫자만 쓸 수 있습니다.")]
        [TestCase(1, 1, "선택을 다시 확인해 주세요. 목록을 새로고침합니다.")]
        [TestCase(1, 3, "선택을 다시 확인해 주세요. 목록을 새로고침합니다.")]
        [TestCase(1, 4, "선택을 다시 확인해 주세요. 목록을 새로고침합니다.")]
        [TestCase(2, 0, "앞의 요청을 처리하는 중입니다. 잠시 기다려 주세요.")]
        [TestCase(3, 0, "지금은 이 요청을 할 수 없습니다. 메뉴로 돌아가 다시 접속해 주세요.")]
        [TestCase(4, 0, "이미 캐릭터가 있는 슬롯입니다. 목록을 새로고침해 주세요.")]
        [TestCase(5, 0, "캐릭터 슬롯 3칸이 모두 찼습니다.")]
        [TestCase(6, 0, "이미 쓰는 이름입니다. 다른 이름을 입력해 주세요.")]
        [TestCase(7, 0, "이 캐릭터를 찾을 수 없습니다. 목록을 새로고침해 주세요.")]
        [TestCase(8, 0, "이 캐릭터가 다른 곳에서 접속 중입니다. 잠시 뒤 다시 시도해 주세요.")]
        [TestCase(9, 0, "캐릭터 상태를 확인하는 중이라 지금은 입장할 수 없습니다.")]
        [TestCase(10, 0, "서버가 잠시 응답하지 못했습니다. 다시 시도해 주세요.")]
        [TestCase(11, 0, "서버가 잠시 응답하지 못했습니다. 다시 시도해 주세요.")]
        [TestCase(12, 0, "결과를 확인하지 못해 연결을 끊습니다. 다시 접속해 목록을 먼저 확인해 주세요.")]
        [TestCase(13, 0, "서버 오류로 처리하지 못했습니다(코드 13).")]
        [TestCase(14, 0, "서버 오류로 처리하지 못했습니다(코드 14).")]
        [TestCase(15, 0, "서버 오류로 처리하지 못했습니다(코드 15).")]
        public void EachRefusal_ShowsItsApprovedWording(int result, int invalidField, string expected)
        {
            Assert.IsTrue(CharacterMenuResultText.TryGetFailureText((byte)result, (byte)invalidField, out string text));
            Assert.AreEqual(expected, text);
        }

        [TestCase(0)]
        [TestCase(16)]
        public void SuccessAndLegacyFlowOnly_HaveNoErrorWording(int result)
        {
            Assert.IsFalse(CharacterMenuResultText.TryGetFailureText((byte)result, 0, out string text));
            Assert.AreEqual(string.Empty, text);
        }

        [TestCase(0)]
        [TestCase(5)]
        [TestCase(255)]
        public void InvalidInputWithoutAKnownField_IsNotPresumedToBeTheName(int invalidField)
        {
            Assert.IsTrue(CharacterMenuResultText.TryGetFailureText(1, (byte)invalidField, out string text));
            Assert.AreNotEqual("이름은 2~12자의 한글·영문·숫자만 쓸 수 있습니다.", text);
            Assert.AreEqual("서버 오류로 처리하지 못했습니다(코드 1).", text);
        }

        [TestCase(17, "서버 오류로 처리하지 못했습니다(코드 17).")]
        [TestCase(255, "서버 오류로 처리하지 못했습니다(코드 255).")]
        public void ResultPastTheContract_GetsTheGeneralCodedWording(int result, string expected)
        {
            Assert.IsTrue(CharacterMenuResultText.TryGetFailureText((byte)result, 0, out string text));
            Assert.AreEqual(expected, text);
        }

        [TestCase(6, 2, "이미 쓰는 이름입니다. 다른 이름을 입력해 주세요.")]
        [TestCase(12, 1, "결과를 확인하지 못해 연결을 끊습니다. 다시 접속해 목록을 먼저 확인해 주세요.")]
        public void InvalidField_IsReadOnlyForInvalidInput(int result, int invalidField, string expected)
        {
            Assert.IsTrue(CharacterMenuResultText.TryGetFailureText((byte)result, (byte)invalidField, out string text));
            Assert.AreEqual(expected, text);
        }

        [Test]
        public void EveryByteCombination_ReturnsWithoutThrowing()
        {
            for (int result = 0; result <= byte.MaxValue; result++)
                for (int invalidField = 0; invalidField <= byte.MaxValue; invalidField++)
                {
                    bool failure = CharacterMenuResultText.TryGetFailureText((byte)result, (byte)invalidField, out string text);
                    Assert.AreEqual(result != 0 && result != 16, failure, $"result {result}, invalidField {invalidField}");
                    Assert.AreEqual(failure, text.Length > 0, $"result {result}, invalidField {invalidField}");
                }
        }
    }
}
