# Angle: Confidential information leakage

Your single focus is **confidential data getting out** — secrets, credentials,
PII, and sensitive internal information — whether committed to the repo, baked
into artifacts, returned to callers, or emitted in logs/errors. Map to OWASP
LLM02 (Sensitive Information Disclosure) and LLM07 (System Prompt Leakage) where
relevant, but lead with the concrete exposure.

Hunt across these channels in the changes:

- **Secrets in source / IaC** — API keys, connection strings, tokens, passwords,
  certificates, or `*.json` credential blobs committed to the repo. Bicep secrets
  as plaintext `value:` instead of `secretRef` / Key Vault. ACR admin credentials
  enabled and surfaced. Any real tenant / subscription / client ID hardcoded.
- **Secrets in the build artifact** — Does the Dockerfile `COPY` in `.env`, local
  config, or build secrets? Are secrets passed as build args that persist in image
  layers? Is the data directory bundling anything sensitive?
- **Sensitive data in the served records** — The tool returns `data/` records
  verbatim. Scan for PII, emails, internal system descriptions, or secrets inside
  the JSON data files. Is there field projection/allowlisting, or is the whole
  record exposed? Is there CI secret-scanning of `data/`?
- **Leakage via errors / logs** — Do error responses or logs include absolute
  paths, stack traces, internal hostnames, config values, or — worst — auth
  headers / bearer tokens / secrets? Search for logging of `Authorization`,
  `Bearer`, `client_secret`, `api_key`, `password`.
- **Leakage via metadata** — Do MCP `tool.description`, input-schema descriptions,
  `serverInfo`, or health endpoints reveal internal architecture, data-source
  paths, or access-control logic to callers?
- **Leakage via CI** — Do GitHub Actions echo secrets, write them to logs, or
  expose them to untrusted PR contexts? Are secrets scoped to the minimum jobs?
- **Git history** — If a secret appears to have ever been committed, flag it as
  Critical even if removed in the current diff (history persists; rotate).

For each finding: name the **exact secret/data and where it is exposed**, who can
read it, the blast radius, and the fix (remove + rotate, move to a secret store,
project fields, scrub logs). Distinguish a **live/real** secret (Critical) from a
placeholder/example. Don't flag obvious non-secrets (e.g. public resource names);
keep the signal high.
