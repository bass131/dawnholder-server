using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps;

/// <summary>
/// 디스크에서 맵 바이너리 파일을 로드해 MapTerrain / MapContent를 반환.
///
/// 파일 위치: {AppContext.BaseDirectory}/Maps/map_{id}.terrain.bin, map_{id}.content.bin
///
/// 플레이 맵(Town/HuntingGround/BossRoom) 파일 부재 → startup hard error (fail loud).
/// Ending 맵은 지형/콘텐츠 없는 "의도된 빈 맵"으로 명시 등록.
/// </summary>
internal static class MapDataLoader
{
    /// <summary>
    /// 4맵 전부 로드해 (MapTerrain?, MapContent?) 쌍 딕셔너리로 반환.
    /// Program.cs에서 GameWorld ctor에 주입. mapsDir 파라미터는 테스트 주입용 (null = 기본 경로).
    /// </summary>
    internal static IReadOnlyDictionary<MapId, (MapTerrain? Terrain, MapContent? Content)> LoadAll(string? mapsDir = null)
    {
        mapsDir ??= Path.Combine(AppContext.BaseDirectory, "Maps");
        var result = new Dictionary<MapId, (MapTerrain?, MapContent?)>();

        // 플레이 맵 3개 — 파일 부재/검증 실패 시 명확한 메시지로 예외.
        result[MapId.Town]          = LoadPlayMap(mapsDir, MapId.Town);
        result[MapId.HuntingGround] = LoadPlayMap(mapsDir, MapId.HuntingGround);
        result[MapId.BossRoom]      = LoadPlayMap(mapsDir, MapId.BossRoom);

        // Ending — 의도된 빈 맵 (지형/콘텐츠 없음).
        result[MapId.Ending] = (null, MapContent.Empty);

        return result;
    }

    static (MapTerrain Terrain, MapContent Content) LoadPlayMap(string mapsDir, MapId mapId)
    {
        int id = (int)mapId;
        string mapName = mapId.ToString();

        string terrainPath = Path.Combine(mapsDir, $"map_{id}.terrain.bin");
        string contentPath = Path.Combine(mapsDir, $"map_{id}.content.bin");

        if (!File.Exists(terrainPath))
        {
            throw new FileNotFoundException(
                $"[MapDataLoader] 맵 '{mapName}'(id={id}) terrain 파일이 없습니다. " +
                $"경로: {terrainPath}\n" +
                "bake 후 bin 파일을 commit했는지 확인하세요.",
                terrainPath);
        }

        if (!File.Exists(contentPath))
        {
            throw new FileNotFoundException(
                $"[MapDataLoader] 맵 '{mapName}'(id={id}) content 파일이 없습니다. " +
                $"경로: {contentPath}\n" +
                "bake 후 bin 파일을 commit했는지 확인하세요.",
                contentPath);
        }

        MapTerrain terrain;
        try
        {
            byte[] terrainBytes = File.ReadAllBytes(terrainPath);
            terrain = MapDataFile.ReadTerrain(terrainBytes, id);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidDataException(
                $"[MapDataLoader] 맵 '{mapName}'(id={id}) terrain 파일 로드 실패. " +
                $"경로: {terrainPath}\n원인: {ex.Message}", ex);
        }

        MapContent content;
        try
        {
            byte[] contentBytes = File.ReadAllBytes(contentPath);
            content = MapDataFile.ReadContent(contentBytes, id);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidDataException(
                $"[MapDataLoader] 맵 '{mapName}'(id={id}) content 파일 로드 실패. " +
                $"경로: {contentPath}\n원인: {ex.Message}", ex);
        }

        return (terrain, content);
    }
}
