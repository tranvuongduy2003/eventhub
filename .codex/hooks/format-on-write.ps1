# PostToolUse hook (Write|Edit|MultiEdit): format the file that was just written,
# routing by extension to the correct formatter. A formatting failure must never
# stop the session, so this script always exits 0.

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
    foreach ($match in [regex]::Matches($patchText, '(?m)^\*\*\* (?:Add File|Update File|Move to):\s+(.+?)\s*$')) {
        $path = $match.Groups[1].Value.Trim()
        if (-not [string]::IsNullOrWhiteSpace($path)) {
            $paths.Add($path) | Out-Null
        }
    }

    return @($paths | Sort-Object -Unique)
}

function Format-Path {
    param([string] $FilePath, [string] $RepoRoot)

    if ([string]::IsNullOrWhiteSpace($FilePath)) { return }
    if (-not (Test-Path -LiteralPath $FilePath)) { return }

    $fullPath = (Resolve-Path -LiteralPath $FilePath).Path
    if (-not $fullPath.StartsWith($RepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) { return }

    $relative = $fullPath.Substring($RepoRoot.Length).TrimStart('\', '/').Replace('\', '/')
    $extension = [System.IO.Path]::GetExtension($fullPath).ToLowerInvariant()

    switch ($extension) {
        '.cs' {
            # Scope the format to the single changed file to keep it fast.
            & dotnet format EventHub.slnx --include $relative 2>&1 | Out-Null
        }
        { $_ -in '.ts', '.tsx' } {
            if ($relative -like 'web/*') {
                $webRelative = $relative.Substring(4)
                & yarn --cwd web prettier --write $webRelative 2>&1 | Out-Null
            }
            elseif ($relative -like 'e2e/*') {
                $e2eRelative = "../$relative"
                & yarn --cwd web prettier --write $e2eRelative 2>&1 | Out-Null
            }
        }
        default { }
    }
}

try {
    $raw = [Console]::In.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }
    $payload = $raw | ConvertFrom-Json

    $repoRoot = (Resolve-Path -LiteralPath "$PSScriptRoot\..\..").Path
    $toolName = [string](Get-InputValue -Object $payload -Names @('tool_name', 'toolName', 'tool'))
    $toolInput = Get-InputValue -Object $payload -Names @('tool_input', 'toolInput')
    if ($null -eq $toolInput) { $toolInput = New-Object psobject }

    $paths = @()
    $filePath = [string](Get-InputValue -Object $toolInput -Names @('file_path', 'filePath', 'path'))
    if (-not [string]::IsNullOrWhiteSpace($filePath)) {
        $paths += $filePath
    }
    if ($toolName -eq 'apply_patch') {
        $paths += Get-PatchPaths $toolInput
    }

    foreach ($path in @($paths | Where-Object { $_ } | Sort-Object -Unique)) {
        Format-Path -FilePath $path -RepoRoot $repoRoot
    }
}
catch {
    # Never let a formatting problem block the session.
}

exit 0







