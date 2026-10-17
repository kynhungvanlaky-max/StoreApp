$storefrontCssFiles = @(
    "src/NexusKeys.Storefront/wwwroot/css/site.css",
    "src/NexusKeys.Storefront/wwwroot/css/epic-games.css",
    "src/NexusKeys.Storefront/wwwroot/design/atlas.css",
    "src/NexusKeys.Storefront/wwwroot/css/nexus-ui.css",
    "src/NexusKeys.Storefront/Components/Layout/MainLayout.razor.css",
    "src/NexusKeys.Storefront/Components/Layout/ReconnectModal.razor.css"
)

$combinedCss = ""
foreach ($f in $storefrontCssFiles) {
    if (Test-Path $f) {
        $combinedCss += "`n" + [System.IO.File]::ReadAllText($f)
    }
}

function Test-CssClassExists([string]$cls, [string]$css) {
    # 1. Direct raw check: e.g. class in quotes or exact
    # In CSS, class starts with a dot: .class-name
    # Characters needing escape: [ ] # / : . % ( ) ,
    
    # Try exact escaped selector
    # Note: # can be escaped as \# or unescaped #
    # Leading digit: e.g. 2xl -> (\32 xl\:|2xl\:)
    
    $p = [regex]::Escape($cls) # escapes all regex chars
    # In regex::Escape, '\:' or '\/' etc.
    # But in CSS, the file literally has '\:' (two chars: backslash and colon)
    
    # Let's construct the literal CSS string to look for:
    # Build regex for CSS selector
    $cssPatternBuilder = [System.Text.StringBuilder]::new()
    $cssPatternBuilder.Append('(?:\.|\b)') | Out-Null
    
    # If starts with digit like 2xl
    $startIdx = 0
    if ($cls -match '^(\d+)(.*)') {
        $digit = $matches[1]
        $rest = $matches[2]
        $cssPatternBuilder.Append("(?:\\3$digit |$digit)") | Out-Null
        $chars = $rest.ToCharArray()
    } else {
        $chars = $cls.ToCharArray()
    }
    
    foreach ($ch in $chars) {
        if ($ch -eq ':') {
            $cssPatternBuilder.Append('\\:') | Out-Null
        }
        elseif ($ch -eq '/') {
            $cssPatternBuilder.Append('\\/') | Out-Null
        }
        elseif ($ch -eq '[') {
            $cssPatternBuilder.Append('\\\[') | Out-Null
        }
        elseif ($ch -eq ']') {
            $cssPatternBuilder.Append('\\\]') | Out-Null
        }
        elseif ($ch -eq '#') {
            $cssPatternBuilder.Append('(?:\\#|#)') | Out-Null
        }
        elseif ($ch -eq '.') {
            $cssPatternBuilder.Append('\\\.') | Out-Null
        }
        elseif ($ch -eq '%') {
            $cssPatternBuilder.Append('\\%') | Out-Null
        }
        elseif ($ch -eq '(') {
            $cssPatternBuilder.Append('\\(?:28|\()') | Out-Null
        }
        elseif ($ch -eq ')') {
            $cssPatternBuilder.Append('\\(?:29|\))') | Out-Null
        }
        elseif ($ch -eq ',') {
            $cssPatternBuilder.Append('(?:\\2c |\\,)') | Out-Null
        }
        else {
            $cssPatternBuilder.Append([regex]::Escape($ch.ToString())) | Out-Null
        }
    }
    
    # Boundary at end: cannot be followed by alphanumeric or hyphen or underscore
    $cssPatternBuilder.Append('(?=[ \t\r\n,{:.>+~\[\(\\>]|$)') | Out-Null
    
    $regex = [regex]::new($cssPatternBuilder.ToString())
    return $regex.IsMatch($css)
}

# Test with a few known classes
$testClasses = @(
    "flex", "hidden", "items-center", "sm:grid-cols-3", "2xl:px-16",
    "bg-[#121212]", "bg-[#0078f2]", "w-1.5", "h-[2px]", "text-white/70"
)

foreach ($tc in $testClasses) {
    $exists = Test-CssClassExists $tc $combinedCss
    Write-Host "$tc -> $exists"
}
