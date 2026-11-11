# tools

| Path | What it does | Runs on |
|---|---|---|
| [`refapp/`](refapp/README.md) | The Windows Phone 8.1 reference app. `run.ps1` builds it on the reference VM, runs it in the emulator and pulls every capture back | This PC, driving the VM over SSH |
| [`android/`](android/README.md) | The device run: builds the Android sample, installs and starts it on the phone over ADB, and pulls back a frame, the start time and what the device reported | This PC, with the phone on USB or wireless debugging |
| [`winrt-api/`](winrt-api/Program.cs) | `types`: lists the XAML control types in a `Windows.winmd`, and which have a default style in a `generic.xaml`. `checklist <Type>`: one control's spec as a Markdown checklist, from the `.winmd`, `generic.xaml` and `themeresources.xaml`, beside Avalonia's control ([PROCESS.md](../docs/PROCESS.md) §1). `enum <Type>`: an enum's members and values | Anywhere with the .NET SDK, with the SDK files |
| [`lucide/`](lucide/Program.cs) | `generate`: the Lucide icons for `Cornflower.Controls`, from the release pinned in `pin.json`, checked by SHA-256, so the same pin gives the same files. `pin <version>` bumps the pin. `symbols.tsv` maps WinRT's `Symbol` values to Lucide icons | Anywhere with the .NET SDK and the network (or the release's files, with `--zip` and `--licence`) |
| [`breakage/run.js`](../docs/performance/README.md#breakage-runs) | Breaks each behaviour in a list on its own, in a fresh clone, and checks that a test goes red | Anywhere with git, Node and the .NET SDK |
| [`fonts/`](fonts/README.md) | Compares font metrics, weight by weight | Anywhere with Node |
| [`tokens/`](tokens/README.md) | Writes the 8.1 theme's token files and the token tests' expected values from the SDK, and cross-checks the SDK's values against a reference app run | Anywhere with Python 3, with the SDK files and a capture |
| `guard/hash-reference.js` | Writes the hashes of the pulled Microsoft files for the repository guard test (`tests/Cornflower.Tests/Repository/`). Run it whenever more files are pulled | Anywhere with Node, with the reference folder |
| `collect-vm.ps1` | Inventories a reference VM's SDK files, and with `-CopyReferenceFiles` copies them out | The VM. Its output is Microsoft's files: keep it outside every repository |
| `setres.ps1` | Sets the VM's desktop resolution (`-W 2560 -H 1440`) | The VM |

None of these read Microsoft's files from the repository, because none are in it. They read them
from where they are kept, outside it.
