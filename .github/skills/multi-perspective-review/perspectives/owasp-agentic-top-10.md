# Perspective: OWASP Top 10 for Agentic Applications (2026, ASI01–ASI10)

Source: OWASP GenAI Security Project — Agentic Security Initiative, *OWASP Top 10
for Agentic Applications 2026* (ASI01–ASI10, published Dec 2025),
https://genai.owasp.org/resource/owasp-top-10-for-agentic-applications/, and the
companion *Practical Guide for Secure MCP Server Development*.

Context: the system under review is an MCP server exposing tools to LLM agent
clients (currently one read-only tool returning JSON from disk; Streamable HTTP
on Azure Container Apps; optional Entra auth; may grow more tools/agency). Review
the changes against each threat; most relevant first.

**ASI01 Agent Behaviour Hijack (HIGH)** — Tool output is an indirect
prompt-injection surface. Check: are content fields sanitized/escaped or clearly
tagged as data before being returned in the tool result? Are `tool.description`
and input-schema descriptions statically defined in source (not loaded from a
mutable/external source)? Does `initialize`/`serverInfo` leak overridable
server-side prompt content?

**ASI02 Tool Misuse & Exploitation (HIGH)** — Check: if the tool takes a record
id/filename/query, is it validated against an allowlist/schema and are
path-traversal inputs (`../`, encoded, absolute) rejected so it can't read outside
the data dir? Does the tool description accurately bound capability? Max response
size enforced?

**ASI03 Identity & Privilege Abuse (HIGH)** — The top priority here. Check: is auth
actually enforced in production (not silently bypassable via an env flag)? Is the
Entra token validated **per tool call**, not just at handshake (no session
piggybacking)? Does the app's own Azure identity have least privilege (read-only,
not Contributor)? No credentials/tokens written to logs.

**ASI06 Memory & Context Poisoning (HIGH)** — If the calling agent stores tool
results in memory/RAG, malicious record content persists. Check: output tagged as
data (`type: "text"`, content-type boundary)? Bounded response size (no context
dilution)? Server stateless per request (no cross-caller in-process cache)?

**ASI04 Agentic Supply Chain (MEDIUM)** — Check: dependencies pinned + scanned;
base image pinned by digest; tool manifest source-controlled (no dynamic external
plugin loading); GitHub Actions pinned and secrets scoped.

**ASI05 Unexpected Code Execution / RCE (MEDIUM)** — Check: no `eval`/`exec`/
`subprocess`/unsafe deserialization (`pickle`, unsafe YAML) in the data path;
JSON parsed safely; error messages not built via unsafe templating. Gate any
future tool taking `command`/`code`/`expression`.

**ASI08 Cascading Failures (MEDIUM)** — Check: failures return well-formed MCP
errors (no raw stack traces leaking internals); I/O has timeouts; read tool is
genuinely side-effect-free; tool metrics/error rates observable.

**ASI09 Human-Agent Trust Exploitation (MEDIUM)** — Check: responses carry
provenance metadata (source, timestamp, count); structured data separated from
imperative freetext in records; tool invocations logged for audit; human-in-loop
required for any future write/action tools.

**ASI07 Insecure Inter-Agent Communication (LOWER now)** — Single-hop topology.
Check: TLS enforced at ingress; authorization decisions never based on
self-asserted identity claims in the request body. Re-review if orchestrator /
multi-agent patterns are added.

**ASI10 Rogue Agents (LOWER now)** — The server is the victim, not the agent.
Check: per-identity rate limiting; treat every caller as potentially adversarial
(input validation + auth are the mitigations); anomaly alerting on access patterns.

Do not fabricate IDs or findings; note threats that are minimal/not-applicable to
a read-only tool and why.
