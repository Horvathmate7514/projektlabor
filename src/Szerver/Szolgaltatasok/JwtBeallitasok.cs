using System.ComponentModel.DataAnnotations;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

public sealed class JwtBeallitasok
{
    public const string Szekcio = "Jwt";

    [Required]
    public string Kibocsato { get; set; } = "";

    [Required]
    public string Celkozonseg { get; set; } = "";

    [Required, MinLength(32, ErrorMessage = "A Jwt:Kulcs legalább 32 karakter hosszú legyen.")]
    public string Kulcs { get; set; } = "";

    [Range(5, 1440)]
    public int ErvenyessegPerc { get; set; } = 480;
}
