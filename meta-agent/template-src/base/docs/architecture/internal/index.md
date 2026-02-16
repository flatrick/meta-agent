# Internal Knowledge Index (Johnny.Decimal)

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

For scaffolded projects, the baseline for high team-maturity docs is:

- `1x`: architecture and engineering execution.
- `3x`: planning and roadmap evolution.
- `4x`: documentation standards and publishing communication.

## Notes

- `x0` is metadata/index for `x1-x9`.
- Category-level `.00` metadata folders are optional; add one only when a category needs local rules.
- Keep Structurizr published docs outside this internal tree.

## When Adding A New Area

1. Add `Nx/index.md` only when there is actual content to add in that area.
2. Add at least one category folder (`Nx/NN/index.md`) with substantive content.
3. Define the area purpose in `Nx/index.md` and keep it narrow.
4. Update this root index to link the new area.
5. Keep placeholder-only trees out of the repository.

## Global Rules

- Use `Ax/NN/NN.MM/index.md` grouping for non-published docs (`1x/11/11.01/index.md`, etc.).
- Keep publishable Structurizr docs under `docs/architecture/site/_docs/` and `docs/architecture/site/model/**/_docs/`.
