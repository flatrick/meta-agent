metaAgent = softwareSystem "meta-agent" "Bootstraps and governs AI-assisted development workflows" {
    !docs _docs/
    !include containers/meta-agent-cli/container.dsl
    !include containers/meta-agent-core/container.dsl
    !include containers/templates/container.dsl
    !include containers/runtime-artifacts/container.dsl
}
