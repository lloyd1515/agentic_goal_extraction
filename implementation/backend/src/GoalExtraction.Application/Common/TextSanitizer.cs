namespace GoalExtraction.Application.Common;

using System.Security.Cryptography;
using System.Text;

public static class TextSanitizer
{
    public static string SanitizeTranscript(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Strips non-printable ASCII control characters (0-31 except \n, \r, \t) and DEL (127)
        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            if (c < 32)
            {
                if (c == '\n' || c == '\r' || c == '\t')
                {
                    sb.Append(c);
                }
                // Discard other control characters (0-8, 11, 12, 14-31)
            }
            else if (c == 127)
            {
                // DEL character
                continue;
            }
            else
            {
                sb.Append(c);
            }
        }

        // Normalize line endings (\r\n and \r to \n)
        var normalized = sb.ToString().Replace("\r\n", "\n").Replace('\r', '\n');
        return normalized.Trim();
    }

    public static string Sanitize(string? input) => SanitizeTranscript(input);

    public static string ComputeSha256Hash(string? input)
    {
        var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeSha256(string? input) => ComputeSha256Hash(input);
}
