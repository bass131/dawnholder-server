using System;
using System.Buffers.Binary;
using System.IO;

namespace Shared.GameData;

/// <summary>
/// terrain.bin / content.bin 파일의 Writer + Reader.
///
/// 헤더 (20바이트, 전부 LittleEndian):
///   [0 ] magic       4B  ASCII "DWMP"
///   [4 ] version     u16 = 1
///   [6 ] fileKind    u16 = 1(terrain) or 2(content)
///   [8 ] mapId       u16
///   [10] reserved    u16 = 0
///   [12] payloadLen  u32 (헤더 제외 본문 길이)
///   [16] crc32       u32 (payload 전체 CRC32)
///
/// float 직렬화: BinaryPrimitives.ReadInt32LittleEndian + BitConverter.Int32BitsToSingle
/// (netstandard2.1에 ReadSingleLittleEndian 없음 — GenPackets wire format 관례 정합).
///
/// fail-closed: 검증 실패 항목은 InvalidDataException으로 즉시 throw. silent fallback 금지.
/// </summary>
public static class MapDataFile
{
    private const int    HeaderSize      = 20;
    private const uint   Magic           = 0x504D5744u; // "DWMP" LE
    private const ushort FormatVersion   = 1;
    private const ushort KindTerrain     = 1;
    private const ushort KindContent     = 2;

    // ── CRC32 (IEEE 802.3 다항식 0xEDB88320) ──────────────────────────────────

