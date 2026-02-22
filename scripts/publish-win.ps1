param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $true
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
$proj = Join-Path $root "src\ConferencePlayer.App\ConferencePlayer.App.csproj"
$outDir = Join-Path $root "publish"

if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }

Write-Host "Publishing $proj -> $outDir"
dotnet publish $proj -c $Configuration -r $Runtime --self-contained:$SelfContained -o $outDir `
  /p:PublishSingleFile=false `
  /p:PublishTrimmed=false

Write-Host "Done. Output in: $outDir"
