# Regenerates contracts/openapi/api.v1.yaml from the API project.
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    node (Join-Path $repositoryRoot 'scripts/openapi/sync-contract.mjs')
}
finally {
    Pop-Location
}
