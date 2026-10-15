using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Test-only adapter for the guild windows of goal 2026-10-15-guild-client-ui PR1. The product types do not exist
    // before the implementation, so they are reached by the names that .backups/verification/2026-10-15-guild-client-ui/
    // pr1-tdd/api-contract.md fixes (contract-v1 req 7) instead of compile-time references, and the suite compiles on
    // the current baseline. A missing type or member fails the calling test with "RED unimplemented" and the missing name.
    internal static class GuildProduct
    {
        const string Namespace = "Dawnholder.Client.UI.";
        static readonly Assembly Client = typeof(InventoryPanel).Assembly;

        internal static Type Require(string name)
        {
            Type type = Client.GetType(Namespace + name);
            if (type == null) Assert.Fail($"RED unimplemented: product type {Namespace}{name} (api-contract.md)");
            return type;
        }

        // A display value built by constructor parameter names, so the product chooses the order and int/long.
        internal static object Construct(string typeName, IReadOnlyDictionary<string, object> arguments)
        {
            Type type = Require(typeName);
            ConstructorInfo[] constructors = type.GetConstructors();
            ConstructorInfo constructor = constructors.FirstOrDefault(candidate => candidate.GetParameters()
                .Select(parameter => parameter.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(arguments.Keys.OrderBy(name => name, StringComparer.Ordinal)));
            if (constructor == null)
            {
                Assert.Fail($"RED unimplemented: public {type.Name}({string.Join(", ", arguments.Keys)}) by parameter name; " +
                    $"found [{string.Join("; ", constructors.Select(Describe))}]");
            }
            object[] values = constructor.GetParameters()
                .Select(parameter => Convert(arguments[parameter.Name], parameter.ParameterType, type.Name + "." + parameter.Name))
                .ToArray();
            return Invoke(() => constructor.Invoke(values));
        }

        internal static MethodInfo Method(Type type, string name, params Type[] parameters)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
                null, parameters, null);
            if (method == null)
            {
                Assert.Fail($"RED unimplemented: public {type.Name}.{name}({string.Join(", ", parameters.Select(p => p.Name))})");
            }
            return method;
        }

        internal static PropertyInfo Property(Type type, string name, Type propertyType)
        {
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || property.PropertyType != propertyType)
            {
                Assert.Fail($"RED unimplemented: public {propertyType.Name} {type.Name}.{name}");
            }
            return property;
        }

        // A field-like event of type Action, so the test counts each intent without a product-specific handler type.
        internal static EventInfo ActionEvent(Type type, string name)
        {
            EventInfo intent = type.GetEvent(name, BindingFlags.Public | BindingFlags.Instance);
            if (intent == null || intent.EventHandlerType != typeof(Action))
            {
                Assert.Fail($"RED unimplemented: public event Action {type.Name}.{name}");
            }
            return intent;
        }

        // Product exceptions surface as themselves rather than as reflection wrappers.
        internal static object Invoke(Func<object> call)
        {
            try
            {
                return call();
            }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }

        static object Convert(object value, Type target, string where)
        {
            Type underlying = Nullable.GetUnderlyingType(target) ?? target;
            if (value == null || underlying.IsInstanceOfType(value)) return value;
            if (underlying.IsEnum && value is string member)
            {
                if (!Enum.IsDefined(underlying, member)) Assert.Fail($"RED unimplemented: {underlying.Name}.{member}");
                return Enum.Parse(underlying, member);
            }
            if (value is Enum || underlying.IsEnum)
            {
                Assert.Fail($"RED unimplemented: {where} must take {value.GetType().Name} (found {target.Name})");
            }
            return System.Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
        }

        static string Describe(ConstructorInfo constructor) =>
            "(" + string.Join(", ", constructor.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ")";
    }

    // One guild status as the server gives it. Defaults are the approved World values (World goal
    // 2026-10-14-guild-server 「승인 A」, consumed by this goal's 「요구사항 원천」): after two accounts' first entries the
    // treasury is 10, the base is level 1 of 2 with the blacksmith locked, and the next level costs 30.
    internal sealed class GuildInput
    {
        internal string GuildName { get; set; } = "새벽 길드";
        internal int MemberCount { get; set; } = 2;
        internal int OnlineCount { get; set; } = 2;
        internal long Treasury { get; set; } = 10;
        internal int BaseLevel { get; set; } = 1;
        internal int MaxBaseLevel { get; set; } = 2;
        internal bool BlacksmithUnlocked { get; set; }
        internal long LevelUpCost { get; set; } = 30;
        internal string LevelUp { get; set; } = "Insufficient";
        internal bool LevelUpPending { get; set; }
        internal string LevelUpResult { get; set; } = "None";

        // Scene 3: two members entered, treasury 10 is short of the 30 level-up.
        internal static GuildInput Entered() => new GuildInput();

        // Scene 6: both clears added 10 each, so treasury 30 affords the level-up.
        internal static GuildInput Funded() => Entered().With(guild =>
        {
            guild.Treasury = 30;
            guild.LevelUp = "Available";
        });

        // Scene 7: the confirmed purchase left treasury 0 at level 2 of 2 with the blacksmith unlocked. There is no
        // next level, so the server gives no next price (0 here, which the window must not show as a price).
        internal static GuildInput Purchased() => Entered().With(guild =>
        {
            guild.Treasury = 0;
            guild.BaseLevel = 2;
            guild.BlacksmithUnlocked = true;
            guild.LevelUpCost = 0;
            guild.LevelUp = "MaxLevel";
            guild.LevelUpResult = "Succeeded";
        });

        internal GuildInput With(Action<GuildInput> change)
        {
            var copy = (GuildInput)MemberwiseClone();
            change(copy);
            return copy;
        }

        internal IReadOnlyDictionary<string, object> Arguments() => new Dictionary<string, object>
        {
            ["guildName"] = GuildName,
            ["memberCount"] = MemberCount,
            ["onlineCount"] = OnlineCount,
            ["treasury"] = Treasury,
            ["baseLevel"] = BaseLevel,
            ["maxBaseLevel"] = MaxBaseLevel,
            ["blacksmithUnlocked"] = BlacksmithUnlocked,
            ["levelUpCost"] = LevelUpCost,
            ["levelUp"] = LevelUp,
            ["levelUpPending"] = LevelUpPending,
            ["levelUpResult"] = LevelUpResult,
        };
    }

    // One personal blacksmith state as the server gives it. Defaults are the approved World values (「승인 A」 개인 강화):
    // level 0 of 1, attack bonus 0, the next step costs 20 gold and 1 Material, here while the facility is locked.
    internal sealed class BlacksmithInput
    {
        internal CharacterClass CharacterClass { get; set; } = CharacterClass.Knight;
        internal int EnhanceLevel { get; set; }
        internal int MaxEnhanceLevel { get; set; } = 1;
        internal int AttackBonus { get; set; }
        internal long GoldCost { get; set; } = 20;
        internal int MaterialCost { get; set; } = 1;
        internal string Enhance { get; set; } = "Locked";
        internal bool EnhancePending { get; set; }
        internal string EnhanceResult { get; set; } = "None";

        // Before the base reaches level 2 the blacksmith is locked.
        internal static BlacksmithInput Locked() => new BlacksmithInput();

        // Scene 8 before the press: unlocked, and the player owns 20 gold and 1 Material.
        internal static BlacksmithInput Ready() => Locked().With(smith => smith.Enhance = "Available");

        // Unlocked, but the player's gold or Material is short of the next step.
        internal static BlacksmithInput Short() => Locked().With(smith => smith.Enhance = "Insufficient");

        // Scene 8 after the confirmed result: level 1 of 1 with attack +5, no next step and so no next price.
        internal static BlacksmithInput Enhanced() => Locked().With(smith =>
        {
            smith.EnhanceLevel = 1;
            smith.AttackBonus = 5;
            smith.GoldCost = 0;
            smith.MaterialCost = 0;
            smith.Enhance = "MaxLevel";
            smith.EnhanceResult = "Succeeded";
        });

        internal BlacksmithInput With(Action<BlacksmithInput> change)
        {
            var copy = (BlacksmithInput)MemberwiseClone();
            change(copy);
            return copy;
        }

        internal IReadOnlyDictionary<string, object> Arguments() => new Dictionary<string, object>
        {
            ["characterClass"] = CharacterClass,
            ["enhanceLevel"] = EnhanceLevel,
            ["maxEnhanceLevel"] = MaxEnhanceLevel,
            ["attackBonus"] = AttackBonus,
            ["goldCost"] = GoldCost,
            ["materialCost"] = MaterialCost,
            ["enhance"] = Enhance,
            ["enhancePending"] = EnhancePending,
            ["enhanceResult"] = EnhanceResult,
        };
    }

    // The product's display source, supplied only with the explicit inputs above (the test is the fake supplier).
    internal sealed class GuildModelDriver
    {
        internal GuildModelDriver()
        {
            Type type = GuildProduct.Require("GuildDisplayModel");
            if (type.GetConstructor(Type.EmptyTypes) == null) Assert.Fail("RED unimplemented: public GuildDisplayModel()");
            Instance = Activator.CreateInstance(type);
        }

        internal object Instance { get; }

        internal int Subscribers => EventSubscribers.Count(Instance, "Changed");

        internal void SetGuild(GuildInput input)
        {
            object display = input == null ? null : GuildProduct.Construct("GuildStatusDisplay", input.Arguments());
            Set("SetGuild", "GuildStatusDisplay", display);
        }

        internal void SetBlacksmith(BlacksmithInput input)
        {
            object display = input == null ? null : GuildProduct.Construct("BlacksmithDisplay", input.Arguments());
            Set("SetBlacksmith", "BlacksmithDisplay", display);
        }

        void Set(string method, string displayType, object display)
        {
            MethodInfo set = GuildProduct.Method(Instance.GetType(), method, GuildProduct.Require(displayType));
            GuildProduct.Invoke(() => set.Invoke(Instance, new[] { display }));
        }
    }

    // One runtime window built under its own root the way a bootstrap builds one: BuildRuntime, then Unity's messages
    // in order (ViewLifecycle, EditMode sends none by itself). Each change is followed by a frame, so a window that
    // redraws in Update or LateUpdate is read after it had the chance to.
    internal sealed class GuildWindowDriver : IDisposable
    {
        internal const string Guild = "GuildWindow";
        internal const string Blacksmith = "BlacksmithWindow";

        // The value labels api-contract.md names; every other shown text is the window's state text.
        static readonly string[] ValueLabels =
            { "GuildName", "MemberCount", "OnlineCount", "Treasury", "BaseLevel", "LevelUpCost",
              "CharacterClass", "EnhanceLevel", "AttackBonus", "GoldCost", "MaterialCost" };
        static readonly Regex Number = new Regex(@"\d(?:[\d,]*\d)?");

        readonly Type _type;
        GameObject _root;
        int _intents;

        internal GuildWindowDriver(string windowType)
        {
            _type = GuildProduct.Require(windowType);
            ActionButtonName = windowType == Guild ? "LevelUp" : "Enhance";
            MethodInfo build = GuildProduct.Method(_type, "BuildRuntime", typeof(Transform));
            EventInfo intent = GuildProduct.ActionEvent(_type, windowType == Guild ? "LevelUpRequested" : "EnhanceRequested");
            _root = new GameObject("GuildWindowTestRoot");
            try
            {
                Window = (Component)GuildProduct.Invoke(() => build.Invoke(null, new object[] { _root.transform }));
                Assert.IsNotNull(Window, $"RED unimplemented: {_type.Name}.BuildRuntime returns the window");
                Assert.IsInstanceOf(_type, Window, $"{_type.Name}.BuildRuntime returns its own component");
                intent.AddEventHandler(Window, (Action)(() => _intents++));
                ViewLifecycle.Begin(_root);
                Frame();
            }
            catch
            {
                // A failed build leaves no object for the next test; the constructor never returned a driver to dispose.
                UnityEngine.Object.DestroyImmediate(_root);
                throw;
            }
        }

        internal string ActionButtonName { get; }

        internal Component Window { get; }

        internal int Intents => _intents;

        internal bool IsOpen => (bool)GuildProduct.Property(_type, "IsOpen", typeof(bool)).GetValue(Window);

        internal Button ActionButton => Find<Button>(ActionButtonName);

        public void Dispose()
        {
            if (_root != null) DestroyWindow();
        }

        internal void Bind(GuildModelDriver model)
        {
            MethodInfo bind = GuildProduct.Method(_type, "Bind", GuildProduct.Require("GuildDisplayModel"));
            GuildProduct.Invoke(() => bind.Invoke(Window, new[] { model?.Instance }));
            Frame();
        }

        internal void SetOpen(bool open) => CallBool("SetOpen", open);

        internal void SetInputAllowed(bool allowed) => CallBool("SetInputAllowed", allowed);

        // The button's onClick, as its click handler calls it: past the interactable flag and the CanvasGroup, so the
        // window's own state gate is what decides.
        internal void PressAction()
        {
            ActionButton.onClick.Invoke();
            Frame();
        }

        internal void Frame()
        {
            if (_root != null) ViewLifecycle.Frame(_root);
        }

        // A scene unload of the window's root: OnDisable, OnDestroy, then the objects are gone.
        internal void DestroyWindow()
        {
            ViewLifecycle.Destroy(_root);
            _root = null;
        }

        internal string Text(string name) => Find<TMP_Text>(name).text ?? string.Empty;

        internal bool IsLabelShown(string name) => IsShown(Find<TMP_Text>(name));

        // Every number in a label, with thousands separators; "Lv.1 / 2" gives 1 and 2.
        internal long[] Numbers(string name) => NumbersIn(Text(name));

        internal static long[] NumbersIn(string text) => Number.Matches(text ?? string.Empty)
            .Cast<Match>()
            .Select(match => long.Parse(match.Value.Replace(",", string.Empty), CultureInfo.InvariantCulture))
            .ToArray();

        internal string[] ShownTextsWithDigits() => Window.GetComponentsInChildren<TMP_Text>(true)
            .Where(text => IsShown(text) && NumbersIn(text.text).Length > 0)
            .Select(text => $"{text.name}='{text.text}'")
            .ToArray();

        // The window's state text: every shown TMP text except the named value labels (button captions included).
        internal string StateText() => string.Join(" | ", Window.GetComponentsInChildren<TMP_Text>(true)
            .Where(text => IsShown(text) && !ValueLabels.Contains(text.name))
            .Select(text => text.text)
            .Where(text => !string.IsNullOrEmpty(text))
            .OrderBy(text => text, StringComparer.Ordinal));

        internal string[] ButtonCaptions() => Window.GetComponentsInChildren<Button>(true)
            .Select(button => string.Join(" ", button.GetComponentsInChildren<TMP_Text>(true).Select(text => text.text)))
            .ToArray();

        // Graphics that take raycasts although they belong to no control: a window background that would stop
        // pointer attacks behind it.
        internal string[] RaycastTargetsOutsideControls() => Window.GetComponentsInChildren<Graphic>(true)
            .Where(graphic => graphic.raycastTarget && graphic.GetComponentInParent<Selectable>(true) == null)
            .Select(graphic => $"{graphic.name} ({graphic.GetType().Name})")
            .ToArray();

        internal WindowView View() => new WindowView(this);

        internal T Find<T>(string name) where T : Component
        {
            T[] found = Window.GetComponentsInChildren<T>(true).Where(item => item.name == name).ToArray();
            if (found.Length != 1)
            {
                Assert.Fail($"RED unimplemented: exactly one {typeof(T).Name} named '{name}' under {_type.Name} " +
                    $"(found {found.Length}, api-contract.md 3.1)");
            }
            return found[0];
        }

        void CallBool(string method, bool value)
        {
            MethodInfo call = GuildProduct.Method(_type, method, typeof(bool));
            GuildProduct.Invoke(() => call.Invoke(Window, new object[] { value }));
            Frame();
        }

        // Active, enabled, not faded out by a parent CanvasGroup and under enabled canvases (ScreenUiProbe's rule).
        internal static bool IsShown(Component component)
        {
            if (component == null || !component.gameObject.activeInHierarchy) return false;
            if (component is Behaviour behaviour && !behaviour.enabled) return false;
            if (component is TMP_Text text && text.color.a <= 0f) return false;
            if (component.GetComponentsInParent<CanvasGroup>().Any(group => group.alpha <= 0f)) return false;
            return component.GetComponentsInParent<Canvas>().All(canvas => canvas.enabled);
        }

        // Shown counts any drawn graphic of the window. Raycasts and clicks are read at the action button, the only
        // thing a pointer may hit: its graphic must be a target under an enabled raycaster and no parent group may
        // block raycasts (or, for clicks, interaction).
        internal readonly struct WindowView
        {
            internal WindowView(GuildWindowDriver driver)
            {
                Shown = driver.Window.GetComponentsInChildren<Graphic>(true).Any(IsShown);
                Button button = driver.ActionButton;
                Graphic graphic = button.targetGraphic;
                Canvas canvas = graphic != null ? graphic.canvas : null;
                GraphicRaycaster raycaster = canvas != null ? canvas.rootCanvas.GetComponent<GraphicRaycaster>() : null;
                CanvasGroup[] groups = button.GetComponentsInParent<CanvasGroup>();
                bool live = graphic != null && IsShown(graphic) && graphic.raycastTarget;
                TakesRaycasts = live && raycaster != null && raycaster.enabled && BlocksRaycasts(groups);
                TakesClicks = TakesRaycasts && button.enabled && button.interactable && Interactable(groups);
            }

            internal bool Shown { get; }

            internal bool TakesRaycasts { get; }

            internal bool TakesClicks { get; }

            internal string Describe() => $"shown={Shown}, raycasts={TakesRaycasts}, clicks={TakesClicks}";

            // Walks up until a group that ignores its parents, as uGUI does.
            static bool BlocksRaycasts(CanvasGroup[] innerToOuter)
            {
                foreach (CanvasGroup group in innerToOuter)
                {
                    if (!group.enabled) continue;
                    if (!group.blocksRaycasts) return false;
                    if (group.ignoreParentGroups) break;
                }
                return true;
            }

            static bool Interactable(CanvasGroup[] innerToOuter)
            {
                foreach (CanvasGroup group in innerToOuter)
                {
                    if (!group.enabled) continue;
                    if (!group.interactable) return false;
                    if (group.ignoreParentGroups) break;
                }
                return true;
            }
        }
    }
}
