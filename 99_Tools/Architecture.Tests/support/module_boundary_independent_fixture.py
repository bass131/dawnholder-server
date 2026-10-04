"""Independent verifier fixtures and public-entry harness for module boundaries.

Every source below is written by hand from the goal wording (three physical
folders, MB001/MB002/MB003, exact dispatcher exception). Nothing is generated
from the checker's policy file, product functions or the implementer's fixture.
"""

import json
import os
import pathlib
import signal
import subprocess
import time
import uuid


REPO = pathlib.Path(__file__).resolve().parents[3]
ENTRY = REPO / "99_Tools/Architecture/check-module-boundaries.sh"
RULES = REPO / "99_Tools/Architecture/Boundaries/module-boundaries.json"
SERVER = "02_Server/GameServer"
PROJECT = f"{SERVER}/GameServer.csproj"
NS = "Dawnholder.Server.GameServer"
WORK_VARIABLE = "MODULE_BOUNDARIES_INDEPENDENT_WORK"

PROJECT_XML = (
    '<Project Sdk="Microsoft.NET.Sdk">\n'
    "  <PropertyGroup>\n"
    "    <TargetFramework>net10.0</TargetFramework>\n"
    "    <Nullable>enable</Nullable>\n"
    "  </PropertyGroup>\n"
    "{extra}"
    "</Project>\n"
)

# Declarations that the probes reference. Paths are relative to SERVER.
BASE_SOURCES = {
    "Maps/WorldMap.cs": f"""namespace {NS}.Maps;

public class WorldMap
{{
    public event System.Action? Changed;

    public void Advance()
    {{
        Changed?.Invoke();
    }}

    public sealed class Cell
    {{
    }}
}}

public static class MapLimits
{{
    public const int MaxPlayers = 8;
}}

public enum MapPhase
{{
    Idle,
    Running,
}}

public abstract class MapListenerBase
{{
}}

[System.AttributeUsage(System.AttributeTargets.Class)]
public sealed class MapOwnedAttribute : System.Attribute
{{
}}

public static class MapKeyExtensions
{{
    public static string ToMapKey(this string value) => value;
}}
""",
    # Physically inside Maps/ although its namespace says otherwise.
    "Maps/Terrain.cs": "namespace Elsewhere.Hidden;\n\npublic class Terrain\n{\n}\n",
    "Handlers/Dispatch.cs": f"""namespace {NS}.Handlers;

public interface IPacketHandler
{{
    void Process();
}}

public static class HandlerRegistry
{{
    public static IPacketHandler? Resolve(int id) => null;
}}

public sealed class PingProbeHandler : IPacketHandler
{{
    public void Process()
    {{
    }}
}}
""",
    "Sessions/ClientSession.cs": f"""namespace {NS}.Sessions;

public class ClientSession
{{
    public void Enqueue(int request)
    {{
    }}

    public void Transmit(int packet)
    {{
    }}
}}
""",
    # Another GameServer folder, and a Maps namespace declared outside Maps/.
    "Combat/DamageTable.cs": f"namespace {NS}.Combat;\n\npublic static class DamageTable\n{{\n    public static int Base => 1;\n}}\n",
    "Misc/ShadowMap.cs": f"namespace {NS}.Maps;\n\npublic class ShadowMap\n{{\n}}\n",
}


def handler_class(name, body, usings=""):
    return f"{usings}namespace {NS}.Handlers;\n\npublic class {name}\n{{\n{body}\n}}\n"


def maps_class(name, body, header=None):
    header = header or f"public class {name}"
    return f"namespace {NS}.Maps;\n\n{header}\n{{\n{body}\n}}\n"


def sessions_class(name, body, usings=""):
    return f"{usings}namespace {NS}.Sessions;\n\npublic class {name}\n{{\n{body}\n}}\n"


MAPS = f"{NS}.Maps"
HANDLERS = f"{NS}.Handlers"

