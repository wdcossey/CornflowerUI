# tools

| Path | What it does | Runs on |
|---|---|---|
| [`refapp/`](refapp/README.md) | The Windows Phone 8.1 reference app. `run.ps1` builds it on the reference VM, runs it in the emulator and pulls every capture back | This PC, driving the VM over SSH |
| [`winrt-api/`](winrt-api/Program.cs) | Lists the XAML control types in a `Windows.winmd`, and which have a default style in a `generic.xaml` | Anywhere with the .NET SDK |
| [`fonts/`](fonts/README.md) | Compares font metrics, weight by weight | Anywhere with Node |
| `guard/hash-reference.js` | Writes the hashes of the pulled Microsoft files for the repository guard test (`tests/Cornflower.Tests/Repository/`). Run it whenever more files are pulled | Anywhere with Node, with the reference folder |
| `collect-vm.ps1` | Inventories a reference VM's SDK files, and with `-CopyReferenceFiles` copies them out | The VM. Its output is Microsoft's files: keep it outside every repository |
| `setres.ps1` | Sets the VM's desktop resolution (`-W 2560 -H 1440`) | The VM |

None of these read Microsoft's files from the repository, because none are in it. They read them
from where they are kept, outside it.
