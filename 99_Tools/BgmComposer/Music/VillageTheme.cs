namespace Dawnholder.Tools.BgmComposer.Music;

/// <summary>
/// Village(초심자 마을) 테마 v2 — 레퍼런스(ShininHarbor.mp3) 신호 분석 기반 "모험" 보강.
///
/// v1 피드백: "모험 같은 느낌이 안 난다" → 레퍼런스 실측(2026-06-05 재분석)과 대조:
/// - 67 BPM 하프타임의 넓은 보폭  → v2: BPM 108→100 + 킥 1박·스네어 3박 하프타임 그루브
/// - 저역 에너지 60.8%            → v2: 베이스에 서브 옥타브 겹침 + 패드 저음 보이싱
/// - 에너지 점층 (전곡 상승 곡선)  → v2: 4마디 단위 빌드 (마림바+베이스 → +드럼·글록 → +스웰·플루트)
/// - 센트로이드 1760Hz (따뜻함)    → v2: 마림바 음역 하향 + 멜로디에 4/5도 상행 훅
///
/// 유지(마을 정체성): C 메이저 · 마림바 리드 · maj7/add9 컬러 · B파트 리디안 II 반짝임 ·
///                    A(8)+B(8) 16마디 심리스 루프 (B 끝 G(V) → C). 16마디 = 38.4초.
/// </summary>
public static class VillageTheme
{
    const int Bars = 16;
    const int DrumsStartBar = 4;    // 5마디째부터 드럼·글록 합류 (점층 1단계)
    const int BSectionStartBar = 8; // 9마디째부터 B파트 — 스웰·플루트 합류 (점층 2단계)

    // 마림바 멜로디 — 4/5도 상행 도약("출발!")을 훅으로, 프레이즈 정점은 길게 노래.
    const string Melody =
        // A — "먼 길을 나서는 발걸음" (Cadd9 | Em7 | Fmaj7 | G6 | Am7 | Fmaj7 | Dm7 | G7sus4 G7)
        "G4:0.5 C5:0.5 G5:1.5 E5:0.5 D5:1 " +   // 4도+5도 상행 훅
        "E5:1.5 G5:0.5 D5:1 B4:1 " +
        "A4:0.5 C5:0.5 F5:1.5 E5:0.5 C5:1 " +   // 4도 도약 정점
        "B4:0.5 D5:0.5 G5:2 R:1 " +             // 길게 열어두는 숨
        "A4:1 C5:0.5 D5:0.5 E5:1.5 G5:0.5 " +
        "A5:1 G5:0.5 E5:0.5 F5:1 C5:1 " +
        "D5:1.5 C5:0.5 A4:1 F4:1 " +
        "G4:1 A4:0.5 B4:0.5 D5:1 R:1 " +        // B로 차오르는 상행
        // B — "설레는 리프트" (F | G | D(리디안 II) | Am7 | F | G | C/E | G)
        "C5:0.5 F5:0.5 A5:1.5 G5:0.5 F5:1 " +
        "D5:0.5 G5:0.5 B5:1.5 A5:0.5 G5:1 " +   // 한 단 위 시퀀스 — 앞으로 나아가는 추진
        "A5:1 F#5:0.5 D5:0.5 E5:1 F#5:1 " +     // 리디안 #4(F#) — "마법" 반짝임
        "E5:0.5 G5:0.5 A5:2 G5:0.5 E5:0.5 " +
        "F5:1 E5:0.5 D5:0.5 C5:1 A4:1 " +
        "B4:0.5 C5:0.5 D5:1 E5:0.5 D5:0.5 B4:1 " +
        "C5:0.5 D5:0.5 E5:1 G5:1 C6:1 " +       // 클라이맥스 — C6까지 솟는다
        "D6:1.5 B5:0.5 G5:1 R:1";               // V 위에서 크게 닫고 숨 → 루프 머리 C로

    // 글로켄슈필 — 5마디째부터, 단(stage)이 바뀌는 길목에서만 반짝임 런.
    const string Sparkle =
        "R:4 R:4 R:4 " +
        "R:1 G5:0.5 A5:0.5 B5:0.5 D6:0.5 G6:1 " + // 4마디: 점층 1단계를 알리는 상승 런
        "R:4 R:4 R:4 " +
        "R:2 G5:0.5 A5:0.5 B5:1 " +               // 8마디: 멜로디와 함께 B로 차오름
        "F6:1 R:3 " +                             // 9마디(B 진입): 옥타브 반짝임 한 점
        "R:4 " +
        "R:2 F#6:0.5 A6:0.5 D6:1 " +              // 11마디: 리디안 반짝임 동참
        "R:4 R:4 R:4 " +
        "R:2 G6:0.5 E6:0.5 C6:1 " +               // 15마디: 클라이맥스 하강 아르페지오
        "R:4";

