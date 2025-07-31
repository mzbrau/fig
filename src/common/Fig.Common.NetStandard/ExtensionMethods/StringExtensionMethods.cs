using System;
using System.Collections.Generic;
using System.Globalization;

namespace Fig.Common.NetStandard.ExtensionMethods;

public static class StringExtensionMethods
{
    private static readonly HashSet<string> CssNamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "black", "silver", "gray", "white", "maroon", "red", "purple", "fuchsia",
        "green", "lime", "olive", "yellow", "navy", "blue", "teal", "aqua",
        // Add full CSS color list if needed
        "orange", "aliceblue", "antiquewhite", "aquamarine", "azure",
        "beige", "bisque", "blanchedalmond", "blueviolet", "brown",
        "burlywood", "cadetblue", "chartreuse", "chocolate", "coral",
        "cornflowerblue", "cornsilk", "crimson", "cyan", "darkblue",
        "darkcyan", "darkgoldenrod", "darkgray", "darkgreen", "darkgrey",
        "darkkhaki", "darkmagenta", "darkolivegreen", "darkorange", "darkorchid",
        "darkred", "darksalmon", "darkseagreen", "darkslateblue", "darkslategray",
        "darkslategrey", "darkturquoise", "darkviolet", "deeppink", "deepskyblue",
        "dimgray", "dimgrey", "dodgerblue", "firebrick", "floralwhite",
        "forestgreen", "gainsboro", "ghostwhite", "gold", "goldenrod",
        "greenyellow", "honeydew", "hotpink", "indianred", "indigo",
        "ivory", "khaki", "lavender", "lavenderblush", "lawngreen",
        "lemonchiffon", "lightblue", "lightcoral", "lightcyan", "lightgoldenrodyellow",
        "lightgray", "lightgreen", "lightgrey", "lightpink", "lightsalmon",
        "lightseagreen", "lightskyblue", "lightslategray", "lightslategrey", "lightsteelblue",
        "lightyellow", "limegreen", "linen", "magenta", "mediumaquamarine",
        "mediumblue", "mediumorchid", "mediumpurple", "mediumseagreen", "mediumslateblue",
        "mediumspringgreen", "mediumturquoise", "mediumvioletred", "midnightblue", "mintcream",
        "mistyrose", "moccasin", "navajowhite", "oldlace", "orangered",
        "orchid", "palegoldenrod", "palegreen", "paleturquoise", "palevioletred",
        "papayawhip", "peachpuff", "peru", "pink", "plum",
        "powderblue", "rosybrown", "royalblue", "saddlebrown", "salmon",
        "sandybrown", "seagreen", "seashell", "sienna", "skyblue",
        "slateblue", "slategray", "slategrey", "snow", "springgreen",
        "steelblue", "tan", "thistle", "tomato", "turquoise",
        "violet", "wheat", "whitesmoke", "yellowgreen", "rebeccapurple"
    };

    public static bool IsValidCssColor(this string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        color = color.Trim();

        // Hex: #RGB, #RRGGBB, #RGBA, #RRGGBBAA
        if (color.StartsWith("#"))
        {
            string hex = color.Substring(1);
            return hex.Length == 3 || hex.Length == 4 || hex.Length == 6 || hex.Length == 8
                && IsHexDigits(hex);
        }

        // Named colors
        if (CssNamedColors.Contains(color))
            return true;

        // rgb(), rgba()
        if (color.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase))
            return ParseRgb(color, alpha: false);

        if (color.StartsWith("rgba(", StringComparison.OrdinalIgnoreCase))
            return ParseRgb(color, alpha: true);

        return false;
    }

    private static bool IsHexDigits(string hex)
    {
        foreach (char c in hex)
        {
            if (!Uri.IsHexDigit(c))
                return false;
        }
        return true;
    }

    private static bool ParseRgb(string input, bool alpha)
    {
        int open = input.IndexOf('(');
        int close = input.IndexOf(')', open + 1);
        if (open < 0 || close < 0 || close <= open)
            return false;

        string[] parts = input.Substring(open + 1, close - open - 1).Split(',');

        if (parts.Length != (alpha ? 4 : 3))
            return false;

        // Validate R, G, B
        for (int i = 0; i < 3; i++)
        {
            if (!int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int val) ||
                val < 0 || val > 255)
            {
                return false;
            }
        }

        // Validate Alpha
        if (alpha)
        {
            string a = parts[3].Trim();
            if (!float.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ||
                f < 0 || f > 1)
            {
                return false;
            }
        }

        return true;
    }
}