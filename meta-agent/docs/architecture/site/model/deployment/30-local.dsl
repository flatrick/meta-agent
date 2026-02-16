deploymentEnvironment "Local Development" {
    deploymentNode "Developer Workstation" "Windows/Linux/macOS" {
        cliLocal = containerInstance cli
        templatesLocal = containerInstance templates
        artifactsLocal = containerInstance artifacts
    }
}
