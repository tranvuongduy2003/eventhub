<#
.SYNOPSIS
    Validates a skill directory structure and SKILL.md content.

.DESCRIPTION
    PowerShell wrapper for quick_validate.py that handles Windows encoding issues.
    Checks that a skill has valid SKILL.md with proper frontmatter.

.PARAMETER SkillPath
    Path to the skill directory to validate

.EXAMPLE
    .\Test-Skill.ps1 -SkillPath "skills/public/my-skill"

.EXAMPLE
    .\Test-Skill.ps1 "skills/public/my-skill"
#>

param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$SkillPath
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "quick_validate.py"

if (-not (Test-Path $pythonScript)) {
    Write-Error "Python script not found: $pythonScript"
    exit 1
}

# Set UTF-8 encoding to handle emojis in output
$env:PYTHONUTF8 = "1"

try {
    python $pythonScript $SkillPath
    exit $LASTEXITCODE
}
catch {
    Write-Error "Failed to run quick_validate.py: $_"
    exit 1
}






