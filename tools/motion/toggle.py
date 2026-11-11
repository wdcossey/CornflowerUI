"""The ToggleSwitch's tap limits, from `-Probe toggleswitch` logs (20261110-0733-dev10 on the 720p profile,
20261109-2128-dev11 on the reference profile).

Usage: python -I toggle.py <run folder> [<run folder> ...]

The probe presses the switch's knob, Off, holds it for a set time while moving right a set distance, and releases.
For each press: how long it was held, how far it moved (window x at release - at press, view px), the knob's
position at release, whether it toggled, and the ToggleStates changes after the release (state>state@ms). Then the
limits: of the presses that didn't drag the knob past halfway (no Dragging>On), the farthest that still toggled (a
tap) and the nearest, held under 900 ms (inside the hold limit), that didn't; of the presses that didn't move, the
longest that still toggled and the shortest that didn't.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

runs, _ = ml.parse_args(__doc__, nargs=(1, 99))
for run in runs:
    print('==', run)
    rows, cur = [], None
    for t, what, det in ml.probe(run):
        p = det.split()
        if what in ('ready', 'layout'):
            print('  ', det)
        if what == 'pressed':
            cur = dict(down=t, x0=float(p[0]), knob=0.0, on=False, states=[])
        elif cur is None:
            continue
        elif what == 'value' and p[0] == 'knobX':
            if 'up' not in cur:
                cur['knob'] = float(p[1])
        elif what == 'released':
            cur['up'] = t
            cur['x1'] = float(p[0])
        elif what == 'toggled' and 'up' in cur and 'settled' not in cur:
            cur['on'] = p[1] == 'True'
        elif what == 'state' and p[0] == 'ToggleStates' and 'up' in cur and 'settled' not in cur:
            cur['states'].append('%s>%s@%.0f' % (p[1], p[3], t - cur['up']))
        elif what == 'settled':
            cur['settled'] = t
            rows.append(cur)
            cur = None
    for r in rows:
        r['held'], r['moved'] = r['up'] - r['down'], r['x1'] - r['x0']
        print('  held %6.0f ms  moved %6.2f  knob %6.2f  %-7s  %s' % (
            r['held'], r['moved'], r['knob'], 'toggled' if r['on'] else '-', ' '.join(r['states'])))
    short = [r for r in rows if not any(s.startswith('Dragging>On') for s in r['states'])]
    still = [r for r in rows if r['moved'] == 0]

    def limit(f, vals, fmt):
        vals = list(vals)
        return fmt % f(vals) if vals else 'none'
    print("  moved: %s toggled, %s didn't" % (
        limit(max, (r['moved'] for r in short if r['on']), '%.2f'),
        limit(min, (r['moved'] for r in short if not r['on'] and r['held'] < 900), '%.2f')))
    print("  held, not moving: %s ms toggled, %s ms didn't" % (
        limit(max, (r['held'] for r in still if r['on']), '%.0f'),
        limit(min, (r['held'] for r in still if not r['on']), '%.0f')))
