using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace RefApp
{
    /// <summary>
    /// What a run captures. run.ps1 -Only writes run-options.txt into the package; without it, everything runs.
    /// One setting per line: <c>only=&lt;regex&gt;</c> keeps the specimens and popups whose name matches;
    /// <c>popups=only</c> or <c>popups=also</c> adds the popups, which a run leaves out by default;
    /// <c>demo</c> shows the demo page instead of running the suite; <c>hold=&lt;ms&gt;</c> holds each popup open,
    /// full window, for the screen grabs (run.ps1 -Grab); <c>probe=&lt;name&gt;</c> runs one <see cref="Probe"/> instead
    /// of the suite (run.ps1 -Probe).
    /// </summary>
    internal sealed class Options
    {
        public string Only;

        /// <summary>Capture the popups. Off by default: with them inside a full run, the emulator connection broke
        /// at about 477 PNGs, twice (2026-11-08); on their own they finish.</summary>
        public bool Popups;

        /// <summary>Capture only the popups, not the specimens.</summary>
        public bool PopupsOnly;

        /// <summary>Show <see cref="Demo"/>, a page to use by hand, and capture nothing.</summary>
        public bool Demo;

        /// <summary>How long each popup stays open after it is dumped, for vm\grab-loop.ps1 to grab the screen. 0: no hold.</summary>
        public int HoldMs;

        /// <summary>The <see cref="Probe"/> to run instead of the suite, or null.</summary>
        public string Probe;

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
                    else if (line.StartsWith("hold=", StringComparison.Ordinal))
                    {
                        options.HoldMs = int.Parse(line.Substring(5), System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (line.StartsWith("probe=", StringComparison.Ordinal))
                    {
                        options.Probe = line.Substring(6);
                    }
                    else if (line == "demo")
                    {
                        options.Demo = true;
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
