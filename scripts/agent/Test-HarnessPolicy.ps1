#Requires -Version 5.1
<#
.SYNOPSIS
  Validate current EventHub Codex harness guardrails.
#>

param(
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$errors = [System.Collections.Generic.List[string]]::new()

function Add-Error {
    param([string]$Message)
    $script:errors.Add($Message) | Out-Null
}

function Test-RequiredFile {
    param([string]$RelativePath)
    $path = Join-Path $repoRoot ($RelativePath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Error "Missing required file: $RelativePath"
        return $false
    }
    return $true
}

function Test-ForbiddenPath {
    param([string]$RelativePath)
    $path = Join-Path $repoRoot ($RelativePath -replace '/', '\')
    if (Test-Path -LiteralPath $path) {
        Add-Error "Forbidden legacy path exists: $RelativePath"
    }
}

function Get-Text {
    param([string]$RelativePath)
    if (-not (Test-RequiredFile $RelativePath)) { return $null }
    return Get-Content -LiteralPath (Join-Path $repoRoot ($RelativePath -replace '/', '\')) -Raw -Encoding UTF8
}

function Test-FileContains {
    param(
        [string]$RelativePath,
        [string[]]$Needles
    )
    $text = Get-Text $RelativePath
    if ($null -eq $text) { return }
    foreach ($needle in $Needles) {
        if ($text -notmatch [regex]::Escape($needle)) {
            Add-Error "$RelativePath missing required text: $needle"
        }
    }
}

function Test-PowerShellParses {
    param([string]$RelativePath)
    $path = Join-Path $repoRoot ($RelativePath -replace '/', '\')
    if (-not (Test-RequiredFile $RelativePath)) { return }
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($path, [ref]$tokens, [ref]$parseErrors) | Out-Null
    foreach ($error in @($parseErrors)) {
        Add-Error "$RelativePath parse error: $($error.Message)"
    }
}

function Invoke-Hook {
    param(
        [string]$RelativePath,
        [string]$Payload
    )
    $path = Join-Path $repoRoot ($RelativePath -replace '/', '\')
    if (-not (Test-RequiredFile $RelativePath)) { return $null }
    $output = $Payload | powershell -NoProfile -ExecutionPolicy Bypass -File $path 2>&1
    $exitCode = $LASTEXITCODE
    return [pscustomobject]@{
        Output = $output
        ExitCode = $exitCode
    }
}

function Invoke-VerifyChangedCodePlan {
    param([string]$Path)

    $scriptPath = Join-Path $repoRoot 'scripts\agent\Verify-ChangedCode.ps1'
    if (-not (Test-RequiredFile 'scripts/agent/Verify-ChangedCode.ps1')) { return $null }

    $output = powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath -Path $Path -PlanOnly -Json 2>&1
    $exitCode = $LASTEXITCODE
    return [pscustomobject]@{
        Output = $output
        ExitCode = $exitCode
    }
}

function Test-GuardAllowsOrDenies {
    param(
        [string]$Payload,
        [bool]$ShouldDeny,
        [string]$Label
    )

    $result = Invoke-Hook -RelativePath '.codex/hooks/guard-dangerous.ps1' -Payload $Payload
    if ($null -eq $result) { return }
    if ($result.ExitCode -ne 0) {
        Add-Error "$Label guard hook exited $($result.ExitCode)"
        return
    }
    $text = ($result.Output | Out-String)
    $denied = $text -match '"permissionDecision"\s*:\s*"deny"'
    if ($denied -ne $ShouldDeny) {
        Add-Error "$Label deny expectation expected $ShouldDeny, got $denied. Output: $text"
    }
}

function Test-VerifyPlanIncludesHarnessPolicy {
    param(
        [string]$Path,
        [string]$Label
    )

    $result = Invoke-VerifyChangedCodePlan -Path $Path
    if ($null -eq $result) { return }
    if ($result.ExitCode -ne 0) {
        Add-Error "$Label verify plan exited $($result.ExitCode): $($result.Output | Out-String)"
        return
    }

    try {
        $json = ($result.Output | Out-String) | ConvertFrom-Json
    }
    catch {
        Add-Error "$Label verify plan did not return JSON: $($_.Exception.Message)"
        return
    }

    $commands = @($json.commands)
    if (-not ($commands -contains 'powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-HarnessPolicy.ps1')) {
        Add-Error "$Label verify plan does not include Test-HarnessPolicy.ps1"
    }
}

$requiredFiles = @(
    'AGENTS.md',
    '.codex/config.toml',
    '.codex/hooks.json',
    '.codex/hooks/guard-dangerous.ps1',
    '.codex/hooks/format-on-write.ps1',
    '.codex/hooks/telemetry-log.ps1',
    '.codex/hooks/telemetry-session.ps1',
    '.codex/hooks/verify-on-stop.ps1',
    '.codex/agents/requirement-analyst.toml',
    '.codex/agents/spec-brainstormer.toml',
    '.codex/agents/implementation-planner.toml',
    '.codex/agents/implementer.toml',
    '.codex/agents/test-writer.toml',
    '.codex/agents/code-reviewer.toml',
    '.codex/agents/security-reviewer.toml',
    '.codex/agents/acceptance-verifier.toml',
    '.codex/agents/harness-doctor.toml',
    'scripts/agent/Verify-ChangedCode.ps1',
    'scripts/agent/Test-DocsMemory.ps1',
    'scripts/agent/Test-HarnessPolicy.ps1'
)

foreach ($file in $requiredFiles) {
    Test-RequiredFile $file | Out-Null
}

foreach ($path in @(
    'harness',
    '.codex/hooks/lib',
    '.codex/policies/harness-policy.json',
    '.agents/skills/memory-sync',
    '.agents/skills/harness-evals',
    '.agents/skills/harness-orchestrator',
    '.agents/skills/harness-policies',
    '.agents/skills/harness-telemetry',
    '.agents/skills/harness-tools',
    'scripts/Get-AffectedTests.ps1',
    'scripts/agent/Get-HarnessStatus.ps1',
    'scripts/agent/New-HarnessSkill.ps1',
    'scripts/agent/New-PrHandoff.ps1',
    'scripts/agent/Repo-Bootstrap.ps1',
    'scripts/agent/Test-CookPlan.ps1'
)) {
    Test-ForbiddenPath $path
}

foreach ($hook in @(
    '.codex/hooks/guard-dangerous.ps1',
    '.codex/hooks/format-on-write.ps1',
    '.codex/hooks/telemetry-log.ps1',
    '.codex/hooks/telemetry-session.ps1',
    '.codex/hooks/verify-on-stop.ps1',
    'scripts/agent/Verify-ChangedCode.ps1',
    'scripts/agent/Test-DocsMemory.ps1'
)) {
    Test-PowerShellParses $hook
}

try {
    Get-Content -LiteralPath (Join-Path $repoRoot '.codex/hooks.json') -Raw -Encoding UTF8 | ConvertFrom-Json | Out-Null
}
catch {
    Add-Error ".codex/hooks.json is not valid JSON: $($_.Exception.Message)"
}

Test-FileContains '.codex/hooks.json' @(
    'guard-dangerous.ps1',
    'format-on-write.ps1',
    'telemetry-log.ps1',
    'verify-on-stop.ps1',
    'exec_command'
)

Test-FileContains '.codex/hooks/guard-dangerous.ps1' @(
    'Get-PatchPaths',
    'functions.exec_command',
    '.env'
)

Test-FileContains '.codex/hooks/format-on-write.ps1' @(
    'dotnet format EventHub.slnx',
    'yarn --cwd web prettier'
)

Test-FileContains '.codex/hooks/verify-on-stop.ps1' @(
    'dotnet build EventHub.slnx',
    'yarn --cwd web build',
    'yarn --cwd e2e test',
    'heavySensorWarnings',
    'large changed surface without heavy sensors'
)

Test-FileContains 'AGENTS.md' @(
    'docs/product.md',
    'docs/features.md',
    'docs/technical.md',
    '$create-pr',
    'scripts/agent/Verify-ChangedCode.ps1'
)

foreach ($agent in @(
    '.codex/agents/requirement-analyst.toml',
    '.codex/agents/spec-brainstormer.toml',
    '.codex/agents/implementation-planner.toml',
    '.codex/agents/implementer.toml',
    '.codex/agents/test-writer.toml',
    '.codex/agents/code-reviewer.toml',
    '.codex/agents/security-reviewer.toml',
    '.codex/agents/acceptance-verifier.toml',
    '.codex/agents/harness-doctor.toml'
)) {
    Test-FileContains $agent @('EventHub')
    $text = Get-Text $agent
    if ($null -ne $text -and $text -match 'docs/codex|docs/domain|docs/_memory|Solution\.slnx|SQL Server|Orval|local-first|en\.json|de\.json|tracker CLI') {
        Add-Error "$agent contains stale scaffold or removed-path guidance"
    }
}

$denyEnvPayload = '{"tool_name":"Write","session_id":"policy-test","tool_input":{"file_path":".env.local"}}'
$denyGeneratedPayload = '{"tool_name":"Write","session_id":"policy-test","tool_input":{"file_path":"web/src/generated/api-schema.ts"}}'
$denyForcePushPayload = '{"tool_name":"Bash","session_id":"policy-test","tool_input":{"command":"git push --force"}}'
$denyExecForcePushPayload = '{"tool_name":"exec_command","session_id":"policy-test","tool_input":{"cmd":"git push --force"}}'
$denyEnvReadPayload = '{"tool_name":"Bash","session_id":"policy-test","tool_input":{"command":"Get-Content .env.local"}}'
$denyEnvTypePayload = '{"tool_name":"Bash","session_id":"policy-test","tool_input":{"command":"type .env.local"}}'
$denyEnvCatPayload = '{"tool_name":"Bash","session_id":"policy-test","tool_input":{"command":"cat .env.local"}}'
$denySecretDirectoryReadPayload = '{"tool_name":"exec_command","session_id":"policy-test","tool_input":{"cmd":"Get-Content secrets/local.txt"}}'
$denyPatchEnvPayload = @{
    tool_name = 'apply_patch'
    session_id = 'policy-test'
    tool_input = @{
        patch = "*** Begin Patch`n*** Update File: .env.local`n@@`n+NOPE`n*** End Patch`n"
    }
} | ConvertTo-Json -Depth 5 -Compress
$allowBuildPayload = '{"tool_name":"Bash","session_id":"policy-test","tool_input":{"command":"dotnet build EventHub.slnx -c Release"}}'
$allowExecBuildPayload = '{"tool_name":"exec_command","session_id":"policy-test","tool_input":{"cmd":"dotnet build EventHub.slnx -c Release"}}'

Test-GuardAllowsOrDenies -Payload $denyEnvPayload -ShouldDeny $true -Label 'env write'
Test-GuardAllowsOrDenies -Payload $denyGeneratedPayload -ShouldDeny $true -Label 'generated write'
Test-GuardAllowsOrDenies -Payload $denyForcePushPayload -ShouldDeny $true -Label 'force push'
Test-GuardAllowsOrDenies -Payload $denyExecForcePushPayload -ShouldDeny $true -Label 'exec force push'
Test-GuardAllowsOrDenies -Payload $denyEnvReadPayload -ShouldDeny $true -Label 'env read'
Test-GuardAllowsOrDenies -Payload $denyEnvTypePayload -ShouldDeny $true -Label 'env type'
Test-GuardAllowsOrDenies -Payload $denyEnvCatPayload -ShouldDeny $true -Label 'env cat'
Test-GuardAllowsOrDenies -Payload $denySecretDirectoryReadPayload -ShouldDeny $true -Label 'secrets directory read'
Test-GuardAllowsOrDenies -Payload $denyPatchEnvPayload -ShouldDeny $true -Label 'apply_patch env write'
Test-GuardAllowsOrDenies -Payload $allowBuildPayload -ShouldDeny $false -Label 'dotnet build'
Test-GuardAllowsOrDenies -Payload $allowExecBuildPayload -ShouldDeny $false -Label 'exec dotnet build'

Test-VerifyPlanIncludesHarnessPolicy -Path '.codex/config.toml' -Label 'config change'
Test-VerifyPlanIncludesHarnessPolicy -Path 'web/AGENTS.md' -Label 'nested AGENTS change'

$telemetryPayload = '{"session_id":"policy-test","source":"startup"}'
$telemetryResult = Invoke-Hook -RelativePath '.codex/hooks/telemetry-session.ps1' -Payload $telemetryPayload
if ($null -ne $telemetryResult -and $telemetryResult.ExitCode -ne 0) {
    Add-Error "telemetry-session hook exited $($telemetryResult.ExitCode)"
}

$stopPayload = '{"session_id":"policy-test","stop_hook_active":true}'
$stopResult = Invoke-Hook -RelativePath '.codex/hooks/verify-on-stop.ps1' -Payload $stopPayload
if ($null -ne $stopResult -and $stopResult.ExitCode -ne 0) {
    Add-Error "verify-on-stop active recursion guard exited $($stopResult.ExitCode)"
}

$result = @{
    status = if ($errors.Count -eq 0) { 'passed' } else { 'failed' }
    errors = @($errors)
    timestamp = (Get-Date).ToUniversalTime().ToString('o')
}

if ($Json) {
    $result | ConvertTo-Json -Depth 6
}
else {
    Write-Host ''
    Write-Host 'EventHub harness policy validation' -ForegroundColor Cyan
    Write-Host "  status: $($result.status)"
    if ($errors.Count -gt 0) {
        Write-Host ''
        Write-Host 'Errors' -ForegroundColor Red
        foreach ($err in $errors) {
            Write-Host "  - $err" -ForegroundColor Red
        }
    }
}

if ($errors.Count -gt 0) { exit 1 }
exit 0
