"""Synthetic comparison inputs shaped like the frozen artifacts.

Product sources, frozen answers and extractor binaries are never used here. Builders return
plain data so a test can change one field and observe the public CLI's reaction.
"""
import copy
import hashlib
import json
import os
import pathlib
import subprocess
import sys
import tempfile

TESTS_ROOT = pathlib.Path(__file__).resolve().parents[1]
REPOSITORY_ROOT = TESTS_ROOT.parents[1]
CLI = REPOSITORY_ROOT / "99_Tools" / "Architecture" / "Pipeline" / "cli.py"

SOURCE_COMMIT = "0123456789abcdef0123456789abcdef01234567"
INCLUDED_ROOTS = ["02_Server/Game", "02_Server/Network", "04_ClientNet", "98_Shared"]
EXCLUDED_ROOTS = ["02_Server/Game.Tests"]

# key: (kind, namespace, type, member, parameters, path, line, layer)
# Two ProcessAttack methods share one qualified name and differ only by declaration line.
# Two TryValidate methods share a type name in different namespaces.
DECLARATIONS = {
    "serverHandler": ("type", "Game.Handlers", "IPacketHandler", None, None, "02_Server/Game/Handlers/IPacketHandler.cs", 3, "Server"),
    "attackType": ("type", "Game.Handlers", "AttackHandler", None, None, "02_Server/Game/Handlers/AttackHandler.cs", 5, "Server"),
    "attackHandle": ("method", "Game.Handlers", "AttackHandler", "Handle", ["System.Int32"], "02_Server/Game/Handlers/AttackHandler.cs", 7, "Server"),
    "attackPacket": ("type", "Shared.Protocol", "C_Attack", None, None, "98_Shared/Packets.cs", 3, "Shared"),
    "attackRead": ("method", "Shared.Protocol", "C_Attack", "Read", ["System.Int32"], "98_Shared/Packets.cs", 5, "Shared"),
    "movePacket": ("type", "Shared.Protocol", "C_Move", None, None, "98_Shared/Packets.cs", 9, "Shared"),
    "moveRead": ("method", "Shared.Protocol", "C_Move", "Read", ["System.Int32"], "98_Shared/Packets.cs", 11, "Shared"),
    "mapType": ("type", "Game.Maps", "GameMap", None, None, "02_Server/Game/Maps/GameMap.cs", 5, "Server"),
    "mapAttack": ("method", "Game.Maps", "GameMap", "ProcessAttack", ["System.Int32"], "02_Server/Game/Maps/GameMap.cs", 7, "Server"),
    "combatType": ("type", "Game.Maps.Systems", "CombatSystem", None, None, "02_Server/Game/Maps/Systems/CombatSystem.cs", 3, "Server"),
    "combatAttack": ("method", "Game.Maps.Systems", "CombatSystem", "ProcessAttack", ["Game.Maps.GameMap", "System.Int32"], "02_Server/Game/Maps/Systems/CombatSystem.cs", 5, "Server"),
    "combatAttackLong": ("method", "Game.Maps.Systems", "CombatSystem", "ProcessAttack", ["Game.Maps.GameMap", "System.Int64"], "02_Server/Game/Maps/Systems/CombatSystem.cs", 9, "Server"),
    "sessionType": ("type", "Client.Net", "PacketSession", None, None, "04_ClientNet/PacketSession.cs", 3, "ClientNet"),
    "netRecv": ("method", "Client.Net", "PacketSession", "OnRecv", ["System.Int32"], "04_ClientNet/PacketSession.cs", 5, "ClientNet"),
    "clientValidatorType": ("type", "Client.Net", "FrameValidator", None, None, "04_ClientNet/FrameValidator.cs", 3, "ClientNet"),
    "clientValidate": ("method", "Client.Net", "FrameValidator", "TryValidate", ["System.UInt16"], "04_ClientNet/FrameValidator.cs", 5, "ClientNet"),
    "serverValidatorType": ("type", "Server.Network", "FrameValidator", None, None, "02_Server/Network/FrameValidator.cs", 3, "Server"),
    "serverValidate": ("method", "Server.Network", "FrameValidator", "TryValidate", ["System.UInt16"], "02_Server/Network/FrameValidator.cs", 5, "Server"),
    "legacyType": ("type", "Game.MapsLegacy", "OldMap", None, None, "02_Server/Game/MapsLegacy/OldMap.cs", 3, "Server"),
}

