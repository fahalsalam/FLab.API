using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fluxion_Lab.Controllers.Image
{
    [Route("api/0303")] 
    public class ImageController : ControllerBase
    {
        private readonly string _azureStorageConnectionString;
        private readonly string _containerName;

        public ImageController(IConfiguration configuration)
        {
         _azureStorageConnectionString = configuration["AzureStorage:ConnectionString"];
         _containerName = configuration["AzureStorage:ContainerName"];
        }

        #region Image Upload  
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty");
            }

            // Check if the system has an internet connection
            bool isOnline = CheckInternetConnection();
            string azureUrl = ""; 
            azureUrl = await UploadToAzureBlob(file);  
            // Fallback: Save locally if not online or Azure upload fails
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string imageUrl = $"{Request.Scheme}://{Request.Host}/images/{fileName}";
            return Ok(new { Url = imageUrl,cloudUrl = azureUrl });
        } 

        #endregion

        #region Delete Image
        [HttpPost("delete-image")]
        public IActionResult DeleteImage(string fileName)
        {
            // Define the path to the images folder
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

            // Combine the folder path with the file name to get the full path
            string filePath = Path.Combine(folderPath, fileName);

            // Check if the file exists
            if (System.IO.File.Exists(filePath))
            {
                // Delete the file
                System.IO.File.Delete(filePath);

                return Ok(new { message = $"Image {fileName} deleted successfully" });
            }
            else
            {
                return NotFound(new { message = $"Image {fileName} not found" });
            }
        }
        #endregion

        private bool CheckInternetConnection()
        {
            try
            {
                using (var client = new System.Net.NetworkInformation.Ping())
                {
                    var reply = client.Send("8.8.8.8", 1000); // Google's public DNS
                    return reply != null && reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> UploadToAzureBlob(IFormFile file)
        {
            try
            {
                // Validate configuration
                if (string.IsNullOrEmpty(_azureStorageConnectionString) || _azureStorageConnectionString == "YOUR_AZURE_STORAGE_CONNECTION_STRING_HERE")
                {
                    Console.WriteLine("Azure Storage is not configured. Skipping cloud upload.");
                    return null;
                }

                // Create BlobServiceClient
                BlobServiceClient blobServiceClient = new BlobServiceClient(_azureStorageConnectionString);

                // Get reference to the container
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

                // Ensure the container exists
                await containerClient.CreateIfNotExistsAsync();

                // Set the container to be publicly accessible
                await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

                // Generate a unique file name
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                // Get a reference to the blob
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                // Upload the file
                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, true);
                }

                // Return the blob's URL
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                // Log the exception (use proper logging in production)
                Console.WriteLine($"Error uploading to Azure Blob: {ex.Message}");
                return null;
            }
        }
    }
}
