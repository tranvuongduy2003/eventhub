# Stop — Reflexion anchor: block "done" until objective checks pass.

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\lib\hook-io.ps1"
. "$PSScriptRoot\lib\verify-gate.ps1"
. "$PSScriptRoot\lib\verify-runner.ps1"

function Send-StopBlock {
    param([string]$Message)
    Write-HookJson @{
        decision = "block"
        reason   = $Message
    }
    exit 0
}

$hookInput = Read-HookInput
$projectRoot = Get-VerifyProjectRoot

$gate = Get-VerifyGate -ProjectRoot $projectRoot
if ($gate -and $gate.blocked) {
    $reason = [string]$gate.reason
    $file = [string]$gate.file
    Send-StopBlock "Stop gate: post-edit verification still failing. Fix and save before declaring done.`n`nFile: $file`n`n$reason"
}

$errors = Invoke-StopVerification -ProjectRoot $projectRoot
if ($errors.Count -gt 0) {
    $detail = ($errors -join "`n- ")
    if ($detail.Length -gt 1500) {
        $detail = $detail.Substring(0, 1500) + '...'
    }
    Send-StopBlock "Stop gate: objective checks failed - not done yet. Fix the issues below, then continue.`n`n- $detail`n`nRun locally: .\evals\run.ps1 -Layer harness; dotnet test; yarn --cwd web exec tsc -b --noEmit"
}

Write-HookJson @{
    decision = "allow"
}
exit 0
