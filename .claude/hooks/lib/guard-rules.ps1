# Deterministic guard rules — paths and shell commands the agent must not touch.

function Test-BlockedEditPath {
    param([string]$Path)
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }
    $normalized = $Path -replace '\\', '/'

    $blockedPatterns = @(
        '/web/src/generated/',
        '/contracts/openapi/.build/',
        '/node_modules/',
        '/bin/',
        '/obj/',
        '/.env',
        '/.mcp.json'
    )

    foreach ($pattern in $blockedPatterns) {
        if ($normalized -match [regex]::Escape($pattern)) {
            return $true
        }
    }
    return $false
}

function Get-BlockedEditReason {
    param([string]$Path)
    $normalized = $Path -replace '\\', '/'
    if ($normalized -match '/web/src/generated/') {
        return 'web/src/generated/ is codegen output - run yarn api:codegen instead of editing by hand.'
    }
    if ($normalized -match '/contracts/openapi/.build/') {
        return 'contracts/openapi/.build/ is build output - edit contracts/openapi/api.v1.yaml or run api:export.'
    }
    if ($normalized -match '/\.env|/\.mcp\.json') {
        return 'Secrets and local MCP config must not be edited by the agent.'
    }
    return 'This path is protected by the agent harness.'
}

function Test-DangerousShellCommand {
    param([string]$Command)
    if ([string]::IsNullOrWhiteSpace($Command)) {
        return $false
    }
    $patterns = @(
        'rm\s+-rf',
        'Remove-Item\s+.*-Recurse\s+-Force',
        'git\s+push\s+.*--force',
        'git\s+push\s+-f\b',
        'git\s+reset\s+--hard',
        'git\s+clean\s+-fd',
        'git\s+config\s+',
        'npm\s+install',
        'npm\s+i\b'
    )
    foreach ($pattern in $patterns) {
        if ($Command -match $pattern) {
            return $true
        }
    }
    return $false
}

function Get-DangerousShellReason {
    param([string]$Command)
    if ($Command -match 'rm\s+-rf|Remove-Item\s+.*-Recurse\s+-Force') {
        return 'Destructive delete blocked by harness - use targeted file edits.'
    }
    if ($Command -match 'git\s+push\s+.*--force|git\s+push\s+-f\b|git\s+reset\s+--hard|git\s+clean\s+-fd') {
        return 'Destructive git operation blocked - see user git safety rules.'
    }
    if ($Command -match 'git\s+config\s+') {
        return 'git config changes are blocked.'
    }
    if ($Command -match 'npm\s+install|npm\s+i\b') {
        return 'web/ uses Yarn only - use yarn --cwd web install.'
    }
    return 'Command blocked by agent harness.'
}

function Test-ShouldVerifyFile {
    param(
        [string]$FilePath,
        [string]$ProjectRoot
    )
    if ([string]::IsNullOrWhiteSpace($FilePath)) {
        return $false
    }

    $rel = $FilePath
    if ($FilePath.StartsWith($ProjectRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        $rel = $FilePath.Substring($ProjectRoot.Length).TrimStart('\', '/')
    }
    $rel = $rel -replace '\\', '/'

    $skipPatterns = @(
        '^\.claude/',
        '^docs/',
        '^\.github/',
        '^README\.md$',
        '\.md$',
        '\.json$',
        '\.yaml$',
        '\.yml$',
        '\.gitignore$',
        '/Migrations/',
        'web/src/generated/'
    )

    foreach ($pattern in $skipPatterns) {
        if ($rel -match $pattern) {
            return $false
        }
    }

    return ($rel -match '\.(cs|tsx?|jsx?|css)$|^tests/|^e2e/')
}
