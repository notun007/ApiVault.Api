$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
python "$PSScriptRoot/validate_source.py"
dotnet restore "$Root/ApiVault.sln"
dotnet build "$Root/ApiVault.sln" --configuration Release --no-restore
