# Verify gate. Runtime state lives under .codex/state, not beside hook code.

function Get-VerifyGatePath {
    param([string]$ProjectRoot)

    if (Get-Command Get-HarnessPolicy -ErrorAction SilentlyContinue) {
        $policy = Get-HarnessPolicy
        $stateRel = if ($policy.stateDirectory) { [string]$policy.stateDirectory } else { '.codex/state' }
    }
    else {
        $stateRel = '.codex/state'
    }

    $stateDir = Join-Path $ProjectRoot ($stateRel -replace '/', '\')
    if (-not (Test-Path -LiteralPath $stateDir)) {
        New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
    }
    return Join-Path $stateDir 'verify-gate.json'
}

function Get-VerifyGate {
    param([string]$ProjectRoot)
    $path = Get-VerifyGatePath -ProjectRoot $ProjectRoot
    if (-not (Test-Path -LiteralPath $path)) {
        return $null
    }
    try {
        return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Set-VerifyGate {
    param(
        [string]$ProjectRoot,
        [string]$Reason,
        [string]$FilePath = ''
    )
    $path = Get-VerifyGatePath -ProjectRoot $ProjectRoot
    @{
        blocked   = $true
        reason    = $Reason
        file      = $FilePath
        timestamp = (Get-Date).ToUniversalTime().ToString('o')
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $path -Encoding utf8
}

function Clear-VerifyGate {
    param([string]$ProjectRoot)
    $path = Get-VerifyGatePath -ProjectRoot $ProjectRoot
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}

function Test-ToolAllowedWhenGated {
    param([string]$ToolName)
    if (Get-Command Get-HarnessPolicy -ErrorAction SilentlyContinue) {
        $policy = Get-HarnessPolicy
        $allowed = @($policy.verify.allowedToolsWhenGated)
    }
    else {
        $allowed = @('Read', 'Write', 'Edit', 'Grep', 'Glob')
    }
    return $allowed -contains $ToolName
}
