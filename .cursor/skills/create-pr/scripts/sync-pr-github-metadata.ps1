# After gh pr create: add PR/issues to Project, labels, assignees, and issue Status.
param(
    [Parameter(Mandatory = $true)]
    [int] $PrNumber,

    [int[]] $IssueNumber = @(),

    # Applies to linked issues only (not the PR).
    [string] $StatusName,

    [string[]] $ExtraPrLabels = @(),

    [string[]] $ExtraPrAssignees = @(),

    [string[]] $ExtraIssueLabels = @(),

    [string[]] $ExtraIssueAssignees = @(),

    [switch] $SkipProjectStatus,

    [string] $ConfigPath = ".github/github-project.json"
)

$ErrorActionPreference = "Stop"

function Get-GhProjectConfig([string]$path) {
    if (-not (Test-Path $path)) {
        Write-Error "Missing $path - copy .github/github-project.json.example and set owner/projectNumber."
    }
    return Get-Content -Raw $path | ConvertFrom-Json
}

function Normalize-StatusName([string]$value) {
    return ($value -replace "\s+", "").ToLowerInvariant()
}

function To-CommaList([string[]]$values) {
    if (-not $values -or $values.Count -eq 0) { return $null }
    return ($values | Where-Object { $_ } | Select-Object -Unique) -join ","
}

function Get-RepoNameWithOwner {
    return gh repo view --json nameWithOwner --jq .nameWithOwner
}

function Get-StatusOption([object]$statusField, [string]$statusName) {
    if (-not $statusField -or -not $statusName) {
        return $null
    }

    $target = Normalize-StatusName $statusName
    $option = $statusField.options | Where-Object { (Normalize-StatusName $_.name) -eq $target } | Select-Object -First 1
    if (-not $option) {
        $available = ($statusField.options.name -join ", ")
        Write-Warning "Status '$statusName' not found. Available: $available"
    }

    return $option
}

function Set-ProjectItemStatus(
    [string]$projectId,
    [object]$statusField,
    [object]$statusOption,
    [string]$itemId) {
    gh project item-edit `
        --project-id $projectId `
        --id $itemId `
        --field-id $statusField.id `
        --single-select-option-id $statusOption.id | Out-Null
}

function Get-ProjectItems([int]$projectNumber, [string]$owner) {
    return (gh project item-list $projectNumber --owner $owner --format json --limit 200 | ConvertFrom-Json).items
}

function Find-ProjectItemByContent(
    [object[]]$items,
    [string]$contentType,
    [int]$contentNumber) {
    return $items | Where-Object {
        $_.content.type -eq $contentType -and $_.content.number -eq $contentNumber
    } | Select-Object -First 1
}

$config = Get-GhProjectConfig $ConfigPath
$owner = $config.owner
$projectNumber = $config.projectNumber
$repo = Get-RepoNameWithOwner

$addPrToProject = $true
if ($null -ne $config.addPrToProject) { $addPrToProject = [bool]$config.addPrToProject }

$ensureIssuesOnProject = $true
if ($null -ne $config.ensureIssuesOnProject) { $ensureIssuesOnProject = [bool]$config.ensureIssuesOnProject }

$inheritPrLabels = $true
$inheritPrAssignees = $true
if ($config.inheritFromIssues) {
    if ($null -ne $config.inheritFromIssues.prLabels) { $inheritPrLabels = [bool]$config.inheritFromIssues.prLabels }
    if ($null -ne $config.inheritFromIssues.prAssignees) { $inheritPrAssignees = [bool]$config.inheritFromIssues.prAssignees }
}

$configPrLabels = @()
if ($config.prLabels) { $configPrLabels = @($config.prLabels) }

$configPrAssignees = @()
if ($config.prAssignees) { $configPrAssignees = @($config.prAssignees) }

$configIssueLabels = @()
if ($config.issueLabels) { $configIssueLabels = @($config.issueLabels) }

$configIssueAssignees = @()
if ($config.issueAssignees) { $configIssueAssignees = @($config.issueAssignees) }

$issueStatusName = $StatusName
if (-not $issueStatusName) {
    if ($config.issueStatusOnPrCreated) {
        $issueStatusName = $config.issueStatusOnPrCreated
    }
    elseif ($config.statusOnPrCreated) {
        $issueStatusName = $config.statusOnPrCreated
    }
    else {
        $issueStatusName = "In review"
    }
}

$prStatusName = $config.prStatusOnProject

$inheritedLabels = [System.Collections.Generic.List[string]]::new()
$inheritedAssignees = [System.Collections.Generic.List[string]]::new()

foreach ($num in $IssueNumber) {
    $issueJson = gh issue view $num --json labels,assignees | ConvertFrom-Json
    foreach ($label in $issueJson.labels) {
        if ($label.name) { [void]$inheritedLabels.Add($label.name) }
    }
    foreach ($assignee in $issueJson.assignees) {
        if ($assignee.login) { [void]$inheritedAssignees.Add($assignee.login) }
    }
}

$prLabels = [System.Collections.Generic.List[string]]::new()
foreach ($l in $configPrLabels) { [void]$prLabels.Add($l) }
if ($inheritPrLabels) {
    foreach ($l in $inheritedLabels) { [void]$prLabels.Add($l) }
}
foreach ($l in $ExtraPrLabels) { [void]$prLabels.Add($l) }

