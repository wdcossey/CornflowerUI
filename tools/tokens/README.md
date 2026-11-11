# tokens: the token generator and its cross-check

Python scripts that write the Windows Phone 8.1 theme's token files from the SDK, and check the SDK's
values against what the phone resolved in a [reference app](../refapp/README.md) run. Run them again
when a new capture is made, or to see what a change to the scripts does to the token files.

| Script | Does |
|---|---|
| `crosscheck.py` | For every capture JSON of a run, follows each dumped property of each template part back to the theme resource key the default style sets it from, in the captured state, and compares the phone's value with the SDK file's. Also compares the resources in the run's `index.json`. Prints every key where they differ and writes all the comparisons to a JSON file |
| `gen.py` | Writes the token files and the token tests' expected values from the SDK, naming beside each token the capture of the run that confirms it |
| `resources.py` | Reads `themeresources.xaml` into dictionaries and resolves a brush to its colour. Used by the two above |
| `summarize.py` | Counts the entries of a `themeresources.xaml` by element type, per theme dictionary |

## What they read and write

They read Microsoft's files at run time, from where they are kept outside this repository, and
write only Cornflower's files. No Microsoft file or content is stored here (rule 6 in
[CLAUDE.md](../../CLAUDE.md)).

| Reads | From |
|---|---|
| `themeresources.xaml`, `generic.xaml` | The 8.1 SDK's `Windows Phone Kits\8.1\Include\abi\Xaml\Design` folder (`--design`) |
| `<Name>.xaml`, the 20 named accents, each one `PhoneAccentColor`, checked against the phone's palette in `resources.py` | The 8.1 SDK's `Microsoft SDKs\Windows Phone\v8.1\Design\AccentColors` folder (`--accents`) |
| The capture JSONs and `index.json` | A refapp run folder (`--capture`) |

| Writes | Who |
|---|---|
| `out/crosscheck.json`, or the file given as `--out`. `out/` is ignored by git | `crosscheck.py` |
| `src/Cornflower.Themes.Wp81/Tokens/Colors.axaml`, `Accent.axaml`, `Typography.axaml`, `Sizes.axaml` | `gen.py` |
| `tests/Cornflower.Tests/Tokens/TokenExpectations.cs` | `gen.py` |

`gen.py` finds the repository root from its own folder (`tools/tokens/../..`). `Tokens/Fonts.axaml`
is not one of its files.

## Running them

Python 3, standard library only (run with 3.13.7). From the repository root, with the paths on the
maintainer's machine as the example:

```sh
DESIGN="R:/cornflower-reference/inventory-20261107/reference/Program Files (x86)/Windows Phone Kits/8.1/Include/abi/Xaml/Design"
ACCENTS="R:/cornflower-reference/vm-extras/Microsoft SDKs/Windows Phone/v8.1/Design/AccentColors"
CAPTURE="R:/cornflower-reference/refapp/20261108-1443-dev11"

python -I tools/tokens/crosscheck.py --design "$DESIGN" --capture "$CAPTURE"
python -I tools/tokens/gen.py --design "$DESIGN" --accents "$ACCENTS"
python -I tools/tokens/summarize.py "$DESIGN/themeresources.xaml"
```

The three folders have no defaults: they are outside the repository, so each run names them.
`gen.py` reads `out/crosscheck.json` unless given `--crosscheck <file>`, and takes the run's name
from it. It writes tokens only from a run on device 11, the reference profile
([ADR 0001](../../docs/decisions/0001-what-faithful-means.md)); `crosscheck.py` takes a run on any
device.

Then `git diff`. With run `20261108-1443-dev11` the five files come out byte for byte as committed.
`gen.py` writes LF line endings, so on a Windows checkout `git status` can list the files as modified
while `git diff` shows nothing.

A run that disagrees with the SDK file on a key gets no `run:` note for that key. `gen.py` lists any
such key that isn't one of the known differences, `DIFFER` in `gen.py`: the two that run
`20261108-1443-dev11` showed, `SliderBorderThemeThickness` and `ProgressRingActiveBackgroundOpacity`,
explained in PR #45. A new one means a look at the capture and either an entry there or a fix.

## Values held in the scripts

The scripts hold no table read from the SDK. The few values written into them:

| Value | Where | Source |
|---|---|---|
| The phone's 20 accents: registry index, name, colour | `resources.py` `PHONE_ACCENTS` | The 8.1 Update 1 image's registry (`config\SOFTWARE`, `Microsoft\Windows\CurrentVersion\Control Panel\Theme`): `Themes\{0,1}\Accents\<i>` `Color` (the same in both themes), named through `AccentDisplayNames\<i>` by `UIXMobileRes.dll.mui` strings 10017-10026 and 10050-10059. The RTM image's is the same. `gen.py` writes the named accents from it |
| `SystemColorControlAccentColor` default, `DefaultAccent` 5 (Cobalt, `#FF3E65FF`) | `resources.py` `DEFAULT_ACCENT_INDEX` | The same key's `DefaultAccent`. The emulator's `PhoneAccentBrush`, read by refapp run `20261108-1443-dev11` (`index.json`), is the same. Chosen in [ADR 0002](../../docs/decisions/0002-decisions-of-2026-11-08.md). The SDK file doesn't define this key: the phone supplies it |
| Where the SDK's accent files differ: Cobalt, 8.1 `#FF0B6CF8`, 8.0 `#FF0050EF` | `resources.py` `SDK_ACCENT_DIFFERS` | The SDK's `AccentColors\Cobalt.xaml` (8.0 compared in PR #45). `gen.py` stops on a difference not listed, or a listed one that no longer holds |
| `Transparent`, `White`, `Black` as `#AARRGGBB` | `resources.py` `NAMED` | The XAML named colours |
| Font weight names as numbers (`Light` 300, `SemiLight` 350, `Semibold` 600 and so on) | `crosscheck.py` `WEIGHTS`, `gen.py` `WNUM` | The OpenType weight classes |
| The two known differences and their reasons | `gen.py` `DIFFER` | Cornflower's reading of run `20261108-1443-dev11`, PR #45 |

The keys the scripts name (`PhoneAccentBrush`, `SliderBorderThemeThickness` and so on) are WinRT API
names.
