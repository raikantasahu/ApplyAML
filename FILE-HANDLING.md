# File handling

ApplyAML can upload a physical file when the AML creates a File item. This document describes that
behavior.

## When it runs

ApplyAML processes an AML payload before it sends the payload to the server. It finds each File item
that is added with a physical file:

- The item type is `File`.
- The `action` attribute exists and is `add`.
- The item has a `<file_path>` child.

A File item that does not meet all three conditions stays unchanged. ApplyAML sends it to the server
as normal AML.

## What it does

For each such File item, ApplyAML does the following:

1. Create a File item on the server.
2. Set the File id (see **The File id** below).
3. Set the `filename` to the name of the physical file, with the extension.
4. Attach the physical file with `attachPhysicalFile`. The bytes stream to the vault on `apply()`.
5. Apply the File item. This uploads the file and returns the new File id.

Then ApplyAML rewrites the payload:

- When the File item is the value of a property (for example `<related_id>`), ApplyAML sets the
  property to the new File id. It removes the inline File item.
- When the File item is not inside a property, ApplyAML removes the File item. The upload is all that
  is needed.

After the rewrite, ApplyAML applies the payload as usual. When every item was an uploaded File, and
nothing is left, ApplyAML applies nothing and reports `ok`.

## The File id

ApplyAML chooses the File id in this order:

1. The `id` attribute on the File item, if it exists.
2. The `<id>` child element, if it exists.
3. A new id from the server, otherwise.

The attribute wins when both the attribute and the child exist. ApplyAML always sets the id
explicitly, so the File gets the supplied id, or a new one. An id is required: `attachPhysicalFile`
throws "Item ID is not set" without one. Because ApplyAML sets the id, this requirement is always met.

## The filename

The `filename` is always the name of the physical file from `file_path`, with the extension. ApplyAML
does not read a `<filename>` element.

## Errors

- A File item with `action="add"` must have a non-empty `<file_path>`. A missing or empty `<file_path>`
  is an error.
- The physical file must exist. A missing file is an error.

An error fails that one file. ApplyAML prints a `FAIL` line and continues with the next file.

## Order and orphaned files

ApplyAML uploads the files first. Then it applies the rewritten payload. So the files reach the vault
before the rest of the AML.

When the later apply fails, the uploaded files stay on the server. ApplyAML does not delete them.
Remove such a file by hand when you need to.

## Examples

### A File inside a property

The CAD ItemType has a `native_file` property that holds a File. The AML below adds a CAD item. The File
is the value of the `native_file` property:

```xml
<AML>
 <Item type="CAD" action="add">
  <item_number>CAD-0001</item_number>
  <native_file>
   <Item type="File" action="add">
    <file_path>C:\Data\model.prt</file_path>
   </Item>
  </native_file>
 </Item>
</AML>
```

ApplyAML uploads `model.prt`, sets `native_file` to the new File id, and then adds the CAD item.

### A File inside a property in a relationship

The AML below adds a Document. The Document has a Document File relationship. The relationship
references a File by `file_path`:

```xml
<AML>
 <Item type="Document" action="add">
  <item_number>DOC-0001</item_number>
  <Relationships>
   <Item type="Document File" action="add">
    <related_id>
     <Item type="File" action="add">
      <file_path>C:\Data\FileName.ext</file_path>
     </Item>
    </related_id>
   </Item>
  </Relationships>
 </Item>
</AML>
```

ApplyAML uploads `FileName.ext`, sets `related_id` to the new File id, and then adds the Document
and the relationship.
