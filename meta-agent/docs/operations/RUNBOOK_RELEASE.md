# Runbook: GitHub-Automated Release

Purpose: cut a new release with GitHub-native automation (verification, packaging, release assets, and Pages deployment) with minimal local commands.

## Preconditions

- You have push rights for `main` and SemVer tags.
- Required local tooling for tag creation: `git`.
- Repository rules allow creating the target SemVer tag (for example `v1.0.1`).
- GitHub Actions workflow token permissions allow release publishing (`Settings -> Actions -> General -> Workflow permissions -> Read and write permissions`).

## Recommended Flow

1. Prepare and merge the release commit to `main`:
   - version markers aligned (`csproj`, `README`, `PLAYBOOK`, `runbook`)
   - release-facing docs updated
   - CI on `main` green
2. Create and push a SemVer tag from the release commit:
```bash
git checkout main
git pull --ff-only
git tag -a v1.2.3 -m "v1.2.3"
git push origin refs/tags/v1.2.3
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
git tag -d v1.2.3
git push origin :refs/tags/v1.2.3
```
- If repository rules block tag creation, inspect:
  - `https://github.com/<owner>/<repo>/rules?ref=refs/tags/v1.2.3`
- If immutable release-tag protections are enabled, create/update behavior for existing version labels may be restricted by GitHub policy.
