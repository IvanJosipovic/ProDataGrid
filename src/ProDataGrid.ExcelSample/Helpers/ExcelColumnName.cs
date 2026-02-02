using System;

namespace ProDataGrid.ExcelSample.Helpers;

public static class ExcelColumnName
{
    public static string FromIndex(int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        var dividend = index + 1;
        Span<char> chars = stackalloc char[8];
        var position = chars.Length;

        while (dividend > 0)
        {
            dividend--;
            var remainder = dividend % 26;
            chars[--position] = (char)('A' + remainder);
            dividend /= 26;
        }

        return new string(chars[position..]);
    }

    public static bool TryParseIndex(string? name, out int index)
    {
        index = -1;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var value = 0;
        for (var i = 0; i < name.Length; i++)
        {
            var ch = name[i];
            if (ch == '$')
            {
                continue;
            }

            if (ch < 'A' || ch > 'Z')
            {
                if (ch >= 'a' && ch <= 'z')
                {
                    ch = (char)(ch - 32);
                }
                else
                {
                    return false;
                }
            }

            value = (value * 26) + (ch - 'A' + 1);
        }

        if (value <= 0)
        {
            return false;
        }

        index = value - 1;
        return true;
    }
}
