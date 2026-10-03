"""Convert independent raw extractor data without reading evaluation answers."""
import json
import pathlib
import sqlite3
from inputs import digest, read_json
from snapshot import stable_id, layer, KINDS


def dump_codegraph(database, output):
    from inputs import write_json
    with sqlite3.connect(f"file:{pathlib.Path(database).as_posix()}?mode=ro", uri=True) as connection:
        connection.row_factory = sqlite3.Row
        tables = {row[0] for row in connection.execute("SELECT name FROM sqlite_master WHERE type='table'")}
        required = {"nodes", "edges", "files", "unresolved_refs"}
        if not required.issubset(tables):
            raise ValueError("CodeGraph database lacks core schema")
        raw = {name: [dict(row) for row in connection.execute(f'SELECT * FROM "{name}" ORDER BY rowid')]
               for name in sorted(required | ({"schema_versions"} & tables))}
        raw.update(rawVersion=1, extractor="CodeGraph", version="1.6.1", status="partial")
        raw["diagnostics"] = [{"kind": "limitation", "message": "C# grammar/resolver index; no compiler configuration or supplied DLL semantic binding. Only explicit method-to-type instantiates is normalized as usesType; references/imports are not."}]
        for file in raw["files"]:
            if file.get("errors") and file["errors"] != "[]":
                raw["diagnostics"].append({"kind": "parser", "path": file["path"], "message": file["errors"]})
        write_json(output, raw)
        with sqlite3.connect(str(pathlib.Path(output).with_suffix(".db"))) as destination:
            connection.backup(destination)
    return raw


class SnapshotBuilder:
    def __init__(self, raw, manifest, manifest_hash, config_hash):
        self.raw = raw
        self.manifest = manifest
        self.nodes = {}
        self.edges = {}
        self.mapping = {}
        self.diagnostics = list(raw.get("diagnostics", []))
        self.snapshot = {
            "schemaVersion": 1, "repository": "DawnHolder_Project", "commitSha": manifest["sourceCommit"],
            "extractor": {"name": raw["extractor"], "version": raw["version"], "configHash": config_hash, "adapterVersion": "1"},
            "analysisScope": {"manifestHash": manifest_hash, "includedRoots": manifest["includedRoots"], "excludedRoots": manifest["excludedRoots"], "inputFileCount": len(manifest["files"]), "sourceFileCount": sum(f["path"].endswith(".cs") for f in manifest["files"])},
            "status": raw.get("status", "failed"), "diagnostics": self.diagnostics,
        }

    def node(self, identity, kind, name, namespace, signature, source, resolution, **extra):
        node_id = stable_id("node", kind, identity)
        self.nodes[node_id] = {"id": node_id, "kind": kind, "displayName": name, "namespace": namespace or "", "signature": signature or "", "layer": layer((source or {}).get("path")), "source": source, "resolution": resolution, **extra}
        return node_id

    def edge(self, source, target, kind, evidence, resolution, provenance=None):
        edge_id = stable_id("edge", kind, source, target)
        if edge_id not in self.edges:
            self.edges[edge_id] = {"id": edge_id, "kind": kind, "sourceId": source, "targetId": target, "resolution": resolution, "evidence": [], "provenance": []}
        edge = self.edges[edge_id]
        if evidence not in edge["evidence"]:
            edge["evidence"].append(evidence)
        if provenance is not None and provenance not in edge["provenance"]:
            edge["provenance"].append(provenance)

    def containment(self):
        # Directory modules describe manifest roots, never Management cards.
        modules = {root: self.node(root, "module", root, "", root, {"path": root}, "resolved") for root in self.manifest["includedRoots"]}
        file_nodes = {}
        for file in self.manifest["files"]:
            path = file["path"]
            if not path.endswith(".cs"):
                continue
            file_id = self.node(path, "file", pathlib.PurePosixPath(path).name, "", path, {"path": path}, "resolved")
            file_nodes[path] = file_id
            for root, module_id in modules.items():
                if path.startswith(root + "/"):
                    self.edge(module_id, file_id, "contains", {"path": path, "context": "declaration"}, "resolved", {"adapter": "manifest containment"})
        for node in list(self.nodes.values()):
            path = (node.get("source") or {}).get("path")
            if node["kind"] in {"type", "method", "packet"} and path in file_nodes and node["resolution"] == "resolved":
                self.edge(file_nodes[path], node["id"], "contains", {**node["source"], "context": "declaration"}, "resolved", {"adapter": "source declaration containment"})

    def finish(self):
        self.containment()
        self.diagnostics.sort(key=lambda x: json.dumps(x, sort_keys=True))
        for edge in self.edges.values():
            edge["evidence"].sort(key=lambda x: json.dumps(x, sort_keys=True))
            edge["provenance"].sort(key=lambda x: json.dumps(x, sort_keys=True))
        self.snapshot["nodes"] = sorted(self.nodes.values(), key=lambda n: n["id"])
        self.snapshot["edges"] = sorted(self.edges.values(), key=lambda e: e["id"])
        return self.snapshot