# Probes whose compiled reference crosses a forbidden direction.
WARNING_PROBES = {
    "Handlers/AliasField.cs": ("MB001", f"using Area = {MAPS}.WorldMap;\n\n" + handler_class(
        "AliasField", "    private Area? held;\n\n    public bool Has => held is not null;")),
    "Handlers/QualifiedReturn.cs": ("MB001", handler_class(
        "QualifiedReturn", f"    public global::{MAPS}.WorldMap? Make() => null;")),
    "Handlers/ParameterOnly.cs": ("MB001", handler_class(
        "ParameterOnly", f"    public void Accept({MAPS}.WorldMap map)\n    {{\n    }}")),
    "Handlers/MemberCall.cs": ("MB001", handler_class(
        "MemberCall", "    public void Run(WorldMap map) => map.Advance();", usings=f"using {MAPS};\n\n")),
    "Handlers/StaticMember.cs": ("MB001", handler_class(
        "StaticMember", f"    public int Limit => {MAPS}.MapLimits.MaxPlayers;")),
    "Handlers/Construct.cs": ("MB001", handler_class(
        "Construct", f"    public object Build() => new {MAPS}.WorldMap();")),
    "Handlers/Inherit.cs": ("MB001",
        f"namespace {NS}.Handlers;\n\npublic class Inherit : {MAPS}.MapListenerBase\n{{\n}}\n"),
    "Handlers/GenericArgument.cs": ("MB001", handler_class(
        "GenericArgument", f"    public System.Collections.Generic.List<{MAPS}.WorldMap> Items {{ get; }} = new();")),
    "Handlers/AttributeUse.cs": ("MB001",
        f"namespace {NS}.Handlers;\n\n[{MAPS}.MapOwned]\npublic class AttributeUse\n{{\n}}\n"),
    "Handlers/ExtensionCall.cs": ("MB001", handler_class(
        "ExtensionCall", "    public string Key(string raw) => raw.ToMapKey();", usings=f"using {MAPS};\n\n")),
    "Handlers/UsingStatic.cs": ("MB001", handler_class(
        "UsingStatic", "    public int Limit() => MaxPlayers;", usings=f"using static {MAPS}.MapLimits;\n\n")),
    "Handlers/EnumMember.cs": ("MB001", handler_class(
        "EnumMember", f"    public object Phase => {MAPS}.MapPhase.Running;")),
    "Handlers/NestedType.cs": ("MB001", handler_class(
        "NestedType", f"    public {MAPS}.WorldMap.Cell? Cell {{ get; set; }}")),
    "Handlers/TypeOf.cs": ("MB001", handler_class(
        "TypeOf", f"    public System.Type Kind => typeof({MAPS}.WorldMap);")),
    "Handlers/EventSubscribe.cs": ("MB001", handler_class(
        "EventSubscribe",
        f"    public void Hook({MAPS}.WorldMap map) => map.Changed += Noop;\n\n    private void Noop()\n    {{\n    }}")),
    "Handlers/PhysicalFolderWins.cs": ("MB001", handler_class(
        "PhysicalFolderWins", "    public Elsewhere.Hidden.Terrain? Ground { get; set; }")),
    "Maps/ReverseField.cs": ("MB002", maps_class(
        "ReverseField", f"    public {HANDLERS}.PingProbeHandler? Handler {{ get; set; }}")),
    # The MB003 dispatcher exception must not leak into MB002.
    "Maps/ReverseRegistry.cs": ("MB002", maps_class(
        "ReverseRegistry", f"    public object? Lookup() => {HANDLERS}.HandlerRegistry.Resolve(1);")),
    "Maps/ReverseInterface.cs": ("MB002", maps_class(
        "MapSideHandler", "    public void Process()\n    {\n    }",
        header=f"public sealed class MapSideHandler : {HANDLERS}.IPacketHandler")),
    "Sessions/ConcreteField.cs": ("MB003", sessions_class(
        "ConcreteField", f"    public {HANDLERS}.PingProbeHandler? Probe {{ get; set; }}")),
    "Sessions/ConcreteConstruct.cs": ("MB003", sessions_class(
        "ConcreteConstruct", f"    public object Build() => new {HANDLERS}.PingProbeHandler();")),
    "Sessions/ConcreteCast.cs": ("MB003", sessions_class(
        "ConcreteCast",
        f"    public object? Narrow({HANDLERS}.IPacketHandler handler) => handler as {HANDLERS}.PingProbeHandler;")),
}

