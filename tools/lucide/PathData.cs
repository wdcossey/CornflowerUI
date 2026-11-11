using System.Globalization;
using System.Text;

namespace IconGenerator;

/// <summary>
/// SVG path data, read with SVG's whole grammar and written again in the small subset that
/// Avalonia's <c>StreamGeometry.Parse</c> reads the way a browser does: absolute <c>M</c>, <c>L</c>,
/// <c>C</c>, <c>Q</c>, <c>A</c> and <c>Z</c>, every number and flag separated by a space.
/// </summary>
/// <remarks>
/// <para>
/// Avonic found where Avalonia 12.1.4's parser and SVG differ (<c>src/Avonic.Components/Controls/Icon/SvgPath.cs</c>
/// in Avonic, branch v2): after a command that isn't a curve, <c>S</c> and <c>T</c> reflect a stale
/// control point; anything but a command after <c>Z</c> loops forever; <c>+1</c> and some exponents are
/// rejected. Avonic patches the text at run time. Here the generator writes data none of that can
/// reach: smooth curves become <c>C</c> and <c>Q</c> with their control point written out, relative
/// commands become absolute, <c>H</c> and <c>V</c> become <c>L</c>, compact arc flags (<c>a2 2 0 012 2</c>,
/// 78 places in Lucide 1.71.0) are spaced, and every command is written with its letter.
/// </para>
/// <para>
/// Numbers are rounded to five decimals, the most any Lucide 1.71.0 file writes, so a sum of
/// relative steps loses only floating-point noise.
/// </para>
/// </remarks>
internal static class PathData
{
    private const int Decimals = 5;

    /// <summary>The data in the subset. Throws <see cref="FormatException"/> where SVG would stop drawing.</summary>
    public static string Normalize(string data)
    {
        var reader = new Reader(data);
        var output = new Writer();
        double x = 0, y = 0, startX = 0, startY = 0;
        double controlX = 0, controlY = 0;
        var previous = '\0';
        var command = '\0';

        while (reader.SkipSeparators())
        {
            if (reader.AtCommand)
            {
                command = reader.ReadCommand();
            }
            else if (command is '\0' or 'Z' or 'z')
            {
                throw new FormatException($"A number where a command was expected, at {reader.Position}: {data}");
            }
            else if (command is 'M')
            {
                command = 'L'; // later pairs of a moveto are linetos
            }
            else if (command is 'm')
            {
                command = 'l';
            }

            if (previous == '\0' && command is not ('M' or 'm'))
            {
                throw new FormatException($"Path data must start with a moveto: {data}");
            }

            var relative = char.IsLower(command);
            double dx = relative ? x : 0, dy = relative ? y : 0;
            switch (char.ToUpperInvariant(command))
            {
                case 'M':
                    x = reader.Number() + dx;
                    y = reader.Number() + dy;
                    startX = x;
                    startY = y;
                    output.Command('M', x, y);
                    break;
                case 'L':
                    x = reader.Number() + dx;
                    y = reader.Number() + dy;
                    output.Command('L', x, y);
                    break;
                case 'H':
                    x = reader.Number() + dx;
                    output.Command('L', x, y);
                    break;
                case 'V':
                    y = reader.Number() + dy;
                    output.Command('L', x, y);
                    break;
                case 'C':
                {
                    double x1 = reader.Number() + dx, y1 = reader.Number() + dy;
                    double x2 = reader.Number() + dx, y2 = reader.Number() + dy;
                    x = reader.Number() + dx;
                    y = reader.Number() + dy;
                    output.Command('C', x1, y1, x2, y2, x, y);
                    (controlX, controlY) = (x2, y2);
                    break;
                }
                case 'S':
                {
                    // The first control point reflects the last one, if the last command was a cubic.
                    var (x1, y1) = char.ToUpperInvariant(previous) is 'C' or 'S' ? (2 * x - controlX, 2 * y - controlY) : (x, y);
                    double x2 = reader.Number() + dx, y2 = reader.Number() + dy;
                    x = reader.Number() + dx;
                    y = reader.Number() + dy;
                    output.Command('C', x1, y1, x2, y2, x, y);
                    (controlX, controlY) = (x2, y2);
                    break;
                }
                case 'Q':
                {
                    double x1 = reader.Number() + dx, y1 = reader.Number() + dy;
                    x = reader.Number() + dx;
                    y = reader.Number() + dy;
                    output.Command('Q', x1, y1, x, y);
                    (controlX, controlY) = (x1, y1);
                    break;
                }
                case 'T':
                {
                    var (x1, y1) = char.ToUpperInvariant(previous) is 'Q' or 'T' ? (2 * x - controlX, 2 * y - controlY) : (x, y);
                    x = reader.Number() + dx;
                    y = reader.Number() + dy;
                    output.Command('Q', x1, y1, x, y);
                    (controlX, controlY) = (x1, y1);
                    break;
                }
                case 'A':
                {
                    // SVG takes a negative radius as its absolute value.
                    double rx = Math.Abs(reader.Number()), ry = Math.Abs(reader.Number()), rotation = reader.Number();
                    double large = reader.Flag(), sweep = reader.Flag();
                    x = reader.Number() + dx;
                    y = reader.Number() + dy;
                    output.Command('A', rx, ry, rotation, large, sweep, x, y);
                    break;
                }
                case 'Z':
                    x = startX;
                    y = startY;
                    output.Command('Z');
                    break;
                default:
                    throw new FormatException($"Unknown command '{command}' in: {data}");
            }

            previous = command;
        }

        if (previous == '\0')
        {
            throw new FormatException("Empty path data.");
        }

        return output.ToString();
    }

