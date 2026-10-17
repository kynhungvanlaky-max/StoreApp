# Scan Storefront CSS Usage vs Definitions
param(
    [string]$StorefrontDir = "src/NexusKeys.Storefront"
)

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# 1. Gather all CSS files
$cssFiles = @(
    "$StorefrontDir/wwwroot/css/site.css",
    "$StorefrontDir/wwwroot/css/epic-games.css",
    "$StorefrontDir/wwwroot/design/atlas.css",
    "$StorefrontDir/wwwroot/css/nexus-ui.css",
    "$StorefrontDir/Components/Layout/MainLayout.razor.css",
    "$StorefrontDir/Components/Layout/ReconnectModal.razor.css"
)

$combinedCss = [System.Text.StringBuilder]::new()
foreach ($f in $cssFiles) {
    if (Test-Path $f) {
        $content = [System.IO.File]::ReadAllText($f)
        $combinedCss.Append("`n").Append($content) | Out-Null
    }
}
$cssText = $combinedCss.ToString()
Write-Host "Loaded $($cssFiles.Count) CSS files ($([Math]::Round($cssText.Length / 1024)) KB)."

# 2. Extract classes from Razor files
function Extract-RazorClasses([string]$content) {
    $classes = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $len = $content.Length
    $i = 0
    
    while ($i -lt $len) {
        $match = [regex]::Match($content.Substring($i), '(?i)(?:^|[\s<])class\s*=\s*')
        if (-not $match.Success) { break }
        
        $attrStart = $i + $match.Index + $match.Length
        if ($attrStart -ge $len) { break }
        
        $quoteChar = $content[$attrStart]
        if ($quoteChar -ne '"' -and $quoteChar -ne "'") {
            $i = $attrStart + 1
            continue
        }
        
        $curr = $attrStart + 1
        $currentStaticToken = [System.Text.StringBuilder]::new()
        
        while ($curr -lt $len) {
            $ch = $content[$curr]
            
            if ($ch -eq $quoteChar) {
                $t = $currentStaticToken.ToString().Trim()
                if ($t) { $classes.Add($t) | Out-Null }
                $curr++
                break
            }
            
            if ($ch -eq '@' -and ($curr + 1) -lt $len -and $content[$curr + 1] -eq '(') {
                $t = $currentStaticToken.ToString().Trim()
                if ($t) { $classes.Add($t) | Out-Null }
                $currentStaticToken.Clear() | Out-Null
                
                $curr += 2
                $parenDepth = 1
                $sawQuestionMark = $false
                
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
                    elseif ($c -eq '?') {
                        $sawQuestionMark = $true
                        $curr++
                    }
                    elseif ($c -eq '"' -or $c -eq "'") {
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
                        
                        # Only take classes from string literals after '?' in a ternary expression
                        # (ignoring arguments in condition like IsCurrentPath("/assistant"))
                        if ($sawQuestionMark) {
                            $tokens = $literalBuilder.ToString() -split '\s+'
                            foreach ($tok in $tokens) {
                                $tok = $tok.Trim()
                                if ($tok -and $tok -notmatch '^[0-9]+$' -and $tok -ne 'true' -and $tok -ne 'false' -and $tok -ne 'null' -and -not $tok.StartsWith('/')) {
                                    $classes.Add($tok) | Out-Null
                                }
                            }
                        }
                    }
                    else {
                        $curr++
                    }
                }
                continue
            }
            
            if ([char]::IsWhiteSpace($ch)) {
                $t = $currentStaticToken.ToString().Trim()
                if ($t) { $classes.Add($t) | Out-Null }
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

$razorFiles = Get-ChildItem -Path $StorefrontDir -Filter "*.razor" -Recurse | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
$allClasses = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$classToFiles = @{}

foreach ($rf in $razorFiles) {
    $content = [System.IO.File]::ReadAllText($rf.FullName)
    $fileClasses = Extract-RazorClasses $content
    $relPath = $rf.FullName.Replace((Get-Location).Path, "").TrimStart('\', '/')
    foreach ($cl in $fileClasses) {
        # Filter out obvious C# artifacts
        if ($cl.StartsWith('@') -or $cl.StartsWith('/') -or $cl -eq 'true' -or $cl -eq 'false' -or $cl -eq 'null' -or $cl -match '^[0-9]+$') {
            continue
        }
        $allClasses.Add($cl) | Out-Null
        if (-not $classToFiles.ContainsKey($cl)) {
            $classToFiles[$cl] = [System.Collections.Generic.List[string]]::new()
        }
        $classToFiles[$cl].Add($relPath)
    }
}

Write-Host "Found $($allClasses.Count) distinct class candidates across $($razorFiles.Count) .razor files."

# 3. Check each class against CSS
function Build-CssRegex([string]$cls) {
    $cssPatternBuilder = [System.Text.StringBuilder]::new()
    $cssPatternBuilder.Append('(?:\.|\b)') | Out-Null
    
    if ($cls -match '^(\d+)(.*)') {
        $digit = $matches[1]
        $rest = $matches[2]
        $cssPatternBuilder.Append("(?:\\3$digit |$digit)") | Out-Null
        $chars = $rest.ToCharArray()
    } else {
        $chars = $cls.ToCharArray()
    }
    
    foreach ($ch in $chars) {
        if ($ch -eq ':') { $cssPatternBuilder.Append('\\:') | Out-Null }
        elseif ($ch -eq '/') { $cssPatternBuilder.Append('\\/') | Out-Null }
        elseif ($ch -eq '[') { $cssPatternBuilder.Append('\\\[') | Out-Null }
        elseif ($ch -eq ']') { $cssPatternBuilder.Append('\\\]') | Out-Null }
        elseif ($ch -eq '#') { $cssPatternBuilder.Append('(?:\\#|#)') | Out-Null }
        elseif ($ch -eq '.') { $cssPatternBuilder.Append('\\\.') | Out-Null }
        elseif ($ch -eq '%') { $cssPatternBuilder.Append('\\%') | Out-Null }
        elseif ($ch -eq '(') { $cssPatternBuilder.Append('\\(?:28|\()') | Out-Null }
        elseif ($ch -eq ')') { $cssPatternBuilder.Append('\\(?:29|\))') | Out-Null }
        elseif ($ch -eq ',') { $cssPatternBuilder.Append('(?:\\2c |\\,)') | Out-Null }
        else { $cssPatternBuilder.Append([regex]::Escape($ch.ToString())) | Out-Null }
    }
    
    $cssPatternBuilder.Append('(?=[ \t\r\n,{:.>+~\[\(\\>]|$)') | Out-Null
    return [regex]::new($cssPatternBuilder.ToString())
}

$presentClasses = [System.Collections.Generic.List[string]]::new()
$missingClasses = [System.Collections.Generic.List[string]]::new()

foreach ($cl in ($allClasses | Sort-Object)) {
    # Check for direct attribute selector match e.g. [class*="bg-[#121212]"]
    if ($cssText.Contains($cl)) {
        # Raw text appears in CSS (could be selector or attribute match)
        $presentClasses.Add($cl)
        continue
    }
    
    $rx = Build-CssRegex $cl
    if ($rx.IsMatch($cssText)) {
        $presentClasses.Add($cl)
    } else {
        $missingClasses.Add($cl)
    }
}

$stopwatch.Stop()

Write-Host "================ SCAN SUMMARY ================"
Write-Host "Total unique classes analyzed: $($allClasses.Count)"
Write-Host "Present classes in CSS:        $($presentClasses.Count)"
Write-Host "MISSING classes in CSS:        $($missingClasses.Count)"
Write-Host "Scan completed in:             $($stopwatch.ElapsedMilliseconds) ms"
Write-Host "=============================================="

# Output missing classes
$missingReportFile = "missing_storefront_classes.txt"
$missingClasses | Out-File -FilePath $missingReportFile -Encoding utf8
Write-Host "Missing classes written to: $missingReportFile"

# Show sample of missing classes
Write-Host "`nSample of missing classes (first 30):"
$missingClasses | Select-Object -First 30 | ForEach-Object {
    Write-Host "  - $_ (used in: $($classToFiles[$_] -join ', '))"
}
