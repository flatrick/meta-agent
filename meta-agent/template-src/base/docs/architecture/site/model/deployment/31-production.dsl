deploymentEnvironment "Production" {
    deploymentNode "Runtime Platform" "Cloud VM, container platform, or on-prem host" {
        prodApp = containerInstance app
    }
}
