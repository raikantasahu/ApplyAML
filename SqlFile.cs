namespace ApplyAML;

/// <summary>Reads a .sql file into the raw SQL string that <c>applySQL</c> expects.</summary>
internal static class SqlFile
{
    /// <summary>
    /// Loads <paramref name="path"/> and returns its SQL, trimmed. Unlike AML, the text is
    /// passed to the server verbatim (applySQL builds the SQL envelope), so there is nothing
    /// to parse or wrap here.
    /// </summary>
    /// <exception cref="InvalidDataException">The file has no SQL to run.</exception>
    public static string Load(string path)
    {
        var sql = File.ReadAllText(path).Trim();
        if (sql.Length == 0)
        {
            throw new InvalidDataException("File contains no SQL.");
        }

        return sql;
    }
}
