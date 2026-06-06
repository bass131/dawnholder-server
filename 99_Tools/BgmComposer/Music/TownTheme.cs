namespace Dawnholder.Tools.BgmComposer.Music;

/// <summary>
/// Town(마을) 테마 v4 — 레퍼런스(ShininHarbor.mp3) 신호 분석 기반.
///
/// 레퍼런스 실측값과 매칭:
/// - 템포 67 BPM (자기상관 측정 66.5~68) — 느긋한 하프타임 그루브
/// - D♭장조 (Krumhansl 키 추정 r=0.907)
/// - 저역 에너지 60.8% — 베이스·패드를 두툼하게
/// - 에너지 점층 구조 — A(8마디, 성김) → B(8마디, 드럼·스트링 스웰 합류)
///
/// 편곡: 플루트 리드 + 이피아노 싱코페이션 스타브 + 묵직한 현 베이스
///       + 따뜻한 패드, B파트만 하프타임 드럼 & 스웰. 16마디 ≈ 57.3초 루프.
/// </summary>
public static class TownTheme
{
    record Chord(string Root, bool Minor);

    // 16마디: A(8) 성김 → B(8) 풍성
    static readonly Chord[] Progression =
    {
        // A: I - vi - IV - V | I - vi - ii - V
        new("Db", false), new("Bb", true), new("Gb", false), new("Ab", false),
        new("Db", false), new("Bb", true), new("Eb", true),  new("Ab", false),
        // B: IV - V - iii - vi | ii - V - I - I
        new("Gb", false), new("Ab", false), new("F", true), new("Bb", true),
        new("Eb", true),  new("Ab", false), new("Db", false), new("Db", false),
    };

    const int FullSectionStartBar = 8; // 이 마디부터 드럼·스웰 합류 (점층 구조)

    // 플루트 멜로디 — 67 BPM의 긴 호흡: 음 하나하나가 노래하도록 여백 충분히.
    const string Melody =
        // A — 잔잔한 주제
        "F4:1 Ab4:1 Bb4:1.5 Ab4:0.5 " +
        "F4:1 Db5:1.5 C5:0.5 Bb4:1 " +
        "Bb4:1 Ab4:0.5 Gb4:0.5 Ab4:2 " +
        "C5:1.5 Bb4:0.5 Ab4:2 " +
        "F4:1 Ab4:1 Db5:1.5 C5:0.5 " +
        "Bb4:1 Ab4:0.5 Bb4:0.5 F5:2 " +
        "Gb4:1 Bb4:1 Eb5:1 Db5:1 " +
        "C5:2.5 R:1.5 " +
        // B — 한 단 위에서 열리는 전개
        "Bb4:0.5 Db5:0.5 Eb5:1 Db5:1 Bb4:1 " +
        "C5:0.5 Eb5:0.5 F5:1 Eb5:1 C5:1 " +
        "Ab4:1 C5:1 F5:1.5 Eb5:0.5 " +
        "Db5:2 Bb4:1 F4:1 " +
        "Gb4:0.5 Ab4:0.5 Bb4:1 Eb5:1 Db5:1 " +
        "C5:1 Eb5:1 Ab4:1 Bb4:1 " +
        "Ab4:1 F4:0.5 Ab4:0.5 Db5:2 " +
        "Eb4:1 F4:2 R:1"; // 9도(Eb)로 살짝 물들였다 F로 — 루프 첫 음도 F4라 이음새 자연스러움

    public static Score Build()
    {
        const int bars = 16;
        const double beatsPerBar = 4;

        var lead = ChannelScore.Parse("lead-flute", new Instrument
        {
            Wave = Wave.Flute, Volume = 0.25, Pan = +0.05,
            Adsr = new Adsr(0.045, 0.12, 0.85, 0.20),
            VibratoDepth = 0.005, VibratoRate = 4.8, VibratoDelay = 0.30,
            GmProgram = 73, MidiVelocity = 100, // GM Flute
        }, Melody);

        return new Score
        {
            Name = "town_theme",
            Bpm = 67,
            BeatsTotal = bars * beatsPerBar,
            Channels = [lead, BuildEPiano(), BuildBass(), BuildPad(), BuildSwell(), BuildDrums()],
            // 따뜻한 잔향 — 하프타임에 맞는 긴 딜레이
            EchoBeats = 0.5, EchoFeedback = 0.25, EchoMix = 0.14,
        };
    }

    static (int root, int third, int fifth) Tones(Chord c, int octave)
    {
        int root = ChannelScore.PitchToMidi(c.Root + octave);
        return (root, root + (c.Minor ? 3 : 4), root + 7);
    }

