namespace ApplyAML;

/// <summary>
/// Traverses a directory and processes each file using an <see cref="IFileProcessor"/>,
/// skipping files that the <see cref="IIgnore{T}"/> strategy determines should be ignored.
/// </summary>
public class DirectoryTraversor
{
	private readonly IFileProcessor _fileProcessor;
	private readonly IIgnore<string> _fileIgnore;
	private readonly IIgnore<string> _directoryIgnore;
	private readonly bool _recurse;
	private readonly bool _depthFirst;

	/// <summary>
	/// Initializes a new instance of <see cref="DirectoryTraversor"/>.
	/// </summary>
	/// <param name="fileProcessor">The processor to invoke for each non-ignored file.</param>
	/// <param name="fileIgnore">The strategy that determines which files to skip. Defaults to <see cref="NeverIgnore{T}"/>.</param>
	/// <param name="directoryIgnore">The strategy that determines which directories to skip. Defaults to <see cref="NeverIgnore{T}"/>.</param>
	/// <param name="recurse">Whether to recurse into subdirectories. Defaults to <c>true</c>.</param>
	/// <param name="depthFirst">Whether to traverse subdirectories before processing files. Defaults to <c>false</c>.</param>
	public DirectoryTraversor(IFileProcessor fileProcessor, IIgnore<string>? fileIgnore = null,
		IIgnore<string>? directoryIgnore = null, bool recurse = true, bool depthFirst = false)
	{
		_fileProcessor = fileProcessor;
		_fileIgnore = fileIgnore ?? NeverIgnore<string>.Instance;
		_directoryIgnore = directoryIgnore ?? NeverIgnore<string>.Instance;
		_recurse = recurse;
		_depthFirst = depthFirst;
	}

	/// <summary>
	/// Traverses the specified directory and processes each file.
	/// </summary>
	/// <param name="directoryPath">The path to the directory to traverse.</param>
	/// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
	public void Traverse(string directoryPath)
	{
		if (!Directory.Exists(directoryPath))
		{
			throw new DirectoryNotFoundException(
				$"Directory not found: '{directoryPath}'.");
		}

		TraverseInternal(directoryPath);
	}

	private void TraverseInternal(string directoryPath)
	{
		if (_directoryIgnore.CanIgnore(directoryPath))
		{
			return;
		}

		if (_depthFirst)
		{
			TraverseSubDirectories(directoryPath);
			ProcessFiles(directoryPath);
		}
		else
		{
			ProcessFiles(directoryPath);
			TraverseSubDirectories(directoryPath);
		}
	}

	private void ProcessFiles(string directoryPath)
	{
		// Sorted by file name (ordinal) so processing order is deterministic rather than raw
		// filesystem order.
		foreach (string filePath in Directory.GetFiles(directoryPath).OrderBy(Path.GetFileName, StringComparer.Ordinal))
		{
			if (!_fileIgnore.CanIgnore(filePath))
			{
				_fileProcessor.ProcessFile(filePath);
			}
		}
	}

	private void TraverseSubDirectories(string directoryPath)
	{
		if (_recurse)
		{
			foreach (string subdirectory in Directory.GetDirectories(directoryPath).OrderBy(Path.GetFileName, StringComparer.Ordinal))
			{
				TraverseInternal(subdirectory);
			}
		}
	}
}
