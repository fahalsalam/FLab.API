using Fluxion_Lab.Models.General;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace Fluxion_Lab.Classes.DBOperations
{
    public class Fluxion_Handler
    {
        #region Encrypt String
        public static string EncryptString(string message, string key)
        {
            try
            {
                byte[] iv = new byte[12]; // 96 bits for AES-GCM
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }

                byte[] ciphertext = new byte[message.Length];
                byte[] tag = new byte[16]; // 128 bits for AES-GCM
                using (AesGcm aesGcm = new AesGcm(Encoding.UTF8.GetBytes(key)))
                {
                    aesGcm.Encrypt(iv, Encoding.UTF8.GetBytes(message), ciphertext, tag, null);
                }

                byte[] result = iv.Concat(ciphertext).Concat(tag).ToArray();
                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Encryption failed: {ex.Message}");
                return "Failed";
            }
        }
        #endregion

        #region Decrypt String
        public static string DecryptString(string cipherText, string key)
        {
            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);
                byte[] iv = fullCipher.Take(12).ToArray(); // Extract IV
                byte[] ciphertext = fullCipher.Skip(12).Take(fullCipher.Length - 28).ToArray(); // Extract Ciphertext
                byte[] tag = fullCipher.Skip(fullCipher.Length - 16).Take(16).ToArray(); // Extract Tag

                using (AesGcm aesGcm = new AesGcm(Encoding.UTF8.GetBytes(key)))
                {
                    byte[] decryptedMessage = new byte[ciphertext.Length];
                    aesGcm.Decrypt(iv, ciphertext, tag, decryptedMessage, null);
                    return Encoding.UTF8.GetString(decryptedMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption failed: {ex.Message}");
                return "Failed";
            }
        }
        #endregion

        #region Refresh Token
        public static string RefreshToken()
        {
            string refreshToken;
            var randomNumber = new byte[32];
            using (var randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(randomNumber);
                refreshToken = Convert.ToBase64String(randomNumber);
            }
            return refreshToken;
        }
        #endregion

        #region JWT String Encrypt
        public static string JWtKey()
        {
            string jwt = "";
            string jwtKey = "KRT/fdZ0pp3qpq00EXnMz8D7ZP+mp9UdZ4LLN778f31V2w==";
            jwt = EncryptString(jwtKey, APIString);
            return jwt;
        }
        #endregion

        #region Key
        public static string APIString = "CkL@U$82n#H6P*#T";
        #endregion

        #region JWT Token Claims
        public static TokenClaims GetJWTTokenClaims(string jwtToken, string JwtKey, bool IsEncrypted)
        {
            try
            {
                if (IsEncrypted)
                {
                    JwtKey = DecryptString(JwtKey, APIString);
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenKey = Encoding.UTF8.GetBytes(JwtKey);
                SecurityToken securityToken;

                var principle = tokenHandler.ValidateToken(jwtToken, new TokenValidationParameters()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(tokenKey),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                }, out securityToken);

                var TokenClaims = new TokenClaims
                {
                    UserId = principle.FindFirst("xU").Value,  // UserID
                    ClientId = principle.FindFirst("xC").Value,  // ClientID 
                };

                return TokenClaims;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion


    }
}
