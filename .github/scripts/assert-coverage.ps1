param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageDirectory,

    [double]$MinimumLineRate = 0.85,

    [double]$MinimumBranchRate = 0.88
)

$coverageFile = Get-ChildItem -LiteralPath $CoverageDirectory -Recurse -Filter coverage.cobertura.xml |
    Select-Object -First 1

if ($null -eq $coverageFile) {
    throw "No coverage.cobertura.xml file was found under '$CoverageDirectory'."
}

$coverage = [xml](Get-Content -LiteralPath $coverageFile.FullName)
$lineRate = [double]$coverage.coverage.'line-rate'
$branchRate = [double]$coverage.coverage.'branch-rate'

Write-Host ("Coverage: lines {0:P2}, branches {1:P2}" -f $lineRate, $branchRate)

if ($lineRate -lt $MinimumLineRate) {
    throw ("Line coverage {0:P2} is below the required {1:P2}." -f $lineRate, $MinimumLineRate)
}

if ($branchRate -lt $MinimumBranchRate) {
    throw ("Branch coverage {0:P2} is below the required {1:P2}." -f $branchRate, $MinimumBranchRate)
}
