#Requires -Version 5.1
<#
.SYNOPSIS
  First-time local setup for the EventHub repository.

.DESCRIPTION
  Verifies prerequisites, restores .NET dependencies, seeds optional
  .env and .mcp.json files, creates and trusts the ASP.NET Core HTTPS development certificate.

  Run from any directory:
    .\scripts\Setup-Environments.ps1

  Pinned local HTTPS ports (see src/AppHost/AppHost.cs):
    API HTTPS  https://localhost:8000
    API HTTP   http://localhost:8001

.PARAMETER SkipDockerCheck
  Do not verify Docker is running (not recommended).

.PARAMETER SkipTrustCert
  Skip HTTPS dev certificate create/trust (useful in CI or locked-down shells).

.PARAMETER SkipBuild
  Skip the final solution build smoke test.

.PARAMETER ForceEnvCopy
  Overwrite existing .env and .mcp.json from their *.example templates.

.EXAMPLE
  .\scripts\Setup-Environments.ps1
#>
[CmdletBinding()]
param(
    [switch] $SkipDockerCheck,
    [switch] $SkipTrustCert,
    [switch] $SkipBuild,
    [switch] $ForceEnvCopy
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$SolutionPath = Join-Path $RepoRoot 'EventHub.slnx'
$WebDir = Join-Path $RepoRoot 'web'
$GlobalJsonPath = Join-Path $RepoRoot 'global.json'

# Must stay in sync with src/AppHost/AppHost.cs and src/Api/Properties/launchSettings.json
$ApiHttpsPort = 8000
$ApiHttpPort = 8001
$WebHttpsPort = 5000
$ApiHttpsUrl = "https://localhost:$ApiHttpsPort"
$ApiHttpUrl = "http://localhost:$ApiHttpPort"
$WebHttpsUrl = "https://localhost:$WebHttpsPort"

function Write-Step([string] $Message) {
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Write-Ok([string] $Message) {
    Write-Host "    OK  $Message" -ForegroundColor Green
}

function Write-Warn([string] $Message) {
    Write-Host "    WARN  $Message" -ForegroundColor Yellow
}

function Write-Err([string] $Message) {
    Write-Host "    FAIL  $Message" -ForegroundColor Red
}

function Test-CommandAvailable([string] $Name) {
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Get-YarnExecutable {
    # npm's yarn.ps1 treats Yarn stderr (progress) as PowerShell errors when
    # $ErrorActionPreference is Stop. Prefer yarn.cmd on Windows.
    if ($env:OS -eq 'Windows_NT') {
        $cmd = Get-Command yarn.cmd -ErrorAction SilentlyContinue
        if ($cmd) {
            return $cmd.Source
        }
    }

    $yarn = Get-Command yarn -ErrorAction SilentlyContinue
    if ($yarn) {
        return $yarn.Source
    }

    throw 'Yarn not found. Install: https://yarnpkg.com/getting-started/install'
}

function Ensure-DevHttpsCertificate {
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'

    try {
        Write-Host '    Creating ASP.NET Core HTTPS development certificate...' -ForegroundColor DarkGray
        dotnet dev-certs https 2>&1 | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw 'dotnet dev-certs https failed to create the development certificate'
        }
        Write-Ok 'HTTPS development certificate created or already present'

        dotnet dev-certs https --check --trust 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Ok 'HTTPS development certificate is trusted'
            return
        }

        Write-Host '    Trusting development certificate (UAC prompt may appear)...' -ForegroundColor DarkGray
        dotnet dev-certs https --trust 2>&1 | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Warn @"
Could not trust the development certificate.
  Run in an elevated shell: dotnet dev-certs https --trust
  Or recreate: dotnet dev-certs https --clean && dotnet dev-certs https --trust
"@
            return
        }

        dotnet dev-certs https --check --trust 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Ok 'HTTPS development certificate trusted'
        }
        else {
            Write-Warn 'Certificate exists but trust could not be verified. Run: dotnet dev-certs https --trust'
        }
    }
    finally {
        $ErrorActionPreference = $prevEap
    }
}

function Invoke-YarnInstall([string] $WorkingDirectory) {
    $yarn = Get-YarnExecutable
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'

    Push-Location $WorkingDirectory
    try {
        & $yarn install --frozen-lockfile 2>&1 | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Warn 'yarn install --frozen-lockfile failed; retrying without --frozen-lockfile'
            & $yarn install 2>&1 | Out-Host
        }

        if ($LASTEXITCODE -ne 0) {
            throw "yarn install failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
        $ErrorActionPreference = $prevEap
    }
}

function Get-SemanticVersion([string] $Text) {
    if ($Text -match '(\d+)\.(\d+)\.(\d+)') {
        return [version]"$($Matches[1]).$($Matches[2]).$($Matches[3])"
    }
    if ($Text -match '(\d+)\.(\d+)') {
        return [version]"$($Matches[1]).$($Matches[2]).0"
    }
    return $null
}

