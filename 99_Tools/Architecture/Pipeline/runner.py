"""Owned WSL comparison pipeline; extraction precedes separate scoring."""
import argparse
import csv
import datetime
import hashlib
import json
import os
import pathlib
import subprocess
import time
from collections import Counter

import codegraph_adapter
from execution import run_process, utc_now
from inputs import read_json, write_json, digest, verify, relative_path
# Keep dump_codegraph importable here as well as from normalization.
from normalization import normalize, dump_codegraph
from snapshot import validate, layer


def require_success(record, folder):
    if record["exitCode"] != 0:
        raise RuntimeError(f"Command failed; see {folder}/command.json and stdout/stderr")


def cache_state(root, extractor):
    if extractor == "Roslyn":
        return {"persistentAnalysisCache": False, "process": "fresh", "workspacePrerequisites": "restored projects/SDK; compiler build hosts may share OS page cache"}
    return codegraph_adapter.cache_state(root)


def freeze_check(source, evidence):
    metadata = read_json(evidence / "source-git.json")
    for item in metadata["frozenFiles"]:
        if digest(source / item["path"]) != item["sha256"]:
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


def check_outputs(source, runtime, evidence, verify_format=False):
    manifest_path = runtime / "manifest.json"
    manifest = read_json(manifest_path)
    metadata = freeze_check(source, evidence)
    latest = read_json(evidence / "latest-run.json")
    checks = []
    for run in latest["runs"]:
        folder = pathlib.Path(run["folder"])
        checks.append({"run": run["label"], "extractor": run["extractor"], **validate(read_json(folder / "normalized.json"), manifest, digest(manifest_path), metadata["tree"])})
        # Recalculate scores using the public scoring CLI, after snapshots exist.
        settings = read_json(runtime / "tool/comparison-settings.json")
        goal = source / relative_path(settings["goalPath"])
        command = ["python3", str(runtime / "tool/Pipeline/cli.py"), "score", "--snapshot", str(folder / "normalized.json"), "--manifest", str(manifest_path), "--scope", str(goal / "evaluation-scope.json"), "--truth", str(goal / "truth.json"), "--out", str(folder / "score.json")]
        record = run_process(command, runtime, folder / "scoring")
        require_success(record, folder / "scoring")
    write_json(evidence / "self-check.json", {"checks": checks, "checkedUtc": utc_now(), "independentVerification": False})
    if verify_format:
        dotnet = os.environ["DOTNET_HOST_PATH"]
        version = run_process([dotnet, "--version"], runtime, evidence / "format/sdk-version")
        require_success(version, evidence / "format/sdk-version")
        if (evidence / "format/sdk-version/stdout.txt").read_text().strip() != "10.0.301":
            raise ValueError("SDK pin mismatch")
        command = [dotnet, "format", "whitespace", runtime / "tool/Roslyn/Architecture.Roslyn.csproj", "--no-restore", "--verify-no-changes", "--verbosity", "diagnostic"]
        record = run_process(command, runtime, evidence / "format/whitespace")
        require_success(record, evidence / "format/whitespace")


