"""The text styles' lines on the phone, from a refapp run's TextBlock captures (B18: 20261111-2351-dev11, named in
docs/controls/TextBlock/README.md).

Usage: python -I lines.py <run folder>

The catalogue (app/Catalog.cs) has each style three ways: one line, "Agjy 0123 cornflower" (variant <style>); the
same line twice, broken by a LineBreak (<style>-two-lines); and, for the styles that wrap, a sentence wrapped by the
stage's width (<style>-wrapped). For each style, dark theme, it prints, in view pixels:

- the font size and LineHeight (the dump's), and the laid-out heights from the dumps: one line (H1), two lines (H2);
- the two lines' pitch read from the two-line PNG: the distance between the two lines' "A" (the first glyph, the same
  in both lines) by the centre of its ink. It is good to about a raw pixel (0.42 view px): the phone puts each line
  on the pixel grid its own way, so the same glyph's ink differs between the lines;
- the bottom, below the last baseline: for one line H1 less the first baseline (1.0791 em, Segoe WP's ascent, as
  in docs/controls/TextBlock/PLAN.md), for two lines H2 less the first baseline and one LineHeight; beside them the
  two candidates, 0.18869 x LineHeight (Segoe WP's share of descent in a line, the one-line fit in PLAN.md) and
  0.25098 em (Segoe WP's descent, 514/2048);
- for the wrapped specimen, its height and the line count that height gives, 1.0791 em + (n - 1) x LineHeight +
  0.25098 em, solved for n (a whole number when the formula holds).

The two MaxHeight styles (ListViewItemSubheader and ListViewItemContent) have no first-baseline formula; their
lines are printed as heights over the line, max(LineHeight, 1.33 em).

Python 3 standard library only; the PNGs are read with zlib.
"""
import glob
import json
import os
import struct
import sys
import zlib

ASCENT = 2210 / 2048  # Segoe WP's ascent, the phone's first baseline below the top, in ems
DESCENT = 514 / 2048  # Segoe WP's descent, in ems
DESCENT_SHARE = 514 / 2724  # Segoe WP's descent's share of its line
OWN_LINE = 1.33  # Segoe WP's line, in ems


