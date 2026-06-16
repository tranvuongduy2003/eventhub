# Back-compat wrapper - prefer sync-pr-github-metadata.ps1
param(
    [Parameter(Mandatory = $true)]
    [int[]] $IssueNumber,

    [string] $StatusName,

    [string] $ConfigPath = ".github/github-project.json"
)

$pr = gh pr view --json number --jq .number
if (-not $pr) {
    Write-Error "No open PR for current branch. Pass -PrNumber to sync-pr-github-metadata.ps1 instead."
}

& "$PSScriptRoot/sync-pr-github-metadata.ps1" `
    -PrNumber $pr `
    -IssueNumber $IssueNumber `
    -StatusName $StatusName `
    -ConfigPath $ConfigPath