    /// <summary>이피아노 — 싱코페이션 코드 스타브 (1 · 2.5 · 4박), 부드럽게 사그라드는 사인.</summary>
    static ChannelScore BuildEPiano()
    {
        var events = new List<NoteEvent>();
        for (int bar = 0; bar < Progression.Length; bar++)
        {
            var (r, t, f) = Tones(Progression[bar], 3);
            double b = bar * 4;
            events.Add(new NoteEvent(b, 1.5, [r, t, f]));
            events.Add(new NoteEvent(b + 2.5, 1, [t, f, r + 12]));
            events.Add(new NoteEvent(b + 4 - 0.5, 0.5, [f]));
        }
        return new ChannelScore
        {
            Name = "epiano",
            Instrument = new Instrument
            {
                Wave = Wave.Sine, Volume = 0.13, Pan = -0.18,
                Adsr = new Adsr(0.004, 0.55, 0.15, 0.20), // 피아노처럼 치면 사그라듦
                GmProgram = 4, MidiVelocity = 78,         // GM Electric Piano 1
            },
            Events = events,
        };
    }

    /// <summary>베이스 — 레퍼런스의 두툼한 저역(60.8%)을 받치는 묵직하고 긴 현.</summary>
    static ChannelScore BuildBass()
    {
        var events = new List<NoteEvent>();
        for (int bar = 0; bar < Progression.Length; bar++)
        {
            var (r, _, f) = Tones(Progression[bar], 2);
            double b = bar * 4;
            events.Add(new NoteEvent(b, 2, [r]));
            events.Add(new NoteEvent(b + 2, 1, [f]));
            events.Add(new NoteEvent(b + 3, 0.5, [r]));
            events.Add(new NoteEvent(b + 3.5, 0.5, [r + 12])); // 옥타브로 살짝 들어 올림
        }
        return new ChannelScore
        {
            Name = "bass",
            Instrument = new Instrument
            {
                Wave = Wave.Pluck, Volume = 0.50, Pan = +0.03, PluckDamp = 0.9988,
                GmProgram = 32, MidiVelocity = 96, // GM Acoustic Bass
            },
            Events = events,
        };
    }

    /// <summary>패드 — 곡 전체에 깔리는 따뜻한 바닥 (스프레드 보이싱).</summary>
    static ChannelScore BuildPad()
    {
        var events = new List<NoteEvent>();
        for (int bar = 0; bar < Progression.Length; bar++)
        {
            var (r, t, f) = Tones(Progression[bar], 3);
            events.Add(new NoteEvent(bar * 4, 4, [r, f, t + 12]));
        }
        return new ChannelScore
        {
            Name = "pad",
            Instrument = new Instrument
            {
                Wave = Wave.Sine, Volume = 0.09, Pan = 0, DetuneCents = 7,
                Adsr = new Adsr(0.4, 0.3, 0.85, 0.6),
                GmProgram = 48, MidiVelocity = 52, // GM String Ensemble 1
            },
            Events = events,
        };
    }

    /// <summary>스트링 스웰 — B파트(9마디~)에서만 위 성부가 합류해 점층감을 만든다.</summary>
    static ChannelScore BuildSwell()
    {
        var events = new List<NoteEvent>();
        for (int bar = FullSectionStartBar; bar < Progression.Length; bar++)
        {
            var (r, _, f) = Tones(Progression[bar], 4);
            events.Add(new NoteEvent(bar * 4, 4, [r, f]));
        }
        return new ChannelScore
        {
            Name = "swell",
            Instrument = new Instrument
            {
                Wave = Wave.Sine, Volume = 0.055, Pan = +0.15, DetuneCents = 9,
                Adsr = new Adsr(0.8, 0.4, 0.9, 0.8),
                GmProgram = 49, MidiVelocity = 45, // GM String Ensemble 2
            },
            Events = events,
        };
    }

    /// <summary>드럼 — B파트만, 하프타임 (1박 킥 · 3박 스네어 · 8분 하이햇). 존재감은 은은하게.</summary>
    static ChannelScore BuildDrums()
    {
        const string bar = "K:0.5 H:0.5 H:0.5 H:0.5 S:0.5 H:0.5 H:0.5 H:0.5 ";
        var notation = string.Concat(
            Enumerable.Repeat("R:4 ", FullSectionStartBar))   // A파트는 쉼
            + string.Concat(Enumerable.Repeat(bar, Progression.Length - FullSectionStartBar));
        return ChannelScore.Parse("drums", new Instrument
        {
            Wave = Wave.Drum, Volume = 0.12, Pan = -0.03, MidiVelocity = 70,
        }, notation);
    }
}