    // 스트링 패드 — 저음 보이싱(레퍼런스 저역 60.8% 반영)으로 곡 전체의 바닥.
    const string Pad =
        // A
        "C3+G3+D4+E4:4 " +   // Cadd9
        "E3+B3+D4+G4:4 " +   // Em7
        "F3+C4+E4+A4:4 " +   // Fmaj7
        "G3+D4+E4+B4:4 " +   // G6
        "A3+E4+G4+C5:4 " +   // Am7
        "F3+C4+E4+A4:4 " +   // Fmaj7
        "D3+A3+C4+F4:4 " +   // Dm7
        "G3+C4+D4+F4:2 G3+B3+D4+F4:2 " + // G7sus4 → G7
        // B
        "F3+C4+A4:4 " +      // F
        "G3+D4+B4:4 " +      // G
        "D3+A3+F#4:4 " +     // D (리디안 II)
        "A3+E4+C5:4 " +      // Am7
        "F3+C4+A4:4 " +      // F
        "G3+D4+B4:4 " +      // G
        "E3+C4+G4:4 " +      // C/E
        "G3+D4+B4:4";        // G — V로 열어두고 C로 루프

    // 플루트 — B파트 전체를 받치는 긴 호흡의 카운터라인 (모험의 "넓게 트이는" 공기).
    const string FluteLine =
        "A4:4 B4:4 A4:4 C5:4 " +     // 9~12마디: F→G→D→Am 공통음 중심으로 천천히
        "C5:2 A4:2 B4:2 D5:2 " +     // 13~14마디
        "C5:2 E5:2 " +               // 15마디: 클라이맥스에서 같이 열림
        "D5:2 B4:1 R:1";             // 16마디: 이끔음 → 루프 머리 C로

    // 베이스 루트 — 15마디는 C/E라 E, 16마디는 G→A(G/A) 분할.
    static readonly string[] BassRoots =
    {
        "C2", "E2", "F2", "G2", "A2", "F2", "D2", "G2",
        "F2", "G2", "D2", "A2", "F2", "G2", "E2", "G2",
    };

    // B파트 스웰 — 높은 현이 합류해 마지막 단을 들어 올린다 (root, octave 4).
    static readonly string[] SwellRoots = { "F4", "G4", "D4", "A4", "F4", "G4", "E4", "G4" };

    public static Score Build()
    {
        const double beatsPerBar = 4;

        var lead = ChannelScore.Parse("lead-marimba", new Instrument
        {
            // 마림바 근사 — 삼각파 + 빠른 감쇠. 레퍼런스 밝기(1760Hz)에 맞춰 컷오프 하향.
            Wave = Wave.Triangle, Volume = 0.30, Pan = +0.05, LowpassHz = 2300,
            Adsr = new Adsr(0.002, 0.35, 0.05, 0.12),
            GmProgram = 12, MidiVelocity = 105, // GM Marimba
        }, Melody);

        var glock = ChannelScore.Parse("glockenspiel", new Instrument
        {
            Wave = Wave.Sine, Volume = 0.09, Pan = +0.25,
            Adsr = new Adsr(0.001, 0.45, 0.05, 0.25),
            GmProgram = 9, MidiVelocity = 80, // GM Glockenspiel
        }, Sparkle);

        var pad = ChannelScore.Parse("pad-strings", new Instrument
        {
            Wave = Wave.Sine, Volume = 0.10, Pan = 0, DetuneCents = 8,
            Adsr = new Adsr(0.4, 0.3, 0.85, 0.6),
            GmProgram = 48, MidiVelocity = 55, // GM String Ensemble 1
        }, Pad);

        return new Score
        {
            Name = "village_theme",
            Bpm = 100,
            BeatsTotal = Bars * beatsPerBar,
            Channels = [lead, glock, pad, BuildBass(), BuildFlute(), BuildSwell(), BuildDrums()],
            // 하프타임 보폭에 맞춘 따뜻하고 긴 잔향
            EchoBeats = 0.75, EchoFeedback = 0.25, EchoMix = 0.13,
        };
    }

