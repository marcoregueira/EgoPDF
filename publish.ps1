<#
.SYNOPSIS
    Pack EgoPDF.Generator + EgoPDF.Zpl and push them to nuget.org.

.DESCRIPTION
    Runs `dotnet pack -c Release` on both NuGet projects, drops the
    .nupkg / .snupkg into ./artifacts and (unless -PackOnly is passed)
    pushes them to https://api.nuget.org/v3/index.json.

    After the EgoPDF.Zpl .nupkg is built the script rewrites its
    .nuspec so the EgoPDF.Generator dependency becomes an EXACT
    version pin (`version="[X.Y.Z]"`) instead of the SDK's optimistic
    default (`version="X.Y.Z"`, which NuGet treats as `>= X.Y.Z`).
    This keeps EgoPDF.Zpl pinned to the specific Generator build it
    was tested against — important while the project is still in
    motion. Disable with -NoPinDependencies.

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
    Only pack/push the EgoPDF.Generator package. Combine with -Zpl to
    publish a single one (default: publish both).

.PARAMETER Zpl
    Only pack/push the EgoPDF.Zpl package.

.PARAMETER NoPinDependencies
    Skip the post-pack rewrite of the Zpl .nuspec. The dependency on
    EgoPDF.Generator will be packed with the SDK's default `>=`
    semantics.

.EXAMPLE
    # Set the key once per shell, then publish both packages.
    $env:NUGET_API_KEY = 'oy2...'
    ./publish.ps1

.EXAMPLE
    # Only build the .nupkg files (no push).
    ./publish.ps1 -PackOnly

.EXAMPLE
    # Publish only the preview ZPL package.
    ./publish.ps1 -Zpl
#>
[CmdletBinding()]
param(
    [string] $ApiKey = $env:NUGET_API_KEY,
    [string] $Source = 'https://api.nuget.org/v3/index.json',
    [switch] $PackOnly,
    [switch] $SkipDuplicate,
    [switch] $Generator,
    [switch] $Zpl,
    [switch] $NoPinDependencies
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'

# If neither -Generator nor -Zpl was supplied, publish both.
$publishBoth = -not ($Generator -or $Zpl)
$publishGenerator = $publishBoth -or $Generator
$publishZpl       = $publishBoth -or $Zpl

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
# bracket syntax). Idempotent — already-pinned versions are left alone.
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

if ($publishGenerator) {
    Invoke-Pack (Join-Path $root 'Ego.PdfCore/Ego.PdfCore.csproj')
    Invoke-Push 'EgoPDF.Generator.*.nupkg'
}

if ($publishZpl) {
    # Pack EgoPDF.Zpl second so its dependency on EgoPDF.Generator
    # picks up the freshly built Generator if you bumped its version.
    Invoke-Pack (Join-Path $root 'Ego.Pdf.Zpl/Ego.Pdf.Zpl.csproj')

    if (-not $NoPinDependencies) {
        # The SDK packs ProjectReferences with `>=` semantics. Rewrite the
        # Zpl nuspec so the Generator dep is exact-pinned — Zpl is moving
        # fast and we don't want a future Generator change leaking in.
        $zplPackages = Get-ChildItem -Path $artifacts -Filter 'EgoPDF.Zpl.*.nupkg' -File
        foreach ($pkg in $zplPackages) {
            Set-ExactDependency -NupkgPath $pkg.FullName -DependencyId 'EgoPDF.Generator'
        }
    }

    Invoke-Push 'EgoPDF.Zpl.*.nupkg'
}

Write-Host "Done." -ForegroundColor Green
if ($PackOnly) {
    Write-Host "Packages are in $artifacts (no push performed)."
}
