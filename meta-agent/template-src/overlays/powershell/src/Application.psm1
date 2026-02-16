Set-StrictMode -Version Latest

function Get-Greeting {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Name = "{{ project_name }}"
    )

    return "Hello from $Name"
}

Export-ModuleMember -Function Get-Greeting
