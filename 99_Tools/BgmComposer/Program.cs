using Dawnholder.Tools.BgmComposer.Music;
using Dawnholder.Tools.BgmComposer.Synth;

namespace Dawnholder.Tools.BgmComposer;

/// <summary>
/// BGM 작곡 도구 — 코드 신디사이저로 16-bit풍 게임 음악을 WAV로 렌더링.
///
/// 사용법:
///   dotnet run -- town                 # Town 테마 렌더 → out/town_theme.wav
///   dotnet run -- town --out C:\dir    # 출력 폴더 지정
///
/// 산출물:
///   {name}.wav          1루프 (Unity Loop 재생용 최종본)
///   {name}_preview.wav  2루프 연속 (이음새 검청용)
/// </summary>
internal static class Program
{
    const int SampleRate = 44100;

    static int Main(string[] args)
    {
        string track = args.Length > 0 && !args[0].StartsWith("--") ? args[0].ToLowerInvariant() : "town";
        string outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "out"));
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--out") outDir = Path.GetFullPath(args[i + 1]);

        if (track == "analyze")
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("사용법: dotnet run -- analyze <오디오 파일 경로>");
                return 1;
            }
            return RefAnalyzer.Analyze(args[1]);
        }

        if (track == "sfx")
        {
            string sfxDir = Path.Combine(outDir, "sfx");
            Directory.CreateDirectory(sfxDir);
            Console.WriteLine("=== BgmComposer — SFX 14종 ===");
            foreach (var (name, pcm) in SfxLibrary.BuildAll(SampleRate))
            {
                string p = Path.Combine(sfxDir, name + ".wav");
                WavWriter.Write(p, pcm, SampleRate);
                Console.WriteLine($"→ {p} ({pcm.Length / 2.0 / SampleRate:F2}s)");
            }
            return 0;
        }

        Score? score = track switch
        {
            "town" => TownTheme.Build(),
            "village" => VillageTheme.Build(),
            "mainmenu" => MainMenuTheme.Build(),
            "hunting" => HuntingTheme.Build(),
            "boss" => BossTheme.Build(),
            _ => null,
        };
        if (score is null)
        {
            Console.Error.WriteLine($"알 수 없는 트랙: '{track}' (사용 가능: town, village, mainmenu, hunting, boss)");
            return 1;
        }

        Console.WriteLine($"=== BgmComposer — {score.Name} ===");
        Console.WriteLine($"BPM {score.Bpm} · {score.BeatsTotal}박 · 채널 {score.Channels.Count}개");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var renderer = new Renderer(SampleRate);
        float[] stereo = renderer.Render(score);
        sw.Stop();

        Directory.CreateDirectory(outDir);
        string wavPath = Path.Combine(outDir, $"{score.Name}.wav");
        string previewPath = Path.Combine(outDir, $"{score.Name}_preview.wav");
        string midPath = Path.Combine(outDir, $"{score.Name}.mid");
        WavWriter.Write(wavPath, stereo, SampleRate);
        WavWriter.Write(previewPath, stereo, SampleRate, repeat: 2);
        MidiWriter.Write(midPath, score);

        double seconds = stereo.Length / 2.0 / SampleRate;
        Console.WriteLine($"렌더 {sw.ElapsedMilliseconds}ms · 루프 길이 {seconds:F2}s");
        Console.WriteLine($"→ {wavPath}");
        Console.WriteLine($"→ {previewPath} (2루프 이음새 검청용)");
        Console.WriteLine($"→ {midPath} (사운드폰트 렌더용 — fluidsynth -ni <sf2> {score.Name}.mid -F out.wav)");
        return 0;
    }
}
