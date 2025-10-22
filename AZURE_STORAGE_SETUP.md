# Azure Storage Configuration

The application uses Azure Storage for blob storage and queue management. You need to configure your Azure Storage connection string.

## Setting up Azure Storage Connection String

### Option 1: Using appsettings.json (Not Recommended for Production)

Update the `AzureStorage:ConnectionString` value in `appsettings.json`:

```json
"AzureStorage": {
  "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT_NAME;AccountKey=YOUR_ACCOUNT_KEY;EndpointSuffix=core.windows.net",
  "ContainerName": "fluxion",
  "QueueName": "fluxion-data-sync"
}
```

### Option 2: Using User Secrets (Recommended for Development)

```bash
dotnet user-secrets init --project Fluxion_Lab
dotnet user-secrets set "AzureStorage:ConnectionString" "YOUR_CONNECTION_STRING_HERE" --project Fluxion_Lab
```

### Option 3: Using Environment Variables (Recommended for Production)

Set the following environment variables:

- `AzureStorage__ConnectionString`
- `AzureStorage__ContainerName`
- `AzureStorage__QueueName`

## Security Notes

?? **IMPORTANT**: Never commit the actual Azure Storage connection string to the repository. It contains sensitive credentials.

- The `appsettings.json` file contains a placeholder value
- Use User Secrets for local development
- Use Environment Variables or Azure Key Vault for production deployments
- The `.gitignore` file is configured to exclude `appsettings.Development.json` where you can safely store local settings

## Getting Your Azure Storage Connection String

1. Log in to [Azure Portal](https://portal.azure.com)
2. Navigate to your Storage Account
3. Go to "Access keys" under Settings
4. Copy the connection string from key1 or key2