SCOPE_SYMBOLS = ["serverHandler", "attackType", "attackHandle", "attackPacket", "attackRead", "movePacket", "moveRead", "mapAttack", "combatAttack", "combatAttackLong", "netRecv", "clientValidate"]

SCOPE_GROUPS = [
    {"id": "handlerInterfaces", "kind": "implements", "sources": ["attackType"], "targets": ["serverHandler"]},
    {"id": "packetUsage", "kind": "usesType", "sources": ["attackHandle"], "targets": ["attackPacket", "movePacket"]},
    {"id": "attackCalls", "kind": "calls", "sources": ["attackHandle"], "targets": ["attackRead", "moveRead", "mapAttack"]},
    {"id": "delegation", "kind": "calls", "sources": ["mapAttack"], "targets": ["combatAttack", "combatAttackLong", "mapAttack"]},
    {"id": "clientFrame", "kind": "calls", "sources": ["netRecv"], "targets": ["clientValidate"]},
]

# (kind, source, target, line, column, context) of the synthetic code's real static relations.
RELATIONS = [
    ("implements", "attackType", "serverHandler", 5, 30, "declaration"),
    ("usesType", "attackHandle", "attackPacket", 9, 9, "direct"),
    ("calls", "attackHandle", "attackRead", 10, 9, "direct"),
    ("calls", "attackHandle", "mapAttack", 12, 13, "deferredLambda"),
    ("calls", "mapAttack", "combatAttack", 8, 9, "direct"),
    ("calls", "netRecv", "clientValidate", 7, 13, "direct"),
]

POSITIVES = [(kind, source, target) for kind, source, target, _, _, _ in RELATIONS]


def documentation_id(key):
    kind, namespace, type_name, member, parameters, _, _, _ = DECLARATIONS[key]
    if kind == "type":
        return f"T:{namespace}.{type_name}"
    return f"M:{namespace}.{type_name}.{member}({','.join(parameters)})"


def source_paths():
    return sorted({declaration[5] for declaration in DECLARATIONS.values()})


def manifest():
    files = []
    for path in source_paths():
        content = f"// synthetic {path}\n".encode()
        files.append({
            "path": path,
            "sha256": hashlib.sha256(content).hexdigest(),
            "bytes": len(content),
            "gitBlobId": hashlib.sha1(b"blob " + str(len(content)).encode() + b"\0" + content).hexdigest(),
        })
    return {
        "schemaVersion": 1,
        "sourceCommit": SOURCE_COMMIT,
        "includedRoots": list(INCLUDED_ROOTS),
        "excludedRoots": list(EXCLUDED_ROOTS),
        "files": files,
        "externalReferenceRoots": [],
        "externalReferences": [],
    }


def git_metadata(manifest_value):
    return {"sourceCommit": manifest_value["sourceCommit"], "tree": {item["path"]: item["gitBlobId"] for item in manifest_value["files"]}}


def scope():
    symbols = {}
    for key in SCOPE_SYMBOLS:
        kind, namespace, type_name, _, _, path, line, layer = DECLARATIONS[key]
        symbols[key] = {"kind": kind, "namespace": namespace, "type": type_name, "documentationId": documentation_id(key), "path": path, "declarationLine": line, "layer": layer}
    return {"schemaVersion": 1, "sourceCommit": SOURCE_COMMIT, "symbols": symbols, "groups": copy.deepcopy(SCOPE_GROUPS)}


def truth():
    candidates = sorted((group["kind"], source, target) for group in SCOPE_GROUPS for source in group["sources"] for target in group["targets"])
    positives = [{"id": f"P{index:02}", "kind": kind, "source": source, "target": target} for index, (kind, source, target) in enumerate(POSITIVES, 1)]
    negatives = [{"id": f"N{index:02}", "kind": kind, "source": source, "target": target} for index, (kind, source, target) in enumerate([c for c in candidates if c not in POSITIVES], 1)]
    return {"schemaVersion": 1, "sourceCommit": SOURCE_COMMIT, "positives": positives, "negatives": negatives, "counts": {"positive": len(positives), "negative": len(negatives), "candidates": len(candidates)}}


