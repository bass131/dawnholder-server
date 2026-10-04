"""Owned WSL selected extraction; command results precede separate analysis/scoring."""
import argparse
import csv
import datetime
import hashlib
import json
import os
import pathlib
import shutil
import subprocess
import sys
import time
from collections import Counter

import codegraph_adapter
from execution import run_process, utc_now
from inputs import read_json, write_json, digest, verify, relative_path
from inputs import checked_path
from execution_status import (
    CODEGRAPH_REPAIR, ExecutionError, evidence_path, execution_locks, record_result,
    retry_command, selected_extractors, validate_runtime,
)
# Keep dump_codegraph importable here as well as from normalization.
from normalization import normalize, dump_codegraph
from snapshot import validate, layer


def require_success(record, folder):
    """Own command-success classification from the original helper's command record."""
    if record["exitCode"] != 0:
        status = "unavailable" if record.get("startUnavailable") else "failed"
        raise ExecutionError(
            f"Command failed; see {folder}/command.json and stdout/stderr",
            "command_start_unavailable" if status == "unavailable" else "command_failed",
            status, command_record=str(folder / "command.json"),
        )


def run_command(command, cwd, folder):
    """Run the existing argv/log/timeout helper; retain evidence when startup fails."""
    try:
        record = run_process(command, cwd, folder)
    except OSError as error:
        record_path = folder / "command.json"
        record = read_json(record_path) if record_path.exists() else {"argv": list(map(str, command))}
        record.update(startUnavailable=True, failure=str(error), exitCode=None, endedUtc=utc_now())
        write_json(record_path, record)
        raise ExecutionError(f"Cannot start command: {command[0]}; {error}", "command_start_unavailable",
                             "unavailable", command_record=str(record_path)) from error
    # GNU time can start although its target cannot; do not treat a tool's own 126/127 as startup failure.
    stderr = (folder / "stderr.txt").read_text()
    if record["exitCode"] in (126, 127) and "/usr/bin/time: cannot run " in stderr:
        record["startUnavailable"] = True
        write_json(folder / "command.json", record)
    return record


def implementation_files(runtime):
    """Hash the selected copied tool files, excluding generated build/bytecode output."""
    return [
        {"path": str(path.relative_to(runtime / "tool")), "sha256": digest(path)}
        for path in sorted((runtime / "tool").rglob("*"))
        if path.is_file() and path.suffix in {".py", ".cs", ".csproj", ".sh", ".props", ".cjs"}
        and "obj" not in path.parts and "bin" not in path.parts
    ]


def cache_state(root, extractor):
    if extractor == "Roslyn":
        return {"persistentAnalysisCache": False, "process": "fresh", "workspacePrerequisites": "restored projects/SDK; compiler build hosts may share OS page cache"}
    return codegraph_adapter.cache_state(root)


def freeze_check(source, evidence):
    """Read captured source provenance and verify frozen evaluation bytes without writes."""
    metadata = read_json(evidence / "source-git.json")
    for item in metadata["frozenFiles"]:
        if digest(checked_path(source, item["path"])) != item["sha256"]:
            raise ValueError("Frozen evaluation input changed")
    return metadata


