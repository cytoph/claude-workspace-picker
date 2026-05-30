using Color = Spectre.Console.Color;

namespace ClaudeWorkspacePicker.Helpers;

static class ColorHelper
{
    public static bool TryParseHexColor(string? hex, out Color color)
    {
        color = Color.Default;

        if (hex is null)
            return false;

        if (!TryParseHexBytes(hex, out byte r, out byte g, out byte b))
            return false;

        color = new Color(r, g, b);

        return true;
    }

    public static bool TryParseHexBytes(string hex, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        string stripped = hex.TrimStart('#');

        if (stripped.Length != 6)
            return false;

        try
        {
            r = Convert.ToByte(stripped[0..2], 16);
            g = Convert.ToByte(stripped[2..4], 16);
            b = Convert.ToByte(stripped[4..6], 16);

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    public static string DarkenHex(string hex, double factor = 0.6)
    {
        if (!TryParseHexBytes(hex, out byte r, out byte g, out byte b))
            return hex;

        return $"#{(byte)(r * factor):x2}{(byte)(g * factor):x2}{(byte)(b * factor):x2}";
    }
}