def roslyn_key(key):
    return f"{documentation_id(key)}|{DECLARATIONS[key][5]}"


def roslyn_raw():
    symbols = []
    for key, (kind, namespace, type_name, member, parameters, path, line, _) in DECLARATIONS.items():
        signature = f"{namespace}.{type_name}" if kind == "type" else f"{namespace}.{type_name}.{member}({', '.join(parameters)})"
        symbols.append({"key": roslyn_key(key), "kind": kind, "name": member or type_name, "namespace": namespace, "signature": signature, "documentationId": documentation_id(key), "source": {"path": path, "line": line, "column": 5}, "resolution": "resolved"})
    external = "M:System.Console.WriteLine(System.String)|@external"
    symbols.append({"key": external, "kind": "method", "name": "WriteLine", "namespace": "System", "signature": "System.Console.WriteLine(string)", "documentationId": "M:System.Console.WriteLine(System.String)", "source": None, "resolution": "external"})
    relations = []
    for kind, source, target, line, column, context in RELATIONS:
        relations.append(_roslyn_relation(source, roslyn_key(target), kind, line, column, context))
    relations.append(_roslyn_relation("netRecv", external, "calls", 8, 13, "direct"))
    return {
        "rawVersion": 1,
        "extractor": "Roslyn",
        "version": "5.6.0.0",
        "status": "partial",
        "sourceCommit": SOURCE_COMMIT,
        "diagnostics": [{"kind": "limitation", "severity": "Warning", "message": "synthetic fixture"}],
        "symbols": symbols,
        "relations": relations,
        "supports": {"calls": True, "contains": True, "implements": True, "usesType": True},
        "coverage": source_paths(),
    }


def _roslyn_relation(source, target_key, kind, line, column, context, resolution="resolved", candidates=None, target_text=None):
    path = DECLARATIONS[source][5]
    return {"sourceKey": roslyn_key(source), "targetKey": target_key, "kind": kind, "evidence": {"path": path, "line": line, "column": column}, "context": context, "resolution": resolution, "targetText": target_text, "candidates": candidates or []}


def roslyn_unresolved(source, kind, line, column, target_text, candidates, resolution):
    return _roslyn_relation(source, None, kind, line, column, "direct", resolution=resolution, candidates=candidates, target_text=target_text)


def codegraph_id(key):
    return f"{DECLARATIONS[key][0]}:{key}"


def codegraph_raw():
    nodes = [{"id": f"file:{path}", "kind": "file", "name": pathlib.PurePosixPath(path).name, "qualified_name": path, "file_path": path, "start_line": 1, "start_column": 0, "signature": None} for path in source_paths()]
    for key, (kind, namespace, type_name, member, _, path, line, _) in DECLARATIONS.items():
        raw_kind = "interface" if type_name.startswith("I") and kind == "type" and type_name[1].isupper() else "class" if kind == "type" else "method"
        qualified = f"{namespace}::{type_name}" if kind == "type" else f"{namespace}::{type_name}::{member}"
        nodes.append({"id": codegraph_id(key), "kind": raw_kind, "name": member or type_name, "qualified_name": qualified, "file_path": path, "start_line": line, "start_column": 4, "signature": None})
    edges = []
    occurrences = []
    for index, (kind, source, target, line, column, context) in enumerate(RELATIONS, 1):
        raw_kind = "instantiates" if kind == "usesType" else kind
        metadata = None if kind == "implements" else json.dumps({"resolvedBy": "qualified-name", "confidence": 0.85})
        edges.append({"id": index, "source": codegraph_id(source), "target": codegraph_id(target), "kind": raw_kind, "line": line, "col": column - 1, "metadata": metadata, "provenance": None})
        if kind != "implements":
            occurrences.append({"path": DECLARATIONS[source][5], "line": line, "column": column, "owner": {"line": DECLARATIONS[source][6], "name": DECLARATIONS[source][3]}, "syntaxKind": "invocation_expression", "context": context})
    unresolved = [{"id": 1, "from_node_id": codegraph_id("netRecv"), "reference_kind": "calls", "reference_name": "Console.WriteLine", "line": 8, "col": 12, "file_path": DECLARATIONS["netRecv"][5], "status": "failed", "candidates": None}]
    return {
        "rawVersion": 1,
        "extractor": "CodeGraph",
        "version": "1.6.1",
        "status": "partial",
        "diagnostics": [{"kind": "limitation", "message": "synthetic fixture"}],
        "nodes": nodes,
        "edges": edges,
        "files": [{"path": path, "errors": None} for path in source_paths()],
        "unresolved_refs": unresolved,
        "syntaxContext": {"rawVersion": 1, "occurrences": occurrences, "diagnostics": []},
    }


