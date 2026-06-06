using System;
using System.Collections.Generic;
using System.IO;
using Shared.GameData;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Dawnholder.Client.EditorTools
{
    /// <summary>
    /// 씬 타일맵 + 마커 → terrain.bin / content.bin bake 파이프라인 (M4.4-03B).
    ///
    /// 레이어 약속: "Tilemap_Solid" = 바닥·벽 / "Tilemap_Platform" = one-way 발판.
    /// 마커 약속: "Spawn_Player"(맵당 1개 의무) / "Spawn_Enemy_Normal" / "Spawn_Enemy_Boss".
    ///
    /// 출력:
    ///   98_Shared/GameData/Maps/map_{id}.terrain.bin  — 서버 + 클라 공유
    ///   03_Client/Assets/StreamingAssets/Maps/map_{id}.terrain.bin  — 클라 전용 (byte-identical)
    ///   98_Shared/GameData/Maps/map_{id}.content.bin  — 서버 전용 (StreamingAssets 출력 X)
    ///
    /// 씬 수정 시 재bake → bin 두 벌 + 씬 동반 commit.
    /// </summary>
    public static class TerrainBaker
    {
        const string SolidTilemapName    = "Tilemap_Solid";
        const string PlatformTilemapName = "Tilemap_Platform";
        const string MarkerPlayer        = "Spawn_Player";
        const string MarkerEnemyPrefix   = "Spawn_Enemy_";

        // EnemyKind 매핑 — append-only (값 변경·제거 X, 서버 EnemyKind byte 약속).
        // 고정 배열 순회 = content.bin 항목 순서 결정론 (bake idempotent — Dictionary 순서 비보장 회피).
        static readonly (string Name, byte KindId)[] EnemyMarkers =
        {
            ("Spawn_Enemy_Normal", 0),
            ("Spawn_Enemy_Boss",   1),
        };

        static bool IsKnownEnemyMarker(string name)
        {
            foreach ((string known, _) in EnemyMarkers)
                if (known == name) return true;
            return false;
        }

        // mapId는 서버 MapId enum / 클라 SceneRouter와 정합 의무 — 맵 추가 시 세 곳 동반 갱신.
        static readonly (int MapId, string MapName, string ScenePath)[] Targets =
        {
            (0, "Town",          "Assets/Scenes/01.PlayArea/Town.unity"),
            (1, "HuntingGround", "Assets/Scenes/01.PlayArea/HuntingGround.unity"),
            (2, "BossRoom",      "Assets/Scenes/01.PlayArea/BossRoom.unity"),
        };

        // Application.dataPath = "…/03_Client/Assets"
        static string SharedMapsPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "98_Shared", "GameData", "Maps"));

        static string StreamingAssetsMapsPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "StreamingAssets", "Maps"));

        // ── 메뉴 진입점 ──────────────────────────────────────────────────────────

        [MenuItem("Tools/Dawnholder/Bake Terrain")]
        public static void Bake()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[TerrainBaker] 미저장 변경 저장 취소 — bake 중단.");
                return;
            }

            SceneSetup[] restore = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                // 1단계: 전 맵 검증 + bytes 생성 (파일 IO 없음).
                // 2단계: 전부 통과한 뒤에만 일괄 쓰기 — 중간 맵 실패 시 부분 출력(맵 간 drift) 차단.
                var outputs = new List<(int MapId, byte[] TerrainBytes, byte[] ContentBytes, string Summary)>();
                foreach ((int mapId, string mapName, string scenePath) in Targets)
                {
                    if (!TryBuildMap(mapId, mapName, scenePath,
                                     out byte[] terrainBytes, out byte[] contentBytes, out string summary))
                        return; // 오류 시 파일 미변경 상태로 중단
                    outputs.Add((mapId, terrainBytes, contentBytes, summary));
                }

                Directory.CreateDirectory(SharedMapsPath);
                Directory.CreateDirectory(StreamingAssetsMapsPath);
                foreach ((int mapId, byte[] terrainBytes, byte[] contentBytes, string summary) in outputs)
                {
                    File.WriteAllBytes(Path.Combine(SharedMapsPath,          $"map_{mapId}.terrain.bin"), terrainBytes);
                    File.WriteAllBytes(Path.Combine(StreamingAssetsMapsPath, $"map_{mapId}.terrain.bin"), terrainBytes); // byte-identical 두 벌
                    File.WriteAllBytes(Path.Combine(SharedMapsPath,          $"map_{mapId}.content.bin"), contentBytes);
                    Debug.Log(summary);
                }
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(restore);
                AssetDatabase.Refresh(); // StreamingAssets .bin + .meta Unity 인식
            }
        }

        // ── 맵 1개 검증 + bytes 생성 (파일 IO 없음 — 쓰기는 Bake()의 2단계 일괄) ──

        static bool TryBuildMap(int mapId, string mapName, string scenePath,
                                out byte[] terrainBytes, out byte[] contentBytes, out string summary)
        {
            terrainBytes = null;
            contentBytes = null;
            summary      = null;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Scene scene = EditorSceneManager.GetSceneByPath(scenePath);

            // ── 타일맵 추출 ──────────────────────────────────────────────────────

            List<Tilemap> solids = FindTilemaps(scene, SolidTilemapName);
            if (solids.Count == 0)
            {
                Debug.LogError($"[TerrainBaker] {mapName}: '{SolidTilemapName}' 없음 — " +
                               "레이어 분리(M4.4-01 약속)가 선행돼야 합니다. bake 중단 (파일 미변경).");
                return false;
            }
            List<Tilemap> platforms = FindTilemaps(scene, PlatformTilemapName);
            if (platforms.Count == 0)
                Debug.LogWarning($"[TerrainBaker] {mapName}: '{PlatformTilemapName}' 없음 — 발판 0개로 진행.");

            List<CellRect> solidRects   = ExtractSolidRects(solids);
            List<Segment>  platformSegs = ExtractPlatformSegments(platforms);

            // ── Shared 타입으로 직접 변환 ────────────────────────────────────────

            TerrainAabb[] terrainSolids = new TerrainAabb[solidRects.Count];
            for (int i = 0; i < solidRects.Count; i++)
            {
                CellRect r = solidRects[i];
                terrainSolids[i] = new TerrainAabb(r.MinX, r.MinY, r.MaxX, r.MaxY);
            }

            TerrainPlatform[] terrainPlatforms = new TerrainPlatform[platformSegs.Count];
            for (int i = 0; i < platformSegs.Count; i++)
            {
                Segment s = platformSegs[i];
                terrainPlatforms[i] = new TerrainPlatform(s.Y, s.MinX, s.MaxX);
            }

            // ── killPlaneY = min(솔리드 MinY) - 10 ──────────────────────────────

            float minSolidY = float.PositiveInfinity;
            for (int i = 0; i < terrainSolids.Length; i++)
            {
                if (terrainSolids[i].MinY < minSolidY)
                    minSolidY = terrainSolids[i].MinY;
            }
            float killPlaneY = terrainSolids.Length > 0 ? minSolidY - 10f : float.NegativeInfinity;

            var terrain = new MapTerrain(terrainSolids, terrainPlatforms, killPlaneY);

            // ── 마커 추출 + Y 스냅 검증 ──────────────────────────────────────────

            if (!CollectMarkers(scene, mapName, terrain,
                                out Vector2 playerSpawn, out List<EnemySpawnPoint> enemies))
                return false;

            // ── bytes 생성 (쓰기는 호출자 일괄) ──────────────────────────────────

            terrainBytes = MapDataFile.WriteTerrain(mapId, terrain);
            contentBytes = MapDataFile.WriteContent(mapId,
                               new MapContent(playerSpawn.x, playerSpawn.y, enemies.ToArray()));

            summary =
                $"[TerrainBaker] {mapName} (mapId={mapId}) bake 완료\n" +
                $"  solids={terrainSolids.Length}  platforms={terrainPlatforms.Length}" +
                $"  killPlaneY={killPlaneY:F2}\n" +
                $"  playerSpawn=({playerSpawn.x:F2}, {playerSpawn.y:F2})  enemies={enemies.Count}\n" +
                $"  → {Path.Combine(SharedMapsPath, $"map_{mapId}.terrain.bin")} (+ content.bin)\n" +
                $"  → {Path.Combine(StreamingAssetsMapsPath, $"map_{mapId}.terrain.bin")}\n" +
                "  씬 수정 시 재bake → bin 두 벌 + 씬 동반 commit.";

            return true;
        }

        // ── 마커 수집 + Y 스냅 검증 ──────────────────────────────────────────────

        /// <returns>성공 시 true. false 반환 시 bake 중단 (파일 미변경).</returns>
        static bool CollectMarkers(Scene scene, string mapName, MapTerrain terrain,
                                   out Vector2 playerSpawn, out List<EnemySpawnPoint> enemies)
        {
            playerSpawn = default;
            enemies     = new List<EnemySpawnPoint>();

            // unknown suffix 오타 → fail-closed
            bool hasBadMarker = false;
            foreach (GameObject root in scene.GetRootGameObjects())
                CheckUnknownEnemyMarkers(root.transform, mapName, ref hasBadMarker);
            if (hasBadMarker)
                return false; // LogError는 CheckUnknownEnemyMarkers 안에서 이미 출력

            // Spawn_Player — 정확히 1개 의무
            var playerGos = new List<GameObject>();
            CollectByName(scene, MarkerPlayer, playerGos);
            if (playerGos.Count == 0)
            {
                Debug.LogError($"[TerrainBaker] {mapName}: '{MarkerPlayer}' 마커 없음 — bake 중단.");
                return false;
            }
            if (playerGos.Count > 1)
            {
                Debug.LogError($"[TerrainBaker] {mapName}: '{MarkerPlayer}' 마커 {playerGos.Count}개 — " +
                               "정확히 1개 배치 필요. bake 중단.");
                return false;
            }

            // 비활성 마커 경고 (기존 타일맵 처리와 동일 정신 — 경고 후 포함)
            if (!playerGos[0].activeInHierarchy)
                Debug.LogWarning($"[TerrainBaker] {mapName}: '{MarkerPlayer}' 비활성 상태지만 bake에 포함.");

            Vector3 pp = playerGos[0].transform.position;
            if (!TrySnapToSolidFace(terrain, pp.x, pp.y, mapName, MarkerPlayer, out float snappedPY))
                return false;
            playerSpawn = new Vector2(pp.x, snappedPY);

            // 적 마커 — 0개+ 허용. 고정 배열 순서 순회 (결정론).
            foreach ((string markerName, byte kindId) in EnemyMarkers)
            {
                var gos = new List<GameObject>();
                CollectByName(scene, markerName, gos);
                foreach (GameObject go in gos)
                {
                    if (!go.activeInHierarchy)
                        Debug.LogWarning($"[TerrainBaker] {mapName}: '{go.name}' 비활성 상태지만 bake에 포함.");

                    Vector3 ep = go.transform.position;
                    if (!TrySnapToSolidFace(terrain, ep.x, ep.y, mapName, go.name, out float snappedEY))
                        return false;
                    enemies.Add(new EnemySpawnPoint(kindId, ep.x, snappedEY));
                }
            }

            return true;
        }

        /// <summary>
        /// 마커 x를 포함하는 솔리드의 윗면(MaxY) 중 마커 y와 가장 가까운 것을 찾아 스냅.
        /// |marker.y - faceY| > 0.5f 또는 후보 없으면 LogError + false 반환.
        /// </summary>
        static bool TrySnapToSolidFace(MapTerrain terrain, float markerX, float markerY,
                                        string mapName, string markerName, out float snappedY)
        {
            const float SnapEps = 0.5f;

            snappedY = markerY;
            float bestFace = float.NaN;
            float bestDist = float.MaxValue;

            foreach (TerrainAabb aabb in terrain.Solids)
            {
                if (markerX < aabb.MinX || markerX > aabb.MaxX) continue;
                float dist = Math.Abs(markerY - aabb.MaxY);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestFace = aabb.MaxY;
                }
            }

            if (float.IsNaN(bestFace))
            {
                Debug.LogError($"[TerrainBaker] {mapName}: 마커 '{markerName}' ({markerX:F2}, {markerY:F2}) — " +
                               "x 범위를 포함하는 솔리드 없음. 솔리드 위에 배치하세요. bake 중단.");
                return false;
            }

            if (bestDist > SnapEps)
            {
                Debug.LogError($"[TerrainBaker] {mapName}: 마커 '{markerName}' ({markerX:F2}, {markerY:F2}) — " +
                               $"가장 가까운 솔리드 윗면 y={bestFace:F2}, 거리={bestDist:F3} > eps={SnapEps}. " +
                               "솔리드 윗면 바로 위에 배치하세요. bake 중단.");
                return false;
            }

            snappedY = bestFace;
            return true;
        }

        // ── 마커 씬 탐색 헬퍼 ────────────────────────────────────────────────────

        static void CollectByName(Scene scene, string name, List<GameObject> result)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                CollectByNameRecursive(root.transform, name, result);
        }

        static void CollectByNameRecursive(Transform t, string name, List<GameObject> result)
        {
            if (t.gameObject.name == name)
                result.Add(t.gameObject);
            for (int i = 0; i < t.childCount; i++)
                CollectByNameRecursive(t.GetChild(i), name, result);
        }

        // "Spawn_Enemy_" 접두이지만 EnemyMarkers에 없는 이름 → 오타 fail-closed.
        static void CheckUnknownEnemyMarkers(Transform t, string mapName, ref bool found)
        {
            string n = t.gameObject.name;
            if (n.StartsWith(MarkerEnemyPrefix) && !IsKnownEnemyMarker(n))
            {
                Debug.LogError($"[TerrainBaker] {mapName}: 알 수 없는 마커 '{n}' — " +
                               "EnemyMarkers에 없는 suffix. 오타 확인. bake 중단.");
                found = true;
            }
            for (int i = 0; i < t.childCount; i++)
                CheckUnknownEnemyMarkers(t.GetChild(i), mapName, ref found);
        }

        // ── 타일맵 탐색 ──────────────────────────────────────────────────────────

        static List<Tilemap> FindTilemaps(Scene scene, string name)
        {
            var found = new List<Tilemap>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Tilemap tm in root.GetComponentsInChildren<Tilemap>(includeInactive: true))
                {
                    if (tm.gameObject.name != name) continue;
                    if (!tm.gameObject.activeInHierarchy)
                        Debug.LogWarning($"[TerrainBaker] '{name}' 비활성 상태지만 bake에 포함합니다 — 의도 확인.");
                    found.Add(tm);
                }
            }
            return found;
        }

        // ── 지형 추출 알고리즘 (검증 완료 — 산출 경로만 교체) ───────────────────

        readonly struct CellRect
        {
            public readonly float MinX, MinY, MaxX, MaxY;
            public CellRect(float minX, float minY, float maxX, float maxY)
            { MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY; }
        }

        readonly struct Segment
        {
            public readonly float Y, MinX, MaxX;
            public Segment(float y, float minX, float maxX) { Y = y; MinX = minX; MaxX = maxX; }
        }

        // 같은 행 연속 셀 run → 동일 x-range가 연속 행으로 이어지면 수직 병합 → AABB.
        // 정렬 순회 → 출력 결정론 = bake idempotent.
        static List<CellRect> ExtractSolidRects(List<Tilemap> tilemaps)
        {
            var rects = new List<CellRect>();
            foreach (Tilemap tm in tilemaps)
            {
                List<(int y, int x0, int x1)> runs = CollectRowRuns(tm);

                var open   = new List<(int x0, int x1, int y0, int y1)>();
                var closed = new List<(int x0, int x1, int y0, int y1)>();
                foreach ((int y, int x0, int x1) in runs)
                {
                    bool extended = false;
                    for (int i = 0; i < open.Count; i++)
                    {
                        if (open[i].x0 == x0 && open[i].x1 == x1 && open[i].y1 == y - 1)
                        {
                            open[i] = (x0, x1, open[i].y0, y);
                            extended = true;
                            break;
                        }
                    }
                    if (!extended) open.Add((x0, x1, y, y));
                }
                closed.AddRange(open);

                foreach ((int x0, int x1, int y0, int y1) in closed)
                {
                    Vector3 min = tm.CellToWorld(new Vector3Int(x0, y0, 0));
                    Vector3 max = tm.CellToWorld(new Vector3Int(x1 + 1, y1 + 1, 0));
                    rects.Add(new CellRect(min.x, min.y, max.x, max.y));
                }
            }
            rects.Sort((a, b) => a.MinY != b.MinY ? a.MinY.CompareTo(b.MinY) : a.MinX.CompareTo(b.MinX));
            return rects;
        }

        // 발판은 행 run의 윗면만 세그먼트로 추출 (착지 면만 데이터).
        static List<Segment> ExtractPlatformSegments(List<Tilemap> tilemaps)
        {
            var segs = new List<Segment>();
            foreach (Tilemap tm in tilemaps)
            {
                foreach ((int y, int x0, int x1) in CollectRowRuns(tm))
                {
                    Vector3 left  = tm.CellToWorld(new Vector3Int(x0,     y + 1, 0));
                    Vector3 right = tm.CellToWorld(new Vector3Int(x1 + 1, y + 1, 0));
                    segs.Add(new Segment(left.y, left.x, right.x));
                }
            }
            segs.Sort((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.MinX.CompareTo(b.MinX));
            return segs;
        }

        static List<(int y, int x0, int x1)> CollectRowRuns(Tilemap tm)
        {
            tm.CompressBounds();
            var byRow = new SortedDictionary<int, List<int>>();
            foreach (Vector3Int pos in tm.cellBounds.allPositionsWithin)
            {
                if (!tm.HasTile(pos)) continue;
                if (!byRow.TryGetValue(pos.y, out List<int> xs))
                {
                    xs = new List<int>();
                    byRow[pos.y] = xs;
                }
                xs.Add(pos.x);
            }

            var runs = new List<(int y, int x0, int x1)>();
            foreach (KeyValuePair<int, List<int>> row in byRow)
            {
                List<int> xs = row.Value;
                xs.Sort();
                int x0 = xs[0], x1 = xs[0];
                for (int i = 1; i < xs.Count; i++)
                {
                    if (xs[i] == x1 + 1) { x1 = xs[i]; continue; }
                    runs.Add((row.Key, x0, x1));
                    x0 = x1 = xs[i];
                }
                runs.Add((row.Key, x0, x1));
            }
            return runs;
        }
    }
}
