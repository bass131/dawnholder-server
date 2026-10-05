"""Current-input inspection orchestration; tool builds never build the product."""

import argparse
import hashlib
import json
import os
import pathlib
import shutil
import signal
import sys
import time

from inputs import CheckFailure, below, fail, file_record, git_command, load_rules, no_links
from inputs import relative_path, reserve_output, safe_path, snapshot_inputs
from processes import run_process, utc_now, write_json


SDK = "10.0.301"
REPO = pathlib.Path(__file__).resolve().parents[2]
BOUNDARIES = REPO / "99_Tools/ModuleBoundaries"


def argument_parser():
    parser = argparse.ArgumentParser(description="Roslyn current-source server module boundary warnings")
    parser.add_argument("--source-root", default=str(REPO))
    parser.add_argument("--source-ref", help="Exact Git commit SHA; copies original blobs instead of workspace bytes")
    parser.add_argument("--project", default="02_Server/GameServer/GameServer.csproj")
    parser.add_argument("--rules", default=str(BOUNDARIES / "module-boundaries.json"))
    parser.add_argument("--output-root", required=True, help="New directory below .backups or RUNNER_TEMP")
    parser.add_argument("--input-kind", choices=("production", "fixture"), default="production")
    parser.add_argument("--max-source-files", type=int, default=5000)
    parser.add_argument("--max-source-bytes", type=int, default=64 * 1024 * 1024)
    parser.add_argument("--stage-timeout", type=float, default=600)
    parser.add_argument("--github-annotations", action="store_true")
    return parser


def isolated_environment(output):
    environment = dict(os.environ)
    runtime = output / "work/runtime"
    directories = {
        "DOTNET_CLI_HOME": runtime / "cli-home",
        "NUGET_PACKAGES": runtime / "nuget/packages",
        "NUGET_HTTP_CACHE_PATH": runtime / "nuget/http",
        "NUGET_PLUGINS_CACHE_PATH": runtime / "nuget/plugins",
        "NUGET_SCRATCH": runtime / "nuget/scratch",
        "TMPDIR": runtime / "tmp",
    }
    for key, path in directories.items():
        path.mkdir(parents=True)
        environment[key] = str(path)
    environment.update(
        DOTNET_ADD_GLOBAL_TOOLS_TO_PATH="0", DOTNET_GENERATE_ASPNET_CERTIFICATE="false",
        DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1",
        MSBUILDDISABLENODEREUSE="1", DOTNET_CLI_USE_MSBUILD_SERVER="0",
        PYTHONDONTWRITEBYTECODE="1", MSBUILDLOGTASKINPUTS="0",
        GIT_OPTIONAL_LOCKS="0",
    )
    return environment


def select_dotnet(environment):
    if "DAWNHOLDER_DOTNET" in environment:
        candidate = environment["DAWNHOLDER_DOTNET"]
        if not candidate or not os.path.isabs(candidate):
            fail("tool_unavailable", "DAWNHOLDER_DOTNET must be an absolute executable", "Select the existing pinned SDK host using an absolute path.")
    elif pathlib.Path("/home/bass1/.local/share/dawnholder/dotnet-10.0.301/dotnet").exists():
        candidate = "/home/bass1/.local/share/dawnholder/dotnet-10.0.301/dotnet"
    else:
        candidate = shutil.which("dotnet") or str(pathlib.Path.home() / ".dotnet/dotnet")
    if not pathlib.Path(candidate).is_file() or not os.access(candidate, os.X_OK):
        fail("tool_unavailable", f"Selected SDK host unavailable: {candidate}", "Supply an existing SDK10.0.301 host; no automatic installation or version fallback.")
    host = pathlib.Path(candidate).resolve()
    environment["DOTNET_HOST_PATH"] = str(host)
    environment["DOTNET_ROOT"] = str(host.parent)
    return str(host)


def annotation_escape(value, property_value=False):
    text = str(value).replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")
    if property_value:
        text = text.replace(":", "%3A").replace(",", "%2C")
    return text


def emit_annotations(result):
    if result["executionStatus"] != "completed":
        print("::error title=Module boundary inspection failed::" + annotation_escape(result["reasonCode"] + ": " + result["message"]))
        return
    for violation in result["violations"]:
        source = violation["source"]
        # Fixture files live below a separate root; keep their real repository path.
        file_name = result["annotationSourcePrefix"] + source["path"]
        properties = (
            f"file={annotation_escape(file_name, True)},line={source['line']},"
            f"col={source['column']},title={annotation_escape(violation['ruleId'], True)}"
        )
        label = "[fixture demonstration] " if result["inputKind"] == "fixture" else ""
        message = label + violation["message"] + " " + violation["repair"]
        print(f"::warning {properties}::{annotation_escape(message)}")


