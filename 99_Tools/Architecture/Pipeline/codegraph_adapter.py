"""CodeGraph bundle, cache and raw schema boundaries for the comparison pipeline.

The runner owns comparison order and measurements; SnapshotBuilder owns the common
snapshot. This module consumes that builder without importing the normalization module.
"""
import json
import pathlib
import shutil
import sqlite3
import os

from execution import run_process
from inputs import digest, read_json, write_json
from execution_status import CODEGRAPH_REPAIR, ExecutionError, assert_unlinked_tree
from snapshot import KINDS

INSPECTED_FILES = (
    "bin/codegraph.js", "directory.js", "index.js", "installer/index.js",
    "telemetry/index.js", "extraction/languages/csharp.js",
    "extraction/tree-sitter.js", "resolution/callback-synthesizer.js", "db/schema.sql",
)


def require_bundle(bundle, syntax_context):
    """Read selected bundle prerequisites only; never install or remove package/cache."""
    bundle, syntax_context = map(pathlib.Path, (bundle, syntax_context))
    assert_unlinked_tree(bundle)
    required = [
        ("codegraph_bundle_missing", bundle / "bin/codegraph"),
        ("codegraph_node_missing", bundle / "node"),
        ("codegraph_grammar_missing", bundle / "lib/dist/extraction/wasm/tree-sitter-c_sharp.wasm"),
        ("codegraph_context_missing", syntax_context),
        *(("codegraph_input_missing", bundle / "lib/dist" / name) for name in INSPECTED_FILES),
    ]
    for reason, path in required:
        if path.is_symlink() or not path.is_file():
            raise ExecutionError(f"Missing/unusable CodeGraph prerequisite: {path}", reason,
                                 "unavailable", CODEGRAPH_REPAIR)
    for path in (bundle / "bin/codegraph", bundle / "node"):
        if not os.access(path, os.X_OK):
            raise ExecutionError(f"CodeGraph executable cannot start: {path}", "codegraph_not_executable",
                                 "unavailable", CODEGRAPH_REPAIR)


def inspect_bundle(runtime, evidence):
    """Copy selected bundle source/help evidence outside measured analysis time."""
    # Source/help inspection stays before the first analysis, outside its timing.
    inspection = evidence / "install/source-inspection"
    inspection.mkdir(parents=True, exist_ok=True)
    for name in INSPECTED_FILES:
        path = runtime / "bundle/lib/dist" / name
        target = inspection / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, target)


def preflight_commands(runtime):
    launcher = runtime / "bundle/bin/codegraph"
    return (
        ("codegraph-version", [launcher, "--version"]),
        ("codegraph-init-help", [launcher, "init", "--help"]),
        ("codegraph-index-help", [launcher, "index", "--help"]),
    )


def check_version(batch):
    if (batch / "codegraph-version/stdout.txt").read_text().strip() != "1.6.1":
        raise ValueError("CodeGraph version differs from approved bundle")


def bundle_config(runtime):
    return {
        "bundleFiles": [
            {"path": str(path.relative_to(runtime / "bundle")), "sha256": digest(path)}
            for path in sorted((runtime / "bundle").rglob("*"))
            if path.is_file() and (
                path.suffix == ".wasm" and "c_sharp" in path.name
                or path == runtime / "bundle/node"
                or path == runtime / "bundle/lib/dist/bin/codegraph.js"
            )
        ],
        "preprocessorPolicy": "CodeGraph blanks conditional directives and indexes both branches; no compiler defines",
    }


def bundle_hashes(runtime):
    return {
        "codegraphNodeSha256": digest(runtime / "bundle/node"),
        "codegraphLauncherSha256": digest(runtime / "bundle/bin/codegraph"),
    }


def archive_cache(runtime, stamp):
    # Only the runtime's analysis cache moves; the original installation is untouched.
    analysis_dir = runtime / "codegraph-input/.codegraph"
    if analysis_dir.exists():
        if analysis_dir.is_symlink():
            raise ValueError("Linked CodeGraph cache")
        archived = runtime / "cache-archive" / stamp
        archived.parent.mkdir(parents=True, exist_ok=True)
        shutil.move(str(analysis_dir), str(archived))