function Assert-MinimumVersion {
    param(
        [string] $Label,
        [string] $VersionText,
        [version] $Minimum
    )

    $parsed = Get-SemanticVersion $VersionText
    if (-not $parsed) {
        throw "Could not parse $Label version from: $VersionText"
    }
    if ($parsed -lt $Minimum) {
        throw "$Label $parsed is below required $Minimum"
    }
    Write-Ok "$Label $parsed (>= $Minimum)"
}

function Invoke-Checked {
    param(
        [string] $Label,
        [scriptblock] $Action
    )

    try {
        & $Action
        if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
            throw "exit code $LASTEXITCODE"
        }
        Write-Ok $Label
    }
    catch {
        Write-Err "$Label - $($_.Exception.Message)"
        throw
    }
}

function Copy-EnvFile {
    param(
        [string] $ExamplePath,
        [string] $TargetPath
    )

    if (-not (Test-Path $ExamplePath)) {
        Write-Warn "Missing template: $ExamplePath"
        return
    }

    if ((Test-Path $TargetPath) -and -not $ForceEnvCopy) {
        Write-Ok "Already exists (use -ForceEnvCopy to overwrite): $TargetPath"
        return
    }

    Copy-Item -Path $ExamplePath -Destination $TargetPath -Force
    Write-Ok "Created $TargetPath from template"
}

function Initialize-WebEnvFile {
    param(
        [string] $ExamplePath,
        [string] $TargetPath,
        [string] $ApiHttpsUrl
    )

    if (-not (Test-Path $ExamplePath)) {
        Write-Warn "Missing template: $ExamplePath"
        return
    }

    if ((Test-Path $TargetPath) -and -not $ForceEnvCopy) {
        Write-Ok "Already exists (use -ForceEnvCopy to overwrite): $TargetPath"
        return
    }

    $content = Get-Content -Path $ExamplePath -Raw
    $content = $content -replace '(?m)^VITE_API_URL=.*$', "VITE_API_URL=$ApiHttpsUrl"

    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($TargetPath, $content.TrimEnd() + "`n", $utf8NoBom)
    Write-Ok "Created $TargetPath from web/.env.example (VITE_API_URL=$ApiHttpsUrl)"
}

function Get-PostgresPasswordFromEnv([string] $EnvFilePath) {
    if (-not (Test-Path $EnvFilePath)) {
        return 'postgres'
    }

    foreach ($line in Get-Content $EnvFilePath) {
        if ($line -match '^\s*POSTGRES_PASSWORD\s*=\s*(.+)\s*$') {
            return $Matches[1].Trim().Trim('"').Trim("'")
        }
    }

    return 'postgres'
}

function Initialize-McpConfig {
    param(
        [string] $RepoRoot,
        [string] $EnvFilePath
    )

    $examplePath = Join-Path $RepoRoot '.mcp.json.example'
    $targetPath = Join-Path $RepoRoot '.mcp.json'

    if (-not (Test-Path $examplePath)) {
        Write-Warn "Missing template: $examplePath"
        return
    }

    if ((Test-Path $targetPath) -and -not $ForceEnvCopy) {
        Write-Ok "Already exists (use -ForceEnvCopy to overwrite): $targetPath"
        return
    }

    Copy-Item -Path $examplePath -Destination $targetPath -Force
    Write-Ok "Created $targetPath from .mcp.json.example"

    $postgresPassword = Get-PostgresPasswordFromEnv -EnvFilePath $EnvFilePath
    $content = Get-Content -Path $targetPath -Raw
    $content = $content.Replace('YOUR_PASSWORD', $postgresPassword)

    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($targetPath, $content, $utf8NoBom)
    Write-Ok 'Personalized MCP Postgres password (from .env)'
}

