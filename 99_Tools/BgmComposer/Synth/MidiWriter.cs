using Dawnholder.Tools.BgmComposer.Music;

namespace Dawnholder.Tools.BgmComposer.Synth;

/// <summary>
/// Score → Standard MIDI File (format 0, 480 PPQ) 내보내기.
///
/// 신디사이저 직접 합성과 별개의 출력 경로 — MIDI를 FluidSynth + GM 사운드폰트로
/// 렌더하면 실제 악기 샘플 질감을 얻는다 (메이플스토리류 BGM의 제작 방식).
/// Drum 채널은 GM 표준대로 10번 채널(zero-based 9)에 매핑된다.
/// </summary>
public static class MidiWriter
{
    const int Ppq = 480; // ticks per quarter note

    // 드럼 코드(K/k/H/S) → GM 퍼커션 노트 + 벨로시티 배율
    static readonly (int note, double velScale)[] DrumMap =
    {
        (36, 1.00), // K  킥
        (36, 0.65), // k  약한 킥
        (42, 0.55), // H  클로즈드 하이햇
        (38, 0.85), // S  스네어
    };

    public static void Write(string path, Score score)
    {
        var events = new List<(long tick, byte[] data)>();
        int melodicChannel = 0;

        foreach (var ch in score.Channels)
        {
            bool isDrum = ch.Instrument.Wave == Wave.Drum;
            int midiCh = isDrum ? 9 : melodicChannel;
            if (!isDrum)
            {
                melodicChannel++;
                if (melodicChannel == 9) melodicChannel++; // 10번 채널은 드럼 전용
                // 프로그램(악기) 선택 + 팬
                events.Add((0, [(byte)(0xC0 | midiCh), (byte)ch.Instrument.GmProgram]));
            }
            events.Add((0, [(byte)(0xB0 | midiCh), 10, (byte)Math.Clamp(64 + ch.Instrument.Pan * 63, 0, 127)]));

            foreach (var ev in ch.Events)
            {
                long onTick = (long)Math.Round(ev.StartBeat * Ppq);
                long offTick = (long)Math.Round((ev.StartBeat + ev.DurBeats * 0.95) * Ppq);
                foreach (var m in ev.Midis)
                {
                    int note = isDrum ? DrumMap[m].note : m;
                    int vel = (int)Math.Clamp(ch.Instrument.MidiVelocity * (isDrum ? DrumMap[m].velScale : 1.0), 1, 127);
                    events.Add((onTick, [(byte)(0x90 | midiCh), (byte)note, (byte)vel]));
                    events.Add((offTick, [(byte)(0x80 | midiCh), (byte)note, 0]));
                }
            }
        }

        // 트랙 데이터 조립 (틱 순 정렬 → 델타 타임)
        using var track = new MemoryStream();
        WriteVar(track, 0); // 템포 메타
        int usPerQuarter = (int)Math.Round(60_000_000.0 / score.Bpm);
        track.Write([0xFF, 0x51, 0x03,
            (byte)(usPerQuarter >> 16), (byte)(usPerQuarter >> 8), (byte)usPerQuarter]);

        long prev = 0;
        foreach (var (tick, data) in events.OrderBy(e => e.tick))
        {
            WriteVar(track, tick - prev);
            track.Write(data);
            prev = tick;
        }
        // 루프 끝까지 채운 뒤 End of Track (루프 길이 보존)
        long endTick = (long)Math.Round(score.BeatsTotal * Ppq);
        WriteVar(track, Math.Max(0, endTick - prev));
        track.Write([0xFF, 0x2F, 0x00]);

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var w = new BinaryWriter(fs);
        w.Write("MThd"u8); WriteBE(w, 6); w.Write((byte)0); w.Write((byte)0); // format 0
        w.Write((byte)0); w.Write((byte)1);                                   // 1 track
        w.Write((byte)(Ppq >> 8)); w.Write((byte)(Ppq & 0xFF));
        w.Write("MTrk"u8); WriteBE(w, (int)track.Length);
        track.Position = 0; track.CopyTo(fs);
    }

    static void WriteVar(Stream s, long value)
    {
        // MIDI 가변 길이 수 (7비트 단위, 마지막 바이트만 MSB 0)
        Span<byte> tmp = stackalloc byte[5];
        int n = 0;
        do { tmp[n++] = (byte)(value & 0x7F); value >>= 7; } while (value > 0);
        for (int i = n - 1; i >= 1; i--) s.WriteByte((byte)(tmp[i] | 0x80));
        s.WriteByte(tmp[0]);
    }

    static void WriteBE(BinaryWriter w, int v)
    {
        w.Write((byte)(v >> 24)); w.Write((byte)(v >> 16));
        w.Write((byte)(v >> 8)); w.Write((byte)v);
    }
}
