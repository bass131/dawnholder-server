"""Validated source snapshots and explicit policy inputs; no frozen assets used."""

import hashlib
import json
import os
import pathlib
import xml.etree.ElementTree as xml

from processes import write_json


class CheckFailure(Exception):
    def __init__(self, reason, message, repair):
        super().__init__(message)
        self.reason = reason
        self.repair = repair


def fail(reason, message, repair):
    raise CheckFailure(reason, message, repair)


def no_links(path):
    path = pathlib.Path(os.path.abspath(path))
    for component in [path, *path.parents]:
        if component.is_symlink():
            fail("unsafe_path", f"Symlink path is not allowed: {component}", "Use a direct path inside the permitted root.")
    return path


def below(path, root):
    return path == root or root in path.parents


def safe_path(path, roots):
    path = no_links(path)
    if not any(below(path, no_links(root)) for root in roots):
        fail("unsafe_path", f"Path leaves permitted roots: {path}", "Select a path inside the repository or job-local RUNNER_TEMP root.")
    return path


def relative_path(text):
    normalized = text.replace("\\", "/")
    path = pathlib.PurePosixPath(normalized)
    if not normalized or path.is_absolute() or any(part in ("..", ".") for part in normalized.split("/")) or ":" in normalized:
        fail("unsafe_path", f"Expected safe relative input path: {text}", "Use a repository-relative path without root traversal.")
    return str(path)


def reserve_output(path, repo, runner_temp, source_root):
    roots = [repo / ".backups"]
    if runner_temp:
        roots.append(pathlib.Path(runner_temp))
    output = safe_path(path, roots)
    if output in roots:
        fail("unsafe_path", "Output root must be a new dedicated child directory", "Choose a new run directory below .backups or RUNNER_TEMP.")
    source = pathlib.Path(os.path.abspath(source_root))
    if source != repo and below(output, source):
        fail("unsafe_path", "Output overlaps the supplied source root", "Place output alongside fixture/source inputs, never inside them.")
    # Even an empty existing folder can belong to another run; never reuse it.
    if output.exists():
        fail("unsafe_path", f"Output already exists; preserving it: {output}", "Choose a new output root; do not delete prior evidence.")
    output.mkdir(parents=True)
    write_json(output / "owner.json", {"tool": "Architecture.Boundaries", "pid": os.getpid()})
    return output


def load_rules(path):
    try:
        rules = json.loads(path.read_text(encoding="utf-8"))
        if (
            not isinstance(rules, dict)
            or type(rules.get("schemaVersion")) is not int
            or rules["schemaVersion"] != 1
            or rules["version"] != "1.0"
        ):
            raise ValueError("Unsupported policy schema/version")
        if rules.get("project") != "02_Server/GameServer/GameServer.csproj":
            raise ValueError("Policy project differs from the approved server entry")
        expected = {"MB001": ("Handlers", "Maps"), "MB002": ("Maps", "Handlers"), "MB003": ("Sessions", "Handlers")}
        exact_allowed = ["Dawnholder.Server.GameServer.Handlers.HandlerRegistry", "Dawnholder.Server.GameServer.Handlers.IPacketHandler"]
        if not isinstance(rules["areas"], dict) or set(rules["areas"]) != {"Handlers", "Maps", "Sessions"}:
            raise ValueError("Policy must describe the three approved areas")
        for area, value in rules["areas"].items():
            if relative_path(value) != "02_Server/GameServer/" + area:
                raise ValueError("Area paths differ from the approved physical boundaries")
        if (
            not isinstance(rules["rules"], list)
            or len(rules["rules"]) != 3
            or {rule["id"] for rule in rules["rules"]} != set(expected)
        ):
            raise ValueError("Policy must contain MB001, MB002 and MB003 exactly once")
        for rule in rules["rules"]:
            if (rule["from"], rule["to"]) != expected[rule["id"]] or rule["severity"] != "warning":
                raise ValueError("Policy direction/severity differs from approved warning scope")
            allowed = exact_allowed if rule["id"] == "MB003" else []
            if rule["allowedTypes"] != allowed:
                raise ValueError("Policy dispatcher exceptions differ from the exact contract")
            if not all(isinstance(rule.get(key), str) and rule[key].strip() for key in ("repair", "reason")):
                raise ValueError("Policy must retain a textual reason and repair")
            if (
                not isinstance(rule["sources"], list)
                or not rule["sources"]
                or not all(isinstance(source, str) and source.strip() for source in rule["sources"])
            ):
                raise ValueError("Policy must retain source, reason and repair")
        if (
            not isinstance(rules["occurrenceUnit"], str)
            or not rules["occurrenceUnit"].strip()
            or not isinstance(rules["exclusions"], list)
            or not rules["exclusions"]
            or not all(isinstance(value, str) and value.strip() for value in rules["exclusions"])
        ):
            raise ValueError("Policy must document occurrence/exclusion boundaries")
        return rules
    except CheckFailure:
        raise
    except (OSError, ValueError, KeyError, TypeError, AttributeError) as error:
        fail("invalid_rules", f"Cannot read valid approved rules: {error}", "Restore the versioned module-boundaries.json policy or supply a valid file.")