def run_cli(*arguments):
    environment = {**os.environ, "PYTHONDONTWRITEBYTECODE": "1"}
    command = [sys.executable, "-B", str(CLI), *map(str, arguments)]
    return subprocess.run(command, capture_output=True, text=True, env=environment, timeout=300)


class Workspace:
    """A temporary folder holding one test's inputs and CLI outputs."""

    def __init__(self):
        self._directory = tempfile.TemporaryDirectory(prefix="architecture-tests-")
        self.root = pathlib.Path(self._directory.name)

    def close(self):
        self._directory.cleanup()

    def write(self, name, value):
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        if isinstance(value, (bytes, bytearray)):
            path.write_bytes(value)
        else:
            path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")
        return path

    def read(self, name):
        return json.loads((self.root / name).read_text(encoding="utf-8"))

    def normalize(self, raw, manifest_value=None, out="normalized.json", config=None):
        raw_path = self.write("raw.json", raw) if not isinstance(raw, pathlib.Path) else raw
        manifest_path = self.write("manifest.json", manifest_value or manifest())
        config_path = self.write("extractor-config.json", config or {"fixture": True})
        result = run_cli("normalize", "--raw", raw_path, "--config", config_path, "--manifest", manifest_path, "--out", self.root / out)
        return result, self.root / out

    def snapshot(self, raw, manifest_value=None):
        """Normalize and require success; callers mutate the returned snapshot copy."""
        result, path = self.normalize(raw, manifest_value)
        if result.returncode != 0:
            raise AssertionError(f"fixture normalization failed: {result.stderr}")
        return json.loads(path.read_text(encoding="utf-8"))

    def score(self, snapshot_value, scope_value=None, truth_value=None, manifest_value=None, out="score.json"):
        snapshot_path = self.write("snapshot.json", snapshot_value)
        manifest_path = self.write("manifest.json", manifest_value or manifest())
        scope_path = self.write("scope.json", scope_value or scope())
        truth_path = self.write("truth.json", truth_value or truth())
        result = run_cli("score", "--snapshot", snapshot_path, "--manifest", manifest_path, "--scope", scope_path, "--truth", truth_path, "--out", self.root / out)
        return result, self.root / out

    def validate(self, snapshot_value, manifest_value=None, git_value=None, out="validation.json"):
        snapshot_path = self.write("snapshot.json", snapshot_value)
        manifest_value = manifest_value or manifest()
        manifest_path = self.write("manifest.json", manifest_value)
        arguments = ["validate", "--snapshot", snapshot_path, "--manifest", manifest_path, "--out", self.root / out]
        if git_value is not None:
            arguments += ["--git-metadata", self.write("git.json", git_value)]
        return run_cli(*arguments), self.root / out

    def join(self, snapshot_value, reference, manifest_value=None, git_value=None, out="join.json"):
        manifest_value = manifest_value or manifest()
        snapshot_path = self.write("snapshot.json", snapshot_value)
        manifest_path = self.write("manifest.json", manifest_value)
        reference_path = self.write("reference.json", reference)
        git_path = self.write("git.json", git_value or git_metadata(manifest_value))
        result = run_cli("join", "--snapshot", snapshot_path, "--manifest", manifest_path, "--reference", reference_path, "--git-metadata", git_path, "--out", self.root / out)
        return result, self.root / out
