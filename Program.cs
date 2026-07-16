using System.Xml.Linq;
using Aras.IOM;

namespace ApplyAML;

internal static class Program
{
    private const int ExitSuccess = 0;
    private const int ExitFileFailures = 1;
    private const int ExitUsage = 2;

    private static int Main(string[] args)
    {
        Options options;
        try
        {
            options = Options.Parse(args);
            if (options.ShowHelp)
            {
                Console.WriteLine(Options.Usage);
                return ExitSuccess;
            }

            options.EnsurePassword();
        }
        catch (OptionsException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            Console.Error.WriteLine();
            Console.Error.WriteLine(Options.Usage);
            return ExitUsage;
        }

        var files = AmlFile.Discover(options.Input, options.Pattern);
        if (files.Count == 0)
        {
            Console.Error.WriteLine($"error: no files matching '{options.Pattern}' under {options.Input}");
            return ExitUsage;
        }

        Console.WriteLine($"Found {files.Count} file(s) to apply.");

        HttpServerConnection connection;
        Innovator innovator;
        try
        {
            connection = IomFactory.CreateHttpServerConnection(
                options.Url, options.Database, options.User, options.Password);

            var login = connection.Login();
            if (login.isError())
            {
                Console.Error.WriteLine($"error: login failed: {login.getErrorString()}");
                return ExitUsage;
            }

            innovator = IomFactory.CreateInnovator(connection);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: could not connect to {options.Url}: {ex.Message}");
            return ExitUsage;
        }

        Console.WriteLine($"Connected to {options.Url} [{options.Database}] as {options.User}.");

        var failures = new List<string>();
        try
        {
            foreach (var file in files)
            {
                if (!Apply(innovator, file, options, out var error))
                {
                    failures.Add($"{Describe(file, options.Input)}: {error}");
                }
            }
        }
        finally
        {
            TryLogout(connection);
        }

        return Report(files.Count, failures);
    }

    /// <summary>Applies one file, returning false and an explanation when it fails.</summary>
    private static bool Apply(Innovator innovator, string file, Options options, out string error)
    {
        var name = Describe(file, options.Input);

        string aml;
        try
        {
            aml = AmlFile.Load(file);
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or InvalidDataException or IOException)
        {
            error = ex.Message;
            Console.WriteLine($"FAIL {name}");
            return false;
        }

        try
        {
            var result = innovator.applyAML(aml);
            var failed = result.isError();

            // A fault is always worth showing; a successful response only when asked for.
            if (options.PrintResults || failed)
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
    private static string Describe(string file, string input)
    {
        if (!Directory.Exists(input))
        {
            return Path.GetFileName(file);
        }

        var root = Path.GetFullPath(input);
        return Path.GetRelativePath(root, file);
    }

    private static void TryLogout(HttpServerConnection connection)
    {
        try
        {
            connection.Logout();
        }
        catch (Exception ex)
        {
            // The work is already done at this point, so a failed logout is not worth failing over.
            Console.Error.WriteLine($"warning: logout failed: {ex.Message}");
        }
    }

    private static int Report(int total, IReadOnlyList<string> failures)
    {
        Console.WriteLine();
        if (failures.Count == 0)
        {
            Console.WriteLine($"All {total} file(s) applied successfully.");
            return ExitSuccess;
        }

        Console.WriteLine($"{total - failures.Count} of {total} file(s) applied; {failures.Count} failed:");
        foreach (var failure in failures)
        {
            Console.WriteLine($"  - {failure}");
        }

        return ExitFileFailures;
    }
}