def measure(source, runtime, evidence, dotnet):
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")
    batch = evidence / "runs" / stamp
    batch.mkdir(parents=True)
    manifest_path = runtime / "manifest.json"
    manifest = read_json(manifest_path)
    metadata = freeze_check(source, evidence)
    source_record = read_json(evidence / "input-copy.json")
    write_json(batch / "input-copy.json", source_record)
    write_json(batch / "source-git.json", metadata)
    codegraph_adapter.inspect_bundle(runtime, evidence)
    # Retain CodeGraph help/version before SDK checks and either extractor run.
    commands = (*codegraph_adapter.preflight_commands(runtime), ("sdk-version", [dotnet, "--version"]))
    for label, command in commands:
        record = run_process(command, runtime, batch / label)
        require_success(record, batch / label)
    if (batch / "sdk-version/stdout.txt").read_text().strip() != "10.0.301":
        raise ValueError("SDK pin mismatch; no fallback")
    codegraph_adapter.check_version(batch)
    tool_project = runtime / "tool/Roslyn/Architecture.Roslyn.csproj"
    for label, command, cwd in (("tool-restore", [dotnet, "restore", tool_project, "--nologo"], runtime),
                                ("tool-build", [dotnet, "build", tool_project, "--no-restore", "--nologo", "-p:UseSharedCompilation=false"], runtime),
                                ("server-restore", [dotnet, "restore", "02_Server/GameServer/GameServer.csproj", "--nologo"], runtime / "roslyn-input"),
                                ("clientnet-restore", [dotnet, "restore", "04_ClientNet/Dawnholder.Client.Net.csproj", "--nologo"], runtime / "roslyn-input")):
        record = run_process(command, cwd, batch / label)
        require_success(record, batch / label)
    config = {"schemaVersion": 1, "sdk": "10.0.301", "configuration": "Debug", "codegraphVersion": "1.6.1", "codegraphMode": "init then full index warm; bundled node; no daemon", "unityMode": "supplied Managed DLLs; C#9 explicit editor/windows symbols; asmdef approximation", "adapterVersion": "1", "sourceCommit": manifest["sourceCommit"], "manifestHash": digest(manifest_path), "implementationHead": metadata["implementationHead"],
              "implementationFiles": [{"path": str(p.relative_to(runtime / "tool")), "sha256": digest(p)} for p in sorted((runtime / "tool").rglob("*")) if p.is_file() and p.suffix in {".py", ".cs", ".csproj", ".sh", ".props", ".cjs"} and "obj" not in p.parts and "bin" not in p.parts],
              "environment": {name: os.environ.get(name) for name in ("DO_NOT_TRACK", "CODEGRAPH_TELEMETRY", "CODEGRAPH_NO_UPDATE_CHECK", "CODEGRAPH_NO_DAEMON")}}
    write_json(batch / "config.json", config)
    codegraph_adapter.archive_cache(runtime, stamp)
    runs = []
    for extractor in ("CodeGraph", "Roslyn"):
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
            record = run_process(command, root, folder / "analysis")
            syntax_seconds = None
            syntax_peak = None
            if record["exitCode"] == 0:
                if extractor == "CodeGraph":
                    raw, syntax_seconds, syntax_peak = codegraph_adapter.read_analysis(
                        runtime, root, manifest_path, folder,
                    )
                else:
                    raw = read_json(folder / "raw.json")
            else:
                raw = {"rawVersion": 1, "extractor": extractor, "version": "1.6.1" if extractor == "CodeGraph" else "SDK10.0.301", "status": "failed", "diagnostics": [{"kind": "execution", "exitCode": record["exitCode"], "commandRecord": str(folder / "analysis/command.json")} ]}
                write_json(folder / "raw.json", raw)
            raw["analysisInput"] = {"sourceCommit": manifest["sourceCommit"], "manifestHash": digest(manifest_path)}
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
            write_json(evidence / "latest-run.json", {"batch": str(batch), "runs": runs})
    write_json(batch / "measurements.json", runs)
    with (batch / "measurements.csv").open("w", encoding="utf-8", newline="") as stream:
        fields = ["extractor", "label", "analysisStatus", "elapsedSeconds", "peakMemoryBytes", "exitCode", "inputFileCount", "inputSourceCount", "symbolCount", "rawRelationCount", "normalizedNodeCount", "normalizedEdgeCount", "syntaxContextSeconds", "syntaxContextPeakMemoryBytes", "normalizationSeconds"]
        writer = csv.DictWriter(stream, fieldnames=fields, extrasaction="ignore")
        writer.writeheader()
        writer.writerows(runs)
    check_outputs(source, runtime, evidence)
    process_rows = subprocess.check_output(["ps", "-eo", "pid,ppid,args"], text=True).splitlines()
    remaining = [row for row in process_rows if str(runtime / "bundle") in row or str(runtime / "tool/Roslyn/bin") in row]
    write_json(batch / "environment.json", {
        "platform": subprocess.check_output(["uname", "-a"], text=True).strip(),
        "dotnetState": os.environ["DOTNET_CLI_HOME"],
        **codegraph_adapter.bundle_hashes(runtime),
        "telemetry": "disabled environment and installed source inspected; no packet capture",
        "remainingOwnedAnalysisProcesses": remaining,
    })
    print(f"Evidence: {batch}", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("measure", "check"))
    for name in ("source", "runtime", "evidence", "dotnet"):
        parser.add_argument("--" + name, required=True)
    args = parser.parse_args()
    source, runtime, evidence = map(pathlib.Path, (args.source, args.runtime, args.evidence))
    if args.action == "measure":
        measure(source, runtime, evidence, args.dotnet)
    else:
        check_outputs(source, runtime, evidence, verify_format=True)


if __name__ == "__main__":
    try:
        main()
    except Exception as exception:
        import sys
        print(f"ERROR: {exception}", file=sys.stderr)
        raise SystemExit(1)
