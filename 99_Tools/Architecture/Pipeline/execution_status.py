"""Selection, owned evidence paths and execution results; no extractor installation.

Snapshot analysis status remains separate. The runner owns command success, while
the shell owns runtime/evidence locks before calling the file-writing helpers here.
"""
import argparse
import hashlib
import json
import os
import pathlib
import shutil
import subprocess
import sys
from contextlib import contextmanager

from inputs import checked_path, read_json, relative_path, write_json

SELECTIONS = {
    "roslyn": ("Roslyn",),
    "codegraph": ("CodeGraph",),
    "compare": ("CodeGraph", "Roslyn"),
}
OWNER_FILE = ".dawnholder-execution.json"
CODEGRAPH_REPAIR = (
    "Use the repository's fixed CodeGraph/package.json and package-lock.json with "
    "pwsh -File 99_Tools/Architecture/install-codegraph.ps1; the approved 1.6.1 "
    "Linux x64 bundle must include bundled Node and the C# grammar. Preserve the "
    "existing installation/cache; do not change global npm/PATH."
)


class ExecutionError(RuntimeError):
    """A failed attempt with machine status, repair and optional command evidence."""

    def __init__(self, message, reason_code="execution_failed", status="failed",
                 repair=None, command_record=None):
        super().__init__(message)
        self.reason_code = reason_code
        self.status = status
        self.repair = repair
        self.command_record = command_record


def selected_extractors(selection):
    """Return the ordered selected names; reject unknown values without fallback."""
    if selection not in SELECTIONS:
        raise ExecutionError(
            f"Unknown extractor selection: {selection!r}", "invalid_selection",
            repair="Use -Extractor Roslyn|CodeGraph|Compare or --extractor roslyn|codegraph|compare.",
        )
    return list(SELECTIONS[selection])


def retry_command(action, selection, evidence):
    quoted_evidence = "'" + str(evidence).replace("'", "''") + "'"
    return (
        f"pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action {action} "
        f"-Extractor {selection.capitalize() if selection != 'codegraph' else 'CodeGraph'} "
        f"-EvidencePath {quoted_evidence}"
    )


def assert_unlinked_tree(root):
    """Inspect an owned tree before mutations; never follow a linked destination."""
    root = pathlib.Path(root)
    if any(path.is_symlink() for path in (root, *root.parents)):
        raise ValueError(f"Linked execution root/ancestor: {root}")
    checked_path(root.parent, root.name)
    if root.exists():
        if not root.is_dir():
            raise ValueError(f"Expected directory: {root}")
        for path in root.rglob("*"):
            if path.is_symlink():
                raise ValueError(f"Linked execution file/directory: {path}")


def evidence_path(source, selection, value=None):
    """Read-only path/Git/owner validation; never create or adopt historical evidence."""
    selected_extractors(selection)
    source = pathlib.Path(source).resolve()
    value = relative_path(value if value is not None else f".backups/architecture/{selection}")
    if not value.startswith(".backups/"):
        raise ValueError("EvidencePath must be a repository-relative path below Git-excluded .backups/.")
    evidence = checked_path(source, value)
    settings = read_json(source / "99_Tools/Architecture/comparison-settings.json")
    historical = checked_path(source, settings["evidencePath"])
    freeze = checked_path(source, settings["freezeRecordPath"])
    current_owned_root = evidence == historical and (historical / OWNER_FILE).is_file()
    if ((evidence.is_relative_to(historical) or historical.is_relative_to(evidence)) and not current_owned_root
            or freeze.is_relative_to(evidence)):
        raise ValueError(f"EvidencePath overlaps preserved comparison/freeze evidence: {evidence}")
    # A Windows worktree pointer must be queried by Windows Git, never Linux Git.
    pointer = source / ".git"
    windows_git = pointer.is_file() and ":/" in pointer.read_text(encoding="utf-8").replace("\\", "/")
    git = shutil.which("git.exe" if windows_git else "git")
    if not git:
        raise ExecutionError("Git exclusion query cannot start", "git_unavailable", "unavailable")
    git_source = str(source)
    # Only the metadata reader owns stdin. WSL interop can drain it even for Git queries.
    if windows_git:
        git_source = subprocess.check_output(
            ["wslpath", "-w", str(source)], stdin=subprocess.DEVNULL, text=True,
        ).strip()
    query = subprocess.run(
        [git, "-C", git_source, "check-ignore", "--quiet", "--", value + "/"],
        stdin=subprocess.DEVNULL, capture_output=True, text=True,
    )
    if query.returncode != 0:
        raise ValueError(f"EvidencePath is not confirmed Git-excluded: {value}; {query.stderr.strip()}")
    assert_unlinked_tree(evidence)
    marker = evidence / OWNER_FILE
    if marker.exists():
        owner = read_json(marker)
        if owner.get("sourceRoot") != str(source) or owner.get("selection") != selection:
            raise ValueError(f"Evidence belongs to another source/selection: {marker}")
    elif evidence.exists() and any(evidence.iterdir()):
        raise ValueError(f"Nonempty evidence has no current execution owner: {evidence}; choose a new root.")
    return evidence


