param([Parameter(Mandatory=$true)][string]$GameDir)
$ErrorActionPreference = 'Stop'
$managed = Join-Path (Resolve-Path -LiteralPath $GameDir).Path 'Probably Stolen_Data/Managed'
foreach ($name in @('Assembly-CSharp.dll','0Harmony.dll','DseCecil.dll','UnityEngine.CoreModule.dll','mscorlib.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $managed $name) -PathType Leaf)) { throw "Missing game dependency: $name" }
}
$output = Join-Path $PSScriptRoot 'out'
& dotnet build (Join-Path $PSScriptRoot 'src/HotLoad.csproj') -c Release "-p:GameManaged=$managed" -o $output
if ($LASTEXITCODE -ne 0) { throw 'Build failed. No installation was performed.' }
Get-FileHash -LiteralPath (Join-Path $output 'HotLoad.dll') -Algorithm SHA256
Write-Output 'Build only. Follow README.md for a separate, manual installation.'
