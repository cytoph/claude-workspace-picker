using System.Text.RegularExpressions;

namespace ClaudeWorkspacePicker.Helpers;

static partial class PathHelper
{
    [GeneratedRegex(@"\$\{([^}]+)\}|\$(\w+)")]
    private static partial Regex UnixEnvironmentVariablePattern();

    public static string ExpandPath(string path)
    {
        if (path.Length >= 1 && path[0] == '~' && (path.Length == 1 || path[1] is '/' or '\\'))
            path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..];

        if (OperatingSystem.IsWindows())
            return Environment.ExpandEnvironmentVariables(path);
        else
            return ExpandUnixEnvironmentVariables(path);
    }

    private static string ExpandUnixEnvironmentVariables(string path) => UnixEnvironmentVariablePattern().Replace(path, match =>
    {
        string name = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        return Environment.GetEnvironmentVariable(name) ?? match.Value;
    });
}
