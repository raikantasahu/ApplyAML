using System.Xml.Linq;
using Aras.IOM;

namespace ApplyAML;

/// <summary>
/// Uploads physical files referenced inside an AML payload. An <c>&lt;Item type="File" action="add"&gt;</c>
/// that carries a <c>&lt;file_path&gt;</c> child is not valid AML on its own: the bytes must be streamed to
/// the vault with <see cref="Item.attachPhysicalFile(string)"/>. This class finds each such File item,
/// creates the File on the server, and rewrites the payload to reference the new File by id.
/// </summary>
internal sealed class FileUploader
{
    private readonly Innovator _innovator;

    public FileUploader(Innovator innovator)
    {
        _innovator = innovator;
    }

    /// <summary>The payload to apply after file upload, and whether any items are left to apply.</summary>
    internal readonly record struct Result(string Aml, bool HasItemsToApply);

    /// <summary>
    /// Uploads every added File item and rewrites the payload. A File item inside a property (e.g.
    /// &lt;related_id&gt;) is replaced by the new File id; a File item that is not inside a property is
    /// removed. A payload with no added File items is returned unchanged.
    /// </summary>
    /// <exception cref="InvalidDataException">An added File item has no &lt;file_path&gt;.</exception>
    /// <exception cref="FileNotFoundException">A referenced physical file does not exist.</exception>
    /// <exception cref="InvalidOperationException">The server rejected a File upload.</exception>
    public Result UploadEmbeddedFiles(string payload)
    {
        var document = XDocument.Parse(payload, LoadOptions.PreserveWhitespace);

        var fileAdds = document.Descendants("Item").Where(IsFileAdd).ToList();
        if (fileAdds.Count == 0)
        {
            return new Result(payload, HasItemsToApply: true);
        }

        foreach (var fileItem in fileAdds)
        {
            UploadAndReplace(fileItem);
        }

        var root = document.Root!;
        var hasItems = root.Elements("Item").Any();
        return new Result(root.ToString(SaveOptions.DisableFormatting), hasItems);
    }

    /// <summary>
    /// A File item that is added: type="File" with an action attribute of "add". Such an item must
    /// carry a &lt;file_path&gt;; <see cref="UploadAndReplace"/> treats a missing one as an error. A
    /// File item with no action, or a different action, is left for applyAML.
    /// </summary>
    private static bool IsFileAdd(XElement item)
        => (string?)item.Attribute("type") == "File"
            && (string?)item.Attribute("action") == "add";

    /// <summary>
    /// Uploads one File item, then rewrites the tree. When the File item is the value of a property,
    /// the property is set to the new File id. Otherwise the File item is removed.
    /// </summary>
    private void UploadAndReplace(XElement fileItem)
    {
        var filePath = (fileItem.Element("file_path")?.Value ?? "").Trim();
        if (filePath.Length == 0)
        {
            throw new InvalidDataException("a File item with action=\"add\" must have a non-empty <file_path>.");
        }

        var fileId = Upload(filePath, GetItemId(fileItem));

        var parent = fileItem.Parent;
        if (parent != null && IsProperty(parent))
        {
            parent.Value = fileId;   // <related_id><Item type="File"/></related_id> -> <related_id>ID</related_id>
        }
        else
        {
            fileItem.Remove();       // not inside a property: the upload is all that was needed
        }
    }

    /// <summary>
    /// Creates the File on the server with the given id and streams the physical bytes to the vault.
    /// The filename is the name of the physical file. Returns the id.
    /// </summary>
    private string Upload(string filePath, string itemId)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"file to attach not found: {filePath}");
        }

        var file = _innovator.newItem("File", "add");
        file.setID(itemId);                                        // attachPhysicalFile requires an id
        file.setProperty("filename", Path.GetFileName(filePath));
        file.attachPhysicalFile(filePath);                     // streamed on apply()

        var result = file.apply();
        if (result.isError())
        {
            throw new InvalidOperationException($"upload failed for {filePath}: {result.getErrorString()}");
        }

        return itemId;
    }

    /// <summary>
    /// The File item's id: the 'id' attribute, else an &lt;id&gt; child, else a new id from
    /// getNewID(). The attribute wins when both are present.
    /// </summary>
    private string GetItemId(XElement fileItem)
    {
        string? suppliedId = fileItem.Attribute("id")?.Value ?? fileItem.Element("id")?.Value;
        return string.IsNullOrEmpty(suppliedId) ? _innovator.getNewID() : suppliedId;
    }

    /// <summary>
    /// True when the element holds a property value. A property's value is a single Item. The elements
    /// that are not properties are the &lt;AML&gt; root, &lt;Relationships&gt;, and &lt;Item&gt; itself.
    /// </summary>
    private static bool IsProperty(XElement element)
        => element.Name.LocalName is not ("AML" or "Item" or "Relationships");
}