def file_record(path, relative):
    content = path.read_bytes()
    return {"path": relative, "bytes": len(content), "sha256": hashlib.sha256(content).hexdigest()}


def git_command(source):
    """Read Windows-created linked-worktree metadata from its WSL mount.

    The backlink must identify this exact checkout. Only command-local Git
    arguments change; neither pointer files nor repository settings are written.
    """
    pointer = no_links(source / ".git")
    if os.name == "nt" or not pointer.is_file():
        return ["git", "-C", str(source)], "native"
    if pointer.stat().st_size > 4096:
        fail("input_invalid", "Git worktree pointer is oversized", "Use the original Orca/Git checkout pointer.")
    text = pointer.read_text(encoding="utf-8").strip()
    if not text.startswith("gitdir: "):
        fail("input_invalid", "Invalid Git worktree pointer", "Use the original Orca/Git checkout pointer.")

    def mounted_windows_path(value):
        windows = pathlib.PureWindowsPath(value)
        drive = windows.drive
        if len(drive) != 2 or drive[1] != ":" or not drive[0].isalpha() or not windows.is_absolute():
            fail("input_invalid", "Unsupported Windows Git metadata path", "Use the original drive-absolute pointer on a WSL /mnt/<drive> mount.")
        mounted = pathlib.Path("/mnt") / drive[0].lower() / pathlib.Path(*windows.parts[1:])
        return no_links(mounted)

    metadata_text = text[len("gitdir: "):]
    if not pathlib.PureWindowsPath(metadata_text).is_absolute():
        return ["git", "-C", str(source)], "native"
    metadata = mounted_windows_path(metadata_text)
    backlink = no_links(metadata / "gitdir")
    if not backlink.is_file() or backlink.stat().st_size > 4096:
        fail("input_missing", "Linked-worktree Git backlink missing/invalid", "Restore the original Orca/Git metadata; do not rewrite it for this checker.")
    if mounted_windows_path(backlink.read_text(encoding="utf-8").strip()) != pointer:
        fail("input_invalid", "Git metadata belongs to a different checkout", "Use the original metadata for this exact source root.")
    return ["git", "--git-dir", str(metadata), "--work-tree", str(source), "-C", str(source)], "windows_linked_worktree_wsl"


