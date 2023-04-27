param ($apiKey = $(Read-Host "NuGet API Key"))

$thisDir = Split-Path $MyInvocation.MyCommand.Definition -Parent

$ErrorActionPreference = "Stop"

$srcDir = "$thisDir"
$project = "Mastersign.ConfigModel\Mastersign.ConfigModel.csproj"
$packageDir = "$thisDir\publish"
$packageName = "Mastersign.ConfigModel"

$srcDir = Resolve-Path $srcDir
Push-Location $srcDir
dotnet build -c Release $project
dotnet pack -c Release --no-restore --no-build -o $packageDir $project
Pop-Location

$packageDir = Resolve-Path $packageDir
Push-Location $packageDir
foreach ($package in (Get-ChildItem "${packageName}.*.nupkg")) {
    dotnet nuget push $package -s https://api.nuget.org/v3/index.json -k $apiKey --skip-duplicate
}
Pop-Location
