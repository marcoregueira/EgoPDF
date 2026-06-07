<#
.SYNOPSIS
    Pack EgoPDF.Generator + EgoPDF.Barcodes + EgoPDF.Markdown and push
    them to nuget.org.

.DESCRIPTION
    Runs `dotnet pack -c Release` on each NuGet project, drops the
    .nupkg / .snupkg into ./artifacts and (unless -PackOnly is passed)
    pushes them to https://api.nuget.org/v3/index.json.

    By default the EgoPDF.Barcodes and EgoPDF.Markdown packages ship
    with the SDK's optimistic `version="X.Y.Z"` dependency on
    EgoPDF.Generator (NuGet treats it as `>= X.Y.Z`), which is what
    most consumers want. Pass -PinDependencies to rewrite their
    .nuspec into an exact pin (`version="[X.Y.Z]"`) -- useful when
    cutting a release that needs to lock to a specific Generator
    build the downstream packages were tested against.

    The API key can be supplied via -ApiKey or by setting the
    NUGET_API_KEY environment variable beforehand. The script never
    echoes the key.

.PARAMETER ApiKey
    Your nuget.org API key. Defaults to $env:NUGET_API_KEY.

.PARAMETER Source
    Push target. Defaults to nuget.org. Set to a different feed (e.g.
    a private MyGet/GitHub Packages URL) if you don't want to publish
    on the public gallery.

.PARAMETER PackOnly
    Skip the push step. Useful for verifying that pack works and
    inspecting the resulting .nupkg locally before publishing.

.PARAMETER SkipDuplicate
    Pass --skip-duplicate to `dotnet nuget push` so re-running the
    script with the same versions is a no-op instead of an error.

.PARAMETER Generator
    Only pack/push the EgoPDF.Generator package. Combine with -Barcodes
    and/or -Markdown to publish a subset (default: publish all three).

.PARAMETER Barcodes
    Only pack/push the EgoPDF.Barcodes package. -Zpl is kept as an
    alias for the same flag (the package was previously named
    EgoPDF.Zpl before the rename).

.PARAMETER Markdown
    Only pack/push the EgoPDF.Markdown package.

.PARAMETER PinDependencies
    Rewrite the Barcodes / Markdown .nuspec so their EgoPDF.Generator
    dependency becomes an exact pin (`[X.Y.Z]`) instead of NuGet's
    default `>=` semantics. Off by default -- use it when you want
    downstream consumers to absorb only the specific Generator build
    you tested against.

.EXAMPLE
    # Set the key once per shell, then publish all three packages.
    $env:NUGET_API_KEY = 'oy2...'
    ./publish.ps1

.EXAMPLE
    # Only build the .nupkg files (no push).
    ./publish.ps1 -PackOnly

.EXAMPLE
    # Publish only the preview Barcodes package.
    ./publish.ps1 -Barcodes

.EXAMPLE
    # Publish Markdown alone with an exact Generator pin.
    ./publish.ps1 -Markdown -PinDependencies
