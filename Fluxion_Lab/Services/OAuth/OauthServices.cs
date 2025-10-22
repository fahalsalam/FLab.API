using Fluxion_Lab.Classes.DatabaseManager;
using Microsoft.IdentityModel.Tokens;
using static Fluxion_Lab.Classes.DatabaseManager.MultiTenantDatabaseManager;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fluxion_Lab.Models.General;
using Fluxion_Lab.Classes.DBOperations;

namespace Fluxion_Lab.Services.OAuth
{
    public class OauthServices
    {
        #region Login
        public static JwtToken LoginNow(string Tenant, string UserName, string Password, string jwtKey)
        {
            JwtToken _token = new JwtToken();
            //MultiTenantDatabaseManager _dbManger = MultiTenantDatabaseManager.Instance;
            try
            { 

                var refreshToken = Fluxion_Handler.RefreshToken(); 

                /** Jwt key Decryption **/
                var jwtKeyDecrypted = Fluxion_Handler.DecryptString(jwtKey, Fluxion_Handler.APIString);

                /** JWT Token Creation Begin.. **/
                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenKey = Encoding.UTF8.GetBytes(jwtKeyDecrypted);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim("xC", "1001"), 
                        new Claim("xU", "1")
                    }),
                    Expires = DateTime.UtcNow.AddYears(5),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var finalToken = tokenHandler.WriteToken(token);
                
                _token._accessToken = finalToken;
                _token._refreshToken = refreshToken;

                return _token;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        #endregion
    }
}
