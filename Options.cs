namespace ApplyAML;

/// <summary>Raised when the command line cannot be turned into a usable <see cref="Options"/>.</summary>
internal sealed class OptionsException : Exception
{
    public OptionsException(string message) : base(message) { }
}

internal sealed class Options
{
    public string Input { get; private set; } = "";
    public string Url { get; private set; } = "";
    public string Database { get; private set; } = "";
    public string User { get; private set; } = "";
    public string Password { get; private set; } = "";
    public string Pattern { get; private set; } = "*.xml";
    public bool PrintResults { get; private set; }
    public bool ShowHelp { get; private set; }

    public const string Usage = """
        ApplyAML - apply Aras Innovator AML files to a server.

        Usage:
          ApplyAML --input <file-or-directory> --url <url> --database <db> --user <name> [--password <pw>]

        Required:
          -i, --input <path>       XML file, or directory searched recursively for AML files.
          -u, --url <url>          Innovator server URL, e.g. http://host/InnovatorServer
          -d, --database <name>    Database to connect to.
          -n, --user <name>        Innovator login name.

        Optional:
          -p, --password <pw>      Password (plaintext or MD5). Prompted for if omitted.
              --pattern <glob>     File filter used when input is a directory. Default: *.xml
          -r, --print-results      Print the AML response returned for each file. Responses
                                   for files that fail are always printed, with or without
                                   this option.
          -h, --help               Show this help.

        Every file is attempted; failures are reported in a summary at the end.

        Exit codes:
          0  all files applied successfully
          1  one or more files failed
          2  bad usage, or could not connect / log in
        """;

    public static Options Parse(string[] args)
    {
        var options = new Options();
        var positional = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg is "-h" or "--help" or "-?" or "/?")
            {
                options.ShowHelp = true;
                return options;
            }

            if (arg is "-r" or "--print-results")
            {
                options.PrintResults = true;
                continue;
            }

            switch (arg)
            {
                case "-i" or "--input":
                    options.Input = NextValue(args, ref i, arg);
                    continue;
                case "-u" or "--url":
                    options.Url = NextValue(args, ref i, arg);
                    continue;
                case "-d" or "--database":
                    options.Database = NextValue(args, ref i, arg);
                    continue;
                case "-n" or "--user":
                    options.User = NextValue(args, ref i, arg);
                    continue;
                case "-p" or "--password":
                    options.Password = NextValue(args, ref i, arg);
                    continue;
                case "--pattern":
                    options.Pattern = NextValue(args, ref i, arg);
                    continue;
            }

            if (arg.StartsWith('-'))
            {
                throw new OptionsException($"Unknown option '{arg}'.");
            }

            positional.Add(arg);
        }

        // A bare path is a convenient shorthand for --input.
        if (positional.Count > 0)
        {
            if (options.Input.Length > 0)
            {
                throw new OptionsException($"Input given twice: '{options.Input}' and '{positional[0]}'.");
            }

            if (positional.Count > 1)
            {
                throw new OptionsException($"Unexpected extra argument '{positional[1]}'.");
            }

            options.Input = positional[0];
        }

        options.Validate();
        return options;
    }

    private static string NextValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new OptionsException($"Option '{option}' requires a value.");
        }

        return args[++index];
    }

    private void Validate()
    {
        var missing = new List<string>();
        if (Input.Length == 0) missing.Add("--input");
        if (Url.Length == 0) missing.Add("--url");
        if (Database.Length == 0) missing.Add("--database");
        if (User.Length == 0) missing.Add("--user");

        if (missing.Count > 0)
        {
            throw new OptionsException($"Missing required option(s): {string.Join(", ", missing)}.");
        }

        if (!File.Exists(Input) && !Directory.Exists(Input))
        {
            throw new OptionsException($"Input path does not exist: {Input}");
        }
    }

    /// <summary>Prompts for the password when it was not supplied on the command line.</summary>
    public void EnsurePassword()
    {
        if (Password.Length > 0)
        {
            return;
        }

        Console.Error.Write($"Password for {User}: ");
        Password = Console.IsInputRedirected ? Console.ReadLine() ?? "" : ReadMasked();
        Console.Error.WriteLine();

        if (Password.Length == 0)
        {
            throw new OptionsException("No password supplied.");
        }
    }

    private static string ReadMasked()
    {
        var password = new System.Text.StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    return password.ToString();
                case ConsoleKey.Backspace when password.Length > 0:
                    password.Length--;
                    Console.Error.Write("\b \b");
                    break;
                case ConsoleKey.Backspace:
                    break;
                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        password.Append(key.KeyChar);
                        Console.Error.Write('*');
                    }
                    break;
            }
        }
    }
}
