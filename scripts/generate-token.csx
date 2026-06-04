#!/usr/bin/env dotnet-script
// Genera un JWT de demo para pruebas locales
// Uso: dotnet script generate-token.csx
// Requiere: dotnet tool install -g dotnet-script

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var key = "super-secret-key-for-demo-at-least-32-chars!";
var issuer = "event-platform";
var audience = "event-platform-clients";

var secKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
var creds  = new SigningCredentials(secKey, SecurityAlgorithms.HmacSha256);

var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub, "00000000-0000-0000-0000-000000000001"),
    new Claim(JwtRegisteredClaimNames.Name, "Admin User"),
    new Claim(ClaimTypes.Role, "admin"),
    new Claim(JwtRegisteredClaimNames.Iss, issuer),
    new Claim(JwtRegisteredClaimNames.Aud, audience),
};

var token = new JwtSecurityToken(
    issuer:   issuer,
    audience: audience,
    claims:   claims,
    expires:  DateTime.UtcNow.AddYears(10),
    signingCredentials: creds
);

var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

Console.WriteLine("=== JWT de Demo ===");
Console.WriteLine(tokenStr);
Console.WriteLine();
Console.WriteLine("Úsalo en Swagger: Bearer " + tokenStr);
