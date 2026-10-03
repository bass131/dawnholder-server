"""Common snapshot contract checks and read-only codeReference joining."""
import hashlib
import json
import re
from inputs import relative_path

KINDS = {"contains", "calls", "implements", "usesType"}
NODE_KINDS = {"module", "file", "type", "method", "packet"}
SHA = re.compile(r"[0-9a-f]{40}\Z")
HASH = re.compile(r"[0-9a-f]{64}\Z")
RESOLUTIONS = {"resolved", "external", "unresolved", "ambiguous", "unsupported"}


def stable_id(prefix, *parts):
    return prefix + ":" + hashlib.sha256(json.dumps(parts, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


def layer(path):
    if not path:
        return "External"
    for root, name in (("02_Server", "Server"), ("03_Client", "Client"), ("04_ClientNet", "ClientNet"), ("98_Shared", "Shared")):
        if path == root or path.startswith(root + "/"):
            return name
    return "Tools"


def validate(snapshot, manifest, manifest_hash, git_paths=None):
    if snapshot.get("schemaVersion") != 1:
        raise ValueError("Unsupported snapshot schema")
    if not SHA.fullmatch(snapshot.get("commitSha", "")) or snapshot["commitSha"] != manifest["sourceCommit"]:
        raise ValueError("Snapshot/source SHA mismatch")
    if snapshot.get("status") not in {"complete", "partial", "failed", "notRun"}:
        raise ValueError("Invalid analysis status")
    if not isinstance(snapshot.get("diagnostics"), list):
        raise ValueError("Diagnostics missing")
    if snapshot["status"] != "complete" and not snapshot["diagnostics"]:
        raise ValueError("Incomplete analysis requires a reason")
    if not snapshot.get("repository"):
        raise ValueError("Repository identity missing")
    extractor = snapshot.get("extractor", {})
    if not extractor.get("name") or not extractor.get("version") or not HASH.fullmatch(extractor.get("configHash", "")):
        raise ValueError("Extractor identity/config missing")
    scope = snapshot.get("analysisScope", {})
    if scope.get("manifestHash") != manifest_hash or not HASH.fullmatch(scope.get("manifestHash", "")):
        raise ValueError("Manifest SHA mismatch")
    if scope.get("includedRoots") != manifest["includedRoots"] or scope.get("excludedRoots") != manifest["excludedRoots"]:
        raise ValueError("Analysis roots changed")
    paths = {f["path"] for f in manifest["files"]}
    for path in paths:
        relative_path(path)
        if git_paths is not None and path not in git_paths:
            raise ValueError("Manifest path is not exact Git case")
    if scope.get("inputFileCount") != len(paths):
        raise ValueError("Input count mismatch")
    nodes = snapshot.get("nodes")
    edges = snapshot.get("edges")
    if not isinstance(nodes, list) or not isinstance(edges, list):
        raise ValueError("Node/edge arrays missing")
    if nodes != sorted(nodes, key=lambda n: n["id"]) or edges != sorted(edges, key=lambda e: e["id"]):
        raise ValueError("Snapshot arrays are not deterministically sorted")
    ids, edge_ids, triples = set(), set(), set()
    for node in nodes:
        if not re.fullmatch(r"node:[0-9a-f]{64}", node.get("id", "")) or node["id"] in ids:
            raise ValueError("Duplicate/missing node ID")
        ids.add(node["id"])
        if node.get("kind") not in NODE_KINDS or node.get("resolution") not in RESOLUTIONS:
            raise ValueError("Unknown node kind/resolution")
        if not node.get("displayName") or not isinstance(node.get("namespace"), str) or not isinstance(node.get("signature"), str) or not node.get("layer"):
            raise ValueError("Node descriptive identity missing")
        source = node.get("source")
        if source:
            path = relative_path(source["path"])
            if node["kind"] != "module" and path not in paths:
                raise ValueError(f"Node source outside manifest/exact case: {path}")
            if node["kind"] == "module" and path not in manifest["includedRoots"]:
                raise ValueError("Module path outside included roots")
            for coordinate in ("line", "column"):
                if coordinate in source and (not isinstance(source[coordinate], int) or source[coordinate] < 1):
                    raise ValueError("Invalid source position")
    for edge in edges:
        if not re.fullmatch(r"edge:[0-9a-f]{64}", edge.get("id", "")) or edge["id"] in edge_ids:
            raise ValueError("Duplicate/missing edge ID")
        edge_ids.add(edge["id"])
        if edge.get("kind") not in KINDS or edge.get("resolution") not in RESOLUTIONS:
            raise ValueError("Unknown relation kind/resolution")
        if edge.get("sourceId") not in ids or edge.get("targetId") not in ids:
            raise ValueError("Dangling endpoint")
        triple = (edge["kind"], edge["sourceId"], edge["targetId"])
        if edge["id"] != stable_id("edge", *triple):
            raise ValueError("Edge ID does not match stable relation identity")
        if triple in triples:
            raise ValueError("Duplicate relation triple")
        triples.add(triple)
        if not isinstance(edge.get("evidence"), list) or not edge["evidence"]:
            raise ValueError("Relation evidence missing")
        for evidence in edge["evidence"]:
            if relative_path(evidence["path"]) not in paths:
                raise ValueError("Relation evidence outside manifest")
            if "line" in evidence and (not isinstance(evidence["line"], int) or evidence["line"] < 1):
                raise ValueError("Invalid evidence line")
            if evidence.get("context") not in {"direct", "declaration", "deferredLambda", "unknown"}:
                raise ValueError("Unknown static relation context")
    if snapshot["status"] in {"failed", "notRun"} and edges:
        raise ValueError("Failed/not-run outputs must not expose confirmed relations")
    if snapshot["status"] == "complete" and any(e["resolution"] != "resolved" for e in edges):
        raise ValueError("Unresolved relations cannot be complete")
    return {"valid": True, "status": snapshot["status"], "nodes": len(nodes), "edges": len(edges)}


def join_code_reference(snapshot, code_reference, git_paths):
    if not SHA.fullmatch(code_reference.get("commitSha", "")):
        raise ValueError("codeReference requires full SHA")
    if code_reference["commitSha"] != snapshot["commitSha"]:
        return {"status": "shaMismatch", "mappings": [], "matches": []}
    mappings = code_reference.get("mappings")
    if not isinstance(mappings, list):
        raise ValueError("Mappings must be an array")
    matches = []
    for index, mapping in enumerate(mappings):
        path = relative_path(mapping["path"])
        if mapping.get("kind") == "file":
            if path not in git_paths:
                raise ValueError("Mapping file has wrong Git case or is absent")
        elif mapping.get("kind") == "directory":
            if not any(p.startswith(path + "/") for p in git_paths):
                raise ValueError("Mapping directory has wrong Git case or is absent")
        else:
            raise ValueError("Unknown mapping kind")
        for node in snapshot["nodes"]:
            candidate = (node.get("source") or {}).get("path")
            if candidate == path or (mapping["kind"] == "directory" and candidate and candidate.startswith(path + "/")):
                matches.append({"mappingIndex": index, "nodeId": node["id"]})
    # Multiple mappings are retained; the extractor assigns no card ownership.
    return {"status": "unmapped" if not mappings else "matched", "analysisStatus": snapshot["status"], "mappings": mappings, "matches": matches}