def runtime_path(source, selection, evidence):
    """Derive a mode/evidence-specific sibling of the preserved comparison runtime."""
    key = hashlib.sha256(str(source).encode()).hexdigest()[:20]
    evidence_key = hashlib.sha256(str(evidence).encode()).hexdigest()[:12]
    return pathlib.Path.home() / ".cache/dawnholder/architecture" / f"{key}-{selection}-{evidence_key}"


def validate_runtime(source, runtime, evidence, selection):
    """Read-only runtime identity/link validation, also used for path lookup."""
    source, runtime, evidence = map(pathlib.Path, (source, runtime, evidence))
    assert_unlinked_tree(runtime)
    marker = runtime / OWNER_FILE
    expected = {"sourceRoot": str(source.resolve()), "selection": selection, "runtimeRoot": str(runtime)}
    if marker.exists():
        if read_json(marker) != {**expected, "evidenceRoot": str(evidence)}:
            raise ValueError(f"Foreign runtime workspace: {marker}")
        source_marker = runtime / ".dawnholder-source"
        if not source_marker.is_file() or source_marker.read_text().strip() != str(source.resolve()):
            raise ValueError(f"Foreign runtime source marker: {source_marker}")
    elif runtime.exists() and any(runtime.iterdir()):
        raise ValueError(f"Foreign runtime workspace (owner marker missing): {runtime}")
    evidence_marker = evidence / OWNER_FILE
    if evidence_marker.exists() and read_json(evidence_marker).get("runtimeRoot") != str(runtime):
        raise ValueError(f"Evidence is bound to another runtime: {evidence_marker}")
    return expected


def claim_execution(source, runtime, evidence, selection):
    """Write owner markers only after the caller holds both execution locks."""
    expected = validate_runtime(source, runtime, evidence, selection)
    runtime.mkdir(parents=True, exist_ok=True)
    evidence.mkdir(parents=True, exist_ok=True)
    write_json(runtime / OWNER_FILE, {**expected, "evidenceRoot": str(evidence)})
    write_json(evidence / OWNER_FILE, expected)
    (runtime / ".dawnholder-source").write_text(str(pathlib.Path(source).resolve()) + "\n")


@contextmanager
def execution_locks(runtime, evidence):
    """Hold runtime/evidence locks, reusing only the shell's verified inherited FDs.

    Direct Python calls acquire the same locks. Child commands close these FDs;
    neither lock contention nor cleanup terminates another owner's processes.
    """
    import fcntl

    lock_root = pathlib.Path.home() / ".cache/dawnholder/locks"
    assert_unlinked_tree(lock_root)
    lock_root.mkdir(parents=True, exist_ok=True)
    evidence_key = hashlib.sha256(str(evidence).encode()).hexdigest()[:20]
    paths = (lock_root / f"architecture-{runtime.name}.lock",
             lock_root / f"architecture-evidence-{evidence_key}.lock")
    opened = []
    try:
        for descriptor, path in zip((8, 9), paths):
            checked_path(lock_root, path.name)
            inherited = pathlib.Path(f"/proc/self/fd/{descriptor}")
            if inherited.exists() and inherited.resolve() == path:
                fcntl.flock(descriptor, fcntl.LOCK_EX | fcntl.LOCK_NB)
            else:
                stream = path.open("a")
                opened.append(stream)
                fcntl.flock(stream, fcntl.LOCK_EX | fcntl.LOCK_NB)
        yield
    except BlockingIOError as error:
        raise ExecutionError(
            f"Execution lock is held by another executor: {path}", "execution_locked",
            repair=f"Wait for the owner of {path}; do not stop that process. Retry the same selection/root.",
        ) from error
    finally:
        for stream in reversed(opened):
            stream.close()


