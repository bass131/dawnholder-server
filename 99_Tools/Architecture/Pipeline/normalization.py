"""Convert independent raw extractor data without reading evaluation answers."""
import json
import pathlib
# Preserve the existing public imports while CodeGraph owns its raw schema.
from codegraph_adapter import dump_codegraph, normalize_codegraph
from snapshot import stable_id, layer, KINDS


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