def cache_state(root):
    folder = root / ".codegraph"
    return {
        "persistentAnalysisCache": folder.exists(),
        "files": [
            {"path": str(path.relative_to(root)), "bytes": path.stat().st_size,
             "sha256": digest(path)}
            for path in sorted(folder.rglob("*")) if path.is_file()
        ] if folder.exists() else [],
    }


def analysis_command(runtime, root, index):
    launcher = runtime / "bundle/bin/codegraph"
    if index == 0:
        return [launcher, "init", str(root), "--yes"]
    return [launcher, "index", str(root), "--quiet"]


def read_analysis(runtime, root, manifest_path, folder, execute_command=None):
    """Dump/backup the DB, then run syntax context and return (raw, command record).

    The runner decides success before reading syntax-context.json. A failed command
    still leaves its logs and the preceding DB dump/backup for diagnosis.
    """
    # Keep the DB dump/backup before syntax context, including on context failure.
    raw = dump_codegraph(root / ".codegraph/codegraph.db", folder / "raw.json")
    command = [
        runtime / "bundle/node", "--liftoff-only", "--disable-warning=ExperimentalWarning",
        runtime / "tool/CodeGraph/syntax-context.cjs", runtime / "bundle",
        root, manifest_path, folder / "syntax-context.json",
    ]
    command_executor = execute_command if execute_command is not None else run_process
    record = command_executor(command, root, folder / "syntax-context")
    return raw, record


def dump_codegraph(database, output):
    with sqlite3.connect(f"file:{pathlib.Path(database).as_posix()}?mode=ro", uri=True) as connection:
        connection.row_factory = sqlite3.Row
        tables = {row[0] for row in connection.execute("SELECT name FROM sqlite_master WHERE type='table'")}
        required = {"nodes", "edges", "files", "unresolved_refs"}
        if not required.issubset(tables):
            raise ValueError("CodeGraph database lacks core schema")
        raw = {
            name: [dict(row) for row in connection.execute(f'SELECT * FROM "{name}" ORDER BY rowid')]
            for name in sorted(required | ({"schema_versions"} & tables))
        }
        raw.update(rawVersion=1, extractor="CodeGraph", version="1.6.1", status="partial")
        raw["diagnostics"] = [{
            "kind": "limitation",
            "message": "C# grammar/resolver index; no compiler configuration or supplied DLL semantic binding. Only explicit method-to-type instantiates is normalized as usesType; references/imports are not.",
        }]
        for file in raw["files"]:
            if file.get("errors") and file["errors"] != "[]":
                raw["diagnostics"].append({"kind": "parser", "path": file["path"], "message": file["errors"]})
        write_json(output, raw)
        with sqlite3.connect(str(pathlib.Path(output).with_suffix(".db"))) as destination:
            connection.backup(destination)
    return raw


