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
//
//   dotnet run --project tools/winrt-api -- enum <Type> <Windows.winmd>
//       An enum's members, one "Name<TAB>Value" line each, in the metadata's order. <Type> is a full
//       name, or a name under Windows.UI.Xaml.Controls (Symbol).
using WinRtApi;

const string Usage = """
    usage:
      types <Windows.winmd> [generic.xaml]
      checklist <Type> <Windows.winmd> <design folder> [--stop <Type>] [--tokens <folder>]
      enum <Type> <Windows.winmd>
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
            var parsed = ChecklistCommand.Options(options);
            Console.Write(ChecklistCommand.Run(type, winmd, design, parsed.GetValueOrDefault("--stop"), parsed.GetValueOrDefault("--tokens")));
            return 0;
        }
        catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException or System.Xml.XmlException or BadImageFormatException)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }

    case ["enum", var type, var winmd]:
        try
        {
            using var api = new ApiReader(winmd);
            foreach (var (name, value) in api.EnumMembers(type))
            {
                Console.WriteLine($"{name}\t{value}");
            }
            return 0;
        }
        catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException or BadImageFormatException)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }

    default:
        Console.Error.WriteLine(Usage);
        return 1;
}

