# 50-01 Multi-Workspace Strategy

Use multiple Structurizr workspaces when a single site becomes too deep for first-time readers.
This is valid even when one team owns the full system.

## Default pattern

1. Keep the primary workspace/site architecture-first:
- system context
- container boundaries
- key design choices
2. Publish companion detailed workspaces/sites for deep component, flow, or domain detail.
3. Link from the primary site to companion sites instead of duplicating deep content.

## Split criteria

- Navigation becomes crowded for onboarding.
- Detailed docs start obscuring high-level architecture.
- The same team needs separate review/release cadence for deeper technical layers.
