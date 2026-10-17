<#
.SYNOPSIS
    Automated CSS Class Coverage Scanner for NexusKeys Storefront & Admin.
    Checks all CSS classes used in .razor / .cshtml files against defined CSS rules.

.DESCRIPTION
    Scans Razor (.razor) and CSHTML (.cshtml) files, extracts all static and dynamic class names,
    and checks their existence in project stylesheets (supporting standard, escaped, and arbitrary Tailwind syntax).
    Returns exit code 0 if 0 missing classes, or 1 if missing classes are detected.

.PARAMETER Target
    "Storefront", "Admin", or "All" (default: "All")

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .agents/teamwork/explorer_o3_3/verify_css_classes.ps1 -Target All
#>

param(
    [ValidateSet("Storefront", "Admin", "All")]
    [string]$Target = "All",
    [switch]$Detailed = $false
)

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$overallSuccess = $true

function Extract-MarkupClasses([string]$content) {
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
            
            # Handle Razor dynamic @(...) blocks
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
                        if ($sawQuestionMark) {
                            $tokens = $literalBuilder.ToString() -split '\s+'
                            foreach ($tok in $tokens) {
                                $cleanTok = $tok.Trim(' ', "`t", "`r", "`n", '"', "'", '`')
                                if ($cleanTok -and $cleanTok -notmatch '^[0-9]+$' -and $cleanTok -ne 'true' -and $cleanTok -ne 'false' -and $cleanTok -ne 'null' -and -not $cleanTok.StartsWith('/') -and -not $cleanTok.StartsWith('${')) {
                                    $classes.Add($cleanTok) | Out-Null
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

function Run-Audit([string]$name, [string[]]$markupPaths, [string]$filter, [string[]]$cssPaths, [bool]$ignoreIcons) {
    Write-Host "`n========================================================"
    Write-Host " [AUDIT] Scanning $name CSS Coverage"
    Write-Host "========================================================"
    
    # 1. Load CSS
    $combinedCss = [System.Text.StringBuilder]::new()
    foreach ($f in $cssPaths) {
        if (Test-Path $f) {
            $content = [System.IO.File]::ReadAllText($f)
            $combinedCss.Append("`n").Append($content) | Out-Null
        }
    }
    $cssText = $combinedCss.ToString()
    Write-Host "Loaded $($cssPaths.Count) CSS files ($([Math]::Round($cssText.Length / 1024)) KB)."
    
    # 2. Extract Markup Classes
    $markupFiles = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    foreach ($mp in $markupPaths) {
        if (Test-Path $mp) {
            Get-ChildItem -Path $mp -Filter $filter -Recurse | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
                $markupFiles.Add($_)
            }
        }
    }
    
    $allClasses = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $classToFiles = @{}
    
    foreach ($mf in $markupFiles) {
        $content = [System.IO.File]::ReadAllText($mf.FullName)
        $fileClasses = Extract-MarkupClasses $content
        $relPath = $mf.FullName.Replace((Get-Location).Path, "").TrimStart('\', '/')
        
        foreach ($cl in $fileClasses) {
            $clean = $cl.Trim(' ', '"', "'", '`')
            if ($clean.StartsWith('@') -or 
                $clean.StartsWith('/') -or 
                $clean.StartsWith('${') -or 
                $clean -eq '===' -or 
                $clean -eq 'true' -or 
                $clean -eq 'false' -or 
                $clean -eq 'null' -or 
                $clean -match '^[0-9]+$') {
                continue
            }
            if ($ignoreIcons -and $clean -match '^(fa|fa-|fa-solid|fa-regular|fa-brands)') {
                continue # Loaded via CDN
            }
            # Ignore C# Controller names inside route checks
            if ($clean -match '^(AiAssistantConfigs|AuditLogs|Comments|Coupons|Home|Keys|News|Orders|PaymentConfigs|Products|Promotions|Support|Transactions|Users)$') {
                continue
            }
            
            $allClasses.Add($clean) | Out-Null
            if (-not $classToFiles.ContainsKey($clean)) {
                $classToFiles[$clean] = [System.Collections.Generic.List[string]]::new()
            }
            $classToFiles[$clean].Add($relPath)
        }
    }
    
    Write-Host "Found $($allClasses.Count) distinct class candidates across $($markupFiles.Count) files."
    
    # 3. Check each against CSS
    $present = [System.Collections.Generic.List[string]]::new()
    $missing = [System.Collections.Generic.List[string]]::new()
    
    foreach ($cl in ($allClasses | Sort-Object)) {
        if ($cssText.Contains($cl)) {
            $present.Add($cl)
            continue
        }
        $rx = Build-CssRegex $cl
        if ($rx.IsMatch($cssText)) {
            $present.Add($cl)
        } else {
            $missing.Add($cl)
        }
    }
    
    Write-Host "--- Result for $name ---"
    Write-Host "Total unique classes: $($allClasses.Count)"
    Write-Host "Classes defined:      $($present.Count)"
    Write-Host "Classes MISSING:      $($missing.Count)"
    
    if ($missing.Count -eq 0) {
        Write-Host "STATUS: PASSED (Zero missing classes!)" -ForegroundColor Green
    } else {
        Write-Host "STATUS: FAILED ($($missing.Count) missing classes require definition)" -ForegroundColor Red
        $script:overallSuccess = $false
        
        $outFile = "missing_${name.ToLower()}_classes.txt"
        $missing | Out-File -FilePath $outFile -Encoding utf8
        Write-Host "Saved missing class list to: $outFile"
        
        Write-Host "Sample missing classes (top 15):"
        $missing | Select-Object -First 15 | ForEach-Object {
            Write-Host "  - $_ (in $($classToFiles[$_][0]))"
        }
    }
    
    return $missing.Count
}

# Run Storefront audit
if ($Target -eq "Storefront" -or $Target -eq "All") {
    $storefrontMarkup = @("src/NexusKeys.Storefront/Components")
    $storefrontCss = @(
        "src/NexusKeys.Storefront/wwwroot/css/site.css",
        "src/NexusKeys.Storefront/wwwroot/css/epic-games.css",
        "src/NexusKeys.Storefront/wwwroot/design/atlas.css",
        "src/NexusKeys.Storefront/wwwroot/css/nexus-ui.css",
        "src/NexusKeys.Storefront/Components/Layout/MainLayout.razor.css",
        "src/NexusKeys.Storefront/Components/Layout/ReconnectModal.razor.css"
    )
    Run-Audit -name "Storefront" -markupPaths $storefrontMarkup -filter "*.razor" -cssPaths $storefrontCss -ignoreIcons $false | Out-Null
}

# Run Admin audit
if ($Target -eq "Admin" -or $Target -eq "All") {
    $adminMarkup = @("src/NexusKeys.Admin/Views")
    $adminCss = @(
        "src/NexusKeys.Admin/wwwroot/css/site.css",
        "src/NexusKeys.Admin/wwwroot/css/nexus-admin.css",
        "src/NexusKeys.Admin/Views/Shared/_Layout.cshtml.css"
    )
    Run-Audit -name "Admin" -markupPaths $adminMarkup -filter "*.cshtml" -cssPaths $adminCss -ignoreIcons $true | Out-Null
}

$stopwatch.Stop()
Write-Host "`n========================================================"
Write-Host "Verification completed in $($stopwatch.ElapsedMilliseconds) ms."
if ($overallSuccess) {
    Write-Host "ALL CSS COVERAGE CHECKS PASSED: 0 MISSING CLASSES" -ForegroundColor Green
    exit 0
} else {
    Write-Host "CSS COVERAGE CHECK FAILED: MISSING CLASSES DETECTED" -ForegroundColor Red
    exit 1
}
