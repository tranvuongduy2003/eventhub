# PreToolUse (Bash) — block destructive or policy-violating commands.

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
    Deny-ShellHook -Reason "Verification gate active - fix lint/test failures before running shell commands. $reason"
}

$command = [string]$hookInput.tool_input.command
if (Test-DangerousShellCommand -Command $command) {
    $msg = Get-DangerousShellReason -Command $command
    Deny-ShellHook -Reason $msg
}

Allow-ShellHook
