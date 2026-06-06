using System;
using System.Buffers.Binary;
using System.IO;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Tests;

// MapDataFile Writer/Reader 계약 검증 (M4.4 Phase 03 스테이지 A).
//
// 케이스:
//   R) round-trip 값 동등
//   I) 무결성 검증 (CRC 변조 / magic 오염 / version 불일치 / kind 교차 / mapId 불일치 / length 불일치)
//   G) golden 헤더 오프셋
public class MapDataFileTests
{
    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    static MapTerrain SampleTerrain() => new MapTerrain(
        new[]
        {
            new TerrainAabb(-10f, -2f, 10f, 0f),
            new TerrainAabb(5f, 0f, 7f, 4f),
        },
        new[]
        {
            new TerrainPlatform(1.5f, 0f, 8f),
        },
        killPlaneY: -50f);

    static MapContent SampleContent() => new MapContent(
        playerSpawnX: 3.5f,
        playerSpawnY: 0f,
        enemies: new[]
        {
            new EnemySpawnPoint(1, 10f, 0f),
            new EnemySpawnPoint(2, -5f, 0f),
        });

    // ── R) round-trip ─────────────────────────────────────────────────────────

    // R-1: terrain round-trip — 솔리드, 발판, killPlaneY 값 동등.
    [Fact]
    public void Terrain_RoundTrip_ValuesEqual()
    {
        MapTerrain src = SampleTerrain();
        byte[] bytes = MapDataFile.WriteTerrain(1, src);
        MapTerrain dst = MapDataFile.ReadTerrain(bytes, 1);

        Assert.Equal(src.Solids.Length,    dst.Solids.Length);
        Assert.Equal(src.Platforms.Length, dst.Platforms.Length);

        for (int i = 0; i < src.Solids.Length; i++)
        {
            Assert.Equal(src.Solids[i].MinX, dst.Solids[i].MinX);
            Assert.Equal(src.Solids[i].MinY, dst.Solids[i].MinY);
            Assert.Equal(src.Solids[i].MaxX, dst.Solids[i].MaxX);
            Assert.Equal(src.Solids[i].MaxY, dst.Solids[i].MaxY);
        }
        for (int i = 0; i < src.Platforms.Length; i++)
        {
            Assert.Equal(src.Platforms[i].Y,    dst.Platforms[i].Y);
            Assert.Equal(src.Platforms[i].MinX, dst.Platforms[i].MinX);
            Assert.Equal(src.Platforms[i].MaxX, dst.Platforms[i].MaxX);
        }
        Assert.Equal(src.KillPlaneY, dst.KillPlaneY);
    }

    // R-2: content round-trip — playerSpawn, 적 스폰 목록 값 동등.
    [Fact]
    public void Content_RoundTrip_ValuesEqual()
    {
        MapContent src = SampleContent();
        byte[] bytes = MapDataFile.WriteContent(2, src);
        MapContent dst = MapDataFile.ReadContent(bytes, 2);

        Assert.Equal(src.PlayerSpawnX,    dst.PlayerSpawnX);
        Assert.Equal(src.PlayerSpawnY,    dst.PlayerSpawnY);
        Assert.Equal(src.Enemies.Length,  dst.Enemies.Length);

        for (int i = 0; i < src.Enemies.Length; i++)
        {
            Assert.Equal(src.Enemies[i].KindId, dst.Enemies[i].KindId);
            Assert.Equal(src.Enemies[i].X,      dst.Enemies[i].X);
            Assert.Equal(src.Enemies[i].Y,      dst.Enemies[i].Y);
        }
    }