# Allowed directions, same-area, excluded targets and non-compiled text.
CLEAN_PROBES = {
    "Handlers/SubmitToSession.cs": handler_class(
        "SubmitToSession", f"    public void Run({NS}.Sessions.ClientSession session) => session.Enqueue(7);"),
    "Handlers/SameArea.cs": handler_class("SameArea", "    public void Run(PingProbeHandler probe) => probe.Process();"),
    "Handlers/OtherArea.cs": handler_class("OtherArea", f"    public int Value => {NS}.Combat.DamageTable.Base;"),
    "Handlers/ExternalTypes.cs": handler_class(
        "ExternalTypes",
        "    public System.Text.StringBuilder Text { get; } = new();\n\n"
        "    public System.Collections.Generic.List<int> Numbers { get; } = new();"),
    "Handlers/NamespaceOutsideFolder.cs": handler_class(
        "NamespaceOutsideFolder", f"    public {MAPS}.ShadowMap? Shadow {{ get; set; }}"),
    "Handlers/NamespaceUsingOnly.cs": handler_class(
        "NamespaceUsingOnly", "    public int Zero => 0;", usings=f"using {MAPS};\n\n"),
    "Handlers/TextOnly.cs": f"""namespace {NS}.Handlers;

/// <summary>Mentions <see cref="{MAPS}.WorldMap"/> only in documentation.</summary>
public class TextOnly
{{
    // {MAPS}.WorldMap.Advance()
    /* {MAPS}.MapLimits.MaxPlayers */
    public string Name => "{MAPS}.WorldMap";

    public System.Type? Reflected => System.Type.GetType("{MAPS}.WorldMap");
#if MODULE_BOUNDARY_INDEPENDENT_NEVER_DEFINED
    public {MAPS}.WorldMap Hidden => new();
#endif
}}
""",
    # Generated boundary files are not hand-written targets.
    "Handlers/GeneratedProbe.g.cs": handler_class("GeneratedProbe", f"    public {MAPS}.WorldMap? Map {{ get; set; }}"),
    "Maps/SendViaSession.cs": maps_class(
        "SendViaSession", f"    public void Push({NS}.Sessions.ClientSession session) => session.Transmit(3);"),
    "Maps/SameAreaMaps.cs": maps_class("SameAreaMaps", "    public void Step(WorldMap map) => map.Advance();"),
    "Sessions/DriveMap.cs": sessions_class("DriveMap", f"    public void Tick({MAPS}.WorldMap map) => map.Advance();"),
    "Sessions/DispatchExact.cs": sessions_class(
        "DispatchExact",
        "    public void Route(int id)\n    {\n"
        "        if (HandlerRegistry.Resolve(id) is IPacketHandler handler)\n        {\n"
        "            handler.Process();\n        }\n    }",
        usings=f"using {HANDLERS};\n\n"),
}


def write_fixture(root, sources, project_extra="", extra_files=None):
    """Write a new explicit fixture root; refuse to reuse an existing one."""
    root = pathlib.Path(root)
    if root.exists():
        raise FileExistsError(f"Fixture root must be new: {root}")
    files = {
        "global.json": json.dumps({"sdk": {"version": "10.0.301", "rollForward": "disable"}}, indent=2) + "\n",
        "Directory.Build.props": "<Project />\n",
        PROJECT: PROJECT_XML.format(extra=project_extra),
    }
    files.update({f"{SERVER}/{relative}": text for relative, text in sources.items()})
    files.update(extra_files or {})
    for relative, text in files.items():
        target = root / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(text, encoding="utf-8")
    return root


def matrix_sources():
    sources = dict(BASE_SOURCES)
    sources.update({path: text for path, (_, text) in WARNING_PROBES.items()})
    sources.update(CLEAN_PROBES)
    return sources


def clean_sources():
    sources = dict(BASE_SOURCES)
    sources.update(CLEAN_PROBES)
    return sources


def work_root():
    """Independent runs need an explicit owned evidence root; no default writes."""
    value = os.environ.get(WORK_VARIABLE)
    return pathlib.Path(value) if value else None


class Invocation:
    """One public-entry run and its preserved raw files."""

    def __init__(self, run, command, completed_returncode, stdout, stderr, elapsed):
        self.run = run
        self.command = command
        self.returncode = completed_returncode
        self.stdout = stdout
        self.stderr = stderr
        self.elapsed = elapsed
        result_path = run / "result" / "result.json"
        self.result = json.loads(result_path.read_text(encoding="utf-8")) if result_path.is_file() else None


