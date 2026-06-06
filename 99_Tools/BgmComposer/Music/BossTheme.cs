namespace Dawnholder.Tools.BgmComposer.Music;

/// <summary>
/// BossRoom(보스방) 테마 — "격렬하고 위압감 있게".
///
/// D 단조, 140 BPM. 위압의 핵심은 프리지안 ♭2(E♭) — 저현 오스티나토가
/// D-E♭-D 반음 마찰을 8분음표로 갈아대고, ♭II장화음(E♭)이 통째로 등장해
/// 화성 단위의 위압을 만든다 (마을 세계의 어휘 밖에 있는 소리 = 침입자).
///
/// 구조: A(8) 오스티나토+팀파니 위압 인트로 → B(8) 브라스 테마 → C(8) 콰이어 상승
///       → D(8) 클라이맥스(테마 옥타브 위+풀 편성) → E(8) 침묵의 재긴장 → A7 반종지 루프.
/// 40마디 = 68.6초.
/// </summary>
public static class BossTheme
{
    const int Bars = 40;
    const int ThemeStartBar = 8;    // 9마디째 — 브라스 테마
    const int ChoirStartBar = 16;   // 17마디째 — 콰이어 상승
    const int ClimaxStartBar = 24;  // 25마디째 — 클라이맥스
    const int BreakStartBar = 32;   // 33마디째 — 브라스가 빠지는 재긴장 단

    // 마디 진행 (40) — 오스티나토·팀파니·베이스가 모두 이 표를 따른다.
    static readonly string[] ChordNames =
    {
        "Dm", "Dm", "Bb", "A7", "Dm", "Eb", "Gm", "A7", // A — 위압 인트로
        "Dm", "Bb", "Gm", "A7", "Dm", "Eb", "Bb", "A7", // B — 테마
        "Gm", "Dm", "Eb", "Dm", "Gm", "Bb", "A7", "A7", // C — 상승
        "Dm", "Bb", "Eb", "A7", "Dm", "Gm", "Bb", "A7", // D — 클라이맥스
        "Dm", "Dm", "Eb", "Eb", "Gm", "Bb", "A7", "A7", // E — 재긴장 → 반종지
    };

    // 브라스 테마 B파트 (9~16마디) — 넓은 음정 도약 + ♭II 위압.
    const string ThemeB =
        "D4:1.5 F4:0.5 A4:2 " +     // Dm
        "Bb4:1.5 A4:0.5 F4:2 " +    // Bb
        "G4:1 Bb4:1 D5:2 " +        // Gm
        "C#5:2 A4:2 " +             // A7 — 이끔음
        "D5:1.5 C5:0.5 A4:2 " +     // Dm
        "Bb4:1 G4:1 Eb4:2 " +       // Eb — ♭II로 낙하, 위압의 마디
        "D4:1 F4:1 Bb4:2 " +        // Bb
        "A4:1 C#5:1 E5:2 ";         // A7

    // 브라스 테마 D파트 (25~32마디) — 옥타브 위 클라이맥스.
    const string ThemeD =
        "D5:1.5 F5:0.5 A5:2 " +     // Dm
        "Bb5:1.5 A5:0.5 F5:2 " +    // Bb
        "Eb5:1 G5:1 Bb5:2 " +       // Eb
        "C#5:1 E5:1 A5:2 " +        // A7
        "D5:1 F5:1 A5:1 D6:1 " +    // Dm — 정점 D6
        "Bb5:1 A5:1 G5:2 " +        // Gm
        "D5:1 F5:1 Bb5:2 " +        // Bb
        "C#6:2 A5:2 ";              // A7

    // 브라스 재진입 (37~40마디) — 침묵 단의 꼬리에서 루프를 조인다.
    const string ThemeE =
        "G4:1 Bb4:1 D5:2 " +        // Gm
        "Bb4:1 D5:1 F5:2 " +        // Bb
        "E5:2 C#5:2 " +             // A7
        "A4:1 C#5:1 E5:1 G5:1";     // A7 — 도미넌트를 가득 채워 Dm 루프로

