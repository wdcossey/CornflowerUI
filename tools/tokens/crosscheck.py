"""Cross-checks the SDK's theme resource values against a reference app capture.

For every capture JSON of a refapp run, finds the control's default style (in themeresources.xaml or
generic.xaml), follows each dumped property of each named template part back to the theme resource
key that sets it, in the captured visual state, and compares the value the phone resolved with the
value the SDK file gives that key. It also compares the resources the run's index.json read.

Prints the number of keys checked and every key where the run and the file differ, and writes every
comparison to a JSON file that gen.py reads to name, beside each token, the capture that confirms it.

    python crosscheck.py --design <SDK Xaml/Design folder> --capture <refapp run folder> [--out <file>]
"""
import argparse
import collections
import glob
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from resources import X, RESOURCE_REF, load, local, resolve  # noqa: E402

DEFAULT_OUT = os.path.join(HERE, 'out', 'crosscheck.json')

ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
ap.add_argument('--design', required=True,
                help="the SDK's Xaml Design folder, holding themeresources.xaml and generic.xaml")
ap.add_argument('--capture', required=True, help='a refapp run folder: capture JSONs and index.json')
ap.add_argument('--out', default=DEFAULT_OUT, help='where to write the results (default: %(default)s)')
args = ap.parse_args()

th, top, troot = load(args.design)
groot = ET.parse(os.path.join(args.design, 'generic.xaml')).getroot()

styles = {}   # key or TargetType -> element
for r in (troot, groot):
    for e in r:
        if local(e.tag) == 'Style':
            k = e.get(X + 'Key') or e.get('TargetType')
            styles.setdefault(k, e)


def style_chain(e):
    chain = [e]
    while e is not None and e.get('BasedOn'):
        m = re.match(r'\{StaticResource (\w+)\}', e.get('BasedOn'))
        e = styles.get(m.group(1))
        chain.append(e)
    return [c for c in chain if c is not None]


def setters(chain):
    out = {}
    for s in reversed(chain):
        for st in s:
            if local(st.tag) == 'Setter' and st.get('Value') is not None:
                out[st.get('Property')] = st.get('Value')
    return out


def template(chain):
    for s in chain:
        for st in s:
            if local(st.tag) == 'Setter' and st.get('Property') == 'Template':
                for ct in st.iter():
                    if local(ct.tag) == 'ControlTemplate':
                        return ct
    return None


def named(ct):
    out = {}
    for e in ct.iter():
        n = e.get(X + 'Name')
        if n and local(e.tag) not in ('VisualState', 'VisualStateGroup', 'Storyboard'):
            out[n] = {k: v for k, v in e.attrib.items() if not k.startswith('{')}
    return out


def state_setters(ct, state):
    out = {}
    for vs in ct.iter():
        if local(vs.tag) == 'VisualState' and vs.get(X + 'Name') == state:
            for a in vs.iter():
                t = local(a.tag)
                tn = a.get('{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Storyboard.TargetName') or a.get('Storyboard.TargetName')
                tp = a.get('Storyboard.TargetProperty')
                if t == 'ObjectAnimationUsingKeyFrames':
                    kf = [k for k in a if local(k.tag) == 'DiscreteObjectKeyFrame']
                    if kf:
                        out[(tn, tp)] = kf[0].get('Value') or '(element)'
                elif t == 'DoubleAnimation' and a.get('To') is not None:
                    out[(tn, tp)] = a.get('To')
    return out


# The capture JSON's property names -> the XAML property names.
PROPS = {'background': 'Background', 'foreground': 'Foreground', 'borderBrush': 'BorderBrush',
         'borderThickness': 'BorderThickness', 'padding': 'Padding', 'margin': 'Margin',
         'minWidth': 'MinWidth', 'minHeight': 'MinHeight', 'fontSize': 'FontSize', 'fontWeight': 'FontWeight',
         'fill': 'Fill', 'stroke': 'Stroke', 'strokeThickness': 'StrokeThickness', 'opacity': 'Opacity',
         'characterSpacing': 'CharacterSpacing', 'lineHeight': 'LineHeight', 'width': 'Width', 'height': 'Height'}
# FontWeight names -> the numbers the capture dumps.
WEIGHTS = {'Light': 300, 'SemiLight': 350, 'Normal': 400, 'Semibold': 600, 'SemiBold': 600, 'Bold': 700}


def thick(s):
    p = [float(x) for x in s.split(',')]
    return p * 4 if len(p) == 1 else (p * 2 if len(p) == 2 else p)


