"""Writes the Windows Phone 8.1 theme's token files and the token tests' expected values.

Reads themeresources.xaml and the named accent files from the SDK, and the results crosscheck.py wrote
for a reference app run, and writes, under the repository root:

    src/Cornflower.Themes.Wp81/Tokens/Colors.axaml, Accent.axaml, Typography.axaml, Sizes.axaml
    tests/Cornflower.Tests/Tokens/TokenExpectations.cs

Each token keeps its SDK key and value; its comment names the key and, where the run read the same
value on the phone, the capture that shows it. Prints the counts, and any key where the run and the
file differ that isn't one of the known differences below.

    python gen.py --design <SDK Xaml/Design folder> --accents <SDK Design/AccentColors folder> [--crosscheck <file>]
"""
import argparse
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from resources import ACCENT, ACCENT_KEY, load, norm_color, resolve  # noqa: E402

ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
ap.add_argument('--design', required=True, help="the SDK's Xaml Design folder, holding themeresources.xaml")
ap.add_argument('--accents', required=True, help="the SDK's Design AccentColors folder, one <Name>.xaml per accent")
ap.add_argument('--crosscheck', default=os.path.join(HERE, 'out', 'crosscheck.json'),
                help='the file crosscheck.py wrote (default: %(default)s)')
args = ap.parse_args()

OUT = os.path.normpath(os.path.join(HERE, '..', '..')) + '/'
TOK = OUT + 'src/Cornflower.Themes.Wp81/Tokens/'
th, top, _ = load(args.design)
dark, light = th['Default'], th['Light']
with open(args.crosscheck) as fh:
    _cc = json.load(fh)
RUN, cc = _cc['capture'], _cc['results']
# The token comments name the run as a capture of the reference profile: device 11, 8.1 Update 1
# 1080p 5.5" (ADR 0001). A run on another device can be cross-checked, but tokens aren't written from it.
if not re.search(r'-dev11\b', RUN):
    sys.exit(f'{RUN} is not a run on device 11, the reference profile: the tokens are written from a '
             'reference-profile run. Cross-check other devices with crosscheck.py alone.')


def run_of(theme, key):
    v = cc.get(f'{theme}|{key}')
    if not v or not all(x[0] for x in v):
        return None
    files = [x[1] for x in v]
    for pref in ('__base.json', '.Normal.json'):
        for f in files:
            if f.endswith(pref):
                return f
    return files[0]


# The keys index.json reads directly; the run's index.json names them.
IDX_KEYS = {'ApplicationPageBackgroundThemeBrush', 'PhoneAccentBrush', 'PhoneForegroundBrush', 'PhoneBackgroundBrush'}
# The keys where run 20261108-1443-dev11 read another value than the file's, and why, as worked out in
# PR #45. Their comments say so instead of naming a capture.
DIFFER = {
    'SliderBorderThemeThickness': 'run: the Slider border Rectangles draw StrokeThickness 1, the type mismatch falling back to the default (Slider value-30 Disabled); see the PR',
    'ProgressRingActiveBackgroundOpacity': 'run: read 0, the Active storyboard not applied at capture (ProgressRing active Active); see the PR',
}


def short(f):
    # dark__Button__default__CommonStates.Pressed.json -> Button default CommonStates.Pressed
    parts = f[:-5].split('__')
    return ' '.join(parts[1:])


def note(theme, key, src):
    bits = [src]
    if key in DIFFER:
        bits.append(DIFFER[key])
    elif key in IDX_KEYS:
        bits.append('run: index.json')
    else:
        r = run_of(theme, key)
        if r:
            bits.append('run: ' + short(r))
    return '; '.join(bits)


def brush_color_xaml(attrs, scope):
    c = attrs['Color']
    m = re.match(r'\{(?:Theme|Static)Resource (\w+)\}', c)
    if m:
        k = m.group(1)
        if k == ACCENT_KEY:
            return '{DynamicResource %s}' % k
        assert k in scope, k
        return '{StaticResource %s}' % k
    return c


def theme_dict(name, d, srcname):
    lines = [f'    <ResourceDictionary x:Key="{name}">']
    colors = sorted(k for k in d if d[k][0] == 'Color')
    brushes = sorted(k for k in d if d[k][0] == 'SolidColorBrush')
    others = sorted(k for k in d if d[k][0] not in ('Color', 'SolidColorBrush'))
    lines.append(f'      <!-- Colours ({len(colors)}) -->')
    for k in colors:
        lines.append(f'      <Color x:Key="{k}">{d[k][1]}</Color> <!-- {note(srcname, k, k)} -->')
    lines.append(f'      <!-- Brushes ({len(brushes)}) -->')
    for k in brushes:
        col = brush_color_xaml(d[k][2], d)
        op = d[k][2].get('Opacity')
        opx = f' Opacity="{op}"' if op else ''
        lines.append(f'      <SolidColorBrush x:Key="{k}" Color="{col}"{opx} /> <!-- {note(srcname, k, k)} -->')
    lines.append(f'      <!-- Opacity ({len(others)}) -->')
    for k in others:
        lines.append(f'      <x:Double x:Key="{k}">{d[k][1]}</x:Double> <!-- {note(srcname, k, k)} -->')
    lines.append('    </ResourceDictionary>')
    return lines, len(colors), len(brushes), len(others)