    /// <summary>A number as the output writes it: rounded, no trailing zeros, no negative zero.</summary>
    internal static string Format(double value)
    {
        var rounded = Math.Round(value, Decimals, MidpointRounding.AwayFromZero) + 0.0;
        return (rounded == 0 ? 0 : rounded).ToString("0.#####", CultureInfo.InvariantCulture);
    }

    private sealed class Writer
    {
        private readonly StringBuilder _text = new();

        public void Command(char letter, params double[] numbers)
        {
            if (_text.Length > 0)
            {
                _text.Append(' ');
            }

            _text.Append(letter);
            for (var i = 0; i < numbers.Length; i++)
            {
                if (i > 0)
                {
                    _text.Append(' ');
                }

                _text.Append(Format(numbers[i]));
            }
        }

        public override string ToString() => _text.ToString();
    }

    private sealed class Reader(string data)
    {
        public int Position { get; private set; }

        public bool AtCommand => Position < data.Length && "MmLlHhVvCcSsQqTtAaZz".Contains(data[Position]);

        /// <summary>Skips white space and commas. False at the end of the data.</summary>
        public bool SkipSeparators()
        {
            while (Position < data.Length && data[Position] is ' ' or '\t' or '\r' or '\n' or '\f' or ',')
            {
                Position++;
            }

            return Position < data.Length;
        }

        public char ReadCommand() => data[Position++];

        public double Number()
        {
            if (!SkipSeparators())
            {
                throw new FormatException($"The data ends where a number was expected: {data}");
            }

            var start = Position;
            if (data[Position] is '+' or '-')
            {
                Position++;
            }

            var digits = Digits();
            if (Position < data.Length && data[Position] == '.')
            {
                Position++;
                digits += Digits();
            }

            if (digits == 0)
            {
                throw new FormatException($"A number was expected at {start}: {data}");
            }

            if (Position < data.Length && data[Position] is 'e' or 'E')
            {
                var mark = Position;
                Position++;
                if (Position < data.Length && data[Position] is '+' or '-')
                {
                    Position++;
                }

                if (Digits() == 0)
                {
                    Position = mark; // not an exponent
                }
            }

            return double.Parse(data.AsSpan(start, Position - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        /// <summary>An arc flag: one character, 0 or 1, which needn't be followed by a separator.</summary>
        public double Flag()
        {
            if (!SkipSeparators() || data[Position] is not ('0' or '1'))
            {
                throw new FormatException($"An arc flag was expected at {Position}: {data}");
            }

            return data[Position++] - '0';
        }

        private int Digits()
        {
            var start = Position;
            while (Position < data.Length && char.IsAsciiDigit(data[Position]))
            {
                Position++;
            }

            return Position - start;
        }
    }
}