    // R-3: 빈 배열 round-trip — solids=0, platforms=0, enemies=0.
    [Fact]
    public void EmptyArrays_RoundTrip_NoException()
    {
        MapTerrain emptyTerrain = new MapTerrain(
            Array.Empty<TerrainAabb>(),
            Array.Empty<TerrainPlatform>(),
            killPlaneY: float.NegativeInfinity);
        byte[] tBytes = MapDataFile.WriteTerrain(0, emptyTerrain);
        MapTerrain tDst = MapDataFile.ReadTerrain(tBytes, 0);
        Assert.Equal(0, tDst.Solids.Length);
        Assert.Equal(0, tDst.Platforms.Length);
        Assert.Equal(float.NegativeInfinity, tDst.KillPlaneY);

        MapContent emptyContent = new MapContent(0f, 0f, Array.Empty<EnemySpawnPoint>());
        byte[] cBytes = MapDataFile.WriteContent(0, emptyContent);
        MapContent cDst = MapDataFile.ReadContent(cBytes, 0);
        Assert.Equal(0, cDst.Enemies.Length);
    }

    // ── I) 무결성 검증 ────────────────────────────────────────────────────────

    // I-1: payload 1바이트 변조 → CRC 불일치 → InvalidDataException.
    [Fact]
    public void Crc32_OneByteTamper_ThrowsInvalidData()
    {
        byte[] bytes = MapDataFile.WriteTerrain(1, SampleTerrain());
        bytes[20] ^= 0xFF; // payload 첫 바이트 반전
        Assert.Throws<InvalidDataException>(() => MapDataFile.ReadTerrain(bytes, 1));
    }

    // I-2: formatVersion 필드를 99로 교체 → InvalidDataException.
    [Fact]
    public void FormatVersion_Mismatch_ThrowsInvalidData()
    {
        byte[] bytes = MapDataFile.WriteTerrain(1, SampleTerrain());
        bytes[4] = 99; // formatVersion 하위 바이트 교체
        Assert.Throws<InvalidDataException>(() => MapDataFile.ReadTerrain(bytes, 1));
    }

    // I-3: magic 첫 바이트 오염 → InvalidDataException.
    [Fact]
    public void Magic_Corrupted_ThrowsInvalidData()
    {
        byte[] bytes = MapDataFile.WriteTerrain(1, SampleTerrain());
        bytes[0] = 0x00;
        Assert.Throws<InvalidDataException>(() => MapDataFile.ReadTerrain(bytes, 1));
    }

    // I-4: terrain bytes를 ReadContent로 읽기 → fileKind 불일치 → InvalidDataException.
    [Fact]
    public void FileKind_CrossRead_TerrainAsContent_ThrowsInvalidData()
    {
        byte[] bytes = MapDataFile.WriteTerrain(1, SampleTerrain());
        Assert.Throws<InvalidDataException>(() => MapDataFile.ReadContent(bytes, 1));
    }

    // I-5: content bytes를 ReadTerrain으로 읽기 → fileKind 불일치 → InvalidDataException.
    [Fact]
    public void FileKind_CrossRead_ContentAsTerrain_ThrowsInvalidData()
    {
        byte[] bytes = MapDataFile.WriteContent(1, SampleContent());
        Assert.Throws<InvalidDataException>(() => MapDataFile.ReadTerrain(bytes, 1));
    }

    // I-6: expectedMapId 불일치 → InvalidDataException.
    [Fact]
    public void MapId_Mismatch_ThrowsInvalidData()
    {
        byte[] bytes = MapDataFile.WriteTerrain(1, SampleTerrain());
        Assert.Throws<InvalidDataException>(() => MapDataFile.ReadTerrain(bytes, expectedMapId: 2));
    }

    // I-7: payloadLength 불일치 (파일 잘림 시뮬) → InvalidDataException.
    [Fact]
    public void PayloadLength_Truncated_ThrowsInvalidData()
    {
        byte[] bytes = MapDataFile.WriteTerrain(1, SampleTerrain());
        // 헤더는 두고 payload를 절반으로 자름 (payloadLength 헤더 기록값 != 실제 길이)
        byte[] truncated = new byte[bytes.Length - 8];
        Array.Copy(bytes, truncated, truncated.Length);
        Assert.Throws<InvalidDataException>(() => MapDataFile.ReadTerrain(truncated, 1));
    }

