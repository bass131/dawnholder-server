using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // HUD 재구성 선행 요구시험(EditMode). 요구 원천은 .backups/verification/2026-10-13-demo-video-art/
    // hud-reconstruction-pr/pretest-01/contract.md 「요구와 독립 기대값」이다. 수신은 실제 UnityClientSession과
    // entry 장벽(GameplayConnection)을, HUD는 UI 씬과 같은 기존 serialized 필드로 연결한 HudController·
    // SkillHudController를 쓴다. Unity 메시지는 ViewLifecycle이 Unity 순서로 보낸다. 기대값은 여기서 보낸 서버
    // 리터럴과 고정 표(직업→Q/E, mapId→지명)뿐이며 PlayerStats·SceneRouter·ClientSkillCatalog로 다시 계산하지 않는다.
    // 화면 가시성(비활성 HpText 등)은 PlayMode HudPresentationLifecycleTests가 실제 UI 씬에서 본다.
    public sealed class HudPresentationTests
    {
        // UI.unity의 SkillHudController._iconSet이 가리키는 실제 아이콘 데이터 자산.
        const string SkillIconSetPath = "Assets/Prefabs/Characters/ClassVisuals/SkillIconSet.asset";

        readonly List<GameObject> _objects = new();
        GameplayConnection _connection;
        GameObject _hud;
        CharacterClass? _serverClass;
        CharacterClass? _menuClass;
        int _latency;

        [SetUp]
        public void Setup()
        {
            Assert.IsNull(UnityClientSession.Instance, "another test left a published session");
            Assert.IsNull(HudController.Instance, "another test left a HUD");
            Assert.IsNull(SkillHudController.Instance, "another test left a skill HUD");
            _serverClass = ClassLoadout.ServerClass;
            _menuClass = ClassLoadout.SessionSelectedClass;
            _latency = UnityClientSession.SimulatedLatencyMs;
            UnityClientSession.SimulatedLatencyMs = 0;
            ClassLoadout.ResetServerClass();
            MapNameDisplay.ResetForTest();
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                _connection?.Dispose();
            }
            finally
            {
                if (_hud != null) ViewLifecycle.Destroy(_hud);
                foreach (GameObject owned in _objects)
                {
                    if (owned != null) Object.DestroyImmediate(owned);
                }
                _objects.Clear();
                ClearLeftSingleton(typeof(HudController));
                ClearLeftSingleton(typeof(SkillHudController));
                ClassLoadout.ResetServerClass();
                if (_serverClass.HasValue) ClassLoadout.ApplyServerClass(_serverClass.Value);
                ClassLoadout.SessionSelectedClass = _menuClass;
                UnityClientSession.SimulatedLatencyMs = _latency;
                MapNameDisplay.ResetForTest();
            }
        }

        // [수신 전 HP] 확정 직업만 있고 서버 HP가 없으면 직업 기본 full HP를 확정값처럼 숫자·가득 찬 바로 보이지 않는다.
        [TestCase(CharacterClass.Knight)]
        [TestCase(CharacterClass.Mage)]
        public void ConfirmedClass_WithoutServerHp_ShowsNoHpNumberAndNoFullBar(CharacterClass confirmed)
        {
            ClassLoadout.ApplyServerClass(confirmed);
            HudUnderTest hud = StartHud(withSkills: false);
            ViewLifecycle.Frame(_hud);
            string shown = hud.HpLabel.text;
            bool fullBar = ShowsFullBar(hud.HpSlider);

            Assert.IsFalse(HasDigit(shown), $"[수신 전 HP] {confirmed} 확정 뒤 HP 수신 전 라벨에 숫자가 없다: '{shown}'");
            StringAssert.Contains("HP", shown, "[수신 전 HP] 수신 전에도 HP 라벨은 무엇의 값인지 읽힌다");
            Assert.IsFalse(fullBar, $"[수신 전 HP] {confirmed} 기본 체력이 가득 찬 바로 보이지 않는다 (value {hud.HpSlider.value})");
        }

        // [확정 직업/HP] 실제 entry 장벽을 지난 서버 HP만 숫자로 보인다. HUD Start 전/후 도착 모두 같은 값이고, 도착 전에는 숫자가 없다.
        [TestCase(false)]
        [TestCase(true)]
        public void RealEntryHp_BeforeOrAfterHudStart_IsTheOnlyHpNumberShown(bool hpBeforeStart)
        {
            _connection = new GameplayConnection();
            _connection.BeginMap(0);
            if (hpBeforeStart) _connection.Ready(); // S_PlayerHp 37/100
            HudUnderTest hud = StartHud(withSkills: false);
            ViewLifecycle.Frame(_hud);
            string beforeHp = hud.HpLabel.text;
            if (!hpBeforeStart)
            {
                _connection.Ready();
                ViewLifecycle.Frame(_hud);
            }
            string firstHp = hud.HpLabel.text;
            _connection.Session.ReceivePlayerHp(7, 12, 100);
            ViewLifecycle.Frame(_hud);
            string nextHp = hud.HpLabel.text;

            if (!hpBeforeStart)
                Assert.IsFalse(HasDigit(beforeHp), $"[수신 전 HP] Start 뒤 HP 도착 전 라벨에 숫자가 없다: '{beforeHp}'");
            Assert.IsTrue(ShowsHp(firstHp, 37, 100), $"[확정 직업/HP] 받은 HP 37/100이 읽힌다: '{firstHp}'");
            Assert.IsTrue(ShowsHp(nextHp, 12, 100), $"[확정 직업/HP] 다음 서버 HP 12/100을 따른다: '{nextHp}'");
        }

        // [같은 연결 map 이동] 새 entry는 HP 미확정으로 시작하고, 옛 entry의 지연 HP는 보이지 않으며, 새 entry HP만 표시를 바꾼다.
        // 지형 자료가 없는 EditMode라 같은 Town으로의 새 entry(서버 S_MapTransition dest 0과 같은 경계)를 쓴다.
        [Test]
        public void SameConnectionNewEntry_HpReturnsToUnconfirmed_AndOnlyTheNewEntryHpIsShown()
        {
            _connection = new GameplayConnection();
            _connection.BeginMap(0);
            _connection.Ready();
            _connection.Session.ReceivePlayerHp(7, 41, 100); // 옛 entry의 마지막 HP
            HudUnderTest hud = StartHud(withSkills: false);
            ViewLifecycle.Frame(_hud);
            string oldEntry = hud.HpLabel.text;
            long oldEpoch = _connection.Session.Entry.Epoch;

            _connection.BeginMap(0);
            ViewLifecycle.Frame(_hud);
            string newEntryBeforeHp = hud.HpLabel.text;
            _connection.Session.Entry.ReceiveHp(oldEpoch, 99, 100); // 옛 epoch로 늦게 도착한 HP
            ViewLifecycle.Frame(_hud);
            string afterLateOldHp = hud.HpLabel.text;
            _connection.Ready(); // 새 entry의 S_PlayerHp 37/100
            ViewLifecycle.Frame(_hud);
            string newEntryHp = hud.HpLabel.text;

            Assert.IsTrue(ShowsHp(oldEntry, 41, 100), $"fixture: 옛 entry HP 41/100이 먼저 보였다: '{oldEntry}'");
            Assert.IsFalse(HasDigit(newEntryBeforeHp),
                $"[같은 연결 map 이동] 새 entry HP 수신 전 옛 HP나 기본값 숫자가 없다: '{newEntryBeforeHp}'");
            Assert.IsFalse(ShowsHp(afterLateOldHp, 99, 100) || ShowsHp(afterLateOldHp, 41, 100),
                $"[같은 연결 map 이동] 옛 epoch 지연 HP가 복구되지 않는다: '{afterLateOldHp}'");
            Assert.IsTrue(ShowsHp(newEntryHp, 37, 100), $"[같은 연결 map 이동] 새 entry HP 37/100만 표시를 바꾼다: '{newEntryHp}'");
        }

        // [연결 종료/교체] transport가 닫힌 뒤 main cleanup 전에도, cleanup 뒤에도 닫힌 연결의 HP와 직업 기술 아이콘을 현재값으로 보이지 않는다.
        [Test]
        public void ClosedTransport_BeforeAndAfterMainCleanup_ShowsNoHpOrSkillOfTheClosedSession()
        {
            _connection = new GameplayConnection(); // 서버 확정 Knight
            _connection.BeginMap(0);
            _connection.Ready();
            HudUnderTest hud = StartHud(withSkills: true);
            ViewLifecycle.Frame(_hud);
            string openHp = hud.HpLabel.text;
            bool openDash = ShowsSprite(_hud, Icon(SkillId.Dash));

            UnityClientSession closed = _connection.Session;
            closed.Disconnect(); // transport close. session cleanup은 수동 main queue에 남아 아직 실행되지 않는다.
            bool cleanupPending = closed.IsClosed && ReferenceEquals(UnityClientSession.Instance, closed);
            ViewLifecycle.Frame(_hud);
            string windowHp = hud.HpLabel.text;
            bool windowDash = ShowsSprite(_hud, Icon(SkillId.Dash));

            _connection.End(); // main queue → session cleanup → 소유자의 미러 reset(직업 권위 포함)
            ViewLifecycle.Frame(_hud);
            string cleanedHp = hud.HpLabel.text;
            bool cleanedDash = ShowsSprite(_hud, Icon(SkillId.Dash));

            Assert.IsTrue(ShowsHp(openHp, 37, 100) && openDash, $"fixture: 열린 연결의 HP '{openHp}'와 Knight Q 아이콘이 먼저 보였다");
            Assert.IsTrue(cleanupPending, "fixture: transport는 닫혔고 main cleanup은 아직이다");
            Assert.IsFalse(HasDigit(windowHp), $"[연결 종료/교체] close 뒤 cleanup 전 닫힌 연결 HP 숫자가 없다: '{windowHp}'");
            Assert.IsFalse(windowDash, "[연결 종료/교체] close 뒤 cleanup 전 닫힌 연결의 Knight Q 아이콘을 현재값으로 보이지 않는다");
            Assert.IsFalse(HasDigit(cleanedHp), $"[연결 종료/교체] cleanup 뒤 HP 숫자가 없다: '{cleanedHp}'");
            Assert.IsFalse(cleanedDash, "[연결 종료/교체] cleanup 뒤 닫힌 연결의 Knight Q 아이콘이 남지 않는다");
        }

        // [실제 Q/E] Mage Q Thunderbolt/E Teleport, Knight Q Dash/E 없음. 연결 소유자의 reset으로 직업이 미확정이 되면 옛 Mage 아이콘이
        // 남지 않고, 다음 Knight 확정에서 옛 Mage E가 되살아나지 않는다.
        [Test]
        public void MageThenOwnerResetThenKnight_QeIconsFollowTheConfirmedClass_WithoutAStaleMageE()
        {
            Sprite thunderbolt = Icon(SkillId.Thunderbolt);
            Sprite teleport = Icon(SkillId.Teleport);
            Sprite dash = Icon(SkillId.Dash);
            ClassLoadout.ApplyServerClass(CharacterClass.Mage);
            StartHud(withSkills: true);
            ViewLifecycle.Frame(_hud);
            bool mageQ = ShowsSprite(_hud, thunderbolt);
            bool mageE = ShowsSprite(_hud, teleport);

            NetworkService.ResetGlobalSessionMirrors(); // 끝난 연결의 소유자 reset. 직업 권위만 비우고 통지는 없다.
            ViewLifecycle.Frame(_hud);
            bool resetQ = ShowsSprite(_hud, thunderbolt);
            bool resetE = ShowsSprite(_hud, teleport);

            ClassLoadout.ApplyServerClass(CharacterClass.Knight);
            ViewLifecycle.Frame(_hud);
            bool knightQ = ShowsSprite(_hud, dash);
            bool knightMageQ = ShowsSprite(_hud, thunderbolt);
            bool knightMageE = ShowsSprite(_hud, teleport);

            Assert.IsTrue(mageQ, "[실제 Q/E] Mage Q는 Thunderbolt 아이콘이다");
            Assert.IsTrue(mageE, "[실제 Q/E] Mage E는 Teleport 아이콘이다");
            Assert.IsFalse(resetQ || resetE, "[실제 Q/E] 직업 미확정이 되면 옛 Mage Q/E 아이콘이 남지 않는다");
            Assert.IsTrue(knightQ, "[실제 Q/E] Knight Q는 Dash 아이콘이다");
            Assert.IsFalse(knightMageQ || knightMageE, "[실제 Q/E] 새 Knight 확정 뒤 옛 Mage Q/E 아이콘이 없다");
        }

        // [장소/보존] mapId 0~3의 고정 지명이 살아 있는 라벨과 UI 재생성 뒤 새 라벨 모두에 보인다.
        [TestCase((byte)0, "Town")]
        [TestCase((byte)1, "Hunting Ground")]
        [TestCase((byte)2, "Boss Room")]
        [TestCase((byte)3, "Ending")]
        public void MapName_IsTheFixedName_OnTheLiveLabelAndARecreatedOne(byte mapId, string expected)
        {
            GameObject first = Own(new GameObject("Map name before"));
            TextMeshProUGUI firstLabel = MapLabel(first);
            ViewLifecycle.Begin(first);
            MapNameDisplay.SetMapId(mapId);
            string live = firstLabel.text;
            ViewLifecycle.Destroy(first);

            GameObject second = Own(new GameObject("Map name recreated"));
            TextMeshProUGUI secondLabel = MapLabel(second);
            ViewLifecycle.Begin(second);
            string recreated = secondLabel.text;

            Assert.AreEqual(expected, live, $"[장소/보존] mapId {mapId}의 살아 있는 지명");
            Assert.AreEqual(expected, recreated, $"[장소/보존] mapId {mapId}의 UI 재생성 뒤 지명");
        }

        // UI.unity와 같은 HUD root: HudController와 (선택) SkillHudController가 기존 serialized 필드로 라벨·바·아이콘을 가진다.
        // HP 라벨과 바는 별도 객체라 HUD가 사라져도 읽을 수 있다.
        HudUnderTest StartHud(bool withSkills)
        {
            var hud = new HudUnderTest
            {
                HpLabel = Own(new GameObject("HP label")).AddComponent<TextMeshProUGUI>(),
                HpSlider = Own(new GameObject("HP slider")).AddComponent<Slider>(),
            };
            _hud = new GameObject("HUD under test");
            var resources = new SerializedObject(_hud.AddComponent<HudController>());
            Bind(resources, "_hpText", hud.HpLabel);
            Bind(resources, "_hpSlider", hud.HpSlider);
            resources.ApplyModifiedPropertiesWithoutUndo();
            if (withSkills)
            {
                var skills = new SerializedObject(_hud.AddComponent<SkillHudController>());
                Bind(skills, "_qCooldownImage", SlotImage("Q cooldown"));
                Bind(skills, "_qIconImage", SlotImage("Q icon"));
                Bind(skills, "_eCooldownImage", SlotImage("E cooldown"));
                Bind(skills, "_eIconImage", SlotImage("E icon"));
                Bind(skills, "_iconSet", IconSet());
                skills.ApplyModifiedPropertiesWithoutUndo();
            }
            ViewLifecycle.Begin(_hud);
            return hud;
        }

        Image SlotImage(string name)
        {
            var slot = new GameObject(name);
            slot.transform.SetParent(_hud.transform, false);
            return slot.AddComponent<Image>();
        }

        TextMeshProUGUI MapLabel(GameObject root)
        {
            TextMeshProUGUI label = root.AddComponent<TextMeshProUGUI>();
            var display = new SerializedObject(root.AddComponent<MapNameDisplay>());
            Bind(display, "_label", label);
            display.ApplyModifiedPropertiesWithoutUndo();
            return label;
        }

        static void Bind(SerializedObject owner, string field, Object value)
        {
            SerializedProperty property = owner.FindProperty(field);
            Assert.IsNotNull(property, $"fixture: {owner.targetObject.GetType().Name} keeps its serialized {field}");
            property.objectReferenceValue = value;
        }

        static SkillIconSet IconSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<SkillIconSet>(SkillIconSetPath);
            Assert.IsNotNull(set, "fixture: the skill icon data asset " + SkillIconSetPath);
            return set;
        }

        // 아이콘 sprite는 실제 데이터 자산에서 읽는다. 어느 SkillId를 기대할지는 시험의 고정 표가 정한다.
        static Sprite Icon(SkillId skill)
        {
            Sprite icon = IconSet().GetIcon(skill);
            Assert.IsNotNull(icon, $"fixture: the skill icon data has {skill}");
            return icon;
        }

        // 플레이어가 보는 아이콘: HUD 아래 켜진 Image가 그 sprite를 그린다.
        static bool ShowsSprite(GameObject root, Sprite sprite)
        {
            if (root == null) return false;
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == sprite && image.enabled && image.gameObject.activeInHierarchy) return true;
            }
            return false;
        }

        static bool ShowsFullBar(Slider bar) => bar.gameObject.activeInHierarchy && bar.value >= 0.999f;

        static bool HasDigit(string text) => Regex.IsMatch(text ?? string.Empty, @"\d");

        // 받은 current와 max가 그 순서로, 다른 숫자에 붙지 않고 읽힌다. 서식(구분자·접두)은 구현에 맡긴다.
        static bool ShowsHp(string text, int current, int max) =>
            Regex.IsMatch(text ?? string.Empty, $@"(?<!\d){current}(?!\d)\D+(?<!\d){max}(?!\d)");

        static void ClearLeftSingleton(System.Type owner)
        {
            var instance = owner.GetProperty("Instance");
            // OnDestroy가 실패했을 때만 남는다. 다음 시험의 Awake가 낡은 소유자를 보지 않게 한다.
            if (!ReferenceEquals(instance.GetValue(null), null)) instance.GetSetMethod(true).Invoke(null, new object[] { null });
        }

        GameObject Own(GameObject owned)
        {
            _objects.Add(owned);
            return owned;
        }

        sealed class HudUnderTest
        {
            public TextMeshProUGUI HpLabel;
            public Slider HpSlider;
        }
    }
}