def summary_text(result):
    counts = result.get("ruleCounts") or {}
    lines = [
        f"### Module boundaries ({result['inputKind']})", "",
        f"Execution: **{result['executionStatus']}**; analysis: **{result['analysisStatus']}**; exit: {result['exitCode']}.",
        f"Checkout SHA: `{result.get('checkoutSha')}`; source SHA: `{result.get('sourceSha')}`; PR head: `{result.get('prHeadSha')}`.",
        f"Reason: {result['reasonCode']}; {result['message']}",
        f"Elapsed: {result['elapsedSeconds']:.3f} s; violations: {result['violationCount']}.",
        "", "| Rule | Occurrences |", "|---|---:|",
    ]
    lines.extend(f"| {rule} | {counts.get(rule, 'not completed')} |" for rule in ("MB001", "MB002", "MB003"))
    if result.get("input"):
        lines.extend([
            "",
            f"Input: {result['input']['sourceFileCount']} source files, "
            f"{result['input']['sourceBytes']} bytes; hash `{result['input']['sha256']}`.",
        ])
    if result.get("coverage"):
        coverage = result["coverage"]
        lines.append(f"Boundary coverage: {coverage['analyzedBoundaryFiles']}/{coverage['expectedBoundaryFiles']} physical compiled files (Debug).")
    lines.extend([
        "", f"Repair: {result['repair']}", "",
        "Static Debug compilation only; no product build/emit, Unity/DB execution, reflection/dynamic target or actor safety verdict.",
        "",
    ])
    return "\n".join(lines)