def expect_value(theme, raw):
    """raw is a xaml attribute value. Returns (key or None, comparable value)."""
    m = RESOURCE_REF.match(raw)
    if not m:
        return None, raw
    r = resolve(th, top, theme, m.group(1))
    return m.group(1), r


def compare(dumped, r):
    if r is None:
        return None
    t = r[0]
    if t in ('SolidColorBrush', 'Color'):
        col = r[1]
        op = r[2] if len(r) > 2 else 1
        exp = col.upper() + (f'@{op:g}' if op != 1 else '')
        return dumped.upper() == exp, exp
    if t == 'Thickness':
        exp = thick(r[1])
        got = dumped if isinstance(dumped, list) else [dumped] * 4
        return all(abs(a - b) < 0.006 for a, b in zip(exp, got)), r[1]
    if t in ('Double', 'Int32'):
        if isinstance(dumped, list):
            return None
        return abs(float(dumped) - float(r[1])) < 0.006, r[1]
    if t == 'FontWeight':
        return WEIGHTS.get(r[1]) == dumped, r[1]
    return None


VARSTATE = {'checked': 'Checked', 'indeterminate': 'Indeterminate'}
results = collections.defaultdict(list)   # (theme, key) -> list of (ok, file, elem, prop, got, exp)
for f in sorted(glob.glob(os.path.join(args.capture, '*.json'))):
    if f.endswith('index.json'):
        continue
    with open(f, encoding='utf-8-sig') as fh:
        d = json.load(fh)
    theme = 'Default' if d['theme'] == 'Dark' else 'Light'
    spec, var, state = d['specimen'], d['variant'], d['state']
    st = styles.get(var) if spec == 'TextBlock' else styles.get(spec)
    if st is None:
        continue
    chain = style_chain(st)
    sets = setters(chain)
    ct = template(chain)
    names = named(ct) if ct is not None else {}
    ss = {}
    if ct is not None:
        if var in VARSTATE and d.get('group') != 'CommonStates':
            ss.update(state_setters(ct, VARSTATE[var]))
        if state:
            ss.update(state_setters(ct, state))

    def visit(node, is_root):
        nm = node.get('name')
        for dk, prop in PROPS.items():
            if dk not in node:
                continue
            raw = None
            if not is_root and nm and (nm, prop) in ss:
                raw = ss[(nm, prop)]
            elif is_root:
                raw = sets.get(prop)
            elif nm in names and prop in names[nm]:
                raw = names[nm][prop]
                m = re.match(r'\{TemplateBinding (\w+)\}', raw)
                if m:
                    raw = sets.get(m.group(1))
            if raw is None:
                continue
            key, r = expect_value(theme, raw)
            if key is None:
                continue
            c = compare(node[dk], r)
            if c is None:
                continue
            results[(theme, key)].append((c[0], os.path.basename(f), nm or '(root)', prop, node[dk], c[1]))
        for ch in node.get('children', []):
            visit(ch, False)
    visit(d['tree'], True)

# The resources the app read directly, per theme, in index.json.
index = os.path.join(args.capture, 'index.json')
if os.path.exists(index):
    with open(index, encoding='utf-8-sig') as fh:
        idx = json.load(fh)
    for tname, theme in (('dark', 'Default'), ('light', 'Light')):
        for key, got in sorted(idx.get('resources', {}).get(tname, {}).items()):
            c = compare(got, resolve(th, top, theme, key))
            if c is not None:
                results[(theme, key)].append((c[0], 'index.json', '(resource)', key, got, c[1]))

ok = sum(1 for v in results.values() if all(x[0] for x in v))
print('capture:', os.path.basename(os.path.normpath(args.capture)))
print('keys checked:', len(results), 'all-agree:', ok)
for (theme, key), v in sorted(results.items()):
    bad = [x for x in v if not x[0]]
    good = [x for x in v if x[0]]
    if bad:
        print(f'\n{theme} {key}: {len(good)} agree, {len(bad)} differ')
        seen = set()
        for x in bad:
            sig = (x[2], x[3], str(x[4]))
            if sig in seen:
                continue
            seen.add(sig)
            print('   ', x[1], x[2], x[3], 'got', x[4], 'file', x[5])

os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
with open(args.out, 'w') as fh:
    json.dump({'capture': os.path.basename(os.path.normpath(args.capture)),
               'results': {f'{t}|{k}': [list(x) for x in v] for (t, k), v in results.items()}},
              fh, indent=0)
print('\nwrote', args.out)
