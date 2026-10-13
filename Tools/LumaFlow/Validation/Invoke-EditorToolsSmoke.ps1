[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $UnityPath,
    [Parameter(Mandatory = $true)] [string] $OutputDirectory,
    [ValidateSet('Edit', 'Performance', 'QuickStart', 'All')] [string] $Mode = 'All'
)

$ErrorActionPreference = 'Stop'
$sourceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Use a new output directory; existing evidence is never overwritten.' }
$unityData = Join-Path (Split-Path $UnityPath) 'Data'
$sdkLine = @(dotnet --list-sdks)[-1]
if ($sdkLine -notmatch '^(\S+) \[(.+)\]$') { throw 'A .NET SDK is required.' }
$compiler = Join-Path $Matches[2] "$($Matches[1])/Roslyn/bincore/csc.dll"
$references = @(Get-ChildItem "$unityData/NetStandard/ref/2.1.0/*.dll", "$unityData/Managed/UnityEngine/*.dll",
    "$unityData/NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll" | ForEach-Object { '-r:' + $_.FullName })
$editor = Join-Path $output 'Assets/Editor'
$plugins = Join-Path $output 'Assets/Plugins'
New-Item -ItemType Directory -Path $editor, $plugins, "$output/ProjectSettings" | Out-Null
Copy-Item "$sourceRoot/ProjectSettings/ProjectVersion.txt" "$output/ProjectSettings/"
$package = Join-Path $sourceRoot 'Packages/com.sahland.lumaflow'
$checkResources = Join-Path $output 'Assets/Resources/LumaFlowIcons'
New-Item -ItemType Directory -Path $checkResources | Out-Null
Copy-Item "$package/Runtime/Icons/Resources/LumaFlowIcons/check.svg", "$package/Runtime/Icons/Resources/LumaFlowIcons/check.svg.meta" $checkResources
$runtimeSources = @(Get-ChildItem "$package/Runtime" -Recurse -Filter '*.cs' | ForEach-Object FullName)
& dotnet $compiler -nologo -target:library -langversion:9.0 -define:UNITY_EDITOR "-out:$plugins/LumaFlow.Runtime.dll" $references $runtimeSources
if ($LASTEXITCODE) { throw 'Runtime compilation failed.' }
$references += "-r:$plugins/LumaFlow.Runtime.dll"
$previewSources = @(
    Get-ChildItem "$package/Editor/UxmlPreview*.cs" | ForEach-Object FullName
    "$package/Editor/LumaPreviewAttribute.cs"
    "$package/Editor/AssemblyInfo.cs"
    "$package/Editor/EditorToolWidgets.cs"
    "$package/Editor/GuiPerformanceWindow.cs"
    "$package/Editor/PublicApiSnapshot.cs"
)
& dotnet $compiler -nologo -target:library -langversion:9.0 -define:UNITY_EDITOR "-out:$editor/LumaFlow.Editor.dll" $references $previewSources
if ($LASTEXITCODE) { throw 'Editor tools compilation failed.' }
$harness = if ($Mode -eq 'QuickStart') { 'QuickStartSmoke.cs.txt' } else { 'EditorToolsSmoke.cs.txt' }
if ($Mode -eq 'QuickStart') {
    New-Item -ItemType Directory -Path "$editor/Templates" | Out-Null
    Copy-Item "$package/Editor/Templates/AppBootstrap.cs.txt" "$editor/Templates/AppBootstrap.cs.txt"
}
& dotnet $compiler -nologo -target:library -langversion:9.0 "-out:$editor/LumaFlow.Editor.Tests.dll" $references `
    "-r:$editor/LumaFlow.Editor.dll" "$PSScriptRoot/$harness"
if ($LASTEXITCODE) { throw 'Editor tools harness compilation failed.' }
$modes = if ($Mode -eq 'All') { @('Edit', 'Performance') } else { @($Mode) }
foreach ($phase in $modes) {
    $log = Join-Path $output ($phase.ToLowerInvariant() + '.log')
    $entryType = if ($phase -eq 'QuickStart') { 'QuickStartValidation' } else { 'EditorToolsValidation' }
    $arguments = '-batchmode -force-d3d11 -noUpm -projectPath "' + $output + '" -executeMethod ' + $entryType + '.Run' + $phase + ' -logFile "' + $log + '"'
    $process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(60000)) { Stop-Process -Id $process.Id; throw "$phase validation timed out. See $log" }
    $marker = if ($phase -eq 'QuickStart') { 'EDITOR_TOOLS_QUICKSTART PASSED' } elseif ($phase -eq 'Edit') { 'EDITOR_TOOLS_EDIT PASSED' } else { 'EDITOR_TOOLS_PERFORMANCE PASSED' }
    if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -SimpleMatch $marker -Quiet)) {
        throw "$phase validation failed. See $log"
    }
    Write-Output "$phase validation passed. Evidence: $log"
}