def execute(options, output, result):
    environment = isolated_environment(output)
    permitted = [REPO]
    if os.environ.get("RUNNER_TEMP"):
        permitted.append(pathlib.Path(os.environ["RUNNER_TEMP"]))
    source = safe_path(options.source_root, permitted)
    if not source.is_dir():
        fail("input_missing", f"Source root missing: {source}", "Select a complete current repository or explicit fixture root.")
    rules_path = safe_path(options.rules, permitted)
    policy = load_rules(rules_path)
    write_json(output / "rules.json", policy)
    result["policy"] = {
        "schemaVersion": policy["schemaVersion"], "version": policy["version"],
        "sha256": hashlib.sha256(rules_path.read_bytes()).hexdigest(),
        "ruleIds": [rule["id"] for rule in policy["rules"]],
    }
    result["limits"] = {
        "sourceFiles": options.max_source_files, "sourceBytes": options.max_source_bytes,
        "stageTimeoutSeconds": options.stage_timeout,
    }
    if (
        not 1 <= options.max_source_files <= 5000
        or not 1 <= options.max_source_bytes <= 64 * 1024 * 1024
        or not 0 < options.stage_timeout <= 600
    ):
        fail("input_limit", "Requested limits must only tighten 5000 files / 64MiB / 600s", "Use the fixed defaults or smaller positive limits.")
    project = relative_path(options.project)
    if project != policy["project"]:
        fail("input_invalid", "Project differs from the approved server entry", "Use 02_Server/GameServer/GameServer.csproj in the selected source root.")

    def stage(name, command, cwd, allow_failure=False):
        folder = output / "stages" / f"{len(result['stages']):04d}-{name}"
        record = run_process(command, cwd, folder, environment, options.stage_timeout)
        record["name"] = name
        record["rawPath"] = str(folder.relative_to(output))
        result["stages"].append(record)
        write_json(output / "progress.json", result)
        if record["exitCode"] != 0 and not allow_failure:
            reason = record["reasonCode"] or "execution_failed"
            fail(
                reason, f"Stage {name} exited {record['exitCode']}; see {record['rawPath']}/stderr.txt",
                "Inspect this run's stage logs, repair its input/tool condition, and rerun in a new output directory.",
            )
        return record, folder

    git_prefix = None

    def git(arguments, binary=False):
        record, folder = stage("git-" + arguments[0], [*git_prefix, *arguments], source)
        content = (folder / "stdout.txt").read_bytes()
        return content if binary else content.decode("utf-8").rstrip("\n")

    result["sourceRoot"] = str(source)
    if options.input_kind == "production":
        git_prefix, result["gitMetadataMode"] = git_command(source)
        result["checkoutSha"] = git(["rev-parse", "HEAD"])
        result["sourceSha"] = result["checkoutSha"]
        result["workspaceStatus"] = git(["status", "--porcelain=v1", "--untracked-files=no"])
    else:
        result["sourceSha"] = None
        result["checkoutSha"] = None
        result["workspaceStatus"] = "explicit synthetic fixture; SHA not claimed"
    revision = options.source_ref
    if revision:
        if options.input_kind != "production" or len(revision) != 40 or any(c not in "0123456789abcdef" for c in revision):
            fail("input_invalid", "source-ref must be an exact lowercase Git commit SHA for production", "Resolve the revision before running and pass its exact commit SHA.")
        result["sourceSha"] = git(["rev-parse", "--verify", "--end-of-options", revision + "^{commit}"])
        result["sourceMode"] = "git_blobs"
    else:
        result["sourceMode"] = "workspace" if options.input_kind == "production" else "fixture"
    result["annotationSourcePrefix"] = (
        str(source.relative_to(REPO)).replace("\\", "/") + "/"
        if below(source, REPO) and source != REPO else ""
    )
    if options.input_kind == "fixture" and not below(source, REPO):
        result["annotationSourcePrefix"] = str(source) + "/"
    snapshot = output / "work/source"
    snapshot_started = time.perf_counter()
    manifest = snapshot_inputs(source, snapshot, project, policy, options.max_source_files, options.max_source_bytes, revision, git)
    write_json(output / "input-manifest.json", manifest)
    result["input"] = manifest
    result["workspaceUntrackedInputs"] = []
    if result["sourceMode"] == "workspace":
        # Query only snapshotted paths. Broad untracked status would enumerate
        # unrelated files; ignored inputs still need provenance if snapshotted.
        paths = [entry["path"] for entry in manifest["files"]]
        tracked = set(git([
            "ls-files", "--cached", "-z", "--",
            *[":(literal)" + path for path in paths],
        ]).split("\0"))
        result["workspaceUntrackedInputs"] = sorted(path for path in paths if path not in tracked)
    result["inputSnapshotElapsedSeconds"] = time.perf_counter() - snapshot_started
    pin = json.loads((snapshot / "global.json").read_text())["sdk"]
    if pin.get("version") != SDK or pin.get("rollForward") != "disable":
        fail("input_invalid", "Source global.json does not pin SDK10.0.301 with rollForward disable", "Restore the approved SDK pin; do not upgrade global configuration.")
    dotnet = select_dotnet(environment)
    _, folder = stage("sdk-version", [dotnet, "--version"], snapshot)
    observed = (folder / "stdout.txt").read_text().strip()
    if observed != SDK:
        fail("tool_unavailable", f"Selected host reports SDK {observed}, required {SDK}", "Use the existing exact SDK10.0.301; no version fallback.")
    result["tool"] = {"sdkVersion": observed, "dotnetHost": dotnet, "pythonVersion": sys.version, "files": []}
    tool = output / "work/tool"
    tool.mkdir()
    for path in sorted(BOUNDARIES.iterdir()):
        if path.is_file() and path.suffix in (".cs", ".csproj", ".py", ".json", ".sh", ".props"):
            no_links(path)
            result["tool"]["files"].append(file_record(path, str(path.relative_to(REPO))))
            if path.suffix in (".cs", ".csproj"):
                shutil.copyfile(path, tool / path.name)
    shutil.copyfile(BOUNDARIES / "Directory.Build.props", tool / "Directory.Build.props")
    shutil.copyfile(snapshot / "global.json", tool / "global.json")
    result["tool"]["inputSha256"] = hashlib.sha256(json.dumps(result["tool"]["files"], sort_keys=True, separators=(",", ":")).encode()).hexdigest()
    write_json(output / "tool-manifest.json", result["tool"])
    # Restore is explicit and isolated; only the checker is built/emitted.
    stage("tool-restore", [dotnet, "restore", str(tool / "Architecture.Boundaries.csproj"), "--disable-parallel", "-p:NuGetAudit=false", "-nr:false"], tool)
    stage("tool-build", [dotnet, "build", str(tool / "Architecture.Boundaries.csproj"), "--no-restore", "-p:UseSharedCompilation=false", "-nr:false"], tool)
    for relative in reversed(manifest["projects"]):
        stage("source-restore", [dotnet, "restore", str(snapshot / relative), "--disable-parallel", "-p:NuGetAudit=false", "-nr:false"], snapshot)
    raw = output / "analysis.json"
    record, _ = stage("analysis", [
        dotnet, str(tool / "bin/Debug/net10.0/Architecture.Boundaries.dll"),
        str(snapshot), str(output / "input-manifest.json"), str(output / "rules.json"),
        str(raw), str(options.stage_timeout),
    ], snapshot, allow_failure=True)
    if record["reasonCode"] in ("timeout", "cancelled", "tool_unavailable"):
        fail(record["reasonCode"], "Analysis process did not complete", "Inspect analysis stage raw logs and rerun in a new output directory.")
    if not raw.is_file():
        fail("execution_failed", "Analysis did not produce its own current raw result", "Inspect this run's analysis stderr; no previous result will be reused.")
    analysis = json.loads(raw.read_text())
    result["diagnostics"] = analysis["diagnostics"]
    result["coverage"] = analysis["coverage"]
    result["tool"]["roslynVersion"] = analysis["roslynVersion"]
    if analysis["reasonCode"] == "timeout":
        fail("timeout", analysis["message"], "Inspect the analysis deadline and rerun in a new output directory.")
    if record["exitCode"] != 0 or analysis["status"] != "completed":
        fail("analysis_failed", analysis["message"], "Resolve the source/Workspace diagnostics before trusting policy counts.")
    result["references"] = analysis["references"]
    result["violations"] = analysis["violations"]
    result["violationCount"] = len(analysis["violations"])
    result["ruleCounts"] = {rule["id"]: sum(v["ruleId"] == rule["id"] for v in analysis["violations"]) for rule in policy["rules"]}
    # Re-read fixed snapshots after design-time work to detect source/tool mutation.
    for entry in manifest["files"]:
        if file_record(snapshot / entry["path"], entry["path"]) != entry:
            fail("input_changed", f"Snapshot input changed during analysis: {entry['path']}", "Inspect the source build contract; a mutated input is not a completed check.")
    if not revision:
        for entry in manifest["files"]:
            original = safe_path(source / entry["path"], [source])
            if file_record(original, entry["path"]) != entry:
                fail("input_changed", f"Original input changed during inspection: {entry['path']}", "Stop concurrent writes and rerun with fresh input/evidence.")
    for entry in result["tool"]["files"]:
        if file_record(REPO / entry["path"], entry["path"]) != entry:
            fail("input_changed", f"Checker source changed during inspection: {entry['path']}", "Finish checker edits and rerun from a fixed tool version.")
    result.update(
        executionStatus="completed",
        analysisStatus="warnings" if result["violationCount"] else "clean",
        reasonCode="policy_warnings" if result["violationCount"] else "no_violations",
        message="Static boundary inspection completed.",
        repair="Review warning locations with the owning part; warning policy does not block the PR.",
        exitCode=0,
    )


