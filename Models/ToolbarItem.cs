using System;

namespace OrganizadorDescargas.Models;

public enum ToolbarItemType { Macro, App }

public sealed class ToolbarItem
{
    public string          Id      { get; set; } = Guid.NewGuid().ToString();
    public ToolbarItemType Type    { get; set; } = ToolbarItemType.Macro;
    public string          Label   { get; set; } = "";
    public string          Glyph   { get; set; } = "";
    public string          MacroId { get; set; } = "";
    public string          ExePath { get; set; } = "";
}
