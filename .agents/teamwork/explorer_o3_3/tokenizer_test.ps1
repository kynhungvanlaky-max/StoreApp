function Extract-ClassesFromRazorContent([string]$content) {
    $classes = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    
    $len = $content.Length
    $i = 0
    
    while ($i -lt $len) {
        # Look for class= or Class=
        # Must be preceded by whitespace or <
        $match = [regex]::Match($content.Substring($i), '(?i)(?:^|[\s<])class\s*=\s*')
        if (-not $match.Success) {
            break
        }
        
        $attrStart = $i + $match.Index + $match.Length
        if ($attrStart -ge $len) { break }
        
        $quoteChar = $content[$attrStart]
        if ($quoteChar -ne '"' -and $quoteChar -ne "'") {
            # Unquoted attribute or something else, advance
            $i = $attrStart + 1
            continue
        }
        
        $curr = $attrStart + 1
        $currentStaticToken = [System.Text.StringBuilder]::new()
        
        while ($curr -lt $len) {
            $ch = $content[$curr]
            
            # Check for closing quote of attribute (when not inside @( ... ))
            if ($ch -eq $quoteChar) {
                # End of class attribute
                $t = $currentStaticToken.ToString().Trim()
                if ($t) { $classes.Add($t) | Out-Null }
                $curr++
                break
            }
            
            # Check for Razor expression @( ... )
            if ($ch -eq '@' -and ($curr + 1) -lt $len -and $content[$curr + 1] -eq '(') {
                # Flush current static token
                $t = $currentStaticToken.ToString().Trim()
                if ($t) { $classes.Add($t) | Out-Null }
                $currentStaticToken.Clear() | Out-Null
                
                # Advance past @(
                $curr += 2
                $parenDepth = 1
                
                while ($curr -lt $len -and $parenDepth -gt 0) {
                    $c = $content[$curr]
                    
                    if ($c -eq '(') {
                        $parenDepth++
                        $curr++
                    }
                    elseif ($c -eq ')') {
                        $parenDepth--
                        $curr++
                    }
                    elseif ($c -eq '"' -or $c -eq "'") {
                        # String literal inside Razor expression!
                        $innerQuote = $c
                        $curr++
                        $literalBuilder = [System.Text.StringBuilder]::new()
                        while ($curr -lt $len) {
                            $innerC = $content[$curr]
                            if ($innerC -eq '\' -and ($curr + 1) -lt $len) {
                                $curr += 2
                                continue
                            }
                            if ($innerC -eq $innerQuote) {
                                $curr++
                                break
                            }
                            $literalBuilder.Append($innerC) | Out-Null
                            $curr++
                        }
                        # Split tokens in this string literal
                        $tokens = $literalBuilder.ToString() -split '\s+'
                        foreach ($tok in $tokens) {
                            $tok = $tok.Trim()
                            if ($tok -and $tok -notmatch '^[0-9]+$' -and $tok -ne 'true' -and $tok -ne 'false' -and $tok -ne 'null') {
                                $classes.Add($tok) | Out-Null
                            }
                        }
                    }
                    else {
                        $curr++
                    }
                }
                continue
            }
            
            # Check whitespace outside Razor expression
            if ([char]::IsWhiteSpace($ch)) {
                $t = $currentStaticToken.ToString().Trim()
                if ($t) {
                    $classes.Add($t) | Out-Null
                }
                $currentStaticToken.Clear() | Out-Null
                $curr++
                continue
            }
            
            $currentStaticToken.Append($ch) | Out-Null
            $curr++
        }
        
        $i = $curr
    }
    
    return $classes
}

# Test on all Storefront razor files
$razorFiles = Get-ChildItem -Path "src/NexusKeys.Storefront" -Filter "*.razor" -Recurse | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

$allStorefrontClasses = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

foreach ($f in $razorFiles) {
    $c = [System.IO.File]::ReadAllText($f.FullName)
    $fileClasses = Extract-ClassesFromRazorContent $c
    foreach ($cl in $fileClasses) {
        $allStorefrontClasses.Add($cl) | Out-Null
    }
}

Write-Host "Total unique classes extracted cleanly: $($allStorefrontClasses.Count)"

$invalidTokens = $allStorefrontClasses | Where-Object { $_ -match '^[0-9]+$' -or $_ -match '[\(\);?]|^\.|\.$' -and $_ -notmatch '\[calc\(|\[rgba\(' }
Write-Host "Invalid tokens count: $($invalidTokens.Count)"
if ($invalidTokens.Count -gt 0) {
    Write-Host "Invalid: $($invalidTokens -join ', ')"
}

$sample = $allStorefrontClasses | Sort-Object | Select-Object -First 30
Write-Host "Sample classes: $($sample -join ', ')"
