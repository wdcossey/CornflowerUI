# Cornflower

A faithful reproduction of the Windows Phone design languages for
[AvaloniaUI](https://avaloniaui.net/), mobile first. The first era is Windows Phone 8.1.

> **Status: bootstrap.** No controls yet. The solution builds, the tests run, and a theme's
> files are discovered at build time. Controls are added one at a time.

## What "faithful" means here

- **Measured, not remembered.** Geometry, colour, states and motion come from the Windows Phone
  8.1 SDK's own templates and API metadata, and from captures of the emulator.
- **Avalonia's controls, themed.** Where Avalonia has a control of the same shape, Cornflower
  themes it. Where it has none (Pivot, Hub, ProgressRing, ContentDialog and others), Cornflower
  adds one, with the WinRT name.
- **Fonts and icons are substituted.** No Microsoft font or file ships.

## Packages

| Package | Contents |
|---|---|
| `Cornflower` | Everything: the one most apps want |
| `Cornflower.Controls` | Cornflower's own controls and behaviours, no visual opinion |
| `Cornflower.Themes.Wp81` | The Windows Phone 8.1 theme |

Targets .NET 8, Avalonia 12.

## Building

```sh
dotnet build Cornflower.sln
dotnet test  Cornflower.sln
```

## Attribution

Cornflower reproduces the visual design of Microsoft's Windows Phone. It is an independent
implementation for the AvaloniaUI ecosystem and is not affiliated with or endorsed by Microsoft
or the AvaloniaUI team. Windows Phone is a trademark of Microsoft.

## Licence

Apache License 2.0; see [LICENSE](LICENSE).
