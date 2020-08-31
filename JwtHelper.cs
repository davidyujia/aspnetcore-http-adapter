using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HttpAdapter
{
    public class JwtHelper
    {
        private readonly IConfiguration _configuration;

        public JwtHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(string userName, int expireMinutes = 30)
        {
            var issuer = _configuration.GetValue<string>("JwtSettings:Issuer");
            var signKey = _configuration.GetValue<string>("JwtSettings:SignKey");

            // �]�w�n�[�J�� JWT Token �����n����T(Claims)
            var claims = new List<Claim>();

            // �b RFC 7519 �W�椤(Section#4)�A�`�@�w�q�F 7 �ӹw�]�� Claims�A�ڭ����ӥu�Ϊ����ءI
            //claims.Add(new Claim(JwtRegisteredClaimNames.Iss, issuer));
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userName)); // User.Identity.Name
            //claims.Add(new Claim(JwtRegisteredClaimNames.Aud, "The Audience"));
            //claims.Add(new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds().ToString()));
            //claims.Add(new Claim(JwtRegisteredClaimNames.Nbf, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())); // �������Ʀr
            //claims.Add(new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())); // �������Ʀr
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())); // JWT ID

            // �����W�`�ݨ쪺�o�� NameId �]�w�O�h�l��
            //claims.Add(new Claim(JwtRegisteredClaimNames.NameId, userName));

            // �o�� Claim �]�H�����Q JwtRegisteredClaimNames.Sub ���N�A�ҥH�]�O�h�l��
            //claims.Add(new Claim(ClaimTypes.Name, userName));

            // �A�i�H�ۦ��X�R "roles" �[�J�n�J�̸Ӧ�������
            claims.Add(new Claim("roles", "Admin"));
            claims.Add(new Claim("roles", "Users"));

            var userClaimsIdentity = new ClaimsIdentity(claims);

            // �إߤ@�չ�٦��[�K�����_�A�D�n�Ω� JWT ñ������
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey));

            // HmacSha256 ���n�D�����n�j�� 128 bits�A�ҥH key ����ӵu�A�ܤ֭n 16 �r���H�W
            // https://stackoverflow.com/questions/47279947/idx10603-the-algorithm-hs256-requires-the-securitykey-keysize-to-be-greater
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            // �إ� SecurityTokenDescriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                //Audience = issuer, // �ѩ�A�� API �����q�`�S���Ϥ��S�O��H�A�]���q�`���ӻݭn�]�w�A�]���ӻݭn����
                //NotBefore = DateTime.Now, // �w�]�ȴN�O DateTime.Now
                //IssuedAt = DateTime.Now, // �w�]�ȴN�O DateTime.Now
                Subject = userClaimsIdentity,
                Expires = DateTime.Now.AddMinutes(expireMinutes),
                SigningCredentials = signingCredentials
            };

            // ���X�һݭn�� JWT securityToken ����A�è��o�ǦC�ƫ᪺ Token ���G(�r��榡)
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var serializeToken = tokenHandler.WriteToken(securityToken);

            return serializeToken;
        }
    }
}