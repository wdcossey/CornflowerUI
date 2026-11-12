"""Each run's frames by the app's stopwatch, and the ratio of animation time to the stopwatch they imply.

Usage: python -I clock.py <run folder> [<run folder> ...]

The emulator moves animation time 15.625 ms a frame, however far apart its frames come (`ringsize.py` on
20261111-2316-dev11), so a duration read off the app's stopwatch is short of its animation time by the frames' spacing
over 15.625 ms. This reads that spacing from a probe log: the times of its per-frame readings (`value` and `tree`
lines), a new frame wherever a reading comes more than 3 ms after the reading before it (motionlib.frames); then the
frames' mean spacing, the gaps counted in frames from their own median, a late frame and the early one after it
paired, and stretches with nothing logged left out (motionlib.frame_spacing). Prints the frames logged and counted,
the gaps' median, the mean spacing, and 15.625 over it: the factor that takes a stopwatch duration in that run to
animation time, as a run-wide average.

The factor is an estimate, to about 0.5%: where the probe logs little, frames with nothing logged are counted from
the gaps, not read. On 20261111-2316-dev11, where the storyboard's own time was read each frame, this gives 1.0418
and the storyboard 1.0405 to 1.0416 (`ringsize.py`); on 20261110-0829-dev11, 1.0425, where the ring's cycle against
its keyframes gives 1.042. It is the run's average: one interval's own frames can differ from it by a frame either
way, so a duration converted with it is good to about a frame, 15.6 ms.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import motionlib as ml

runs, _ = ml.parse_args(__doc__, nargs=(1, 99))
for run in runs:
    frames = ml.frames(t for t, what, _ in ml.probe(run) if what in ('value', 'tree') and t > 0)
    name = os.path.basename(os.path.normpath(run))
    if len(frames) < 10:
        print('%s: %d frames logged, too few to read their spacing' % (name, len(frames)))
        continue
    mean, med, counted = ml.frame_spacing(frames)
    print('%s: %d frames logged, %d counted; gaps median %.2f ms; frames %.3f ms apart; animation time / stopwatch '
          '%.4f' % (name, len(frames), counted, med, mean, ml.TICK / mean))
