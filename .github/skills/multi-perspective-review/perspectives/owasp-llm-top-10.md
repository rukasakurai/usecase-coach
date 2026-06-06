# Perspective: OWASP Top 10 for LLM Applications (2025)

Source: OWASP GenAI Security Project, *Top 10 for LLM Applications — 2025 (v2.0.0)*,
https://genai.owasp.org/llm-top-10/.

Context: the system under review is an MCP server that returns curated JSON
records read from disk to LLM-based clients. It does not call an LLM itself, but
it is a **data/tool provider in an agentic pipeline**, so the items about tool
output, data exposure, and agency matter most. Review the changes against each
item; the most relevant for this role are listed first.

**LLM01 Prompt Injection (HIGH)** — Returned record text is an *indirect*
injection vector. Check: is content from untrusted/external sources? Are
text fields structurally separated (clearly-named data fields) rather than mixed
with instructions? Is there a provenance/review gate on what enters the data set?

**LLM05 Improper Output Handling (HIGH)** — Treat the tool output as untrusted by
downstream consumers. Check: JSON produced by a safe serializer (not hand-built
strings)? Records parsed into a typed model and re-serialized (not raw file bytes
passed through)? Egress validated against the schema?

**LLM02 Sensitive Information Disclosure (HIGH)** — Records are returned verbatim
to any caller. Check: field allowlist/projection vs. returning whole records?
Secret-scanning of the `data/` directory in CI? Authn/authz on the tool endpoint?

**LLM06 Excessive Agency (HIGH)** — The server defines the agent's capability
surface. Check: is every exposed tool strictly necessary (read-only here)? Any
write/delete/admin operation justified? Filesystem access read-only and scoped?
No hidden side effects in "read" handlers?

**LLM09 Misinformation (HIGH)** — Served records become downstream "ground truth".
Check: documented, auditable process (PR review/approval) for adding/editing
records? Integrity (checksum/schema) validation on load? Staleness/version
metadata? Human review of any LLM-generated records.

**LLM07 System Prompt Leakage (MODERATE)** — Check: error messages free of
absolute paths, internal hostnames, stack traces, config values? Tool/schema
descriptions free of internal architecture or secrets?

**LLM03 Supply Chain (MODERATE)** — Check: dependencies pinned and audited
(Dependabot/`*-audit`)? Base image pinned by digest? Data-import pipeline (if any)
integrity-checked?

**LLM10 Unbounded Consumption (MODERATE)** — The server is a dependency of LLM
clients. Check: rate limiting? Bounded input/query params? Pagination / max
result size? Timeouts on I/O?

**LLM04 Data and Model Poisoning (LOWER)** — No training here, but check the
`data/` directory is read-only at runtime and mutation requires a reviewed,
authenticated path (overlaps LLM01/LLM09).

**LLM08 Vector and Embedding Weaknesses (N/A now)** — No vector/RAG layer today.
If a PR introduces embeddings/RAG, require a dedicated review (access partitioning,
embedding inversion, knowledge-base poisoning).

Do not invent issues; note items that are not applicable and why.
