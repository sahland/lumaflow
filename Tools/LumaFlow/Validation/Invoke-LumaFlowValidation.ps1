[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $UnityPath,

    [ValidateSet('EditMode', 'PlayMode', 'StandaloneWindows64Mono', 'StandaloneWindows64Il2Cpp')]
    [string[]] $Modes = @('EditMode', 'PlayMode'),

    [string] $WorkDirectory
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$packageRoot = Join-Path $repositoryRoot 'Packages\com.sahland.lumaflow'
$testRoot = Join-Path $repositoryRoot 'Assets\LumaFlow.Tests'
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity executable was not found at '$UnityPath'."
}

if ([string]::IsNullOrWhiteSpace($WorkDirectory)) {
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
    $WorkDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "LumaFlowValidation-$stamp"
}
$projectRoot = [System.IO.Path]::GetFullPath($WorkDirectory)
if (Test-Path -LiteralPath $projectRoot) {
    throw "Validation directory already exists: '$projectRoot'. Supply a new empty path."
}

function Invoke-Unity([string[]] $Arguments, [string] $Label) {
    Write-Host "[$Label] $UnityPath $($Arguments -join ' ')"
    $argumentLine = ($Arguments | ForEach-Object {
        '"' + $_.Replace('"', '\"') + '"'
    }) -join ' '
    $process = Start-Process -FilePath $UnityPath -ArgumentList $argumentLine `
        -Wait -PassThru -WindowStyle Hidden
    $exitCode = $process.ExitCode
    if ($exitCode -ne 0) {
        $logIndex = [Array]::IndexOf($Arguments, '-logFile')
        $logHint = if ($logIndex -ge 0 -and $logIndex + 1 -lt $Arguments.Length) {
            " Inspect '$($Arguments[$logIndex + 1])'."
        } else {
            ''
        }
        throw "$Label failed with Unity exit code $exitCode.$logHint"
    }
}

$bootstrapLog = Join-Path ([System.IO.Path]::GetTempPath()) `
    "$([System.IO.Path]::GetFileName($projectRoot))-bootstrap.log"
$bootstrapArguments = @(
    '-batchmode', '-nographics', '-quit', '-createProject', $projectRoot,
    '-logFile', $bootstrapLog
)
try {
    Invoke-Unity $bootstrapArguments 'Create validation project'
} catch {
    # Unity 6000.4 can create the folder but leave an empty manifest and report
    # a Package Manager error. Seed a minimal manifest and continue with the
    # created project instead of mistaking this editor bootstrap defect for a
    # package failure.
    $bootstrapManifestPath = Join-Path $projectRoot 'Packages\manifest.json'
    if (-not (Test-Path -LiteralPath $bootstrapManifestPath -PathType Leaf)) { throw }
    $bootstrapManifest = @{
        dependencies = @{
            'com.unity.modules.accessibility' = '1.0.0'
            'com.unity.modules.uielements' = '1.0.0'
            'com.unity.modules.vectorgraphics' = '1.0.0'
            'com.unity.test-framework' = '1.6.0'
            'com.unity.test-framework.performance' = '3.4.0'
        }
    } | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText(
        $bootstrapManifestPath,
        $bootstrapManifest,
        [System.Text.UTF8Encoding]::new($false))

    $sourceProjectVersion = Join-Path $repositoryRoot 'ProjectSettings\ProjectVersion.txt'
    $targetProjectVersion = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'
    if (Test-Path -LiteralPath $sourceProjectVersion -PathType Leaf) {
        Copy-Item -LiteralPath $sourceProjectVersion -Destination $targetProjectVersion -Force
    }
}

$packageTarget = Join-Path $projectRoot 'Packages\com.sahland.lumaflow'
$testTarget = Join-Path $projectRoot 'Assets\LumaFlow.Tests'
$artifactRoot = Join-Path $projectRoot 'Artifacts'
$sampleTarget = Join-Path $projectRoot 'Assets\LumaFlowGettingStarted'
New-Item -ItemType Directory -Path $packageTarget, $testTarget, $artifactRoot, $sampleTarget,
    (Join-Path $projectRoot 'Assets\Editor') | Out-Null
Copy-Item -LiteralPath $bootstrapLog -Destination (Join-Path $artifactRoot 'create-project.log') -Force
Copy-Item -Path (Join-Path $packageRoot '*') -Destination $packageTarget -Recurse -Force
Copy-Item -Path (Join-Path $testRoot '*') -Destination $testTarget -Recurse -Force
Copy-Item -LiteralPath (Join-Path $packageRoot 'Samples~\Getting Started\LumaFlowCounterSample.cs') `
    -Destination $sampleTarget -Force

$manifestPath = Join-Path $projectRoot 'Packages\manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -AsHashtable
$manifest.dependencies['com.unity.test-framework'] = '1.6.0'
$manifest.dependencies['com.unity.test-framework.performance'] = '3.4.0'
$manifest.testables = @('com.sahland.lumaflow')
[System.IO.File]::WriteAllText(
    $manifestPath,
    ($manifest | ConvertTo-Json -Depth 16),
    [System.Text.UTF8Encoding]::new($false))

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LumaFlowValidationSettings.cs.txt') `
    -Destination (Join-Path $projectRoot 'Assets\Editor\LumaFlowValidationSettings.cs') -Force

function Assert-TestResult([string] $Path, [string] $Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label did not produce '$Path'."
    }
    [xml] $document = Get-Content -Raw -LiteralPath $Path
    $run = $document.'test-run'
    if ($null -eq $run) { throw "$Label produced an unrecognized NUnit result document." }
    $failed = [int] $run.failed
    $passed = [int] $run.passed
    $skipped = [int] $run.skipped + [int] $run.inconclusive
    Write-Host "[$Label] passed=$passed failed=$failed skipped-or-inconclusive=$skipped"
    if ($failed -ne 0 -or $skipped -ne 0) {
        throw "$Label is not green. Preserve the project and inspect '$Path'."
    }
}

