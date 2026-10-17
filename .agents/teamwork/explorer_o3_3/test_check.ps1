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

Write-Host "Total Storefront CSS length: $($combinedCss.Length)"

# Let's check occurrences of grid-cols-3
$matches = [regex]::Matches($combinedCss, '[^\s{},;]*grid-cols-3[^\s{},;]*')
foreach ($m in $matches) {
    Write-Host "Matched: $($m.Value)"
}
