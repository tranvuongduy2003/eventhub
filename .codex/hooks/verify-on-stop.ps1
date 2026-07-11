# Stop hook: verify the session left the working tree in a clean state. It always
# runs a fast static scan of the changed source files for leftover debug statements,
# and - when CODEX_STOP_VERIFY_BUILD is set to '1' - additionally runs the relevant
# build/typecheck for the areas that changed.
#
# It does not fix anything; it reports. When it finds a problem it writes the details
# to stderr and exits 2, which surfaces them to the agent so it can address them
# before finishing. Otherwise it exits 0.
#
# The heavy build/typecheck is OFF by default because this hook fires on every turn
# end; a multi-minute build each time would make the session unusable. Turn it on for
# a focused run (for example inside $cook) with:  $env:CODEX_STOP_VERIFY_BUILD = '1'

$ErrorActionPreference = 'SilentlyContinue'

# Read the hook payload. When Codex is already continuing because of a prior Stop
# hook block, `stop_hook_active` is true - short-circuit to exit 0 so a persistent
# finding (e.g. a debug line the agent deliberately keeps) can never loop forever.
$sessionId = 'unknown'
try {
    $raw = [Console]::In.ReadToEnd()
    if (-not [string]::IsNullOrWhiteSpace($raw)) {
        $payload = $raw | ConvertFrom-Json
        if ($payload.session_id) { $sessionId = $payload.session_id }
        if ($payload.stop_hook_active) { exit 0 }
    }
}
catch {
    # A malformed payload must not block the session - fail open.
    exit 0
}

$repoRoot = (Resolve-Path -LiteralPath "$PSScriptRoot\..\..").Path
Set-Location -LiteralPath $repoRoot

# Deep telemetry: record a metadata-only end-of-turn verify summary so $harness-review can measure
# verification strength and recovery. Never logs file contents or prompts.
function Write-VerifyTelemetry([int]$problemCount, [bool]$heavy, [int]$changedCount, [string[]]$Warnings) {
    try {
        $telemetryDir = Join-Path $repoRoot '.codex/tmp/telemetry'
        if (-not (Test-Path -LiteralPath $telemetryDir)) {
            New-Item -ItemType Directory -Path $telemetryDir -Force | Out-Null
        }
        $record = [ordered]@{
            ts           = (Get-Date).ToUniversalTime().ToString('o')
            event        = 'verify-on-stop'
            session      = $sessionId
            changedFiles = $changedCount
            heavySensors = $heavy
            problems     = $problemCount
        }
        if ($Warnings -and $Warnings.Count -gt 0) {
            $record['heavySensorWarnings'] = @($Warnings)
        }
        Add-Content -LiteralPath (Join-Path $telemetryDir "$sessionId.jsonl") `
            -Value ($record | ConvertTo-Json -Depth 3 -Compress) -Encoding utf8
    }
    catch { }
}

$problems = [System.Collections.Generic.List[string]]::new()

# --- Determine changed source files (tracked changes + untracked). ---
$changed = @()
$changed += (& git diff --name-only HEAD 2>$null)
$changed += (& git ls-files --others --exclude-standard 2>$null)
$changed = $changed | Where-Object { $_ } | Sort-Object -Unique

$backendChanged  = $changed | Where-Object { $_ -like 'src/*' -and $_ -like '*.cs' }
$webChanged = $changed | Where-Object { $_ -like 'web/*' -and ($_ -like '*.ts' -or $_ -like '*.tsx') }
$e2eChanged      = $changed | Where-Object { $_ -like 'e2e/*' -and ($_ -like '*.ts' -or $_ -like '*.tsx') }

$stopWarnings = [System.Collections.Generic.List[string]]::new()
if ($env:CODEX_STOP_VERIFY_BUILD -ne '1') {
    if (@($changed).Count -gt 50) {
        $stopWarnings.Add('large changed surface without heavy sensors') | Out-Null
    }

    $sensitiveChanged = $changed | Where-Object {
        $_ -eq 'AGENTS.md' -or
        $_ -like '*/AGENTS.md' -or
        $_ -like '.codex/config.toml' -or
        $_ -like '.codex/hooks/*' -or
        $_ -like '.codex/hooks.json' -or
        $_ -like 'scripts/agent/*' -or
        $_ -like '.agents/skills/*' -or
        $_ -like 'src/Api/*' -or
        $_ -like 'src/Application/*' -or
        $_ -like 'src/Infrastructure/*'
    }
    if ($sensitiveChanged) {
        $stopWarnings.Add('sensitive paths changed without heavy sensors') | Out-Null
    }
}

# --- Fast static scan: leftover debug statements in changed source files. ---
foreach ($file in $changed) {
    if (-not (Test-Path -LiteralPath $file)) { continue }
    if ($file -like '.agents/skills/*/scripts/*' -or $file -like '.agents\skills\*\scripts\*') { continue }
    $extension = [System.IO.Path]::GetExtension($file).ToLowerInvariant()
    if ($extension -notin @('.cs', '.ts', '.tsx')) { continue }
    $debugHits = Select-String -LiteralPath $file -Pattern 'Console\.WriteLine|console\.(log|debug)|debugger;' 2>$null
    foreach ($hit in $debugHits) {
        $problems.Add("Leftover debug statement: ${file}:$($hit.LineNumber) -> $($hit.Line.Trim())")
    }
}

# --- Optional heavy verification (opt-in). ---
if ($env:CODEX_STOP_VERIFY_BUILD -eq '1') {
    if ($backendChanged) {
        $buildOutput = & dotnet build EventHub.slnx --nologo 2>&1
        if ($LASTEXITCODE -ne 0) {
            $problems.Add("Backend build failed (dotnet build EventHub.slnx):`n$($buildOutput | Select-Object -Last 20 | Out-String)")
        }
    }
    if ($webChanged) {
        $typecheckOutput = & yarn --cwd web build 2>&1
        if ($LASTEXITCODE -ne 0) {
            $problems.Add("Frontend build failed (yarn --cwd web build):`n$($typecheckOutput | Select-Object -Last 20 | Out-String)")
        }
    }
    if ($e2eChanged) {
        $checkOutput = & yarn --cwd e2e test 2>&1
        if ($LASTEXITCODE -ne 0) {
            $problems.Add("E2E test failed (yarn --cwd e2e test):`n$($checkOutput | Select-Object -Last 20 | Out-String)")
        }
    }
}

Write-VerifyTelemetry $problems.Count ($env:CODEX_STOP_VERIFY_BUILD -eq '1') @($changed).Count @($stopWarnings)

if ($problems.Count -gt 0) {
    [Console]::Error.WriteLine("verify-on-stop found issues to resolve before finishing:")
    foreach ($problem in $problems) {
        [Console]::Error.WriteLine("  - $problem")
    }
    exit 2
}

exit 0