    private static readonly uint[] s_crcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        uint[] table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? (0xEDB88320u ^ (c >> 1)) : (c >> 1);
            table[i] = c;
        }
        return table;
    }

    private static uint ComputeCrc32(byte[] data, int offset, int length)
    {
        uint crc = 0xFFFFFFFFu;
        for (int i = offset; i < offset + length; i++)
            crc = s_crcTable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    private static float ReadF32LE(byte[] buf, int offset)
        => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(
               new ReadOnlySpan<byte>(buf, offset, 4)));

    private static int WriteF32LE(byte[] buf, int offset, float value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(
            new Span<byte>(buf, offset, 4),
            BitConverter.SingleToInt32Bits(value));
        return offset + 4;
    }

    // ── 헤더 쓰기 / 읽기 ─────────────────────────────────────────────────────

    private static void WriteHeader(byte[] buf, ushort fileKind, ushort mapId,
                                    int payloadLength, uint crc32)
    {
        // magic "DWMP"
        buf[0] = (byte)'D'; buf[1] = (byte)'W'; buf[2] = (byte)'M'; buf[3] = (byte)'P';
        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(buf, 4, 2), FormatVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(buf, 6, 2), fileKind);
        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(buf, 8, 2), mapId);
        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(buf, 10, 2), 0); // reserved
        BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(buf, 12, 4), (uint)payloadLength);
        BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(buf, 16, 4), crc32);
    }

    /// <summary>
    /// 헤더 검증. 실패 항목마다 InvalidDataException — 어떤 검증이 왜 실패했는지 메시지에 명시.
    /// </summary>
    private static void ValidateHeader(byte[] data, ushort expectedKind, int expectedMapId)
    {
        if (data.Length < HeaderSize)
            throw new InvalidDataException(
                $"MapDataFile: 파일 크기 {data.Length}B가 헤더 최소 크기 {HeaderSize}B 미만.");

        // magic
        if (data[0] != 'D' || data[1] != 'W' || data[2] != 'M' || data[3] != 'P')
        {
            string found = $"0x{data[0]:X2}{data[1]:X2}{data[2]:X2}{data[3]:X2}";
            throw new InvalidDataException(
                $"MapDataFile: magic 불일치 — expected 'DWMP', found {found}.");
        }

        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(data, 4, 2));
        if (version != FormatVersion)
            throw new InvalidDataException(
                $"MapDataFile: formatVersion 불일치 — expected {FormatVersion}, found {version}.");

        ushort kind = BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(data, 6, 2));
        if (kind != expectedKind)
            throw new InvalidDataException(
                $"MapDataFile: fileKind 불일치 — expected {expectedKind}, found {kind}. " +
                "terrain 파일을 ReadContent로(또는 반대로) 읽은 것은 아닌지 확인.");

        ushort mapId = BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(data, 8, 2));
        if (mapId != (ushort)expectedMapId)
            throw new InvalidDataException(
                $"MapDataFile: mapId 불일치 — expected {expectedMapId}, found {mapId}.");

        uint payloadLen = BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(data, 12, 4));
        int actualPayload = data.Length - HeaderSize;
        if ((int)payloadLen != actualPayload)
            throw new InvalidDataException(
                $"MapDataFile: payloadLength 불일치 — 헤더 기록값 {payloadLen}B, " +
                $"실제 payload {actualPayload}B. 파일이 잘렸거나 손상됐을 가능성.");

        uint storedCrc = BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(data, 16, 4));
        uint actualCrc = ComputeCrc32(data, HeaderSize, actualPayload);
        if (storedCrc != actualCrc)
            throw new InvalidDataException(
                $"MapDataFile: CRC32 불일치 — 헤더 기록값 0x{storedCrc:X8}, " +
                $"계산값 0x{actualCrc:X8}. payload 데이터가 변조됐거나 손상됐을 가능성.");
    }

    // ── terrain ──────────────────────────────────────────────────────────────

    /// <summary>
    /// terrain payload:
    ///   solidCount  i32
    ///   solid[]     4×f32 (MinX, MinY, MaxX, MaxY)
    ///   platCount   i32
    ///   plat[]      3×f32 (Y, MinX, MaxX)
    ///   killPlaneY  f32
    /// </summary>
    public static byte[] WriteTerrain(int mapId, MapTerrain terrain)
    {
        if (terrain == null) throw new ArgumentNullException(nameof(terrain));

        ReadOnlySpan<TerrainAabb>     solids    = terrain.Solids;
        ReadOnlySpan<TerrainPlatform> platforms = terrain.Platforms;

        int payloadSize = 4                      // solidCount
                        + solids.Length * 16     // 4×f32 each
                        + 4                      // platCount
                        + platforms.Length * 12  // 3×f32 each
                        + 4;                     // killPlaneY

        byte[] buf = new byte[HeaderSize + payloadSize];

        // payload 직렬화
        int pos = HeaderSize;
        BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(buf, pos, 4), solids.Length);
        pos += 4;
        for (int i = 0; i < solids.Length; i++)
        {
            pos = WriteF32LE(buf, pos, solids[i].MinX);
            pos = WriteF32LE(buf, pos, solids[i].MinY);
            pos = WriteF32LE(buf, pos, solids[i].MaxX);
            pos = WriteF32LE(buf, pos, solids[i].MaxY);
        }
        BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(buf, pos, 4), platforms.Length);
        pos += 4;
        for (int i = 0; i < platforms.Length; i++)
        {
            pos = WriteF32LE(buf, pos, platforms[i].Y);
            pos = WriteF32LE(buf, pos, platforms[i].MinX);
            pos = WriteF32LE(buf, pos, platforms[i].MaxX);
        }
        pos = WriteF32LE(buf, pos, terrain.KillPlaneY);

        uint crc = ComputeCrc32(buf, HeaderSize, payloadSize);
        WriteHeader(buf, KindTerrain, (ushort)mapId, payloadSize, crc);

        return buf;
    }

    public static MapTerrain ReadTerrain(byte[] data, int expectedMapId)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        ValidateHeader(data, KindTerrain, expectedMapId);

        int pos = HeaderSize;

        int solidCount = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(data, pos, 4));
        pos += 4;
        // count 가드 — CRC 통과 파일이라도 구조 불일치는 InvalidDataException으로 일관 (할당 전 검증).
        // +8 = platCount(4) + killPlaneY(4) 최소 잔여.
        if (solidCount < 0 || (long)solidCount * 16 + 8 > data.Length - pos)
            throw new InvalidDataException(
                $"MapDataFile: solidCount {solidCount} 비정상 — 남은 payload {data.Length - pos}B에 수용 불가.");
        TerrainAabb[] solids = new TerrainAabb[solidCount];
        for (int i = 0; i < solidCount; i++)
        {
            float minX = ReadF32LE(data, pos);      pos += 4;
            float minY = ReadF32LE(data, pos);      pos += 4;
            float maxX = ReadF32LE(data, pos);      pos += 4;
            float maxY = ReadF32LE(data, pos);      pos += 4;
            solids[i] = new TerrainAabb(minX, minY, maxX, maxY);
        }

        int platCount = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(data, pos, 4));
        pos += 4;
        // 등호 비교 = killPlaneY까지 정확히 소진하는지 (trailing 잔여 바이트도 구조 불일치로 거부).
        if (platCount < 0 || (long)platCount * 12 + 4 != data.Length - pos)
            throw new InvalidDataException(
                $"MapDataFile: platformCount {platCount} 비정상 — 남은 payload {data.Length - pos}B와 구조 불일치.");
        TerrainPlatform[] platforms = new TerrainPlatform[platCount];
        for (int i = 0; i < platCount; i++)
        {
            float y    = ReadF32LE(data, pos);      pos += 4;
            float minX = ReadF32LE(data, pos);      pos += 4;
            float maxX = ReadF32LE(data, pos);      pos += 4;
            platforms[i] = new TerrainPlatform(y, minX, maxX);
        }

        float killPlaneY = ReadF32LE(data, pos);

        // MapTerrain ctor이 방어 복사를 해주므로 배열을 직접 전달해도 안전.
        return new MapTerrain(solids, platforms, killPlaneY);
    }

    // ── content ──────────────────────────────────────────────────────────────

    /// <summary>
    /// content payload:
    ///   playerSpawnX  f32
    ///   playerSpawnY  f32
    ///   enemyCount    i32
    ///   enemy[]       u8 kindId + f32 x + f32 y  (= 9B each)
    /// </summary>
    public static byte[] WriteContent(int mapId, MapContent content)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));

        ReadOnlySpan<EnemySpawnPoint> enemies = content.Enemies;

        int payloadSize = 4               // playerSpawnX
                        + 4               // playerSpawnY
                        + 4               // enemyCount
                        + enemies.Length * 9; // u8 + 2×f32

        byte[] buf = new byte[HeaderSize + payloadSize];

        int pos = HeaderSize;
        pos = WriteF32LE(buf, pos, content.PlayerSpawnX);
        pos = WriteF32LE(buf, pos, content.PlayerSpawnY);
        BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(buf, pos, 4), enemies.Length);
        pos += 4;
        for (int i = 0; i < enemies.Length; i++)
        {
            buf[pos] = enemies[i].KindId;  pos += 1;
            pos = WriteF32LE(buf, pos, enemies[i].X);
            pos = WriteF32LE(buf, pos, enemies[i].Y);
        }

        uint crc = ComputeCrc32(buf, HeaderSize, payloadSize);
        WriteHeader(buf, KindContent, (ushort)mapId, payloadSize, crc);

        return buf;
    }

    public static MapContent ReadContent(byte[] data, int expectedMapId)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        ValidateHeader(data, KindContent, expectedMapId);

        int pos = HeaderSize;
        float spawnX = ReadF32LE(data, pos); pos += 4;
        float spawnY = ReadF32LE(data, pos); pos += 4;

        int enemyCount = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(data, pos, 4));
        pos += 4;
        if (enemyCount < 0 || (long)enemyCount * 9 != data.Length - pos)
            throw new InvalidDataException(
                $"MapDataFile: enemyCount {enemyCount} 비정상 — 남은 payload {data.Length - pos}B와 구조 불일치.");
        EnemySpawnPoint[] enemies = new EnemySpawnPoint[enemyCount];
        for (int i = 0; i < enemyCount; i++)
        {
            byte  kindId = data[pos];         pos += 1;
            float x      = ReadF32LE(data, pos); pos += 4;
            float y      = ReadF32LE(data, pos); pos += 4;
            enemies[i] = new EnemySpawnPoint(kindId, x, y);
        }

        return new MapContent(spawnX, spawnY, enemies);
    }
}
