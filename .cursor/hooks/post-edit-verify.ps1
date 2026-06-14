# afterFileEdit — format/lint + affected tests; sets verify gate on failure (hard block via preToolUse).

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\lib\hook-io.ps1"
. "$PSScriptRoot\lib\verify-gate.ps1"
. "$PSScriptRoot\lib\guard-rules.ps1"
. "$PSScriptRoot\lib\verify-runner.ps1"

$hookInput = Read-HookInput
if ($null -eq $hookInput) {
    exit 0
}

$projectRoot = Get-VerifyProjectRoot
$filePath = [string]$hookInput.file_path

if (-not (Test-ShouldVerifyFile -FilePath $filePath -ProjectRoot $projectRoot)) {
    Clear-VerifyGate -ProjectRoot $projectRoot
    exit 0
}

$plan = Get-AffectedPlan -ProjectRoot $projectRoot -FilePath $filePath
if ($null -eq $plan -or $plan.skip -or -not $plan.steps -or $plan.steps.Count -eq 0) {
    Clear-VerifyGate -ProjectRoot $projectRoot
    exit 0
}

$errors = Invoke-VerificationSteps -ProjectRoot $projectRoot -Steps $plan.steps
if ($errors.Count -gt 0) {
    $reason = ($errors -join ' | ')
    if ($reason.Length -gt 1200) {
        $reason = $reason.Substring(0, 1200) + '...'
    }
    Set-VerifyGate -ProjectRoot $projectRoot -Reason $reason -FilePath $filePath
    exit 0
}

Clear-VerifyGate -ProjectRoot $projectRoot
exit 0
