# Shared verification runner for post-edit and stop hooks.

function Get-VerifyProjectRoot {
    if ($env:CODEX_PROJECT_DIR) {
        return $env:CODEX_PROJECT_DIR
    }
    return (Get-Location).Path
}

function Invoke-VerifyQuiet {
    param(
        [string]$FilePath,
        [string[]]$ArgumentList,
        [string]$WorkingDirectory = (Get-VerifyProjectRoot)
    )
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FilePath
    $psi.Arguments = ConvertTo-ProcessArgumentString -ArgumentList $ArgumentList
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.WorkingDirectory = $WorkingDirectory

    $proc = [System.Diagnostics.Process]::Start($psi)
    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    $proc.WaitForExit()
    return @{
        ExitCode = $proc.ExitCode
        Output   = ($stdout + $stderr).Trim()
    }
}

function ConvertTo-ProcessArgumentString {
    param([string[]]$ArgumentList)

    $quoted = foreach ($arg in @($ArgumentList)) {
        if ($null -eq $arg) {
            continue
        }

        $value = [string]$arg
        if ($value -notmatch '[\s"]') {
            $value
            continue
        }

        $escaped = $value -replace '(\\*)"', '$1$1\"'
        $escaped = $escaped -replace '(\\+)$', '$1$1'
        '"' + $escaped + '"'
    }

    return ($quoted -join ' ')
}

