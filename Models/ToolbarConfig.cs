using System.Collections.Generic;

namespace OrganizadorDescargas.Models;

public sealed class ToolbarConfig
{
    public List<ToolbarItem> Items        { get; set; } = new();
    public int               X            { get; set; } = 200;
    public int               Y            { get; set; } = 200;
    public bool              AlwaysOnTop  { get; set; } = true;
    public bool              AutoCollapse { get; set; } = false;
}
