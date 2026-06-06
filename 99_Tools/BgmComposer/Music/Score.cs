namespace Dawnholder.Tools.BgmComposer.Music;

/// <summary>
/// 파형 종류.
/// - Pluck: Karplus-Strong 현 합성 (기타/하프 같은 뜯는 소리 — 어쿠스틱 포크의 핵심)
/// - Flute: 배음 쌓은 사인파 (부드러운 관악기 리드)
/// - Drum: 노트 이름(K/k/H/S)을 타악기로 해석하는 특수 채널
/// </summary>
public enum Wave { Pulse, Triangle, Sine, Noise, Pluck, Flute, Drum }

/// <summary>ADSR 엔벨로프 (초 단위 attack/decay/release + 0~1 sustain 레벨).</summary>
public sealed record Adsr(double Attack, double Decay, double Sustain, double Release);

/// <summary>채널 하나가 쓰는 악기 정의 — 파형 + 음색 파라미터.</summary>
public sealed class Instrument
{
    public Wave Wave { get; init; } = Wave.Pulse;
    /// <summary>펄스파 듀티 사이클 (0.5 = 사각파, 0.25 = 가늘고 밝은 소리).</summary>
    public double Duty { get; init; } = 0.5;
    public double Volume { get; init; } = 0.25;
    /// <summary>-1(왼쪽) ~ +1(오른쪽).</summary>
    public double Pan { get; init; } = 0.0;
    public Adsr Adsr { get; init; } = new(0.008, 0.08, 0.75, 0.10);
    /// <summary>비브라토 깊이 (주파수 비율, 0.004 ≈ ±7센트).</summary>
    public double VibratoDepth { get; init; } = 0.0;
    public double VibratoRate { get; init; } = 5.5;
    /// <summary>비브라토 시작 지연 (초) — 노트 머리는 곧게, 꼬리에 흔들림.</summary>
    public double VibratoDelay { get; init; } = 0.15;
    /// <summary>원폴 로우패스 컷오프(Hz). 0이면 필터 없음. 칩튠의 날을 다듬는 용도.</summary>
    public double LowpassHz { get; init; } = 0.0;
    /// <summary>디튠(센트). 0이 아니면 ±값으로 두 보이스를 겹쳐 16-bit 패드 공간감.</summary>
    public double DetuneCents { get; init; } = 0.0;
    /// <summary>Pluck 전용 — 현 감쇠 계수. 1에 가까울수록 길게 울림 (기타 0.996, 베이스 0.998).</summary>
    public double PluckDamp { get; init; } = 0.996;

    // ── MIDI 내보내기용 (사운드폰트 렌더 경로) ──
    /// <summary>General MIDI 프로그램 번호 (0=그랜드피아노, 73=플루트, 48=스트링 등).</summary>
    public int GmProgram { get; init; } = 0;
    /// <summary>MIDI 노트 벨로시티 (1~127). 채널 간 믹스 균형은 이 값으로 조절.</summary>
    public int MidiVelocity { get; init; } = 90;
}

/// <summary>노트 한 개 — 시작 박, 길이(박), 동시 발음 피치들(MIDI). 비어 있으면 쉼표.</summary>
public sealed record NoteEvent(double StartBeat, double DurBeats, int[] Midis);

/// <summary>채널 = 악기 1개 + 노트 시퀀스.</summary>
public sealed class ChannelScore
{
    public required string Name { get; init; }
    public required Instrument Instrument { get; init; }
    public required List<NoteEvent> Events { get; init; }

    /// <summary>
    /// 표기법 파싱: 공백 구분 토큰, 토큰 = 피치:길이.
    /// 피치 = "F4" / "Bb3" / "C#5", 화음 = "F3+A3+C4", 쉼표 = "R".
    /// Drum 채널은 K(킥)/k(약한 킥)/H(하이햇)/S(스네어).
    /// </summary>
    public static ChannelScore Parse(string name, Instrument inst, string notation)
    {
        var events = new List<NoteEvent>();
        double cursor = 0;
        foreach (var token in notation.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            int colon = token.LastIndexOf(':');
            if (colon < 0) throw new FormatException($"[{name}] 길이 없는 토큰: '{token}'");
            double dur = double.Parse(token[(colon + 1)..]);
            string pitchPart = token[..colon];
            if (!pitchPart.Equals("R", StringComparison.OrdinalIgnoreCase))
            {
                int[] midis = pitchPart.Split('+')
                    .Select(p => inst.Wave == Wave.Drum ? DrumCode(p) : PitchToMidi(p))
                    .ToArray();
                events.Add(new NoteEvent(cursor, dur, midis));
            }
            cursor += dur;
        }
        return new ChannelScore { Name = name, Instrument = inst, Events = events };
    }

    /// <summary>"F#4" → MIDI 번호. (MIDI 69 = A4 = 440Hz)</summary>
    public static int PitchToMidi(string pitch)
    {
        int[] baseSemis = { 9, 11, 0, 2, 4, 5, 7 }; // A B C D E F G
        char letter = char.ToUpperInvariant(pitch[0]);
        if (letter is < 'A' or > 'G') throw new FormatException($"피치 오류: '{pitch}'");
        int semi = baseSemis[letter - 'A'];
        int idx = 1;
        if (idx < pitch.Length && (pitch[idx] == '#' || pitch[idx] == 'b'))
        {
            semi += pitch[idx] == '#' ? 1 : -1;
            idx++;
        }
        int octave = int.Parse(pitch[idx..]);
        return 12 * (octave + 1) + semi;
    }

    static int DrumCode(string p) => p switch
    {
        "K" => 0, "k" => 1, "H" => 2, "S" => 3,
        _ => throw new FormatException($"드럼 토큰 오류: '{p}' (K/k/H/S만 허용)")
    };
}

/// <summary>곡 전체 — BPM + 루프 총 박 수 + 채널들.</summary>
public sealed class Score
{
    public required string Name { get; init; }
    public required double Bpm { get; init; }
    /// <summary>루프 한 바퀴의 총 박 수. 이 길이로 샘플 수가 고정돼 끊김 없는 반복이 보장된다.</summary>
    public required double BeatsTotal { get; init; }
    public required List<ChannelScore> Channels { get; init; }
    /// <summary>마스터 에코 — 박 단위 딜레이 (SNES풍 잔향의 핵심).</summary>
    public double EchoBeats { get; init; } = 0.5;
    public double EchoFeedback { get; init; } = 0.25;
    public double EchoMix { get; init; } = 0.18;
}
