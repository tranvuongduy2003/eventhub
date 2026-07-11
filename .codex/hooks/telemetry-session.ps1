# SessionStart hook: deep telemetry (metadata only).
#
# Records a session-boundary marker so a session's trace can be reconstructed and compared across
# harness versions (replayability). Metadata only - git branch + HEAD sha, no prompts or content, so
# no sensitive user data can leak. Appends one line to .codex/tmp/telemetry/<session>.jsonl (gitignored)
# and always exits 0 - telemetry must never block a session.

$ErrorActionPreference = 'SilentlyContinue'

try {
    $raw = [Console]::In.ReadToEnd()
    $sessionId = 'unknown'
    $source = ''
    if (-not [string]::IsNullOrWhiteSpace($raw)) {
        $payload = $raw | ConvertFrom-Json
        if ($payload.session_id) { $sessionId = $payload.session_id }
        if ($payload.source) { $source = $payload.source }
    }

    $repoRoot = (Resolve-Path -LiteralPath "$PSScriptRoot\..\..").Path
    Set-Location -LiteralPath $repoRoot
    $telemetryDir = Join-Path $repoRoot '.codex/tmp/telemetry'
    if (-not (Test-Path -LiteralPath $telemetryDir)) {
        New-Item -ItemType Directory -Path $telemetryDir -Force | Out-Null
    }

    $branch = (& git rev-parse --abbrev-ref HEAD 2>$null)
    $sha    = (& git rev-parse --short HEAD 2>$null)

    $record = [ordered]@{
        ts      = (Get-Date).ToUniversalTime().ToString('o')
        event   = 'session-start'
        session = $sessionId
        source  = $source
        branch  = if ($branch) { "$branch".Trim() } else { '' }
        sha     = if ($sha) { "$sha".Trim() } else { '' }
    }
    Add-Content -LiteralPath (Join-Path $telemetryDir "$sessionId.jsonl") `
        -Value ($record | ConvertTo-Json -Depth 3 -Compress) -Encoding utf8
}
catch {
    # Telemetry failure must never affect the session.
}

exit 0