    /// <summary>베이스 — 서브 옥타브 겹침 + 긴 루트 (레퍼런스 저역 60.8%의 무게), 마디 끝만 바운스.</summary>
    static ChannelScore BuildBass()
    {
        var events = new List<NoteEvent>();
        for (int bar = 0; bar < Bars; bar++)
        {
            int r = ChannelScore.PitchToMidi(BassRoots[bar]);
            double b = bar * 4;
            if (bar == Bars - 1)
            {
                // 16마디 후반은 G/A — 베이스만 A로 올라가 루프 머리(C)를 당긴다
                events.Add(new NoteEvent(b, 1.75, [r - 12, r]));
                events.Add(new NoteEvent(b + 2, 0.75, [r - 10, r + 2]));
                events.Add(new NoteEvent(b + 3, 0.5, [r + 2]));
                events.Add(new NoteEvent(b + 3.5, 0.5, [r + 14]));
                continue;
            }
            events.Add(new NoteEvent(b, 1.75, [r - 12, r]));      // 묵직하게 깔리는 루트+서브
            events.Add(new NoteEvent(b + 2, 0.75, [r + 7]));
            events.Add(new NoteEvent(b + 3, 0.5, [r]));
            events.Add(new NoteEvent(b + 3.5, 0.5, [r + 12]));    // 다음 마디로 들어 올리는 바운스
        }
        return new ChannelScore
        {
            Name = "bass-deep",
            Instrument = new Instrument
            {
                Wave = Wave.Pluck, Volume = 0.50, Pan = -0.05, PluckDamp = 0.9988,
                GmProgram = 32, MidiVelocity = 100, // GM Acoustic Bass — 피치카토보다 두툼
            },
            Events = events,
        };
    }

    /// <summary>플루트 — B파트(9마디~) 전체를 긴 호흡으로 받치는 카운터라인.</summary>
    static ChannelScore BuildFlute()
    {
        string notation = string.Concat(Enumerable.Repeat("R:4 ", BSectionStartBar)) + FluteLine;
        return ChannelScore.Parse("flute-counter", new Instrument
        {
            Wave = Wave.Flute, Volume = 0.13, Pan = -0.18,
            Adsr = new Adsr(0.06, 0.12, 0.85, 0.25),
            VibratoDepth = 0.004, VibratoRate = 4.8, VibratoDelay = 0.30,
            GmProgram = 73, MidiVelocity = 80, // GM Flute
        }, notation);
    }

    /// <summary>스트링 스웰 — B파트에서만 높은 현이 합류 (레퍼런스의 점층 마지막 단).</summary>
    static ChannelScore BuildSwell()
    {
        var events = new List<NoteEvent>();
        for (int bar = BSectionStartBar; bar < Bars; bar++)
        {
            int r = ChannelScore.PitchToMidi(SwellRoots[bar - BSectionStartBar]);
            events.Add(new NoteEvent(bar * 4, 4, [r, r + 7]));
        }
        return new ChannelScore
        {
            Name = "swell",
            Instrument = new Instrument
            {
                Wave = Wave.Sine, Volume = 0.05, Pan = +0.15, DetuneCents = 9,
                Adsr = new Adsr(0.8, 0.4, 0.9, 0.8),
                GmProgram = 49, MidiVelocity = 45, // GM String Ensemble 2
            },
            Events = events,
        };
    }

    /// <summary>드럼 — 5마디째 합류. 하프타임(킥 1박·스네어 3박)으로 보폭을 넓힌다. B파트만 가벼운 보강 킥.</summary>
    static ChannelScore BuildDrums()
    {
        const string barA = "K:0.5 H:0.5 R:0.5 H:0.5 S:0.5 H:0.5 R:0.5 H:0.5 ";
        const string barB = "K:0.5 H:0.5 H:0.5 k:0.5 S:0.5 H:0.5 k:0.5 H:0.5 ";
        string notation =
            string.Concat(Enumerable.Repeat("R:4 ", DrumsStartBar)) +
            string.Concat(Enumerable.Repeat(barA, BSectionStartBar - DrumsStartBar)) +
            string.Concat(Enumerable.Repeat(barB, Bars - BSectionStartBar));
        return ChannelScore.Parse("drums", new Instrument
        {
            Wave = Wave.Drum, Volume = 0.12, Pan = -0.03, MidiVelocity = 68,
        }, notation);
    }
}
