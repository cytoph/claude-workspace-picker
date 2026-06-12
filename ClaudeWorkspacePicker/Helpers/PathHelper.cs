namespace ClaudeWorkspacePicker.Helpers;

static class PathHelper
{
    public static string ExpandPath(string path)
    {
        if (path.Length >= 1 && path[0] == '~' && (path.Length == 1 || path[1] is '/' or '\\'))
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..];

        return Environment.ExpandEnvironmentVariables(path);
    }
}
