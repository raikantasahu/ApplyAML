using System.Xml.Linq;
using Aras.IOM;

namespace ApplyAML;

/// <summary>
/// The <see cref="IFileProcessor"/> that applies each file to the server: a <c>.sql</c> file is run
/// as SQL, anything else as AML. It counts the files it processes and collects a message for each
/// failure, so the run can print a summary and pick an exit code afterwards.
/// </summary>
internal sealed class AmlApplier : IFileProcessor
{
    private readonly Innovator _innovator;
    private readonly Options _options;
    private readonly List<string> _failures = new();

    public AmlApplier(Innovator innovator, Options options)
    {
        _innovator = innovator;
        _options = options;
    }

    /// <summary>The number of files processed so far.</summary>
    public int Count { get; private set; }

    /// <summary>One "&lt;file&gt;: &lt;reason&gt;" message for each file that failed.</summary>
    public IReadOnlyList<string> Failures => _failures;

    /// <inheritdoc />
    public void ProcessFile(string filePath)
    {
        Count++;
        if (!Apply(filePath, out var error))
        {
            _failures.Add($"{Describe(filePath)}: {error}");
        }
    }

    /// <summary>
    /// Applies one file, returning false and an explanation when it fails. A .sql file is run
    /// as SQL; anything else is applied as AML. The response handling is shared.
    /// </summary>
    private bool Apply(string file, out string error)
    {
        var name = Describe(file);
        var isSql = Path.GetExtension(file).Equals(".sql", StringComparison.OrdinalIgnoreCase);

        string payload;
        try
        {
            payload = isSql ? SqlFile.Load(file) : AmlFile.Load(file);
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or InvalidDataException or IOException)
        {
            error = ex.Message;
            Console.WriteLine($"FAIL {name}");
            return false;
        }

        try
        {
            var result = isSql ? _innovator.applySQL(payload) : _innovator.applyAML(payload);
            var failed = result.isError();

            // The response is printed by default; --brief suppresses it, but a fault is always
            // worth showing regardless.
            if (!_options.Brief || failed)
            {
                PrintResponse(name, result);
            }

            if (failed)
            {
                // getErrorDetail carries the low-level cause (e.g. the SQL error), but often
                // just repeats getErrorString, so only append it when it adds something.
                var message = result.getErrorString()?.Trim() ?? "";
                var detail = result.getErrorDetail()?.Trim() ?? "";
                error = detail.Length == 0 || detail == message
                    ? message
                    : $"{message} ({detail})";
                Console.WriteLine($"FAIL {name}");
                return false;
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
            Console.WriteLine($"FAIL {name}");
            return false;
        }

        Console.WriteLine($"ok   {name}");
        error = "";
        return true;
    }

    /// <summary>Writes the server's AML response, indented for readability.</summary>
    private static void PrintResponse(string name, Item result)
    {
        var xml = result.dom?.OuterXml;
        if (string.IsNullOrEmpty(xml))
        {
            Console.WriteLine($"--- {name}: (empty response)");
            return;
        }

        Console.WriteLine($"--- {name}");
        try
        {
            Console.WriteLine(XDocument.Parse(xml).ToString());
        }
        catch (System.Xml.XmlException)
        {
            // Never let a formatting problem hide the response the user asked to see.
            Console.WriteLine(xml);
        }

        Console.WriteLine("---");
    }

    /// <summary>Shortens a path to something readable relative to the input the user gave.</summary>
    private string Describe(string file)
    {
        if (!Directory.Exists(_options.Input))
        {
            return Path.GetFileName(file);
        }

        var root = Path.GetFullPath(_options.Input);
        return Path.GetRelativePath(root, file);
    }
}
