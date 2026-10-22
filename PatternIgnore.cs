using System.IO.Enumeration;

namespace ApplyAML;

/// <summary>
/// An <see cref="IIgnore{T}"/> that keeps only files whose name matches one of the given glob
/// patterns (e.g. "*.xml"); every other file is ignored. Directories are never ignored,
/// so the traversal still descends into them. This mirrors <c>FileExtensionFilter</c>'s shape but
/// matches ApplyAML's <c>--pattern</c> globs rather than a single extension.
/// </summary>
internal sealed class PatternIgnore : IIgnore<string>
{
    private readonly IReadOnlyList<string> _patterns;

    /// <summary>
    /// Initializes a new instance of <see cref="PatternIgnore"/>.
    /// </summary>
    /// <param name="patterns">The glob patterns to keep (e.g. "*.xml").</param>
    public PatternIgnore(IReadOnlyList<string> patterns)
    {
        _patterns = patterns;
    }

    /// <summary>
    /// Returns <c>true</c> for files whose name matches none of the patterns. Returns <c>false</c>
    /// for directories and for files matching at least one pattern.
    /// </summary>
    /// <param name="filePath">The file or directory path to evaluate.</param>
    /// <returns><c>true</c> if the path should be ignored; otherwise, <c>false</c>.</returns>
    public bool CanIgnore(string filePath)
    {
        if (Directory.Exists(filePath))
        {
            return false;
        }

        string name = Path.GetFileName(filePath);
        foreach (string pattern in _patterns)
        {
            if (FileSystemName.MatchesSimpleExpression(pattern, name))
            {
                return false;
            }
        }

        return true;
    }
}
