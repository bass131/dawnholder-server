using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Dawnholder.Client.EditorTools
{
    /// <summary>
    /// 씬 타일맵 → 98_Shared 생성 C# 지형 데이터 bake 파이프라인 (M4.4-01).
    ///
    /// 레이어 약속 (2026-06-06): "Tilemap_Solid" = 바닥·벽 / "Tilemap_Platform" = one-way 발판.
    /// 그 외 이름의 타일맵은 무시 (장식 레이어 자유).
    ///
    /// Unity = 저작 도구, 생성 데이터 = 단일 진실 (헌법 #1·#4) — 서버는 씬을 모르고
    /// 생성된 Shared 코드만 소비. 재생성 절차는 PacketGenerator와 동일: bake → diff 확인
    /// → dotnet build → 생성 .cs + Shared.dll 동반 commit (drift 함정).
    /// </summary>
    public static class TerrainBaker
    {
        const string SolidTilemapName = "Tilemap_Solid";
        const string PlatformTilemapName = "Tilemap_Platform";

        // mapId는 서버 MapId enum / 클라 SceneRouter와 정합 의무 — 맵 추가 시 세 곳 동반 갱신.
        static readonly (int MapId, string MapName, string ScenePath)[] Targets =
        {
            (0, "Town", "Assets/Scenes/01.PlayArea/Town.unity"),
            (1, "HuntingGround", "Assets/Scenes/01.PlayArea/HuntingGround.unity"),
            (2, "BossRoom", "Assets/Scenes/01.PlayArea/BossRoom.unity"),
        };

        static string OutputPath => Path.GetFullPath(Path.Combine(
            Application.dataPath, "..", "..", "98_Shared", "GameData", "Generated", "MapTerrainData.cs"));

        [MenuItem("Tools/Dawnholder/Bake Terrain")]
        public static void Bake()
        {
            // 미저장 씬 보호 — 사용자가 저장 프롬프트를 취소하면 bake 전체 중단.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[TerrainBaker] 미저장 변경 저장 취소 — bake 중단.");
                return;
            }

            SceneSetup[] restore = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var body = new StringBuilder();
                var summaries = new List<string>();

                foreach ((int mapId, string mapName, string scenePath) in Targets)
                {
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                    List<Tilemap> solids = FindTilemaps(scene, SolidTilemapName);
                    if (solids.Count == 0)
                    {
                        Debug.LogError($"[TerrainBaker] {mapName}: '{SolidTilemapName}' 없음 — " +
                                       "레이어 분리(M4.4-01 약속)가 선행돼야 합니다. bake 중단 (파일 미변경).");
                        return;
                    }
                    List<Tilemap> platforms = FindTilemaps(scene, PlatformTilemapName);
                    if (platforms.Count == 0)
                    {
                        Debug.LogWarning($"[TerrainBaker] {mapName}: '{PlatformTilemapName}' 없음 — " +
                                         "발판 0개로 진행 (미저작 상태면 정상).");
                    }

                    List<CellRect> solidRects = ExtractSolidRects(solids);
                    List<Segment> platformSegs = ExtractPlatformSegments(platforms);

                    AppendSolidArray(body, mapName, solidRects);
                    AppendPlatformArray(body, mapName, platformSegs);
                    summaries.Add($"{mapName}(mapId={mapId}): solids={solidRects.Count} platforms={platformSegs.Count}");
                }

                WriteGeneratedFile(body);
                Debug.Log("[TerrainBaker] bake 완료 → " + OutputPath + "\n  " +
                          string.Join("\n  ", summaries) +
                          "\n  다음: dotnet build Dawnholder.slnx (Shared.dll 재빌드) + 생성 .cs/.dll 동반 commit");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(restore);
            }
        }

        static List<Tilemap> FindTilemaps(Scene scene, string name)
        {
            var found = new List<Tilemap>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Tilemap tm in root.GetComponentsInChildren<Tilemap>(includeInactive: true))
                {
                    if (tm.gameObject.name != name) continue;
                    if (!tm.gameObject.activeInHierarchy)
                        Debug.LogWarning($"[TerrainBaker] '{name}'이 비활성 상태지만 bake에 포함합니다 — 의도 확인.");
                    found.Add(tm);
                }
            }
            return found;
        }

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
        // (수천 셀 → 수십~수백 AABB. 정렬 순회라 출력 결정론 = bake idempotent.)
        static List<CellRect> ExtractSolidRects(List<Tilemap> tilemaps)
        {
            var rects = new List<CellRect>();
            foreach (Tilemap tm in tilemaps)
            {
                List<(int y, int x0, int x1)> runs = CollectRowRuns(tm);

                // 수직 병합: 직전 행에서 같은 (x0,x1)로 열린 사각형이 있으면 연장.
                var open = new List<(int x0, int x1, int y0, int y1)>();
                var closed = new List<(int x0, int x1, int y0, int y1)>();
                foreach ((int y, int x0, int x1) in runs) // runs는 y 오름차순, 행 안 x 오름차순
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

        // 발판은 행 run의 윗면만 세그먼트로 추출 (수직 병합 무의미 — 착지 면만 데이터).
        static List<Segment> ExtractPlatformSegments(List<Tilemap> tilemaps)
        {
            var segs = new List<Segment>();
            foreach (Tilemap tm in tilemaps)
            {
                foreach ((int y, int x0, int x1) in CollectRowRuns(tm))
                {
                    Vector3 left = tm.CellToWorld(new Vector3Int(x0, y + 1, 0));
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

        // 좌표는 "R"(round-trip) + InvariantCulture — 머신 로케일 무관 동일 출력 (idempotent 약속).
        static string F(float v) => v.ToString("R", CultureInfo.InvariantCulture) + "f";

        static void AppendSolidArray(StringBuilder sb, string mapName, List<CellRect> rects)
        {
            sb.Append("    public static readonly TerrainAabb[] ").Append(mapName).Append("Solids =\n    {\n");
            foreach (CellRect r in rects)
                sb.Append("        new TerrainAabb(").Append(F(r.MinX)).Append(", ").Append(F(r.MinY))
                  .Append(", ").Append(F(r.MaxX)).Append(", ").Append(F(r.MaxY)).Append("),\n");
            sb.Append("    };\n\n");
        }

        static void AppendPlatformArray(StringBuilder sb, string mapName, List<Segment> segs)
        {
            sb.Append("    public static readonly TerrainPlatform[] ").Append(mapName).Append("Platforms =\n    {\n");
            foreach (Segment s in segs)
                sb.Append("        new TerrainPlatform(").Append(F(s.Y)).Append(", ").Append(F(s.MinX))
                  .Append(", ").Append(F(s.MaxX)).Append("),\n");
            sb.Append("    };\n\n");
        }

        static void WriteGeneratedFile(StringBuilder body)
        {
            var sb = new StringBuilder();
            sb.Append("// <auto-generated />\n");
            sb.Append("// 본 파일은 TerrainBaker(03_Client/Assets/Editor/TerrainBaker.cs)가 씬 타일맵에서 자동 생성. 직접 수정 X.\n");
            sb.Append("// 재생성: Unity 메뉴 Tools/Dawnholder/Bake Terrain → dotnet build Dawnholder.slnx\n");
            sb.Append("//   (생성 .cs + 재빌드 Shared.dll 동반 commit — 생성기-산출물 drift 함정, PacketGenerator와 동일)\n");
            sb.Append("// 좌표 = 월드 좌표. mapId = 서버 MapId enum 값 정합 (Town=0 / HuntingGround=1 / BossRoom=2).\n");
            sb.Append("\nnamespace Shared.GameData;\n\n");
            sb.Append("public static class MapTerrainData\n{\n");
            sb.Append(body);
            sb.Append("    public static TerrainAabb[] GetSolids(int mapId) => mapId switch\n    {\n");
            foreach ((int mapId, string mapName, _) in Targets)
                sb.Append("        ").Append(mapId).Append(" => ").Append(mapName).Append("Solids,\n");
            sb.Append("        _ => System.Array.Empty<TerrainAabb>(),\n    };\n\n");
            sb.Append("    public static TerrainPlatform[] GetPlatforms(int mapId) => mapId switch\n    {\n");
            foreach ((int mapId, string mapName, _) in Targets)
                sb.Append("        ").Append(mapId).Append(" => ").Append(mapName).Append("Platforms,\n");
            sb.Append("        _ => System.Array.Empty<TerrainPlatform>(),\n    };\n}\n");

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllText(OutputPath, sb.ToString(), new UTF8Encoding(false));
        }
    }
}
