# Scan Admin CSS Usage vs Definitions (distinguishing FontAwesome vs Utility classes)
param(
    [string]$AdminDir = "src/NexusKeys.Admin"
)

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# 1. Gather all Admin CSS files
$cssFiles = @(
    "$AdminDir/wwwroot/css/site.css",
    "$AdminDir/wwwroot/css/nexus-admin.css",
    "$AdminDir/Views/Shared/_Layout.cshtml.css"
)

$combinedCss = [System.Text.StringBuilder]::new()
foreach ($f in $cssFiles) {
    if (Test-Path $f) {
        $content = [System.IO.File]::ReadAllText($f)
        $combinedCss.Append("`n").Append($content) | Out-Null
    }
}
$cssText = $combinedCss.ToString()
Write-Host "Loaded $($cssFiles.Count) Admin CSS files ($([Math]::Round($cssText.Length / 1024)) KB)."

# 2. Extract classes from CSHTML files
function Extract-CshtmlClasses([string]$content) {
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

$cshtmlFiles = Get-ChildItem -Path $AdminDir -Filter "*.cshtml" -Recurse | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
$allClasses = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$classToFiles = @{}

foreach ($cf in $cshtmlFiles) {
    $content = [System.IO.File]::ReadAllText($cf.FullName)
    $fileClasses = Extract-CshtmlClasses $content
    $relPath = $cf.FullName.Replace((Get-Location).Path, "").TrimStart('\', '/')
    foreach ($cl in $fileClasses) {
        $cleanCl = $cl.Trim('"', "'", '`')
        if ($cleanCl.StartsWith('@') -or $cleanCl.StartsWith('/') -or $cleanCl.StartsWith('${') -or $cleanCl -eq '===' -or $cleanCl -eq 'true' -or $cleanCl -eq 'false' -or $cleanCl -eq 'null' -or $cleanCl -match '^[0-9]+$') {
            continue
        }
        $allClasses.Add($cleanCl) | Out-Null
        if (-not $classToFiles.ContainsKey($cleanCl)) {
            $classToFiles[$cleanCl] = [System.Collections.Generic.List[string]]::new()
        }
        $classToFiles[$cleanCl].Add($relPath)
    }
}

# 3. Check each class against Admin CSS
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
    if ($cssText.Contains($cl)) {
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

# Split missing into FontAwesome and CSS Utility
$missingFa = $missingClasses | Where-Object { $_ -match '^(fa|fa-|fa-solid|fa-regular|fa-brands)' }
$missingUtility = $missingClasses | Where-Object { $_ -notmatch '^(fa|fa-|fa-solid|fa-regular|fa-brands)' }

Write-Host "================ ADMIN SCAN SUMMARY ================"
Write-Host "Total unique classes analyzed: $($allClasses.Count)"
Write-Host "Present classes in Admin CSS:  $($presentClasses.Count)"
Write-Host "Total MISSING in local CSS:    $($missingClasses.Count)"
Write-Host "  - Missing FontAwesome icons: $($missingFa.Count) (loaded externally via CDN)"
Write-Host "  - Missing CSS Utility:       $($missingUtility.Count) (Tailwind utilities needing definition)"
Write-Host "Scan completed in:             $($stopwatch.ElapsedMilliseconds) ms"
Write-Host "===================================================="

$missingUtility | Out-File -FilePath "missing_admin_utility_classes.txt" -Encoding utf8
Write-Host "Missing utility classes written to: missing_admin_utility_classes.txt"

Write-Host "`nSample of missing utility classes:"
$missingUtility | Select-Object -First 30 | ForEach-Object {
    Write-Host "  - $_ (used in: $($classToFiles[$_] -join ', '))"
}
