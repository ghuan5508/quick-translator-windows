$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'QuickTranslator.csproj'
$output = Join-Path $PSScriptRoot 'publish'
dotnet publish $project -c Release -r win-x64 --self-contained true -o $output
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
Write-Host "Built: $(Join-Path $output 'QuickTranslator.exe')"
