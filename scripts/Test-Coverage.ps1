param(
    [string]$CoverageRoot = "artifacts/test-results"
)

$ErrorActionPreference = "Stop"

$thresholds = [ordered]@{
    "DataCleaner.App" = 55.0
    "DataCleaner.Application" = 70.0
    "DataCleaner.Domain" = 65.0
    "DataCleaner.Infrastructure" = 80.0
}

$coverageFiles = @(Get-ChildItem -Path $CoverageRoot -Recurse -Filter "coverage.cobertura.xml")
if ($coverageFiles.Count -eq 0) {
    throw "No Cobertura coverage files were found under '$CoverageRoot'."
}

$rates = @{}
foreach ($file in $coverageFiles) {
    [xml]$report = Get-Content -Raw -LiteralPath $file.FullName
    foreach ($package in @($report.coverage.packages.package)) {
        $name = [string]$package.name
        if (-not $thresholds.Contains($name)) {
            continue
        }

        $rate = [math]::Round(([double]$package.'line-rate') * 100, 2)
        if (-not $rates.ContainsKey($name) -or $rate -gt $rates[$name]) {
            $rates[$name] = $rate
        }
    }
}

$rows = foreach ($entry in $thresholds.GetEnumerator()) {
    $actual = if ($rates.ContainsKey($entry.Key)) { [double]$rates[$entry.Key] } else { 0.0 }
    [pscustomobject]@{
        Assembly = $entry.Key
        LineCoverage = $actual
        Minimum = [double]$entry.Value
        Result = if ($actual -ge [double]$entry.Value) { "PASS" } else { "FAIL" }
    }
}

$rows | Format-Table -AutoSize

if ($env:GITHUB_STEP_SUMMARY) {
    @(
        "## Coverage baseline"
        ""
        "| Assembly | Line coverage | Minimum | Result |"
        "| --- | ---: | ---: | --- |"
        $rows | ForEach-Object { "| $($_.Assembly) | $($_.LineCoverage)% | $($_.Minimum)% | $($_.Result) |" }
    ) | Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY
}

$failures = @($rows | Where-Object Result -eq "FAIL")
if ($failures.Count -gt 0) {
    throw "Coverage baseline failed for: $($failures.Assembly -join ', ')."
}

Write-Host "Coverage baseline passed for $($rows.Count) production assemblies."
