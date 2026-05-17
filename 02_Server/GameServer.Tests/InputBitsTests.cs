using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests;

// Phase 07 (M2): InputBits 비트필드 인코딩/디코딩 검증.
// D2 결정 (현업 정석) + Codex 5규칙 (상의 결과) + Codex 함정 3건 반영.
//
// 테스트 카테고리:
//   1) Round-trip (정상 사용 — Encode → Decode 보존)
//   2) 비트 위치 검증 (jumpPressed 독립성)
//   3) invalid 방어 (Codex 함정 #1 — 11 reserved 코드)
//   4) Encode 예외 (호출자 책임 — 프로그래밍 에러)
//   5) 미래 예약 비트 (bit 3~7) 무시 검증
public class InputBitsTests
{
    // === 1) Round-trip (정상 사용 4 조합) ===
    [Theory]
    [InlineData((sbyte)-1, false)]
    [InlineData((sbyte)0, false)]
    [InlineData((sbyte)1, false)]
    [InlineData((sbyte)-1, true)]
    [InlineData((sbyte)0, true)]
    [InlineData((sbyte)1, true)]
    public void Encode_Then_Decode_PreservesAllValues(sbyte inputX, bool jumpPressed)
    {
        byte encoded = InputBits.Encode(inputX, jumpPressed);
        (sbyte decodedX, bool decodedJump, bool valid) = InputBits.Decode(encoded);

        Assert.Equal(inputX, decodedX);
        Assert.Equal(jumpPressed, decodedJump);
        Assert.True(valid); // 정상 인코드는 항상 valid
    }

    // === 2) 비트 위치 검증 (PDL 주석 일치 — Codex 함정 #3) ===
    [Fact]
    public void Encode_InputXNeg_NoJump_ProducesZero()
    {
        // -1 → bit 0~1 = 00, jumpPressed=false → bit 2 = 0 → 0x00
        Assert.Equal((byte)0x00, InputBits.Encode(-1, false));
    }

    [Fact]
    public void Encode_InputXZero_NoJump_ProducesBit0Set()
    {
        // 0 → bit 0~1 = 01 → 0x01
        Assert.Equal((byte)0x01, InputBits.Encode(0, false));
    }

    [Fact]
    public void Encode_InputXPos_NoJump_ProducesBit1Set()
    {
        // +1 → bit 0~1 = 10 → 0x02
        Assert.Equal((byte)0x02, InputBits.Encode(1, false));
    }

    [Fact]
    public void Encode_InputXNeg_Jump_OnlyBit2Set()
    {
        // -1 (00) + jumpPressed (bit 2) → 0x04
        Assert.Equal((byte)0x04, InputBits.Encode(-1, true));
    }

    [Fact]
    public void Encode_InputXPos_Jump_ProducesBits1And2()
    {
        // +1 (10) + jumpPressed (bit 2) → 0x06
        Assert.Equal((byte)0x06, InputBits.Encode(1, true));
    }

    // === 3) invalid 방어 (Codex 함정 #1) ===
    [Fact]
    public void Decode_ReservedCode_NormalizesToZero_FlagsInvalid()
    {
        // 0x03 = 0b11 (inputX 비트가 reserved)
        (sbyte inputX, bool jumpPressed, bool valid) = InputBits.Decode(0x03);

        Assert.Equal((sbyte)0, inputX);    // 안전 default 정상화
        Assert.False(jumpPressed);          // bit 2 안 켜졌으니 false
        Assert.False(valid);                // cheat / mismatch 시그널
    }

    [Fact]
    public void Decode_ReservedCodeWithJump_NormalizesButStillInvalid()
    {
        // 0x07 = 0b111 (inputX reserved + jumpPressed)
        (sbyte inputX, bool jumpPressed, bool valid) = InputBits.Decode(0x07);

        Assert.Equal((sbyte)0, inputX);
        Assert.True(jumpPressed);           // bit 2는 살아있음 (독립 처리)
        Assert.False(valid);
    }

    // === 4) Encode 예외 (호출자 책임) ===
    [Theory]
    [InlineData((sbyte)2)]
    [InlineData((sbyte)-2)]
    [InlineData(sbyte.MaxValue)]
    [InlineData(sbyte.MinValue)]
    public void Encode_InvalidInputX_Throws(sbyte invalidInputX)
    {
        // Encode는 호출자가 정규화 책임 (LocalPlayerController.EncodeInputX가 -1/0/1로 clamp).
        // 예외는 *프로그래밍 에러* 빨리 잡기 의도 (cheat는 wire 측에서 처리).
        Assert.Throws<ArgumentOutOfRangeException>(() => InputBits.Encode(invalidInputX, false));
    }

    // === 5) 미래 예약 비트 무시 ===
    [Fact]
    public void Decode_ReservedBitsSet_IgnoresFutureBits()
    {
        // 0xF8 = 0b1111_1000 — bit 3~7만 set. inputX=-1 (00), jumpPressed=false (bit 2=0).
        (sbyte inputX, bool jumpPressed, bool valid) = InputBits.Decode(0xF8);

        Assert.Equal((sbyte)-1, inputX);
        Assert.False(jumpPressed);
        Assert.True(valid); // bit 0~1이 00이라 valid
    }

    [Fact]
    public void Decode_AllBitsSet_DetectsInvalid()
    {
        // 0xFF — 모든 비트 on. bit 0~1 = 11 (reserved) → invalid.
        (sbyte inputX, bool jumpPressed, bool valid) = InputBits.Decode(0xFF);

        Assert.Equal((sbyte)0, inputX); // 정상화
        Assert.True(jumpPressed);        // bit 2 on
        Assert.False(valid);
    }

    // === 6) 비트 독립성 (jumpPressed가 inputX 인코딩에 영향 X) ===
    [Theory]
    [InlineData((sbyte)-1)]
    [InlineData((sbyte)0)]
    [InlineData((sbyte)1)]
    public void Encode_JumpDoesNotAffectInputXBits(sbyte inputX)
    {
        byte withoutJump = InputBits.Encode(inputX, false);
        byte withJump = InputBits.Encode(inputX, true);

        // 차이는 정확히 bit 2 (= 0x04)만
        Assert.Equal(0x04, withJump - withoutJump);
    }
}