$prAssignees = [System.Collections.Generic.List[string]]::new()
foreach ($a in $configPrAssignees) { [void]$prAssignees.Add($a) }
if ($inheritPrAssignees) {
    foreach ($a in $inheritedAssignees) { [void]$prAssignees.Add($a) }
}
foreach ($a in $ExtraPrAssignees) { [void]$prAssignees.Add($a) }

$prEditArgs = @("pr", "edit", $PrNumber)
$didPrEdit = $false

if ($addPrToProject) {
    $projectTitle = $config.projectTitle
    if (-not $projectTitle) {
        $projectTitle = (gh project view $projectNumber --owner $owner --format json | ConvertFrom-Json).title
    }
    if ($projectTitle) {
        $prEditArgs += "--add-project"
        $prEditArgs += $projectTitle
        $didPrEdit = $true
        Write-Host "PR #$PrNumber -> project '$projectTitle'"
    }
}

$labelCsv = To-CommaList @($prLabels.ToArray())
if ($labelCsv) {
    $prEditArgs += "--add-label"
    $prEditArgs += $labelCsv
    $didPrEdit = $true
    Write-Host "PR #$PrNumber labels: $labelCsv"
}

$assigneeCsv = To-CommaList @($prAssignees.ToArray())
if ($assigneeCsv) {
    $prEditArgs += "--add-assignee"
    $prEditArgs += $assigneeCsv
    $didPrEdit = $true
    Write-Host "PR #$PrNumber assignees: $assigneeCsv"
}

if ($didPrEdit) {
    & gh @prEditArgs | Out-Null
}

$needsProjectFields = $addPrToProject -or ($IssueNumber.Count -gt 0 -and -not $SkipProjectStatus)
$projectId = $null
$statusField = $null
$issueStatusOption = $null
$prStatusOption = $null

if ($needsProjectFields -and -not $SkipProjectStatus) {
    $project = gh project view $projectNumber --owner $owner --format json | ConvertFrom-Json
    $projectId = $project.id

    $fieldsJson = gh project field-list $projectNumber --owner $owner --format json | ConvertFrom-Json
    $statusField = $fieldsJson.fields | Where-Object { $_.name -eq "Status" }

    $issueStatusOption = Get-StatusOption $statusField $issueStatusName
    $prStatusOption = Get-StatusOption $statusField $prStatusName
}

function Update-PrProjectStatus {
    if (-not $addPrToProject -or -not $projectId -or -not $statusField -or -not $prStatusOption) {
        return
    }

    $items = Get-ProjectItems $projectNumber $owner
    $prItem = Find-ProjectItemByContent $items "PullRequest" $PrNumber

    if (-not $prItem) {
        Write-Warning "PR #$PrNumber not found on project; PR Status not updated."
        return
    }

    Set-ProjectItemStatus $projectId $statusField $prStatusOption $prItem.id
    Write-Host "PR #$PrNumber project Status -> '$($prStatusOption.name)'"
}

if ($IssueNumber.Count -eq 0) {
    Update-PrProjectStatus
    return
}

$items = Get-ProjectItems $projectNumber $owner

foreach ($num in $IssueNumber) {
    $item = Find-ProjectItemByContent $items "Issue" $num

    if (-not $item -and $ensureIssuesOnProject) {
        $issueUrl = "https://github.com/$repo/issues/$num"
        gh project item-add $projectNumber --owner $owner --url $issueUrl | Out-Null
        Write-Host "Issue #$num added to project #$projectNumber"
        $items = Get-ProjectItems $projectNumber $owner
        $item = Find-ProjectItemByContent $items "Issue" $num
    }

    if ($item -and $issueStatusOption) {
        Set-ProjectItemStatus $projectId $statusField $issueStatusOption $item.id
        Write-Host "Issue #$num project Status -> '$($issueStatusOption.name)'"
    }

    $issueLabels = [System.Collections.Generic.List[string]]::new()
    foreach ($l in $configIssueLabels) { [void]$issueLabels.Add($l) }
    foreach ($l in $ExtraIssueLabels) { [void]$issueLabels.Add($l) }
    $issueLabelCsv = To-CommaList @($issueLabels.ToArray())

    $issueAssignees = [System.Collections.Generic.List[string]]::new()
    foreach ($a in $configIssueAssignees) { [void]$issueAssignees.Add($a) }
    foreach ($a in $ExtraIssueAssignees) { [void]$issueAssignees.Add($a) }
    $issueAssigneeCsv = To-CommaList @($issueAssignees.ToArray())

    $issueEditArgs = @("issue", "edit", $num)
    $didIssueEdit = $false

    if ($issueLabelCsv) {
        $issueEditArgs += "--add-label"
        $issueEditArgs += $issueLabelCsv
        $didIssueEdit = $true
        Write-Host "Issue #$num labels: $issueLabelCsv"
    }

    if ($issueAssigneeCsv) {
        $issueEditArgs += "--add-assignee"
        $issueEditArgs += $issueAssigneeCsv
        $didIssueEdit = $true
        Write-Host "Issue #$num assignees: $issueAssigneeCsv"
    }

    if ($didIssueEdit) {
        & gh @issueEditArgs | Out-Null
    }
}

Update-PrProjectStatus
