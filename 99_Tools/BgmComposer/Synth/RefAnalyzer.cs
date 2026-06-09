using System.Diagnostics;

namespace Dawnholder.Tools.BgmComposer.Synth;

/// <summary>
/// 레퍼런스 곡 신호 분석기 — 작곡 파라미터 실측 도구.
///
/// "이런 느낌으로"를 말로 옮기는 대신 신호에서 직접 잰다 (Town v4에서 검증된 방법):
/// - 템포: 스펙트럼 플럭스(온셋 강도) 자기상관 피크 → BPM
/// - 조성: 크로마 분포 × Krumhansl-Schmuckler 프로파일 상관 → 키
/// - 밝기: 스펙트럼 센트로이드 평균 (악기 선택·로우패스 기준)
/// - 무게: 저역(&lt;200Hz) 에너지 비중 (베이스·패드 보이싱 기준)
/// - 구조: 5초 단위 에너지 곡선 (점층/항상성 — 편곡 단 설계 기준)
///
/// 입력은 mp3/ogg/wav 아무거나 — ffmpeg로 22.05kHz 모노 s16le 디코딩 후 분석.
/// 임시 프로젝트(Temp\RefAnalyzer)에서 99_Tools 정식 편입 (2026-06-06).
/// </summary>
public static class RefAnalyzer
{
    const int Rate = 22050;
    const int N = 4096;   // FFT 크기
    const int Hop = 1024;

    public static int Analyze(string audioPath)
    {
        if (!File.Exists(audioPath))
        {
            Console.Error.WriteLine($"파일 없음: {audioPath}");
            return 1;
        }

        Console.WriteLine($"=== RefAnalyzer — {Path.GetFileName(audioPath)} ===");
        double[]? pcm = Decode(audioPath);
        if (pcm is null) return 1;
        Console.WriteLine($"샘플 {pcm.Length} ({pcm.Length / (double)Rate:F1}s)");

        // ── 프레임 분석: 스펙트럼 플럭스 + 크로마 + 센트로이드 + 저역 비중 ──
        var hann = new double[N];
        for (int i = 0; i < N; i++) hann[i] = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / N);

        int frames = (pcm.Length - N) / Hop;
        if (frames < 16)
        {
            Console.Error.WriteLine("곡이 너무 짧아 분석 불가 (최소 ~3초)");
            return 1;
        }
        var flux = new double[frames];
        var chroma = new double[12];
        double centroidSum = 0, bassSum = 0, energySum = 0;
        var prevMag = new double[N / 2];
        var re = new double[N];
        var im = new double[N];

        for (int f = 0; f < frames; f++)
        {
            int off = f * Hop;
            for (int i = 0; i < N; i++) { re[i] = pcm[off + i] * hann[i]; im[i] = 0; }
            Fft(re, im);

            double fluxV = 0, cSum = 0, cWsum = 0, bass = 0, eng = 0;
            for (int b = 1; b < N / 2; b++)
            {
                double mag = Math.Sqrt(re[b] * re[b] + im[b] * im[b]);
                double freq = b * (double)Rate / N;
                double d = mag - prevMag[b];
                if (d > 0) fluxV += d;
                prevMag[b] = mag;
                cWsum += mag; cSum += mag * freq;
                eng += mag * mag;
                if (freq < 200) bass += mag * mag;
                if (freq is > 55 and < 5000)
                {
                    int midi = (int)Math.Round(69 + 12 * Math.Log2(freq / 440.0));
                    chroma[((midi % 12) + 12) % 12] += mag;
                }
            }
            flux[f] = fluxV;
            if (cWsum > 0) centroidSum += cSum / cWsum;
            bassSum += bass; energySum += eng;
        }

        // ── 템포: 플럭스 자기상관 (60~180 BPM 탐색) ──
        double fluxRate = (double)Rate / Hop;
        double mean = flux.Average();
        for (int i = 0; i < frames; i++) flux[i] -= mean;
        double bestBpm = 0, bestCorr = double.MinValue;
        var corrByBpm = new List<(double bpm, double corr)>();
        for (double bpm = 60; bpm <= 180; bpm += 0.5)
        {
            int lag = (int)Math.Round(60.0 / bpm * fluxRate);
            if (lag < 4 || lag >= frames / 2) continue;
            double c = 0;
            for (int i = 0; i + lag < frames; i++) c += flux[i] * flux[i + lag];
            c /= frames - lag;
            corrByBpm.Add((bpm, c));
            if (c > bestCorr) { bestCorr = c; bestBpm = bpm; }
        }
        Console.WriteLine($"\n추정 템포: {bestBpm:F1} BPM (자기상관 피크)");
        foreach (var (bpm, corr) in corrByBpm.OrderByDescending(x => x.corr).Take(5))
            Console.WriteLine($"  후보 {bpm,6:F1} BPM  상관 {corr:E2}");