def normalize_codegraph(raw, builder):
    kinds = {
        "class": "type", "struct": "type", "interface": "type", "enum": "type",
        "type_alias": "type", "method": "method", "function": "method", "file": "file",
    }
    for symbol in raw["nodes"]:
        if symbol["kind"] not in kinds:
            continue
        path = symbol["file_path"]
        # Raw paths are preserved; invalid/case-mismatched paths fail validation.
        qualified = symbol["qualified_name"].replace("::", ".")
        namespace_parts = symbol["qualified_name"].split("::")
        namespace = "::".join(
            namespace_parts[:-2] if kinds[symbol["kind"]] == "method" else namespace_parts[:-1]
        ).replace("::", ".")
        identity = path if kinds[symbol["kind"]] == "file" else [
            path, qualified, symbol.get("signature"), symbol["start_line"], symbol["start_column"],
        ]
        source = {"path": path, "line": symbol["start_line"], "column": symbol["start_column"] + 1}
        builder.mapping[symbol["id"]] = builder.node(
            identity, kinds[symbol["kind"]], qualified, namespace,
            symbol.get("signature") or qualified, source, "resolved",
            documentationId=None, qualifiedName=qualified, rawKind=symbol["kind"],
            signatureStatus="supplied" if symbol.get("signature") else "unavailable",
        )
    unsupported = {}
    for relation in raw["edges"]:
        kind = relation["kind"]
        metadata = json.loads(relation["metadata"]) if relation.get("metadata") else None
        source = builder.mapping.get(relation["source"])
        target = builder.mapping.get(relation["target"])
        if kind == "calls" and metadata and metadata.get("synthesizedBy"):
            label = "synthesizedCalls:" + metadata["synthesizedBy"]
            unsupported[label] = unsupported.get(label, 0) + 1
            continue
        if (kind == "instantiates" and source and target
                and builder.nodes[source]["kind"] == "method"
                and builder.nodes[target]["kind"] == "type"):
            kind = "usesType"
        if kind not in KINDS or source is None or target is None:
            unsupported[kind] = unsupported.get(kind, 0) + 1
            continue
        source_node = builder.nodes[source]
        evidence = {"path": source_node["source"]["path"], "context": "unknown", "contextOrigin": "not supplied"}
        if relation.get("line") and relation["line"] > 0:
            evidence["line"] = relation["line"]
        if relation.get("col") is not None and relation["col"] >= 0:
            evidence["column"] = relation["col"] + 1
        if kind in {"contains", "implements"}:
            evidence["context"] = "declaration"
            evidence["contextOrigin"] = "CodeGraph raw declaration relation"
        elif kind in {"calls", "usesType"}:
            contexts = [
                entry for entry in raw.get("syntaxContext", {}).get("occurrences", [])
                if entry["path"] == evidence["path"] and entry["line"] == evidence.get("line")
                and entry["owner"]["line"] == source_node["source"]["line"]
                and ("column" not in evidence or entry["column"] == evidence["column"])
            ]
            if contexts and len({entry["context"] for entry in contexts}) == 1:
                evidence["context"] = contexts[0]["context"]
                evidence["contextOrigin"] = "CodeGraph bundled C# grammar syntax-context.cjs"
        builder.edge(
            source, target, kind, evidence, "resolved",
            {"binding": "CodeGraph resolver", "rawKind": relation["kind"],
             "rawProvenance": relation.get("provenance"), "metadata": metadata},
        )
    for relation in raw["unresolved_refs"]:
        source = builder.mapping.get(relation["from_node_id"])
        kind = relation["reference_kind"]
        if source is None or kind not in KINDS:
            unsupported["unresolved:" + kind] = unsupported.get("unresolved:" + kind, 0) + 1
            continue
        evidence = {"path": relation.get("file_path") or builder.nodes[source]["source"]["path"],
                    "line": relation["line"], "context": "unknown"}
        target = builder.node(
            [source, kind, evidence, relation["reference_name"]],
            "method" if kind == "calls" else "type", relation["reference_name"], "",
            relation["reference_name"], None, "unresolved",
        )
        builder.edge(source, target, kind, evidence, "unresolved", {
            "rawStatus": relation.get("status"), "rawCandidates": relation.get("candidates"),
        })
    builder.diagnostics.append({
        "kind": "unsupportedRawRelations", "counts": unsupported,
        "message": "Other relation kinds are retained in raw, not reinterpreted as usesType/implements.",
    })
    builder.snapshot["supports"] = {"calls": True, "contains": True, "implements": True, "usesType": True}
    builder.snapshot["supportDetails"] = {
        "usesType": "Partial: explicit method-to-type instantiation only",
        "calls": "Resolver targets; synthesized runtime dispatch excluded",
        "lambdaContext": "Bundled grammar syntax context matched by exact source declaration and occurrence coordinates; unknown retained when missing",
    }
    indexed = {file["path"] for file in raw["files"] if file["path"].endswith(".cs")}
    expected = {file["path"] for file in builder.manifest["files"] if file["path"].endswith(".cs")}
    builder.snapshot["analysisScope"]["indexedSourceFileCount"] = len(indexed)
    if indexed != expected:
        builder.diagnostics.append({
            "kind": "coverage", "missing": sorted(expected - indexed), "extra": sorted(indexed - expected),
        })
