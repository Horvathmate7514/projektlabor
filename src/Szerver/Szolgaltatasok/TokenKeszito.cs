using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeltarKezelo.Szerver.Adat.Entitasok;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LeltarKezelo.Szerver.Szolgaltatasok;

public class TokenKeszito(IOptions<JwtBeallitasok> beallitasok, TimeProvider ido)
{
    private readonly JwtBeallitasok _jwt = beallitasok.Value;

    public (string Token, DateTime Lejarat) Keszit(Felhasznalo felhasznalo, IEnumerable<string> szerepkorok)
    {
        var most = ido.GetUtcNow().UtcDateTime;
        var lejarat = most.AddMinutes(_jwt.ErvenyessegPerc);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, felhasznalo.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, felhasznalo.Felhasznalonev),
            new(JwtRegisteredClaimNames.Name, felhasznalo.Nev),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(szerepkorok.Select(s => new Claim(ClaimTypes.Role, s)));

        var kulcs = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Kulcs));
        var token = new JwtSecurityToken(
            issuer: _jwt.Kibocsato,
            audience: _jwt.Celkozonseg,
            claims: claims,
            notBefore: most,
            expires: lejarat,
            signingCredentials: new SigningCredentials(kulcs, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), lejarat);
    }
}
