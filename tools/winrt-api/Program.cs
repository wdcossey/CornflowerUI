// Reads the Windows Phone 8.1 SDK's spec files as data only: the API metadata (Windows.winmd) and the
// default templates and theme resources (generic.xaml, themeresources.xaml). They stay outside the
// repo (ADR 0002). Needs the .NET SDK; System.Reflection.Metadata ships in it.
//
//   dotnet run --project tools/winrt-api -- types <Windows.winmd> [generic.xaml]
//       Every XAML control type, with its base and whether generic.xaml styles it, as Markdown.
//       (The first argument may also be the .winmd itself, as before.)
//
//   dotnet run --project tools/winrt-api -- checklist <Type> <Windows.winmd> <design folder> [--stop <Type>] [--tokens <folder>]
//       One control's checklist for its plan (docs/PROCESS.md): its API from the .winmd, down its base
//       types to Control (or --stop); its default style, parts, states, transitions and resources from
//       generic.xaml and themeresources.xaml in <design folder>; each beside Avalonia's equivalent. <Type>
//       is a full name, or a name under Windows.UI.Xaml.Controls or .Primitives. --tokens is the theme's
//       Tokens folder, found from the repo by default.
using WinRtApi;

const string Usage = """
    usage:
      types <Windows.winmd> [generic.xaml]
      checklist <Type> <Windows.winmd> <design folder> [--stop <Type>] [--tokens <folder>]
    """;

Console.OutputEncoding = System.Text.Encoding.UTF8;

switch (args)
{
    case ["types", var winmd, .. var rest]:
        Types.Print(winmd, rest.FirstOrDefault());
        return 0;
    case [var winmd, .. var rest] when winmd.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase):
        Types.Print(winmd, rest.FirstOrDefault());
        return 0;
    case ["checklist", var type, var winmd, var design, .. var options]:
        try
        {
            Console.Write(ChecklistCommand.Run(type, winmd, design, Option(options, "--stop"), Option(options, "--tokens")));
            return 0;
        }
        catch (ArgumentException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }

    default:
        Console.Error.WriteLine(Usage);
        return 1;
}

static string? Option(string[] options, string name)
{
    var index = Array.IndexOf(options, name);
    return index >= 0 && index + 1 < options.Length ? options[index + 1] : null;
}
