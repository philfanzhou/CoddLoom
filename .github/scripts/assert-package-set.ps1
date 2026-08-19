param(
    [Parameter(Mandatory = $true)]
    [string] $PackageDirectory
)

$ErrorActionPreference = 'Stop'

$expectedPackages = @(
    'CoddLoom',
    'CoddLoom.MariaDb',
    'CoddLoom.MySql',
    'CoddLoom.Oracle',
    'CoddLoom.PostgreSql',
    'CoddLoom.Sqlite',
    'CoddLoom.SqlServer'
)

$packageDirectoryPath = (Resolve-Path -LiteralPath $PackageDirectory).Path
$packages = @(Get-ChildItem -LiteralPath $packageDirectoryPath -File -Filter '*.nupkg')
$symbolPackages = @(Get-ChildItem -LiteralPath $packageDirectoryPath -File -Filter '*.snupkg')

if ($packages.Count -ne $expectedPackages.Count) {
    throw "Expected $($expectedPackages.Count) NuGet packages, found $($packages.Count): $($packages.Name -join ', ')"
}

if ($symbolPackages.Count -ne $expectedPackages.Count) {
    throw "Expected $($expectedPackages.Count) symbol packages, found $($symbolPackages.Count): $($symbolPackages.Name -join ', ')"
}

foreach ($packageName in $expectedPackages) {
    $escapedName = [Regex]::Escape($packageName)
    if (-not ($packages.Name -match "^$escapedName\.[0-9]")) {
        throw "Missing NuGet package for $packageName."
    }
    if (-not ($symbolPackages.Name -match "^$escapedName\.[0-9]")) {
        throw "Missing symbol package for $packageName."
    }
}

Write-Host "Verified all $($expectedPackages.Count) NuGet packages and symbol packages."
