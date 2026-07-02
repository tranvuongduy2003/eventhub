#Requires -Version 5.1
<#
.SYNOPSIS
  First-time local setup for the EventHub repository.

.DESCRIPTION
  Verifies prerequisites, restores .NET dependencies, seeds optional
  .env files, creates and trusts the ASP.NET Core HTTPS development certificate.

  Run from any directory:
    .\scripts\Setup-Environments.ps1

  Pinned local HTTPS ports (see src/AppHost/AppHost.cs):
    API HTTPS  https://localhost:8000
    API HTTP   http://localhost:8001

.PARAMETER SkipDockerCheck
  Do not verify Docker is running (not recommended).

.PARAMETER SkipTrustCert
  Skip HTTPS dev certificate create/trust (useful in CI or locked-down shells).

.PARAMETER SkipRestore
  Skip .NET package restore (useful when packages are already restored or network is unavailable).

.PARAMETER SkipBuild
  Skip the final solution build smoke test.

.PARAMETER ForceEnvCopy
  Overwrite existing .env files from their *.example templates.

.EXAMPLE
  .\scripts\Setup-Environments.ps1
#>
[CmdletBinding()]
param(
    [switch] $SkipDockerCheck,
    [switch] $SkipTrustCert,
    [switch] $SkipRestore,
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
$YarnAvailable = $false

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
        # Yarn writes progress/warnings to stderr; redirect to stdout to avoid
        # PowerShell 5.1 wrapping them as NativeCommandError ErrorRecords.
        $stderr = (& $yarn install --frozen-lockfile 2>&1)
        $stderr | ForEach-Object { if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.ToString() } else { $_ } } | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Warn 'yarn install --frozen-lockfile failed; retrying without --frozen-lockfile'
            $stderr2 = (& $yarn install 2>&1)
            $stderr2 | ForEach-Object { if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.ToString() } else { $_ } } | Out-Host
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
            $prevEap = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            try {
                $dockerOutput = docker info 2>&1
                if ($LASTEXITCODE -ne 0) {
                    $message = ($dockerOutput | ForEach-Object { if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.ToString() } else { $_ } }) -join "`n"
                    if ([string]::IsNullOrWhiteSpace($message)) {
                        $message = 'Start Docker Desktop, wait until it is running, then re-run this script.'
                    }
                    throw $message
                }
            }
            finally {
                $ErrorActionPreference = $prevEap
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
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $yarnVersionOutput = & $yarnExe --version 2>&1
        if ($LASTEXITCODE -ne 0) {
            $yarnLines = $yarnVersionOutput | ForEach-Object { if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.ToString() } else { $_ } }
            $message = $yarnLines | Where-Object { $_ -match 'EPERM|Error:|failed|not permitted' } | Select-Object -First 1
            if (-not $message) {
                $message = $yarnLines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
            }
            throw "Yarn version check failed: $message"
        }
        $yarnVersion = ($yarnVersionOutput | Out-String).Trim()
        $YarnAvailable = $true
    }
    catch {
        $webNodeModules = Join-Path $WebDir 'node_modules'
        $e2eNodeModules = Join-Path $RepoRoot 'e2e\node_modules'
        if ((Test-Path $webNodeModules) -and (Test-Path $e2eNodeModules)) {
            Write-Warn "Yarn is installed but could not run in this shell; existing node_modules found, so Yarn install steps will be skipped. Error: $($_.Exception.Message)"
        }
        else {
            throw
        }
    }
    finally {
        $ErrorActionPreference = $prevEap
    }
    if ($YarnAvailable) {
        Write-Ok "Yarn $yarnVersion"
    }

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
    if (-not $SkipRestore) {
        Write-Step 'Restoring .NET packages'
        Invoke-Checked 'dotnet restore' {
            dotnet restore $SolutionPath | Out-Host
        }
    }
    else {
        Write-Warn 'Skipped .NET package restore (-SkipRestore)'
    }

    Write-Step 'Installing Aspire project templates (idempotent)'
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $templateOutput = dotnet new install Aspire.ProjectTemplates 2>&1
        $templateLines = $templateOutput | ForEach-Object { if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.ToString() } else { $_ } }
        if ($LASTEXITCODE -eq 0) {
            $templateLines | Out-Host
            Write-Ok 'Aspire.ProjectTemplates installed or already present'
        }
        else {
            $firstTemplateError = $templateLines | Where-Object { $_ -match 'could not|invalid|failed|Error:' } | Select-Object -First 1
            if (-not $firstTemplateError) {
                $firstTemplateError = $templateLines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
            }
            if ($firstTemplateError) {
                Write-Warn "Aspire.ProjectTemplates install failed: $firstTemplateError"
            }
            Write-Warn 'Continuing because templates are optional for running this repo'
        }
    }
    finally {
        $ErrorActionPreference = $prevEap
    }

    # --- Frontend ---
    if (-not (Test-Path (Join-Path $WebDir 'package.json'))) {
        throw "Frontend project not found: $WebDir"
    }

    if ($YarnAvailable) {
        Write-Step 'Installing frontend dependencies (yarn)'
        Invoke-Checked 'yarn install' {
            Invoke-YarnInstall -WorkingDirectory $WebDir
        }
    }
    else {
        Write-Warn 'Skipped frontend dependency install because Yarn is unavailable in this shell and web/node_modules already exists'
    }

    # --- E2E tests (Playwright) ---
    $E2eDir = Join-Path $RepoRoot 'e2e'
    if (Test-Path (Join-Path $E2eDir 'package.json')) {
        if ($YarnAvailable) {
            Write-Step 'Installing e2e test dependencies (Playwright)'
            Invoke-Checked 'e2e yarn install' {
                Invoke-YarnInstall -WorkingDirectory $E2eDir
            }

            Write-Step 'Installing Playwright Chromium browser'
            $prevEap = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            try {
                $yarnExe2 = Get-YarnExecutable
                & $yarnExe2 --cwd $E2eDir playwright install chromium 2>&1 | Out-Host
                if ($LASTEXITCODE -eq 0) {
                    Write-Ok 'Playwright Chromium installed'
                }
                else {
                    Write-Warn 'Playwright Chromium install failed; run manually: cd e2e && yarn install:browsers'
                }
            }
            finally {
                $ErrorActionPreference = $prevEap
            }
        }
        else {
            Write-Warn 'Skipped e2e dependency and Playwright browser install because Yarn is unavailable in this shell and e2e/node_modules already exists'
        }
    }

    # --- Local config (not committed) ---
    Write-Step 'Seeding local config (.env)'
    $rootEnvPath = Join-Path $RepoRoot '.env'
    Copy-EnvFile -ExamplePath (Join-Path $RepoRoot '.env.example') -TargetPath $rootEnvPath
    Initialize-WebEnvFile `
        -ExamplePath (Join-Path $WebDir '.env.example') `
        -TargetPath (Join-Path $WebDir '.env') `
        -ApiHttpsUrl $ApiHttpsUrl

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

Docs: README.md, docs/_memory/source/technical-design.md
MCP: shared Codex MCP servers live in .codex/config.toml.
Re-run with -ForceEnvCopy to refresh .env and web\.env from templates.

"@ -ForegroundColor Green
}
finally {
    Pop-Location
}