def layer_metrics(raw, snapshot):
    symbol_counts = Counter()
    for symbol in raw.get("symbols", raw.get("nodes", [])):
        path = (symbol.get("source") or {}).get("path") if raw["extractor"] == "Roslyn" else symbol.get("file_path")
        symbol_counts[layer(path)] += 1
    node_layers = {node["id"]: node["layer"] for node in snapshot["nodes"]}
    edge_counts = Counter((node_layers[edge["sourceId"]], edge["kind"], edge["resolution"]) for edge in snapshot["edges"])
    project_layers = {compilation["name"]: {layer(option["path"]) for option in compilation["parseOptions"]}
                      for compilation in raw.get("compilations", [])}
    error_layers = set()
    for diagnostic in raw.get("diagnostics", []):
        if diagnostic.get("severity") == "Error":
            error_layers.update(project_layers.get(diagnostic.get("project"), {layer((diagnostic.get("source") or {}).get("path"))}))
        if diagnostic.get("kind") == "workspace" and diagnostic.get("severity") == "Failure":
            error_layers.update(("Server", "Shared", "ClientNet", "Client"))
    return {name: {"rawSymbolCount": symbol_counts[name], "normalizedRelations": [{"kind": kind, "resolution": resolution, "count": count} for (source_layer, kind, resolution), count in sorted(edge_counts.items()) if source_layer == name],
                   "status": raw["status"] if raw["status"] in {"failed", "notRun"} else "partial" if name == "Client" or name in error_layers or raw["extractor"] == "CodeGraph" else "complete",
                   "statusReason": "Unity asmdef approximation; missing package references" if name == "Client" and raw["extractor"] == "Roslyn" else "Compiler-free grammar/resolver index" if raw["extractor"] == "CodeGraph" else "Actual Debug project compilation; errors/diagnostics retained in raw"}
            for name in ("Server", "Shared", "ClientNet", "Client")}


def extractor_config(raw, runtime, batch):
    config = {"baseConfigHash": digest(batch / "config.json"), "extractor": raw["extractor"], "version": raw["version"]}
    if raw["extractor"] == "Roslyn":
        config["compilations"] = [{"name": c["name"], "options": c["options"], "compiler": c["compiler"],
                                    "parseOptions": sorted(c["parseOptions"], key=lambda p: p["path"]),
                                    "references": sorted(c["references"], key=lambda r: json.dumps(r, sort_keys=True))}
                                   for c in raw.get("compilations", [])]
    else:
        config.update(codegraph_adapter.bundle_config(runtime))
    return config


