namespace Dawnholder.Tools.BgmComposer.Synth;

/// <summary>16-bit PCM 스테레오 WAV 파일 출력.</summary>
public static class WavWriter
{
    /// <param name="interleaved">L/R 인터리브 float 버퍼 (-1 ~ +1).</param>
    /// <param name="repeat">버퍼를 몇 번 이어 쓸지 (루프 검청용 프리뷰에 사용).</param>
    public static void Write(string path, float[] interleaved, int sampleRate, int repeat = 1)
    {
        const short channels = 2;
        const short bitsPerSample = 16;
        int frames = interleaved.Length / channels * repeat;
        int dataBytes = frames * channels * (bitsPerSample / 8);
        int byteRate = sampleRate * channels * (bitsPerSample / 8);

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var w = new BinaryWriter(fs);

        w.Write("RIFF"u8);
        w.Write(36 + dataBytes);
        w.Write("WAVE"u8);
        w.Write("fmt "u8);
        w.Write(16);                          // fmt 청크 크기
        w.Write((short)1);                    // PCM
        w.Write(channels);
        w.Write(sampleRate);
        w.Write(byteRate);
        w.Write((short)(channels * bitsPerSample / 8)); // block align
        w.Write(bitsPerSample);
        w.Write("data"u8);
        w.Write(dataBytes);

        for (int r = 0; r < repeat; r++)
            foreach (var s in interleaved)
            {
                int v = (int)Math.Round(Math.Clamp(s, -1f, 1f) * short.MaxValue);
                w.Write((short)v);
            }
    }
}
