namespace OrganizadorDescargas.Models;

public enum OrganizationMode
{
    ByZone,       // classify by file type → place in matching zone (default)
    Alphabetical, // A–Z within each zone
    ByExtension,  // group same extensions together within each zone
}
