namespace Dawnholder.Tools.BgmComposer.Music;

/// <summary>
/// HuntingGround(사냥터) 테마 — "경쾌한 긴장감, 너무 무겁지 않게".
///
/// A 단조 = C 메이저의 나란한조 — 마을과 같은 세계의 그늘진 숲.
/// 무게 대신 추진력: 126 BPM 8분음표 리프 + 풀타임 드럼 + 옥타브 바운스 베이스.
/// 색채 장치: A'에서 나란한 장조(C)로 잠깐 볕이 들고, C파트 도리안 IV(D장화음의 F#)가
/// Village B파트 리디안 반짝임과 같은 어휘로 호응. 매 단 끝 E7 반종지 — 긴장 유지.
///
/// 구조: A(8) 리프 확립 → B(8) 플루트 멜로디 → A'(8) 장조 볕 + 자일로폰 옥타브 위
///       → C(8) 브릿지 상승 → E7 → 루프. 32마디 = 61.0초.
/// </summary>
public static class HuntingTheme
{
    const int Bars = 32;
    const int MelodyStartBar = 8;   // 9마디째 — 플루트 멜로디 합류
    const int LiftStartBar = 16;    // 17마디째 — A' 장조 볕 + 자일로폰
    const int BridgeStartBar = 24;  // 25마디째 — C 브릿지

    // 마디 진행 (32) — 리프·베이스·스탭이 모두 이 표를 따른다.
    // 코드명 → (루트, 3음 간격, 5음 간격) : E7만 장3도+단7 색.
    static readonly string[] ChordNames =
    {
        "Am", "Am", "F", "G", "Am", "F", "Dm", "E7",   // A
        "Am", "Am", "F", "G", "Am", "F", "Dm", "E7",   // B
        "C", "G", "Am", "Em", "F", "C", "D", "E7",     // A' — 장조 볕, D = 도리안 IV
        "Dm", "Em", "F", "G", "Am", "F", "D", "E7",    // C  — 단계 상승 브릿지
    };

    // 플루트 멜로디 B파트 (9~16마디) — 리프 위에서 노래하는 사냥꾼의 호흡.
    const string MelodyB =
        "E5:1 A5:1.5 G5:0.5 E5:1 " +   // Am
        "C5:1 D5:1 E5:2 " +            // Am
        "F5:1 A5:1.5 G5:0.5 F5:1 " +   // F
        "D5:2 B4:1 D5:1 " +            // G
        "C5:1 E5:1 A5:1.5 G5:0.5 " +   // Am
        "F5:1 E5:1 C5:2 " +            // F
        "D5:1 F5:1 A5:1 F5:1 " +       // Dm
        "G#5:2 B5:1 E5:1 ";            // E7 — 이끔음 G#이 긴장을 조인다

    // 플루트 멜로디 C파트 (25~32마디) — 단계 상승, 도리안 F# 반짝.
    const string MelodyC =
        "D5:1 F5:1 A5:2 " +            // Dm
        "B5:1 G5:1 E5:2 " +            // Em
        "A5:1 G5:1 F5:1 A5:1 " +       // F
        "B5:2 D6:1 B5:1 " +            // G
        "C6:1 A5:1 E5:1 G5:1 " +       // Am — 정점에서 내려오며
        "A5:1 F5:1 C5:2 " +            // F
        "F#5:1 A5:1 D5:2 " +           // D — 도리안 F# 반짝임
        "E5:1 G#5:1 B5:2";             // E7 반종지 → 루프 머리 Am

    // 글로켄슈필 — 단이 바뀌는 길목에서만 (Village Sparkle과 같은 역할).
    const string Sparkle =
        "R:4 R:4 R:4 R:4 R:4 R:4 R:4 " +
        "R:2 E6:0.5 G#6:0.5 B6:1 " +              // 8마디: B파트 진입 알림
        "R:4 R:4 R:4 R:4 R:4 R:4 R:4 " +
        "R:2 G6:0.5 E6:0.5 C6:1 " +               // 16마디: 장조 볕 예고
        "C6:0.5 D6:0.5 E6:0.5 G6:0.5 C7:1 R:1 " + // 17마디: A' 진입 상승 런
        "R:4 R:4 R:4 R:4 R:4 R:4 " +
        "R:2 F#6:0.5 A6:0.5 D6:1 " +              // 24마디: 브릿지 진입
        "R:4 R:4 R:4 R:4 R:4 R:4 R:4 " +
        "R:2 E6:0.5 D6:0.5 B5:1";                 // 32마디: 루프 머리로 낙하