    // 콰이어 — C파트(17~24)와 E파트(33~40) 장음 보이싱 (root+5도, E는 긴장 보이싱).
    static readonly string[][] ChoirC =
    {
        ["G4", "D5"], ["D4", "A4"], ["Eb4", "Bb4"], ["D4", "A4"],
        ["G4", "D5"], ["Bb4", "F5"], ["A4", "E5"], ["A4", "C#5"],
    };
    static readonly string[][] ChoirE =
    {
        ["D4", "A4"], ["D4", "A4"], ["Eb4", "Bb4"], ["Eb4", "Bb4"],
        ["G4", "D5"], ["Bb4", "F5"], ["A4", "E5"], ["A4", "C#5"],
    };

    public static Score Build()
    {
        const double beatsPerBar = 4;

        var brass = ChannelScore.Parse("brass-theme", new Instrument
        {
            // 브라스 근사 — 펄스 + 로우패스의 두꺼운 날.
            Wave = Wave.Pulse, Duty = 0.30, Volume = 0.22, Pan = +0.05, LowpassHz = 2800,
            Adsr = new Adsr(0.02, 0.10, 0.80, 0.12),
            GmProgram = 61, MidiVelocity = 112, // GM Brass Section
        },
        string.Concat(Enumerable.Repeat("R:4 ", ThemeStartBar)) + ThemeB +
        string.Concat(Enumerable.Repeat("R:4 ", ChoirStartBar - ThemeStartBar)) + ThemeD +
        string.Concat(Enumerable.Repeat("R:4 ", BreakStartBar + 4 - ClimaxStartBar - 8)) + ThemeE);

        return new Score
        {
            Name = "boss_theme",
            Bpm = 140,
            BeatsTotal = Bars * beatsPerBar,
            Channels = [BuildOstinato(), brass, BuildChoir(), BuildTimpani(), BuildBass(), BuildDrums()],
            // 격렬한 곡 — 잔향은 짧게, 마찰음이 뭉개지지 않도록
            EchoBeats = 0.375, EchoFeedback = 0.18, EchoMix = 0.09,
        };
    }

    // ── 코드표 헬퍼 — 오스티나토용 (루트 + 위/아래 이웃음) ──
    static (int root, int upper, int lower) OstinatoTones(string name)
    {
        int R(string p) => ChannelScore.PitchToMidi(p);
        return name switch
        {
            // upper = 반음/온음 위 마찰음, lower = 아래 경과음 — 프리지안 ♭2 어휘
            "Dm" => (R("D3"), R("Eb3"), R("C3")),
            "Bb" => (R("Bb2"), R("C3"), R("A2")),
            "A7" => (R("A2"), R("Bb2"), R("G2")),
            "Eb" => (R("Eb3"), R("F3"), R("D3")),
            "Gm" => (R("G2"), R("A2"), R("F2")),
            _ => throw new FormatException($"코드 오류: {name}"),
        };
    }

    /// <summary>저현 오스티나토 — 8분 기관총. D-E♭-D 반음 마찰이 위압의 바닥.</summary>
    static ChannelScore BuildOstinato()
    {
        var events = new List<NoteEvent>();
        for (int bar = 0; bar < Bars; bar++)
        {
            var (r, upper, lower) = OstinatoTones(ChordNames[bar]);
            double b = bar * 4;
            events.Add(new NoteEvent(b + 0.0, 0.5, [r]));
            events.Add(new NoteEvent(b + 0.5, 0.5, [r]));
            events.Add(new NoteEvent(b + 1.0, 0.5, [r + 12]));
            events.Add(new NoteEvent(b + 1.5, 0.5, [r]));
            events.Add(new NoteEvent(b + 2.0, 0.5, [upper]));   // 반음 위 마찰
            events.Add(new NoteEvent(b + 2.5, 0.5, [r]));
            events.Add(new NoteEvent(b + 3.0, 0.5, [lower]));
            events.Add(new NoteEvent(b + 3.5, 0.5, [r]));
        }
        return new ChannelScore
        {
            Name = "ostinato-strings",
            Instrument = new Instrument
            {
                Wave = Wave.Pluck, Volume = 0.30, Pan = -0.10, PluckDamp = 0.995,
                GmProgram = 48, MidiVelocity = 96, // GM String Ensemble 1 — 스타카토 저현
            },
            Events = events,
        };
    }

