using Aras.IOM;

namespace ApplyAML;

internal static class Program
{
    private const int ExitSuccess = 0;
    private const int ExitFileFailures = 1;
    private const int ExitUsage = 2;

    private static int Main(string[] args)
    {
        // Emit UTF-8 so non-ASCII in server responses (e.g. the superscript in a unit
        // like "g/cm³") survives a redirect to a file. Without this, a redirected stdout
        // uses the legacy console code page, whose best-fit mapping silently rewrites such
        // characters to ASCII lookalikes ("g/cm3") with no error. Covers Out and Error.
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);

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

        // A single file is applied directly; a directory is walked depth-first (subdirectories
        // before files, each level sorted by name) and each matching file applied as it is visited.
        var applier = new AmlApplier(innovator, options);
        try
        {
            if (File.Exists(options.Input))
            {
                applier.ProcessFile(Path.GetFullPath(options.Input));
            }
            else
            {
                var traversor = new DirectoryTraversor(
                    applier,
                    fileIgnore: new PatternIgnore(options.Patterns),
                    directoryIgnore: NeverIgnore<string>.Instance,
                    recurse: true,
                    depthFirst: true);
                traversor.Traverse(options.Input);
            }
        }
        finally
        {
            TryLogout(connection);
        }

        if (applier.Count == 0)
        {
            Console.Error.WriteLine(
                $"error: no files matching {string.Join(", ", options.Patterns)} under {options.Input}");
            return ExitUsage;
        }

        return Report(applier.Count, applier.Failures);
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
