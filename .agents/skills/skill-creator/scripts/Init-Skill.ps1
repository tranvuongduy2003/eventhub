<#
.SYNOPSIS
    Creates a new skill from template.

.DESCRIPTION
    PowerShell wrapper for init_skill.py that handles Windows encoding issues.
    Creates a new skill directory with template SKILL.md and example files.

.PARAMETER SkillName
    Name of the skill (hyphen-case, e.g., 'my-new-skill')

.PARAMETER Path
    Path where the skill directory should be created

.EXAMPLE
    .\Init-Skill.ps1 -SkillName "my-new-skill" -Path "skills/public"

.EXAMPLE
    .\Init-Skill.ps1 my-api-helper "skills/private"
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$SkillName,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$Path
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "init_skill.py"

if (-not (Test-Path $pythonScript)) {
    Write-Error "Python script not found: $pythonScript"
    exit 1
}

# Set UTF-8 encoding to handle emojis in output
$env:PYTHONUTF8 = "1"

try {
    python $pythonScript $SkillName --path $Path
    exit $LASTEXITCODE
}
catch {
    Write-Error "Failed to run init_skill.py: $_"
    exit 1
}