def normalize_roslyn(raw, builder):
    for symbol in raw["symbols"]:
        builder.mapping[symbol["key"]] = builder.node(symbol["key"], symbol["kind"], symbol["signature"] or symbol["name"], symbol["namespace"], symbol["signature"], symbol["source"], symbol["resolution"], documentationId=symbol.get("documentationId"))
    for relation in raw["relations"]:
        source = builder.mapping[relation["sourceKey"]]
        evidence = relation.get("evidence")
        if evidence is None:
            raise ValueError("Roslyn relation lacks source evidence")
        target = builder.mapping.get(relation.get("targetKey"))
        if target is None:
            identity = [relation["sourceKey"], relation["kind"], evidence, relation.get("targetText"), relation.get("candidates")]
            target = builder.node(identity, "method" if relation["kind"] == "calls" else "type", relation.get("targetText") or "Unresolved symbol", "", relation.get("targetText") or "", None, relation["resolution"], candidates=relation.get("candidates", []))
        builder.edge(source, target, relation["kind"], {**evidence, "context": relation["context"], "contextOrigin": "Roslyn syntax ancestry"}, relation["resolution"], {"binding": "Roslyn compiler symbol"})
    builder.snapshot["supports"] = raw["supports"]
    expected = {f["path"] for f in builder.manifest["files"] if f["path"].endswith(".cs")}
    if set(raw.get("coverage", [])) != expected:
        builder.snapshot["status"] = "partial"
        builder.diagnostics.append({"kind": "coverage", "missing": sorted(expected - set(raw.get("coverage", [])))})


def normalize_codegraph(raw, builder):
    kinds = {"class": "type", "struct": "type", "interface": "type", "enum": "type", "type_alias": "type", "method": "method", "function": "method", "file": "file"}
    for symbol in raw["nodes"]:
        if symbol["kind"] not in kinds:
            continue
        path = symbol["file_path"]
        # Raw paths are preserved; invalid/case-mismatched paths fail validation.
        qualified = symbol["qualified_name"].replace("::", ".")
        namespace_parts = symbol["qualified_name"].split("::")
        namespace = "::".join(namespace_parts[:-2] if kinds[symbol["kind"]] == "method" else namespace_parts[:-1]).replace("::", ".")
        identity = path if kinds[symbol["kind"]] == "file" else [path, qualified, symbol.get("signature"), symbol["start_line"], symbol["start_column"]]
        source = {"path": path, "line": symbol["start_line"], "column": symbol["start_column"] + 1}
        builder.mapping[symbol["id"]] = builder.node(identity, kinds[symbol["kind"]], qualified, namespace, symbol.get("signature") or qualified, source, "resolved", documentationId=None, qualifiedName=qualified, rawKind=symbol["kind"], signatureStatus="supplied" if symbol.get("signature") else "unavailable")
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
        if kind == "instantiates" and source and target and builder.nodes[source]["kind"] == "method" and builder.nodes[target]["kind"] == "type":
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
            contexts = [entry for entry in raw.get("syntaxContext", {}).get("occurrences", []) if entry["path"] == evidence["path"] and entry["line"] == evidence.get("line") and entry["owner"]["line"] == source_node["source"]["line"] and ("column" not in evidence or entry["column"] == evidence["column"])]
            if contexts and len({entry["context"] for entry in contexts}) == 1:
                evidence["context"] = contexts[0]["context"]
                evidence["contextOrigin"] = "CodeGraph bundled C# grammar syntax-context.cjs"
        builder.edge(source, target, kind, evidence, "resolved", {"binding": "CodeGraph resolver", "rawKind": relation["kind"], "rawProvenance": relation.get("provenance"), "metadata": metadata})
    for relation in raw["unresolved_refs"]:
        source = builder.mapping.get(relation["from_node_id"])
        kind = relation["reference_kind"]
        if source is None or kind not in KINDS:
            unsupported["unresolved:" + kind] = unsupported.get("unresolved:" + kind, 0) + 1
            continue
        evidence = {"path": relation.get("file_path") or builder.nodes[source]["source"]["path"], "line": relation["line"], "context": "unknown"}
        target = builder.node([source, kind, evidence, relation["reference_name"]], "method" if kind == "calls" else "type", relation["reference_name"], "", relation["reference_name"], None, "unresolved")
        builder.edge(source, target, kind, evidence, "unresolved", {"rawStatus": relation.get("status"), "rawCandidates": relation.get("candidates")})
    builder.diagnostics.append({"kind": "unsupportedRawRelations", "counts": unsupported, "message": "Other relation kinds are retained in raw, not reinterpreted as usesType/implements."})
    builder.snapshot["supports"] = {"calls": True, "contains": True, "implements": True, "usesType": True}
    builder.snapshot["supportDetails"] = {"usesType": "Partial: explicit method-to-type instantiation only", "calls": "Resolver targets; synthesized runtime dispatch excluded", "lambdaContext": "Bundled grammar syntax context matched by exact source declaration and occurrence coordinates; unknown retained when missing"}
    indexed = {f["path"] for f in raw["files"] if f["path"].endswith(".cs")}
    expected = {f["path"] for f in builder.manifest["files"] if f["path"].endswith(".cs")}
    builder.snapshot["analysisScope"]["indexedSourceFileCount"] = len(indexed)
    if indexed != expected:
        builder.diagnostics.append({"kind": "coverage", "missing": sorted(expected - indexed), "extra": sorted(indexed - expected)})


def normalize(raw, manifest, manifest_hash, config_hash):
    if raw.get("rawVersion") != 1:
        raise ValueError("Unsupported raw version")
    builder = SnapshotBuilder(raw, manifest, manifest_hash, config_hash)
    if raw.get("status") in {"failed", "notRun"}:
        builder.snapshot["nodes"] = []
        builder.snapshot["edges"] = []
        builder.snapshot["supports"] = {kind: False for kind in KINDS}
        return builder.snapshot
    if raw["extractor"] == "Roslyn":
        normalize_roslyn(raw, builder)
    elif raw["extractor"] == "CodeGraph":
        normalize_codegraph(raw, builder)
    else:
        raise ValueError("Unknown extractor")
    return builder.finish()
