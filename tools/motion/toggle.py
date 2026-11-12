"""The ToggleSwitch's tap limits and the drag's halfway line, from `-Probe toggleswitch`, `toggletap` and
`togglehalf` logs (20261110-0733-dev10 on the 720p profile, 20261109-2128-dev11 on the reference profile; #33's runs
are named in tools/refapp/README.md).

Usage: python -I toggle.py <run folder> [<run folder> ...]

The probes press the switch's knob, Off, hold it for a set time while moving right a set distance, and release.
For each press: how long it was held, how far it moved (window x at release - at press, view px), the knob's
position at release (in the switch's own pixels, which a Viewbox scales on `togglehalf`), when the last pointer event
before the release arrived (ms after the press: the emulator sends a still touch's position every 100 ms), whether
it toggled, the gesture events (Tapped, Holding) and the ToggleStates changes after the release (state>state@ms).
Where the log has the pointer timestamps (`ts`, in microseconds, from #33), the press time and the last event's time
are also given by them. Then the limits: of the presses that didn't drag the knob past halfway (no Dragging>On), the
farthest that still toggled (a tap) and the nearest, held under 900 ms (inside the hold limit), that didn't; of
the presses that didn't move, the longest that still toggled and the shortest that didn't; of the presses that
moved less than 17 px, the latest last event that still toggled and the earliest that didn't; and of the drags
past the tap distance, the farthest knob position that went back Off and the nearest that went On.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml


def stamp(p):
    """The pointer timestamp in a pointer line (after `ts`), in ms, or None."""
    return int(p[p.index('ts') + 1]) / 1000.0 if 'ts' in p else None


runs, _ = ml.parse_args(__doc__, nargs=(1, 99))
for run in runs:
    print('==', run)
    rows, cur = [], None
    for t, what, det in ml.probe(run):
        p = det.split()
        if what in ('ready', 'layout'):
            print('  ', det)
        if what == 'pressed':
            cur = dict(down=t, x0=float(p[0]), ts0=stamp(p), knob=0.0, on=False, states=[], moves=[], gestures=[])
        elif cur is None:
            continue
        elif what == 'value' and p[0] == 'knobX':
            if 'up' not in cur:
                cur['knob'] = float(p[1])
        elif what == 'moved' and 'up' not in cur:
            cur['moves'].append((t - cur['down'], stamp(p)))
        elif what == 'gesture' and 'settled' not in cur:
            cur['gestures'].append('%s@%.0f' % ('/'.join(p[:2]) if p[0] == 'Holding' else p[0], t - cur['down']))
        elif what == 'released':
            cur['up'] = t
            cur['x1'] = float(p[0])
            cur['ts1'] = stamp(p)
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
        r['last'] = r['moves'][-1][0] if r['moves'] else 0.0
        by_ts = ''
        if r['ts0'] is not None and r['ts1'] is not None:
            last_ts = r['moves'][-1][1] - r['ts0'] if r['moves'] and r['moves'][-1][1] is not None else 0.0
            by_ts = '  ts: held %6.1f last %6.1f' % (r['ts1'] - r['ts0'], last_ts)
        print('  held %6.0f ms  moved %6.2f  knob %6.2f  last event %6.1f  %-7s  %s  %s%s' % (
            r['held'], r['moved'], r['knob'], r['last'], 'toggled' if r['on'] else '-', ' '.join(r['gestures']),
            ' '.join(r['states']), by_ts))
    short = [r for r in rows if not any(s.startswith('Dragging>On') for s in r['states'])]
    still = [r for r in rows if r['moved'] == 0]
    near = [r for r in rows if r['moved'] < 17]
    drags = [r for r in rows if r['moved'] > 19 and r['knob'] > 0]

    def limit(f, vals, fmt):
        vals = list(vals)
        return fmt % f(vals) if vals else 'none'
    print("  moved: %s toggled, %s didn't" % (
        limit(max, (r['moved'] for r in short if r['on']), '%.2f'),
        limit(min, (r['moved'] for r in short if not r['on'] and r['held'] < 900), '%.2f')))
    print("  held, not moving: %s ms toggled, %s ms didn't" % (
        limit(max, (r['held'] for r in still if r['on']), '%.0f'),
        limit(min, (r['held'] for r in still if not r['on']), '%.0f')))
    print("  last event, moved under 17: %s ms toggled, %s ms didn't" % (
        limit(max, (r['last'] for r in near if r['on']), '%.1f'),
        limit(min, (r['last'] for r in near if not r['on']), '%.1f')))
    print("  knob at release, drags: %s back Off, %s On" % (
        limit(max, (r['knob'] for r in drags if any(s.startswith('Dragging>Off') for s in r['states'])), '%.3f'),
        limit(min, (r['knob'] for r in drags if any(s.startswith('Dragging>On') for s in r['states'])), '%.3f')))
