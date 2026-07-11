<#
.SYNOPSIS
    Packages a skill folder into a distributable .skill file.

.DESCRIPTION
    PowerShell wrapper for package_skill.py that handles Windows encoding issues.
    Creates a .skill file (zip format) from a skill directory after validation.

.PARAMETER SkillPath
    Path to the skill folder to package

.PARAMETER OutputDir
    Optional output directory for the .skill file (defaults to current directory)

.EXAMPLE
    .\Package-Skill.ps1 -SkillPath "skills/public/my-skill"

.EXAMPLE
    .\Package-Skill.ps1 -SkillPath "skills/public/my-skill" -OutputDir "./dist"
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$SkillPath,

    [Parameter(Mandatory = $false, Position = 1)]
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "package_skill.py"

if (-not (Test-Path $pythonScript)) {
    Write-Error "Python script not found: $pythonScript"
    exit 1
}

# Set UTF-8 encoding to handle emojis in output
$env:PYTHONUTF8 = "1"

try {
    if ($OutputDir) {
        python $pythonScript $SkillPath $OutputDir
    }
    else {
        python $pythonScript $SkillPath
    }
    exit $LASTEXITCODE
}
catch {
    Write-Error "Failed to run package_skill.py: $_"
    exit 1
}






