# PreCompact hook — backup transcript + structured notes before context compaction.
# Observational only; cannot block compaction. See context-memory.md and .opencode/notes/

$ErrorActionPreference = 'Stop'

function Write-HookJson([hashtable]$Payload) {
    $Payload | ConvertTo-Json -Compress
}

$raw = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($raw)) {
    Write-HookJson @{}
    exit 0
}

try {
    $hookInput = $raw | ConvertFrom-Json
}
catch {
    Write-HookJson @{ user_message = 'PreCompact backup: invalid hook input JSON.' }
    exit 0
}

$projectRoot = if ($env:OPENCODE_PROJECT_DIR) {
    $env:OPENCODE_PROJECT_DIR
}
elseif ($env:CURSOR_PROJECT_DIR) {
    $env:CURSOR_PROJECT_DIR
}
else {
    (Get-Location).Path
}
$notesDir = Join-Path $projectRoot '.opencode\notes'
$backupRoot = Join-Path $notesDir 'backups'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'

$convId = [string]$hookInput.conversation_id
$convShort = if ($convId.Length -ge 8) { $convId.Substring(0, 8) } else { 'unknown' }
$sessionDir = Join-Path $backupRoot "$stamp-$convShort"

New-Item -ItemType Directory -Force -Path $sessionDir | Out-Null

$hookInput | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $sessionDir 'precompact-meta.json') -Encoding utf8

$transcriptPath = [string]$hookInput.transcript_path
if ([string]::IsNullOrWhiteSpace($transcriptPath) -and $env:CURSOR_TRANSCRIPT_PATH) {
    $transcriptPath = $env:CURSOR_TRANSCRIPT_PATH
}

if (-not [string]::IsNullOrWhiteSpace($transcriptPath) -and (Test-Path -LiteralPath $transcriptPath)) {
    Copy-Item -LiteralPath $transcriptPath -Destination (Join-Path $sessionDir 'transcript.jsonl')
}

$progressPath = Join-Path $notesDir 'progress.md'
if (Test-Path -LiteralPath $progressPath) {
    Copy-Item -LiteralPath $progressPath -Destination (Join-Path $sessionDir 'progress.md')
}

$memoryDir = Join-Path $projectRoot '.opencode\agent-memory'
if (Test-Path -LiteralPath $memoryDir) {
    $memBackup = Join-Path $sessionDir 'agent-memory'
    New-Item -ItemType Directory -Force -Path $memBackup | Out-Null
    Get-ChildItem -LiteralPath $memoryDir -Filter '*.md' -File |
        Where-Object { $_.Name -ne 'README.md' } |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $memBackup }
}

if (Test-Path -LiteralPath $backupRoot) {
    $allBackups = @(Get-ChildItem -LiteralPath $backupRoot -Directory | Sort-Object Name -Descending)
    if ($allBackups.Count -gt 20) {
        $allBackups | Select-Object -Skip 20 | Remove-Item -Recurse -Force
    }
}

$trigger = if ($hookInput.trigger) { [string]$hookInput.trigger } else { 'unknown' }
$pct = if ($null -ne $hookInput.context_usage_percent) { [string]$hookInput.context_usage_percent } else { '?' }
$relDir = ".opencode/notes/backups/$stamp-$convShort"

Write-HookJson @{ user_message = "Pre-compact backup: $relDir ($trigger, ${pct}% context used)." }
exit 0
