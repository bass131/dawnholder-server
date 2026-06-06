#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Prediction
{
    // 클라 지형 로더 — StreamingAssets/Maps/map_{id}.terrain.bin 읽기 + 맵당 1회 캐시.
    //
    // fail loud 정책: 파일 부재 / CRC 불일치는 예외 전파 (silent fallback 금지).
    // 이전 맵 지형으로 prediction하는 드리프트보다 시끄러운 실패가 진단에 유리.
    //
    // Windows standalone / Editor: streamingAssetsPath = 직접 File IO 가능.
    // Android/WebGL(StreamingAssets=compressed) 경로가 필요하면 UnityWebRequest 교체 필요.
    public static class ClientTerrainStore
    {
        static readonly Dictionary<int, MapTerrain> s_cache = new Dictionary<int, MapTerrain>();

        public static MapTerrain Load(int mapId)
        {
            if (s_cache.TryGetValue(mapId, out MapTerrain cached))
                return cached;

            string path = Path.Combine(Application.streamingAssetsPath, "Maps", $"map_{mapId}.terrain.bin");

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"[ClientTerrainStore] map_{mapId}.terrain.bin 없음 — StreamingAssets 배포 확인. 경로: {path}",
                    path);

            byte[] bytes = File.ReadAllBytes(path);

            // MapDataFile.ReadTerrain: CRC32 + mapId 불일치 시 InvalidDataException 발생.
            MapTerrain terrain = MapDataFile.ReadTerrain(bytes, mapId);

            s_cache[mapId] = terrain;
            return terrain;
        }

        // 맵 전환 이전 캐시는 유지(동일 맵 재진입 IO 0). 명시 플러시 필요 시 호출.
        public static void ClearCache() => s_cache.Clear();
    }
}
