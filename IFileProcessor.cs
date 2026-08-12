namespace ApplyAML;

/// <summary>
/// Defines a processor that handles a file by its path.
/// </summary>
public interface IFileProcessor
{
    /// <summary>
    /// Processes the specified file.
    /// </summary>
    /// <param name="filePath">The path to the file to process.</param>
    void ProcessFile(string filePath);
}
