"""Compile supplied bytes without importing targets or writing bytecode."""

import ast
import base64
import hashlib
import json
import sys


def check_source(item):
    source = base64.b64decode(item["base64"], validate=True)
    digest = hashlib.sha256(source).hexdigest()
    if digest != item["sha256"]:
        raise ValueError(f"Python input hash mismatch: {item['path']}")
    result = {"path": item["path"], "sha256": digest, "diagnostics": []}
    try:
        tree = compile(source, item["path"], "exec", flags=ast.PyCF_ONLY_AST, dont_inherit=True)
        compile(tree, item["path"], "exec", dont_inherit=True)
    except (SyntaxError, ValueError, TypeError) as error:
        result["diagnostics"].append({
            "path": item["path"],
            "rule": "PythonCompile",
            "line": getattr(error, "lineno", None),
            "column": getattr(error, "offset", None),
            "message": str(error),
        })
    return result


def main():
    output = {"version": sys.version, "files": [], "errors": []}
    try:
        inputs = json.load(sys.stdin)
        output["files"] = [check_source(item) for item in inputs]
    except Exception as error:
        output["errors"].append(str(error))
    print(json.dumps(output, ensure_ascii=True))
    return int(bool(output["errors"] or any(item["diagnostics"] for item in output["files"])))


if __name__ == "__main__":
    sys.exit(main())