Push-Location $RepoRoot
try {
    Write-Host @"

Solution - local environment setup
Repository: $RepoRoot

"@ -ForegroundColor White

    # --- Prerequisites ---
    Write-Step 'Checking prerequisites'

    if (-not (Test-Path $SolutionPath)) {
        throw "Solution not found: $SolutionPath"
    }
    Write-Ok "Solution file found"

    if (-not (Test-CommandAvailable 'dotnet')) {
        throw '.NET SDK not found. Install .NET 10 SDK: https://dotnet.microsoft.com/download'
    }

    $dotnetVersion = (dotnet --version).Trim()
    Assert-MinimumVersion -Label '.NET SDK' -VersionText $dotnetVersion -Minimum ([version]'10.0.0')

    if (Test-Path $GlobalJsonPath) {
        Write-Ok "global.json pins SDK (see $GlobalJsonPath)"
    }

    if (-not $SkipDockerCheck) {
        if (-not (Test-CommandAvailable 'docker')) {
            throw 'Docker CLI not found. Install Docker Desktop: https://www.docker.com/products/docker-desktop/'
        }
        Invoke-Checked 'Docker engine reachable' {
            docker info 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw 'Start Docker Desktop, wait until it is running, then re-run this script.'
            }
        }
    }
    else {
        Write-Warn 'Docker check skipped (-SkipDockerCheck)'
    }

    if (-not (Test-CommandAvailable 'node')) {
        throw 'Node.js not found. Install Node.js 22 LTS: https://nodejs.org/'
    }
    $nodeVersion = (node --version).Trim().TrimStart('v')
    Assert-MinimumVersion -Label 'Node.js' -VersionText $nodeVersion -Minimum ([version]'20.0.0')

    if (-not (Test-CommandAvailable 'yarn') -and -not (Test-CommandAvailable 'yarn.cmd')) {
        throw 'Yarn not found. Install: https://yarnpkg.com/getting-started/install'
    }
    $yarnExe = Get-YarnExecutable
    $yarnVersion = (& $yarnExe --version 2>&1 | Out-String).Trim()
    Write-Ok "Yarn $yarnVersion"

    if (Test-CommandAvailable 'aspire') {
        $aspireVersion = (aspire --version 2>&1 | Out-String).Trim()
        if ($aspireVersion) {
            Write-Ok "Aspire CLI $aspireVersion"
        }
    }
    else {
        Write-Warn @"
Aspire CLI not on PATH (optional but recommended).
  Install: https://aspire.dev
  You can still run: dotnet run --project src/AppHost/EventHub.AppHost.csproj
"@
    }

    # --- .NET ---
    Write-Step 'Restoring .NET packages'
    Invoke-Checked 'dotnet restore' {
        dotnet restore $SolutionPath | Out-Host
    }

    Write-Step 'Installing Aspire project templates (idempotent)'
    Invoke-Checked 'Aspire.ProjectTemplates' {
        dotnet new install Aspire.ProjectTemplates | Out-Host
    }

    # --- Frontend ---
    if (-not (Test-Path (Join-Path $WebDir 'package.json'))) {
        throw "Frontend project not found: $WebDir"
    }

    Write-Step 'Installing frontend dependencies (yarn)'
    Invoke-Checked 'yarn install' {
        Invoke-YarnInstall -WorkingDirectory $WebDir
    }

    # --- Local config (not committed) ---
    Write-Step 'Seeding local config (.env, MCP)'
    $rootEnvPath = Join-Path $RepoRoot '.env'
    Copy-EnvFile -ExamplePath (Join-Path $RepoRoot '.env.example') -TargetPath $rootEnvPath
    Initialize-WebEnvFile `
        -ExamplePath (Join-Path $WebDir '.env.example') `
        -TargetPath (Join-Path $WebDir '.env') `
        -ApiHttpsUrl $ApiHttpsUrl
    Initialize-McpConfig -RepoRoot $RepoRoot -EnvFilePath $rootEnvPath

    # --- HTTPS dev cert ---
    if (-not $SkipTrustCert) {
        Write-Step 'HTTPS development certificate (create + trust)'
        Write-Host "    API: $ApiHttpsUrl (HTTP $ApiHttpUrl) | Web: $WebHttpsUrl" -ForegroundColor DarkGray
        Ensure-DevHttpsCertificate
        Write-Host @"
    Standalone Vite: web\.env uses VITE_API_URL=$ApiHttpsUrl (from Setup-Environments.ps1 / web\.env.example).
    Optional HTTPS Vite: set VITE_DEV_HTTPS=1 in web\.env, then yarn --cwd web dev (matches API CORS).
"@ -ForegroundColor DarkGray
    }
    else {
        Write-Warn 'Skipped HTTPS dev certificate create/trust (-SkipTrustCert)'
    }

    # --- Smoke build ---
    if (-not $SkipBuild) {
        Write-Step 'Smoke build (Release)'
        Invoke-Checked 'dotnet build' {
            dotnet build $SolutionPath -c Release --no-restore | Out-Host
        }
    }

    Write-Host @"

Setup complete.

Next steps:
  1. Start Docker Desktop (if not already running).
  2. Run the stack:
       dotnet run --project src/AppHost/EventHub.AppHost.csproj
     Or:
       aspire run --project src/AppHost/EventHub.AppHost.csproj
  3. Open the Aspire dashboard URL shown in the console.
  4. HTTPS endpoints (pinned in AppHost):
       API:    $ApiHttpsUrl  (HTTP $ApiHttpUrl)
       Web UI: $WebHttpsUrl

Docs: README.md, docs/technical.md
MCP: .mcp.json is local-only (copy from .mcp.json.example); Postgres URI must match .env / AppHost.
Re-run with -ForceEnvCopy to refresh .env, web\.env, and .mcp.json from templates.

"@ -ForegroundColor Green
}
finally {
    Pop-Location
}