HEAD = '''<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">'''

# ---------- Colors.axaml
dl, nc, nb, no = theme_dict('Dark', dark, 'Default')
ll, _, _, _ = theme_dict('Light', light, 'Light')
shared_colors = sorted(k for k, v in top.items() if v[0] == 'Color')
shared_brushes = sorted(k for k, v in top.items() if v[0] == 'SolidColorBrush')
L = [HEAD, f'''  <!--
    Windows Phone 8.1 theme colours and brushes, dark and light.

    Source: themeresources.xaml of the Windows Phone 8.1 SDK (Windows Phone Kits\\8.1\\Include\\abi\\Xaml\\Design),
    read as a specification. Its Default dictionary is the dark theme and is ThemeVariant Dark here; its Light
    dictionary is Light. HighContrastBlack and HighContrastWhite are not here yet. The keys are the SDK's own.

    Each entry ends with a comment naming its source key and, where the reference app read the value on the
    phone, "run:" with the capture's specimen, variant and state (refapp run {RUN}, device 11, the
    reference profile; index.json is the run's own resource read). A run value is the file's unless the comment
    says otherwise.

    A brush gives the colour key it takes in its Color. The accent comes from SystemColorControlAccentColor, in
    Accent.axaml, through a DynamicResource, so an app that redefines that colour moves every accent brush.
    Every other brush takes its colour from its own theme dictionary through a StaticResource, fixed when the
    file loads.
  -->''', '  <ResourceDictionary.ThemeDictionaries>']
L += dl + ll + ['  </ResourceDictionary.ThemeDictionaries>', '',
                f'  <!-- The same in both themes: the entries outside the SDK file\'s theme dictionaries. -->',
                f'  <!-- Colours ({len(shared_colors)}) -->']
for k in shared_colors:
    L.append(f'  <Color x:Key="{k}">{top[k][1]}</Color> <!-- {note("Default", k, k)} -->')
L.append(f'  <!-- Brushes ({len(shared_brushes)}) -->')
for k in shared_brushes:
    col = brush_color_xaml(top[k][2], top)
    L.append(f'  <SolidColorBrush x:Key="{k}" Color="{col}" /> <!-- {note("Default", k, k)} -->')
