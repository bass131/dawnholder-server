"""Replay of a recorded comparison batch from local evidence.

By default the batch is the one latest-run.json names under the tool's configured evidencePath,
and the code it recorded as executed must equal the current repository tool.

ARCHITECTURE_EVIDENCE_BATCH replays another batch (an absolute `<root>/runs/<stamp>` folder);
its evidence root is the folder holding that runs/. A replayed batch is compared with the bytes
that ran then, named explicitly by ARCHITECTURE_EVIDENCE_TOOL_COMMIT (raw Git blobs of
99_Tools/Architecture at that commit, without checkout conversion) and/or
ARCHITECTURE_EVIDENCE_TOOL_ROOT (a preserved copy of the executed tool folder). The batch's own
implementationHead is not trusted for this, because the tool may have been uncommitted when it
ran; without either input the replay fails instead of comparing the record with itself.

Evidence is Git-ignored local data, so every replay test here is skipped with the reason when
it is absent. Scores are recounted from raw without the product scoring module."""
import datetime
import hashlib
import json
import os
import pathlib
import subprocess
import tempfile
import unittest

from support import mini_inputs

TOOL_ROOT = mini_inputs.REPOSITORY_ROOT / "99_Tools" / "Architecture"
EXECUTED_SUFFIXES = {".py", ".cs", ".csproj", ".sh", ".props", ".cjs"}
GENERATED_FOLDERS = {"bin", "obj", "node_modules", "__pycache__", ".npm-cache"}
CODEGRAPH_ENVIRONMENT = {"DO_NOT_TRACK": "1", "CODEGRAPH_TELEMETRY": "0", "CODEGRAPH_NO_UPDATE_CHECK": "1", "CODEGRAPH_NO_DAEMON": "1"}
CODEGRAPH_TYPE_KINDS = {"class", "struct", "interface", "enum"}
CODEGRAPH_METHOD_KINDS = {"method", "function"}


def _read(path):
    return json.loads(pathlib.Path(path).read_text(encoding="utf-8-sig"))


def _sha256(path):
    return hashlib.sha256(pathlib.Path(path).read_bytes()).hexdigest()


def _utc(text):
    return datetime.datetime.fromisoformat(text.replace("Z", "+00:00"))


def _locate():
    settings = _read(TOOL_ROOT / "comparison-settings.json")
    override = os.environ.get("ARCHITECTURE_EVIDENCE_BATCH")
    if override:
        batch = pathlib.Path(override)
        # First-analysis records and every run of a batch live in the root above its runs/.
        evidence = batch.parent.parent
    else:
        evidence = mini_inputs.REPOSITORY_ROOT / settings["evidencePath"]
        latest = evidence / "latest-run.json"
        if not latest.exists():
            return None
        batch = pathlib.Path(_read(latest)["batch"])
    goal = mini_inputs.REPOSITORY_ROOT / settings["goalPath"]
    return {
        "batch": batch,
        "evidence": evidence,
        "replayed": bool(override),
        "goal": goal,
        "manifest": goal / "input-manifest.json",
        "freeze": mini_inputs.REPOSITORY_ROOT / settings["freezeRecordPath"],
    }


LOCATION = _locate()
SKIP_REASON = "local comparison evidence is absent (Git-ignored); set ARCHITECTURE_EVIDENCE_BATCH to replay a batch"


def _is_executed(relative):
    path = pathlib.PurePosixPath(relative)
    return path.suffix in EXECUTED_SUFFIXES and not GENERATED_FOLDERS & set(path.parts)


def tool_files_in_folder(root):
    """{tool-relative path: SHA256} of the executed files under a tool folder."""
    root = pathlib.Path(root)
    return {
        path.relative_to(root).as_posix(): _sha256(path)
        for path in sorted(root.rglob("*"))
        if path.is_file() and _is_executed(path.relative_to(root).as_posix())
    }


def _git(repository, *arguments):
    """Read-only Git query; a Windows worktree pointer is read through Windows Git from WSL."""
    pointer = pathlib.Path(repository) / ".git"
    if pointer.is_file() and ":/" in pointer.read_text(encoding="utf-8").replace("\\", "/"):
        windows_root = subprocess.run(["wslpath", "-w", str(repository)], capture_output=True, text=True, check=True).stdout.strip()
        command = ["git.exe", "-C", windows_root, *arguments]
    else:
        command = ["git", "-C", str(repository), *arguments]
    return subprocess.run(command, capture_output=True, check=True).stdout


