$ErrorActionPreference = "Stop"

$ScriptDir = $PSScriptRoot
if (-not $ScriptDir) {
    $ScriptDir = (Get-Location).Path
}

Write-Host "=== Building ApiLogger ===" -ForegroundColor Cyan

$ProjectFile = Get-ChildItem -Path $ScriptDir -Filter "*.csproj" -Recurse | Select-Object -First 1
if (-not $ProjectFile) {
    Write-Error "No .csproj file found in $ScriptDir"
    exit 1
}

$ProjectDir = $ProjectFile.DirectoryName
$BuildDir = Join-Path $ProjectDir "Build"

Write-Host "[1/4] Cleaning Build folder..." -ForegroundColor Yellow
if (Test-Path $BuildDir) {
    Remove-Item -Path $BuildDir -Recurse -Force
}
New-Item -ItemType Directory -Path $BuildDir -Force | Out-Null

Push-Location $ProjectDir

try {
    Write-Host "[2/4] Restoring packages..." -ForegroundColor Yellow
    dotnet restore --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Restore failed"
        exit 1
    }

    Write-Host "[3/4] Building and publishing..." -ForegroundColor Yellow
    dotnet publish -c Release -o $BuildDir --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed"
        exit 1
    }

    Write-Host "[4/4] Cleaning intermediate files..." -ForegroundColor Yellow
    $BinDir = Join-Path $ProjectDir "bin"
    $ObjDir = Join-Path $ProjectDir "obj"

    if (Test-Path $BinDir) {
        Remove-Item -Path $BinDir -Recurse -Force
    }

    if (Test-Path $ObjDir) {
        Remove-Item -Path $ObjDir -Recurse -Force
    }

    Write-Host "=== Build completed successfully ===" -ForegroundColor Green
    Write-Host "Output: $BuildDir" -ForegroundColor Cyan
}
finally {
    Pop-Location
}