#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Owns only temporary in-memory sizes. The external capture runner owns the file baseline,
    // process lifetime and final receipt; no method here saves or restores Editor preferences.
    internal sealed class GameViewSizeScope : IDisposable
    {
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        readonly object _sizes;
        readonly object _group;
        readonly Type _sizeType;
        readonly Type _kindType;
        readonly MethodInfo _add;
        readonly MethodInfo _remove;
        readonly GroupState[] _before;
        readonly List<object> _added = new();
        readonly Dictionary<object, string> _addedValues = new();
        readonly Permit _permit;
        readonly string _output;
        bool _closed;

        GameViewSizeScope(object group, Permit permit, string output)
        {
            _group = group;
            _permit = permit;
            _output = output;
            Assembly editor = typeof(EditorWindow).Assembly;
            Type sizesType = RequireType(editor, "UnityEditor.GameViewSizes");
            _sizeType = RequireType(editor, "UnityEditor.GameViewSize");
            _kindType = RequireType(editor, "UnityEditor.GameViewSizeType");
            Type groupType = RequireType(editor, "UnityEditor.GameViewSizeGroup");
            Require(group.GetType() == groupType, "unexpected current group type");
            _sizes = Property(sizesType, "instance").GetValue(null);
            Require(ReferenceEquals(Property(sizesType, "currentGroup").GetValue(_sizes), group), "current group changed");
            Require(Property(sizesType, "currentGroupType").GetValue(_sizes).ToString() == "Standalone", "Standalone group required");
            var filePath = Method(sizesType.BaseType, "GetFilePath", typeof(string), Type.EmptyTypes);
            Require(SamePath((string)filePath.Invoke(null, null), permit._target), "Unity serialized preferences path differs");
            _add = Method(groupType, "AddCustomSize", typeof(void), new[] { _sizeType });
            _remove = Method(groupType, "RemoveCustomSize", typeof(void), new[] { typeof(int) });
            Method(groupType, "GetBuiltinCount", typeof(int), Type.EmptyTypes);
            Method(groupType, "GetTotalCount", typeof(int), Type.EmptyTypes);
            Method(groupType, "GetGameViewSize", _sizeType, new[] { typeof(int) });
            Require(_sizeType.GetConstructor(new[] { _kindType, typeof(int), typeof(int), typeof(string) }) != null,
                "GameViewSize constructor differs");
            foreach (string name in new[] { "width", "height" })
                Require(Property(_sizeType, name).PropertyType == typeof(int), name + " property differs");
            Require(Property(_sizeType, "sizeType").PropertyType == _kindType, "sizeType property differs");
            Require(Property(_sizeType, "baseText").PropertyType == typeof(string), "baseText property differs");
            _before = new[] { "m_Standalone", "m_iOS", "m_Android", "m_HMD" }.Select(name =>
            {
                FieldInfo field = sizesType.GetField(name, Any);
                Require(field != null && field.FieldType == groupType, name + " serialized group differs");
                return new GroupState(name, field.GetValue(_sizes));
            }).ToArray();
            Require(_before.Any(state => ReferenceEquals(state.Group, group)), "current group not in serialized groups");
            Require(Enumerable.Range(0, Count(group, "GetBuiltinCount")).Any(index =>
            {
                object size = Size(group, index);
                return Matches(size, 1920, 1080) && (string)Read(size, "baseText") == "Full HD";
            }), "existing built-in Full HD 1920x1080 is required");
        }

        public void Dispose()
        {
            if (_closed) return;
            Exception failure = null;
            bool restored = false;
            try
            {
                AssertOwned();
                for (int index = _added.Count - 1; index >= 0; index--)
                {
                    _remove.Invoke(_group, new object[] { Count(_group, "GetTotalCount") - 1 });
                    _added.RemoveAt(index);
                }
                AssertOwned();
                restored = true;
            }
            catch (Exception error) { failure = error; }
            _closed = true;
            WriteReceipt("gameview-scope-closed.json", "closed", restored, failure?.ToString());
            if (failure != null) throw failure;
        }

        internal static GameViewSizeScope Open(object group)
        {
            string output = Environment.GetEnvironmentVariable("DAWNHOLDER_UI_CAPTURE_DIR");
            Require(!string.IsNullOrEmpty(output), "capture output missing");
            string permitPath = Environment.GetEnvironmentVariable("DAWNHOLDER_GAMEVIEW_PERMIT");
            Require(!string.IsNullOrEmpty(permitPath) && SamePath(permitPath, Path.Combine(output, "gameview-permit.json")),
                "permit must be the runner's exact capture output file");
            Permit permit = JsonUtility.FromJson<Permit>(File.ReadAllText(permitPath));
            ValidatePermit(permit);
            var scope = new GameViewSizeScope(group, permit, output);
            try
            {
                scope.WriteReceipt("gameview-scope-before.json", "before", false, null);
                foreach (RequestedSize size in permit._sizes) scope.EnsureSize(size._width, size._height);
                scope.AssertOwned();
                scope.WriteReceipt("gameview-scope-ready.json", "ready", false, null);
                return scope;
            }
            catch (Exception prepareError)
            {
                try { scope.Dispose(); }
                catch (Exception cleanupError) { throw new AggregateException(prepareError, cleanupError); }
                throw;
            }
        }

        internal void AssertOwned()
        {
            Require(!_closed, "scope already closed");
            ValidatePermit(_permit);
            Require(ReferenceEquals(Read(_sizes, "currentGroup"), _group), "current platform group changed");
            foreach (GroupState state in _before)
            {
                object[] expected = ReferenceEquals(state.Group, _group) ? state.Items.Concat(_added).ToArray() : state.Items;
                Require(Count(state.Group, "GetBuiltinCount") == state.BuiltinCount, state.Name + " built-in count changed");
                Require(Count(state.Group, "GetTotalCount") == expected.Length, state.Name + " size count changed");
                for (int index = 0; index < expected.Length; index++)
                {
                    object actual = Size(state.Group, index);
                    Require(ReferenceEquals(actual, expected[index]), state.Name + " entry ownership changed at " + index);
                    string original = index < state.Items.Length ? state.Values[index] : _addedValues[actual];
                    Require(Value(actual) == original, state.Name + " entry content changed at " + index);
                }
            }
        }

        void EnsureSize(int width, int height)
        {
            AssertOwned();
            if (Enumerable.Range(0, Count(_group, "GetTotalCount")).Any(index => Matches(Size(_group, index), width, height))) return;
            object size = Activator.CreateInstance(_sizeType, Enum.Parse(_kindType, "FixedResolution"), width, height,
                "Temporary capture " + width + "x" + height);
            int count = Count(_group, "GetTotalCount");
            try { _add.Invoke(_group, new[] { size }); }
            finally
            {
                // An invocation may throw after mutating the list; retain only an observed owned append.
                if (Count(_group, "GetTotalCount") == count + 1 && ReferenceEquals(Size(_group, count), size))
                {
                    _added.Add(size);
                    _addedValues.Add(size, "Temporary capture " + width + "x" + height + "|FixedResolution|" + width + "|" + height);
                }
            }
            Require(_added.Contains(size), "custom append was not observed");
        }

        void WriteReceipt(string name, string stage, bool restored, string error)
        {
            var receipt = new ScopeReceipt
            {
                _taskId = _permit._taskId, _dispatchId = _permit._dispatchId, _token = _permit._token,
                _utc = DateTime.UtcNow.ToString("o"), _stage = stage, _memoryRestored = restored, _error = error,
                _groups = _before.Select(state => new GroupReceipt { _name = state.Name,
                    _builtinCount = Count(state.Group, "GetBuiltinCount"),
                    _entries = Enumerable.Range(0, Count(state.Group, "GetTotalCount")).Select(index => Value(Size(state.Group, index))).ToArray() }).ToArray(),
            };
            using (var stream = new FileStream(Path.Combine(_output, name), FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(stream)) writer.Write(JsonUtility.ToJson(receipt, true));
        }

        static void ValidatePermit(Permit permit)
        {
            Require(permit != null && permit._schema == 1 && permit._mode == "memory-only", "memory-only permit required");
            Require(Application.unityVersion == "6000.6.4f1" && !Application.isBatchMode, "approved GUI Editor version required");
            Require(Regex.IsMatch(permit._token ?? "", "^[a-f0-9]{64}$")
                && permit._token == Environment.GetEnvironmentVariable("DAWNHOLDER_GAMEVIEW_TOKEN"), "scope token differs");
            Require(Regex.IsMatch(permit._taskId ?? "", "^task_[a-f0-9]+$")
                && permit._taskId == Environment.GetEnvironmentVariable("DAWNHOLDER_CAPTURE_TASK"), "Task differs");
            Require(Regex.IsMatch(permit._dispatchId ?? "", "^ctx_[a-f0-9]+$")
                && permit._dispatchId == Environment.GetEnvironmentVariable("DAWNHOLDER_CAPTURE_DISPATCH"), "Dispatch differs");
            Require(SamePath(permit._projectPath, Directory.GetParent(Application.dataPath).FullName), "project differs");
            Require(SamePath(permit._target, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Unity", "Editor-5.x", "Preferences", "GameViewSizes.asset")), "preferences target differs");
            Require(Regex.IsMatch(permit._deadlineUtc ?? "", "^\\d{4}-\\d{2}-\\d{2}T.*(Z|[+-]\\d{2}:\\d{2})$")
                && DateTimeOffset.TryParse(permit._deadlineUtc, CultureInfo.InvariantCulture, DateTimeStyles.None, out var deadline)
                && DateTimeOffset.UtcNow < deadline, "scope allocation expired");
            using (Process current = Process.GetCurrentProcess())
                Require(current.Id == permit._unityPid && Birth(current) == permit._unityBirth, "Unity PID/birth differs");
            using (Process owner = Process.GetProcessById(permit._ownerPid))
                Require(!owner.HasExited && Birth(owner) == permit._ownerBirth, "runner owner missing or reused");
            Require(permit._sizes != null && permit._sizes.Length > 0 && permit._sizes.Length <= 3, "approved sizes missing");
            var seen = new HashSet<string>();
            foreach (RequestedSize size in permit._sizes)
            {
                string value = size._width + "x" + size._height;
                Require(new[] { "709x399", "802x451", "1280x720" }.Contains(value) && seen.Add(value), "unapproved or duplicate size");
            }
        }

        static string Birth(Process process) => process.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture);
        static bool SamePath(string left, string right) => !string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(right)
            && string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
        static bool Matches(object size, int width, int height) => Read(size, "sizeType").ToString() == "FixedResolution"
            && (int)Read(size, "width") == width && (int)Read(size, "height") == height;
        static string Value(object size) => Read(size, "baseText") + "|" + Read(size, "sizeType") + "|"
            + Read(size, "width") + "|" + Read(size, "height");
        static int Count(object group, string method) => (int)group.GetType().GetMethod(method, Any).Invoke(group, null);
        static object Size(object group, int index) => group.GetType().GetMethod("GetGameViewSize", Any).Invoke(group, new object[] { index });
        static object Read(object owner, string name) => Property(owner.GetType(), name).GetValue(owner);
        static PropertyInfo Property(Type type, string name) => type.GetProperty(name, Any)
            ?? throw new InvalidOperationException("GameView API property missing: " + name);
        static Type RequireType(Assembly assembly, string name) => assembly.GetType(name)
            ?? throw new InvalidOperationException("GameView API type missing: " + name);
        static MethodInfo Method(Type type, string name, Type result, Type[] arguments)
        {
            MethodInfo method = type.GetMethod(name, Any, null, arguments, null);
            Require(method != null && method.ReturnType == result, "GameView API method differs: " + name);
            return method;
        }
        static void Require(bool condition, string error)
        {
            if (!condition) throw new InvalidOperationException("GameView scope: " + error);
        }

        sealed class GroupState
        {
            internal GroupState(string name, object group)
            {
                Name = name;
                Group = group;
                BuiltinCount = Count(group, "GetBuiltinCount");
                Items = Enumerable.Range(0, Count(group, "GetTotalCount")).Select(index => Size(group, index)).ToArray();
                Values = Items.Select(Value).ToArray();
            }

            internal string Name { get; }
            internal object Group { get; }
            internal int BuiltinCount { get; }
            internal object[] Items { get; }
            internal string[] Values { get; }
        }

        [Serializable] sealed class Permit
        {
            public int _schema, _unityPid, _ownerPid;
            public string _mode, _taskId, _dispatchId, _token, _target, _projectPath, _deadlineUtc, _unityBirth, _ownerBirth;
            public RequestedSize[] _sizes;
        }
        [Serializable] sealed class RequestedSize { public int _width, _height; }
        [Serializable] sealed class ScopeReceipt
        {
            public string _taskId, _dispatchId, _token, _utc, _stage, _error;
            public bool _memoryRestored;
            public GroupReceipt[] _groups;
        }
        [Serializable] sealed class GroupReceipt { public string _name; public int _builtinCount; public string[] _entries; }
    }
}
#endif