    // CRC32 재계산 (비트 단위 — 테이블 구현과 독립이라 본체 CRC의 교차 검증도 겸함).
    static void FixCrc(byte[] bytes)
    {
        uint crc = 0xFFFFFFFFu;
        for (int i = 20; i < bytes.Length; i++)
        {
            crc ^= bytes[i];
            for (int k = 0; k < 8; k++)
                crc = (crc & 1) != 0 ? (0xEDB88320u ^ (crc >> 1)) : (crc >> 1);
        }
        BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(bytes, 16, 4), crc ^ 0xFFFFFFFFu);
    }

    // I-8: CRC까지 맞춘 비정상 solidCount(과대/음수) → 구조 가드가 InvalidDataException으로 거부.
    //   (OverflowException/ArgumentOutOfRange가 아닌 일관된 예외 계약 — reviewer 🟡 봉합)
    [Fact]
    public void SolidCount_TamperedWithValidCrc_ThrowsInvalidData()
    {
        byte[] bytes = MapDataFile.WriteTerrain(1, SampleTerrain());

        BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(bytes, 20, 4), int.MaxValue);
        FixCrc(bytes);
        Assert.Throws<InvalidDataException>(() => MapDataFile.ReadTerrain(bytes, 1));

        BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(bytes, 20, 4), -1);
        FixCrc(bytes);
        Assert.Throws<InvalidDataException>(() => MapDataFile.ReadTerrain(bytes, 1));
    }

    // I-9: CRC까지 맞춘 비정상 enemyCount → InvalidDataException. (enemyCount 오프셋 = 20+8)
    [Fact]
    public void EnemyCount_TamperedWithValidCrc_ThrowsInvalidData()
    {
        byte[] bytes = MapDataFile.WriteContent(1, SampleContent());

        BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(bytes, 28, 4), 9999);
        FixCrc(bytes);
        Assert.Throws<InvalidDataException>(() => MapDataFile.ReadContent(bytes, 1));
    }

    // ── G) golden 헤더 오프셋 ─────────────────────────────────────────────────

    // G-1: 헤더 오프셋 정합 (위 표 그대로).
    //   [0] magic = "DWMP"
    //   [4] version = 1
    //   [6] fileKind = 1 (terrain)
    //   [8] mapId = 3
    //   [10] reserved = 0
    //   [12] payloadLength = 실제 payload 크기
    //   [16] crc32 != 0 (payload 있으면 거의 항상)
    [Fact]
    public void Header_GoldenOffsets_MatchSpec()
    {
        const int mapId = 3;
        byte[] bytes = MapDataFile.WriteTerrain(mapId, SampleTerrain());

        // magic
        Assert.Equal((byte)'D', bytes[0]);
        Assert.Equal((byte)'W', bytes[1]);
        Assert.Equal((byte)'M', bytes[2]);
        Assert.Equal((byte)'P', bytes[3]);

        // formatVersion = 1 (LE)
        Assert.Equal(1, System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes, 4, 2)));

        // fileKind = 1 (terrain)
        Assert.Equal(1, System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes, 6, 2)));

        // mapId
        Assert.Equal((ushort)mapId, System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes, 8, 2)));

        // reserved = 0
        Assert.Equal(0, System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(bytes, 10, 2)));

        // payloadLength = total - header
        uint recordedLen = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(
            new ReadOnlySpan<byte>(bytes, 12, 4));
        Assert.Equal((uint)(bytes.Length - 20), recordedLen);

        // crc32 필드 위치 확인 (비교는 round-trip 테스트에서 간접 검증됨)
        uint crc = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(
            new ReadOnlySpan<byte>(bytes, 16, 4));
        Assert.NotEqual(0u, crc); // 정상 payload에서 CRC가 0일 확률 1/2^32
    }
}
