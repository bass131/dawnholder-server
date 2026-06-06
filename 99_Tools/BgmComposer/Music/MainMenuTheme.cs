namespace Dawnholder.Tools.BgmComposer.Music;

/// <summary>
/// MainMenu(메인 메뉴) 테마 — "게임 정체성: 잔잔하게 시작해 모험 기대감으로".
///
/// Village 테마와 같은 세계(C 메이저)에서 출발하되 한 호흡 느리게(72 BPM).
/// 정체성 장치: Village의 4도+5도 상행 훅(G→C→G)을 뮤직박스가 1마디째부터 인용 —
/// 메뉴에서 들은 멜로디가 마을에서 다시 피어나는 구조.
///
/// 점층(모험 기대감): A(8) 뮤직박스+패드만 → B(8) 하프 아르페지오·베이스 합류 →
/// C(8) 플루트·스웰·글로켄슈필 — 마지막 G7이 열어둔 채 루프 머리(C)로.
/// 타악기 없음 — 메뉴는 바람만 부는 공기감. 24마디 = 80.0초.
/// </summary>
public static class MainMenuTheme
{
    const int Bars = 24;
    const int HarpStartBar = 8;   // 9마디째 — 하프·베이스 합류 (점층 1단계)
    const int FullStartBar = 16;  // 17마디째 — 플루트·스웰·글록 합류 (점층 2단계)

    // 뮤직박스 멜로디 — Village 훅 인용으로 시작, 여백 많게.
    const string Melody =
        // A — "타이틀 화면의 새벽" (C | Am7 | Fmaj7 | G6 | C | Em7 | Fmaj7 | G7sus4)
        "G4:1 C5:1 G5:1.5 R:0.5 " +     // Village 훅 — 느린 4도+5도 상행
        "E5:1 D5:1 C5:1.5 R:0.5 " +
        "A4:1 C5:1 F5:1.5 E5:0.5 " +
        "D5:2.5 R:1.5 " +
        "E5:1 G5:1 C5:1.5 R:0.5 " +
        "B4:1 D5:1 G4:1.5 R:0.5 " +
        "A4:1 C5:1 E5:1 F5:1 " +
        "D5:3 R:1 " +
        // B — "한 단 위에서 노래" (Am7 | Fmaj7 | C/E | G | Am7 | Dm7 | Fmaj7 | G7)
        "C5:1 E5:1 A5:1.5 R:0.5 " +
        "A5:1 G5:1 F5:1 E5:1 " +
        "G5:2 E5:1 C5:1 " +
        "D5:2.5 B4:1 R:0.5 " +
        "C5:1 E5:1 A5:1 C6:1 " +
        "D6:1.5 C6:0.5 A5:1 F5:1 " +
        "A5:1 G5:1 E5:1 C5:1 " +
        "D5:3.5 R:0.5 " +
        // C — "문이 열린다" (C | Em7 | Fmaj7 | G6 | Am7 | Fmaj7 | Dm7 | G7sus4 G7)
        "G4:0.5 C5:0.5 G5:1 E5:1 D5:1 " + // 훅 재인용 — 이번엔 빠르게 차오른다
        "E5:1 G5:1 B5:1.5 R:0.5 " +
        "A5:1 C6:1 E6:1.5 R:0.5 " +       // 클라이맥스 정점 E6
        "D6:1 B5:1 G5:2 " +
        "C6:1 A5:1 E5:1 G5:1 " +
        "A5:1 F5:1 C5:1 E5:1 " +
        "D5:1 E5:1 F5:1 A5:1 " +
        "G5:2 D5:1 B4:1";                 // 이끔음 B — 루프 머리 C를 당긴다

