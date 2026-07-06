# Runs the DataSeeder project to populate the database with seed data.
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    dotnet run --project src/DataSeeder/EventHub.DataSeeder.csproj
}
finally {
    Pop-Location
}