def tool_files_at_commit(commit, repository=mini_inputs.REPOSITORY_ROOT):
    """{tool-relative path: SHA256} of raw blobs (no checkout conversion) under 99_Tools/Architecture."""
    prefix = "99_Tools/Architecture/"
    files = {}
    for entry in _git(repository, "ls-tree", "-r", "-z", "--full-tree", commit, "--", prefix).split(b"\0"):
        if not entry:
            continue
        metadata, path = entry.split(b"\t", 1)
        _, kind, blob = metadata.split()
        relative = path.decode("utf-8")[len(prefix):]
        if kind == b"blob" and _is_executed(relative):
            files[relative] = hashlib.sha256(_git(repository, "cat-file", "blob", blob.decode())).hexdigest()
    return files


def tool_sources():
    """Named byte sources to compare with the batch's recorded executed code."""
    if not LOCATION["replayed"]:
        return {"current repository tool": tool_files_in_folder(TOOL_ROOT)}
    sources = {}
    commit = os.environ.get("ARCHITECTURE_EVIDENCE_TOOL_COMMIT")
    folder = os.environ.get("ARCHITECTURE_EVIDENCE_TOOL_ROOT")
    if commit:
        sources[f"Git {commit}"] = tool_files_at_commit(commit)
    if folder:
        sources[f"folder {folder}"] = tool_files_in_folder(folder) if pathlib.Path(folder).is_dir() else None
    return sources


def _reorder(value):
    if isinstance(value, dict):
        return {key: _reorder(value[key]) for key in reversed(list(value))}
    if isinstance(value, list):
        return [_reorder(item) for item in reversed(value)] if value and isinstance(value[0], dict) else [_reorder(item) for item in value]
    return value


def _member_name(documentation_id):
    return documentation_id[2:].split("(", 1)[0].rsplit(".", 1)[-1]


def _independent_predictions(raw, scope):
    """Closed-domain triples read directly from raw identities, not from evaluation.py."""
    if raw["extractor"] == "Roslyn":
        by_identity = {(symbol["documentationId"], symbol["path"]): key for key, symbol in scope["symbols"].items()}
        keys = {}
        for symbol in raw["symbols"]:
            source = symbol.get("source") or {}
            keys[symbol["key"]] = by_identity.get((symbol.get("documentationId"), source.get("path")))
        return {(relation["kind"], keys.get(relation["sourceKey"]), keys.get(relation["targetKey"])) for relation in raw["relations"] if relation["resolution"] == "resolved" and relation["kind"] != "contains"}
    by_anchor = {(symbol["path"], symbol["declarationLine"], _member_name(symbol["documentationId"]), symbol["kind"]): key for key, symbol in scope["symbols"].items()}
    keys = {}
    for node in raw["nodes"]:
        kind = "type" if node["kind"] in CODEGRAPH_TYPE_KINDS else "method" if node["kind"] in CODEGRAPH_METHOD_KINDS else None
        keys[node["id"]] = by_anchor.get((node["file_path"], node["start_line"], node["name"], kind)) if kind else None
    kinds = {node["id"]: node["kind"] for node in raw["nodes"]}
    triples = set()
    for edge in raw["edges"]:
        metadata = json.loads(edge["metadata"]) if edge.get("metadata") else {}
        if edge["kind"] == "calls" and not metadata.get("synthesizedBy"):
            triples.add(("calls", keys.get(edge["source"]), keys.get(edge["target"])))
        elif edge["kind"] == "implements":
            triples.add(("implements", keys.get(edge["source"]), keys.get(edge["target"])))
        elif edge["kind"] == "instantiates" and kinds.get(edge["source"]) in CODEGRAPH_METHOD_KINDS and kinds.get(edge["target"]) in CODEGRAPH_TYPE_KINDS:
            triples.add(("usesType", keys.get(edge["source"]), keys.get(edge["target"])))
    return triples


