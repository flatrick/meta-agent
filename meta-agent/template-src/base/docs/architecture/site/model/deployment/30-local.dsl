deploymentEnvironment "Local Development" {
    deploymentNode "Developer Workstation" "Windows/Linux/macOS" {
        localApp = containerInstance app
    }
}
