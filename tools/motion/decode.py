"""Decodes a run's recording into raw 8-bit gray frames, for the other scripts: <run>/analysis/record.gray.

Usage: python -I decode.py <run folder> [--ffmpeg <path>]

Needs ffmpeg (on PATH, or given). The command is the one the 2026-11-10 runs were decoded with, on the reference VM:
ffmpeg -i record.mkv -vf format=gray -fps_mode passthrough -f rawvideo record.gray
`-fps_mode passthrough` keeps every grabbed frame, one per line of frames.csv.
"""
import os
import shutil
import subprocess
import sys

args = sys.argv[1:]
ffmpeg = 'ffmpeg'
if '--ffmpeg' in args:
    i = args.index('--ffmpeg')
    ffmpeg = args[i + 1]
    del args[i:i + 2]
if len(args) != 1:
    raise SystemExit(__doc__)
run = args[0]
exe = shutil.which(ffmpeg)
if exe is None:
    raise SystemExit('ffmpeg not found; give it with --ffmpeg')
out_dir = os.path.join(run, 'analysis')
os.makedirs(out_dir, exist_ok=True)
out = os.path.join(out_dir, 'record.gray')
subprocess.run([exe, '-hide_banner', '-loglevel', 'error', '-i', os.path.join(run, 'record.mkv'),
                '-vf', 'format=gray', '-fps_mode', 'passthrough', '-f', 'rawvideo', '-y', out], check=True)
with open(os.path.join(run, 'frames.csv')) as f:
    n = sum(1 for l in f if l.strip())
print('%s: %d bytes, %d frames in frames.csv' % (out, os.path.getsize(out), n))