@unittest.skipIf(LOCATION is None, SKIP_REASON)
class FinalBatchReplayTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.batch = LOCATION["batch"]
        cls.manifest = LOCATION["manifest"]
        cls.scope = _read(LOCATION["goal"] / "evaluation-scope.json")
        cls.truth = _read(LOCATION["goal"] / "truth.json")
        cls.runs = _read(cls.batch / "measurements.json")
        cls._temporary = tempfile.TemporaryDirectory(prefix="architecture-replay-")
        cls.out = pathlib.Path(cls._temporary.name)

    @classmethod
    def tearDownClass(cls):
        cls._temporary.cleanup()

    def run_folder(self, run):
        return pathlib.Path(run["folder"])

    def normalize(self, raw_path, run, name):
        out = self.out / f"{name}.json"
        folder = self.run_folder(run)
        result = mini_inputs.run_cli("normalize", "--raw", raw_path, "--config", folder / "extractor-config.json", "--manifest", self.manifest, "--out", out)
        self.assertEqual(0, result.returncode, result.stderr)
        return out

    def test_batch_has_cold_and_three_warm_runs_per_extractor(self):
        labels = sorted((run["extractor"], run["label"]) for run in self.runs)
        self.assertEqual(sorted((extractor, label) for extractor in ("CodeGraph", "Roslyn") for label in ("cold", "warm1", "warm2", "warm3")), labels)

    def test_frozen_answers_precede_every_analysis(self):
        freeze = _read(LOCATION["freeze"])
        for item in freeze["files"]:
            self.assertEqual(item["sha256"], _sha256(mini_inputs.REPOSITORY_ROOT / item["path"]), item["path"])
        committed = _utc(freeze["committedAt"])
        evidence = LOCATION["evidence"]
        first_records = {name: _read(evidence / f"first-analysis-{name}.json") for name in ("codegraph", "roslyn")}
        for name, record in first_records.items():
            with self.subTest(name):
                recorded = _utc(record["recordedBeforeExecutionUtc"])
                self.assertEqual(committed, _utc(record["freezeCommittedUtc"]))
                self.assertLess(committed, recorded)
                command = _read(record["commandRecord"])
                self.assertEqual(record["argv"], command["argv"])
                self.assertGreaterEqual(_utc(command["startedUtc"]), recorded)
        earliest = min(_utc(record["recordedBeforeExecutionUtc"]) for record in first_records.values())
        analysis_starts = [_utc(_read(path)["startedUtc"]) for path in (evidence / "runs").glob("*/*/*/analysis/command.json")]
        self.assertTrue(analysis_starts)
        self.assertGreaterEqual(min(analysis_starts), earliest, "an analysis ran before the first-analysis record")

    def test_executed_code_matches_repository_tool(self):
        """Recorded executed code equals the current tool, or for a replay the named bytes of then."""
        config = _read(self.batch / "config.json")
        recorded = {item["path"]: item["sha256"] for item in config["implementationFiles"]}
        sources = tool_sources()
        self.assertTrue(sources, "replaying a selected batch needs ARCHITECTURE_EVIDENCE_TOOL_COMMIT or "
                                 "ARCHITECTURE_EVIDENCE_TOOL_ROOT naming the tool bytes that ran then; "
                                 "the batch's own implementationHead and hashes are not an independent source")
        for name, files in sources.items():
            with self.subTest(name):
                self.assertIsNotNone(files, f"tool folder is missing: {name}")
                self.assertEqual(files, recorded)
        self.assertEqual([], [path for path in recorded if path.endswith(".py") and "/" not in path], "stale root modules must not be part of the executed tool")
        self.assertEqual(config["manifestHash"], _sha256(self.manifest))

    def test_normalized_snapshots_replay_byte_for_byte(self):
        for run in self.runs:
            with self.subTest(f"{run['extractor']} {run['label']}"):
                folder = self.run_folder(run)
                out = self.normalize(folder / "raw.json", run, f"replay-{run['extractor']}-{run['label']}")
                self.assertEqual((folder / "normalized.json").read_bytes(), out.read_bytes())

    def test_real_raw_key_and_list_order_do_not_change_snapshot(self):
        for run in (run for run in self.runs if run["label"] == "cold"):
            with self.subTest(run["extractor"]):
                folder = self.run_folder(run)
                reordered = self.out / f"reordered-raw-{run['extractor']}.json"
                reordered.write_text(json.dumps(_reorder(_read(folder / "raw.json"))), encoding="utf-8")
                out = self.normalize(reordered, run, f"reordered-{run['extractor']}")
                self.assertEqual((folder / "normalized.json").read_bytes(), out.read_bytes())

    def test_scores_replay_and_match_independent_recount(self):
        candidates = {(group["kind"], source, target) for group in self.scope["groups"] for source in group["sources"] for target in group["targets"]}
        positives = {(row["kind"], row["source"], row["target"]) for row in self.truth["positives"]}
        for run in self.runs:
            with self.subTest(f"{run['extractor']} {run['label']}"):
                folder = self.run_folder(run)
                out = self.out / f"score-{run['extractor']}-{run['label']}.json"
                result = mini_inputs.run_cli("score", "--snapshot", folder / "normalized.json", "--manifest", self.manifest, "--scope", LOCATION["goal"] / "evaluation-scope.json", "--truth", LOCATION["goal"] / "truth.json", "--out", out)
                self.assertEqual(0, result.returncode, result.stderr)
                replayed, stored = _read(out), _read(folder / "score.json")
                self.assertEqual(stored, replayed)
                predicted = _independent_predictions(_read(folder / "raw.json"), self.scope) & candidates
                expected = {"tp": len(predicted & positives), "fp": len(predicted - positives), "fn": len(positives - predicted)}
                self.assertEqual(expected, {name: stored["overall"][name] for name in expected})
                self.assertEqual({row_key for row_key in candidates}, {(row["kind"], row["source"], row["target"]) for row in stored["rows"]})
                for row in stored["rows"]:
                    triple = (row["kind"], row["source"], row["target"])
                    self.assertEqual(triple in predicted, row["observed"], triple)

    def test_unresolved_occurrences_are_all_carried_into_scores(self):
        for run in (run for run in self.runs if run["label"] == "cold"):
            with self.subTest(run["extractor"]):
                folder = self.run_folder(run)
                raw = _read(folder / "raw.json")
                if raw["extractor"] == "Roslyn":
                    open_count = sum(1 for relation in raw["relations"] if relation["resolution"] in {"unresolved", "ambiguous"})
                else:
                    open_count = sum(1 for item in raw["unresolved_refs"] if item["reference_kind"] in {"calls", "implements", "contains", "usesType"})
                self.assertEqual(open_count, _read(folder / "score.json")["unresolvedOrAmbiguousEdgeCount"])

    def test_measurements_trace_to_command_records(self):
        for run in self.runs:
            with self.subTest(f"{run['extractor']} {run['label']}"):
                folder = self.run_folder(run)
                self.assertEqual(run, _read(folder / "measurement.json"))
                command = _read(folder / "analysis" / "command.json")
                self.assertEqual((0, command["elapsedSeconds"], command["peakMemoryBytes"]), (run["exitCode"], run["elapsedSeconds"], run["peakMemoryBytes"]))
                self.assertFalse(run["osPageCacheCleared"])
                if run["extractor"] == "CodeGraph":
                    for step in ("analysis", "syntax-context"):
                        environment = _read(folder / step / "command.json")["environment"]
                        self.assertEqual(CODEGRAPH_ENVIRONMENT, {name: environment.get(name) for name in CODEGRAPH_ENVIRONMENT})
                    self.assertEqual(run["label"] != "cold", run["cacheBefore"]["persistentAnalysisCache"], "cold means no CodeGraph analysis DB before the run")
                    self.assertEqual(_read(folder / "syntax-context" / "command.json")["elapsedSeconds"], run["syntaxContextSeconds"])
                else:
                    self.assertFalse(run["cacheBefore"]["persistentAnalysisCache"])

    def test_dotnet_steps_use_isolated_state(self):
        for step in ("sdk-version", "tool-restore", "tool-build", "server-restore", "clientnet-restore"):
            with self.subTest(step):
                command = _read(self.batch / step / "command.json")
                environment = command["environment"]
                self.assertEqual(0, command["exitCode"])
                self.assertEqual(("0", "false", "1"), (environment["DOTNET_ADD_GLOBAL_TOOLS_TO_PATH"], environment["DOTNET_GENERATE_ASPNET_CERTIFICATE"], environment["DOTNET_CLI_TELEMETRY_OPTOUT"]))
                state = pathlib.PurePosixPath(environment["DOTNET_CLI_HOME"]).parent
                self.assertTrue(state.name.startswith(".dotnet-state-"))
                for name in ("NUGET_PACKAGES", "NUGET_HTTP_CACHE_PATH", "NUGET_PLUGINS_CACHE_PATH", "NUGET_SCRATCH"):
                    self.assertTrue(environment[name].startswith(str(state) + "/"), name)
        self.assertEqual("10.0.301", (self.batch / "sdk-version" / "stdout.txt").read_text().strip())

    def test_unity_semantics_are_partial_and_never_read_client_library_or_obj(self):
        manifest = _read(self.manifest)
        forbidden = ("03_Client/Library/", "03_Client/obj/", "03_Client/Temp/")
        self.assertEqual([], [item["path"] for item in manifest["files"] if item["path"].startswith(forbidden)])
        cold = next(self.run_folder(run) for run in self.runs if run["extractor"] == "Roslyn" and run["label"] == "cold")
        raw = _read(cold / "raw.json")
        references = [reference["path"] for compilation in raw["compilations"] for reference in compilation["references"] if reference["kind"] == "file"]
        self.assertTrue(references)
        self.assertEqual([], [path for path in references if any(part in path for part in forbidden)])
        unity = next(compilation for compilation in raw["compilations"] if compilation["name"] == "Unity asmdef approximation")
        external = [reference["path"] for reference in unity["references"] if reference["kind"] == "file" and "/.architecture-references/" in reference["path"]]
        self.assertEqual(len(manifest["externalReferences"]), len(external))
        unity_errors = [item for item in raw["diagnostics"] if item.get("project") == "Unity asmdef approximation" and item.get("severity") == "Error"]
        self.assertTrue(unity_errors)
        self.assertEqual("partial", _read(cold / "normalized.json")["status"])