    // 스트링 패드 — 전곡의 바닥 (Village와 같은 저음 보이싱 어휘).
    const string Pad =
        // A
        "C3+G3+D4+E4:4 " +   // Cadd9
        "A3+E4+G4+C5:4 " +   // Am7
        "F3+C4+E4+A4:4 " +   // Fmaj7
        "G3+D4+E4+B4:4 " +   // G6
        "C3+G3+D4+E4:4 " +
        "E3+B3+D4+G4:4 " +   // Em7
        "F3+C4+E4+A4:4 " +
        "G3+C4+D4+F4:4 " +   // G7sus4
        // B
        "A3+E4+G4+C5:4 " +
        "F3+C4+E4+A4:4 " +
        "E3+C4+G4:4 " +      // C/E
        "G3+D4+B4:4 " +
        "A3+E4+G4+C5:4 " +
        "D3+A3+C4+F4:4 " +   // Dm7
        "F3+C4+E4+A4:4 " +
        "G3+B3+D4+F4:4 " +   // G7
        // C
        "C3+G3+D4+E4:4 " +
        "E3+B3+D4+G4:4 " +
        "F3+C4+E4+A4:4 " +
        "G3+D4+E4+B4:4 " +
        "A3+E4+G4+C5:4 " +
        "F3+C4+E4+A4:4 " +
        "D3+A3+C4+F4:4 " +
        "G3+C4+D4+F4:2 G3+B3+D4+F4:2"; // G7sus4 → G7 — 열어둔 채 루프

    // 플루트 — C파트(17마디~)를 긴 호흡으로 받친다.
    const string FluteLine =
        "G4:2 A4:2 B4:4 C5:4 B4:2 D5:2 " + // 17~20마디
        "C5:4 A4:4 F4:2 A4:2 B4:2 D5:2";   // 21~24마디 — 이끔음 영역에서 닫음

    // 글로켄슈필 — C파트에서만, 단의 길목 반짝임.
    const string Sparkle =
        "G5:0.5 A5:0.5 B5:0.5 D6:0.5 G6:1 R:1 " + // 17마디: 점층 2단계 알림 런
        "R:4 " +
        "R:2 A6:0.5 G6:0.5 E6:1 " +               // 19마디: 정점에 동참
        "R:4 R:4 " +
        "R:2 E6:0.5 C6:0.5 A5:1 " +               // 22마디: 하강 아르페지오
        "R:4 " +
        "R:2 G5:0.5 A5:0.5 B5:1";                 // 24마디: 루프 머리로 차오름

    // 하프 아르페지오 음형 (마디당 6음 상행-하행 + 1박 쉼) — 9마디째부터.
    static readonly string[][] HarpChords =
    {
        ["A3", "E4", "A4", "C5"], // 9  Am7
        ["F3", "C4", "A4", "C5"], // 10 Fmaj7
        ["E3", "G4", "C5", "E5"], // 11 C/E
        ["G3", "D4", "B4", "D5"], // 12 G
        ["A3", "E4", "A4", "C5"], // 13 Am7
        ["D3", "A3", "F4", "A4"], // 14 Dm7
        ["F3", "C4", "A4", "C5"], // 15 Fmaj7
        ["G3", "D4", "B4", "F5"], // 16 G7
        ["C3", "G3", "E4", "G4"], // 17 C
        ["E3", "B3", "G4", "B4"], // 18 Em7
        ["F3", "C4", "A4", "C5"], // 19 Fmaj7
        ["G3", "D4", "B4", "D5"], // 20 G6
        ["A3", "E4", "C5", "E5"], // 21 Am7
        ["F3", "C4", "A4", "C5"], // 22 Fmaj7
        ["D3", "A3", "F4", "A4"], // 23 Dm7
        ["G3", "D4", "F4", "B4"], // 24 G7
    };

    // 베이스 루트 — 9마디째부터 길게.
    static readonly string[] BassRoots =
    {
        "A2", "F2", "E2", "G2", "A2", "D2", "F2", "G2",
        "C2", "E2", "F2", "G2", "A2", "F2", "D2", "G2",
    };

    // C파트 스웰 — 높은 현 (root, +5도).
    static readonly string[] SwellRoots = { "C4", "E4", "F4", "G4", "A4", "F4", "D4", "G4" };

