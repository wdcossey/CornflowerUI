using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IconGenerator;

/// <summary>One file the generator downloads, and the SHA-256 it must have.</summary>
internal sealed record PinnedFile(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("sha256")] string Sha256);

/// <summary>
/// The Lucide release the generator reads: its version, its icons' zip and its licence, each with a
/// hash, so the same pin downloads the same bytes or fails. Kept in <c>tools/lucide/pin.json</c>.
/// </summary>
internal sealed record Pin(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("icons")] PinnedFile Icons,
    [property: JsonPropertyName("licence")] PinnedFile Licence)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>Where a release's files are published: the icons zip on the GitHub release, the licence at its tag.</summary>
    public static (string Icons, string Licence) UrlsFor(string version) => (
        $"https://github.com/lucide-icons/lucide/releases/download/{version}/lucide-icons-{version}.zip",
        $"https://raw.githubusercontent.com/lucide-icons/lucide/{version}/LICENSE");

    public static Pin Load(string path) =>
        JsonSerializer.Deserialize<Pin>(File.ReadAllText(path)) ?? throw new FormatException($"{path} is empty.");

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Options) + "\n");

    /// <summary>The file's bytes, if their SHA-256 is the pinned one.</summary>
    public static async Task<byte[]> FetchAsync(HttpClient http, PinnedFile file)
    {
        var bytes = await http.GetByteArrayAsync(file.Url);
        Verify(file, bytes);
        return bytes;
    }

    public static void Verify(PinnedFile file, byte[] bytes)
    {
        var actual = Sha256(bytes);
        if (!string.Equals(actual, file.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"{file.Url} has SHA-256 {actual}, not the pinned {file.Sha256}.");
        }
    }

    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
