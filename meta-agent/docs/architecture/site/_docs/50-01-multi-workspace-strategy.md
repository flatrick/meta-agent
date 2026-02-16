# 50-01 Multi-Workspace Strategy

Use multiple Structurizr workspaces when one model/site becomes too deep for first-time understanding.
This is valid even when a single team owns the full system.

## Goal

Keep the primary published site focused on:

- system context
- container boundaries
- key design choices
- high-level operations and governance

## Recommended split

1. Primary workspace/site (this repository):
- architecture-first overview for onboarding and decision context.
2. Detailed workspace/site(s):
- deeper domain/workflow/component details by bounded area.

## Ownership note

- Separate ownership is not required.
- A single team can maintain multiple layered workspaces to keep each site usable.
- The split is driven by comprehension and change-surface, not org chart boundaries.

## Linking model

- Keep short links in the primary site to detailed sites.
- Treat the primary site as the architectural entrypoint.
- Avoid duplicating deep content across sites.

## When to split

- Navigation becomes crowded for new readers.
- Detailed runbooks/components dominate top-level architecture pages.
- A single team still needs independent review/release cadence for deep technical layers.
