# Verification and CI

This project enforces Structurizr generation quality through scripts and CI workflows.

## Local verification

Preferred architecture verification command:

```bash
python3 ./meta-agent/scripts/verify-architecture.py
```

What this verifies:

- Site generation succeeds.
- Expected view keys exist in generated `workspace.json`.
- Expected key elements are present.

## CI integration

GitHub Actions:

- `.github/workflows/ci.yml`
- `structurizr-site` job runs architecture verification before publishing artifacts/pages

GitLab CI:

- `meta-agent/ci/gitlab-ci.yml`
- `structurizr-site` and `pages` jobs run architecture verification script

## Compatibility discipline

Before using new Structurizr DSL features in production docs, consult:

- `meta-agent/docs/architecture/internal/4x/41/41.03/index.md`

If a new feature is tested, update compatibility status and changelog.

## Alignment routine (template-ready)

Run this loop whenever code behavior or boundaries change:

1. Confirm container boundaries and relationships in `workspace.dsl`.
2. Verify code anchors for each container and critical relationship.
3. Record mismatches as explicit follow-up items in published docs.
4. Generate and validate site output.
5. Ensure CI jobs continue to run architecture verification.

Constraints:

- Keep Structurizr DSL as the canonical architecture source.
- Do not introduce parallel architecture documents outside Structurizr docs.
- Report model/code mismatches explicitly instead of silently changing model semantics.
