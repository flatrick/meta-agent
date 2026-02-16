# Deployment Topology

This model intentionally documents two baseline deployment environments.

## Local Development

Purpose:
- Interactive iteration with developers and operators.
- Local policy tuning and command verification.

Modeled deployment node:
- `Developer Workstation` (Windows/Linux/macOS)

Container instances:
- CLI instance.
- Templates filesystem access.
- Runtime artifacts output.

## CI

Purpose:
- Repeatable validation and site generation.
- Release-quality gate enforcement.

Modeled deployment node:
- `CI Runner` (GitHub Actions or GitLab CI)

Container instances:
- CLI instance.
- Runtime artifacts output.

CI execution anchors:

- GitHub Actions workflow: `.github/workflows/ci.yml` (`structurizr-site` job).
- GitLab pipeline include: `.gitlab-ci.yml` -> `meta-agent/ci/gitlab-ci.yml` (`structurizr-site` and `pages` jobs).
- Shared verification script: `meta-agent/scripts/verify-architecture.py`.

## Security and publication guidance

- Keep public deployment views sanitized.
- Keep sensitive environment overlays in private documentation repositories.
- Treat deployment details in this site as baseline architecture, not full operational runbook.
