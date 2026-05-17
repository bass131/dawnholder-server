using System;

namespace Shared.GameData;

/// <summary>
/// Phase 07 (M2): C_MoveIntent의 `byte input` 비트필드 인코딩/디코딩 단일 출처.
///
/// **헌법 #1 (Server Authority) 정합 + Codex 시니어 5규칙 (2026-05-17 상의)**:
///   - 양쪽이 같은 헬퍼 호출 → drift 0 (서버/클라 중복 디코드 금지)
///   - PDL `C_MoveIntent.input` 비트 레이아웃은 *본 파일 + PDL 주석에 동시 박힘*
///     → 살아있는 단일 출처. PDL XML 주석이 *문서*고 본 파일이 *구현* (호환 의무).
///   - 미래 입력 종류 늘면 (공격/방어/스킬) → 본 파일 그대로 확장. 어느 시점에
///     `InputState` struct로 승격은 자연 진화 (지금은 wire format 헬퍼만).
///
/// **비트 레이아웃** (`byte input`, 8 bit):
///   bit 0~1: inputX (00=-1 / 01=0 / 10=+1 / 11=reserved/invalid)
///   bit 2:   jumpPressed (edge — 클라가 1tick만 true 송신, D4 결정)
///   bit 3~7: reserved (미래 입력 — 공격/방어/스킬 등)
///
/// **invalid 방어** (Codex 함정 #1): `11` (=3) inputX 패턴은 cheat 또는 protocol mismatch.
///   Decode는 `inputX=0` 정상화 + `valid=false` 플래그 반환 — 호출자(서버 `GameSession`)가
///   cheat 기록 결정 (헌법 #3 Trust Boundary). 클라는 절대 박을 일 없음
///   (Encode는 sbyte -1/0/1만 받고 그 외 throw).
///
/// **D2 결정 (현업 정석)**: Quake3 `usercmd_t.buttons` / Source `CUserCmd::buttons` 패턴.
///   byte 단독(D2-a)이 학습은 쉽지만 입력 종류 늘면 패킷 비대 → 비트필드(D2-b) 채택.
///
/// **SOLID**:
///   - SRP: 비트 인코딩/디코딩만. `Physics` / `Constants`와 책임 분리.
///   - Static class: 상태 X, 결정론 변환만.
/// </summary>
public static class InputBits
{
    // ──────────────────────────────────────────────────────────────────
    // 비트 위치 (PDL XML 주석과 일치 의무 — Codex 함정 #3)
    // ──────────────────────────────────────────────────────────────────

    /// <summary>inputX (2 bit) — bit 0~1.</summary>
    private const int InputXShift = 0;

    /// <summary>inputX 추출 마스크 (bit 0~1).</summary>
    private const byte InputXMask = 0b0000_0011;

    /// <summary>jumpPressed 비트 (bit 2).</summary>
    private const byte JumpBit = 0b0000_0100;

    // ──────────────────────────────────────────────────────────────────
    // inputX literal 매핑 (Codex 권장 — 2비트 enum처럼 다루면 디버깅 용이)
    // 11=reserved/invalid: cheat 또는 protocol mismatch
    // ──────────────────────────────────────────────────────────────────

    private const byte InputXCodeNeg = 0b00;       // -1 (좌)
    private const byte InputXCodeZero = 0b01;      //  0 (정지)
    private const byte InputXCodePos = 0b10;       // +1 (우)
    private const byte InputXCodeReserved = 0b11;  // invalid

    /// <summary>
    /// (inputX, jumpPressed) → byte. 클라 송신 직전 호출.
    /// inputX는 -1/0/1만 유효. 그 외는 ArgumentOutOfRangeException — 프로그래밍 에러
    /// (클라 입력 모듈이 EncodeInputX로 정규화 후 호출하므로 발생할 일 없음).
    /// </summary>
    public static byte Encode(sbyte inputX, bool jumpPressed)
    {
        byte code = inputX switch
        {
            -1 => InputXCodeNeg,
            0 => InputXCodeZero,
            1 => InputXCodePos,
            _ => throw new ArgumentOutOfRangeException(
                nameof(inputX), inputX,
                "inputX must be -1, 0, or 1 (caller responsibility — normalize before Encode)")
        };
        byte result = (byte)(code << InputXShift);
        if (jumpPressed) result |= JumpBit;
        return result;
    }

    /// <summary>
    /// byte → (inputX, jumpPressed, valid). 서버 수신 직후 호출.
    ///
    /// **valid=false 케이스**: `11` reserved 코드 — 호출자가 cheat 기록 결정 (헌법 #3).
    /// 정상화: invalid 시 inputX=0 반환 (안전 default — 서버 시뮬레이션이 폭주 안 함).
    /// </summary>
    public static (sbyte inputX, bool jumpPressed, bool valid) Decode(byte input)
    {
        byte code = (byte)((input >> InputXShift) & InputXMask);
        bool valid = code != InputXCodeReserved;
        sbyte inputX = code switch
        {
            InputXCodeNeg => (sbyte)-1,
            InputXCodeZero => (sbyte)0,
            InputXCodePos => (sbyte)1,
            _ => (sbyte)0  // reserved 정상화 (안전 default)
        };
        bool jumpPressed = (input & JumpBit) != 0;
        return (inputX, jumpPressed, valid);
    }
}
