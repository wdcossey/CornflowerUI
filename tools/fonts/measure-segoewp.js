// Compares Segoe WP (the Windows Phone 8.1 text font) with Selawik and Segoe UI, weight by weight.
// Usage: node measure-segoewp.js <segoe wp dir> <selawik dir> <windows fonts dir>
// Needs opentype.js beside it (same setup as measure-fonts.js). Reads font files as data only.
// Prints Markdown. Everything in thousandths of an em.
const fs = require('fs');
const path = require('path');
const opentype = require('opentype.js');
const [wp, selawik, winFonts] = process.argv.slice(2);
const load = file => opentype.parse(new Uint8Array(fs.readFileSync(file)).buffer);
const families = {
  'Segoe WP': { 300: 'SegoeWP-Light.ttf', 350: 'SegoeWP-Semilight.ttf', 400: 'SegoeWP.ttf', 600: 'SegoeWP-Semibold.ttf', 700: 'SegoeWP-Bold.ttf', 900: 'SegoeWP-Black.ttf' },
  'Selawik': { 300: 'selawkl.ttf', 350: 'selawksl.ttf', 400: 'selawk.ttf', 600: 'selawksb.ttf', 700: 'selawkb.ttf' },
  'Segoe UI': { 300: 'segoeuil.ttf', 350: 'segoeuisl.ttf', 400: 'segoeui.ttf', 600: 'seguisb.ttf', 700: 'segoeuib.ttf', 900: 'seguibl.ttf' },
};
const dirs = { 'Segoe WP': wp, 'Selawik': selawik, 'Segoe UI': winFonts };
const samples = {
  lower: 'the quick brown fox jumps over the lazy dog',
  upper: 'THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG',
  digits: '0123456789',
  ui: 'settings email+accounts Wi-Fi Bluetooth lock screen notifications 12:45 Tuesday, 6 November',
};
function measure(file) {
  const f = load(file), u = f.unitsPerEm, em = v => Math.round((v / u) * 1000);
  const os2 = f.tables.os2, hhea = f.tables.hhea;
  const out = {
    version: (f.names.version && (f.names.version.en || Object.values(f.names.version)[0])) || '',
    weightClass: os2.usWeightClass, unitsPerEm: u,
    xHeight: em(os2.sxHeight), capHeight: em(os2.sCapHeight),
    ascent: em(hhea.ascender), descent: em(-hhea.descender),
    hheaLine: em(hhea.ascender - hhea.descender + hhea.lineGap),
    winLine: em(os2.usWinAscent + os2.usWinDescent),
    typoLine: em(os2.sTypoAscender - os2.sTypoDescender + os2.sTypoLineGap),
    glyphs: f.glyphs.length,
  };
  for (const [n, t] of Object.entries(samples)) {
    out[n] = Math.round(f.getAdvanceWidth(t, 1000, { kerning: false }));
    out[n + 'Kern'] = Math.round(f.getAdvanceWidth(t, 1000, { kerning: true }));
  }
  return out;
}
const results = {};
for (const [fam, weights] of Object.entries(families)) {
  results[fam] = {};
  for (const [w, name] of Object.entries(weights)) {
    const file = path.join(dirs[fam], name);
    if (!fs.existsSync(file)) { console.error(`missing: ${file}`); continue; }
    results[fam][w] = measure(file);
  }
}
const pct = (a, b) => ((a / b - 1) * 100).toFixed(1).replace(/^(?!-)/, '+') + '%';
const ref = results['Segoe WP'];
console.log('## Weights found\n');
for (const [fam, ws] of Object.entries(results))
  console.log(`- ${fam}: ${Object.entries(ws).map(([w, m]) => `${w} (usWeightClass ${m.weightClass}, ${m.version.trim()}, ${m.glyphs} glyphs, ${m.unitsPerEm} upem)`).join('; ')}`);
console.log('\n## Vertical metrics (thousandths of an em)\n');
console.log('| Family | Weight | x-height | cap height | hhea ascent | hhea descent | hhea line | win line | typo line |');
console.log('|---|---|---|---|---|---|---|---|---|');
for (const [fam, ws] of Object.entries(results)) for (const [w, m] of Object.entries(ws))
  console.log(`| ${fam} | ${w} | ${m.xHeight} | ${m.capHeight} | ${m.ascent} | ${m.descent} | ${m.hheaLine} | ${m.winLine} | ${m.typoLine} |`);
console.log('\n## Against Segoe WP at the same weight (no kerning)\n');
console.log('| Family | Weight | lower | upper | digits | ui | x-height | cap height | hhea line |');
console.log('|---|---|---|---|---|---|---|---|---|');
for (const [fam, ws] of Object.entries(results)) {
  if (fam === 'Segoe WP') continue;
  for (const [w, m] of Object.entries(ws)) { const r = ref[w]; if (!r) continue;
    console.log(`| ${fam} | ${w} | ${pct(m.lower, r.lower)} | ${pct(m.upper, r.upper)} | ${pct(m.digits, r.digits)} | ${pct(m.ui, r.ui)} | ${pct(m.xHeight, r.xHeight)} | ${pct(m.capHeight, r.capHeight)} | ${pct(m.hheaLine, r.hheaLine)} |`); }
}
console.log('\n## Raw widths of the "ui" sample (no kerning / with kerning)\n');
console.log('| Family | Weight | no kerning | kerning |');
console.log('|---|---|---|---|');
for (const [fam, ws] of Object.entries(results)) for (const [w, m] of Object.entries(ws)) console.log(`| ${fam} | ${w} | ${m.ui} | ${m.uiKern} |`);
