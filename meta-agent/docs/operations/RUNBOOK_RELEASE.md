# Runbook: GitHub-Automated Release

Purpose: cut a new release with GitHub-native automation (verification, packaging, release assets, and Pages deployment) with minimal local commands.

## Preconditions

- You have push rights for `main` and SemVer tags.
- Required local tooling for tag creation: `git`.
- Required local tooling for pre-tag verification: `dotnet`, `python3`.
- Repository rules allow creating the target SemVer tag (for example `v1.0.2`).
- GitHub Actions workflow token permissions allow release publishing (`Settings -> Actions -> General -> Workflow permissions -> Read and write permissions`).

## Pre-Tag Verification (Required)

Run this before creating or pushing a release tag.

Preferred one-command gate:

```bash
python3 ./meta-agent/scripts/pre-release-verify.py --tag v1.0.2 --summary-out ./.meta-agent-temp/pre-release-verification/latest-summary.json
```

If you need explicit step-by-step execution, run:

```bash
python3 ./meta-agent/scripts/test-pre-release-verify.py
python3 ./meta-agent/scripts/test-sync-version-markers.py
python3 ./meta-agent/scripts/sync-version-markers.py --check --tag v1.0.2
python3 ./meta-agent/scripts/check-version-sync.py --tag v1.0.2
python3 ./meta-agent/scripts/test-package-release.py
python3 ./meta-agent/scripts/test-compose-templates.py
python3 ./meta-agent/scripts/compose-templates.py
python3 ./meta-agent/scripts/compose-templates.py --check
python3 ./meta-agent/scripts/test-structurizr-site-wrappers.py
python3 ./meta-agent/scripts/test-manage-doc-delta.py
python3 ./meta-agent/scripts/manage-doc-delta.py check
python3 ./meta-agent/scripts/check-doc-command-alignment.py
python3 ./meta-agent/scripts/structurizr-site.py generate --dry-run
python3 ./meta-agent/scripts/structurizr-site.py serve --port 8080 --dry-run
dotnet test ./meta-agent/dotnet/MetaAgent.slnx -v minimal
python3 ./meta-agent/scripts/test-with-coverage.py
python3 ./meta-agent/scripts/clean-worktree.py --check-tracked
python3 ./meta-agent/scripts/clean-worktree.py --apply --include-coverage
python3 ./meta-agent/scripts/clean-worktree.py --check
python3 ./meta-agent/scripts/clean-worktree.py --check-tracked
python3 ./meta-agent/scripts/scan-markdown-links.py
python3 ./meta-agent/scripts/scan-markdown-links.py --fail-on-dead
```

Expected result before tagging:
- all commands above pass
- summary file exists at `.meta-agent-temp/pre-release-verification/latest-summary.json`
- working tree is clean or only contains intentional release changes
- release marker coverage is maintained via `meta-agent/config/release-version-markers.json` (add new marker locations there, not in script code)

## Recommended Flow

1. Prepare and merge the release commit to `main`:
   - version markers aligned (`csproj`, `README`, `PLAYBOOK`, `runbook`)
   - release-facing docs updated
   - pre-tag verification passed locally
   - CI on `main` green
2. Create and push a SemVer tag from the release commit:
```bash
git checkout main
git pull --ff-only
git tag -a v1.0.2 -m "v1.0.2"
git push origin refs/tags/v1.0.2
```
3. Let GitHub Actions handle release execution:
   - run `pre-release-verification` gate
   - run coverage/tests and architecture verification
   - build runtime release packages (`win-x64`, `linux-x64`, `osx-arm64`, `osx-x64`) + `SHA256SUMS.txt`
   - bundle generated Structurizr site output as `meta-agent-<version>-structurizr-site.zip`
   - publish GitHub Release for that tag with generated notes and attached assets
   - deploy Structurizr site to GitHub Pages
4. Verify outputs in GitHub:
   - Actions run for the tag is successful
   - Release exists under `Releases` with expected assets
   - GitHub Pages reflects the new Structurizr site publish

## Operational Notes

- Preferred path is tag-driven automation; avoid manual `gh release create` for normal releases.
- If a tag was pushed by mistake, delete it locally and remotely before re-tagging:
```bash
git tag -d v1.0.2
git push origin :refs/tags/v1.0.2
```
- If repository rules block tag creation, inspect:
  - `https://github.com/<owner>/<repo>/rules?ref=refs/tags/v1.0.2`
- If immutable release-tag protections are enabled, create/update behavior for existing version labels may be restricted by GitHub policy.
