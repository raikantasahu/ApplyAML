# ApplyAML

A small .NET command-line tool that applies Aras Innovator **AML** files to an
Innovator server, using the public
[`Aras.IOM`](https://www.nuget.org/packages/Aras.IOM) package.

Point it at a single XML file or a directory. Each file is expected to contain
AML — an `<AML>` root with one or more `<Item>` children, or a bare `<Item>`
that is wrapped automatically. The tool logs in, sends each file's AML to the
server, and reports the outcome.

## Build

Requires the .NET 10 SDK.

    dotnet build

The `Aras.IOM` package (net10.0) is restored from nuget.org.

## Usage

    ApplyAML --input <file-or-dir> --url <url> --database <db> --user <name> [--password <pw>]

| Short | Long | Description |
|-------|------|-------------|
| `-i` | `--input <path>` | XML file, or directory searched recursively for AML files. |
| `-u` | `--url <url>` | Innovator server URL, e.g. `http://host/InnovatorServer`. |
| `-d` | `--database <name>` | Database to connect to. |
| `-n` | `--user <name>` | Innovator login name. |
| `-p` | `--password <pw>` | Password (plaintext or MD5). Prompted for (masked) if omitted. |
|      | `--pattern <glob>` | File filter when the input is a directory. Default `*.xml`. |
| `-r` | `--print-results` | Print each file's AML response. Fault responses are always printed. |
| `-h` | `--help` | Show help. |

Example:

    dotnet run -- -i C:\aml -u http://host/InnovatorServer -d MyDB -n admin

Omit `--password` and the tool prompts for it with masked input, keeping it out
of shell history. The password may be plaintext or its Innovator MD5 — `Aras.IOM`
accepts either.

## Behavior

- **Directory input** is searched recursively and applied in sorted order.
- **Every file is attempted**; failures are collected and reported in a summary
  at the end.
- A bare `<Item>` root is wrapped in `<AML>`; any other root element is rejected
  as not being AML.
- **File items** that add a physical file get custom logic: ApplyAML uploads the
  file and rewrites the AML to reference the new File. See
  [FILE-HANDLING.md](FILE-HANDLING.md).

### Exit codes

| Code | Meaning |
|------|---------|
| `0` | All files applied successfully. |
| `1` | One or more files failed. |
| `2` | Bad usage, or could not connect / log in. |

## License

[MIT](LICENSE)
