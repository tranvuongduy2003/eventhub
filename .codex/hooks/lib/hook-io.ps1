# Shared helpers for Codex command hooks (stdin JSON → stdout JSON).

function Read-HookInput {
    $raw = [Console]::In.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return $null
    }
    try {
        return $raw | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Get-ProjectRoot {
    if ($env:CODEX_PROJECT_DIR) {
        return $env:CODEX_PROJECT_DIR
    }
    return (Get-Location).Path
}

function Write-HookJson([hashtable]$Payload) {
    $Payload | ConvertTo-Json -Compress -Depth 10
}

function Deny-Hook {
    param(
        [string]$Reason
    )
    Write-HookJson @{
        permission = "deny"
        reason     = $Reason
    }
    exit 2
}

function Allow-Hook {
    Write-HookJson @{
        permission = "allow"
    }
    exit 0
}

function Deny-ShellHook {
    param(
        [string]$Reason
    )
    Write-HookJson @{
        permission = "deny"
        reason     = $Reason
    }
    exit 2
}

function Allow-ShellHook {
    Write-HookJson @{
        permission = "allow"
    }
    exit 0
}
