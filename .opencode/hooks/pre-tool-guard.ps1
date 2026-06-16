# PreToolUse — hard guards + verify gate (deterministic enforcement layer).

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\lib\hook-io.ps1"
. "$PSScriptRoot\lib\verify-gate.ps1"
. "$PSScriptRoot\lib\guard-rules.ps1"

$hookInput = Read-HookInput
if ($null -eq $hookInput) {
    Allow-Hook
}

$projectRoot = Get-ProjectRoot
$toolName = [string]$hookInput.tool_name

$gate = Get-VerifyGate -ProjectRoot $projectRoot
if ($gate -and $gate.blocked -and -not (Test-ToolAllowedWhenGated -ToolName $toolName)) {
    $reason = [string]$gate.reason
    Deny-Hook `
        -UserMessage "Verification gate active - fix failing checks before using $toolName." `
        -AgentMessage "Harness verify gate is active: $reason Fix the issue (Write/Read allowed), then save again to re-run verification."
}

if ($toolName -eq 'Write') {
    $filePath = [string]$hookInput.tool_input.file_path
    if (-not $filePath) {
        $filePath = [string]$hookInput.tool_input.path
    }
    if (Test-BlockedEditPath -Path $filePath) {
        $msg = Get-BlockedEditReason -Path $filePath
        Deny-Hook -UserMessage $msg -AgentMessage $msg
    }
}

if ($toolName -eq 'Shell') {
    $command = [string]$hookInput.tool_input.command
    if (Test-DangerousShellCommand -Command $command) {
        $msg = Get-DangerousShellReason -Command $command
        Deny-Hook -UserMessage $msg -AgentMessage $msg
    }
}

Allow-Hook
