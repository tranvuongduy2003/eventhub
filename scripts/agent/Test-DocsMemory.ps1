#Requires -Version 5.1
<#
.SYNOPSIS
  Validate the simplified EventHub docs tree.
#>

param(
    [switch]$Json
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$docsRoot = Join-Path $repoRoot 'docs'
$errors = [System.Collections.Generic.List[string]]::new()

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

function Test-ForbiddenPath {
    param([string]$RelativePath)
    $path = Join-Path $repoRoot ($RelativePath -replace '/', '\')
    if (Test-Path -LiteralPath $path) {
        Add-Error "Forbidden legacy path still exists: $RelativePath"
    }
}

function Get-Text {
    param([string]$RelativePath)
    if (-not (Test-RequiredFile $RelativePath)) { return $null }
    return Get-Content -LiteralPath (Join-Path $repoRoot ($RelativePath -replace '/', '\')) -Raw -Encoding UTF8
}

function Test-FileContains {
    param(
        [string]$RelativePath,
        [string[]]$Needles
    )
    $text = Get-Text $RelativePath
    if ($null -eq $text) { return }
    foreach ($needle in $Needles) {
        if ($text -notmatch [regex]::Escape($needle)) {
            Add-Error "$RelativePath missing required text: $needle"
        }
    }
}

function Test-DocsFrontMatter {
    Get-ChildItem -LiteralPath $docsRoot -Recurse -File -Filter '*.md' | ForEach-Object {
        $relative = $_.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
        $text = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8

        if ($text -notmatch '(?s)^---\r?\n(.+?)\r?\n---\r?\n') {
            Add-Error "$relative missing YAML frontmatter"
            return
        }

        $frontMatter = $Matches[1]
        foreach ($legacyParameter in @(
            'document_type',
            'artifact_type',
            'artifact_version',
            'prd_refs',
            'ddd_refs',
            'tech_refs',
            'db_refs',
            'filename_template',
            'search_index',
            'product_name',
            'methodology',
            'companion_documents',
            'identifier_scheme'
        )) {
            if ($frontMatter -match "(?m)^$([regex]::Escape($legacyParameter))\s*:") {
                Add-Error "$relative frontmatter uses legacy parameter: $legacyParameter"
            }
        }

        foreach ($needle in @(
            'doc_schema: eventhub-doc-v1',
            'doc_kind:',
            'doc_id:',
            'title:',
            'status:',
            'owner:',
            'language:'
        )) {
            if ($frontMatter -notmatch [regex]::Escape($needle)) {
                Add-Error "$relative frontmatter missing required parameter: $needle"
            }
        }

        if ($relative -in @('docs/product.md', 'docs/features.md', 'docs/technical.md')) {
            if ($frontMatter -notmatch [regex]::Escape('doc_kind: source_spec')) {
                Add-Error "$relative must use doc_kind: source_spec"
            }
        }
        elseif ($relative -eq 'docs/specs/README.md') {
            if ($frontMatter -notmatch [regex]::Escape('doc_kind: index')) {
                Add-Error "$relative must use doc_kind: index"
            }
        }
        elseif ($relative.StartsWith('docs/specs/')) {
            if ($frontMatter -notmatch [regex]::Escape('doc_kind: implementation_spec')) {
                Add-Error "$relative must use doc_kind: implementation_spec"
            }
            foreach ($sourceDoc in @('docs/product.md', 'docs/features.md', 'docs/technical.md')) {
                if ($frontMatter -notmatch [regex]::Escape($sourceDoc)) {
                    Add-Error "$relative frontmatter missing source document: $sourceDoc"
                }
            }
        }
        elseif ($relative.StartsWith('docs/harness/')) {
            if ($frontMatter -notmatch [regex]::Escape('doc_kind: harness_doc')) {
                Add-Error "$relative must use doc_kind: harness_doc"
            }
        }
    }
}

function Test-MarkdownLinks {
    Get-ChildItem -LiteralPath $docsRoot -Recurse -File -Filter '*.md' | ForEach-Object {
        $relative = $_.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
        $text = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
        foreach ($match in [regex]::Matches($text, '\[[^\]]+\]\(([^)]+\.md)(?:#[^)]+)?\)')) {
            $target = $match.Groups[1].Value
            if ($target -match '^[a-z]+://') { continue }
            $candidate = Join-Path $_.DirectoryName ($target -replace '/', '\')
            if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
                Add-Error "Broken markdown link in $relative -> $target"
            }
        }
    }
}

function Test-NoLegacyDocReferences {
    $excluded = @(
        '.git/',
        'bin/',
        'obj/',
        'node_modules/',
        '.codex/state/',
        '.codex/tmp/'
    )

    Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Force -Include '*.md', '*.toml', '*.ps1', '*.cs' -ErrorAction SilentlyContinue | ForEach-Object {
        $relative = $_.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
        foreach ($prefix in $excluded) {
            if ($relative.StartsWith($prefix)) { return }
        }
        if ($relative -in @('scripts/agent/Test-DocsMemory.ps1', 'scripts/agent/Test-HarnessPolicy.ps1')) { return }

        $text = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
        if ($text -match 'docs[/\\]_memory|docs[/\\]CONSTITUTION\.md|docs[/\\]domain|docs[/\\]codex[/\\]rules|harness[/\\](graph|orchestrator|policies|telemetry|tools|evals)') {
            Add-Error "Legacy docs or harness reference found in $relative"
        }
    }
}

$requiredFiles = @(
    'docs/product.md',
    'docs/features.md',
    'docs/technical.md',
    'docs/specs/README.md',
    'docs/harness/README.md',
    'docs/harness/pev-loop.md',
    'docs/harness/permission-tiers.md',
    'docs/harness/evidence-bundle.md',
    'docs/harness/repository-view.md',
    'docs/harness/shared-substrate.md',
    'docs/harness/caveats.md'
)

foreach ($file in $requiredFiles) {
    Test-RequiredFile $file | Out-Null
}

foreach ($legacyPath in @(
    'docs/_memory',
    'docs/.obsidian',
    'docs/CONSTITUTION.md',
    'docs/README.md',
    'harness'
)) {
    Test-ForbiddenPath $legacyPath
}

Test-FileContains 'docs/product.md' @(
    'doc_schema: eventhub-doc-v1',
    'doc_kind: source_spec',
    'doc_id: eventhub.product',
    'features.md',
    'technical.md',
    'This is one of exactly three authoritative EventHub specifications'
)

Test-FileContains 'docs/features.md' @(
    'doc_schema: eventhub-doc-v1',
    'doc_kind: source_spec',
    'doc_id: eventhub.features',
    'product.md',
    'technical.md',
    'Delivery snapshot',
    'Acceptance criteria'
)

Test-FileContains 'docs/technical.md' @(
    'doc_schema: eventhub-doc-v1',
    'doc_kind: source_spec',
    'doc_id: eventhub.technical',
    'Clean Architecture',
    'Domain-Driven Design',
    'CQRS',
    'PostgreSQL',
    'contracts/openapi/api.v1.yaml',
    'Feature completion'
)

Test-FileContains 'docs/specs/README.md' @(
    'docs/specs/',
    'product.md',
    'features.md',
    'technical.md'
)

Test-FileContains 'docs/harness/README.md' @(
    '.agents/skills/',
    '.codex/agents/',
    '.codex/hooks.json',
    'scripts/agent/Test-HarnessPolicy.ps1'
)

Test-DocsFrontMatter
Test-MarkdownLinks
Test-NoLegacyDocReferences

$specCount = @(Get-ChildItem -LiteralPath (Join-Path $docsRoot 'specs') -File -Filter '*.md' | Where-Object { $_.Name -ne 'README.md' }).Count
$result = @{
    status = if ($errors.Count -eq 0) { 'passed' } else { 'failed' }
    errors = @($errors)
    checkedFiles = $requiredFiles.Count
    specCount = $specCount
    timestamp = (Get-Date).ToUniversalTime().ToString('o')
}

if ($Json) {
    $result | ConvertTo-Json -Depth 6
}
else {
    Write-Host ''
    Write-Host 'EventHub docs validation' -ForegroundColor Cyan
    Write-Host "  status: $($result.status)"
    Write-Host "  checked files: $($result.checkedFiles)"
    Write-Host "  specs: $($result.specCount)"
    if ($errors.Count -gt 0) {
        Write-Host ''
        Write-Host 'Errors' -ForegroundColor Red
        foreach ($err in $errors) {
            Write-Host "  - $err" -ForegroundColor Red
        }
    }
}

if ($errors.Count -gt 0) { exit 1 }
exit 0
