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

# The phone's accent palette: registry index, name, colour. Read from the 8.1 Update 1 image's registry
# (config\SOFTWARE, Microsoft\Windows\CurrentVersion\Control Panel\Theme): Themes\0\Accents\<i> and
# Themes\1\Accents\<i> (light and dark, the same) give each Color, and AccentDisplayNames\<i> names it by a
# UIXMobileRes.dll string id, which UIXMobileRes.dll.mui (en-US) resolves (ids 10017-10026 and 10050-10059).
# The RTM image's registry is the same. gen.py writes the named accents from this table and checks the SDK's
# AccentColors\<Name>.xaml against it.
PHONE_ACCENTS = [
    (0, 'Lime', '#FFA4C400'), (1, 'Green', '#FF60A917'), (2, 'Emerald', '#FF008A00'),
    (3, 'Teal', '#FF00ABA9'), (4, 'Cyan', '#FF1BA1E2'), (5, 'Cobalt', '#FF3E65FF'),
    (6, 'Indigo', '#FF6A00FF'), (7, 'Violet', '#FFAA00FF'), (8, 'Pink', '#FFF472D0'),
    (9, 'Magenta', '#FFD80073'), (10, 'Crimson', '#FFA20025'), (11, 'Red', '#FFE51400'),
    (12, 'Orange', '#FFFA6800'), (13, 'Amber', '#FFF0A30A'), (14, 'Yellow', '#FFE3C800'),
    (15, 'Brown', '#FF825A2C'), (16, 'Olive', '#FF6D8764'), (17, 'Steel', '#FF647687'),
    (18, 'Mauve', '#FF76608A'), (19, 'Taupe', '#FF87794E'),
]

# The phone supplies SystemColorControlAccentColor; themeresources.xaml doesn't define it. Cornflower's
# default is the phone's: the same registry key's DefaultAccent, 5, Cobalt. The emulator's PhoneAccentBrush
# reads the same colour in both themes (refapp run 20261108-1443-dev11, index.json). ADR 0002.
ACCENT_KEY = 'SystemColorControlAccentColor'
DEFAULT_ACCENT_INDEX = 5
DEFAULT_ACCENT = next(n for i, n, _ in PHONE_ACCENTS if i == DEFAULT_ACCENT_INDEX)
ACCENT = next(c for i, _, c in PHONE_ACCENTS if i == DEFAULT_ACCENT_INDEX)

# Where the SDK's design files differ from the phone: name, the 8.1 SDK file's value, the 8.0 SDK file's
# (compared in PR #45). gen.py stops on any other difference, or if one of these comes to agree.
SDK_ACCENT_DIFFERS = {'Cobalt': ('#FF0B6CF8', '#FF0050EF')}

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
