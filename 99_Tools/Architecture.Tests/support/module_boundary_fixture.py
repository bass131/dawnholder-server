"""Hand-written requirement fixtures; never generated from checker policy/results."""

import json
import os
import pathlib
import subprocess


PROJECT = "02_Server/GameServer/GameServer.csproj"
NS = "Dawnholder.Server.GameServer"


def write_fixture(root, mode="matrix"):
    root = pathlib.Path(root)
    files = {
        "global.json": json.dumps({"sdk": {"version": "10.0.301", "rollForward": "disable"}}),
        "Directory.Build.props": "<Project />\n",
        PROJECT: '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>\n',
        "02_Server/GameServer/Maps/GameMap.cs": f"namespace {NS}.Maps; public class GameMap {{ public void Tick() {{ }} }}\n",
        "02_Server/GameServer/Handlers/Dispatcher.cs": f"namespace {NS}.Handlers; public interface IPacketHandler {{ void Handle(); }} public static class HandlerRegistry {{ public static IPacketHandler? Find() => null; }} public class ConcreteHandler {{ public void Run() {{ }} }}\n",
        "02_Server/GameServer/Sessions/GameSession.cs": f"namespace {NS}.Sessions; public class GameSession {{ public void Submit() {{ }} public void Send() {{ }} }}\n",
    }
    if mode == "matrix":
        files.update({
            "02_Server/GameServer/Handlers/FieldDependency.cs": f"using MapAlias = {NS}.Maps.GameMap; namespace {NS}.Handlers; public class FieldDependency {{ public MapAlias? Map; }}\n",
            "02_Server/GameServer/Handlers/SignatureDependency.cs": f"namespace {NS}.Handlers; public class SignatureDependency {{ public global::{NS}.Maps.GameMap Echo(global::{NS}.Maps.GameMap value) => value; }}\n",
            "02_Server/GameServer/Handlers/CallDependency.cs": f"using {NS}.Maps; namespace {NS}.Handlers; public class CallDependency {{ public void Run(GameMap map) => map.Tick(); }}\n",
            "02_Server/GameServer/Maps/ReverseDependency.cs": f"namespace {NS}.Maps; public class ReverseDependency {{ public void Run(global::{NS}.Handlers.ConcreteHandler h) => h.Run(); }}\n",
            "02_Server/GameServer/Sessions/ConcreteDependency.cs": f"namespace {NS}.Sessions; public class ConcreteDependency {{ public global::{NS}.Handlers.ConcreteHandler? Handler; }}\n",
            "02_Server/GameServer/Sessions/DispatcherUse.cs": f"using {NS}.Handlers; namespace {NS}.Sessions; public class DispatcherUse {{ public IPacketHandler? Find() => HandlerRegistry.Find(); }}\n",
            "02_Server/GameServer/Handlers/SubmitUse.cs": f"namespace {NS}.Handlers; public class SubmitUse {{ public void Run(global::{NS}.Sessions.GameSession session) => session.Submit(); }}\n",
            "02_Server/GameServer/Maps/SendUse.cs": f"namespace {NS}.Maps; public class SendUse {{ public void Run(global::{NS}.Sessions.GameSession session) => session.Send(); }}\n",
            "02_Server/GameServer/Sessions/MapUse.cs": f"namespace {NS}.Sessions; public class MapUse {{ public void Run(global::{NS}.Maps.GameMap map) => map.Tick(); }}\n",
            "02_Server/GameServer/Handlers/TextOnly.cs": f'namespace {NS}.Handlers; public class TextOnly {{ /* {NS}.Maps.GameMap */ public string Text => "{NS}.Maps.GameMap"; }}\n',
        })
    elif mode == "inferred":
        files.update({
            "02_Server/GameServer/Sessions/ShapeProvider.cs": f"namespace {NS}.Sessions; public class ShapeProvider {{ public global::{NS}.Maps.GameMap[] Array() => []; public List<global::{NS}.Maps.GameMap> List() => []; }}\n",
            "02_Server/GameServer/Handlers/InferredArray.cs": f"namespace {NS}.Handlers; public class InferredArray {{ public void Run(global::{NS}.Sessions.ShapeProvider source) {{ var maps = source.Array(); }} }}\n",
            "02_Server/GameServer/Handlers/InferredGeneric.cs": f"namespace {NS}.Handlers; public class InferredGeneric {{ public void Run(global::{NS}.Sessions.ShapeProvider source) {{ var maps = source.List(); }} }}\n",
        })
    elif mode == "unresolved":
        files["02_Server/GameServer/Handlers/Broken.cs"] = "namespace Fixture; public class Broken { public MissingType? Missing; }\n"
    elif mode == "zero":
        files = {key: value for key, value in files.items() if not key.endswith(".cs")}
        files["02_Server/GameServer/Other.cs"] = "public class Other {}\n"
        for name in ("Handlers", "Maps", "Sessions"):
            (root / "02_Server/GameServer" / name).mkdir(parents=True, exist_ok=True)
    elif mode == "missing":
        files.pop("02_Server/GameServer/Maps/GameMap.cs")
    elif mode == "clean":
        pass
    else:
        raise ValueError(f"Unknown explicit fixture mode: {mode}")
    for relative, content in files.items():
        target = root / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(content, encoding="utf-8")
    return root


