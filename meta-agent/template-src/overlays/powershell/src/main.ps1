Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$modulePath = Join-Path $PSScriptRoot "Application.psm1"
Import-Module $modulePath -Force

Write-Output (Get-Greeting)
