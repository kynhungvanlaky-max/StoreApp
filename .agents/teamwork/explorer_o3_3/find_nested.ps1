$razorFiles = Get-ChildItem -Path "src/NexusKeys.Storefront" -Filter "*.razor" -Recurse | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

# Let's inspect lines containing class=
foreach ($file in $razorFiles) {
    $lines = [System.IO.File]::ReadAllLines($file.FullName)
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        if ($line -match 'class=".*@\(.*".*"\)') {
            Write-Host "$($file.Name):$($i+1) -> $line"
        }
    }
}
