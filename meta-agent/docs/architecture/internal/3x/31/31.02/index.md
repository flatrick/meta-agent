# Structurizr Learnings Plan

Date: 2026-02-15
Source: temporary Structurizr probe workspace (removed after learnings were integrated)
Goal: retain actionable Structurizr DSL + `structurizr-site-generatr` decisions without depending on temporary folders.

## 1) Key Learnings Captured

1. Use modular DSL structure with stable view keys.
2. Model dependency direction explicitly (inbound/outbound/bidirectional).
3. Keep agent-operation docs outside published `!docs` scope.
4. Track feature support gaps in a compatibility matrix.
5. Enforce generation + static-output checks via verification scripts and CI.

## 2) Integration Status (This Repository)

Completed:

1. Added human+agent Structurizr playbook docs in `meta-agent/docs/architecture/internal/4x/41/`.
2. Added canonical compatibility matrix in `meta-agent/docs/architecture/internal/4x/41/`.
3. Updated Structurizr docs/home pages to reference playbook + compatibility matrix.
4. Added `meta-agent/scripts/verify-architecture.py` as the canonical verification entrypoint.
5. Updated repository CI/docs jobs to run architecture verification script.

## 3) Integration Status (Scaffold Templates)

Completed for `generic`, `node`, and `dotnet` templates:

1. Added scaffolded playbook docs.
2. Added scaffolded compatibility matrix docs.
3. Added scaffolded `scripts/verify-architecture.py` as the template verification entrypoint.
4. Updated template GitHub/GitLab docs workflows to run verification scripts.
5. Updated template documentation entry points to route users/agents to these docs.

## 4) Ongoing Maintenance Rules

1. When validating a new Structurizr DSL feature, update compatibility matrix with `Supported`, `Limited`, or `Not validated`.
2. Keep view keys stable unless intentionally breaking; if changed, update verification scripts/docs in same change.
3. Keep static HTML smoke checks as part of architecture verification before release.
4. Record significant compatibility findings in `meta-agent/DOC_DELTA.md`.
