param(
    [string]$TargetFolder = "Publish"
)

$ErrorActionPreference = "Stop"

$ScriptDir = $PSScriptRoot
if (-not $ScriptDir) {
    $ScriptDir = (Get-Location).Path
}

$ProjectFile = Get-ChildItem -Path $ScriptDir -Filter "*.csproj" -Recurse | Select-Object -First 1
$RepoRoot = $ProjectFile.DirectoryName
$RunningDir = (Get-Location).Path
$SrcDir = Join-Path $RepoRoot "src"
$TestsDir = Join-Path $RepoRoot "ApiLogger.Tests"

Write-Host "=== Copying Source Code ===" -ForegroundColor Cyan

if (-not (Test-Path $SrcDir)) {
    Write-Error "Source folder not found: $SrcDir"
    exit 1
}

Write-Host "[1/8] Cleaning previous target folder..." -ForegroundColor Yellow
$TargetDir = Join-Path $RunningDir $TargetFolder
if (Test-Path $TargetDir) {
    Remove-Item -Path $TargetDir -Recurse -Force
}
$ZipFile = Join-Path $RunningDir "$TargetFolder.zip"
if (Test-Path $ZipFile) {
    Remove-Item -Path $ZipFile -Force
}

Write-Host "[2/8] Creating target folder..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null

Write-Host "[3/8] Copying source files..." -ForegroundColor Yellow
$SrcFolderInTarget = Join-Path $TargetDir "src"
Copy-Item -Path $SrcDir -Destination $SrcFolderInTarget -Recurse -Force

Write-Host "[4/8] Copying project file..." -ForegroundColor Yellow
$CsprojFiles = Get-ChildItem -Path $RepoRoot -Filter "*.csproj"
foreach ($File in $CsprojFiles) {
    $RelativePath = $File.Name
    $DestPath = Join-Path $TargetDir $RelativePath
    Copy-Item -Path $File.FullName -Destination $DestPath -Force
}

Write-Host "[5/8] Copying test files..." -ForegroundColor Yellow
if (Test-Path $TestsDir) {
    $TestsFolderInTarget = Join-Path $TargetDir "ApiLogger.Tests"
    $TestCsFiles = Get-ChildItem -Path $TestsDir -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' }
    foreach ($File in $TestCsFiles) {
        $RelativePath = $File.FullName.Substring($TestsDir.Length).TrimStart('\')
        $DestPath = Join-Path $TestsFolderInTarget $RelativePath
        $DestDir = Split-Path $DestPath -Parent
        if (-not (Test-Path $DestDir)) {
            New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
        }
        Copy-Item -Path $File.FullName -Destination $DestPath -Force
    }
    $TestCsprojFiles = Get-ChildItem -Path $TestsDir -Filter "*.csproj"
    foreach ($File in $TestCsprojFiles) {
        Copy-Item -Path $File.FullName -Destination (Join-Path $TestsFolderInTarget $File.Name) -Force
    }
}

$DocsDir = Join-Path $RepoRoot "docs"
Write-Host "[6/8] Copying documentation..." -ForegroundColor Yellow
if (Test-Path $DocsDir) {
    $DocsFolderInTarget = Join-Path $TargetDir "docs"
    Copy-Item -Path $DocsDir -Destination $DocsFolderInTarget -Recurse -Force
}

Write-Host "[7/8] Creating zip archive..." -ForegroundColor Yellow
Compress-Archive -Path "$TargetDir\*" -DestinationPath $ZipFile -Force

Write-Host "[8/8] Cleaning intermediate files..." -ForegroundColor Yellow
if (Test-Path $TargetDir) {
    Remove-Item -Path $TargetDir -Recurse -Force
}

Write-Host "=== Copy completed successfully ===" -ForegroundColor Green
Write-Host "Zip: $ZipFile" -ForegroundColor Cyan