#>
[CmdletBinding()]
param(
    [string] $ApiKey = $env:NUGET_API_KEY,
    [string] $Source = 'https://api.nuget.org/v3/index.json',
    [switch] $PackOnly,
    [switch] $SkipDuplicate,
    [switch] $Generator,
    [Alias('Zpl')]
    [switch] $Barcodes,
    [switch] $Markdown,
    [switch] $PinDependencies
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'

# If none of the package switches was supplied, publish all three.
$publishAll       = -not ($Generator -or $Barcodes -or $Markdown)
$publishGenerator = $publishAll -or $Generator
$publishBarcodes  = $publishAll -or $Barcodes
$publishMarkdown  = $publishAll -or $Markdown

if (-not (Test-Path $artifacts)) {
    New-Item -ItemType Directory -Path $artifacts | Out-Null
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Invoke-Pack {
    param([string] $Project)
    Write-Host "==> pack $Project" -ForegroundColor Cyan
    & dotnet pack $Project -c Release -o $artifacts --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed for $Project (exit $LASTEXITCODE)" }
}

function Invoke-Push {
    param([string] $Pattern)
    if ($PackOnly) { return }
    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        throw "No API key. Pass -ApiKey or set `$env:NUGET_API_KEY before running the script."
    }
    $packages = Get-ChildItem -Path $artifacts -Filter $Pattern -File
    if (-not $packages) {
        Write-Warning "No package matched $Pattern under $artifacts. Skipping push."
        return
    }
    foreach ($p in $packages) {
        Write-Host "==> push $($p.Name)" -ForegroundColor Green
        $pushArgs = @('nuget', 'push', $p.FullName, '--source', $Source, '--api-key', $ApiKey)
        if ($SkipDuplicate) { $pushArgs += '--skip-duplicate' }
        & dotnet @pushArgs
        if ($LASTEXITCODE -ne 0) { throw "dotnet nuget push failed for $($p.Name) (exit $LASTEXITCODE)" }
    }
}

# Rewrite a <dependency id="$DependencyId" version="X.Y.Z" ...> entry inside
# the .nuspec of $NupkgPath so the version reads "[X.Y.Z]" (NuGet exact-pin
# bracket syntax). Idempotent -- already-pinned versions are left alone.
function Set-ExactDependency {
    param(
        [Parameter(Mandatory)] [string] $NupkgPath,
        [Parameter(Mandatory)] [string] $DependencyId
    )

    if (-not (Test-Path $NupkgPath)) { throw "Nupkg not found: $NupkgPath" }

    $zip = [IO.Compression.ZipFile]::Open($NupkgPath, 'Update')
    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
        if (-not $entry) {
            Write-Warning "No .nuspec inside $NupkgPath, skipping pin."
            return
        }
        $entryName = $entry.FullName

        $reader = New-Object IO.StreamReader($entry.Open(), [Text.Encoding]::UTF8)
        $content = $reader.ReadToEnd()
        $reader.Dispose()

        # The .nuspec uses the NuSpec XSD as its default namespace. Parse it
        # so we don't get tripped up by attribute order or extra whitespace.
        $xml = New-Object Xml.XmlDocument
        $xml.PreserveWhitespace = $true
        $xml.LoadXml($content)

        $ns = New-Object Xml.XmlNamespaceManager($xml.NameTable)
        $nsuri = $xml.DocumentElement.NamespaceURI
        if ([string]::IsNullOrEmpty($nsuri)) { $nsuri = '' }
        if ($nsuri) { $ns.AddNamespace('n', $nsuri) }

        $query = if ($nsuri) { "//n:dependency[@id='$DependencyId']" } else { "//dependency[@id='$DependencyId']" }
        $matches = $xml.SelectNodes($query, $ns)
        if (-not $matches -or $matches.Count -eq 0) {
            Write-Verbose "No <dependency id='$DependencyId'> in $($NupkgPath | Split-Path -Leaf), nothing to pin."
            return
        }

        $changed = $false
        foreach ($node in $matches) {
            $v = $node.GetAttribute('version')
            if ([string]::IsNullOrWhiteSpace($v)) { continue }
            if ($v -match '^\s*\[.*\]\s*$') { continue }   # already pinned
            $node.SetAttribute('version', "[$v]")
            $changed = $true
        }

        if (-not $changed) { return }

        # Replace the .nuspec entry in place. ZipArchiveMode.Update lets us
        # delete + recreate the entry inside the existing archive.
        $entry.Delete()
        $newEntry = $zip.CreateEntry($entryName, [IO.Compression.CompressionLevel]::Optimal)
        $writer = New-Object IO.StreamWriter($newEntry.Open(), (New-Object Text.UTF8Encoding($false)))
        $xml.Save($writer)
        $writer.Dispose()

        Write-Host "==> pinned $DependencyId in $($NupkgPath | Split-Path -Leaf)" -ForegroundColor Yellow
    }
    finally {
        $zip.Dispose()
    }
}

# Pin EgoPDF.Generator inside every freshly built downstream nupkg matching
# $Pattern. The SDK packs ProjectReferences with `>=` semantics; that's the
# default since the 1.0.0 release because most consumers want to absorb
# Generator bug fixes automatically. Pass -PinDependencies to opt in to the
# exact `[X.Y.Z]` pin when a downstream package needs the specific Generator
# build it was tested against.
function Invoke-PinGeneratorDependency {
    param([string] $Pattern)
    if (-not $PinDependencies) { return }
    $packages = Get-ChildItem -Path $artifacts -Filter $Pattern -File
    foreach ($pkg in $packages) {
        Set-ExactDependency -NupkgPath $pkg.FullName -DependencyId 'EgoPDF.Generator'
    }
}

if ($publishGenerator) {
    Invoke-Pack (Join-Path $root 'Ego.PdfCore/Ego.PdfCore.csproj')
    Invoke-Push 'EgoPDF.Generator.*.nupkg'
}

if ($publishBarcodes) {
    # Pack EgoPDF.Barcodes after Generator so its dependency on
    # EgoPDF.Generator picks up the freshly built version if you bumped it.
    Invoke-Pack (Join-Path $root 'Ego.PDF.Barcodes/Ego.PDF.Barcodes.csproj')
    Invoke-PinGeneratorDependency 'EgoPDF.Barcodes.*.nupkg'
    Invoke-Push 'EgoPDF.Barcodes.*.nupkg'
}

if ($publishMarkdown) {
    # Same story for EgoPDF.Markdown -- ProjectReference resolves to whatever
    # Generator version is current locally, so pin it before publishing.
    Invoke-Pack (Join-Path $root 'Ego.PDF.Markdown/Ego.PDF.Markdown.csproj')
    Invoke-PinGeneratorDependency 'EgoPDF.Markdown.*.nupkg'
    Invoke-Push 'EgoPDF.Markdown.*.nupkg'
}

Write-Host "Done." -ForegroundColor Green
if ($PackOnly) {
    Write-Host "Packages are in $artifacts (no push performed)."
}
