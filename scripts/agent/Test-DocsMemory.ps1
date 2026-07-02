#Requires -Version 5.1
<#
.SYNOPSIS
  Validate the docs/ Obsidian vault as EventHub long-term knowledge memory.
#>

param(
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$docsRoot = Join-Path $repoRoot 'docs'
$errors = New-Object System.Collections.Generic.List[string]

function Add-Error {
    param([string]$Message)
    $script:errors.Add($Message) | Out-Null
}

function Test-RequiredFile {
    param([string]$RelativePath)
    $path = Join-Path $repoRoot ($RelativePath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        Add-Error "Missing required file: $RelativePath"
        return $false
    }
    return $true
}

function Test-ForbiddenFile {
    param([string]$RelativePath)
    $path = Join-Path $repoRoot ($RelativePath -replace '/', '\')
    if (Test-Path -LiteralPath $path) {
        Add-Error "Forbidden legacy file still exists: $RelativePath"
    }
}

function Read-JsonFile {
    param([string]$RelativePath)
    if (-not (Test-RequiredFile $RelativePath)) { return $null }
    try {
        return Get-Content -LiteralPath (Join-Path $repoRoot ($RelativePath -replace '/', '\')) -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        Add-Error "Invalid JSON in $RelativePath`: $($_.Exception.Message)"
        return $null
    }
}

function ConvertTo-WikiTargetSet {
    $targets = @{}
    Get-ChildItem -LiteralPath $docsRoot -Recurse -Filter '*.md' | ForEach-Object {
        $relative = $_.FullName.Substring($docsRoot.Length + 1).Replace('\', '/')
        $withoutExtension = $relative -replace '\.md$', ''
        $base = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
        $targets[$withoutExtension] = $true
        $targets[$base] = $true
    }
    return $targets
}

function Test-WikiLinks {
    $targets = ConvertTo-WikiTargetSet
    Get-ChildItem -LiteralPath $docsRoot -Recurse -Filter '*.md' | ForEach-Object {
        $relative = $_.FullName.Substring($docsRoot.Length + 1).Replace('\', '/')
        $text = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
        foreach ($match in [regex]::Matches($text, '\[\[([^\]|#]+)(?:#[^\]|]+)?(?:\|[^\]]+)?\]\]')) {
            $link = $match.Groups[1].Value
            if (-not $targets.ContainsKey($link)) {
                Add-Error "Broken wiki link in docs/$relative -> $link"
            }
        }
    }
}

function Test-MarkdownLinks {
    Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Force -Include '*.md', '*.toml', '*.json' | ForEach-Object {
        $relative = $_.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
        if ($relative.StartsWith('.git/') -or
            $relative.StartsWith('bin/') -or
            $relative.StartsWith('obj/') -or
            $relative.StartsWith('node_modules/') -or
            $relative.Contains('/node_modules/') -or
            $relative.StartsWith('evals/results/') -or
            $relative.StartsWith('.codex/state/')) {
            return
        }

        try {
            $text = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
        }
        catch {
            return
        }
        if ($null -eq $text) {
            return
        }

        foreach ($match in [regex]::Matches($text, '\[[^\]]+\]\(([^)]+\.md)(?:#[^)]+)?\)')) {
            $target = $match.Groups[1].Value
            if ($target -match '^[a-z]+://') {
                continue
            }

            $candidate = Join-Path $_.DirectoryName ($target -replace '/', '\')
            if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
                Add-Error "Broken markdown link in $relative -> $target"
            }
        }
    }
}

function Test-NoLegacyDocReferences {
    $legacyDocNames = 'prd|features|ddd|technical'
    $legacyHarnessDir = 'harness'
    $legacyHarnessDocs = 'architecture|operational-policies'
    $legacySpecsDir = 'specs'
    $legacyPattern = "(?i)docs[/\\]($legacyDocNames)\.md|docs[/\\]$legacyHarnessDir[/\\]($legacyHarnessDocs)\.md|docs[/\\]$legacySpecsDir[/\\]|(?<!source[/\\])($legacyDocNames)\.md|(?<!source[/\\])$legacyHarnessDir[/\\]($legacyHarnessDocs)\.md|\[\[$legacySpecsDir/"
    $excludedFragments = @(
        '/.git/',
        '/bin/',
        '/obj/',
        '/node_modules/',
        '/evals/results/',
        '/.codex/state/'
    )

    Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Force | ForEach-Object {
        $relative = $_.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
        foreach ($fragment in $excludedFragments) {
            $normalizedFragment = $fragment.Trim('/')
            if ($relative -eq $normalizedFragment -or
                $relative.StartsWith("$normalizedFragment/") -or
                $relative.Contains("/$normalizedFragment/")) {
                return
            }
        }

        if ($relative -eq 'scripts/agent/Test-DocsMemory.ps1') {
            return
        }

        try {
            $text = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
        }
        catch {
            return
        }

        if ($text -match $legacyPattern) {
            Add-Error "Legacy docs reference found in $relative"
        }
    }
}

function Test-FileContains {
    param(
        [string]$RelativePath,
        [string[]]$Needles
    )
    if (-not (Test-RequiredFile $RelativePath)) { return }
    $text = Get-Content -LiteralPath (Join-Path $repoRoot ($RelativePath -replace '/', '\')) -Raw -Encoding UTF8
    foreach ($needle in $Needles) {
        if ($text -notmatch [regex]::Escape($needle)) {
            Add-Error "$RelativePath missing required text: $needle"
        }
    }
}

function Test-GraphMapping {
    param(
        [string]$Prefix,
        [string]$ExpectedCommand
    )
    $graph = Read-JsonFile '.graph/index.json'
    if ($null -eq $graph) { return }
    $layer = $graph.layers.$Prefix
    if ($null -eq $layer) {
        Add-Error ".graph/index.json missing layer mapping: $Prefix"
        return
    }
    if ($layer.postEditAction -ne 'test') {
        Add-Error ".graph/index.json layer $Prefix must use postEditAction test"
    }
    if ($layer.testCommand -ne $ExpectedCommand) {
        Add-Error ".graph/index.json layer $Prefix testCommand expected '$ExpectedCommand', got '$($layer.testCommand)'"
    }
}

$requiredFiles = @(
    'docs/README.md',
    'docs/.gitignore',
    'docs/.obsidian/app.json',
    'docs/.obsidian/appearance.json',
    'docs/.obsidian/core-plugins.json',
    'docs/.obsidian/graph.json',
    'docs/.obsidian/templates.json',
    'docs/_memory/source/README.md',
    'docs/_memory/source/product-requirements.md',
    'docs/_memory/source/feature-specification.md',
    'docs/_memory/source/domain-model-specification.md',
    'docs/_memory/source/technical-design.md',
    'docs/_memory/source/harness-architecture.md',
    'docs/_memory/source/harness-operational-policies.md',
    'docs/_memory/specs/README.md',
    'docs/_memory/long-term-memory-operating-model.md',
    'docs/_memory/source-of-truth-map.md',
    'docs/_memory/agent-retrieval-guide.md',
    'docs/_memory/mocs/product-intent.md',
    'docs/_memory/mocs/feature-roadmap.md',
    'docs/_memory/mocs/domain-model.md',
    'docs/_memory/mocs/technical-architecture.md',
    'docs/_memory/mocs/harness-memory.md',
    'docs/_memory/glossary/ubiquitous-language.md',
    'docs/_memory/glossary/decision-log.md',
    'docs/_memory/glossary/architecture-invariants.md',
    'docs/_memory/assets/.gitkeep',
    'docs/_memory/inbox/README.md',
    'docs/_memory/templates/source-note.md',
    'docs/_memory/templates/decision-note.md',
    'docs/_memory/templates/feature-memory-note.md'
)

foreach ($file in $requiredFiles) {
    Test-RequiredFile $file | Out-Null
}

$legacyDocumentNames = @('prd', 'features', 'ddd', 'technical')
foreach ($name in $legacyDocumentNames) {
    Test-ForbiddenFile "docs/$name.md"
}
$legacyHarnessDir = 'harness'
foreach ($name in @('architecture', 'operational-policies')) {
    Test-ForbiddenFile "docs/$legacyHarnessDir/$name.md"
}
Test-ForbiddenFile "docs/$legacyHarnessDir"
$legacySpecsDir = 'specs'
Test-ForbiddenFile "docs/$legacySpecsDir"

$app = Read-JsonFile 'docs/.obsidian/app.json'
if ($null -ne $app) {
    if ($app.newFileFolderPath -ne '_memory/inbox') {
        Add-Error "docs/.obsidian/app.json newFileFolderPath must be _memory/inbox"
    }
    if ($app.attachmentFolderPath -ne '_memory/assets') {
        Add-Error "docs/.obsidian/app.json attachmentFolderPath must be _memory/assets"
    }
    if ($app.alwaysUpdateLinks -ne $true) {
        Add-Error "docs/.obsidian/app.json alwaysUpdateLinks must be true"
    }
}

$templates = Read-JsonFile 'docs/.obsidian/templates.json'
if ($null -ne $templates -and $templates.folder -ne '_memory/templates') {
    Add-Error "docs/.obsidian/templates.json folder must be _memory/templates"
}

Test-FileContains 'docs/README.md' @(
    'This `docs/` folder is an Obsidian vault and the long-term knowledge memory for EventHub.',
    'Working memory',
    'Task memory',
    'Long-term knowledge memory'
)

Test-FileContains 'docs/.gitignore' @(
    '.obsidian/workspace.json',
    '.obsidian/workspace-mobile.json',
    '.trash/'
)

Test-FileContains 'docs/_memory/long-term-memory-operating-model.md' @(
    'Working memory',
    'Task memory',
    'Long-term memory',
    'Promotion rules',
    'Retrieval contract'
)

Test-FileContains 'docs/_memory/source-of-truth-map.md' @(
    'Precedence',
    'Reading by task type',
    '[[CONSTITUTION]]',
    '[[_memory/source/product-requirements]]',
    '[[_memory/source/feature-specification]]',
    '[[_memory/source/domain-model-specification]]',
    '[[_memory/source/technical-design]]',
    '[[_memory/source/harness-architecture]]',
    '[[_memory/source/harness-operational-policies]]',
    'docs/_memory/specs/'
)

Test-FileContains 'docs/_memory/source/README.md' @(
    'Authoritative Source Memory',
    '[[product-requirements]]',
    '[[feature-specification]]',
    '[[domain-model-specification]]',
    '[[technical-design]]',
    '[[harness-architecture]]',
    '[[harness-operational-policies]]'
)

Test-FileContains 'docs/_memory/specs/README.md' @(
    'Implementation Specs Memory',
    'docs/_memory/specs/',
    '.codex/plans/',
    'Do not recreate the legacy'
)

Test-FileContains 'docs/_memory/mocs/harness-memory.md' @(
    'Long-term knowledge memory',
    'Improvement loop',
    'Responses API',
    'Agents SDK'
)

$docsMemoryCommand = 'powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Test-DocsMemory.ps1'
$harnessCommand = 'powershell -NoProfile -ExecutionPolicy Bypass -File evals/run.ps1 -Layer harness'
Test-GraphMapping 'docs/README.md' $docsMemoryCommand
Test-GraphMapping 'docs/CONSTITUTION.md' $docsMemoryCommand
Test-GraphMapping 'docs/.gitignore' $docsMemoryCommand
Test-GraphMapping 'docs/.obsidian/' $docsMemoryCommand
Test-GraphMapping 'docs/_memory/source/harness-' $harnessCommand
Test-GraphMapping 'docs/_memory/' $docsMemoryCommand

Test-WikiLinks
Test-MarkdownLinks
Test-NoLegacyDocReferences

$result = @{
    status = if ($errors.Count -eq 0) { 'passed' } else { 'failed' }
    errors = @($errors)
    checkedFiles = $requiredFiles.Count
    timestamp = (Get-Date).ToUniversalTime().ToString('o')
}

if ($Json) {
    $result | ConvertTo-Json -Depth 6
}
else {
    Write-Host ''
    Write-Host 'EventHub docs memory validation' -ForegroundColor Cyan
    Write-Host "  status: $($result.status)"
    Write-Host "  checked files: $($result.checkedFiles)"
    if ($errors.Count -gt 0) {
        Write-Host ''
        Write-Host 'Errors' -ForegroundColor Red
        foreach ($err in $errors) {
            Write-Host "  - $err" -ForegroundColor Red
        }
    }
}

if ($errors.Count -gt 0) {
    exit 1
}
exit 0
