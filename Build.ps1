param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Get-ScriptDirectory {
    return Split-Path -Parent $PSCommandPath
}

function Write-Step {
    param([string]$Message)
    Write-Host "[Build] $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "[Build] $Message" -ForegroundColor Green
}

function Write-ErrorStop {
    param([string]$Message)
    Write-Host "[Build] ERROR: $Message" -ForegroundColor Red
    exit 1
}

function Find-ProjectFile {
    param([string]$SearchPath)

    $project = Get-ChildItem -Path $SearchPath -Filter "*.csproj" -Recurse | Select-Object -First 1
    if (-not $project) {
        Write-ErrorStop "No .csproj file found in $SearchPath"
    }
    return $project
}

function Clear-Directory {
    param([string]$Path)

    if (Test-Path $Path) {
        Remove-Item -Path $Path -Recurse -Force
    }
}

function Find-MSBuild {
    Write-Host "Searching for MSBuild..." -ForegroundColor Yellow

    $vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vsWhere) {
        $instances = & $vsWhere -all -format json 2>&1 | ConvertFrom-Json
        if ($instances) {
            $sorted = $instances | Sort-Object -Property installationVersion -Descending
            foreach ($instance in $sorted) {
                foreach ($sub in @("MSBuild\Current\Bin\MSBuild.exe", "MSBuild\15.0\Bin\MSBuild.exe")) {
                    $msbuild = Join-Path $instance.installationPath $sub
                    if (Test-Path $msbuild) {
                        Write-Host "  Found (vswhere): $msbuild" -ForegroundColor Green
                        return $msbuild
                    }
                }
            }
        }
    }

    $candidates = @(
        "C:\Program Files\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe",
        "C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            Write-Host "  Found (hardcoded): $candidate" -ForegroundColor Green
            return $candidate
        }
    }

    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        Write-Host "  Found (PATH): $($command.Source)" -ForegroundColor Yellow
        return $command.Source
    }

    throw "MSBuild.exe was not found. Install Visual Studio Build Tools or Visual Studio."
}

function Invoke-MSBuild {
    param(
        [Parameter(Mandatory)]
        [string]$MsBuildPath,
        [Parameter(Mandatory)]
        [string]$ProjectPath,
        [string]$Configuration = "Release",
        [string]$Platform = "AnyCPU",
        [string[]]$ExtraArgs
    )

    $msbuildArgs = @(
        $ProjectPath
        "/t:Build"
        "/p:Configuration=$Configuration"
        "/p:Platform=$Platform"
        "/consoleloggerparameters:Summary"
        "/noLogo"
    )

    if ($ExtraArgs) {
        $msbuildArgs += $ExtraArgs
    }

    Write-Host "Building via $MsBuildPath" -ForegroundColor Cyan
    $output = & $MsBuildPath @msbuildArgs 2>&1
    $output | ForEach-Object { Write-Host $_ }

    if ($LASTEXITCODE -ne 0) {
        Write-Host "MSBuild exited with code $LASTEXITCODE. Full output above." -ForegroundColor Red
        throw "Build failed with exit code $LASTEXITCODE"
    }
}

function Invoke-DotNetRestore {
    param([string]$ProjectDir)

    Write-Step "[1/4] Restoring NuGet packages..."
    Push-Location $ProjectDir
    try {
        dotnet restore --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorStop "NuGet restore failed"
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-DotNetPublish {
    param(
        [string]$ProjectDir,
        [string]$OutputDir,
        [string]$Configuration
    )

    Write-Step "[2/4] Building and publishing..."
    Push-Location $ProjectDir
    try {
        dotnet publish -c $Configuration -o $OutputDir --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorStop "Build failed"
        }
    }
    finally {
        Pop-Location
    }
}

function Invoke-Cleanup {
    param([string]$ProjectDir)

    Write-Step "[3/4] Cleaning intermediate files..."
    Clear-Directory -Path (Join-Path $ProjectDir "bin")
    Clear-Directory -Path (Join-Path $ProjectDir "obj")
}

$ScriptDir = Get-ScriptDirectory
Write-Host "=== Building ApiLogger ===" -ForegroundColor Cyan

$ProjectFile = Find-ProjectFile -SearchPath $ScriptDir
$ProjectDir = $ProjectFile.DirectoryName
$BuildDir = Join-Path $ProjectDir "Build"

Write-Step "[0/4] Preparing Build folder..."
Clear-Directory -Path $BuildDir
New-Item -ItemType Directory -Path $BuildDir -Force | Out-Null

try {
    Invoke-DotNetRestore -ProjectDir $ProjectDir
    Invoke-DotNetPublish -ProjectDir $ProjectDir -OutputDir $BuildDir -Configuration $Configuration
    Invoke-Cleanup -ProjectDir $ProjectDir

    Write-Success "=== Build completed successfully ==="
    Write-Host "[Build] Output: $BuildDir" -ForegroundColor Cyan
}
catch {
    Write-ErrorStop $_.Exception.Message
}
