namespace Dawnholder.Tools.BgmComposer.Synth;

/// <summary>
/// 효과음(SFX) 합성 프리미티브 — BGM 채널 렌더러와 별개의 원샷 DSP.
///
/// 효과음의 뼈대는 두 가지뿐이다:
/// - <see cref="Tone"/>     : 지수 피치 스윕 톤 (점프 "뿅", 피격 "욱", 사망 하강)
/// - <see cref="NoiseSweep"/>: 이동하는 밴드 노이즈 (휘두름 휙, 타격 퍽, 착지 퉁)
/// 이 둘을 겹치고 파라미터(주파수 곡선·감쇠·시드)만 바꾸면 변형(A/B)이 나온다.
///
/// 톤 방침: 메이플풍 BGM(GM 사운드폰트)과 어울리도록 거친 LFSR 칩노이즈 대신
/// 필터로 다듬은 노이즈 + 사인/삼각 바디 — 부드럽고 캐주얼하게.
/// </summary>
public static class SfxKit
{
    /// <summary>지정 길이(초)의 모노 작업 버퍼.</summary>
    public static float[] Buf(double seconds, int sampleRate) =>
        new float[(int)Math.Round(seconds * sampleRate)];

    /// <summary>
    /// 지수 피치 스윕 톤을 버퍼에 더한다.
    /// 엔벨로프 = 선형 어택(attack초) × 지수 감쇠(decay = 시정수, 초).
    /// </summary>
    public static void Tone(float[] buf, int sampleRate, double f0, double f1, double amp,
                            double attack, double decay, bool triangle = false)
    {
        double dur = (double)buf.Length / sampleRate;
        double phase = 0;
        for (int i = 0; i < buf.Length; i++)
        {
            double t = (double)i / sampleRate;
            double f = f0 * Math.Pow(f1 / f0, t / dur);
            phase += f / sampleRate;
            if (phase >= 1) phase -= 1;
            double env = Math.Min(1, t / Math.Max(attack, 1e-4))
                       * Math.Exp(-Math.Max(0, t - attack) / decay);
            double raw = triangle ? 4 * Math.Abs(phase - 0.5) - 1 : Math.Sin(2 * Math.PI * phase);
            buf[i] += (float)(raw * env * amp);
        }
    }

    /// <summary>
    /// 이동 밴드 노이즈를 버퍼에 더한다. 밴드 = LP(high) − LP(low) 원폴 차감,
    /// 밴드 자체가 (low0,high0) → (low1,high1)로 지수 이동 — "휙" 스윕의 핵심.
    /// </summary>
    public static void NoiseSweep(float[] buf, int sampleRate,
                                  double low0, double high0, double low1, double high1,
                                  double amp, double attack, double decay, int seed)
    {
        double dur = (double)buf.Length / sampleRate;
        var rnd = new Random(seed); // 결정론 — 같은 시드 = 같은 소리
        double yLow = 0, yHigh = 0;
        for (int i = 0; i < buf.Length; i++)
        {
            double t = (double)i / sampleRate;
            double k = t / dur;
            double lo = low0 * Math.Pow(low1 / low0, k);
            double hi = high0 * Math.Pow(high1 / high0, k);
            double w = rnd.NextDouble() * 2 - 1;
            yHigh += (1 - Math.Exp(-2 * Math.PI * hi / sampleRate)) * (w - yHigh);
            yLow += (1 - Math.Exp(-2 * Math.PI * lo / sampleRate)) * (w - yLow);
            double env = Math.Min(1, t / Math.Max(attack, 1e-4))
                       * Math.Exp(-Math.Max(0, t - attack) / decay);
            // ×3 — 차감 밴드의 에너지 손실 보상
            buf[i] += (float)((yHigh - yLow) * env * amp * 3.0);
        }
    }

    /// <summary>마무리: 피크 정규화 + 끝 5ms 페이드(클릭 방지) + 스테레오 인터리브.</summary>
    public static float[] Finish(float[] mono, int sampleRate, double peak = 0.80)
    {
        float max = 1e-9f;
        foreach (var s in mono) max = Math.Max(max, Math.Abs(s));
        float norm = (float)peak / max;

        int fade = Math.Min((int)(0.005 * sampleRate), mono.Length);
        var stereo = new float[mono.Length * 2];
        for (int i = 0; i < mono.Length; i++)
        {
            float g = norm;
            if (i >= mono.Length - fade) g *= (float)(mono.Length - i) / fade;
            stereo[i * 2] = stereo[i * 2 + 1] = mono[i] * g;
        }
        return stereo;
    }
}
