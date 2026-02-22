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
  /p:PublishSingleFile=true `
  /p:PublishTrimmed=false `
  /p:DebugType=None `
  /p:DebugSymbols=false

# Clean up unused libvlc architectures
if ($Runtime -eq "win-x64") {
    $x86Path = Join-Path $outDir "libvlc\win-x86"
    if (Test-Path $x86Path) {
        Write-Host "Removing unused libvlc/win-x86..."
        Remove-Item $x86Path -Recurse -Force
    }
} elseif ($Runtime -eq "win-x86") {
    $x64Path = Join-Path $outDir "libvlc\win-x64"
    if (Test-Path $x64Path) {
        Write-Host "Removing unused libvlc/win-x64..."
        Remove-Item $x64Path -Recurse -Force
    }
}

Write-Host "Done. Output in: $outDir"