foreach ($mode in $Modes) {
    $testPlatform = $mode
    $assemblyNames = 'LumaFlow.Runtime.Tests'
    if ($mode -eq 'EditMode') {
        $assemblyNames = 'LumaFlow.Editor.Tests'
    } elseif ($mode -eq 'PlayMode') {
        $testPlatform = 'PlayMode'
    } elseif ($mode -eq 'StandaloneWindows64Mono') {
        Invoke-Unity @(
            '-batchmode', '-nographics', '-projectPath', $projectRoot,
            '-executeMethod', 'LumaFlowValidationSettings.UseMono',
            '-logFile', (Join-Path $artifactRoot 'configure-mono.log')
        ) 'Configure Windows Mono'
        $testPlatform = 'StandaloneWindows64'
    } elseif ($mode -eq 'StandaloneWindows64Il2Cpp') {
        Invoke-Unity @(
            '-batchmode', '-nographics', '-projectPath', $projectRoot,
            '-executeMethod', 'LumaFlowValidationSettings.UseIl2Cpp',
            '-logFile', (Join-Path $artifactRoot 'configure-il2cpp.log')
        ) 'Configure Windows IL2CPP'
        $testPlatform = 'StandaloneWindows64'
    }

    $resultPath = Join-Path $artifactRoot "$mode-results.xml"
    $logPath = Join-Path $artifactRoot "$mode.log"
    Invoke-Unity @(
        '-batchmode', '-nographics', '-projectPath', $projectRoot,
        '-runTests', '-testPlatform', $testPlatform,
        '-assemblyNames', $assemblyNames,
        '-testResults', $resultPath,
        '-logFile', $logPath
    ) $mode
    Assert-TestResult $resultPath $mode
}

Write-Host "Validation project preserved at '$projectRoot'."
