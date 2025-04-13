using System.Text.RegularExpressions;

namespace AICodeReviewerCLI;

public class FileIgnoreService
{
    private readonly List<string> _patterns;

    public FileIgnoreService()
    {
        _patterns = File.Exists(".ignore")
            ? File.ReadAllLines(".ignore").Where(line => !string.IsNullOrWhiteSpace(line)).ToList()
            : [];
    }

    public bool IsIgnored(string filePath)
    {
        return _patterns.Any(pattern =>
        {
            if (pattern.EndsWith("/")) return filePath.StartsWith(pattern);
            return Regex.IsMatch(filePath, WildcardToRegex(pattern));
        });
    }

    private string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
    }
}