# Shared helpers for Claude Code command hooks (stdin JSON → stdout JSON).

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
    if ($env:CLAUDE_PROJECT_DIR) {
        return $env:CLAUDE_PROJECT_DIR
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
        hookSpecificOutput = @{
            hookEventName             = "PreToolUse"
            permissionDecision        = "deny"
            permissionDecisionReason  = $Reason
        }
    }
    exit 0
}

function Allow-Hook {
    exit 0
}

function Deny-ShellHook {
    param(
        [string]$Reason
    )
    Write-HookJson @{
        hookSpecificOutput = @{
            hookEventName             = "PreToolUse"
            permissionDecision        = "deny"
            permissionDecisionReason  = $Reason
        }
    }
    exit 0
}

function Allow-ShellHook {
    exit 0
}
