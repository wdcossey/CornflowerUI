// Generates the icon set from a pinned Lucide release (lucide.dev, ISC; its Feather-derived icons MIT).
// Needs only the .NET SDK, and the network unless --zip and --licence are given.
//
//   dotnet run --project tools/lucide -- generate [--zip <icons.zip>] [--licence <LICENSE>] [--out <folder>]
//                                                 [--namespace <ns>] [--enum <name>] [--data <file name>]
//       Downloads the release in tools/lucide/pin.json (or reads the given files), checks each file's
//       SHA-256 against the pin, and writes into --out (src/Cornflower.Controls/Icons by default):
//         <enum>.g.cs   the icons as an enum, by Lucide's PascalCase names (LucideIconKind)
//         <data>        one "name<TAB>path data" line per icon, Brotli-compressed (lucide.br)
//         LICENSE       Lucide's licence, verbatim
//       The same pin gives the same files. A Lucide icon outside its convention (24-unit box, a stroke
//       2 wide with round caps and joins, no fill) stops it with the icon's name.
//
//   dotnet run --project tools/lucide -- pin <version>
//       Points pin.json at another release: downloads its zip and licence and records their SHA-256.
//       To bump Lucide: run this, then generate, then build and test. Tests that name icons (the
//       Symbol table, tools/lucide/symbols.tsv) fail if the release renamed or dropped one.
using IconGenerator;

const string Usage = """
    usage:
      generate [--zip <icons.zip>] [--licence <LICENSE>] [--out <folder>] [--namespace <ns>] [--enum <name>] [--data <file name>]
      pin <version>
    """;

// The repository is the folder above the working directory, or the tool's build, that holds tools/lucide/pin.json.
var repo = FindRepo(Directory.GetCurrentDirectory()) ?? FindRepo(AppContext.BaseDirectory);
if (repo is null)
{
    Console.Error.WriteLine("tools/lucide/pin.json not found above the working directory: run from the repository.");
    return 1;
}

var pinPath = Path.Combine(repo, "tools", "lucide", "pin.json");

try
{
    switch (args)
    {
        case ["generate", .. var rest]:
        {
            var options = Options(rest);
            var pin = Pin.Load(pinPath);
            using var http = new HttpClient();
            var zip = options.TryGetValue("--zip", out var zipPath) ? File.ReadAllBytes(zipPath) : await Pin.FetchAsync(http, pin.Icons);
            var licence = options.TryGetValue("--licence", out var licencePath) ? File.ReadAllBytes(licencePath) : await Pin.FetchAsync(http, pin.Licence);
            Pin.Verify(pin.Icons, zip);
            Pin.Verify(pin.Licence, licence);

            var output = new Output(
                options.GetValueOrDefault("--out") ?? Path.Combine(repo, "src", "Cornflower.Controls", "Icons"),
                options.GetValueOrDefault("--namespace") ?? "Cornflower.Controls",
                options.GetValueOrDefault("--enum") ?? "LucideIconKind",
                options.GetValueOrDefault("--data") ?? "lucide.br");
            var release = Generator.Read(pin.Version, new MemoryStream(zip), System.Text.Encoding.UTF8.GetString(licence));
            var files = Generator.Generate(release, output);
            Generator.Write(files, output);
            Console.WriteLine($"Lucide {pin.Version}: {release.Icons.Count} icons into {output.Folder}: "
                + string.Join(", ", files.Select(file => $"{file.Key} {file.Value.Length:N0} bytes")));
            return 0;
        }

        case ["pin", var version]:
        {
            using var http = new HttpClient();
            var (iconsUrl, licenceUrl) = Pin.UrlsFor(version);
            var zip = await http.GetByteArrayAsync(iconsUrl);
            var licence = await http.GetByteArrayAsync(licenceUrl);
            var pin = new Pin(version, new PinnedFile(iconsUrl, Pin.Sha256(zip)), new PinnedFile(licenceUrl, Pin.Sha256(licence)));
            pin.Save(pinPath);
            Console.WriteLine($"Pinned Lucide {version} in {pinPath}. Now run generate.");
            return 0;
        }

        default:
            Console.Error.WriteLine(Usage);
            return 1;
    }
}
catch (Exception e) when (e is FormatException or InvalidDataException or IOException or HttpRequestException or System.Text.Json.JsonException)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}

static Dictionary<string, string> Options(string[] rest)
{
    var options = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var i = 0; i < rest.Length; i++)
    {
        if (!rest[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= rest.Length)
        {
            throw new FormatException($"Unexpected argument {rest[i]}.");
        }

        options[rest[i]] = rest[++i];
    }

    return options;
}

static string? FindRepo(string start)
{
    for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
    {
        if (File.Exists(Path.Combine(folder.FullName, "tools", "lucide", "pin.json")))
        {
            return folder.FullName;
        }
    }

    return null;
}
