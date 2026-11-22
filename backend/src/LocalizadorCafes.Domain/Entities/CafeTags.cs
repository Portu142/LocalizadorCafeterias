namespace LocalizadorCafes.Domain.Entities;

/// <summary>
/// Vocabulario cerrado de etiquetas de una cafetería. El frontend traduce cada
/// clave a su texto e icono, así que solo se aceptan estos valores.
/// </summary>
public static class CafeTags
{
    public const string Wifi = "wifi";
    public const string Terrace = "terraza";
    public const string PowerOutlets = "enchufes";
    public const string Accessible = "accesible";
    public const string Takeaway = "para-llevar";
    public const string PetFriendly = "mascotas";
    public const string Vegan = "vegano";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Wifi, Terrace, PowerOutlets, Accessible, Takeaway, PetFriendly, Vegan
    };
}
