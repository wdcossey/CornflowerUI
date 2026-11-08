using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace RefApp
{
    /// <summary>
    /// What a run captures. run.ps1 -Only writes run-options.txt into the package; without it, everything runs.
    /// One setting per line: <c>only=&lt;regex&gt;</c> keeps the specimens and popups whose name matches;
    /// <c>popups=only</c> or <c>popups=also</c> adds the popups, which a run leaves out by default.
    /// </summary>
    internal sealed class Options
    {
        public string Only;

        /// <summary>Capture the popups. Off by default: with them inside a full run, the emulator connection broke
        /// at about 477 PNGs, twice (2026-11-08); on their own they finish.</summary>
        public bool Popups;

        /// <summary>Capture only the popups, not the specimens.</summary>
        public bool PopupsOnly;

        public bool Matches(string name) =>
            string.IsNullOrEmpty(Only) || Regex.IsMatch(name, Only, RegexOptions.IgnoreCase);

        public static async Task<Options> LoadAsync()
        {
            var options = new Options();
            try
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///run-options.txt"));
                foreach (var raw in (await FileIO.ReadTextAsync(file)).Split('\n'))
                {
                    var line = raw.Trim();
                    if (line.StartsWith("only=", StringComparison.Ordinal))
                    {
                        options.Only = line.Substring(5);
                    }
                    else if (line == "popups=also")
                    {
                        options.Popups = true;
                    }
                    else if (line == "popups=only")
                    {
                        options.Popups = true;
                        options.PopupsOnly = true;
                    }
                }
            }
            catch (Exception)
            {
                // No options file: a full run.
            }

            return options;
        }
    }
}