function Get-AffectedPlan {
    param(
        [string]$ProjectRoot,
        [string]$FilePath
    )
    $node = Get-Command node -ErrorAction SilentlyContinue
    if (-not $node) {
        return $null
    }

    $scriptPath = Join-Path $ProjectRoot 'scripts\affected-tests.mjs'
    $result = Invoke-VerifyQuiet -FilePath 'node' -ArgumentList @($scriptPath, $FilePath) -WorkingDirectory $ProjectRoot
    if ($result.ExitCode -ne 0) {
        return $null
    }

    try {
        return $result.Output | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Get-StepKey {
    param([object]$Step)
    $filter = if ($Step.filter) { [string]$Step.filter } else { '' }
    $file = if ($Step.file) { [string]$Step.file } else { '' }
    $project = if ($Step.project) { [string]$Step.project } else { '' }
    $command = if ($Step.command) { [string]$Step.command } else { '' }
    return "$($Step.kind)|$project|$filter|$file|$command"
}

function Invoke-VerificationSteps {
    param(
        [string]$ProjectRoot,
        [object[]]$Steps
    )

    $errors = New-Object System.Collections.Generic.List[string]

    foreach ($step in $Steps) {
        switch ($step.kind) {
            'eslint' {
                $webRel = [string]$step.file
                if ($webRel.StartsWith('web/')) {
                    $webRel = $webRel.Substring(4)
                }
                $result = Invoke-VerifyQuiet -FilePath 'yarn' -ArgumentList @(
                    '--cwd', 'web', 'eslint', $webRel, '--max-warnings', '0'
                ) -WorkingDirectory $ProjectRoot
                if ($result.ExitCode -ne 0) {
                    $errors.Add("eslint failed on web/$webRel`: $($result.Output)")
                }
            }
            'dotnet-format' {
                $include = [string]$step.file
                $result = Invoke-VerifyQuiet -FilePath 'dotnet' -ArgumentList @(
                    'format', 'EventHub.slnx', '--verify-no-changes', '--include', $include
                ) -WorkingDirectory $ProjectRoot
                if ($result.ExitCode -ne 0) {
                    $errors.Add("dotnet format failed on $include")
                }
            }
            'dotnet-test' {
                $args = @('test', [string]$step.project)
                if ($step.filter) {
                    $args += @('--filter', [string]$step.filter)
                }
                $result = Invoke-VerifyQuiet -FilePath 'dotnet' -ArgumentList $args -WorkingDirectory $ProjectRoot
                if ($result.ExitCode -ne 0) {
                    $filter = if ($step.filter) { " filter=$($step.filter)" } else { '' }
                    $errors.Add("dotnet test failed for $($step.project)$filter")
                }
            }
            'dotnet-build' {
                $result = Invoke-VerifyQuiet -FilePath 'dotnet' -ArgumentList @(
                    'build', [string]$step.project, '-v', 'q'
                ) -WorkingDirectory $ProjectRoot
                if ($result.ExitCode -ne 0) {
                    $errors.Add("dotnet build failed for $($step.project)")
                }
            }
            'shell-test' {
                $cmd = [string]$step.command
                $parts = $cmd -split ' ', 2
                $exe = $parts[0]
                $argStr = if ($parts.Length -gt 1) { $parts[1] } else { '' }
                $argList = if ($argStr) { $argStr -split ' ' } else { @() }
                $result = Invoke-VerifyQuiet -FilePath $exe -ArgumentList $argList -WorkingDirectory $ProjectRoot
                if ($result.ExitCode -ne 0) {
                    $errors.Add("shell test failed: $cmd")
                }
            }
        }
    }

    return ,$errors
}

function Get-GitChangedFiles {
    param([string]$ProjectRoot)

    $git = Get-Command git -ErrorAction SilentlyContinue
    if (-not $git) {
        return @()
    }

    Push-Location $ProjectRoot
    try {
        $previousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $files = New-Object System.Collections.Generic.List[string]
        foreach ($line in (& git diff --name-only 2>$null)) {
            if ($line) { $files.Add([string]$line) }
        }
        foreach ($line in (& git diff --name-only --cached 2>$null)) {
            if ($line) { $files.Add([string]$line) }
        }
        foreach ($line in (& git ls-files --others --exclude-standard 2>$null)) {
            if ($line) { $files.Add([string]$line) }
        }
        return @($files | Select-Object -Unique)
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
        Pop-Location
    }
}

function Test-WebTypeCheck {
    param([string]$ProjectRoot)

    $result = Invoke-VerifyQuiet -FilePath 'yarn' -ArgumentList @(
        '--cwd', 'web', 'exec', 'tsc', '-b', '--noEmit'
    ) -WorkingDirectory $ProjectRoot

    if ($result.ExitCode -ne 0) {
        return ,@("TypeScript check failed (yarn --cwd web exec tsc -b --noEmit): $($result.Output)")
    }
    return ,@()
}

function Invoke-StopVerification {
    param(
        [string]$ProjectRoot
    )

    . "$PSScriptRoot\guard-rules.ps1"

    $errors = New-Object System.Collections.Generic.List[string]
    $changed = Get-GitChangedFiles -ProjectRoot $ProjectRoot

    if ($changed.Count -eq 0) {
        return ,@()
    }

    $needsTypeCheck = $false
    $stepKeys = @{}
    $steps = New-Object System.Collections.Generic.List[object]

    foreach ($rel in $changed) {
        $relPosix = $rel -replace '\\', '/'
        if ($relPosix -match '^web/.*\.(tsx?|jsx?)$') {
            $needsTypeCheck = $true
        }

        $abs = Join-Path $ProjectRoot ($rel -replace '/', '\')
        if (-not (Test-ShouldVerifyFile -FilePath $abs -ProjectRoot $ProjectRoot)) {
            continue
        }

        $plan = Get-AffectedPlan -ProjectRoot $ProjectRoot -FilePath $abs
        if ($null -eq $plan -or $plan.skip -or -not $plan.steps) {
            continue
        }

        foreach ($step in $plan.steps) {
            $key = Get-StepKey -Step $step
            if (-not $stepKeys.ContainsKey($key)) {
                $stepKeys[$key] = $true
                $steps.Add($step)
            }
        }
    }

    if ($needsTypeCheck) {
        foreach ($err in (Test-WebTypeCheck -ProjectRoot $ProjectRoot)) {
            $errors.Add($err)
        }
    }

    foreach ($err in (Invoke-VerificationSteps -ProjectRoot $ProjectRoot -Steps $steps.ToArray())) {
        $errors.Add($err)
    }

    return ,$errors
}
