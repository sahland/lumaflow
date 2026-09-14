[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $UnityPath,
    [Parameter(Mandatory = $true)] [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Use a new output directory; existing evidence is never overwritten.' }
$unityData = Join-Path (Split-Path $UnityPath) 'Data'
$sdkLine = @(dotnet --list-sdks)[-1]
if ($sdkLine -notmatch '^(\S+) \[(.+)\]$') { throw 'A .NET SDK is required for the standalone compiler.' }
$compiler = Join-Path $Matches[2] "$($Matches[1])/Roslyn/bincore/csc.dll"
$references = @(Get-ChildItem "$unityData/NetStandard/ref/2.1.0/*.dll", "$unityData/Managed/UnityEngine/*.dll",
    "$unityData/NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll" | ForEach-Object { '-r:' + $_.FullName })
$nunit = Get-ChildItem "$root/Library/PackageCache/com.unity.ext.nunit*/net40/unity-custom/nunit.framework.dll" | Select-Object -First 1
if ($null -eq $nunit) { throw 'The development project must have a cached Unity NUnit assembly.' }
$editor = Join-Path $output 'Assets/Editor'
$plugins = Join-Path $output 'Assets/Plugins'
New-Item -ItemType Directory -Path $editor, $plugins, "$output/ProjectSettings" | Out-Null
Copy-Item "$root/ProjectSettings/ProjectVersion.txt" "$output/ProjectSettings/"
$package = Join-Path $root 'Packages/com.sahland.lumaflow'
$runtimeSources = @(Get-ChildItem "$package/Runtime" -Recurse -Filter '*.cs' | ForEach-Object FullName)
& dotnet $compiler -nologo -target:library -langversion:9.0 "-out:$plugins/LumaFlow.Runtime.dll" $references $runtimeSources
if ($LASTEXITCODE) { throw 'Runtime compilation failed.' }
$references += "-r:$plugins/LumaFlow.Runtime.dll"
$previewSources = @(Get-ChildItem "$package/Editor/UxmlPreview*.cs" | ForEach-Object FullName)
& dotnet $compiler -nologo -target:library -langversion:9.0 "-out:$editor/LumaFlow.Preview.Compile.dll" $references $previewSources
if ($LASTEXITCODE) { throw 'Preview compilation failed.' }
& dotnet $compiler -nologo -target:library -langversion:9.0 "-out:$editor/LumaFlow.Preview.Tests.dll" $references `
    "-r:$editor/LumaFlow.Preview.Compile.dll" "-r:$($nunit.FullName)" "$root/Assets/LumaFlow.Tests/Editor/UxmlPreviewTests.cs"
if ($LASTEXITCODE) { throw 'Preview test compilation failed.' }
Copy-Item $nunit.FullName $editor
Copy-Item "$PSScriptRoot/UxmlPreviewSmoke.cs.txt" "$editor/UxmlPreviewSmoke.cs"
$log = Join-Path $output 'smoke.log'
$arguments = '-batchmode -nographics -noUpm -projectPath "' + $output + '" -executeMethod UxmlPreviewSmoke.Run -logFile "' + $log + '"'
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(180000)) {
    Stop-Process -Id $process.Id
    throw "Preview smoke timed out. See $log"
}
if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -SimpleMatch 'UXML_PREVIEW_SMOKE_PASSED' -Quiet)) {
    throw "Preview smoke failed. See $log"
}
Write-Output "Preview compile, regression and layout checks passed. Evidence: $log"
