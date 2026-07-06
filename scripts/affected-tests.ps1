#Requires -Version 5.1
<#
.SYNOPSIS
  Map a changed file to verification steps for the agent harness.

.DESCRIPTION
  Uses lightweight layer heuristics plus harness/graph/index.json. The output is
  JSON for hook and eval consumption:
    { "skip": bool, "steps": [], "rel": string?, "error": string? }

.PARAMETER Path
  Absolute or repository-relative file path.
#>

param(
    [Parameter(Position = 0)]
    [string]$Path
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Get-Graph {
    $graphPath = Join-Path $repoRoot 'harness\graph\index.json'
    try {
        $json = Get-Content -LiteralPath $graphPath -Raw -Encoding UTF8
        $json = $json -replace "^\uFEFF", ''
        return $json | ConvertFrom-Json
    }
    catch {
        return [pscustomobject]@{
            version = 1
            layers = [pscustomobject]@{}
            skipPatterns = @()
        }
    }
}

function ConvertTo-PosixPath {
    param([string]$Value)
    return $Value -replace '\\', '/'
}

function Get-RelativePath {
    param([string]$FilePath)

    $baseUriValue = $repoRoot
    if (-not $baseUriValue.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $baseUriValue += [System.IO.Path]::DirectorySeparatorChar
    }

    $absolutePath = if ([System.IO.Path]::IsPathRooted($FilePath)) {
        [System.IO.Path]::GetFullPath($FilePath)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $repoRoot ($FilePath -replace '/', '\')))
    }

    $baseUri = [System.Uri]::new($baseUriValue)
    $fileUri = [System.Uri]::new($absolutePath)
    $relativeUri = $baseUri.MakeRelativeUri($fileUri).ToString()
    return [System.Uri]::UnescapeDataString((ConvertTo-PosixPath $relativeUri))
}

function Convert-GlobToRegex {
    param([string]$Pattern)

    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.Append('^')

    for ($index = 0; $index -lt $Pattern.Length; $index++) {
        $char = $Pattern[$index]
        if ($char -eq '*') {
            if ($index + 1 -lt $Pattern.Length -and $Pattern[$index + 1] -eq '*') {
                [void]$builder.Append('.*')
                $index++
            }
            else {
                [void]$builder.Append('[^/]*')
            }
            continue
        }

        [void]$builder.Append([regex]::Escape([string]$char))
    }

    [void]$builder.Append('$')
    return $builder.ToString()
}

function Test-MatchesAny {
    param(
        [string]$RelativePath,
        [object[]]$Patterns
    )

    foreach ($pattern in @($Patterns)) {
        if ($RelativePath -match (Convert-GlobToRegex ([string]$pattern))) {
            return $true
        }
    }

    return $false
}

function Get-FeatureSegment {
    param(
        [string]$RelativePath,
        [string]$LayerPrefix
    )

    $rest = $RelativePath.Substring($LayerPrefix.Length)
    $segment = ($rest -split '/')[0]
    if ($segment -and -not $segment.Contains('.')) {
        return $segment
    }

    return $null
}

function Get-MatchingLayers {
    param(
        [string]$RelativePath,
        [object]$Graph
    )

    $matches = New-Object System.Collections.Generic.List[object]
    if (-not $Graph.layers) {
        return @()
    }

    foreach ($property in $Graph.layers.PSObject.Properties) {
        if ($RelativePath.StartsWith($property.Name, [System.StringComparison]::Ordinal)) {
            $matches.Add([pscustomobject]@{
                prefix = $property.Name
                config = $property.Value
            })
        }
    }

    return @($matches.ToArray() | Sort-Object { $_.prefix.Length } -Descending)
}

function New-Step {
    param([hashtable]$Values)
    return [pscustomobject]$Values
}

function Get-BuildSteps {
    param(
        [string]$RelativePath,
        [object]$Graph
    )

    $steps = New-Object System.Collections.Generic.List[object]

    if ($RelativePath -match '^web/' -and $RelativePath -match '\.(tsx?|jsx?)$') {
        $steps.Add((New-Step @{ kind = 'eslint'; file = $RelativePath }))
        return $steps.ToArray()
    }

    if ($RelativePath.EndsWith('.cs', [System.StringComparison]::Ordinal)) {
        $steps.Add((New-Step @{ kind = 'dotnet-format'; file = $RelativePath }))

        foreach ($match in @(Get-MatchingLayers -RelativePath $RelativePath -Graph $Graph)) {
            $config = $match.config
            $feature = $null
            if ($config.namespaceFromSegment -eq $true) {
                $feature = Get-FeatureSegment -RelativePath $RelativePath -LayerPrefix $match.prefix
            }

            if ($config.postEditAction -eq 'test' -and $config.testProject) {
                $step = [ordered]@{
                    kind = 'dotnet-test'
                    project = [string]$config.testProject
                }
                if ($feature) {
                    $step.filter = "FullyQualifiedName~$feature"
                }
                $steps.Add((New-Step $step))
            }
            elseif ($config.postEditAction -eq 'build' -and $config.buildProject) {
                $steps.Add((New-Step @{
                    kind = 'dotnet-build'
                    project = [string]$config.buildProject
                }))
            }
            break
        }
    }

    foreach ($match in @(Get-MatchingLayers -RelativePath $RelativePath -Graph $Graph)) {
        $config = $match.config
        if ($config.postEditAction -eq 'test' -and $config.testCommand) {
            $steps.Add((New-Step @{
                kind = 'shell-test'
                command = [string]$config.testCommand
            }))
        }
        break
    }

    return $steps.ToArray()
}

function Write-Result {
    param([hashtable]$Result)
    [pscustomobject]$Result | ConvertTo-Json -Depth 10 -Compress
}

if (-not $Path) {
    Write-Result @{ skip = $true; steps = @(); error = 'missing file path' }
    exit 0
}

$graph = Get-Graph
$relativePath = Get-RelativePath -FilePath $Path

if (Test-MatchesAny -RelativePath $relativePath -Patterns @($graph.skipPatterns)) {
    Write-Result @{ skip = $true; steps = @(); rel = $relativePath }
    exit 0
}

$steps = @(Get-BuildSteps -RelativePath $relativePath -Graph $graph)
if ($steps.Count -eq 0) {
    Write-Result @{ skip = $true; steps = @(); rel = $relativePath }
    exit 0
}

Write-Result @{ skip = $false; steps = $steps; rel = $relativePath }