L.append('</ResourceDictionary>')
open(TOK + 'Colors.axaml', 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')

# ---------- Accent.axaml
ACCENTS = {}
for f in sorted(os.listdir(args.accents)):
    if f.endswith('.xaml'):
        with open(os.path.join(args.accents, f), encoding='utf-8-sig') as fh:
            m = re.search(r'<Color x:Key="PhoneAccentColor">([^<]+)</Color>', fh.read())
        ACCENTS[f[:-5]] = m.group(1).strip()
assert len(ACCENTS) == 20, f'{len(ACCENTS)} accent files in {args.accents}, not 20'
A = [HEAD, f'''  <!--
    The accent.

    SystemColorControlAccentColor is the system colour that every accent brush in themeresources.xaml takes
    (PhoneAccentBrush, ButtonPressedBackgroundThemeBrush and the rest, in Colors.axaml). The SDK file doesn't
    define it: the phone supplies it. Its default here is {ACCENT}, the emulator's PhoneAccentBrush as the
    reference app read it in both themes (refapp run {RUN}, index.json; ADR 0002).

    An app picks another accent by defining SystemColorControlAccentColor in Application.Resources, which is
    searched before the theme, for example as one of the named accents below.

    The 20 named accents are the phone's accent palette, from the Windows Phone 8.1 SDK's design files
    (Microsoft SDKs\\Windows Phone\\v8.1\\Design\\AccentColors\\<Name>.xaml, each one PhoneAccentColor).
    WinRT has no keys for them, so these keys are Cornflower's: CornflowerAccent<Name>Color. The emulator's
    default, {ACCENT}, is none of the 20.
  -->''',
     f'  <Color x:Key="{ACCENT_KEY}">{ACCENT}</Color> <!-- {ACCENT_KEY}; run: index.json, PhoneAccentBrush in dark and light -->', '',
     '  <!-- Named accents (20) -->']
for n, v in ACCENTS.items():
    # Cobalt is the one accent whose value changed from 8.0 to 8.1; the 8.0 value, read from the 8.0 SDK's
    # AccentColors\Cobalt.xaml, is kept in its comment.
    extra = '; the 8.0 SDK has #FF0050EF' if n == 'Cobalt' else ''
    A.append(f'  <Color x:Key="CornflowerAccent{n}Color">{v}</Color> <!-- PhoneAccentColor, AccentColors\\{n}.xaml{extra} -->')
A.append('</ResourceDictionary>')
open(TOK + 'Accent.axaml', 'w', encoding='utf-8', newline='\n').write('\n'.join(A) + '\n')

# ---------- Typography.axaml / Sizes.axaml
typo = [k for k, v in top.items() if (v[0] == 'Double' and 'FontSize' in k) or v[0] in ('FontWeight', 'Int32')]
sizes = [k for k, v in top.items() if v[0] in ('Double', 'Thickness') and k not in typo]
WEIGHT = {'Semibold': 'SemiBold', 'SemiLight': 'SemiLight', 'Light': 'Light'}


def entry(k):
    t, text, _ = top[k]
    n = note('Default', k, k)
    if t == 'Double':
        return f'  <x:Double x:Key="{k}">{text}</x:Double> <!-- {n} -->'
    if t == 'Int32':
        return f'  <x:Int32 x:Key="{k}">{text}</x:Int32> <!-- {n} -->'
    if t == 'Thickness':
        return f'  <Thickness x:Key="{k}">{text}</Thickness> <!-- {n} -->'
    if t == 'FontWeight':
        extra = f'; the file spells it {text}' if WEIGHT[text] != text else ''
        return f'  <FontWeight x:Key="{k}">{WEIGHT[text]}</FontWeight> <!-- {n}{extra} -->'


T = [HEAD, f'''  <!--
    The type ramp: font sizes, weights and character spacing.

    Source: the entries outside the theme dictionaries of the Windows Phone 8.1 SDK's themeresources.xaml, the
    same in both themes. Each entry ends with a comment naming its source key and, where the reference app read
    the value on the phone, "run:" with the capture's specimen, variant and state (refapp run
    {RUN}, device 11). Every value the run read is the file's.

    Not here: the font family keys (PhoneFontFamilyNormal and the rest, ContentControlThemeFontFamily,
    PivotHeaderItemFontFamily), which are for Fonts.axaml; and the sizes the SDK's TextBlock styles set as
    literals rather than keys (HeaderTextBlockStyle's 57.5, for example), which belong with those styles.

    Sizes are in view pixels, as on the phone. A CharacterSpacing is in WinRT's unit, thousandths of an em.
    Avalonia's LetterSpacing is in DIPs, so a template sets n * FontSize / 1000.
  -->''']
T += ['', f'  <!-- Font sizes ({sum(1 for k in typo if top[k][0] == "Double")}), smallest first -->'] + [entry(k) for k in sorted((k for k in typo if top[k][0] == 'Double'), key=lambda k: (float(top[k][1]), k))]
T += ['', f'  <!-- Font weights ({sum(1 for k in typo if top[k][0] == "FontWeight")}) -->'] + [entry(k) for k in typo if top[k][0] == 'FontWeight']
T += ['', f'  <!-- Character spacing ({sum(1 for k in typo if top[k][0] == "Int32")}) -->'] + [entry(k) for k in typo if top[k][0] == 'Int32']
T.append('</ResourceDictionary>')
open(TOK + 'Typography.axaml', 'w', encoding='utf-8', newline='\n').write('\n'.join(T) + '\n')

S = [HEAD, f'''  <!--
    Thicknesses, sizes, opacities and offsets.

    Source: the entries outside the theme dictionaries of the Windows Phone 8.1 SDK's themeresources.xaml, the
    same in both themes. Each entry ends with a comment naming its source key and, where the reference app read
    the value on the phone, "run:" with the capture's specimen, variant and state (refapp run
    {RUN}, device 11). Values are in view pixels.

    Each keeps the file's type, even where its template's property has another: CheckBoxBorderThemeThickness
    and RadioButtonBorderThemeThickness are doubles, and SliderBorderThemeThickness is a Thickness that the
    Slider template binds to a Rectangle's StrokeThickness.
  -->''']
nt = sum(1 for k in sizes if top[k][0] == 'Thickness')
S += ['', f'  <!-- Thicknesses ({nt}) -->'] + [entry(k) for k in sorted(sizes) if top[k][0] == 'Thickness']
S += ['', f'  <!-- Sizes, opacities and offsets ({len(sizes) - nt}) -->'] + [entry(k) for k in sorted(sizes) if top[k][0] == 'Double']
S.append('</ResourceDictionary>')
open(TOK + 'Sizes.axaml', 'w', encoding='utf-8', newline='\n').write('\n'.join(S) + '\n')


# ---------- expectations
def thick4(s):
    p = s.split(',')
    p = p * 4 if len(p) == 1 else (p * 2 if len(p) == 2 else p)
    return ','.join(f'{float(x):g}' for x in p)


WNUM = {'Semibold': 600, 'SemiLight': 350, 'Light': 300}


def expect(theme, k, d):
    t = d[k][0]
    if t == 'Color':
        return 'Color', norm_color(d[k][1])
    if t == 'SolidColorBrush':
        r = resolve(th, top, theme, k)
        return 'Brush', r[1] + (f'@{r[2]:g}' if r[2] != 1 else '')
    if t == 'Double':
        return 'Double', f'{float(d[k][1]):g}'
    if t == 'Int32':
        return 'Int32', d[k][1]
    if t == 'Thickness':
        return 'Thickness', thick4(d[k][1])
    if t == 'FontWeight':
        return 'FontWeight', str(WNUM[d[k][1]])


def src_cs(theme, k):
    if k in DIFFER:
        return 'themeresources.xaml; the run differs, see the PR'
    if k in IDX_KEYS:
        return 'themeresources.xaml; run index.json'
    r = run_of(theme, k)
    return 'themeresources.xaml' + (f'; run {r}' if r else '')


C = ['''// Expected token values, written out here because CI can't read the reference folder.
// Source: themeresources.xaml of the Windows Phone 8.1 SDK (Windows Phone Kits\\8.1\\Include\\abi\\Xaml\\Design):
// its Default (dark) and Light dictionaries, and the entries outside them; the named accents from the 8.1 SDK's
// design files (AccentColors\\<Name>.xaml). "run" names a capture of refapp run {RUN} (device 11,
// the reference profile) in which the phone resolved the same value.
// Values: colours and brushes #AARRGGBB, with a brush's Opacity after "@"; thicknesses left,top,right,bottom;
// font weights as numbers.

namespace Cornflower.Tests.Tokens;

internal static class TokenExpectations
{
    /// <summary>The keys in the theme dictionaries: key, kind, dark, light, source.</summary>
    internal static readonly ThemedToken[] Themed =
    ['''.replace('{RUN}', RUN)]
for k in sorted(dark):
    kind, dv = expect('Default', k, dark)
    _, lv = expect('Light', k, light)
    sd, sl = src_cs('Default', k), src_cs('Light', k)
    src = sd if sd == sl else f'{sd} (dark); {sl.replace("themeresources.xaml; ", "").replace("themeresources.xaml", "no run")} (light)'
    C.append(f'        new("{k}", TokenKind.{kind}, "{dv}", "{lv}", "{src}"),')
C += ['    ];', '', '    /// <summary>The keys that are the same in both themes: key, kind, value, source.</summary>',
      '    internal static readonly SharedToken[] Shared =', '    [']
C.append(f'        new("{ACCENT_KEY}", TokenKind.Color, "{ACCENT}", "the default accent; run index.json, PhoneAccentBrush in dark and light"),')
for n, v in ACCENTS.items():
    C.append(f'        new("CornflowerAccent{n}Color", TokenKind.Color, "{v.upper()}", "AccentColors\\\\{n}.xaml PhoneAccentColor"),')
for k in sorted(top):
    if top[k][0] not in ('Color', 'SolidColorBrush', 'Double', 'Thickness', 'Int32', 'FontWeight'):
        continue
    kind, v = expect('Default', k, top)
    C.append(f'        new("{k}", TokenKind.{kind}, "{v}", "{src_cs("Default", k)}"),')
C += ['    ];', '}', '']
open(OUT + 'tests/Cornflower.Tests/Tokens/TokenExpectations.cs', 'w', encoding='utf-8', newline='\n').write('\n'.join(C))

confirmed = set()
for key in list(dark) + list(top):
    if key in IDX_KEYS or run_of('Default', key) or run_of('Light', key):
        confirmed.add(key)
print('themed keys', len(dark), 'colours', nc, 'brushes', nb, 'other', no)
print('shared colours', len(shared_colors), 'brushes', len(shared_brushes))
print('typo', len(typo), 'sizes', len(sizes), 'thickness', nt, 'accents', len(ACCENTS))
print('confirmed by run', len(confirmed))

# A key where this run and the file disagree, other than the known differences, gets no "run:" note.
# Say so, so a new capture that disagrees isn't missed.
new_diffs = sorted(tk for tk, v in cc.items() if not all(x[0] for x in v) and tk.split('|')[1] not in DIFFER)
if new_diffs:
    print(f'\n{len(new_diffs)} key(s) where run {RUN} differs from the file and DIFFER has no entry '
          "(see crosscheck.py's output):")
    for tk in new_diffs:
        print('   ', tk)
