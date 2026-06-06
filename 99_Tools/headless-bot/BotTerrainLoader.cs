using Shared.GameData;

namespace Dawnholder.Tools.HeadlessBot;

// 봇 지형 로더 — 맵당 1회 로드 후 캐시.
//
// 파일 위치: {AppContext.BaseDirectory}/Maps/map_{id}.terrain.bin
// (HeadlessBot.csproj Content copy로 배포, GameServer.csproj 동일 패턴).
//
// 헌법 #1 정합: 봇은 클라 역할 시뮬레이터 — terrain.bin만 로드.
// content.bin (적/플레이어 스폰)은 서버 전용이므로 로드하지 않는다.
//
// fail loud: 파일 부재/무결성 실패 → 명확한 메시지의 예외. silent flat 금지.
// 이유: 지형 없이 Physics.Step이 flat fallback을 타면 봇 시뮬이 서버와 달라지는데,
// 이 차이가 무음으로 숨겨지면 desync 테스트 자체가 거짓 PASS가 된다.
internal static class BotTerrainLoader
{
    static readonly object s_lock = new();
    static readonly Dictionary<int, MapTerrain> s_cache = new();
    static readonly string s_mapsDir = Path.Combine(AppContext.BaseDirectory, "Maps");

    // 맵 id에 해당하는 terrain을 반환. 캐시 미스 시 디스크에서 로드.
    // terrain.bin이 없는 맵(Ending 등) = null 반환이 아닌 예외 — 봇 시나리오에서
    // 필요한 맵의 terrain이 없으면 "알 수 없는 flat 동작"보다 즉시 실패가 낫다.
    internal static MapTerrain Load(int mapId)
    {
        lock (s_lock)
        {
            if (s_cache.TryGetValue(mapId, out MapTerrain? cached))
                return cached;

            string path = Path.Combine(s_mapsDir, $"map_{mapId}.terrain.bin");

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"[BotTerrainLoader] map_{mapId}.terrain.bin 없음. " +
                    $"경로: {path}\n" +
                    "HeadlessBot.csproj Content copy 설정 또는 bake 산출물을 확인하세요.",
                    path);

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (IOException ex)
            {
                throw new IOException(
                    $"[BotTerrainLoader] map_{mapId}.terrain.bin 읽기 실패. 경로: {path}", ex);
            }

            // MapDataFile.ReadTerrain은 magic/version/mapId/payloadLen/CRC32 전부 검증.
            // 검증 실패 시 InvalidDataException — fail-closed (D3 설계 정신).
            MapTerrain terrain = MapDataFile.ReadTerrain(bytes, mapId);
            s_cache[mapId] = terrain;
            return terrain;
        }
    }
}