def main():
    options = argument_parser().parse_args()
    started = time.perf_counter()
    result = {
        "schemaVersion": 1, "startedUtc": utc_now(), "argv": sys.argv[:],
        "inputKind": options.input_kind, "executionStatus": "failed", "analysisStatus": "not_completed",
        "reasonCode": "not_started", "message": "Inspection has not completed", "repair": "Inspect current raw evidence.",
        "sourceSha": None, "checkoutSha": None, "prHeadSha": os.environ.get("MODULE_BOUNDARIES_PR_HEAD_SHA"),
        "violationCount": None, "violations": [], "references": [], "diagnostics": [], "stages": [], "exitCode": 1,
    }
    output = None
    def cancel(_number, _frame):
        raise KeyboardInterrupt
    signal.signal(signal.SIGTERM, cancel)
    try:
        output = reserve_output(options.output_root, REPO, os.environ.get("RUNNER_TEMP"), options.source_root)
        execute(options, output, result)
    except CheckFailure as error:
        result.update(
            reasonCode=error.reason, message=str(error), repair=error.repair,
            violationCount=None, analysisStatus="not_completed",
        )
        if error.reason == "tool_unavailable":
            result["executionStatus"] = "unavailable"
        result["exitCode"] = 124 if error.reason == "timeout" else 1
        print(f"{error.reason}: {error}. Repair: {error.repair}", file=sys.stderr)
    except KeyboardInterrupt:
        result.update(
            executionStatus="cancelled", reasonCode="cancelled",
            message="Inspection cancelled; owned process group settled.",
            repair="Use the preserved evidence and a new output directory for retry.",
            exitCode=130, violationCount=None, analysisStatus="not_completed",
        )
    except Exception as error:
        result.update(
            reasonCode="execution_failed", message=f"{type(error).__name__}: {error}",
            repair="Inspect current raw logs and repair the tool/input condition.",
            violationCount=None, analysisStatus="not_completed",
        )
        print(result["message"], file=sys.stderr)
    result.update(elapsedSeconds=time.perf_counter() - started, endedUtc=utc_now())
    if output:
        write_json(output / "result.json", result)
        (output / "summary.md").write_text(summary_text(result), encoding="utf-8")
    if options.github_annotations:
        emit_annotations(result)
    print(
        f"module-boundaries ({options.input_kind}): {result['executionStatus']}/{result['analysisStatus']}; "
        f"{result['reasonCode']}; exit {result['exitCode']}; {result['elapsedSeconds']:.3f}s"
    )
    return result["exitCode"]
