using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // E1-E4: inspect real spawned bodies and render their overlap; never reproduce the product's order calculation.
    public sealed class CharacterOverlapSortSceneTests
    {
        // S1/S3 fixture identities/classes: local 700 Knight; the larger remote id joins first.
        const int LocalId = 700;
        const byte Knight = 0;
        const byte Mage = 1;
        const int EffectOrderFloor = 4; // E4: lowest existing Player-layer effect order.
        const int RenderFrames = 60; // E2/S3 observation duration, measured in rendered frames.
        const int PixelTolerance = 2; // Two RGBA32 levels tolerate color conversion/readback rounding, not visible mixing.
        MapEntryPlayFixture _fixture;

        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();
        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        IEnumerator EnterTown()
        {
            yield return _fixture.EnterScriptedTown(); // Calls Prepare(false): framed TCP authority, no production server.
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            Assert.AreEqual(LocalId, _fixture.Session.LocalEntityId, "fixture: local entity identity");
            Assert.AreEqual(2f, _fixture.Session.Entry.SpawnX, "S1 fixture: local spawn x=2");
        }

        void Join(int id, byte characterClass)
        {
            Vector3 position = _fixture.Player.transform.position;
            _fixture.Peer.Send(new S_PlayerJoin {
                entityId = id, characterClass = characterClass, spawnX = position.x, spawnY = position.y
            }.Write());
        }

        static Transform Remote(int id)
        {
            bool exists = RemoteEntityRegistry.Instance != null &&
                RemoteEntityRegistry.Instance.TryGetTransform(id, out _);
            Assert.IsTrue(exists, $"fixture: remote entity {id} exists through registry");
            RemoteEntityRegistry.Instance.TryGetTransform(id, out Transform root);
            return root;
        }

        IEnumerator WaitForRemote(int id)
        {
            yield return MapEntryPlayFixture.Wait(() => RemoteEntityRegistry.Instance != null &&
                RemoteEntityRegistry.Instance.TryGetTransform(id, out _), $"remote entity {id} attached");
            // Observe after the attachment frame has finished, including deferred Destroy. Works in batchmode.
            yield return _fixture.Frames(1);
        }

        IEnumerator ThreeCharacters()
        {
            yield return EnterTown();
            Join(702, Mage);
            yield return WaitForRemote(702);
            Join(701, Knight);
            yield return WaitForRemote(701);
        }

        [UnityTest]
        public IEnumerator S1_OverlappingBodiesHaveLocalThenAscendingEntityOrder()
        {
            yield return ThreeCharacters();
            var failures = new List<string>();
            ObserveRules("initial larger-id-first join", failures, 701, 702);
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator S2_OrderAndPreservationSurviveSwapLeaveAndMapRoundTrip()
        {
            yield return ThreeCharacters();
            var failures = new List<string>();
            ObserveRules("before replacement", failures, 701, 702);
            Transform root = Remote(701);
            Transform oldVisual = root.Find("Visual");
            Assert.IsNotNull(oldVisual, "fixture: entity 701 original Visual exists");
            Join(701, Mage);
            yield return MapEntryPlayFixture.Wait(() => root != null && root.Find("Visual") != oldVisual,
                "entity 701 class replacement through S_PlayerJoin");
            yield return _fixture.Frames(1);
            Assert.AreSame(root, Remote(701), "fixture: class swap retains entity 701 root");
            ObserveRules("701 Knight to Mage", failures, 701, 702);

            _fixture.Peer.Send(new S_PlayerLeave { entityId = 702 }.Write());
            yield return MapEntryPlayFixture.Wait(() => !RemoteEntityRegistry.Instance.TryGetTransform(702, out _),
                "entity 702 leave applied");
            yield return _fixture.Frames(1);
            ObserveRules("702 departed", failures, 701);
            Join(703, Knight);
            yield return WaitForRemote(703);
            ObserveRules("703 joined after 702 left", failures, 701, 703);

            Transform formerLocal = _fixture.Player.transform;
            yield return ChangeMap(1);
            Assert.IsTrue(formerLocal == null, "fixture: Town local destroyed on map departure");
            ObserveRules("HuntingGround new local", failures);
            yield return ChangeMap(0);
            Join(703, Knight);
            yield return WaitForRemote(703);
            Join(701, Mage);
            yield return WaitForRemote(701);
            ObserveRules("Town return and remote recreation", failures, 701, 703);
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        IEnumerator ChangeMap(byte map)
        {
            _fixture.Peer.Send(new S_MapTransition { destMapId = map, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == map &&
                _fixture.Session.Entry.SceneReady, $"map {map} scene before fresh HP");
            _fixture.Hp();
            yield return _fixture.WaitMap(map);
            yield return _fixture.Frames(1);
        }

        static SpriteRenderer[] Bodies(Transform root) => root.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(renderer => renderer.gameObject.activeInHierarchy && renderer.enabled && renderer.sprite != null)
            .ToArray();

        void ObserveRules(string stage, List<string> failures, params int[] remoteIds)
        {
            var characters = new List<(int Id, Transform Root)> { (LocalId, _fixture.Player.transform) };
            characters.AddRange(remoteIds.OrderBy(id => id).Select(id => (id, Remote(id))));
            var bodies = new List<SpriteRenderer[]>();
            int checks = 0;
            int stageFailures = failures.Count;
            void Check(bool passed, string message)
            {
                checks++;
                TestContext.Out.WriteLine($"[{stage}] {(passed ? "PASS" : "FAIL")} {message}");
                if (!passed) failures.Add($"[{stage}] {message}");
            }

            foreach (var character in characters)
            {
                SpriteRenderer[] renderers = Bodies(character.Root);
                Assert.IsNotEmpty(renderers, $"[{stage}] existence: {character.Root.name} entity={character.Id} bodies");
                bodies.Add(renderers);
                string description = Describe(character.Id, renderers);
                Check(character.Root.position.z == 0f, $"S4/E4 root z=0: {description}; z={character.Root.position.z:R}");
                Check(character.Root.GetComponentsInChildren<SortingGroup>(true).Length == 0,
                    $"S1/E1 no SortingGroup: {description}");
                foreach (SpriteRenderer renderer in renderers)
                {
                    Check(renderer.sortingLayerName == "Player", $"S4/E4 layer Player: {Describe(character.Id, renderer)}");
                    Check(renderer.sortingOrder < EffectOrderFloor,
                        $"S4/E4 order below effects ({EffectOrderFloor}): {Describe(character.Id, renderer)}");
                }
                for (int first = 0; first < renderers.Length; first++)
                for (int second = first + 1; second < renderers.Length; second++)
                    Check(renderers[first].sortingOrder != renderers[second].sortingOrder,
                        $"S1/E1 distinct within body: {Describe(character.Id, renderers[first])} vs " +
                        Describe(character.Id, renderers[second]));

                if (character.Id == LocalId) continue;
                // 런타임 원격 프리팹(Resources/RemotePlayer.prefab)에는 뿌리 SpriteRenderer가 없다.
                Transform visual = character.Root.Find("Visual");
                Assert.IsNotNull(visual, $"[{stage}] existence: {description}; Visual");
                SpriteRenderer[] visualBodies = renderers.Where(renderer => renderer.transform.IsChildOf(visual)).ToArray();
                Assert.IsNotEmpty(visualBodies, $"[{stage}] existence: {description}; Visual bodies");
            }
            for (int front = 0; front < characters.Count; front++)
            for (int back = front + 1; back < characters.Count; back++)
                Check(bodies[front].Min(renderer => renderer.sortingOrder) > bodies[back].Max(renderer => renderer.sortingOrder),
                    $"S1/E1 front minimum > back maximum: FRONT {Describe(characters[front].Id, bodies[front])}; " +
                    $"BACK {Describe(characters[back].Id, bodies[back])}");
            TestContext.Out.WriteLine($"[{stage}] checks={checks} violations={failures.Count - stageFailures}");
        }

        static string Describe(int id, params SpriteRenderer[] renderers) => string.Join("; ", renderers.Select(renderer =>
            $"entity={id} path={HierarchyPath(renderer.transform)} layer={renderer.sortingLayerName} order={renderer.sortingOrder}"));

        static string HierarchyPath(Transform node)
        {
            var names = new Stack<string>();
            for (; node != null; node = node.parent) names.Push(node.name);
            return string.Join("/", names);
        }

        [UnityTest]
        public IEnumerator S3_EveryOverlappingPixelShowsLocalFor60Frames()
        {
            yield return EnterTown();
            Join(701, Mage);
            yield return WaitForRemote(701);
            Camera camera = Camera.main;
            Assert.IsNotNull(camera, "S3 existence: scene main camera");
            SpriteRenderer[] local = Bodies(_fixture.Player.transform);
            SpriteRenderer[] remote = Bodies(Remote(701));
            Assert.IsNotEmpty(local, "S3 existence: local 700 bodies");
            Assert.IsNotEmpty(remote, "S3 existence: remote 701 bodies");
            string identities = Describe(LocalId, local) + "; " + Describe(701, remote);
            var failures = new List<string>();
            int minimumOverlap = int.MaxValue;
            int maximumOverlap = 0;
            using (var capture = new OverlapCapture(camera, local, remote))
            {
                for (int frame = 0; frame < RenderFrames; frame++)
                {
                    // All six renders share one frame; coverage is observed, not inferred from silhouette geometry.
                    Color32[][] plates = capture.PlatesWithCoverage(frame);
                    int overlap = 0;
                    int fullyCovered = 0;
                    int partiallyCovered = 0;
                    int incorrect = 0;
                    int firstIncorrect = -1;
                    for (int pixel = 0; pixel < plates[0].Length; pixel++)
                    {
                        bool localPresent = !Close(plates[1][pixel], plates[0][pixel]);
                        bool remotePresent = !Close(plates[2][pixel], plates[0][pixel]);
                        bool distinguishable = !Close(plates[1][pixel], plates[2][pixel]);
                        if (!localPresent || !remotePresent || !distinguishable) continue;
                        overlap++;
                        if (!Close(plates[4][pixel], plates[5][pixel]))
                        {
                            partiallyCovered++;
                            continue;
                        }
                        fullyCovered++;
                        if (Close(plates[3][pixel], plates[1][pixel])) continue;
                        incorrect++;
                        if (firstIncorrect < 0) firstIncorrect = pixel;
                    }
                    minimumOverlap = Mathf.Min(minimumOverlap, overlap);
                    maximumOverlap = Mathf.Max(maximumOverlap, overlap);
                    TestContext.Out.WriteLine($"E2 frame={frame:D3} overlap={overlap} fullyCovered={fullyCovered} " +
                        $"partiallyCovered={partiallyCovered} incorrect={incorrect}");
                    if (overlap == 0) failures.Add($"E2 frame={frame:D3} existence: no distinguishable overlap pixels; {identities}");
                    if (fullyCovered == 0) failures.Add($"E2 frame={frame:D3} existence: no fully covered overlap pixels; {identities}");
                    // At least half is a protective requirement against vacuous sampling, not a measured coverage value.
                    if (fullyCovered * 2 < overlap) failures.Add($"E2 frame={frame:D3} coverage: " +
                        $"fullyCovered={fullyCovered} is less than half of overlap={overlap}; {identities}");
                    if (incorrect > 0) failures.Add($"E2 frame={frame:D3} local result: incorrect={incorrect}, " +
                        $"firstIndex={firstIncorrect}, local={plates[1][firstIncorrect]}, both={plates[3][firstIncorrect]}; {identities}");
                    if (frame + 1 < RenderFrames) yield return _fixture.Frames(1);
                }
            }
            TestContext.Out.WriteLine($"E2 frames={RenderFrames} overlapMin={minimumOverlap} overlapMax={maximumOverlap} " +
                $"channelTolerance={PixelTolerance}/255");
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        static bool Close(Color32 left, Color32 right) =>
            Math.Abs(left.r - right.r) <= PixelTolerance && Math.Abs(left.g - right.g) <= PixelTolerance &&
            Math.Abs(left.b - right.b) <= PixelTolerance && Math.Abs(left.a - right.a) <= PixelTolerance;

        // Owns temporary camera/readback state; each plate set restores renderer and coverage-isolation state.
        sealed class OverlapCapture : IDisposable
        {
            const int CoverageLayer = 31; // Unnamed in TagManager; checked for renderer collisions before isolation.
            readonly Camera _camera;
            readonly SpriteRenderer[] _local;
            readonly SpriteRenderer[] _remote;
            readonly RenderTexture _oldTarget;
            readonly bool _oldHdr, _oldMsaa, _oldEnabled;
            readonly RenderTexture _target;
            readonly Texture2D _readback;

            internal OverlapCapture(Camera camera, SpriteRenderer[] local, SpriteRenderer[] remote)
            {
                _camera = camera;
                _local = local;
                _remote = remote;
                _oldTarget = camera.targetTexture;
                _oldHdr = camera.allowHDR;
                _oldMsaa = camera.allowMSAA;
                _oldEnabled = camera.enabled;
                // S3 has no output-size requirement. 640x360 keeps readbacks small while testing actual pixels.
                _target = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                _target.Create();
                _readback = new Texture2D(_target.width, _target.height, TextureFormat.RGBA32, false, false);
                camera.enabled = false;
                camera.allowHDR = camera.allowMSAA = false;
                camera.targetTexture = _target;
            }

            internal Color32[][] PlatesWithCoverage(int frame)
            {
                bool[] localEnabled = _local.Select(renderer => renderer.enabled).ToArray();
                bool[] remoteEnabled = _remote.Select(renderer => renderer.enabled).ToArray();
                var plates = new Color32[6][];
                try
                {
                    foreach (SpriteRenderer renderer in _local) renderer.enabled = false;
                    foreach (SpriteRenderer renderer in _remote) renderer.enabled = false;
                    plates[0] = Render("bg", frame);
                    Restore(_local, localEnabled);
                    plates[1] = Render("local", frame);
                    foreach (SpriteRenderer renderer in _local) renderer.enabled = false;
                    Restore(_remote, remoteEnabled);
                    plates[2] = Render("remote", frame);
                    Restore(_local, localEnabled);
                    Restore(_remote, remoteEnabled);
                    plates[3] = Render("both", frame);
                    RenderCoverage(plates, frame);
                    return plates;
                }
                finally
                {
                    Restore(_local, localEnabled);
                    Restore(_remote, remoteEnabled);
                }
            }

            void RenderCoverage(Color32[][] plates, int frame)
            {
                Assert.IsEmpty(LayerMask.LayerToName(CoverageLayer), "S3 fixture: coverage layer is unnamed");
                Assert.IsFalse(UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                    .Any(renderer => renderer.gameObject.layer == CoverageLayer),
                    "S3 fixture: coverage layer has no other renderers");
                GameObject[] bodies = _local.Select(renderer => renderer.gameObject).Distinct().ToArray();
                int[] layers = bodies.Select(body => body.layer).ToArray();
                int oldCullingMask = _camera.cullingMask;
                CameraClearFlags oldClearFlags = _camera.clearFlags;
                Color oldBackground = _camera.backgroundColor;
                // The test assembly has no URP reference. Keep an existing camera component, without adding one.
                Component pipelineCamera = _camera.GetComponent("UniversalAdditionalCameraData");
                var postProcessing = pipelineCamera != null
                    ? pipelineCamera.GetType().GetProperty("renderPostProcessing") : null;
                object oldPostProcessing = postProcessing?.GetValue(pipelineCamera);
                try
                {
                    foreach (GameObject body in bodies) body.layer = CoverageLayer;
                    _camera.cullingMask = 1 << CoverageLayer;
                    _camera.clearFlags = CameraClearFlags.SolidColor;
                    postProcessing?.SetValue(pipelineCamera, false);
                    // Opposite backgrounds expose partial coverage even where the scene background resembles the body.
                    _camera.backgroundColor = Color.black;
                    plates[4] = Render("coverage-black", frame);
                    _camera.backgroundColor = Color.white;
                    plates[5] = Render("coverage-white", frame);
                }
                finally
                {
                    for (int i = 0; i < bodies.Length; i++)
                        if (bodies[i] != null) bodies[i].layer = layers[i];
                    _camera.cullingMask = oldCullingMask;
                    _camera.clearFlags = oldClearFlags;
                    _camera.backgroundColor = oldBackground;
                    postProcessing?.SetValue(pipelineCamera, oldPostProcessing);
                }
            }

            Color32[] Render(string plate, int frame)
            {
                _camera.Render();
                RenderTexture previous = RenderTexture.active;
                try
                {
                    RenderTexture.active = _target;
                    _readback.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0, false);
                    _readback.Apply(false);
                    string directory = Environment.GetEnvironmentVariable("DAWNHOLDER_OVERLAP_SORT_CAPTURE_DIR");
                    if (frame == 0 && !string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                        using var output = new FileStream(Path.Combine(directory, $"overlap-{plate}-f000.png"), FileMode.CreateNew);
                        byte[] png = _readback.EncodeToPNG();
                        output.Write(png, 0, png.Length);
                    }
                    return _readback.GetPixels32();
                }
                finally { RenderTexture.active = previous; }
            }

            static void Restore(SpriteRenderer[] renderers, bool[] enabled)
            {
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) renderers[i].enabled = enabled[i];
            }

            public void Dispose()
            {
                if (_camera != null)
                {
                    _camera.targetTexture = _oldTarget;
                    _camera.allowHDR = _oldHdr;
                    _camera.allowMSAA = _oldMsaa;
                    _camera.enabled = _oldEnabled;
                }
                _target.Release();
                UnityEngine.Object.Destroy(_target);
                UnityEngine.Object.Destroy(_readback);
            }
        }
    }
}