def record_result(evidence, action, selection, status, reason_code, message,
                  repair=None, **details):
    """Persist this attempt and its latest execution result under a caller-owned lock."""
    from execution import utc_now

    evidence = pathlib.Path(evidence)
    retry = retry_command(action, selection, evidence.relative_to(pathlib.Path(
        read_json(evidence / OWNER_FILE)["sourceRoot"])).as_posix())
    result = {
        "action": action, "selection": selection, "selectedExtractors": selected_extractors(selection),
        "executionStatus": status, "reasonCode": reason_code, "message": message,
        "repair": repair or retry, "retryCommand": retry,
        "evidencePath": str(evidence), "recordedUtc": utc_now(), **details,
    }
    attempt = result["recordedUtc"].replace(":", "").replace("+", "-")
    write_json(checked_path(evidence, f"attempts/{attempt}-{os.getpid()}.json"), result)
    write_json(checked_path(evidence, "execution-result.json"), result)
    return result


def main():
    """Query paths read-only, or lock and write selected ownership/attempt evidence."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("operation", choices=("path", "claim", "record", "metadata", "bundle"))
    parser.add_argument("--source", required=True)
    parser.add_argument("--extractor", default="roslyn")
    parser.add_argument("--evidence")
    parser.add_argument("--action", default="prepare")
    parser.add_argument("--status", default="failed")
    parser.add_argument("--reason", default="preparation_failed")
    parser.add_argument("--message", default="Execution preparation failed")
    parser.add_argument("--repair")
    args = parser.parse_args()
    source = pathlib.Path(args.source).resolve()
    evidence = evidence_path(source, args.extractor, args.evidence)
    runtime = runtime_path(source, args.extractor, evidence)
    validate_runtime(source, runtime, evidence, args.extractor)
    if args.operation == "path":
        print(runtime)
        return 0
    with execution_locks(runtime, evidence):
        validate_runtime(source, runtime, evidence, args.extractor)
        if args.operation == "claim":
            claim_execution(source, runtime, evidence, args.extractor)
        elif args.operation == "metadata":
            metadata = json.load(sys.stdin)
            metadata.update(selection=args.extractor, selectedExtractors=selected_extractors(args.extractor))
            write_json(evidence / "source-git.json", metadata)
        elif args.operation == "bundle":
            from codegraph_adapter import require_bundle

            try:
                require_bundle(
                    source / "99_Tools/Architecture/CodeGraph/node_modules/@colbymchenry/codegraph-linux-x64",
                    source / "99_Tools/Architecture/CodeGraph/syntax-context.cjs",
                )
            except Exception as error:
                repair = getattr(error, "repair", None) or CODEGRAPH_REPAIR
                record_result(evidence, args.action, args.extractor, getattr(error, "status", "failed"),
                              getattr(error, "reason_code", "codegraph_bundle_invalid"), str(error), repair)
                print(f"ERROR: {error}\nEvidence: {evidence}\nRepair: {repair}\n"
                      f"Retry: {retry_command(args.action, args.extractor, evidence.relative_to(source).as_posix())}",
                      file=sys.stderr)
                return 1
        else:
            result = record_result(evidence, args.action, args.extractor, args.status, args.reason,
                                   args.message, args.repair)
            print(json.dumps(result, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    # The bundle imports this module too; keep its ExecutionError identity when invoked as a script.
    sys.modules["execution_status"] = sys.modules[__name__]
    try:
        raise SystemExit(main())
    except Exception as error:
        repair = getattr(error, "repair", None) or (
            "Choose an unlinked, Git-excluded .backups/ root owned by this source/selection; "
            "use --extractor roslyn|codegraph|compare. No extraction completed."
        )
        print(f"ERROR: {error}\nRepair: {repair}", file=sys.stderr)
        raise SystemExit(1)
