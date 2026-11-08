// Compares open fonts with Segoe UI, weight by weight.
// Usage: node measure-fonts.js <fontsource dir> <selawik dir> <windows fonts dir> [symbol fonts...]
// Needs opentype.js beside it. Setup is in README.md beside it.
// Reads font files as data only. Prints Markdown.
const fs = require('fs');
const path = require('path');
const opentype = require('opentype.js');

const [fontsource, selawik, winFonts] = process.argv.slice(2);

const load = file => opentype.parse(new Uint8Array(fs.readFileSync(file)).buffer);

const fs_ = (family, weight) =>
  path.join(fontsource, 'node_modules', '@fontsource', family, 'files', `${family}-latin-${weight}-normal.woff`);

const families = {
  'Segoe UI': {
    300: path.join(winFonts, 'segoeuil.ttf'),
    350: path.join(winFonts, 'segoeuisl.ttf'),
    400: path.join(winFonts, 'segoeui.ttf'),
    600: path.join(winFonts, 'seguisb.ttf'),
    700: path.join(winFonts, 'segoeuib.ttf'),
  },
  'Selawik': {
    300: path.join(selawik, 'selawkl.ttf'),
    350: path.join(selawik, 'selawksl.ttf'),
    400: path.join(selawik, 'selawk.ttf'),
    600: path.join(selawik, 'selawksb.ttf'),
    700: path.join(selawik, 'selawkb.ttf'),
  },
  'Open Sans': { 300: fs_('open-sans', 300), 400: fs_('open-sans', 400), 600: fs_('open-sans', 600), 700: fs_('open-sans', 700) },
  'Noto Sans': { 300: fs_('noto-sans', 300), 400: fs_('noto-sans', 400), 600: fs_('noto-sans', 600), 700: fs_('noto-sans', 700) },
};

const samples = {
  lower: 'the quick brown fox jumps over the lazy dog',
  upper: 'THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG',
  digits: '0123456789',
  ui: 'settings email+accounts Wi-Fi Bluetooth lock screen notifications 12:45 Tuesday, 6 November',
};

// Everything in thousandths of an em, so fonts with different unitsPerEm compare directly.
function measure(file) {
  const f = load(file);
  const u = f.unitsPerEm;
  const em = v => Math.round((v / u) * 1000);
  const os2 = f.tables.os2;
  const hhea = f.tables.hhea;
  const out = {
    weightClass: os2.usWeightClass,
    xHeight: em(os2.sxHeight),
    capHeight: em(os2.sCapHeight),
    // What a renderer adds up for one line: hhea ascender - descender + lineGap.
    hheaLine: em(hhea.ascender - hhea.descender + hhea.lineGap),
    winLine: em(os2.usWinAscent + os2.usWinDescent),
    typoLine: em(os2.sTypoAscender - os2.sTypoDescender + os2.sTypoLineGap),
    ascent: em(hhea.ascender),
    descent: em(-hhea.descender),
    glyphs: f.glyphs.length,
  };
  for (const [name, text] of Object.entries(samples)) {
    out[name] = Math.round(f.getAdvanceWidth(text, 1000, { kerning: false }));
    out[name + 'Kern'] = Math.round(f.getAdvanceWidth(text, 1000, { kerning: true }));
  }
  return out;
}

const results = {};
for (const [family, weights] of Object.entries(families)) {
  results[family] = {};
  for (const [w, file] of Object.entries(weights)) {
    if (!fs.existsSync(file)) { console.error(`missing: ${file}`); continue; }
    results[family][w] = measure(file);
  }
}

const pct = (a, b) => ((a / b - 1) * 100).toFixed(1).replace(/^(?!-)/, '+') + '%';
const ref = results['Segoe UI'];

console.log('## Weights found\n');
for (const [family, weights] of Object.entries(results)) {
  console.log(`- ${family}: ${Object.entries(weights).map(([w, m]) => `${w} (usWeightClass ${m.weightClass})`).join(', ')}`);
}

console.log('\n## Vertical metrics (thousandths of an em)\n');
console.log('| Family | Weight | x-height | cap height | hhea ascent | hhea descent | hhea line | win line | typo line |');
console.log('|---|---|---|---|---|---|---|---|---|');
for (const [family, weights] of Object.entries(results)) {
  for (const [w, m] of Object.entries(weights)) {
    console.log(`| ${family} | ${w} | ${m.xHeight} | ${m.capHeight} | ${m.ascent} | ${m.descent} | ${m.hheaLine} | ${m.winLine} | ${m.typoLine} |`);
  }
}

console.log('\n## Text width against Segoe UI at the same weight (no kerning)\n');
console.log('| Family | Weight | lower | upper | digits | ui | x-height | cap height | hhea line |');
console.log('|---|---|---|---|---|---|---|---|---|');
for (const [family, weights] of Object.entries(results)) {
  if (family === 'Segoe UI') continue;
  for (const [w, m] of Object.entries(weights)) {
    const r = ref[w];
    if (!r) continue;
    console.log(`| ${family} | ${w} | ${pct(m.lower, r.lower)} | ${pct(m.upper, r.upper)} | ${pct(m.digits, r.digits)} | ${pct(m.ui, r.ui)} | ${pct(m.xHeight, r.xHeight)} | ${pct(m.capHeight, r.capHeight)} | ${pct(m.hheaLine, r.hheaLine)} |`);
  }
}

console.log('\n## Segoe UI Semilight (350) against the nearest weights of fonts without one\n');
console.log('| Family | Weight used | lower | upper | ui |');
console.log('|---|---|---|---|---|');
for (const family of ['Open Sans', 'Noto Sans']) {
  for (const w of [300, 400]) {
    const m = results[family][w], r = ref[350];
    console.log(`| ${family} | ${w} | ${pct(m.lower, r.lower)} | ${pct(m.upper, r.upper)} | ${pct(m.ui, r.ui)} |`);
  }
}

console.log('\n## Raw widths of the "ui" sample (thousandths of an em; no kerning / with kerning)\n');
console.log('| Family | Weight | no kerning | kerning |');
console.log('|---|---|---|---|');
for (const [family, weights] of Object.entries(results)) {
  for (const [w, m] of Object.entries(weights)) console.log(`| ${family} | ${w} | ${m.ui} | ${m.uiKern} |`);
}

// The symbol fonts, for the icon section: how many glyphs each has.
const extra = process.argv.slice(5);
if (extra.length) {
  console.log('\n## Symbol fonts\n');
  for (const file of extra) {
    const f = load(file);
    const cps = Object.keys(f.tables.cmap.glyphIndexMap).map(Number);
    const pua = cps.filter(c => c >= 0xE000 && c <= 0xF8FF);
    console.log(`- ${path.basename(file)}: ${f.glyphs.length} glyphs, ${cps.length} mapped code points, ${pua.length} in the private use area` +
      (pua.length ? ` (U+${Math.min(...pua).toString(16).toUpperCase()} to U+${Math.max(...pua).toString(16).toUpperCase()})` : ''));
  }
}