def record(run, command, returncode, stdout, stderr, elapsed, environment_overrides):
    raw = {
        "argv": [str(part) for part in command], "cwd": str(REPO), "exitCode": returncode,
        "elapsedSeconds": elapsed, "environmentOverrides": environment_overrides,
    }
    (run / "invocation.json").write_text(json.dumps(raw, indent=2) + "\n", encoding="utf-8")
    (run / "stdout.txt").write_text(stdout, encoding="utf-8")
    (run / "stderr.txt").write_text(stderr, encoding="utf-8")


def new_run(work, label):
    run = work / f"{label}-{uuid.uuid4().hex[:12]}"
    run.mkdir(parents=True)
    return run


def invoke(work, label, arguments, environment=None, source_root=None, output=None):
    """Run the public Bash entry with argv only; raw files stay under work."""
    run = new_run(work, label)
    output = output or run / "result"
    command = ["bash", str(ENTRY)]
    if source_root is not None:
        command += ["--source-root", str(source_root)]
    command += ["--output-root", str(output), *map(str, arguments)]
    env = dict(os.environ)
    env.update(environment or {})
    started = time.perf_counter()
    completed = subprocess.run(command, cwd=REPO, env=env, capture_output=True, text=True, timeout=900)
    elapsed = time.perf_counter() - started
    record(run, command, completed.returncode, completed.stdout, completed.stderr, elapsed, environment or {})
    return Invocation(run, command, completed.returncode, completed.stdout, completed.stderr, elapsed)


def invoke_fixture(work, label, sources, arguments=(), environment=None, project_extra="", extra_files=None):
    run_parent = new_run(work, label + "-source")
    source = write_fixture(run_parent / "source", sources, project_extra, extra_files)
    return invoke(work, label, ["--input-kind", "fixture", *arguments], environment, source_root=source)


def cancel_after_stage(work, label, sources, stage_name, deadline=240):
    """Start the public entry, send SIGTERM once the named stage has started."""
    run = new_run(work, label)
    source = write_fixture(run / "source", sources)
    output = run / "result"
    command = ["bash", str(ENTRY), "--source-root", str(source), "--output-root", str(output), "--input-kind", "fixture"]
    started = time.perf_counter()
    stdout_path, stderr_path = run / "stdout.txt", run / "stderr.txt"
    with stdout_path.open("w", encoding="utf-8") as stdout, stderr_path.open("w", encoding="utf-8") as stderr:
        # Own session: cleanup below can only ever target this verifier's group.
        process = subprocess.Popen(command, cwd=REPO, stdout=stdout, stderr=stderr, start_new_session=True)
        signalled = False
        try:
            while time.perf_counter() - started < deadline and process.poll() is None:
                stages = output / "stages"
                if stages.is_dir() and any(path.name.endswith("-" + stage_name) for path in stages.iterdir()):
                    time.sleep(1.0)
                    process.send_signal(signal.SIGTERM)
                    signalled = True
                    break
                time.sleep(0.1)
            returncode = process.wait(timeout=120)
        finally:
            if process.poll() is None:
                os.killpg(process.pid, signal.SIGKILL)
                process.wait()
    elapsed = time.perf_counter() - started
    raw = {
        "argv": command, "cwd": str(REPO), "exitCode": returncode, "elapsedSeconds": elapsed,
        "signal": "SIGTERM" if signalled else None, "signalAfterStage": stage_name,
    }
    (run / "invocation.json").write_text(json.dumps(raw, indent=2) + "\n", encoding="utf-8")
    result_path = output / "result.json"
    result = json.loads(result_path.read_text(encoding="utf-8")) if result_path.is_file() else None
    return returncode, signalled, result, output


def group_alive(pid):
    """True while any process remains in the group led by pid."""
    try:
        os.killpg(pid, 0)
    except ProcessLookupError:
        return False
    except PermissionError:
        return True
    return True


def position(text, needle):
    """1-based line/column of the first needle in hand-written source text."""
    offset = text.index(needle)
    line = text.count("\n", 0, offset) + 1
    column = offset - (text.rfind("\n", 0, offset) + 1) + 1
    return line, column
