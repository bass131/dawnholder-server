using Dawnholder.Tools.BgmComposer.Music;

namespace Dawnholder.Tools.BgmComposer.Synth;

/// <summary>
/// Score → 스테레오 PCM 렌더러.
///
/// 루프 이음새 처리 (핵심):
/// - 루프 길이를 샘플 수로 고정: L = round(BeatsTotal × SPB)
/// - 노트 릴리즈 꼬리가 L을 넘으면 buf[(i) % L]로 *루프 머리에 감아* 쓴다
/// - 마스터 에코는 같은 딜레이 라인으로 2패스 돌려 두 번째 패스를 취한다
///   → 에코 꼬리도 루프 머리로 자연스럽게 이어져 무한 반복 시 끊김이 없다
/// </summary>
public sealed class Renderer(int sampleRate)
{
    public int SampleRate { get; } = sampleRate;

    /// <summary>한 루프 분량의 인터리브 스테레오 버퍼(float, L*2)를 렌더링.</summary>
    public float[] Render(Score score)
    {
        double spb = SampleRate * 60.0 / score.Bpm;         // samples per beat
        int loop = (int)Math.Round(score.BeatsTotal * spb); // 루프 샘플 수 (고정)

        var master = new float[loop * 2]; // L,R 인터리브

        foreach (var ch in score.Channels)
        {
            float[] mono = RenderChannel(ch, spb, loop);
            if (ch.Instrument.LowpassHz > 0)
                LowpassWrapped(mono, ch.Instrument.LowpassHz);

            // 등파워 패닝
            double angle = (ch.Instrument.Pan + 1) * Math.PI / 4;
            float gl = (float)Math.Cos(angle);
            float gr = (float)Math.Sin(angle);
            for (int i = 0; i < loop; i++)
            {
                master[i * 2] += mono[i] * gl;
                master[i * 2 + 1] += mono[i] * gr;
            }
        }

        EchoWrapped(master, loop, (int)Math.Round(score.EchoBeats * spb),
                    (float)score.EchoFeedback, (float)score.EchoMix);

        // 소프트클립 후 -1dBFS 정규화
        for (int i = 0; i < master.Length; i++)
            master[i] = (float)Math.Tanh(master[i] * 1.1);
        float peak = 1e-9f;
        foreach (var s in master) peak = Math.Max(peak, Math.Abs(s));
        float norm = 0.891f / peak;
        for (int i = 0; i < master.Length; i++) master[i] *= norm;

        return master;
    }

    float[] RenderChannel(ChannelScore ch, double spb, int loop)
    {
        var buf = new float[loop];
        var inst = ch.Instrument;
        foreach (var ev in ch.Events)
        {
            int s0 = (int)Math.Round(ev.StartBeat * spb);
            if (inst.Wave == Wave.Drum)
            {
                foreach (var code in ev.Midis) RenderDrum(buf, loop, s0, code, inst.Volume);
                continue;
            }
            int sustain = (int)(ev.DurBeats * spb * 0.92); // 노트 사이 약간의 숨
            foreach (var midi in ev.Midis)
            {
                double freq = 440.0 * Math.Pow(2, (midi - 69) / 12.0);
                if (inst.Wave == Wave.Pluck)
                {
                    RenderPluck(buf, loop, s0, sustain, freq, inst);
                }
                else if (inst.DetuneCents > 0)
                {
                    double r = Math.Pow(2, inst.DetuneCents / 1200.0);
                    RenderVoice(buf, loop, s0, sustain, freq * r, inst, 0.5);
                    RenderVoice(buf, loop, s0, sustain, freq / r, inst, 0.5);
                }
                else
                {
                    RenderVoice(buf, loop, s0, sustain, freq, inst, 1.0);
                }
            }
        }
        return buf;
    }

    void RenderVoice(float[] buf, int loop, int s0, int sustainSamples,
                     double freq, Instrument inst, double gain)
    {
        var (a, d, s, r) = (inst.Adsr.Attack, inst.Adsr.Decay, inst.Adsr.Sustain, inst.Adsr.Release);
        int aS = (int)(a * SampleRate), dS = (int)(d * SampleRate), rS = (int)(r * SampleRate);
        int total = sustainSamples + rS;
        double phase = 0;
        uint lfsr = 0xACE1;

        for (int i = 0; i < total; i++)
        {
            double t = (double)i / SampleRate;

            // 엔벨로프
            double env;
            if (i < aS) env = (double)i / aS;
            else if (i < aS + dS) env = 1 - (1 - s) * (i - aS) / dS;
            else if (i < sustainSamples) env = s;
            else env = s * (1 - (double)(i - sustainSamples) / rS);
            if (env <= 0) break;

            // 비브라토
            double f = freq;
            if (inst.VibratoDepth > 0 && t > inst.VibratoDelay)
            {
                double vibEnv = Math.Min(1, (t - inst.VibratoDelay) / 0.2); // 서서히 깊어짐
                f *= 1 + inst.VibratoDepth * vibEnv * Math.Sin(2 * Math.PI * inst.VibratoRate * t);
            }

            phase += f / SampleRate;
            if (phase >= 1) phase -= 1;

            double raw = inst.Wave switch
            {
                Wave.Pulse => phase < inst.Duty ? 1.0 : -1.0,
                Wave.Triangle => 4 * Math.Abs(phase - 0.5) - 1,
                Wave.Sine => Math.Sin(2 * Math.PI * phase),
                // 플루트 — 기음 + 2·3배음을 약하게 섞어 둥글고 따뜻한 관악 음색
                Wave.Flute => (Math.Sin(2 * Math.PI * phase)
                             + 0.35 * Math.Sin(4 * Math.PI * phase)
                             + 0.12 * Math.Sin(6 * Math.PI * phase)) * 0.68,
                Wave.Noise => NextNoise(ref lfsr),
                _ => 0
            };

            buf[(s0 + i) % loop] += (float)(raw * env * inst.Volume * gain);
        }
    }

