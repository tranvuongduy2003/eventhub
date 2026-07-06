# Shared helpers for Codex command hooks (stdin JSON → stdout JSON).

function Read-HookInput {
    param(
        [object[]]$PipelineInput
    )

    $candidates = New-Object System.Collections.Generic.List[string]
    $candidates.Add([Console]::In.ReadToEnd()) | Out-Null
    if ($PipelineInput -and $PipelineInput.Count -gt 0) {
        $candidates.Add((@($PipelineInput) -join [Environment]::NewLine)) | Out-Null
    }
    if ($env:EVENTHUB_HOOK_INPUT_JSON) {
        $candidates.Add($env:EVENTHUB_HOOK_INPUT_JSON) | Out-Null
    }

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }
        $raw = $candidate.TrimStart([char]0xFEFF)
        try {
            $parsed = $raw | ConvertFrom-Json -ErrorAction Stop
            return $parsed
        }
        catch {
            continue
        }
    }

    return $null
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
