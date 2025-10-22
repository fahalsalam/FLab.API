using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Fluxion_Lab.Classes;

namespace Fluxion_Lab.Services.BlobService
{
    public class AzureBlobService
    {
        private readonly string _connectionString;
        private readonly string _containerName;
        private readonly string _destinationPath;
        private readonly string _taskName = "start_react_app";    

        public AzureBlobService(string connectionString, string containerName, string destinationPath)
        {
            _connectionString = connectionString;
            _containerName = containerName;
            _destinationPath = destinationPath;
          
        }

        public async Task DownloadAndReplaceFolderAsync()
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
