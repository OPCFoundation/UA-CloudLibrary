using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DataPlane.Sdk.Example.Web;
using Microsoft.IdentityModel.Tokens;

string GenerateJwtToken(string principal, ConfigurationManager config)
{
    var jwtSettings = config.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(secretKey ?? throw new InvalidOperationException("JwtSettings:SecretKey must not be empty"));

    var tokenDescriptor = new SecurityTokenDescriptor {
        Expires = DateTime.UtcNow.AddHours(1),
        Issuer = jwtSettings["Issuer"],
        Audience = jwtSettings["Audience"],
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
        Claims = new Dictionary<string, object>
        {
            { "sub", principal }
        }
    };

    return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
}

var builder = WebApplication.CreateBuilder(args);

// Add controllers to the container.

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


// SDK: add all services, read configuration etc. 
builder.Services.AddDataPlaneSdk(builder.Configuration);

// generates and prints a JWT token that can be used to authenticate API calls
var token = GenerateJwtToken("60022e97-3595-4d3c-9907-dc5366e0f808", builder.Configuration);

var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
logger.LogInformation(token);
var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
