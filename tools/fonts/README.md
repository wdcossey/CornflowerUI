# fonts: font metric comparisons

Two Node scripts that read font files as data and print Markdown tables, in thousandths of an em.

| Script | Compares |
|---|---|
| `measure-fonts.js` | Open fonts (Open Sans, Noto Sans, Selawik) with Segoe UI, weight by weight |
| `measure-segoewp.js` | Segoe WP, the Windows Phone 8.1 text font, with Selawik and Segoe UI |

Segoe WP and Segoe UI are Microsoft's fonts. They are read where they are installed or kept, outside
this repository, and never committed (rule 6 in [CLAUDE.md](../../CLAUDE.md)).

## Running them

```sh
# in this folder
npm init -y && npm install --ignore-scripts opentype.js
# in an empty folder outside the repo, for the downloads
npm init -y && npm install --ignore-scripts @fontsource/open-sans @fontsource/noto-sans
curl -sL -o selawik.zip https://github.com/microsoft/Selawik/releases/download/1.01/Selawik_Release.zip && unzip selawik.zip -d selawik
# then, from this folder
node measure-fonts.js <downloads folder> <selawik folder> C:/Windows/Fonts [symbol fonts...]
node measure-segoewp.js <segoe wp folder> <selawik folder> C:/Windows/Fonts
```

The last folder is the one holding Segoe UI's files (`segoeui.ttf` and its weights): `C:/Windows/Fonts`
on Windows; on Linux or macOS, wherever a copy of them is kept outside the repository.

Versions measured on 2026-11-07: `@fontsource/open-sans` 5.3.0, `@fontsource/noto-sans` 5.3.0 (Latin
subset, `.woff`), Selawik release 1.01. `node_modules/` is ignored by git.
