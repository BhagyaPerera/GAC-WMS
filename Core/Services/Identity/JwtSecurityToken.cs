using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Core.Services.Identity
{
    internal class JwtSecurityToken
    {
        private string? issuer;
        private string? audience;
        private List<Claim> claims;
        private DateTime expires;
        private SigningCredentials signingCredentials;

        public JwtSecurityToken(string? issuer, string? audience, List<Claim> claims, DateTime expires, SigningCredentials signingCredentials)
        {
            this.issuer = issuer;
            this.audience = audience;
            this.claims = claims;
            this.expires = expires;
            this.signingCredentials = signingCredentials;
        }
    }
}