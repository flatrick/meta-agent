# Documentation Index

This folder is organized by document purpose to prevent mixed-content drift.
`meta-agent/docs/` is the canonical documentation root for this repository.
Structurizr site generation is intentionally scoped to [`meta-agent/docs/architecture/site/`](architecture/site/_docs/00-00-index.md) to avoid publishing unintended files.
Repository structure/navigation reference: [`meta-agent/PROJECT_MAP.md`](../PROJECT_MAP.md).
Any documentation outside [`meta-agent/docs/architecture/site/`](architecture/site/_docs/00-00-index.md) is not published by the repository Structurizr site.

## Placement Rules (Source Of Truth)

- Put public architecture/site content in [`meta-agent/docs/architecture/site/`](architecture/site/_docs/00-00-index.md) only.
- Put non-published architecture model/governance docs in [`meta-agent/docs/architecture/internal/`](architecture/internal/index.md).
- Put operator workflow and day-2 run procedures in [`meta-agent/docs/operations/`](operations/runbook.md).
- Put product/program planning in [`meta-agent/docs/planning/`](planning/IMPLEMENTATION_ROADMAP.md).
- For repository-wide Markdown link checks/backlinks, use `python3 ./meta-agent/scripts/scan-markdown-links.py` (do not manually scan all markdown files).

## Linking Rules

- Use markdown links when the target document is expected to be reachable in the same documentation surface.
- In published Structurizr site docs (`meta-agent/docs/architecture/site/_docs/` and `meta-agent/docs/architecture/site/model/**/_docs/`), keep references to non-published internal docs as inline-code paths, not markdown links.
- This avoids publishing broken links while still giving humans and AI/LLM agents deterministic file pointers.
- Use inline code for commands and filesystem paths used as operational examples.

Naming intent:
- [`architecture/internal/1x/11/`](architecture/internal/1x/11/index.md) is architecture modeling detail.
- [`architecture/internal/1x/12/`](architecture/internal/1x/12/12.00/index.md) is architecture governance/tooling detail.
- [`architecture/internal/3x/31/`](architecture/internal/3x/31/index.md) is architecture roadmap and learnings detail.
- [`architecture/internal/4x/41/`](architecture/internal/4x/41/index.md) is Structurizr documentation/publishing guidance.
- Top-level `operations/` and `planning/` are repository-level, not architecture-internal.

## `architecture/`

- [`site/adrs/0001-meta-agent-governance-model.md`](architecture/site/adrs/0001-meta-agent-governance-model.md) — core architecture and design decisions.
- [`internal/index.md`](architecture/internal/index.md) — non-published architecture docs root index (1x-9x buckets).
- [`internal/1x/10/index.md`](architecture/internal/1x/10/index.md) — metadata index for the `1x` development bucket.
- [`internal/index.md#jd-bucket-map-1x-9x`](architecture/internal/index.md#jd-bucket-map-1x-9x) — canonical 1x–9x Johnny.Decimal bucket map for this repo.
- [`internal/1x/11/`](architecture/internal/1x/11/index.md) — internal architecture modeling detail (non-published).
- [`internal/1x/12/`](architecture/internal/1x/12/12.00/index.md) — internal architecture governance/tooling detail (non-published).
- [`internal/3x/31/`](architecture/internal/3x/31/index.md) — internal architecture roadmap and learnings (non-published).
- [`internal/4x/41/`](architecture/internal/4x/41/index.md) — internal Structurizr documentation/publishing guidance (non-published).

## `operations/`

- [`USAGE_GUIDE.md`](operations/USAGE_GUIDE.md) — end-to-end team usage over time.
- [`runbook.md`](operations/runbook.md) — operator runbook and command-level procedures.
- [`VERIFICATION_MATRIX.md`](operations/VERIFICATION_MATRIX.md) — required checks by command/mode and stabilization gate checklist.
- [`RUNBOOK_NEW_PROJECT.md`](operations/RUNBOOK_NEW_PROJECT.md) — explicit onboarding flow for new repositories.
- [`RUNBOOK_EXISTING_PROJECT.md`](operations/RUNBOOK_EXISTING_PROJECT.md) — explicit onboarding flow for existing/legacy repositories.
- [`RUNBOOK_INTERACTIVE_IDE.md`](operations/RUNBOOK_INTERACTIVE_IDE.md) — day-2 flow for developer-assist mode.
- [`RUNBOOK_AUTONOMOUS_RUNNER.md`](operations/RUNBOOK_AUTONOMOUS_RUNNER.md) — day-2 flow for autonomous ticket-runner mode.
- [`RUNBOOK_RELEASE.md`](operations/RUNBOOK_RELEASE.md) — GitHub-automated release flow for SemVer tags.
- [`GITHUB_ACTIONS_BILLING_AUDITOR_PROMPT.md`](operations/GITHUB_ACTIONS_BILLING_AUDITOR_PROMPT.md) — conservative prompt contract for auditing GitHub Actions free-tier/billing risk.
- [`POLICY_UPGRADE_GUIDE.md`](operations/POLICY_UPGRADE_GUIDE.md) — policyVersion migration/upgrade and troubleshooting guide.

## `planning/`

- [`IMPLEMENTATION_ROADMAP.md`](planning/IMPLEMENTATION_ROADMAP.md) — phased implementation and status tracking.

## Top-level docs

- [`architecture/site/`](architecture/site/_docs/00-00-index.md) — Structurizr model + published docs subtree (`_docs/`, `model/**/_docs/`, `adrs/`, `assets/`).
- [`architecture/internal/`](architecture/internal/index.md) — non-published detailed working architecture notes.
