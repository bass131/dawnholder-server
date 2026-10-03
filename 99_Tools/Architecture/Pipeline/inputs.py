"""Verify immutable manifest bytes and copy only declared analysis inputs."""
import hashlib
import json
import pathlib
import shutil


def read_json(path):
    return json.loads(pathlib.Path(path).read_text(encoding="utf-8-sig"))


def write_json(path, value):
    path = pathlib.Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def digest(path):
    return hashlib.sha256(pathlib.Path(path).read_bytes()).hexdigest()


def relative_path(value):
    if (not isinstance(value, str) or not value or value.startswith("/") or
            any(c in value for c in "\\:*?[]") or
            any(p in ("", ".", "..") for p in value.split("/"))):
        raise ValueError(f"Invalid repository path: {value!r}")
    return value


def checked_path(root, value):
    value = relative_path(value)
    root = pathlib.Path(root).resolve()
    path = root / value
    if not path.resolve().is_relative_to(root):
        raise ValueError(f"Path escapes root: {value}")
    # Reject links even when their destination happens to be inside the root.
    if any(p.is_symlink() for p in [path, *path.parents] if p != root.parent):
        raise ValueError(f"Linked input/destination: {path}")
    return path


def windows_path(value):
    if len(value) > 2 and value[1:3] == ":/":
        return pathlib.Path("/mnt") / value[0].lower() / value[3:]
    return pathlib.Path(value)


def verify(manifest, root):
    records = []
    seen = set()
    for item in manifest["files"]:
        if item["path"] in seen:
            raise ValueError("Duplicate manifest path")
        seen.add(item["path"])
        path = checked_path(root, item["path"])
        actual = digest(path)
        if actual != item["sha256"] or path.stat().st_size != item["bytes"]:
            raise ValueError(f"Manifest mismatch: {item['path']}")
        records.append({"path": item["path"], "sha256": actual, "bytes": path.stat().st_size})
    for item in manifest["externalReferences"]:
        path = checked_path(root, f".architecture-references/{item['rootId']}/{item['path']}")
        if digest(path) != item["sha256"] or path.stat().st_size != item["bytes"]:
            raise ValueError(f"External reference mismatch: {path}")
    return records


def prepare(source, runtime, manifest_path, evidence):
    source, runtime = pathlib.Path(source), pathlib.Path(runtime)
    manifest = read_json(manifest_path)
    # Windows worktree .git pointers are not Linux paths. Capture Git metadata
    # with the Windows entry point; WSL never opens the original .git directory.
    metadata = read_json(pathlib.Path(evidence) / "source-git.json")
    if metadata["manifestHash"] != digest(manifest_path) or metadata["sourceCommit"] != manifest["sourceCommit"]:
        raise ValueError("Git metadata belongs to another manifest")
    tree = list(metadata["tree"])
    tree_set = set(tree)
    records = []
    roots = {r["id"]: windows_path(r["observedRoot"]) for r in manifest["externalReferenceRoots"]}
    for item in manifest["files"]:
        if item["path"] not in tree_set:
            raise ValueError(f"Input path is not exact Git case at source SHA: {item['path']}")
        path = checked_path(source, item["path"])
        if digest(path) != item["sha256"] or path.stat().st_size != item["bytes"]:
            raise ValueError(f"Source input changed: {path}")
        blob = metadata["tree"][item["path"]]
        if blob != item["gitBlobId"]:
            raise ValueError(f"Git blob identity mismatch: {path}")
        for name in ("roslyn-input", "codegraph-input"):
            dest = checked_path(runtime / name, item["path"])
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, dest)
        records.append({"path": item["path"], "originalSha256": digest(path), "gitBlobId": blob})
    references = []
    for item in manifest["externalReferences"]:
        path = checked_path(roots[item["rootId"]], item["path"])
        if digest(path) != item["sha256"] or path.stat().st_size != item["bytes"]:
            raise ValueError(f"Unity reference changed: {path}")
        for name in ("roslyn-input", "codegraph-input"):
            dest = checked_path(runtime / name, f".architecture-references/{item['rootId']}/{item['path']}")
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, dest)
        references.append({**item, "originalPath": str(path), "originalSha256": digest(path),
                           "roslyn-inputSha256": digest(runtime / "roslyn-input/.architecture-references" / item["rootId"] / item["path"]),
                           "codegraph-inputSha256": digest(runtime / "codegraph-input/.architecture-references" / item["rootId"] / item["path"])})
    for name in ("roslyn-input", "codegraph-input"):
        copied_records = verify(manifest, runtime / name)
        for record, copied in zip(records, copied_records):
            record[name + "Sha256"] = copied["sha256"]
    write_json(pathlib.Path(evidence) / "input-copy.json", {
        "sourceCommit": manifest["sourceCommit"],
        "implementationHead": metadata["implementationHead"],
        "manifestHash": digest(manifest_path), "files": records, "externalReferences": references,
        "verifiedCopies": ["roslyn-input", "codegraph-input"], "gitTreePaths": tree,
        "sourceRoot": str(source), "runtimeRoot": str(runtime),
    })


if __name__ == "__main__":
    import sys
    prepare(*sys.argv[1:])
