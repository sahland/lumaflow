[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $UnityPath,

    [Parameter(Mandatory = $true)]
    [string] $PackageArchive,

    [string] $WorkDirectory
)

$ErrorActionPreference = 'Stop'
$archivePath = [System.IO.Path]::GetFullPath($PackageArchive)
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity executable was not found at '$UnityPath'."
}
if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
    throw "Package archive was not found at '$archivePath'."
}
if ([string]::IsNullOrWhiteSpace($WorkDirectory)) {
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
    $WorkDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "LumaFlowTarballValidation-$stamp"
}
$projectRoot = [System.IO.Path]::GetFullPath($WorkDirectory)
if (Test-Path -LiteralPath $projectRoot) {
    throw "Validation directory already exists: '$projectRoot'."
}

function Invoke-Unity([string[]] $Arguments, [string] $Label) {
    $argumentLine = ($Arguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }) -join ' '
    $process = Start-Process -FilePath $UnityPath -ArgumentList $argumentLine -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        $logIndex = [Array]::IndexOf($Arguments, '-logFile')
        $hint = if ($logIndex -ge 0) { " Inspect '$($Arguments[$logIndex + 1])'." } else { '' }
        throw "$Label failed with Unity exit code $($process.ExitCode).$hint"
    }
}

$entries = @(tar -tf $archivePath | ForEach-Object { $_.Replace('\', '/') })
if ($LASTEXITCODE -ne 0) { throw 'Unable to read the package archive.' }
$required = @(
    'package/package.json',
    'package/Runtime/LumaFlow.Runtime.asmdef',
    'package/Editor/LumaFlow.Editor.asmdef',
    'package/Samples~/Getting Started/LumaFlowCounterSample.cs',
    'package/Documentation~/index.md',
    'package/ThirdPartyNotices/Lucide/LICENSE',
    'package/Runtime/Icons/Resources/LumaFlowIcons/lumaflow.svg',
    'package/Runtime/Icons/Resources/LumaFlowIcons/lumaflow.svg.meta',
    'package/Documentation~/Images/lumaflow.png'
)
foreach ($entry in $required) {
    if ($entries -notcontains $entry) { throw "Required archive entry is missing: $entry" }
}
$forbidden = @($entries | Where-Object {
    $_ -match '^package/(Tests|Tools)(/|$)' -or $_ -match '\.tgz(\.meta)?$'
})
if ($forbidden.Count -ne 0) { throw "Forbidden archive entries: $($forbidden -join ', ')" }

$extractRoot = "$projectRoot-extracted"
New-Item -ItemType Directory -Path $extractRoot | Out-Null
tar -xf $archivePath -C $extractRoot
if ($LASTEXITCODE -ne 0) { throw 'Unable to extract the package archive.' }

$bootstrapLog = "$projectRoot-bootstrap.log"
Invoke-Unity @('-batchmode', '-nographics', '-quit', '-createProject', $projectRoot, '-logFile', $bootstrapLog) 'Create validation project'

$artifactRoot = Join-Path $projectRoot 'Artifacts'
$testRoot = Join-Path $projectRoot 'Assets\LumaFlowTarballTests'
$sampleTarget = Join-Path $projectRoot 'Assets\Samples\LumaFlow\Getting Started'
New-Item -ItemType Directory -Path $artifactRoot, $testRoot, $sampleTarget | Out-Null
Copy-Item -LiteralPath $bootstrapLog -Destination (Join-Path $artifactRoot 'create-project.log') -Force
Copy-Item -Path (Join-Path $extractRoot 'package\Samples~\Getting Started\*') -Destination $sampleTarget -Recurse -Force

$manifestPath = Join-Path $projectRoot 'Packages\manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -AsHashtable
$archiveUri = 'file:' + $archivePath.Replace('\', '/')
$manifest.dependencies['com.sahland.lumaflow'] = $archiveUri
$manifest.dependencies['com.unity.test-framework'] = '1.6.0'
[System.IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 16), [System.Text.UTF8Encoding]::new($false))

$templateRoot = $PSScriptRoot
Copy-Item -LiteralPath (Join-Path $templateRoot 'LumaFlowTarballRuntimeSmokeTests.cs.txt') -Destination (Join-Path $testRoot 'LumaFlowTarballRuntimeSmokeTests.cs')
Copy-Item -LiteralPath (Join-Path $templateRoot 'LumaFlowTarballRuntimeSmokeTests.asmdef.txt') -Destination (Join-Path $testRoot 'LumaFlowTarballRuntimeSmokeTests.asmdef')
Copy-Item -LiteralPath (Join-Path $templateRoot 'LumaFlowTarballEditorSmokeTests.cs.txt') -Destination (Join-Path $testRoot 'LumaFlowTarballEditorSmokeTests.cs')
Copy-Item -LiteralPath (Join-Path $templateRoot 'LumaFlowTarballEditorSmokeTests.asmdef.txt') -Destination (Join-Path $testRoot 'LumaFlowTarballEditorSmokeTests.asmdef')

$importLog = Join-Path $artifactRoot 'import.log'
Invoke-Unity @('-batchmode', '-nographics', '-quit', '-projectPath', $projectRoot, '-logFile', $importLog) 'Import packed package'

function Assert-TestResult([string] $Path, [string] $Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Label did not produce a result document." }
    [xml] $document = Get-Content -LiteralPath $Path -Raw
    $run = $document.'test-run'
    if ($null -eq $run -or [int]$run.failed -ne 0 -or ([int]$run.skipped + [int]$run.inconclusive) -ne 0) {
        throw "$Label is not green. Inspect '$Path'."
    }
}

foreach ($suite in @(
    @{ Label = 'EditMode'; Platform = 'EditMode'; Assembly = 'LumaFlow.Tarball.Editor.Tests' },
    @{ Label = 'PlayMode'; Platform = 'PlayMode'; Assembly = 'LumaFlow.Tarball.Runtime.Tests' }
)) {
    $resultPath = Join-Path $artifactRoot "$($suite.Label)-results.xml"
    $logPath = Join-Path $artifactRoot "$($suite.Label).log"
    Invoke-Unity @('-batchmode', '-nographics', '-projectPath', $projectRoot, '-runTests', '-testPlatform', $suite.Platform, '-assemblyNames', $suite.Assembly, '-testResults', $resultPath, '-logFile', $logPath) $suite.Label
    Assert-TestResult $resultPath $suite.Label
}

Write-Host "Packed archive validation passed. Evidence: '$artifactRoot'."
