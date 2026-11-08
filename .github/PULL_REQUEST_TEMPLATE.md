Closes #

## Control

`<WinRT type>` → `<Avalonia type, themed>` or `Cornflower.<Name>` (rung: theme, attached behaviour, part class, subclass, new control)

## Screenshot

The emulator's capture beside ours, through URLs pinned to the commit. Name the emulator image and say if ours is headless or a real window.

## Fidelity

- Checklist coverage: _n_ / _m_ rows ticked; omissions and why:
- Deliberate differences from Windows Phone, each with its measurement:
- Cornflower extensions:

## Review

- `/code-review` findings: _n_ raised, _n_ fixed, _n_ waived (reasons below)
-

## Checks

- [ ] `dotnet build Cornflower.sln`: 0 warnings, 0 errors
- [ ] `dotnet test Cornflower.sln`: all passing; one test per planned behaviour
- [ ] No declared property without a working implementation and a test
- [ ] No hex colours in a theme outside `Tokens/`; no visual opinion in `Cornflower.Controls`
- [ ] No Microsoft font, template or other SDK file added
- [ ] Only this control's files touched (theme files are auto-discovered)
