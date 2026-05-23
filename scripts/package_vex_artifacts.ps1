[CmdletBinding()]
param(
    [string[]]$RuntimeIdentifier = @(
        "win-x64",
        "linux-x64",
        "linux-arm64",
        "osx-x64",
        "osx-arm64"
    ),
    [string]$Version = "",
    [string]$PublishRoot = "",
    [string]$ArtifactsRoot = "",
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")

if ([string]::IsNullOrWhiteSpace($Version)) {
    $buildPropsPath = Join-Path $repoRoot "Directory.Build.props"
    if (-not (Test-Path -LiteralPath $buildPropsPath -PathType Leaf)) {
        throw "Directory.Build.props was not found. Pass -Version explicitly."
    }

    [xml]$buildProps = Get-Content -Raw -Encoding UTF8 -LiteralPath $buildPropsPath
    $versionNode = $buildProps.Project.PropertyGroup |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_.Version) } |
        Select-Object -First 1

    if ($null -eq $versionNode) {
        throw "Directory.Build.props does not define Version. Pass -Version explicitly."
    }

    $Version = [string]$versionNode.Version
}

if ([string]::IsNullOrWhiteSpace($PublishRoot)) {
    $PublishRoot = Join-Path $repoRoot "publish"
}

if ([string]::IsNullOrWhiteSpace($ArtifactsRoot)) {
    $ArtifactsRoot = Join-Path $repoRoot "artifacts\release"
}

New-Item -ItemType Directory -Force -Path $ArtifactsRoot | Out-Null

$packagedAt = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
$packages = New-Object System.Collections.Generic.List[object]

foreach ($rid in $RuntimeIdentifier) {
    if ([string]::IsNullOrWhiteSpace($rid)) {
        throw "Runtime identifier cannot be empty."
    }

    $publishDir = Join-Path $PublishRoot $rid
    if (-not (Test-Path -LiteralPath $publishDir -PathType Container)) {
        throw "Publish directory '$publishDir' was not found. Run publish_vex_all.bat first or pass -PublishRoot."
    }

    $entries = @(Get-ChildItem -LiteralPath $publishDir -Recurse -File)
    if ($entries.Count -eq 0) {
        throw "Publish directory '$publishDir' does not contain files."
    }

    $archiveName = "Vex-$Version-$rid.zip"
    $archivePath = Join-Path $ArtifactsRoot $archiveName
    $checksumPath = "$archivePath.sha256"

    $existingOutputs = @(
        @($archivePath, $checksumPath) |
            Where-Object { Test-Path -LiteralPath $_ -PathType Leaf }
    )

    if ($existingOutputs.Count -gt 0 -and -not $Force) {
        $existingList = $existingOutputs -join "', '"
        throw "Artifact output already exists: '$existingList'. Pass -Force to replace it."
    }

    Compress-Archive `
        -Path (Join-Path $publishDir "*") `
        -DestinationPath $archivePath `
        -CompressionLevel Optimal `
        -Force:$Force

    $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath
    $sha256 = $hash.Hash.ToLowerInvariant()
    Set-Content -Encoding ASCII -LiteralPath $checksumPath -Value "$sha256  $archiveName"

    $uncompressedBytes = ($entries | Measure-Object -Property Length -Sum).Sum
    if ($null -eq $uncompressedBytes) {
        $uncompressedBytes = 0
    }

    $packages.Add([ordered]@{
        product = "Vex"
        version = $Version
        runtimeIdentifier = $rid
        archive = $archiveName
        sha256 = $sha256
        fileCount = $entries.Count
        uncompressedBytes = [int64]$uncompressedBytes
    }) | Out-Null

    Write-Host "Packaged $archiveName"
}

$manifestPath = Join-Path $ArtifactsRoot "Vex-$Version-release-manifest.json"
if ((Test-Path -LiteralPath $manifestPath -PathType Leaf) -and -not $Force) {
    throw "Release manifest already exists: '$manifestPath'. Pass -Force to replace it."
}

$manifest = [ordered]@{
    product = "Vex"
    version = $Version
    packagedAt = $packagedAt
    packages = $packages
}

$manifest |
    ConvertTo-Json -Depth 6 |
    Set-Content -Encoding UTF8 -LiteralPath $manifestPath

Write-Host "Wrote release manifest $manifestPath"
