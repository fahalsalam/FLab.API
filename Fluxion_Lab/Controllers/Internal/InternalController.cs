using Fluxion_Lab.Classes.DBOperations;
using Fluxion_Lab.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fluxion_Lab.Controllers.Internal
{  
    public class InternalController : ControllerBase
    {
        private readonly string inputFilePath = "C:\\Contacts\\phone_numbers.txt";  // Input file
        private readonly string outputFilePath = "C:\\Contacts\\formatted_contacts.txt"; // Output file 


        #region Encypt String
        [HttpGet("encyptString")]
        public IActionResult EncryptString(string text)
        {
            string encyptString = Fluxion_Handler.EncryptString(text, Fluxion_Handler.APIString);
            return Ok(encyptString);
        }
        #endregion

        #region Decrypt String
        [HttpGet("decryptString")]
        public IActionResult DecryptString(string text)
        {
            string DecryptString = Fluxion_Handler.DecryptString(text, Fluxion_Handler.APIString);
            return Ok(DecryptString);
        }
        #endregion

        [AllowAnonymous]
        [HttpGet("format-and-save")]
        public async Task<IActionResult> FormatAndSaveContacts()
        {
            if (!System.IO.File.Exists(inputFilePath))
            {
                return NotFound("Contacts file not found.");
            }

            // Read contacts file
            var content = await System.IO.File.ReadAllTextAsync(inputFilePath);

            // Split and format contacts
            var contacts = content.Split(',')
                                  .Select(c => c.Trim())
                                  .ToList();

            // Write formatted contacts to a new file (each contact in a new line)
            await System.IO.File.WriteAllLinesAsync(outputFilePath, contacts);

            return Ok(new { message = "Contacts formatted and saved successfully!", filePath = outputFilePath });
        }
        


    }
}
