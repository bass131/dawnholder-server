"""Closed-domain scoring; this module alone reads frozen scope and truth."""
import csv
from inputs import write_json


def symbol_key(node, symbols):
    source = node.get("source") or {}
    path = source.get("path")
    documentation_id = node.get("documentationId")
    matches = []
    for key, symbol in symbols.items():
        if path != symbol["path"]:
            continue
        if documentation_id:
            if documentation_id == symbol["documentationId"]:
                matches.append(key)
        else:
            # Evaluation may join an exact declaration anchor to a scope symbol.
            # It never chooses an edge target by method name or by truth.
            qualified = symbol["documentationId"][2:].split("(", 1)[0]
            if node.get("qualifiedName") == qualified and source.get("line") == symbol["declarationLine"]:
                matches.append(key)
    return matches[0] if len(matches) == 1 else None


def metrics(tp, fp, fn):
    return {"tp": tp, "fp": fp, "fn": fn, "predicted": tp + fp, "expected": tp + fn,
            "precision": tp / (tp + fp) if tp + fp else None,
            "recall": tp / (tp + fn) if tp + fn else None}


def score(snapshot, scope, truth):
    if snapshot["commitSha"] != scope["sourceCommit"] or snapshot["commitSha"] != truth["sourceCommit"]:
        raise ValueError("Scoring SHA mismatch")
    candidates = set()
    for group in scope["groups"]:
        for source in group["sources"]:
            for target in group["targets"]:
                candidates.add((group["kind"], source, target))
    positives = {(p["kind"], p["source"], p["target"]) for p in truth["positives"]}
    if not positives.issubset(candidates):
        raise ValueError("Truth positive outside frozen scope")
    negatives = {(p["kind"], p["source"], p["target"]) for p in truth["negatives"]}
    if positives & negatives or candidates != positives | negatives:
        raise ValueError("Truth does not partition the closed domain")
    if snapshot["status"] in {"failed", "notRun"}:
        return {"status": "notScored", "analysisStatus": snapshot["status"], "reason": "Failed or unexecuted analysis is not a successful empty graph", "candidateCount": len(candidates), "positiveCount": len(positives), "negativeCount": len(negatives), "overall": None, "rows": []}
    node_keys = {n["id"]: symbol_key(n, scope["symbols"]) for n in snapshot["nodes"]}
    predictions = set()
    evidence_ids = {}
    outside, unresolved = 0, 0
    for edge in snapshot["edges"]:
        if edge["resolution"] != "resolved":
            unresolved += 1
            continue
        triple = (edge["kind"], node_keys.get(edge["sourceId"]), node_keys.get(edge["targetId"]))
        if triple not in candidates:
            outside += 1
            continue
        predictions.add(triple)
        evidence_ids.setdefault(triple, []).append(edge["id"])
    rows = []
    groups = {}
    for triple in sorted(candidates):
        kind, source, target = triple
        expected, observed = triple in positives, triple in predictions
        result = "TP" if expected and observed else "FP" if observed else "FN" if expected else "TN"
        row = {"kind": kind, "source": source, "target": target, "expected": expected, "observed": observed, "result": result,
               "sourceLayer": scope["symbols"][source]["layer"], "targetLayer": scope["symbols"][target]["layer"],
               "supported": snapshot.get("supports", {}).get(kind, False), "edgeIds": sorted(evidence_ids.get(triple, []))}
        rows.append(row)
        for category, key in (("kind", kind), ("layer", row["sourceLayer"]), ("kindLayer", kind + ":" + row["sourceLayer"]), ("layerPair", row["sourceLayer"] + "->" + row["targetLayer"])):
            counters = groups.setdefault((category, key), {"TP": 0, "FP": 0, "FN": 0, "TN": 0})
            counters[result] += 1
    by_group = [{"category": category, "key": key, **metrics(c["TP"], c["FP"], c["FN"]), "tn": c["TN"], "candidateCount": sum(c.values())} for (category, key), c in sorted(groups.items())]
    return {"status": "scored", "analysisStatus": snapshot["status"], "candidateCount": len(candidates), "positiveCount": len(positives), "negativeCount": len(negatives),
            "overall": metrics(len(predictions & positives), len(predictions - positives), len(positives - predictions)), "outsideScopeEdgeCount": outside,
            "unresolvedOrAmbiguousEdgeCount": unresolved, "groups": by_group, "rows": rows,
            "identityJoin": "Roslyn documentationId+exact path; CodeGraph exact path+declaration line+qualified name only. No name-only or truth-assisted matching."}


def save_score(path, result):
    write_json(path, result)
    rows = result["rows"]
    with path.with_suffix(".csv").open("w", encoding="utf-8", newline="") as stream:
        fieldnames = ["kind", "source", "target", "sourceLayer", "targetLayer", "expected", "observed", "result", "supported", "edgeIds"]
        writer = csv.DictWriter(stream, fieldnames=fieldnames)
        writer.writeheader()
        for row in rows:
            writer.writerow({**row, "edgeIds": "|".join(row["edgeIds"])})