def write_git_fixture(root, owner_root):
    """Create history only in a new test fixture beneath the explicit owned work.

    Git identity, signing and hooks are process-local options; no operating
    checkout index, user configuration, remote or existing fixture is changed.
    """
    owner = pathlib.Path(owner_root).resolve(strict=True)
    root = pathlib.Path(root)
    if not root.is_absolute() or any(path.is_symlink() for path in (root, *root.parents)):
        raise ValueError("Git fixture must use a direct absolute path")
    root = root.resolve()
    if owner not in root.parents or root.exists():
        raise ValueError("Git fixture must be new and strictly below the owned work root")
    write_fixture(root, "clean")
    (root / ".gitignore").write_text(".backups/\n", encoding="utf-8")
    environment = dict(os.environ, GIT_OPTIONAL_LOCKS="0")
    prefix = [
        "git", "-c", "user.name=Module Boundary Fixture", "-c", "user.email=fixture@example.invalid",
        "-c", "core.hooksPath=/dev/null", "-c", "commit.gpgSign=false",
        "-c", "core.excludesFile=/dev/null", "-c", "init.defaultBranch=fixture",
    ]
    records = []

    def git(*arguments):
        command = [*prefix, *arguments]
        completed = subprocess.run(command, cwd=root, env=environment, capture_output=True, text=True, timeout=30)
        records.append({
            "argv": command, "cwd": str(root), "exitCode": completed.returncode,
            "stdout": completed.stdout, "stderr": completed.stderr,
        })
        (root.parent / "fixture-git-commands.json").write_text(json.dumps(records, indent=2) + "\n", encoding="utf-8")
        if completed.returncode:
            raise RuntimeError(f"Owned fixture Git failed: {completed.stderr}")
        return completed.stdout.strip()

    git("init", "--quiet")
    git("add", "--", ".")
    git("commit", "--quiet", "--no-gpg-sign", "--message", "Track clean module boundary fixture")
    revision = git("rev-parse", "HEAD")
    untracked = "02_Server/GameServer/Handlers/UntrackedDependency.cs"
    (root / untracked).write_text(
        f"namespace {NS}.Handlers; public class UntrackedDependency {{ public global::{NS}.Maps.GameMap? Map; }}\n",
        encoding="utf-8",
    )
    (root / "notes.md").write_text("Unrelated untracked fixture note.\n", encoding="utf-8")
    (root / ".backups").mkdir()
    (root / ".backups/ignored-evidence.txt").write_text("Ignored fixture evidence.\n", encoding="utf-8")
    return {"root": root, "head": revision, "untrackedPath": untracked}


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="Create a separately labelled warning demonstration fixture")
    parser.add_argument("root")
    options = parser.parse_args()
    root = pathlib.Path(options.root)
    if root.exists():
        parser.error("Fixture root must be new")
    write_fixture(root)
