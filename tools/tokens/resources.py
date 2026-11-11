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
# Cornflower's default accent, the phone's default, Cobalt. In the 8.1 Update 1 image's registry
# (config\SOFTWARE, Microsoft\Windows\CurrentVersion\Control Panel\Theme), DefaultAccent is 5, and accent 5
# is #FF3E65FF in both themes, named "Cobalt" by UIXMobileRes.dll.mui string 10022. The emulator's
# PhoneAccentBrush reads the same in both themes (refapp run 20261108-1443-dev11, index.json). ADR 0002.
ACCENT_KEY = 'SystemColorControlAccentColor'
ACCENT = '#FF3E65FF'

# The named accents where the phone's palette differs from the SDK's design files. The palette is the same
# registry key's Themes\{0,1}\Accents\0-19, named by UIXMobileRes.dll.mui strings 10017-10026 and
# 10050-10059; it is the same in both themes, and in RTM and Update 1. Of the 20, only Cobalt differs from
# the SDK: the 8.1 SDK's AccentColors\Cobalt.xaml has #FF0B6CF8 (the 8.0 SDK's #FF0050EF). The phone wins.
PHONE_ACCENTS = {'Cobalt': ACCENT}

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