def check_outputs(source, runtime, evidence, verify_format=False, selection="roslyn", batch=None, runs=None):
    """Validate only the selected complete batch and execute scoring (and Roslyn format).

    Stored selection/input/config and current implementation hashes must agree.
    A previous successful batch cannot certify the latest failed measure/prepare.
    """
    extractors = selected_extractors(selection)
    stored_result = batch is None
    manifest_path = runtime / "manifest.json"
    manifest = read_json(manifest_path)
    metadata = freeze_check(source, evidence)
    settings = read_json(runtime / "tool/comparison-settings.json")
    current_settings = read_json(source / "99_Tools/Architecture/comparison-settings.json")
    if ({key: value for key, value in settings.items() if key != "evidencePath"}
            != {key: value for key, value in current_settings.items() if key != "evidencePath"}):
        raise ValueError("Current frozen input/tool settings differ from prepared settings")
    source_manifest = checked_path(source, settings["goalPath"]) / "input-manifest.json"
    if (metadata.get("selection") != selection or metadata.get("selectedExtractors") != extractors
            or metadata["sourceCommit"] != manifest["sourceCommit"]
            or metadata["manifestHash"] != digest(manifest_path) or digest(source_manifest) != digest(manifest_path)):
        raise ValueError("Stored mode/manifest/source SHA differs from selected inputs")
    if batch is None:
        result = read_json(evidence / "execution-result.json")
        if result.get("selection") != selection or result.get("executionStatus") != "completed":
            raise ValueError("Latest execution did not complete this selection; rerun prepare/measure")
        latest = read_json(evidence / "latest-run.json")
        if latest.get("selection") != selection or latest.get("selectedExtractors") != extractors:
            raise ValueError("Stored results belong to another extractor selection")
        batch = pathlib.Path(latest["batch"])
        runs = latest["runs"]
    if not batch.is_relative_to(evidence / "runs"):
        raise ValueError("Stored batch escapes selected evidence root")
    checked_path(evidence, batch.relative_to(evidence).as_posix())
    config = read_json(batch / "config.json")
    if (config.get("selection") != selection or config.get("selectedExtractors") != extractors
            or config.get("manifestHash") != digest(manifest_path)
            or config.get("sourceCommit") != manifest["sourceCommit"]
            or config.get("implementationHead") != metadata["implementationHead"]
            or config.get("sourceMetadataHash") != digest(batch / "source-git.json")
            or config.get("sourceMetadataHash") != digest(evidence / "source-git.json")
            or config.get("inputCopyHash") != digest(batch / "input-copy.json")
            or config.get("settingsHash") != digest(runtime / "tool/comparison-settings.json")
            or config.get("evidenceRoot") != str(evidence) or config.get("runtimeRoot") != str(runtime)):
        raise ValueError("Batch config selection/manifest/source provenance mismatch")
    if config["implementationFiles"] != implementation_files(runtime):
        raise ValueError("Runtime implementation hashes differ from recorded batch")
    for item in config["implementationFiles"]:
        if digest(checked_path(source / "99_Tools/Architecture", item["path"])) != item["sha256"]:
            raise ValueError(f"Current implementation differs from recorded batch: {item['path']}")
    for item in config["entryPointFiles"]:
        if digest(checked_path(source, item["path"])) != item["sha256"]:
            raise ValueError(f"Current entry point differs from recorded batch: {item['path']}")
    expected = [(extractor, label) for extractor in extractors for label in ("cold", "warm1", "warm2", "warm3")]
    if [(run["extractor"], run["label"]) for run in runs] != expected:
        raise ValueError("Selected extractor results are missing, extra or out of order")
    copy_record = read_json(batch / "input-copy.json")
    input_names = [name.lower() + "-input" for name in ("Roslyn", "CodeGraph") if name in extractors]
    if (copy_record.get("selection") != selection or copy_record.get("selectedExtractors") != extractors
            or copy_record.get("verifiedCopies") != input_names
            or copy_record.get("manifestHash") != digest(manifest_path)
            or copy_record.get("sourceCommit") != manifest["sourceCommit"]
            or copy_record.get("implementationHead") != metadata["implementationHead"]
            or copy_record.get("sourceRoot") != str(source) or copy_record.get("runtimeRoot") != str(runtime)):
        raise ValueError("Verified input copies differ from selected batch")
    for name in input_names:
        verify(manifest, runtime / name)
    checks = []
    for run in runs:
        folder = pathlib.Path(run["folder"])
        if folder != batch / run["extractor"].lower() / run["label"]:
            raise ValueError("Run folder does not belong to selected batch")
        snapshot = read_json(folder / "normalized.json")
        raw = read_json(folder / "raw.json")
        command_record = read_json(folder / "analysis/command.json")
        if (snapshot["extractor"]["name"] != run["extractor"]
                or snapshot["extractor"]["configHash"] != digest(folder / "extractor-config.json")
                or read_json(folder / "extractor-config.json") != extractor_config(raw, runtime, batch)
                or raw["extractor"] != run["extractor"]
                or raw["analysisInput"] != {"sourceCommit": manifest["sourceCommit"], "manifestHash": digest(manifest_path)}
                or run["analysisStatus"] != snapshot["status"]
                or run["exitCode"] != command_record["exitCode"]):
            raise ValueError("Snapshot extractor/config hash mismatch")
        if stored_result:
            require_success(command_record, folder / "analysis")
            if snapshot["status"] in {"failed", "notRun"}:
                raise ValueError("Stored selected analysis failed/notRun; rerun measure")
            if run["extractor"] == "CodeGraph":
                require_success(read_json(folder / "syntax-context/command.json"), folder / "syntax-context")
        checks.append({"run": run["label"], "extractor": run["extractor"],
                       **validate(snapshot, manifest, digest(manifest_path), metadata["tree"])})
        # Recalculate scores using the public scoring CLI, after snapshots exist.
        goal = source / relative_path(settings["goalPath"])
        command = ["python3", str(runtime / "tool/Pipeline/cli.py"), "score", "--snapshot", str(folder / "normalized.json"), "--manifest", str(manifest_path), "--scope", str(goal / "evaluation-scope.json"), "--truth", str(goal / "truth.json"), "--out", str(folder / "score.json")]
        record = run_command(command, runtime, folder / "scoring")
        require_success(record, folder / "scoring")
    write_json(evidence / "self-check.json", {"checks": checks, "checkedUtc": utc_now(), "independentVerification": False})
    if verify_format and "Roslyn" in extractors:
        dotnet = os.environ["DOTNET_HOST_PATH"]
        version = run_command([dotnet, "--version"], runtime, evidence / "format/sdk-version")
        require_success(version, evidence / "format/sdk-version")
        if (evidence / "format/sdk-version/stdout.txt").read_text().strip() != "10.0.301":
            raise ValueError("SDK pin mismatch")
        command = [dotnet, "format", "whitespace", runtime / "tool/Roslyn/Architecture.Roslyn.csproj", "--no-restore", "--verify-no-changes", "--verbosity", "diagnostic"]
        record = run_command(command, runtime, evidence / "format/whitespace")
        require_success(record, evidence / "format/whitespace")
    return {"batch": str(batch), "analysisStatus": [
        {"extractor": run["extractor"], "label": run["label"], "status": run["analysisStatus"]} for run in runs
    ]}