        // ── 조성: Krumhansl-Schmuckler 키 추정 ──
        double[] majP = { 6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88 };
        double[] minP = { 6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17 };
        string[] names = { "C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B" };
        var keys = new List<(string key, double corr)>();
        for (int tonic = 0; tonic < 12; tonic++)
        {
            keys.Add(($"{names[tonic]} major", ProfileCorr(chroma, majP, tonic)));
            keys.Add(($"{names[tonic]} minor", ProfileCorr(chroma, minP, tonic)));
        }
        Console.WriteLine("\n조성 추정 (상위 4):");
        foreach (var (key, corr) in keys.OrderByDescending(k => k.corr).Take(4))
            Console.WriteLine($"  {key,-10} r={corr:F3}");

        Console.WriteLine("\n크로마 분포 (음 사용 비중):");
        double cmax = chroma.Max();
        for (int i = 0; i < 12; i++)
            Console.WriteLine($"  {names[i],-2} {new string('#', (int)(chroma[i] / cmax * 40))}");

        // ── 음색 특성 ──
        Console.WriteLine($"\n스펙트럼 센트로이드 평균: {centroidSum / frames:F0} Hz (밝기 지표)");
        Console.WriteLine($"저역(<200Hz) 에너지 비중: {bassSum / energySum * 100:F1}%");

        // ── 구조: 5초 구간 RMS ──
        Console.WriteLine("\n에너지 곡선 (5초 단위):");
        int chunk = Rate * 5;
        for (int s = 0; s + chunk <= pcm.Length; s += chunk)
        {
            double rms = 0;
            for (int i = s; i < s + chunk; i++) rms += pcm[i] * pcm[i];
            rms = Math.Sqrt(rms / chunk);
            Console.WriteLine($"  {s / Rate,4}s {new string('#', (int)(rms * 220))}");
        }
        return 0;
    }

    /// <summary>ffmpeg로 22.05kHz 모노 s16le 디코딩 (mp3/ogg/wav/m4a 전부 흡수).</summary>
    static double[]? Decode(string audioPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var a in new[] { "-v", "error", "-i", audioPath,
                                  "-f", "s16le", "-ac", "1", "-ar", Rate.ToString(), "-" })
            psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi);
        if (proc is null) { Console.Error.WriteLine("ffmpeg 실행 실패 (PATH 확인)"); return null; }

        using var ms = new MemoryStream();
        proc.StandardOutput.BaseStream.CopyTo(ms);
        string err = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0 || ms.Length < 4)
        {
            Console.Error.WriteLine($"디코딩 실패: {err.Trim()}");
            return null;
        }

        byte[] bytes = ms.ToArray();
        int total = bytes.Length / 2;
        var pcm = new double[total];
        for (int i = 0; i < total; i++)
            pcm[i] = BitConverter.ToInt16(bytes, i * 2) / 32768.0;
        return pcm;
    }

    static double ProfileCorr(double[] x, double[] prof, int rot)
    {
        double mx = x.Average(), mp = prof.Average(), num = 0, dx = 0, dp = 0;
        for (int i = 0; i < 12; i++)
        {
            double a = x[(i + rot) % 12] - mx, b = prof[i] - mp;
            num += a * b; dx += a * a; dp += b * b;
        }
        return num / Math.Sqrt(dx * dp + 1e-12);
    }

    static void Fft(double[] re, double[] im)
    {
        int n = re.Length;
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j) { (re[i], re[j]) = (re[j], re[i]); (im[i], im[j]) = (im[j], im[i]); }
        }
        for (int len = 2; len <= n; len <<= 1)
        {
            double ang = -2 * Math.PI / len;
            double wr = Math.Cos(ang), wi = Math.Sin(ang);
            for (int i = 0; i < n; i += len)
            {
                double cr = 1, ci = 0;
                for (int k = 0; k < len / 2; k++)
                {
                    int a = i + k, b = i + k + len / 2;
                    double tr = re[b] * cr - im[b] * ci;
                    double ti = re[b] * ci + im[b] * cr;
                    re[b] = re[a] - tr; im[b] = im[a] - ti;
                    re[a] += tr; im[a] += ti;
                    (cr, ci) = (cr * wr - ci * wi, cr * wi + ci * wr);
                }
            }
        }
    }
}
