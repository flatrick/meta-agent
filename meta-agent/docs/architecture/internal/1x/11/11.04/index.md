# Deployment Modeling and Security Guidance

## Goal

Use Structurizr deployment views to document how the system runs across environments while controlling sensitive operational detail.

## Recommended Baseline

- Keep non-sensitive deployment topology in the main repository.
- Model at least:
- local development deployment
- CI deployment
- production baseline deployment
- Publish these views via `structurizr-site-generatr` with the rest of project docs.

## Sensitive Detail Policy

- Treat detailed infrastructure data as potentially sensitive.
- Examples:
- internal hostnames and IP ranges
- private network topology
- tenant IDs and subscription/account identifiers
- operational secrets and secret-store paths
- private endpoint names

## Optional Split-Repository Pattern

- Keep sanitized deployment architecture in the main repository.
- Maintain sensitive deployment overlays/docs in a private repository.
- Generate/publish the private deployment docs site separately with restricted access.
- Cross-link from the public/internal main site to the private site where appropriate.

## Why This Split Helps

- Reduces blast radius if the main repository is copied.
- Keeps day-to-day architecture documentation broadly shareable.
- Allows stricter access controls for operationally sensitive data.
