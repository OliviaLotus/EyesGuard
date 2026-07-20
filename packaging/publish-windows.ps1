param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string] $Runtime = 'win-x64',
    [string] $Configuration = 'Release',
    [string] $OutputRoot = '.artifacts\windows'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\EyesGuard.App\EyesGuard.App.csproj'
$output = Join-Path $PSScriptRoot "..\$OutputRoot\$Runtime"
$output = [System.IO.Path]::GetFullPath($output)

dotnet publish $project -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false -o $output

$zip = Join-Path (Split-Path $output -Parent) "EyesGuard-$Runtime.zip"
if (Test-Path $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path (Join-Path $output '*') -DestinationPath $zip
Write-Host "Published: $output"
Write-Host "Archive:   $zip"