def measure(source, runtime, evidence, dotnet=None, selection="roslyn"):
    """Run selected cold/warm commands in order, keeping partial/failed analysis evidence.

    Only an entirely completed execution publishes latest-run. Failed command
    snapshots remain failed and are scored as notScored, never a successful empty graph.
    """
    extractors = selected_extractors(selection)
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    batch = evidence / "runs" / stamp
    batch.mkdir(parents=True)
    manifest_path = runtime / "manifest.json"
    manifest = read_json(manifest_path)
    metadata = freeze_check(source, evidence)
    source_record = read_json(evidence / "input-copy.json")
    write_json(batch / "input-copy.json", source_record)
    write_json(batch / "source-git.json", metadata)
    if (metadata.get("selection") != selection or metadata.get("selectedExtractors") != extractors
            or source_record.get("selection") != selection or source_record.get("selectedExtractors") != extractors
            or source_record.get("verifiedCopies") != [
                name.lower() + "-input" for name in ("Roslyn", "CodeGraph") if name in extractors
            ]
            or source_record.get("sourceCommit") != manifest["sourceCommit"]
            or source_record.get("implementationHead") != metadata["implementationHead"]
            or source_record.get("sourceRoot") != str(source) or source_record.get("runtimeRoot") != str(runtime)
            or source_record["manifestHash"] != digest(manifest_path)
            or metadata["manifestHash"] != digest(manifest_path)
            or metadata["sourceCommit"] != manifest["sourceCommit"]):
        raise ValueError("Prepared input selection/manifest/source SHA mismatch")
    if "CodeGraph" in extractors:
        codegraph_adapter.require_bundle(runtime / "bundle", runtime / "tool/CodeGraph/syntax-context.cjs")
        codegraph_adapter.inspect_bundle(runtime, evidence)
    if "Roslyn" in extractors:
        if not dotnet or not pathlib.Path(dotnet).is_file() or not os.access(dotnet, os.X_OK):
            raise ExecutionError(f"Pinned SDK host missing/unusable: {dotnet}", "sdk_unavailable", "unavailable")
    # Retain CodeGraph help/version before SDK checks and either extractor run.
    commands = list(codegraph_adapter.preflight_commands(runtime)) if "CodeGraph" in extractors else []
    if "Roslyn" in extractors:
        commands.append(("sdk-version", [dotnet, "--version"]))
    for label, command in commands:
        record = run_command(command, runtime, batch / label)
        require_success(record, batch / label)
    if "Roslyn" in extractors and (batch / "sdk-version/stdout.txt").read_text().strip() != "10.0.301":
        raise ValueError("SDK pin mismatch; no fallback")
    if "CodeGraph" in extractors:
        codegraph_adapter.check_version(batch)
    tool_project = runtime / "tool/Roslyn/Architecture.Roslyn.csproj"
    roslyn_commands = (("tool-restore", [dotnet, "restore", tool_project, "--nologo"], runtime),
                                ("tool-build", [dotnet, "build", tool_project, "--no-restore", "--nologo", "-p:UseSharedCompilation=false"], runtime),
                                ("server-restore", [dotnet, "restore", "02_Server/GameServer/GameServer.csproj", "--nologo"], runtime / "roslyn-input"),
                                ("clientnet-restore", [dotnet, "restore", "04_ClientNet/Dawnholder.Client.Net.csproj", "--nologo"], runtime / "roslyn-input"))
    for label, command, cwd in roslyn_commands if "Roslyn" in extractors else ():
        record = run_command(command, cwd, batch / label)
        require_success(record, batch / label)
    config = {
        "schemaVersion": 1, "selection": selection, "selectedExtractors": extractors,
        "adapterVersion": "1", "sourceCommit": manifest["sourceCommit"],
        "manifestHash": digest(manifest_path), "implementationHead": metadata["implementationHead"],
        "sourceMetadataHash": digest(batch / "source-git.json"),
        "inputCopyHash": digest(batch / "input-copy.json"),
        "settingsHash": digest(runtime / "tool/comparison-settings.json"),
        "evidenceRoot": str(evidence), "runtimeRoot": str(runtime),
        "implementationFiles": implementation_files(runtime),
        "entryPointFiles": [
            {"path": "99_Tools/Architecture/" + name, "sha256": digest(source / "99_Tools/Architecture" / name)}
            for name in ("run-architecture.ps1", "Architecture.Common.ps1", "run-wsl.sh")
        ],
        "environment": {name: os.environ.get(name) for name in (
            "DO_NOT_TRACK", "CODEGRAPH_TELEMETRY", "CODEGRAPH_NO_UPDATE_CHECK", "CODEGRAPH_NO_DAEMON"
        )} if "CodeGraph" in extractors else {},
    }
    if "Roslyn" in extractors:
        config.update(sdk="10.0.301", configuration="Debug",
                      unityMode="supplied Managed DLLs; C#9 explicit editor/windows symbols; asmdef approximation")
    if "CodeGraph" in extractors:
        config.update(codegraphVersion="1.6.1", codegraphMode="init then full index warm; bundled node; no daemon")
    write_json(batch / "config.json", config)
    if "CodeGraph" in extractors:
        codegraph_adapter.archive_cache(runtime, stamp)
    runs = []
    failed_commands = []
    for extractor in extractors:
        root = runtime / ("codegraph-input" if extractor == "CodeGraph" else "roslyn-input")
        for index, label in enumerate(("cold", "warm1", "warm2", "warm3")):
            folder = batch / extractor.lower() / label
            folder.mkdir(parents=True)
            verify(manifest, root)
            freeze_check(source, evidence)
            before = cache_state(root, extractor)
            if extractor == "CodeGraph":
                command = codegraph_adapter.analysis_command(runtime, root, index)
            else:
                command = [
                    dotnet, runtime / "tool/Roslyn/bin/Debug/net10.0/Architecture.Roslyn.dll",
                    root, manifest_path, folder / "raw.json",
                ]
            first_record_path = evidence / f"first-analysis-{extractor.lower()}.json"
            if not first_record_path.exists():
                started = utc_now()
                if datetime.datetime.fromisoformat(started) <= datetime.datetime.fromisoformat(metadata["freezeCommittedUtc"].replace("Z", "+00:00")):
                    raise ValueError("Analysis precedes frozen commit")
                write_json(first_record_path, {"recordedBeforeExecutionUtc": started, "freezeCommit": metadata["freezeCommit"], "freezeCommittedUtc": metadata["freezeCommittedUtc"], "argv": list(map(str, command)), "commandRecord": str(folder / "analysis/command.json")})
            record = run_command(command, root, folder / "analysis")
            syntax_seconds = None
            syntax_peak = None
            if record["exitCode"] == 0:
                if extractor == "CodeGraph":
                    raw, syntax_record = codegraph_adapter.read_analysis(
                        runtime, root, manifest_path, folder, execute_command=run_command,
                    )
                    require_success(syntax_record, folder / "syntax-context")
                    syntax_seconds = syntax_record["elapsedSeconds"]
                    syntax_peak = syntax_record["peakMemoryBytes"]
                    raw["syntaxContext"] = read_json(folder / "syntax-context.json")
                else:
                    raw = read_json(folder / "raw.json")
            else:
                if record.get("startUnavailable"):
                    require_success(record, folder / "analysis")
                failed_commands.append(str(folder / "analysis/command.json"))
                raw = {"rawVersion": 1, "extractor": extractor, "version": "1.6.1" if extractor == "CodeGraph" else "SDK10.0.301", "status": "failed", "diagnostics": [{"kind": "execution", "exitCode": record["exitCode"], "commandRecord": str(folder / "analysis/command.json")} ]}
                write_json(folder / "raw.json", raw)
            raw["analysisInput"] = {"sourceCommit": manifest["sourceCommit"], "manifestHash": digest(manifest_path)}
            if raw["status"] in {"failed", "notRun"} and str(folder / "analysis/command.json") not in failed_commands:
                failed_commands.append(str(folder / "analysis/command.json"))
            write_json(folder / "raw.json", raw)
            write_json(folder / "extractor-config.json", extractor_config(raw, runtime, batch))
            adapter_started = time.perf_counter()
            snapshot = normalize(raw, manifest, digest(manifest_path), digest(folder / "extractor-config.json"))
            adapter_seconds = time.perf_counter() - adapter_started
            write_json(folder / "normalized.json", snapshot)
            validation = validate(snapshot, manifest, digest(manifest_path), metadata["tree"])
            write_json(folder / "validation.json", validation)
            verify(manifest, root)
            measurement = {"extractor": extractor, "label": label, "folder": str(folder), "analysisStatus": snapshot["status"], "elapsedSeconds": record["elapsedSeconds"], "peakMemoryBytes": record["peakMemoryBytes"], "peakMemoryMethod": record["peakMemoryMethod"], "exitCode": record["exitCode"], "inputFileCount": len(manifest["files"]), "inputSourceCount": snapshot["analysisScope"]["sourceFileCount"], "symbolCount": len(raw.get("symbols", raw.get("nodes", []))), "rawRelationCount": len(raw.get("relations", raw.get("edges", []))), "normalizedNodeCount": len(snapshot["nodes"]), "normalizedEdgeCount": len(snapshot["edges"]), "cacheBefore": before, "cacheAfter": cache_state(root, extractor), "osPageCacheCleared": False,
                           "syntaxContextSeconds": syntax_seconds, "syntaxContextPeakMemoryBytes": syntax_peak, "normalizationSeconds": adapter_seconds, "normalizationPeakMemoryBytes": None, "normalizationMemoryReason": "N/A; adapter runs inside runner, not independently profiled"}
            measurement["layers"] = layer_metrics(raw, snapshot)
            write_json(folder / "measurement.json", measurement)
            runs.append(measurement)
    write_json(batch / "measurements.json", runs)
    with (batch / "measurements.csv").open("w", encoding="utf-8", newline="") as stream:
        fields = ["extractor", "label", "analysisStatus", "elapsedSeconds", "peakMemoryBytes", "exitCode", "inputFileCount", "inputSourceCount", "symbolCount", "rawRelationCount", "normalizedNodeCount", "normalizedEdgeCount", "syntaxContextSeconds", "syntaxContextPeakMemoryBytes", "normalizationSeconds"]
        writer = csv.DictWriter(stream, fieldnames=fields, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(runs)
    checked = check_outputs(source, runtime, evidence, selection=selection, batch=batch, runs=runs)
    process_rows = subprocess.check_output(["ps", "-eo", "pid,ppid,args"], text=True).splitlines()
    remaining = [row for row in process_rows if str(runtime / "bundle") in row or str(runtime / "tool/Roslyn/bin") in row]
    write_json(batch / "environment.json", {
        "platform": subprocess.check_output(["uname", "-a"], text=True).strip(),
        "selection": selection, "selectedExtractors": extractors,
        **({"dotnetState": os.environ["DOTNET_CLI_HOME"]} if "Roslyn" in extractors else {}),
        **(codegraph_adapter.bundle_hashes(runtime) if "CodeGraph" in extractors else {}),
        "telemetry": "selected tool environment recorded; no packet capture",
        "remainingOwnedAnalysisProcesses": remaining,
    })
    if failed_commands:
        error = ExecutionError(
            f"Selected analysis failed/notRun; see {batch}", "analysis_failed",
            command_record=failed_commands,
        )
        error.analysis_status = checked["analysisStatus"]
        error.batch = str(batch)
        raise error
    write_json(evidence / "latest-run.json", {
        "batch": str(batch), "runs": runs, "selection": selection, "selectedExtractors": extractors,
    })
    print(f"Evidence: {batch}", flush=True)
    return checked


def main():
    """Validate and lock direct calls, execute the chosen action and persist its outcome."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("measure", "check"))
    for name in ("source", "runtime", "evidence"):
        parser.add_argument("--" + name, required=True)
    parser.add_argument("--dotnet")
    parser.add_argument("--extractor", default="roslyn")
    args = parser.parse_args()
    source, runtime, evidence = map(pathlib.Path, (args.source, args.runtime, args.evidence))
    source = source.resolve()
    evidence = evidence_path(source, args.extractor, evidence.relative_to(source).as_posix())
    validate_runtime(source, runtime, evidence, args.extractor)
    with execution_locks(runtime, evidence):
        validate_runtime(source, runtime, evidence, args.extractor)
        if not (runtime / ".dawnholder-execution.json").is_file():
            raise ValueError("Owned runtime is not prepared; run the PowerShell prepare entry point")
        try:
            if args.action == "measure":
                details = measure(source, runtime, evidence, args.dotnet, args.extractor)
            else:
                details = check_outputs(source, runtime, evidence, verify_format=True, selection=args.extractor)
            record_result(evidence, args.action, args.extractor, "completed", "command_completed",
                          "Selected commands completed; analysisStatus/diagnostics retain partial results.", **details)
        except Exception as error:
            status = getattr(error, "status", "unavailable" if isinstance(error, FileNotFoundError) else "failed")
            repair = getattr(error, "repair", None) or (
                (CODEGRAPH_REPAIR + " ") if "CodeGraph" in selected_extractors(args.extractor) else ""
            ) + "Inspect command.json/stdout.txt/stderr.txt; correct the pinned inputs/SDK and rerun prepare then measure."
            result = record_result(
                evidence, args.action, args.extractor, status,
                getattr(error, "reason_code", "input_missing" if status == "unavailable" else "validation_failed"),
                str(error), repair, commandRecord=getattr(error, "command_record", None),
                analysisStatus=getattr(error, "analysis_status", None), batch=getattr(error, "batch", None),
            )
            print(f"ERROR: {error}\nStatus: {status}; evidence: {result['evidencePath']}\nRepair: {repair}\n"
                  f"Retry: {retry_command(args.action, args.extractor, evidence.relative_to(source).as_posix())}", file=sys.stderr)
            return 1
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exception:
        print(f"ERROR: {exception}\nRepair: use --extractor roslyn|codegraph|compare, a new unlinked "
              ".backups/ root and its owned runtime; run prepare before measure/check. No extraction completed.", file=sys.stderr)
        raise SystemExit(1)
