using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MicroServiceSystem.SharedKernel.Constants;

namespace MicroServiceSystem.Testing.Utilities;

public static class TestJwtFactory
{
    public static string CreateToken(
        Guid userId,
        Guid? tenantId = null,
        string userName = "tester",
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null,
        string issuer = "msf-tests",
        string audience = "msf-tests",
        string signingKey = "0123456789abcdef0123456789abcdef")
    {
        var claims = new Dictionary<string, object>
        {
            [FrameworkClaimTypes.UserId] = userId.ToString(),
            [FrameworkClaimTypes.UserName] = userName,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N")
        };

        if (tenantId is { } tid)
        {
            claims[FrameworkClaimTypes.TenantId] = tid.ToString();
        }

        string[] roleArray = roles?.ToArray() ?? [];
        if (roleArray.Length > 0)
        {
            claims[FrameworkClaimTypes.Role] = roleArray;
        }

        string[] permissionArray = permissions?.ToArray() ?? [];
        if (permissionArray.Length > 0)
        {
            claims[FrameworkClaimTypes.Permission] = permissionArray;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
