# Deterministic guard rules. Policy data lives in .codex/policies; this file is
# only the lifecycle-hook adapter.

function Get-HarnessPolicyPath {
    $root = Get-ProjectRoot
    return Join-Path $root '.codex\policies\harness-policy.json'
}

function Get-HarnessPolicy {
    if ($script:HarnessPolicy) {
        return $script:HarnessPolicy
    }

    $path = Get-HarnessPolicyPath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Harness policy not found: $path"
    }

    $script:HarnessPolicy = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    return $script:HarnessPolicy
}

function Normalize-HarnessPath {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ''
    }
    return ($Path -replace '\\', '/')
}

function Get-MatchingPolicyRule {
    param(
        [object[]]$Rules,
        [string]$Value
    )
    foreach ($rule in @($Rules)) {
        if ($Value -match [string]$rule.pattern) {
            return $rule
        }
    }
    return $null
}

function Test-BlockedEditPath {
    param([string]$Path)
    $normalized = Normalize-HarnessPath -Path $Path
    if (-not $normalized) {
        return $false
    }

    $policy = Get-HarnessPolicy
    return $null -ne (Get-MatchingPolicyRule -Rules $policy.protectedEditPaths -Value $normalized)
}

function Get-BlockedEditReason {
    param([string]$Path)
    $normalized = Normalize-HarnessPath -Path $Path
    $policy = Get-HarnessPolicy
    $rule = Get-MatchingPolicyRule -Rules $policy.protectedEditPaths -Value $normalized
    if ($rule) {
        return [string]$rule.reason
    }
    return 'This path is protected by the agent harness.'
}

function Test-DangerousShellCommand {
    param([string]$Command)
    if ([string]::IsNullOrWhiteSpace($Command)) {
        return $false
    }

    $policy = Get-HarnessPolicy
    return $null -ne (Get-MatchingPolicyRule -Rules $policy.blockedShellCommands -Value $Command)
}

function Get-DangerousShellReason {
    param([string]$Command)
    $policy = Get-HarnessPolicy
    $rule = Get-MatchingPolicyRule -Rules $policy.blockedShellCommands -Value $Command
    if ($rule) {
        return [string]$rule.reason
    }
    return 'Command blocked by agent harness.'
}

function Test-ShouldVerifyFile {
    param(
        [string]$FilePath,
        [string]$ProjectRoot
    )
    if ([string]::IsNullOrWhiteSpace($FilePath)) {
        return $false
    }

    $rel = $FilePath
    if ($FilePath.StartsWith($ProjectRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        $rel = $FilePath.Substring($ProjectRoot.Length).TrimStart('\', '/')
    }
    $rel = $rel -replace '\\', '/'

    $policy = Get-HarnessPolicy
    foreach ($pattern in @($policy.verify.skipPatterns)) {
        if ($rel -match [string]$pattern) {
            return $false
        }
    }

    return ($rel -match [string]$policy.verify.verifiablePattern)
}
