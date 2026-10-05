"""Bounded argv execution; each invocation owns exactly one Linux process group."""

import datetime
import json
import os
import pathlib
import signal
import subprocess
import time


ENVIRONMENT_KEYS = (
    "DOTNET_ROOT", "DOTNET_HOST_PATH", "DOTNET_CLI_HOME",
    "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH", "NUGET_PACKAGES", "NUGET_HTTP_CACHE_PATH",
    "NUGET_PLUGINS_CACHE_PATH", "NUGET_SCRATCH", "MSBUILDDISABLENODEREUSE",
    "DOTNET_GENERATE_ASPNET_CERTIFICATE", "DOTNET_CLI_USE_MSBUILD_SERVER",
    "TMPDIR", "GIT_OPTIONAL_LOCKS",
)


def utc_now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()


def write_json(path, value):
    pathlib.Path(path).write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def settle_group(process):
    """Never enumerate/kill another owner's dotnet or MSBuild processes."""
    try:
        os.killpg(process.pid, signal.SIGTERM)
    except ProcessLookupError:
        return "already_exited"
    try:
        process.wait(timeout=2)
    except subprocess.TimeoutExpired:
        pass
    # The leader can exit before a descendant; kill this original group as well.
    try:
        os.killpg(process.pid, signal.SIGKILL)
    except ProcessLookupError:
        pass
    process.wait()
    return "terminated_owned_group"


def run_process(command, cwd, folder, environment, timeout=600):
    folder = pathlib.Path(folder)
    folder.mkdir(parents=True, exist_ok=True)
    record = {
        "argv": list(map(str, command)), "cwd": str(cwd), "startedUtc": utc_now(),
        "timeoutSeconds": timeout,
        "environment": {key: environment.get(key) for key in ENVIRONMENT_KEYS},
        "exitCode": None, "reasonCode": None,
    }
    write_json(folder / "command.json", record)
    started = time.perf_counter()
    process = None
    cancelled = False
    try:
        with (folder / "stdout.txt").open("w", encoding="utf-8") as stdout, (folder / "stderr.txt").open("w", encoding="utf-8") as stderr:
            try:
                process = subprocess.Popen(record["argv"], cwd=cwd, env=environment, stdout=stdout, stderr=stderr, start_new_session=True)
                record["pid"] = process.pid
                try:
                    record["exitCode"] = process.wait(timeout=timeout)
                    if record["exitCode"] != 0:
                        record["reasonCode"] = "execution_failed"
                except subprocess.TimeoutExpired:
                    record.update(exitCode=124, reasonCode="timeout")
                except KeyboardInterrupt:
                    record.update(exitCode=130, reasonCode="cancelled")
                    cancelled = True
            except OSError as error:
                stderr.write(str(error) + "\n")
                record.update(exitCode=127, reasonCode="tool_unavailable")
            finally:
                if process is not None:
                    record["processSettlement"] = settle_group(process)
    finally:
        record["elapsedSeconds"] = time.perf_counter() - started
        record["endedUtc"] = utc_now()
        write_json(folder / "command.json", record)
    if cancelled:
        raise KeyboardInterrupt
    return record