    /// <summary>콰이어 — C파트 상승과 E파트 재긴장을 장음으로 누른다.</summary>
    static ChannelScore BuildChoir()
    {
        var events = new List<NoteEvent>();
        for (int i = 0; i < 8; i++)
        {
            events.Add(new NoteEvent((ChoirStartBar + i) * 4, 4,
                ChoirC[i].Select(ChannelScore.PitchToMidi).ToArray()));
            events.Add(new NoteEvent((BreakStartBar + i) * 4, 4,
                ChoirE[i].Select(ChannelScore.PitchToMidi).ToArray()));
        }
        return new ChannelScore
        {
            Name = "choir",
            Instrument = new Instrument
            {
                Wave = Wave.Sine, Volume = 0.09, Pan = +0.10, DetuneCents = 10,
                Adsr = new Adsr(0.5, 0.3, 0.9, 0.6),
                GmProgram = 52, MidiVelocity = 72, // GM Choir Aahs
            },
            Events = events,
        };
    }

    /// <summary>팀파니 — 1박 루트 강타 + 4박 꼬리 더블 (다음 마디를 부르는 북소리).</summary>
    static ChannelScore BuildTimpani()
    {
        var events = new List<NoteEvent>();
        for (int bar = 0; bar < Bars; bar++)
        {
            var (r, _, _) = OstinatoTones(ChordNames[bar]);
            // 팀파니 음역(F1~F3)으로 정리
            while (r > ChannelScore.PitchToMidi("F3")) r -= 12;
            while (r < ChannelScore.PitchToMidi("F1")) r += 12;
            double b = bar * 4;
            events.Add(new NoteEvent(b, 1, [r]));
            events.Add(new NoteEvent(b + 3, 0.5, [r]));
            events.Add(new NoteEvent(b + 3.5, 0.5, [r]));
        }
        return new ChannelScore
        {
            Name = "timpani",
            Instrument = new Instrument
            {
                Wave = Wave.Sine, Volume = 0.16, Pan = -0.15,
                Adsr = new Adsr(0.003, 0.30, 0.10, 0.20),
                GmProgram = 47, MidiVelocity = 100, // GM Timpani
            },
            Events = events,
        };
    }

    /// <summary>콘트라베이스 — 오스티나토 아래에서 루트를 2박씩 받친다.</summary>
    static ChannelScore BuildBass()
    {
        var events = new List<NoteEvent>();
        for (int bar = 0; bar < Bars; bar++)
        {
            var (r, _, _) = OstinatoTones(ChordNames[bar]);
            int low = r - 12;
            while (low < ChannelScore.PitchToMidi("D1")) low += 12;
            double b = bar * 4;
            events.Add(new NoteEvent(b, 2, [low]));
            events.Add(new NoteEvent(b + 2, 2, [low]));
        }
        return new ChannelScore
        {
            Name = "contrabass",
            Instrument = new Instrument
            {
                Wave = Wave.Pluck, Volume = 0.34, Pan = 0, PluckDamp = 0.9988,
                GmProgram = 43, MidiVelocity = 92, // GM Contrabass
            },
            Events = events,
        };
    }

    /// <summary>드럼 — A 절제 → B·C 풀타임 → D 클라이맥스 더블 킥 → E 풀타임 유지.</summary>
    static ChannelScore BuildDrums()
    {
        const string barTense   = "K:0.5 H:0.5 R:0.5 H:0.5 S:0.5 H:0.5 R:0.5 H:0.5 ";
        const string barFull    = "K:0.5 H:0.5 K:0.5 H:0.5 S:0.5 H:0.5 k:0.5 H:0.5 ";
        const string barClimax  = "K:0.5 H:0.5 K:0.5 S:0.5 K:0.5 H:0.5 S:0.5 S:0.5 ";
        string notation =
            string.Concat(Enumerable.Repeat(barTense, ThemeStartBar)) +
            string.Concat(Enumerable.Repeat(barFull, ClimaxStartBar - ThemeStartBar)) +
            string.Concat(Enumerable.Repeat(barClimax, BreakStartBar - ClimaxStartBar)) +
            string.Concat(Enumerable.Repeat(barFull, Bars - BreakStartBar));
        return ChannelScore.Parse("drums", new Instrument
        {
            Wave = Wave.Drum, Volume = 0.15, Pan = -0.03, MidiVelocity = 88,
        }, notation);
    }
}
