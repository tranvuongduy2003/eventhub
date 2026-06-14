# beforeShellExecution — block destructive or policy-violating commands.

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\lib\hook-io.ps1"
. "$PSScriptRoot\lib\verify-gate.ps1"
. "$PSScriptRoot\lib\guard-rules.ps1"

$hookInput = Read-HookInput
if ($null -eq $hookInput) {
    Allow-ShellHook
}

$projectRoot = Get-ProjectRoot
$gate = Get-VerifyGate -ProjectRoot $projectRoot
if ($gate -and $gate.blocked) {
    $reason = [string]$gate.reason
    Deny-ShellHook `
        -UserMessage 'Verification gate active - fix lint/test failures before running shell commands.' `
        -AgentMessage "Harness verify gate: $reason Edit files to fix, then save to re-run post-edit verification."
}

$command = [string]$hookInput.command
if (Test-DangerousShellCommand -Command $command) {
    $msg = Get-DangerousShellReason -Command $command
    Deny-ShellHook -UserMessage $msg -AgentMessage $msg
}

Allow-ShellHook
