# PreToolUse — hard guards + verify gate (deterministic enforcement layer).

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\lib\Use-HookIO.ps1"
. "$PSScriptRoot\lib\Use-VerifyGate.ps1"
. "$PSScriptRoot\lib\Use-GuardRules.ps1"

$hookInput = Read-HookInput -PipelineInput @($input)
if ($null -eq $hookInput) {
    Allow-Hook
}

$projectRoot = Get-ProjectRoot
$toolName = [string]$hookInput.tool_name

$gate = Get-VerifyGate -ProjectRoot $projectRoot
if ($gate -and $gate.blocked -and -not (Test-ToolAllowedWhenGated -ToolName $toolName)) {
    $reason = [string]$gate.reason
    Deny-Hook -Reason "Verification gate active - fix failing checks before using $toolName. $reason"
}

if ($toolName -eq 'Write' -or $toolName -eq 'Edit') {
    $filePath = [string]$hookInput.tool_input.file_path
    if (-not $filePath) {
        $filePath = [string]$hookInput.tool_input.path
    }
    if (Test-BlockedEditPath -Path $filePath) {
        $msg = Get-BlockedEditReason -Path $filePath
        Deny-Hook -Reason $msg
    }
}

if ($toolName -eq 'Bash') {
    $command = [string]$hookInput.tool_input.command
    if (Test-DangerousShellCommand -Command $command) {
        $msg = Get-DangerousShellReason -Command $command
        Deny-Hook -Reason $msg
    }
}

Allow-Hook
