using Dawnholder.Tools.BgmComposer.Synth;

namespace Dawnholder.Tools.BgmComposer.Music;

/// <summary>
/// 핵심 효과음 14개 — 팀장 발주 9종 (공격/피격 5종은 A/B 변형 2개씩).
///
/// 모두 0.2~0.9초 원샷 + StageClear 징글(4.8초)만 미니 Score로 작곡.
/// 변형(A/B)은 같은 레시피에서 주파수 곡선·길이·시드만 흔든 것 —
/// 연타로 겹쳐 재생될 때 기계적 반복감을 깨는 용도.
/// </summary>
public static class SfxLibrary
{
    public static IEnumerable<(string Name, float[] Stereo)> BuildAll(int sr)
    {
        // ── 1. 전사 공격 — 근접 휘두름 "휙" (상승/하강 스윕 한 쌍) ──
        var m = SfxKit.Buf(0.30, sr);
        SfxKit.NoiseSweep(m, sr, 400, 1200, 2200, 5500, 0.9, 0.05, 0.07, seed: 11);
        SfxKit.Tone(m, sr, 500, 900, 0.12, 0.04, 0.08);
        yield return ("AttackWarriorA", SfxKit.Finish(m, sr));

        m = SfxKit.Buf(0.26, sr);
        SfxKit.NoiseSweep(m, sr, 2200, 5500, 600, 1600, 0.9, 0.04, 0.06, seed: 12);
        yield return ("AttackWarriorB", SfxKit.Finish(m, sr));

        // ── 2. 레인저 공격 — 시위 "팅" + 화살 바람소리 ──
        m = SfxKit.Buf(0.30, sr);
        SfxKit.Tone(m, sr, 1700, 1100, 0.55, 0.002, 0.05, triangle: true);
        SfxKit.NoiseSweep(m, sr, 2500, 7000, 1200, 3500, 0.4, 0.002, 0.08, seed: 21);
        yield return ("AttackRangerA", SfxKit.Finish(m, sr));

        m = SfxKit.Buf(0.26, sr);
        SfxKit.Tone(m, sr, 1400, 900, 0.55, 0.002, 0.045, triangle: true);
        SfxKit.NoiseSweep(m, sr, 2200, 6000, 1000, 3000, 0.4, 0.002, 0.07, seed: 22);
        yield return ("AttackRangerB", SfxKit.Finish(m, sr));

        // ── 3. 몬스터 공격 — 으르렁 + 무는 스냅 ──
        m = SfxKit.Buf(0.38, sr);
        SfxKit.Tone(m, sr, 260, 100, 0.6, 0.01, 0.12, triangle: true);
        SfxKit.NoiseSweep(m, sr, 120, 700, 70, 280, 0.5, 0.01, 0.10, seed: 31);
        yield return ("MonsterAttackA", SfxKit.Finish(m, sr));

        m = SfxKit.Buf(0.33, sr);
        SfxKit.Tone(m, sr, 330, 130, 0.6, 0.008, 0.10, triangle: true);
        SfxKit.NoiseSweep(m, sr, 150, 850, 90, 340, 0.5, 0.008, 0.09, seed: 32);
        yield return ("MonsterAttackB", SfxKit.Finish(m, sr));

        // ── 4. 플레이어 피격 — "욱" 톤 낙하 + 짧은 타격 노이즈 ──
        m = SfxKit.Buf(0.30, sr);
        SfxKit.Tone(m, sr, 620, 210, 0.65, 0.003, 0.09);
        SfxKit.NoiseSweep(m, sr, 900, 3000, 700, 1800, 0.3, 0.002, 0.04, seed: 41);
        yield return ("PlayerHitA", SfxKit.Finish(m, sr));

        m = SfxKit.Buf(0.27, sr);
        SfxKit.Tone(m, sr, 520, 170, 0.65, 0.003, 0.08);
        SfxKit.NoiseSweep(m, sr, 800, 2600, 600, 1500, 0.3, 0.002, 0.035, seed: 42);
        yield return ("PlayerHitB", SfxKit.Finish(m, sr));

        // ── 5. 몬스터 피격 — 마른 "퍽" (노이즈 위주, 플레이어 피격보다 가볍게) ──
        m = SfxKit.Buf(0.24, sr);
        SfxKit.NoiseSweep(m, sr, 1100, 3200, 500, 1400, 0.55, 0.002, 0.05, seed: 51);
        SfxKit.Tone(m, sr, 270, 170, 0.5, 0.002, 0.07);
        yield return ("MonsterHitA", SfxKit.Finish(m, sr));

        m = SfxKit.Buf(0.21, sr);
        SfxKit.NoiseSweep(m, sr, 1350, 3800, 600, 1600, 0.55, 0.002, 0.045, seed: 52);
        SfxKit.Tone(m, sr, 310, 190, 0.5, 0.002, 0.06);
        yield return ("MonsterHitB", SfxKit.Finish(m, sr));

        // ── 6. 몬스터 사망 — 길게 무너지는 하강 ──
        m = SfxKit.Buf(0.90, sr);
        SfxKit.Tone(m, sr, 420, 65, 0.55, 0.01, 0.35, triangle: true);
        SfxKit.NoiseSweep(m, sr, 700, 2200, 90, 350, 0.35, 0.01, 0.30, seed: 61);
        yield return ("MonsterDie", SfxKit.Finish(m, sr));

        // ── 7. 점프 — 가벼운 상승 "뿅" ──
        m = SfxKit.Buf(0.22, sr);
        SfxKit.Tone(m, sr, 270, 620, 0.55, 0.01, 0.07);
        SfxKit.Tone(m, sr, 540, 1240, 0.15, 0.01, 0.05, triangle: true);
        yield return ("JumpStart", SfxKit.Finish(m, sr));

        // ── 8. 착지 — 낮은 "퉁" ──
        m = SfxKit.Buf(0.18, sr);
        SfxKit.Tone(m, sr, 140, 55, 0.65, 0.002, 0.06);
        SfxKit.NoiseSweep(m, sr, 90, 450, 60, 200, 0.35, 0.002, 0.04, seed: 81);
        yield return ("JumpLand", SfxKit.Finish(m, sr));

        // ── 9. 스테이지 클리어 징글 — 보스 처치 승리 (4.8초) ──
        yield return ("StageClear", new Renderer(sr).Render(BuildStageClear()));
    }