    /// <summary>
    /// Karplus-Strong 현 합성 — 노이즈 버스트를 짧은 딜레이 라인 + 평균 필터로 돌리면
    /// 물리적인 현의 울림이 된다. 기타·하프 같은 "뜯는" 어쿠스틱 음색의 표준 기법.
    /// </summary>
    void RenderPluck(float[] buf, int loop, int s0, int sustainSamples, double freq, Instrument inst)
    {
        int n = Math.Max(2, (int)Math.Round(SampleRate / freq));
        var rnd = new Random(s0 * 31 + n); // 결정론
        var str = new double[n];
        for (int i = 0; i < n; i++) str[i] = rnd.NextDouble() * 2 - 1;

        // 현은 자연 감쇠하므로 엔벨로프는 게이트 역할만 — 노트 길이 + 여운 후 페이드아웃
        int tail = (int)(0.30 * SampleRate);
        int total = sustainSamples + tail;
        for (int i = 0; i < total; i++)
        {
            int j = i % n;
            double cur = str[j];
            str[j] = (cur + str[(j + 1) % n]) * 0.5 * inst.PluckDamp;

            double gate = 1.0;
            if (i > sustainSamples) gate = 1.0 - (double)(i - sustainSamples) / tail;
            buf[(s0 + i) % loop] += (float)(cur * gate * inst.Volume);
        }
    }

    /// <summary>15-bit LFSR — NES/SNES 계열 노이즈.</summary>
    static double NextNoise(ref uint lfsr)
    {
        uint bit = ((lfsr >> 0) ^ (lfsr >> 1)) & 1;
        lfsr = (lfsr >> 1) | (bit << 14);
        return (lfsr & 1) == 1 ? 1.0 : -1.0;
    }

    void RenderDrum(float[] buf, int loop, int s0, int code, double volume)
    {
        var rnd = new Random(s0 * 7919 + code); // 결정론 (같은 입력 → 같은 출력)
        switch (code)
        {
            case 0 or 1: // K / k — 사인 피치 스윕 썸프
            {
                double vol = code == 0 ? volume : volume * 0.55;
                int len = (int)(0.14 * SampleRate);
                double phase = 0;
                for (int i = 0; i < len; i++)
                {
                    double t = (double)i / SampleRate;
                    double f = 150 * Math.Pow(50.0 / 150.0, t / 0.14); // 150→50Hz
                    phase += f / SampleRate;
                    double env = Math.Exp(-t * 28);
                    buf[(s0 + i) % loop] += (float)(Math.Sin(2 * Math.PI * phase) * env * vol * 1.6);
                }
                break;
            }
            case 2: // H — 노이즈 하이햇 (1차 차분 = 간이 하이패스)
            {
                int len = (int)(0.05 * SampleRate);
                double prev = 0;
                for (int i = 0; i < len; i++)
                {
                    double t = (double)i / SampleRate;
                    double n = rnd.NextDouble() * 2 - 1;
                    double hp = n - prev; prev = n;
                    buf[(s0 + i) % loop] += (float)(hp * Math.Exp(-t * 90) * volume * 0.5);
                }
                break;
            }
            case 3: // S — 노이즈 + 톤 스네어 (가볍게)
            {
                int len = (int)(0.11 * SampleRate);
                for (int i = 0; i < len; i++)
                {
                    double t = (double)i / SampleRate;
                    double n = rnd.NextDouble() * 2 - 1;
                    double tone = Math.Sin(2 * Math.PI * 190 * t);
                    buf[(s0 + i) % loop] += (float)((n * 0.7 + tone * 0.3) * Math.Exp(-t * 35) * volume);
                }
                break;
            }
        }
    }

    /// <summary>원폴 로우패스 — 2패스로 돌려 필터 상태가 루프 경계를 넘어 이어지게 한다.</summary>
    void LowpassWrapped(float[] mono, double cutoffHz)
    {
        double alpha = 1 - Math.Exp(-2 * Math.PI * cutoffHz / SampleRate);
        double y = 0;
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < mono.Length; i++)
            {
                y += alpha * (mono[i] - y);
                if (pass == 1) mono[i] = (float)y;
            }
    }

    /// <summary>마스터 에코 — 딜레이 라인을 유지한 채 2패스, 두 번째 패스 채택 (루프 감김).</summary>
    static void EchoWrapped(float[] stereo, int loop, int delaySamples, float feedback, float mix)
    {
        if (delaySamples <= 0 || mix <= 0) return;
        for (int chan = 0; chan < 2; chan++)
        {
            var dl = new float[delaySamples];
            int idx = 0;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < loop; i++)
                {
                    float dry = stereo[i * 2 + chan];
                    float wet = dl[idx];
                    dl[idx] = dry + wet * feedback;
                    idx = (idx + 1) % delaySamples;
                    if (pass == 1) stereo[i * 2 + chan] = dry + wet * mix;
                }
        }
    }
}
