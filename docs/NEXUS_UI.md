# Nexus UI

Nexus UI is the buildless visual layer for the NexusKeys storefront and management portal. It does not require Node.js, npm, Tailwind, DaisyUI, PostCSS, or a CSS build command.

## Files

- `src/NexusKeys.Storefront/wwwroot/css/site.css` is the frozen compatibility stylesheet for existing Razor utility classes.
- `src/NexusKeys.Storefront/wwwroot/css/nexus-ui.css` owns storefront tokens, typography, components, responsive behavior, and motion.
- `src/NexusKeys.Admin/wwwroot/css/site.css` is the frozen compatibility stylesheet for existing MVC utility classes.
- `src/NexusKeys.Admin/wwwroot/css/nexus-admin.css` owns management portal typography and component styling.
- `src/NexusKeys.Storefront/wwwroot/js/ui-helpers.js` provides dependency-free reveal and media motion using `IntersectionObserver`.

The new stylesheets are loaded after the compatibility stylesheets and therefore define the final visual behavior.

## Typography

The main typeface is Be Vietnam Pro. It contains Vietnamese glyphs and is loaded from Google Fonts. The fallback stack is `Segoe UI Variable Text`, `Segoe UI`, Arial, and sans-serif.

## Tokens

Storefront tokens use the `--nui-` prefix. Admin tokens use the `--admin-` prefix. New components must use tokens instead of hardcoded page colors.

Core storefront tokens include:

- `--nui-page`
- `--nui-surface`
- `--nui-surface-raised`
- `--nui-text`
- `--nui-text-muted`
- `--nui-primary`
- `--nui-border`
- `--nui-radius-*`
- `--nui-shadow-*`

## Component rules

- Use `.card`, `.btn`, `.input`, `.select`, and `.textarea` for shared primitives.
- Use `.nui-bento` for a dense twelve-column bento grid. Children must define spans that fill each row.
- Use `.nui-media` around interactive images.
- Use `.nui-reveal` only for content that benefits from entrance hierarchy.
- Keep clickable targets at least 44 pixels tall.
- Maintain visible keyboard focus and preserve the reduced-motion fallback.

## Development

Edit the CSS files directly. No compilation step is required. Run the .NET projects normally:

```text
dotnet run --project src/NexusKeys.Storefront/NexusKeys.Storefront.csproj
dotnet run --project src/NexusKeys.Admin/NexusKeys.Admin.csproj
```

Validate changes with:

```text
dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj -c Release --no-restore
dotnet build src/NexusKeys.Admin/NexusKeys.Admin.csproj -c Release --no-restore
```
