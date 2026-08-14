[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$failures = [System.Collections.Generic.List[string]]::new()

$markdownFiles = Get-ChildItem -Path $repositoryRoot -Recurse -File -Filter '*.md' |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

foreach ($file in $markdownFiles) {
    $content = Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName

    foreach ($match in [regex]::Matches($content, '\[[^\]]+\]\((?<target>[^)]+)\)')) {
        $target = $match.Groups['target'].Value

        if ($target -match '^(https?://|mailto:|#)' -or $target -eq '<repository-url>') {
            continue
        }

        $pathPart = $target.Split('#')[0]
        if (-not $pathPart) {
            continue
        }

        $resolvedPath = Join-Path $file.DirectoryName $pathPart
        if (-not (Test-Path -LiteralPath $resolvedPath)) {
            $failures.Add("Broken link in $($file.FullName): $target")
        }
    }

    if ($content -match 'â†|â‚|â”|�') {
        $failures.Add("Possible encoding corruption in $($file.FullName)")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Documentation validation passed for $($markdownFiles.Count) Markdown files."
