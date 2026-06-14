# Shared helpers for Cursor command hooks (stdin JSON → stdout JSON).

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
    if ($env:CURSOR_PROJECT_DIR) {
        return $env:CURSOR_PROJECT_DIR
    }
    return (Get-Location).Path
}

function Write-HookJson([hashtable]$Payload) {
    $Payload | ConvertTo-Json -Compress
}

function Deny-Hook {
    param(
        [string]$UserMessage,
        [string]$AgentMessage = $UserMessage
    )
    Write-HookJson @{
        permission    = 'deny'
        user_message  = $UserMessage
        agent_message = $AgentMessage
    }
    exit 2
}

function Allow-Hook {
    Write-HookJson @{ permission = 'allow' }
    exit 0
}

function Allow-ShellHook {
    Write-HookJson @{ permission = 'allow' }
    exit 0
}

function Deny-ShellHook {
    param(
        [string]$UserMessage,
        [string]$AgentMessage = $UserMessage
    )
    Write-HookJson @{
        permission    = 'deny'
        user_message  = $UserMessage
        agent_message = $AgentMessage
    }
    exit 2
}
