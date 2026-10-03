"""Explicit artifact commands for normalization, validation, scoring and joins."""
import argparse
import sys
from pathlib import Path
from inputs import digest, read_json, write_json
from normalization import normalize
from snapshot import validate, join_code_reference
from evaluation import score, save_score


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    normalizer = commands.add_parser("normalize")
    normalizer.add_argument("--raw", required=True)
    normalizer.add_argument("--config", required=True)
    for name in ("validate", "score", "join"):
        commands.add_parser(name).add_argument("--snapshot", required=True)
    for command in commands.choices.values():
        command.add_argument("--manifest", required=True)
        command.add_argument("--out", required=True)
    commands.choices["validate"].add_argument("--git-metadata")
    commands.choices["score"].add_argument("--scope", required=True)
    commands.choices["score"].add_argument("--truth", required=True)
    commands.choices["join"].add_argument("--reference", required=True)
    commands.choices["join"].add_argument("--git-metadata", required=True)
    args = parser.parse_args()
    manifest = read_json(args.manifest)
    if args.command == "normalize":
        result = normalize(read_json(args.raw), manifest, digest(args.manifest), digest(args.config))
        validate(result, manifest, digest(args.manifest))
    else:
        snapshot = read_json(args.snapshot)
        validate(snapshot, manifest, digest(args.manifest))
        if args.command == "validate":
            git_paths = read_json(args.git_metadata)["tree"] if args.git_metadata else None
            result = validate(snapshot, manifest, digest(args.manifest), git_paths)
        elif args.command == "score":
            result = score(snapshot, read_json(args.scope), read_json(args.truth))
            save_score(Path(args.out), result)
            return
        else:
            result = join_code_reference(snapshot, read_json(args.reference), read_json(args.git_metadata)["tree"])
    write_json(args.out, result)


if __name__ == "__main__":
    try:
        main()
    except Exception as exception:
        print(f"ERROR: {exception}", file=sys.stderr)
        raise SystemExit(1)
