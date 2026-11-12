"""Each run's frames by the app's stopwatch, and the ratio of animation time to the stopwatch they imply.

Usage: python -I clock.py <run folder> [<run folder> ...]

The emulator moves animation time 15.625 ms a frame, however far apart its frames come (`ringsize.py` on
20261111-2316-dev11), so a duration read off the app's stopwatch is short of its animation time by the frames' spacing over
15.625 ms. This reads that spacing from a probe log: the times of its per-frame readings (`value` and `tree` lines; a
line more than 3 ms after the one before starts a new frame), and the gaps between consecutive frames under 25 ms,
longer ones being frames with nothing logged between. Prints the frames, the gaps' median and mean, and 15.625 over
the mean: the factor that takes a stopwatch duration in that run to animation time.

The factor is an estimate, to about 1%: a frame can move animation time with no reading logged, so the logged frames
are not exactly the clock's. On 20261111-2316-dev11, where the storyboard's own time was read, this gives 1.042 and
the storyboard's 1.0405 to 1.0416 (`ringsize.py`); on 20261110-0829-dev11, 1.044 where the ring's cycle against its
keyframes gives 1.042. A log with few consecutive frames (a probe that reads little each frame) gives a rougher one;
the count is printed.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

runs, _ = ml.parse_args(__doc__, nargs=(1, 99))
for run in runs:
    times = sorted({t for t, what, _ in ml.probe(run) if what in ('value', 'tree') and t > 0})
    frames = []
    for t in times:
        if not frames or t - frames[-1] > 3:
            frames.append(t)
    gaps = [b - a for a, b in zip(frames, frames[1:]) if b - a < 25]
    name = os.path.basename(os.path.normpath(run))
    if len(gaps) < 2:
        print('%s: %d frames logged, too few consecutive to read their spacing' % (name, len(frames)))
        continue
    mean = sum(gaps) / len(gaps)
    print('%s: %d frames logged, %d consecutive gaps: median %.2f ms, mean %.3f; animation time / stopwatch %.3f' % (
        name, len(frames), len(gaps), ml.median(gaps), mean, ml.TICK / mean))
