BeforeAll {
    Set-StrictMode -Version Latest
    $modulePath = Join-Path $PSScriptRoot ".." "src" "Application.psm1"
    Import-Module $modulePath -Force
}

Describe "Get-Greeting" {
    It "returns default project greeting" {
        Get-Greeting | Should -Be "Hello from {{ project_name }}"
    }

    It "allows overriding the greeting name" {
        Get-Greeting -Name "Pester" | Should -Be "Hello from Pester"
    }
}
