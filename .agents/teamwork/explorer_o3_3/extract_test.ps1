$razorFiles = Get-ChildItem -Path "src/NexusKeys.Storefront" -Filter "*.razor" -Recurse | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

$allUsedClasses = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

# Balanced parenthesis regex for @(...)
$balancedAtExpr = [regex]::new('@\((?:[^()]|\((?<Depth>)|\)(?<-Depth>))*(?(Depth)(?!))\)')

foreach ($file in $razorFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    
    # Match class="..." or Class="..." (single or double quotes)
    $classAttrRegex = [regex]::new('(?i)\bclass\s*=\s*(?<quote>["''])(?<val>.*?)\k<quote>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $matches = $classAttrRegex.Matches($content)
    
    foreach ($m in $matches) {
        $val = $m.Groups['val'].Value
        
        # Step A: Find and extract from all @(...) blocks
        $atMatches = $balancedAtExpr.Matches($val)
        foreach ($atM in $atMatches) {
            $expr = $atM.Value
            # Find string literals inside this @(...) block
            $strLiterals = [regex]::Matches($expr, '\"([^\"]*)\"')
            foreach ($strLit in $strLiterals) {
                $tokens = $strLit.Groups[1].Value -split '\s+'
                foreach ($t in $tokens) {
                    $t = $t.Trim()
                    if ($t) {
                        $allUsedClasses.Add($t) | Out-Null
                    }
                }
            }
        }
        
        # Step B: Replace all @(...) blocks with a space
        $cleanVal = $balancedAtExpr.Replace($val, ' ')
        # Also replace standalone @variable if any, e.g. @itemClass
        $cleanVal = [regex]::Replace($cleanVal, '@[a-zA-Z0-9_.]+', ' ')
        
        # Step C: Split remaining static tokens by whitespace
        $tokens = $cleanVal -split '\s+'
        foreach ($t in $tokens) {
            $t = $t.Trim()
            if ($t -and $t -notmatch '^[0-9]+$' -and $t -ne 'true' -and $t -ne 'false' -and $t -ne 'null') {
                $allUsedClasses.Add($t) | Out-Null
            }
        }
    }
}

Write-Host "Total unique classes extracted from Storefront: $($allUsedClasses.Count)"

# Check if any weird artifacts remain
$weird = $allUsedClasses | Where-Object { $_ -match '[\(\);?]|^\.|\.$|^"|^''' }
Write-Host "Weird items count: $($weird.Count)"
if ($weird.Count -gt 0) {
    Write-Host "Weird items: $($weird -join ', ')"
}

$sample = $allUsedClasses | Sort-Object | Select-Object -First 30
Write-Host "First 30 classes: $($sample -join ', ')"
