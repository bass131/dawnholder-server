"""Check the approved venv distribution versions without importing inspected SQL."""

import importlib.metadata
import json
import sys


def main():
    result = {"runtime": sys.version, "dependencies": [], "errors": []}
    try:
        expected = json.load(sys.stdin)
        for dependency in expected["dependencies"]:
            actual = importlib.metadata.version(dependency["name"])
            result["dependencies"].append({"name": dependency["name"], "version": actual})
            if actual != dependency["version"]:
                raise ValueError(f"Dependency version mismatch: {dependency['name']} {actual}")
    except Exception as error:
        result["errors"].append(str(error))
    print(json.dumps(result))
    return int(bool(result["errors"]))


if __name__ == "__main__":
    sys.exit(main())
