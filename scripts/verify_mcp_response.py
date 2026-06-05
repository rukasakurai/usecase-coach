#!/usr/bin/env python3
"""Verify a deployed usecase-coach MCP tool response matches the repo's reference data.

Reads the raw Streamable HTTP (Server-Sent Events) response from a
``get_reference_usecases`` tool call, parses the MCP/JSON-RPC layers, and asserts
the returned records are well-formed and exactly match the source data files in
``data/reference-usecases`` (excluding ``schema.json``).
"""
import argparse
import json
import pathlib
import sys

REQUIRED_FIELDS = ("problem", "approach", "impact")


def fail(msg: str) -> None:
    print(f"::error::{msg}", file=sys.stderr)
    sys.exit(1)


def canonicalize(records: list[dict]) -> list[dict]:
    return sorted(
        ({field: r.get(field) for field in REQUIRED_FIELDS} for r in records),
        key=lambda r: r["problem"] or "",
    )


def parse_jsonrpc_from_sse(path: pathlib.Path, request_id: int) -> dict:
    """Return the JSON-RPC message matching request_id from an SSE stream."""
    message = None
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.startswith("data:"):
            continue
        try:
            obj = json.loads(line[len("data:"):].strip())
        except json.JSONDecodeError:
            continue
        if obj.get("id") == request_id:
            message = obj
    if message is None:
        fail("no JSON-RPC tool response found in the SSE stream")
    return message


def extract_records(message: dict) -> list[dict]:
    if "error" in message:
        fail(f"tool call returned a JSON-RPC error: {message['error']}")
    result = message.get("result") or {}
    if result.get("isError"):
        fail(f"tool call returned isError: {result}")
    text = next(
        (c.get("text") for c in result.get("content", []) if c.get("type") == "text"),
        None,
    )
    if not text:
        fail("tool response has no text content block")
    try:
        records = json.loads(text)
    except json.JSONDecodeError as exc:
        fail(f"tool response text is not valid JSON: {exc}")
    if not isinstance(records, list) or not records:
        fail("tool response is not a non-empty JSON array")
    return records


def load_expected(data_dir: pathlib.Path) -> list[dict]:
    records = []
    for path in sorted(data_dir.glob("*.json")):
        if path.name == "schema.json":
            continue
        records.append(json.loads(path.read_text(encoding="utf-8")))
    if not records:
        fail(f"no reference use cases found in {data_dir}")
    return records


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--response", required=True, help="File with the raw SSE tool-call response")
    parser.add_argument("--data-dir", required=True, help="Path to data/reference-usecases")
    parser.add_argument("--request-id", type=int, default=3, help="JSON-RPC id used for tools/call")
    args = parser.parse_args()

    message = parse_jsonrpc_from_sse(pathlib.Path(args.response), args.request_id)
    actual = extract_records(message)

    for record in actual:
        for field in REQUIRED_FIELDS:
            value = record.get(field)
            if not isinstance(value, str) or not value.strip():
                fail(f"record is missing a non-empty '{field}': {record}")

    expected = load_expected(pathlib.Path(args.data_dir))
    if canonicalize(actual) != canonicalize(expected):
        print("--- expected ---", file=sys.stderr)
        print(json.dumps(canonicalize(expected), indent=2), file=sys.stderr)
        print("--- actual ---", file=sys.stderr)
        print(json.dumps(canonicalize(actual), indent=2), file=sys.stderr)
        fail("MCP tool response does not match the repo's reference data")

    print(f"MCP smoke test passed: response matches {len(actual)} expected reference use case(s).")


if __name__ == "__main__":
    main()
