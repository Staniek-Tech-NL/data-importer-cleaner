param(
    [ValidateRange(1, 1000000)]
    [int]$Rows = 10000,
    [string]$OutputPath = "artifacts/synthetic-customers-$Rows.csv"
)

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [System.IO.Path]::GetDirectoryName($resolvedOutput)
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

$utf8 = [System.Text.UTF8Encoding]::new($true)
$writer = [System.IO.StreamWriter]::new($resolvedOutput, $false, $utf8)
try {
    $writer.WriteLine('Customer ID,Full Name,Email,Country,Signup Date,Revenue,Notes')
    $countries = @('PL', 'NL', 'DE', 'BE', 'IT')
    for ($index = 1; $index -le $Rows; $index++) {
        $customerId = 'C-{0:D7}' -f $index
        $duplicateSeed = $index % 49000
        $email = ' customer{0}@EXAMPLE.COM ' -f $duplicateSeed
        $country = $countries[$index % $countries.Count]
        $date = (Get-Date '2025-01-01').AddDays($index % 700).ToString('yyyy-MM-dd', [System.Globalization.CultureInfo]::InvariantCulture)
        $revenue = (($index % 100000) / 100.0).ToString('0.00', [System.Globalization.CultureInfo]::InvariantCulture)
        $writer.WriteLine("$customerId,  Synthetic User $index  ,$email,$country,$date,$revenue,generated")
    }
}
finally {
    $writer.Dispose()
}

Write-Output $resolvedOutput