    public static Score Build()
    {
        const double beatsPerBar = 4;

        var lead = ChannelScore.Parse("lead-musicbox", new Instrument
        {
            // 뮤직박스 근사 — 사인 + 빠른 감쇠의 영롱함.
            Wave = Wave.Sine, Volume = 0.30, Pan = +0.05,
            Adsr = new Adsr(0.001, 0.55, 0.04, 0.30),
            GmProgram = 10, MidiVelocity = 105, // GM Music Box
        }, Melody);

        var pad = ChannelScore.Parse("pad-strings", new Instrument
        {
            Wave = Wave.Sine, Volume = 0.10, Pan = 0, DetuneCents = 8,
            Adsr = new Adsr(0.5, 0.3, 0.85, 0.7),
            GmProgram = 48, MidiVelocity = 52, // GM String Ensemble 1
        }, Pad);

        var flute = ChannelScore.Parse("flute-counter", new Instrument
        {
            Wave = Wave.Flute, Volume = 0.12, Pan = -0.18,
            Adsr = new Adsr(0.07, 0.12, 0.85, 0.30),
            VibratoDepth = 0.004, VibratoRate = 4.6, VibratoDelay = 0.35,
            GmProgram = 73, MidiVelocity = 75, // GM Flute
        }, string.Concat(Enumerable.Repeat("R:4 ", FullStartBar)) + FluteLine);

        var glock = ChannelScore.Parse("glockenspiel", new Instrument
        {
            Wave = Wave.Sine, Volume = 0.08, Pan = +0.25,
            Adsr = new Adsr(0.001, 0.45, 0.05, 0.25),
            GmProgram = 9, MidiVelocity = 78, // GM Glockenspiel
        }, string.Concat(Enumerable.Repeat("R:4 ", FullStartBar)) + Sparkle);

        return new Score
        {
            Name = "mainmenu_theme",
            Bpm = 72,
            BeatsTotal = Bars * beatsPerBar,
            Channels = [lead, pad, BuildHarp(), BuildBass(), flute, glock, BuildSwell()],
            // 느린 보폭에 맞춘 길고 따뜻한 잔향
            EchoBeats = 1.0, EchoFeedback = 0.28, EchoMix = 0.15,
        };
    }

    /// <summary>하프 — 9마디째부터 상행-하행 아르페지오 (0,1,2,3,2,1 × 0.5박 + 1박 숨).</summary>
    static ChannelScore BuildHarp()
    {
        var events = new List<NoteEvent>();
        int[] pattern = [0, 1, 2, 3, 2, 1];
        for (int bar = HarpStartBar; bar < Bars; bar++)
        {
            string[] tones = HarpChords[bar - HarpStartBar];
            for (int i = 0; i < pattern.Length; i++)
                events.Add(new NoteEvent(bar * 4 + i * 0.5, 0.5,
                    [ChannelScore.PitchToMidi(tones[pattern[i]])]));
        }
        return new ChannelScore
        {
            Name = "harp",
            Instrument = new Instrument
            {
                Wave = Wave.Pluck, Volume = 0.16, Pan = +0.18, PluckDamp = 0.997,
                GmProgram = 46, MidiVelocity = 72, // GM Orchestral Harp
            },
            Events = events,
        };
    }

    /// <summary>베이스 — 9마디째부터 루트 3박 + 5도 1박의 느린 걸음.</summary>
    static ChannelScore BuildBass()
    {
        var events = new List<NoteEvent>();
        for (int bar = HarpStartBar; bar < Bars; bar++)
        {
            int r = ChannelScore.PitchToMidi(BassRoots[bar - HarpStartBar]);
            events.Add(new NoteEvent(bar * 4, 3, [r]));
            events.Add(new NoteEvent(bar * 4 + 3, 1, [r + 7]));
        }
        return new ChannelScore
        {
            Name = "bass-soft",
            Instrument = new Instrument
            {
                Wave = Wave.Pluck, Volume = 0.38, Pan = -0.05, PluckDamp = 0.9988,
                GmProgram = 32, MidiVelocity = 80, // GM Acoustic Bass — 메뉴라 부드럽게
            },
            Events = events,
        };
    }

    /// <summary>스트링 스웰 — C파트(17마디~)에서만 높은 현이 천장을 연다.</summary>
    static ChannelScore BuildSwell()
    {
        var events = new List<NoteEvent>();
        for (int bar = FullStartBar; bar < Bars; bar++)
        {
            int r = ChannelScore.PitchToMidi(SwellRoots[bar - FullStartBar]);
            events.Add(new NoteEvent(bar * 4, 4, [r, r + 7]));
        }
        return new ChannelScore
        {
            Name = "swell",
            Instrument = new Instrument
            {
                Wave = Wave.Sine, Volume = 0.05, Pan = +0.15, DetuneCents = 9,
                Adsr = new Adsr(0.9, 0.4, 0.9, 0.9),
                GmProgram = 49, MidiVelocity = 42, // GM String Ensemble 2
            },
            Events = events,
        };
    }
}
