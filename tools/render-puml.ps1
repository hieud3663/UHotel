<#
.SYNOPSIS
Render all PlantUML .puml diagrams to image files.

.DESCRIPTION
This script renders every .puml file under docs/uml/plantuml to an output image
folder while preserving the activity/class/sequence/usecase subfolders.

It supports two render modes:
1. Local PlantUML jar, if provided with -PlantUmlJar or found as plantuml.jar.
2. Official PlantUML public server, if -UseServer is passed.

EXAMPLES
# Render PNG with local jar in the repository root or with explicit jar path
.\tools\render-puml.ps1
.\tools\render-puml.ps1 -PlantUmlJar .\plantuml.jar

# Render SVG
.\tools\render-puml.ps1 -Format svg

# Render via public PlantUML server, no jar required
.\tools\render-puml.ps1 -UseServer
#>

[CmdletBinding()]
param(
    [string]$InputDir = "docs/uml/plantuml",
    [string]$OutputDir = "docs/uml/images",
    [ValidateSet("png", "svg")]
    [string]$Format = "png",
    [string]$PlantUmlJar = "plantuml.jar",
    [switch]$UseServer,
    [string]$ServerUrl = "https://www.plantuml.com/plantuml"
)

$ErrorActionPreference = "Stop"

function Resolve-FullPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$TargetPath
    )

    $baseUri = [System.Uri]::new(($BasePath.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar))
    $targetUri = [System.Uri]::new($TargetPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
}

function ConvertTo-PlantUmlServerText {
    param([Parameter(Mandatory = $true)][string]$Text)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
    $deflater = [System.IO.Compression.DeflateStream]::new(([System.IO.MemoryStream]::new()), [System.IO.Compression.CompressionLevel]::Optimal)
    $output = [System.IO.MemoryStream]::new()
    $deflater = [System.IO.Compression.DeflateStream]::new($output, [System.IO.Compression.CompressionLevel]::Optimal, $true)
    $deflater.Write($bytes, 0, $bytes.Length)
    $deflater.Dispose()
    $compressed = $output.ToArray()
    $output.Dispose()

    $alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_"
    $encoded = [System.Text.StringBuilder]::new()

    for ($i = 0; $i -lt $compressed.Length; $i += 3) {
        if ($i + 2 -eq $compressed.Length) {
            $b1 = [int]$compressed[$i]
            $b2 = [int]$compressed[$i + 1]
            [void]$encoded.Append($alphabet[($b1 -shr 2) -band 0x3F])
            [void]$encoded.Append($alphabet[((($b1 -band 0x3) -shl 4) -bor ($b2 -shr 4)) -band 0x3F])
            [void]$encoded.Append($alphabet[(($b2 -band 0xF) -shl 2) -band 0x3F])
        }
        elseif ($i + 1 -eq $compressed.Length) {
            $b1 = [int]$compressed[$i]
            [void]$encoded.Append($alphabet[($b1 -shr 2) -band 0x3F])
            [void]$encoded.Append($alphabet[(($b1 -band 0x3) -shl 4) -band 0x3F])
        }
        else {
            $b1 = [int]$compressed[$i]
            $b2 = [int]$compressed[$i + 1]
            $b3 = [int]$compressed[$i + 2]
            [void]$encoded.Append($alphabet[($b1 -shr 2) -band 0x3F])
            [void]$encoded.Append($alphabet[((($b1 -band 0x3) -shl 4) -bor ($b2 -shr 4)) -band 0x3F])
            [void]$encoded.Append($alphabet[((($b2 -band 0xF) -shl 2) -bor ($b3 -shr 6)) -band 0x3F])
            [void]$encoded.Append($alphabet[$b3 -band 0x3F])
        }
    }

    return $encoded.ToString()
}

function Invoke-PlantUmlJarRender {
    param(
        [Parameter(Mandatory = $true)][System.IO.FileInfo[]]$Files,
        [Parameter(Mandatory = $true)][string]$InputRoot,
        [Parameter(Mandatory = $true)][string]$OutputRoot,
        [Parameter(Mandatory = $true)][string]$JarPath,
        [Parameter(Mandatory = $true)][string]$ImageFormat
    )

    $java = Get-Command java -ErrorAction SilentlyContinue
    if (-not $java) {
        throw "Không tìm thấy Java. Hãy cài Java hoặc chạy với -UseServer."
    }

    $jarFullPath = Resolve-FullPath $JarPath
    if (-not (Test-Path $jarFullPath)) {
        throw "Không tìm thấy PlantUML jar tại '$jarFullPath'. Hãy đặt plantuml.jar ở thư mục gốc, truyền -PlantUmlJar, hoặc dùng -UseServer."
    }

    foreach ($file in $Files) {
        $relativePath = Get-RelativePath -BasePath $InputRoot -TargetPath $file.FullName
        $relativeDir = Split-Path $relativePath -Parent
        $targetDir = if ($relativeDir) { Join-Path $OutputRoot $relativeDir } else { $OutputRoot }
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

        Write-Host "Rendering $relativePath -> $targetDir"
        & java "-Dfile.encoding=UTF-8" -jar $jarFullPath "-t$ImageFormat" -charset UTF-8 -o $targetDir $file.FullName
        if ($LASTEXITCODE -ne 0) {
            throw "PlantUML render thất bại với file: $($file.FullName)"
        }
    }
}

function Invoke-PlantUmlServerRender {
    param(
        [Parameter(Mandatory = $true)][System.IO.FileInfo[]]$Files,
        [Parameter(Mandatory = $true)][string]$InputRoot,
        [Parameter(Mandatory = $true)][string]$OutputRoot,
        [Parameter(Mandatory = $true)][string]$ImageFormat,
        [Parameter(Mandatory = $true)][string]$BaseUrl
    )

    foreach ($file in $Files) {
        $relativePath = Get-RelativePath -BasePath $InputRoot -TargetPath $file.FullName
        $relativeDir = Split-Path $relativePath -Parent
        $targetDir = if ($relativeDir) { Join-Path $OutputRoot $relativeDir } else { $OutputRoot }
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

        $targetFileName = [System.IO.Path]::ChangeExtension([System.IO.Path]::GetFileName($file.FullName), $ImageFormat)
        $targetFile = Join-Path $targetDir $targetFileName
        $sourceText = Get-Content -Raw -Encoding UTF8 -Path $file.FullName
        $encoded = ConvertTo-PlantUmlServerText -Text $sourceText
        $url = "$($BaseUrl.TrimEnd('/'))/$ImageFormat/$encoded"

        Write-Host "Rendering $relativePath -> $targetFile"
        Invoke-WebRequest -Uri $url -OutFile $targetFile | Out-Null
    }
}

$inputRoot = Resolve-FullPath $InputDir
$outputRoot = Resolve-FullPath $OutputDir

if (-not (Test-Path $inputRoot)) {
    throw "Không tìm thấy thư mục input: $inputRoot"
}

$files = @(Get-ChildItem -Path $inputRoot -Recurse -Filter *.puml -File | Sort-Object FullName)
if ($files.Count -eq 0) {
    throw "Không tìm thấy file .puml nào trong: $inputRoot"
}

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

if ($UseServer) {
    Invoke-PlantUmlServerRender -Files $files -InputRoot $inputRoot -OutputRoot $outputRoot -ImageFormat $Format -BaseUrl $ServerUrl
}
else {
    Invoke-PlantUmlJarRender -Files $files -InputRoot $inputRoot -OutputRoot $outputRoot -JarPath $PlantUmlJar -ImageFormat $Format
}

Write-Host "Done. Rendered $($files.Count) diagram(s) to $outputRoot"