def png(path):
    """(width, height, rows of luminance 0-255) of an 8-bit RGB or RGBA PNG, not interlaced."""
    with open(path, 'rb') as f:
        data = f.read()
    if data[:8] != b'\x89PNG\r\n\x1a\n':
        raise SystemExit(path + ': not a PNG')
    at, idat, w, h, bpp = 8, b'', 0, 0, 0
    while at < len(data):
        n, kind = struct.unpack('>I4s', data[at:at + 8])
        body = data[at + 8:at + 8 + n]
        if kind == b'IHDR':
            w, h, depth, colour, _, _, interlace = struct.unpack('>IIBBBBB', body)
            if depth != 8 or colour not in (2, 6) or interlace:
                raise SystemExit(path + ': only 8-bit RGB or RGBA, not interlaced')
            bpp = 3 if colour == 2 else 4
        elif kind == b'IDAT':
            idat += body
        at += 12 + n
    raw = zlib.decompress(idat)
    stride = w * bpp
    rows, prev = [], bytearray(stride)
    for y in range(h):
        ft = raw[y * (stride + 1)]
        line = bytearray(raw[y * (stride + 1) + 1:(y + 1) * (stride + 1)])
        for i in range(stride):
            a = line[i - bpp] if i >= bpp else 0
            b = prev[i]
            c = prev[i - bpp] if i >= bpp else 0
            if ft == 1:
                line[i] = (line[i] + a) & 255
            elif ft == 2:
                line[i] = (line[i] + b) & 255
            elif ft == 3:
                line[i] = (line[i] + (a + b) // 2) & 255
            elif ft == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[i] = (line[i] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 255
        rows.append([(line[x * bpp] * 299 + line[x * bpp + 1] * 587 + line[x * bpp + 2] * 114) // 1000 for x in range(w)])
        prev = line
    return w, h, rows


def ink(rows, background):
    """Each pixel's ink, 0 to 1, against the background's luminance."""
    return [[abs(v - background) / 255.0 for v in r] for r in rows]


def lines_of(cov, x0=0, x1=None, gap=0.02):
    """The runs of rows with ink between columns x0 and x1, as (first, last) row: one per line, if the lines don't touch."""
    runs, start = [], None
    for y, r in enumerate(cov):
        inked = max(r[x0:x1]) > gap
        if inked and start is None:
            start = y
        elif not inked and start is not None:
            runs.append((start, y - 1))
            start = None
    if start is not None:
        runs.append((start, len(cov) - 1))
    return runs


def first_glyph(cov, gap=0.02):
    """The columns of the first glyph: from the first inked column to the next column with no ink in any row."""
    w = len(cov[0])
    inked = [max(r[x] for r in cov) > gap for x in range(w)]
    x0 = inked.index(True)
    x1 = x0
    while x1 < w and inked[x1]:
        x1 += 1
    return x0, x1


def centre(cov, x0, x1, y0, y1):
    """The ink-weighted centre row of a glyph."""
    weight = [sum(cov[y][x0:x1]) for y in range(y0, y1 + 1)]
    return y0 + sum(i * v for i, v in enumerate(weight)) / sum(weight)


def capture(run, variant):
    """The dark capture's JSON and PNG path, or None."""
    base = os.path.join(run, 'dark__TextBlock__' + variant + '__base')
    if not os.path.exists(base + '.json'):
        return None
    with open(base + '.json', encoding='utf-8') as f:
        return json.load(f), base + '.png'


def main():
    args = sys.argv[1:]
    if len(args) != 1 or args[0].startswith('-'):
        raise SystemExit(__doc__)
    run = args[0]
    styles = sorted(os.path.basename(p)[len('dark__TextBlock__'):-len('-two-lines__base.json')]
                    for p in glob.glob(os.path.join(run, 'dark__TextBlock__*-two-lines__base.json')))
    if not styles:
        raise SystemExit(run + ': no two-line TextBlock captures')
    print('view px; bottom = height - 1.0791 em - (n - 1) x LineHeight')
    print('%-36s %5s %5s | %7s %7s | %6s | %6s %6s | %6s %6s | %s' % (
        'style', 'size', 'LH', 'H1', 'H2', 'A ctr', 'bot 1', 'bot 2', '.189LH', '.251em', 'wrapped: height, n'))
    for style in styles:
        j2, p2 = capture(run, style + '-two-lines')
        one = capture(run, style)
        wrapped = capture(run, style + '-wrapped')
        t = j2['tree']
        fs, lh = t['fontSize'], t['lineHeight']
        h2 = j2['elementInStage'][3]
        h1 = one[0]['elementInStage'][3] if one else float('nan')
        w, _, rows = png(p2)
        scale = w / j2['stage'][0]
        cov = ink(rows, 0)
        x0, x1 = first_glyph(cov)
        runs = lines_of(cov, x0, x1)
        pitch = (centre(cov, x0, x1, *runs[1]) - centre(cov, x0, x1, *runs[0])) / scale if len(runs) == 2 else float('nan')
        out = '%-36s %5.2f %5.2f | %7.3f %7.3f | %6.3f |' % (style, fs, lh, h1, h2, pitch)
        if t['lineStackingStrategy'] == 'BaselineToBaseline':
            first = ASCENT * fs
            out += ' %6.3f %6.3f | %6.3f %6.3f |' % (h1 - first, h2 - first - lh, DESCENT_SHARE * lh, DESCENT * fs)
            if wrapped:
                hw = wrapped[0]['elementInStage'][3]
                out += ' %7.3f, %.3f' % (hw, 1 + (hw - first - DESCENT * fs) / lh)
        else:
            line = max(lh, OWN_LINE * fs)
            out += ' MaxHeight, line %.3f: H1 %.3f lines, H2 %.3f lines' % (line, h1 / line, h2 / line)
        print(out)


main()