    public static Score Build()
    {
        const double beatsPerBar = 4;

        var flute = ChannelScore.Parse("flute-melody", new Instrument
        {
            Wave = Wave.Flute, Volume = 0.15, Pan = -0.12,
            Adsr = new Adsr(0.04, 0.10, 0.85, 0.20),
            VibratoDepth = 0.004, VibratoRate = 5.2, VibratoDelay = 0.22,
            GmProgram = 73, MidiVelocity = 96, // GM Flute
        },
        string.Concat(Enumerable.Repeat("R:4 ", MelodyStartBar)) + MelodyB +
        string.Concat(Enumerable.Repeat("R:4 ", LiftStartBar - MelodyStartBar)) + MelodyC);

        var glock = ChannelScore.Parse("glockenspiel", new Instrument
        {
            Wave = Wave.Sine, Volume = 0.08, Pan = +0.25,
            Adsr = new Adsr(0.001, 0.40, 0.05, 0.22),
            GmProgram = 9, MidiVelocity = 80, // GM Glockenspiel
        }, Sparkle);

        return new Score
        {
            Name = "hunting_theme",
            Bpm = 126,
            BeatsTotal = Bars * beatsPerBar,
            Channels = [BuildRiff(), BuildXylo(), flute, glock, BuildStabs(), BuildBass(), BuildDrums()],
            // 빠른 보폭 — 잔향은 짧고 마른 편이 리프의 또렷함을 살린다
            EchoBeats = 0.5, EchoFeedback = 0.20, EchoMix = 0.10,
        };
    }

    // ── 코드표 헬퍼 ──
    static (int root, int third, int fifth, int seventh) Chord(string name)
    {
        // seventh < 0 이면 7음 없음. E7만 도미넌트 색(장3+단7).
        return name switch
        {
            "Am" => (ChannelScore.PitchToMidi("A3"), 3, 7, -1),
            "Dm" => (ChannelScore.PitchToMidi("D4"), 3, 7, -1),
            "Em" => (ChannelScore.PitchToMidi("E4"), 3, 7, -1),
            "F"  => (ChannelScore.PitchToMidi("F3"), 4, 7, -1),
            "G"  => (ChannelScore.PitchToMidi("G3"), 4, 7, -1),
            "C"  => (ChannelScore.PitchToMidi("C4"), 4, 7, -1),
            "D"  => (ChannelScore.PitchToMidi("D4"), 4, 7, -1), // 도리안 IV — F# 포함
            "E7" => (ChannelScore.PitchToMidi("E4"), 4, 7, 10),
            _ => throw new FormatException($"코드 오류: {name}"),
        };
    }

    /// <summary>마림바 리프 — 전곡 엔진. 코드톤 8분 음형 (르네상스풍 짜기 대신 펜타토닉 바운스).</summary>
    static ChannelScore BuildRiff()
    {
        var events = new List<NoteEvent>();
        for (int bar = 0; bar < Bars; bar++)
        {
            var (r, third, fifth, seventh) = Chord(ChordNames[bar]);
            double b = bar * 4;
            // 음형: 루트-3음-5음-3음 / 꼬리는 7음 유무에 따라 변형
            events.Add(new NoteEvent(b + 0.0, 0.5, [r]));
            events.Add(new NoteEvent(b + 0.5, 0.5, [r + third]));
            events.Add(new NoteEvent(b + 1.0, 0.5, [r + fifth]));
            events.Add(new NoteEvent(b + 1.5, 0.5, [r + third]));
            events.Add(new NoteEvent(b + 2.0, 0.5, [r + 12]));
            if (seventh > 0)
            {
                events.Add(new NoteEvent(b + 2.5, 0.5, [r + seventh]));
                events.Add(new NoteEvent(b + 3.0, 0.5, [r + fifth]));
                events.Add(new NoteEvent(b + 3.5, 0.5, [r + third]));
            }
            else
            {
                events.Add(new NoteEvent(b + 2.5, 0.5, [r + fifth]));
                events.Add(new NoteEvent(b + 3.0, 0.5, [r + third]));
                events.Add(new NoteEvent(b + 3.5, 0.5, [r]));
            }
        }
        return new ChannelScore
        {
            Name = "riff-marimba",
            Instrument = new Instrument
            {
                Wave = Wave.Triangle, Volume = 0.28, Pan = +0.05, LowpassHz = 2400,
                Adsr = new Adsr(0.002, 0.30, 0.05, 0.10),
                GmProgram = 12, MidiVelocity = 100, // GM Marimba
            },
            Events = events,
        };
    }

