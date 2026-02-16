deploymentEnvironment "CI" {
    deploymentNode "CI Runner" "GitHub Actions or GitLab CI runner" {
        cliCi = containerInstance cli
        artifactsCi = containerInstance artifacts
    }
}
