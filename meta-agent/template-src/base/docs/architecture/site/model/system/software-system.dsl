system = softwareSystem "{{ project_name }}" "Application/system scaffolded with meta-agent" {
    !docs _docs/
    !include containers/application/container.dsl
}