    /// <summary>
    /// 승리 징글 — C장조 상행 아르페지오 → 높은 G 팡파르 → add9 화음 여운.
    /// 마을 세계(C장조)로 돌아오는 소리. 마지막 6박은 여백 — 에코 꼬리가
    /// 루프 머리로 감기지 않도록 침묵으로 흡수한다 (원샷 보장).
    /// </summary>
    static Score BuildStageClear()
    {
        var lead = ChannelScore.Parse("lead-marimba", new Instrument
        {
            Wave = Wave.Triangle, Volume = 0.30, Pan = +0.05, LowpassHz = 2600,
            Adsr = new Adsr(0.002, 0.35, 0.05, 0.12),
            GmProgram = 12, MidiVelocity = 105,
        }, "C5:0.5 E5:0.5 G5:0.5 C6:0.5 D6:0.5 E6:0.5 G6:3 R:6");

        var glock = ChannelScore.Parse("glockenspiel", new Instrument
        {
            Wave = Wave.Sine, Volume = 0.10, Pan = +0.22,
            Adsr = new Adsr(0.001, 0.45, 0.05, 0.25),
            GmProgram = 9, MidiVelocity = 85,
        }, "R:1 C6:0.5 E6:0.5 G6:0.5 C7:0.5 R:1 E7:2 R:6");

        var pad = ChannelScore.Parse("pad-strings", new Instrument
        {
            Wave = Wave.Sine, Volume = 0.10, Pan = 0, DetuneCents = 8,
            Adsr = new Adsr(0.10, 0.3, 0.85, 0.8),
            GmProgram = 48, MidiVelocity = 60,
        }, "R:3 C4+E4+G4+D5:5 R:4");

        var bass = ChannelScore.Parse("bass", new Instrument
        {
            Wave = Wave.Pluck, Volume = 0.42, Pan = -0.05, PluckDamp = 0.998,
            GmProgram = 32, MidiVelocity = 95,
        }, "C3:0.5 R:0.5 G2:0.5 R:0.5 C2:0.5 R:0.5 C2:3.5 R:5.5");

        var drums = ChannelScore.Parse("drums", new Instrument
        {
            Wave = Wave.Drum, Volume = 0.13, Pan = -0.03, MidiVelocity = 80,
        }, "K:0.5 H:0.5 S:0.5 H:0.5 K:0.5 H:0.5 K:0.5 S:0.5 K:8");

        return new Score
        {
            Name = "stage_clear",
            Bpm = 150,
            BeatsTotal = 12, // 6박 연주 + 6박 여백 (에코·릴리즈 흡수)
            Channels = [lead, glock, pad, bass, drums],
            EchoBeats = 0.5, EchoFeedback = 0.22, EchoMix = 0.14,
        };
    }
}
