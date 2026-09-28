using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LeltarKezelo.Szerver.Adat;
using LeltarKezelo.Szerver.Adat.Entitasok;
using LeltarKezelo.Szerver.Szolgaltatasok;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddDbContext<LeltarDbContext>((sp, o) =>
{
    var kapcsolat = sp.GetRequiredService<IConfiguration>().GetConnectionString("Leltar")
        ?? throw new InvalidOperationException(
            "Hiányzik a ConnectionStrings:Leltar beállítás (lásd appsettings.Local.example.json).");
    o.UseSqlServer(kapcsolat);
});

builder.Services.AddOptions<JwtBeallitasok>()
    .BindConfiguration(JwtBeallitasok.Szekcio)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtBeallitasok>>((o, jwt) =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Value.Kibocsato,
            ValidAudience = jwt.Value.Celkozonseg,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Kulcs)),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPasswordHasher<Felhasznalo>, PasswordHasher<Felhasznalo>>();
builder.Services.AddScoped<TokenKeszito>();
builder.Services.AddScoped<AuthSzolgaltatas>();
builder.Services.AddScoped<EszkozSzolgaltatas>();

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Leltárkezelő API", Version = "v1" });
    var sema = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    o.AddSecurityDefinition("Bearer", sema);
    o.AddSecurityRequirement(new OpenApiSecurityRequirement { [sema] = [] });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous().ExcludeFromDescription();
    await KezdoAdmin.LetrehozasAsync(app.Services);
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