    /// <summary>자일로폰 — A'(17~24마디)에서 리프를 옥타브 위로 복제 (볕이 드는 단).</summary>
    static ChannelScore BuildXylo()
    {
        var events = new List<NoteEvent>();
        for (int bar = LiftStartBar; bar < BridgeStartBar; bar++)
        {
            var (r, third, fifth, _) = Chord(ChordNames[bar]);
            double b = bar * 4;
            r += 12;
            // 리프 골격만 옥타브 위에서 — 빽빽하지 않게 반 밀도
            events.Add(new NoteEvent(b + 0.0, 0.5, [r]));
            events.Add(new NoteEvent(b + 1.0, 0.5, [r + fifth]));
            events.Add(new NoteEvent(b + 2.0, 0.5, [r + 12]));
            events.Add(new NoteEvent(b + 3.0, 0.5, [r + third]));
        }
        return new ChannelScore
        {
            Name = "xylo-lift",
            Instrument = new Instrument
            {
                Wave = Wave.Triangle, Volume = 0.12, Pan = -0.20, LowpassHz = 3600,
                Adsr = new Adsr(0.001, 0.18, 0.03, 0.07),
                GmProgram = 13, MidiVelocity = 76, // GM Xylophone
            },
            Events = events,
        };
    }

    /// <summary>스트링 스탭 — 9마디째부터 오프비트(1.5박·3.5박) 짧은 화음 — 긴장의 콕콕.</summary>
    static ChannelScore BuildStabs()
    {
        var events = new List<NoteEvent>();
        for (int bar = MelodyStartBar; bar < Bars; bar++)
        {
            var (r, third, fifth, _) = Chord(ChordNames[bar]);
            double b = bar * 4;
            events.Add(new NoteEvent(b + 1.5, 0.5, [r + 12, r + third + 12]));
            events.Add(new NoteEvent(b + 3.5, 0.5, [r + fifth + 12]));
        }
        return new ChannelScore
        {
            Name = "stab-strings",
            Instrument = new Instrument
            {
                Wave = Wave.Sine, Volume = 0.08, Pan = +0.12, DetuneCents = 7,
                Adsr = new Adsr(0.01, 0.10, 0.4, 0.08),
                GmProgram = 48, MidiVelocity = 70, // GM String Ensemble 1 — 짧게 끊는 스탭
            },
            Events = events,
        };
    }

    /// <summary>베이스 — 8분 드라이브 + 옥타브 바운스. 사냥의 발걸음.</summary>
    static ChannelScore BuildBass()
    {
        var events = new List<NoteEvent>();
        for (int bar = 0; bar < Bars; bar++)
        {
            var (chordRoot, _, _, _) = Chord(ChordNames[bar]);
            int r = chordRoot - 24; // 베이스 음역으로
            while (r < ChannelScore.PitchToMidi("E1")) r += 12;
            double b = bar * 4;
            events.Add(new NoteEvent(b + 0.0, 0.5, [r]));
            events.Add(new NoteEvent(b + 0.5, 0.5, [r]));
            events.Add(new NoteEvent(b + 1.0, 0.5, [r + 12]));
            events.Add(new NoteEvent(b + 1.5, 0.5, [r]));
            events.Add(new NoteEvent(b + 2.0, 0.5, [r]));
            events.Add(new NoteEvent(b + 2.5, 0.5, [r + 7]));
            events.Add(new NoteEvent(b + 3.0, 0.5, [r + 12]));
            events.Add(new NoteEvent(b + 3.5, 0.5, [r + 7]));
        }
        return new ChannelScore
        {
            Name = "bass-drive",
            Instrument = new Instrument
            {
                Wave = Wave.Pluck, Volume = 0.46, Pan = -0.05, PluckDamp = 0.998,
                GmProgram = 32, MidiVelocity = 98, // GM Acoustic Bass
            },
            Events = events,
        };
    }

    /// <summary>드럼 — 1~4마디는 하이햇 워밍업, 이후 풀타임. 백비트가 곡의 발놀림.</summary>
    static ChannelScore BuildDrums()
    {
        const string barWarm = "K:0.5 H:0.5 H:0.5 H:0.5 k:0.5 H:0.5 H:0.5 H:0.5 ";
        const string barFull = "K:0.5 H:0.5 S:0.5 H:0.5 k:0.5 H:0.5 S:0.5 H:0.5 ";
        string notation =
            string.Concat(Enumerable.Repeat(barWarm, 4)) +
            string.Concat(Enumerable.Repeat(barFull, Bars - 4));
        return ChannelScore.Parse("drums", new Instrument
        {
            Wave = Wave.Drum, Volume = 0.13, Pan = -0.03, MidiVelocity = 76,
        }, notation);
    }
}