class ToolByteSourceTests(unittest.TestCase):
    """The replay byte sources read real bytes: raw Git blobs and a folder, never the record."""

    def setUp(self):
        self._temporary = tempfile.TemporaryDirectory(prefix="architecture-tool-source-")
        self.addCleanup(self._temporary.cleanup)
        self.repository = pathlib.Path(self._temporary.name)
        self.tool = self.repository / "99_Tools/Architecture"
        self.files = {
            "Pipeline/runner.py": b"print('ran then')\n",
            "run-wsl.sh": b"#!/usr/bin/env bash\n",
            "Roslyn/Program.cs": b"class P {}\n",
            "Roslyn/bin/Debug/Generated.cs": b"// generated\n",
            "comparison-settings.json": b"{}\n",
            "CodeGraph/node_modules/x/index.cjs": b"// dependency\n",
        }
        for relative, data in self.files.items():
            (self.tool / relative).parent.mkdir(parents=True, exist_ok=True)
            (self.tool / relative).write_bytes(data)
        # A checkout of these files would turn LF into CRLF; the replay must use the raw blob.
        (self.repository / ".gitattributes").write_bytes(b"*.py text eol=crlf\n")
        for command in (["init", "--quiet"], ["add", "-f", "."],
                        ["-c", "user.name=t", "-c", "user.email=t@example.invalid", "commit", "--quiet", "-m", "tool"]):
            subprocess.run(["git", "-C", str(self.repository), *command], check=True, capture_output=True)

    def expected(self, names):
        return {name: hashlib.sha256(self.files[name]).hexdigest() for name in names}

    def test_commit_source_is_raw_blob_bytes_of_executed_files_only(self):
        executed = ["Pipeline/runner.py", "Roslyn/Program.cs", "run-wsl.sh"]
        self.assertEqual(self.expected(executed), tool_files_at_commit("HEAD", self.repository))

    def test_folder_source_reads_current_bytes_and_sees_a_changed_file(self):
        executed = ["Pipeline/runner.py", "Roslyn/Program.cs", "run-wsl.sh"]
        committed = tool_files_at_commit("HEAD", self.repository)
        (self.tool / "Pipeline/runner.py").write_bytes(b"print('changed later')\n")
        folder = tool_files_in_folder(self.tool)
        self.assertEqual(sorted(executed), sorted(folder))
        self.assertNotEqual(committed["Pipeline/runner.py"], folder["Pipeline/runner.py"])
        self.assertEqual(hashlib.sha256(b"print('changed later')\n").hexdigest(), folder["Pipeline/runner.py"])

    def test_unknown_commit_is_an_error_not_an_empty_match(self):
        with self.assertRaises(subprocess.CalledProcessError):
            tool_files_at_commit("0" * 40, self.repository)


if __name__ == "__main__":
    unittest.main()
