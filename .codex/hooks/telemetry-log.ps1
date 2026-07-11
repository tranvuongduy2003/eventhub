# PostToolUse hook: deep telemetry (metadata/results ONLY).
#
# Deep telemetry is the substrate for measuring and improving the harness itself. This records the
# *trajectory* of a session as structured JSONL so `$harness-review` (the harness-doctor subagent)
# can diagnose failure modes from evidence instead of guesswork.
#
# sensitive data policy SAFETY (human decision #1): this logs ONLY metadata and results - tool name, file path,
# the command string (approved as metadata), and coarse outcome. It NEVER logs prompts, file
# contents, or model output, so no sensitive user data can leak into the trace.
#
# It appends one line to .codex/tmp/telemetry/<session>.jsonl (gitignored) and always exits 0 -
# telemetry must never block or slow a session.

$ErrorActionPreference = 'SilentlyContinue'

function Get-InputValue {
    param($Object, [string[]] $Names)
    if ($null -eq $Object) { return $null }
    foreach ($name in $Names) {
        if ($Object.PSObject.Properties.Name -contains $name) {
            $value = $Object.$name
            if ($null -ne $value) { return $value }
        }
    }
    return $null
}

function Get-TextValue {
    param($Value)
    if ($null -eq $Value) { return '' }
    if ($Value -is [string]) { return $Value }

    $text = Get-InputValue -Object $Value -Names @('patch', 'input', 'text', 'content', 'value')
    if ($null -ne $text) { return [string]$text }

    return ''
}

function Get-PatchPaths {
    param($ToolInput)

    $patchText = Get-TextValue $ToolInput
    if ([string]::IsNullOrWhiteSpace($patchText)) { return @() }

    $paths = [System.Collections.Generic.List[string]]::new()
    foreach ($match in [regex]::Matches($patchText, '(?m)^\*\*\* (?:Add File|Update File|Delete File|Move to):\s+(.+?)\s*$')) {
        $path = $match.Groups[1].Value.Trim()
        if (-not [string]::IsNullOrWhiteSpace($path)) {
            $paths.Add($path) | Out-Null
        }
    }

    return @($paths | Sort-Object -Unique)
}

function Get-Area {
    param([string] $Path)

    $normalized = $Path.Replace('\', '/')
    if ($normalized -match '(^|/)src/') { return 'backend' }
    if ($normalized -match '(^|/)web/') { return 'web' }
    if ($normalized -match '(^|/)e2e/') { return 'e2e' }
    if ($normalized -match '(^|/)(\.codex|\.agents|docs|scripts/agent|AGENTS\.md)') { return 'harness' }
    return 'other'
}

try {
    $raw = [Console]::In.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }
    $payload = $raw | ConvertFrom-Json

    $repoRoot = (Resolve-Path -LiteralPath "$PSScriptRoot\..\..").Path
    $telemetryDir = Join-Path $repoRoot '.codex/tmp/telemetry'
    if (-not (Test-Path -LiteralPath $telemetryDir)) {
        New-Item -ItemType Directory -Path $telemetryDir -Force | Out-Null
    }

    $sessionId = if ($payload.session_id) { $payload.session_id } else { 'unknown' }
    $toolName  = [string](Get-InputValue -Object $payload -Names @('tool_name', 'toolName', 'tool'))
    $toolInput = Get-InputValue -Object $payload -Names @('tool_input', 'toolInput')
    if ($null -eq $toolInput) { $toolInput = New-Object psobject }

    # Build a metadata-only record. Start minimal; add fields only when reliably present.
    $record = [ordered]@{
        ts      = (Get-Date).ToUniversalTime().ToString('o')
        event   = 'tool'
        session = $sessionId
        tool    = $toolName
    }

    switch ($toolName) {
        { $_ -in 'Write', 'Edit', 'MultiEdit', 'apply_patch' } {
            $paths = @()
            $filePath = [string](Get-InputValue -Object $toolInput -Names @('file_path', 'filePath', 'path'))
            if (-not [string]::IsNullOrWhiteSpace($filePath)) {
                $paths += $filePath
            }
            if ($toolName -eq 'apply_patch') {
                $paths += Get-PatchPaths $toolInput
            }

            $paths = @($paths | Where-Object { $_ } | Sort-Object -Unique)
            if ($paths.Count -eq 1) {
                $normalized = $paths[0].Replace('\', '/')
                $record['fileExtension'] = [System.IO.Path]::GetExtension($normalized).ToLowerInvariant()
                $record['area'] = Get-Area $normalized
            }
            elseif ($paths.Count -gt 1) {
                $record['fileCount'] = $paths.Count
                $record['fileExtensions'] = @($paths | ForEach-Object { [System.IO.Path]::GetExtension($_.Replace('\', '/')).ToLowerInvariant() } | Sort-Object -Unique)
                $record['areas'] = @($paths | ForEach-Object { Get-Area $_ } | Sort-Object -Unique)
            }
        }
        { $_ -in 'Bash', 'exec_command', 'functions.exec_command' } {
            $command = Get-InputValue -Object $toolInput -Names @('command', 'cmd')
            if (-not [string]::IsNullOrWhiteSpace($command)) {
                # Command string is metadata (human decision #1). Truncate to keep lines small.
                $flat = ($command -replace '\s+', ' ').Trim()
                if ($flat.Length -gt 300) { $flat = $flat.Substring(0, 300) + '...' }
                $record['command'] = $flat
            }
        }
        'Task' {
            # Subagent delegation - record which agent, not its prompt.
            if ($payload.tool_input.subagent_type) { $record['subagent'] = $payload.tool_input.subagent_type }
        }
    }

    # Coarse outcome, when the tool response exposes an error flag. Never log the response body.
    if ($null -ne $payload.tool_response) {
        $resp = $payload.tool_response
        if ($resp.PSObject.Properties.Name -contains 'interrupted' -and $resp.interrupted) { $record['outcome'] = 'interrupted' }
        elseif ($resp.PSObject.Properties.Name -contains 'is_error' -and $resp.is_error)   { $record['outcome'] = 'error' }
    }

    $line = ($record | ConvertTo-Json -Depth 4 -Compress)
    $file = Join-Path $telemetryDir "$sessionId.jsonl"
    Add-Content -LiteralPath $file -Value $line -Encoding utf8
}
catch {
    # Telemetry failure must never affect the session.
}

exit 0







