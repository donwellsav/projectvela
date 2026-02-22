param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [string]$PackId = "ProjectVela",
    [string]$PackTitle = "Project Vela",
    [string]$Channel = "stable"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
$publishDir = Join-Path $root "publish"
$outDir = Join-Path $root "Releases"

if (!(Test-Path $publishDir)) {
    throw "Publish directory not found: $publishDir. Run scripts\publish-win.ps1 first."
}

Write-Host "Packing Velopack release from: $publishDir"
Write-Host "Output dir: $outDir"

vpk pack `
  --packId $PackId `
  --packVersion $Version `
  --packDir $publishDir `
  --mainExe "ProjectVela.exe" `
  --packTitle $PackTitle `
  --outputDir $outDir `
  --channel $Channel

Write-Host "Done. Installer and packages are in: $outDir"
