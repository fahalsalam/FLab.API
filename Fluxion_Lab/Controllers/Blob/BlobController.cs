using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Fluxion_Lab.Models.General;
using Microsoft.AspNetCore.Mvc;

namespace Fluxion_Lab.Controllers.Blob
{
    [Route("api/0101")]
    //[ApiController]
    public class BlobController : ControllerBase
    {
        protected APIResponse _response;
        private readonly string _connectionString;
        private readonly string _containerName;
        private readonly string _destinationPath;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _taskName = "start_react_app";

        public BlobController(IConfiguration configuration,BlobServiceClient blobServiceClient)
        {
            _response = new APIResponse();
            _connectionString = configuration.GetValue<string>("AzureBlobStorage:ConnectionString");
            _containerName = configuration.GetValue<string>("AzureBlobStorage:ContainerName");
            _destinationPath = configuration.GetValue<string>("AzureBlobStorage:DestinationPath");
            _blobServiceClient = blobServiceClient;
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadAndReplaceFolder()
        {
            try
            {
                // Create a temporary directory for downloading files
                string tempDirectoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectoryPath);

                // Download files to the temporary directory
                BlobContainerClient containerClient = new BlobContainerClient(_connectionString, _containerName);
                await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                {
                    BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
                    string localFilePath = Path.Combine(tempDirectoryPath, blobItem.Name);

                    string localDirPath = Path.GetDirectoryName(localFilePath);
                    if (!Directory.Exists(localDirPath))
                    {
                        Directory.CreateDirectory(localDirPath);
                    }

                    await blobClient.DownloadToAsync(localFilePath);
                }

                // Copy files from the temporary directory to the destination folder
                CopyFilesRecursively(new DirectoryInfo(tempDirectoryPath), new DirectoryInfo(_destinationPath));

                // Delete the temporary directory
                Directory.Delete(tempDirectoryPath, true);

                _response.isSucess = true;
                _response.message = "Folder downloaded and replaced successfully";
                _response.data = null;

                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.isSucess = false;
                _response.message = ex.Message;
                return StatusCode(500, _response);
            }
        }

        private void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                DirectoryInfo targetSubDir = target.CreateSubdirectory(dir.Name);
                CopyFilesRecursively(dir, targetSubDir);
            }

            foreach (FileInfo file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }
        }
    }
}
