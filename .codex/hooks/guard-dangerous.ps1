# PreToolUse hook: deterministic policy gate for destructive or unsafe operations.
$ErrorActionPreference = 'SilentlyContinue'

function Get-InputValue {
    param($Object, [string[]] $Names)
    if ($null -eq $Object) { return $null }
    foreach ($name in $Names) {
        if ($null -ne $Object -and $Object.PSObject.Properties.Name -contains $name) {
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

function Test-ProtectedPath {
    param([string] $Path)

    if ([string]::IsNullOrWhiteSpace($Path)) { return $null }

    $normalized = $Path.Replace('\', '/')
    $protectedPathPatterns = @(
        '(^|/)\.env($|\.|/)',
        '(^|/)secrets(/|$)',
        '(^|/)(id_rsa|id_dsa|id_ecdsa|id_ed25519)(\.pub)?$',
        '\.(pem|pfx|p12|key)$',
        '/Migrations/',
        '(^|/)web/src/generated/',
        '\.generated\.',
        '\.g\.ts$',
        '(^|/)\.codex/worktrees/'
    )
    foreach ($pattern in $protectedPathPatterns) {
        if ($normalized -match $pattern) {
            return $pattern
        }
    }

    return $null
}

function Write-DenyTelemetry {
    param([string] $Reason, [string] $SessionId)
    try {
        $repoRoot = (Resolve-Path -LiteralPath "$PSScriptRoot\..\..").Path
        $telemetryDir = Join-Path $repoRoot '.codex\tmp\telemetry'
        if (-not (Test-Path -LiteralPath $telemetryDir)) {
            New-Item -ItemType Directory -Path $telemetryDir -Force | Out-Null
        }
        $safeSessionId = if ([string]::IsNullOrWhiteSpace($SessionId)) { 'unknown' } else { $SessionId }
        $record = [ordered]@{
            ts = (Get-Date).ToUniversalTime().ToString('o')
            event = 'permission-deny'
            session = $safeSessionId
            reason = $Reason
        }
        Add-Content -LiteralPath (Join-Path $telemetryDir "$safeSessionId.jsonl") -Value ($record | ConvertTo-Json -Depth 5 -Compress) -Encoding UTF8
    }
    catch { }
}

function Deny {
    param([string] $Reason, [string] $SessionId)
    Write-DenyTelemetry -Reason $Reason -SessionId $SessionId
    @{
        hookSpecificOutput = @{
            hookEventName = 'PreToolUse'
            permissionDecision = 'deny'
            permissionDecisionReason = $Reason
        }
    } | ConvertTo-Json -Depth 5 -Compress | Write-Output
    exit 0
}

$raw = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }

try {
    $payload = $raw | ConvertFrom-Json
}
catch {
    exit 0
}

$toolName = [string](Get-InputValue -Object $payload -Names @('tool_name', 'toolName', 'tool'))
$toolInput = Get-InputValue -Object $payload -Names @('tool_input', 'toolInput')
$sessionId = [string](Get-InputValue -Object $payload -Names @('session_id', 'sessionId'))
if ($null -eq $toolInput) { $toolInput = New-Object psobject }

if ($toolName -in @('Write', 'Edit', 'MultiEdit', 'apply_patch')) {
    $paths = @()
    $filePath = [string](Get-InputValue -Object $toolInput -Names @('file_path', 'filePath', 'path'))
    if (-not [string]::IsNullOrWhiteSpace($filePath)) {
        $paths += $filePath
    }
    if ($toolName -eq 'apply_patch') {
        $paths += Get-PatchPaths $toolInput
    }

    foreach ($path in @($paths | Where-Object { $_ } | Sort-Object -Unique)) {
        $matchedPattern = Test-ProtectedPath $path
        if ($matchedPattern) {
            Deny -Reason "Writing to a protected path is blocked by policy (matched '$matchedPattern'): $path" -SessionId $sessionId
        }
    }
}

if ($toolName -in @('Bash', 'exec_command', 'functions.exec_command')) {
    $command = [string](Get-InputValue -Object $toolInput -Names @('command', 'cmd'))
    if (-not [string]::IsNullOrWhiteSpace($command)) {
        $rules = @(
            @{ Pattern = 'rm\s+-[a-zA-Z]*r[a-zA-Z]*f|rm\s+-[a-zA-Z]*f[a-zA-Z]*r'; Reason = 'recursive force delete' },
            @{ Pattern = 'Remove-Item[^\n]*-Recurse'; Reason = 'recursive Remove-Item' },
            @{ Pattern = '(^|\s|;|&|\|)sudo\s'; Reason = 'sudo privilege escalation' },
            @{ Pattern = '(^|\s|;|&|\|)curl\s'; Reason = 'curl network call' },
            @{ Pattern = '(^|\s|;|&|\|)wget\s'; Reason = 'wget network call' },
            @{ Pattern = '(^|\s|;|&|\|)(Invoke-WebRequest|Invoke-RestMethod|iwr|irm)\s'; Reason = 'PowerShell web request' },
            @{ Pattern = 'git\s+push[^\n]*--force|git\s+push[^\n]*-f(\s|$)'; Reason = 'git force push' },
            @{ Pattern = 'git\s+filter-branch|git\s+filter-repo|git\s+push[^\n]*--mirror'; Reason = 'git history rewrite' },
            @{ Pattern = '(^|[\s''"=:/\\])\.env(\.[A-Za-z0-9_.-]+)?($|[\s''";&|<>])'; Reason = 'reading or writing a .env secret file' },
            @{ Pattern = '(^|[\s''"=])(\.?[/\\])?secrets[/\\]'; Reason = 'reading or writing a secrets directory' },
            @{ Pattern = '(^|[\s''"=:/\\])(id_rsa|id_dsa|id_ecdsa|id_ed25519)(\.pub)?($|[\s''";&|<>])'; Reason = 'reading or writing a private key file' },
            @{ Pattern = '\.(pem|pfx|p12|key)($|[\s''";&|<>])'; Reason = 'reading or writing a private key or certificate file' }
        )
        foreach ($rule in $rules) {
            if ($command -match $rule.Pattern) {
                Deny -Reason "Command blocked by policy - $($rule.Reason): $command" -SessionId $sessionId
            }
        }
    }
}

exit 0





