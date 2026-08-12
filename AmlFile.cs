using System.Xml.Linq;

namespace ApplyAML;

/// <summary>Reads an AML file and normalizes it into the form <c>applyAML</c> expects.</summary>
internal static class AmlFile
{
    /// <summary>
    /// Loads <paramref name="path"/> and returns AML with an &lt;AML&gt; root element.
    /// Files whose root is a bare &lt;Item&gt; are wrapped, since applyAML requires the
    /// &lt;AML&gt; envelope.
    /// </summary>
    /// <exception cref="System.Xml.XmlException">The file is not well-formed XML.</exception>
    /// <exception cref="InvalidDataException">The file is XML but not recognizable AML.</exception>
    public static string Load(string path)
    {
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);

        var root = document.Root
            ?? throw new InvalidDataException("File contains no root element.");

        if (root.Name.LocalName == "AML")
        {
            if (!root.Elements("Item").Any())
            {
                throw new InvalidDataException("<AML> root contains no <Item> elements.");
            }

            return root.ToString(SaveOptions.DisableFormatting);
        }

        if (root.Name.LocalName == "Item")
        {
            return new XElement("AML", root).ToString(SaveOptions.DisableFormatting);
        }

        throw new InvalidDataException(
            $"Root element is <{root.Name.LocalName}>; expected <AML> or <Item>.");
    }
}
