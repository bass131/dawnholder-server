"""Sequential bounded processes with durable argv, logs and GNU time metrics."""
import datetime
import os
import pathlib
import re
import signal
import subprocess
import time
from inputs import write_json

ENVIRONMENT_KEYS = ("DOTNET_ROOT", "DOTNET_HOST_PATH", "DOTNET_CLI_HOME", "DOTNET_ADD_GLOBAL_TOOLS_TO_PATH", "DOTNET_GENERATE_ASPNET_CERTIFICATE", "DOTNET_CLI_TELEMETRY_OPTOUT", "NUGET_PACKAGES", "NUGET_HTTP_CACHE_PATH", "NUGET_PLUGINS_CACHE_PATH", "NUGET_SCRATCH", "MSBUILDDISABLENODEREUSE", "DO_NOT_TRACK", "CODEGRAPH_TELEMETRY", "CODEGRAPH_NO_UPDATE_CHECK", "CODEGRAPH_NO_DAEMON")


def utc_now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()


def run_process(command, cwd, folder, timeout=600):
    folder = pathlib.Path(folder)
    folder.mkdir(parents=True, exist_ok=True)
    record = {"argv": list(map(str, command)), "cwd": str(cwd), "startedUtc": utc_now(),
              "environment": {name: os.environ.get(name) for name in ENVIRONMENT_KEYS}, "timeoutSeconds": timeout}
    time_binary = pathlib.Path("/usr/bin/time")
    invoked = [str(time_binary), "-v", "-o", str(folder / "time.txt"), *record["argv"]] if time_binary.exists() else record["argv"]
    record["invokedArgv"] = invoked
    write_json(folder / "command.json", record)
    started = time.perf_counter()
    with (folder / "stdout.txt").open("w") as stdout, (folder / "stderr.txt").open("w") as stderr:
        process = subprocess.Popen(invoked, cwd=cwd, stdout=stdout, stderr=stderr, start_new_session=True)
        try:
            record["exitCode"] = process.wait(timeout=timeout)
        except subprocess.TimeoutExpired:
            os.killpg(process.pid, signal.SIGTERM)
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                os.killpg(process.pid, signal.SIGKILL)
                process.wait()
            record["exitCode"] = 124
            record["failure"] = "Timeout; terminated only this command process group"
    record["elapsedSeconds"] = time.perf_counter() - started
    record["endedUtc"] = utc_now()
    peak = None
    if (folder / "time.txt").exists():
        match = re.search(r"Maximum resident set size \(kbytes\):\s*(\d+)", (folder / "time.txt").read_text())
        if match:
            peak = int(match.group(1)) * 1024
    record["peakMemoryBytes"] = peak
    record["peakMemoryMethod"] = "GNU time maximum RSS as reported for the command and waited-for children; not a sampled simultaneous sum" if peak is not None else "N/A; GNU time unavailable or no metrics"
    write_json(folder / "command.json", record)
    print(f"{folder.name}: exit {record['exitCode']}, {record['elapsedSeconds']:.3f}s", flush=True)
    return record
