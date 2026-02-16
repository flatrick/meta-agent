# Internal Knowledge Index (Johnny.Decimal)

This is the non-published JD root for internal architecture and team knowledge.

## Active Areas

- [1x Development](1x/index.md)
- [3x Projects And Planning](3x/index.md)
- [4x Documentation And Communication](4x/index.md)

No other area is materialized yet.

## Capacity Model

Johnny.Decimal allows up to 9 top-level areas (`1x` to `9x`) in this tree.
These are capacity slots, not mandatory predefined buckets.

Rules:

- Only materialize an area when it has real content.
- Do not create placeholder-only trees.
- Keep area numbers stable once introduced.
- Keep area purpose explicit in its `Nx/index.md`.

## JD Bucket Map (1x-9x)

## Maturity Baseline

For this repository and templates, the baseline for high team-maturity docs is:

- [`1x`](1x/index.md): architecture and engineering execution.
- [`3x`](3x/index.md): planning and roadmap evolution.
- [`4x`](4x/index.md): documentation standards and publishing communication.

Additional areas (`2x`, `5x`, `6x`, `7x`, `8x`, `9x`) are added only when their first real content appears.

## Current Area Map

### 1x Development

- [`10`](1x/10/index.md) About the `1x` sub-structure (metadata/index).
- [`11`](1x/11/index.md) Architecture and system design (C4 model alignment, boundaries, responsibilities).
- [`12`](1x/12/index.md) Engineering governance and tooling (standards, verification, alignment loop).
- `13-19` currently unused.

### 3x Projects And Planning

- [`31`](3x/31/index.md) Architecture roadmap and learnings.

### 4x Documentation And Communication

- [`41`](4x/41/index.md) Structurizr/architecture documentation standards and publishing guidance.

## Notes

- `x0` buckets are metadata/index buckets for their area (`10` explains `11-19`, `20` explains `21-29`, etc.).
- Category-level `.00` metadata folders are optional; create one only when that category needs local rules or guidance.
- Navigation chain is explicit at each level: `internal/index.md` -> `Nx/index.md` -> `Nx/NN/index.md`.
- Use only the areas and categories you need.
- Keep Structurizr published site content outside this internal JD tree.

## When Adding A New Area

1. Add `Nx/index.md` only when there is actual content to add in that area.
2. Add at least one category folder (`Nx/NN/index.md`) with substantive content.
3. Define the area purpose in `Nx/index.md` and keep it narrow.
4. Update this root index to link the new area.
5. Keep placeholder-only trees out of the repository.

## Global Rules

- Use `Ax/NN/NN.MM/index.md` grouping for non-published docs ([`1x/11/11.01/index.md`](1x/11/11.01/index.md), etc.).
- Keep publishable Structurizr docs in `meta-agent/docs/architecture/site/_docs/` and `meta-agent/docs/architecture/site/model/**/_docs/`.
