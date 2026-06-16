# PreCompact hook — backup transcript + structured notes before context compaction.

$ErrorActionPreference = 'Stop'

function Write-HookJson([hashtable]$Payload) {
    $Payload | ConvertTo-Json -Compress -Depth 10
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

$projectRoot = if ($env:CLAUDE_PROJECT_DIR) { $env:CLAUDE_PROJECT_DIR } else { (Get-Location).Path }
$notesDir = Join-Path $projectRoot '.claude\notes'
$backupRoot = Join-Path $notesDir 'backups'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'

$sessionDir = Join-Path $backupRoot "$stamp"
New-Item -ItemType Directory -Force -Path $sessionDir | Out-Null

$hookInput | ConvertTo-Json -Depth 10 | Set-Content -Path (Join-Path $sessionDir 'precompact-meta.json') -Encoding utf8

$progressPath = Join-Path $notesDir 'progress.md'
if (Test-Path -LiteralPath $progressPath) {
    Copy-Item -LiteralPath $progressPath -Destination (Join-Path $sessionDir 'progress.md')
}

$memoryDir = Join-Path $projectRoot '.claude\agent-memory'
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

$relDir = ".claude/notes/backups/$stamp"
Write-HookJson @{ user_message = "Pre-compact backup: $relDir" }
exit 0