def snapshot_inputs(source, snapshot, project, policy, max_files, max_bytes, revision=None, git=None):
    """Only explicit project references and their physical source/build inputs.

    Unsupported dynamic project paths/imports fail closed instead of claiming
    partial coverage. Workspace later confirms actual Compile inputs separately.
    """
    snapshot.mkdir()
    listing = None
    if revision:
        listing = git(["ls-tree", "-r", "--long", "-z", revision]).split("\0")
        entries = {}
        for entry in listing:
            if not entry:
                continue
            attributes, name = entry.split("\t", 1)
            mode, kind, object_id, size = attributes.split()
            entries[name] = (mode, kind, object_id, int(size) if size != "-" else None)

    def exists(relative):
        return relative in entries if revision else (source / relative).is_file()

    def read(relative):
        relative = relative_path(relative)
        if revision:
            if relative not in entries:
                fail("input_missing", f"Git input missing: {relative}", "Resolve the exact source revision containing every referenced project/input.")
            mode, kind, object_id, _ = entries[relative]
            if mode not in ("100644", "100755") or kind != "blob":
                fail("unsafe_path", f"Git input is a link/non-file: {relative}", "Use regular source files; submodule/link inputs are unsupported.")
            return git(["cat-file", "blob", object_id], binary=True)
        path = safe_path(source / relative, [source])
        if not path.is_file():
            fail("input_missing", f"Input missing: {relative}", "Restore the referenced project/source before rerunning.")
        return path.read_bytes()

    selected = set()
    projects = []
    source_paths = set()
    source_bytes = 0

    def add(relative):
        nonlocal source_bytes
        relative = relative_path(relative)
        if relative in selected:
            return
        if relative.endswith(".cs"):
            if revision:
                size = entries.get(relative, (None, None, None, None))[3]
            else:
                original = safe_path(source / relative, [source])
                if not original.is_file():
                    fail("input_missing", f"Source input missing: {relative}", "Restore the explicit Compile source before rerunning.")
                size = original.stat().st_size
            if size is not None and (len(source_paths) >= max_files or source_bytes + size > max_bytes):
                fail(
                    "input_limit", f"Source limit would be exceeded by {relative}: {size} bytes",
                    "Reduce source scope within the approved server graph; do not raise the fixed ceilings.",
                )
        data = read(relative)
        if relative.endswith(".cs"):
            source_paths.add(relative)
            source_bytes += len(data)
            if len(source_paths) > max_files or source_bytes > max_bytes:
                fail(
                    "input_limit", f"Source limit exceeded: {len(source_paths)} files / {source_bytes} bytes",
                    "Reduce source scope within the approved server graph; do not raise the fixed ceilings.",
                )
        target = snapshot / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
        selected.add(relative)

    def collect_project(relative):
        if relative in projects:
            return
        projects.append(relative)
        add(relative)
        try:
            document = xml.fromstring(read(relative))
        except xml.ParseError as error:
            fail("input_invalid", f"Invalid project XML {relative}: {error}", "Repair the source project XML.")
        directory = pathlib.PurePosixPath(relative).parent
        for element in document.iter():
            tag = element.tag.split("}")[-1]
            if tag == "Import":
                fail("input_invalid", f"Explicit project Import unsupported: {relative}", "Declare all required inputs before adding an imported project contract.")
            if tag == "Compile" and "Include" in element.attrib:
                include = element.attrib["Include"].replace("\\", "/")
                if "$" in include or "*" in include or ";" in include:
                    fail("input_invalid", f"Dynamic Compile Include unsupported: {relative}: {include}", "Use explicit in-root Compile includes for this checker.")
                linked = pathlib.PurePosixPath(os.path.normpath(str(directory / include)).replace("\\", "/"))
                add(str(linked))
            if tag == "ProjectReference":
                reference = element.attrib.get("Include", "").replace("\\", "/")
                if any(character in reference for character in ("$", "*", ";")):
                    fail("input_invalid", f"Dynamic ProjectReference unsupported: {reference}", "Make the source graph explicit for inspection.")
                normalized = os.path.normpath(str(directory / reference)).replace("\\", "/")
                collect_project(relative_path(normalized))
        prefix = str(directory) + "/"
        if revision:
            candidates = [name for name in entries if name.startswith(prefix)]
        else:
            candidates = []
            for current, subdirs, names in os.walk(source / str(directory), followlinks=False):
                for child in subdirs:
                    no_links(pathlib.Path(current) / child)
                subdirs[:] = [name for name in subdirs if name not in ("bin", "obj", ".git")]
                candidates.extend(str((pathlib.Path(current) / name).relative_to(source)).replace("\\", "/") for name in names)
        for name in sorted(candidates):
            if name.endswith(".cs") and not ({"bin", "obj", ".git"} & set(pathlib.PurePosixPath(name).parts)):
                add(name)
        for ancestor in [directory, *directory.parents]:
            for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "NuGet.Config", "nuget.config", ".editorconfig"):
                candidate = str(ancestor / name)
                if exists(candidate):
                    add(candidate)

    add("global.json")
    collect_project(project)
    expected = []
    missing = []
    for area, prefix in policy["areas"].items():
        area_files = sorted(name for name in source_paths if name.startswith(prefix + "/") and not name.endswith((".g.cs", ".generated.cs")))
        expected.extend(area_files)
        if not area_files:
            missing.append(area)
    if not expected:
        fail("zero_targets", "No compiled boundary source candidates", "Provide nonempty Handlers, Maps and Sessions source inputs.")
    if missing:
        fail("missing_boundary", f"Missing boundary sources: {', '.join(missing)}", "Restore each approved source boundary; a partial target set is not completion.")
    records = [file_record(snapshot / name, name) for name in sorted(selected)]
    manifest = {
        "files": records, "sourceFiles": sorted(source_paths), "sourceFileCount": len(source_paths),
        "sourceBytes": source_bytes, "fileCount": len(records), "projects": projects,
        "expectedBoundaryFiles": sorted(expected),
        "sha256": hashlib.sha256(json.dumps(records, sort_keys=True, separators=(",", ":")).encode()).hexdigest(),
    }
    return manifest
