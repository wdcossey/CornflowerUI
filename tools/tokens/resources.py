"""Reads the Windows Phone 8.1 SDK's themeresources.xaml into Python dictionaries.

Used by crosscheck.py and gen.py. The file is read at run time from where it is kept, outside the
repository; nothing from it is stored here.

load() returns the file's theme dictionaries (Default, which is dark, and Light, and the high-contrast
ones), the entries outside them, and the parsed root. Each entry is (element type, text, attributes),
keyed by its x:Key. resolve() follows a brush's {ThemeResource} or {StaticResource} to its colour.
"""
import os
import re
import xml.etree.ElementTree as ET

X = '{http://schemas.microsoft.com/winfx/2006/xaml}'

# The phone supplies SystemColorControlAccentColor; themeresources.xaml doesn't define it. This is
# Cornflower's default accent, the emulator's PhoneAccentBrush as the reference app read it in both
# themes (refapp run 20261108-1443-dev11, index.json), chosen in ADR 0002.
ACCENT_KEY = 'SystemColorControlAccentColor'
ACCENT = '#FF3E65FF'

# The XAML named colours that appear as values, as #AARRGGBB.
NAMED = {'Transparent': '#00FFFFFF', 'White': '#FFFFFFFF', 'Black': '#FF000000'}

RESOURCE_REF = re.compile(r'\{(?:Theme|Static)Resource (\w+)\}')


def local(tag):
    return tag.split('}')[-1]


def raw_entries(elems):
    out = {}
    for e in elems:
        t = local(e.tag)
        if t in ('Style', 'DataTemplate', 'ControlTemplate', 'ResourceDictionary.ThemeDictionaries'):
            continue
        k = e.get(X + 'Key')
        attrs = {kk: v for kk, v in e.attrib.items() if not kk.startswith(X)}
        out[k] = (t, (e.text or '').strip(), attrs)
    return out


def load(design_dir):
    """Parses <design_dir>/themeresources.xaml. Returns (themes, top, root)."""
    root = ET.parse(os.path.join(design_dir, 'themeresources.xaml')).getroot()
    themes = {}
    for c in root:
        if local(c.tag) == 'ResourceDictionary.ThemeDictionaries':
            for td in c:
                themes[td.get(X + 'Key')] = raw_entries(td)
    top = raw_entries(root)
    return themes, top, root


def norm_color(v):
    v = v.strip()
    if v in NAMED:
        return NAMED[v]
    return v.upper()


def resolve(themes, top, theme, key):
    """Returns (type, value): colours as #AARRGGBB, brushes as (type, #AARRGGBB, opacity)."""
    d = themes[theme]
    if key == ACCENT_KEY:
        return ('Color', ACCENT)
    ent = d.get(key) or top.get(key)
    if ent is None:
        return None
    t, text, attrs = ent
    if t == 'Color':
        return ('Color', norm_color(text))
    if t == 'SolidColorBrush':
        c = attrs.get('Color')
        m = RESOURCE_REF.match(c)
        col = resolve(themes, top, theme, m.group(1))[1] if m else norm_color(c)
        return ('SolidColorBrush', col, float(attrs.get('Opacity', '1')))
    return (t, text)
