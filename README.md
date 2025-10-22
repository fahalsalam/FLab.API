# Fluxion Lab API

A multi-tenant SaaS laboratory management system built with .NET 8 and ASP.NET Core.

## Features

- **Multi-tenant Architecture**: Supports both SaaS and On-Premises deployment modes
- **JWT Authentication**: Secure token-based authentication with refresh tokens
- **Dynamic Database Routing**: Tenant-specific database connections
- **Azure Integration**: Blob storage for files and Queue for background tasks
- **Role-Based Access Control (RBAC)**: Granular permission system
- **RESTful API**: Comprehensive REST API for all operations

## Technology Stack

- **.NET 8**: Latest .NET framework
- **ASP.NET Core**: Web API framework
- **Dapper**: Lightweight ORM for database operations
- **SQL Server**: Primary database
- **Azure Storage**: Blob and Queue services
- **JWT**: JSON Web Tokens for authentication
- **Serilog**: Structured logging

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server 2019 or later
- Azure Storage Account (for SaaS mode)
- Visual Studio 2022 or VS Code

### Configuration

1. **Clone the repository**
   ```bash
   git clone https://github.com/fahalsalam/FLab.API.git
   cd FLab.API
 ```

2. **Configure Azure Storage** (Required for SaaS mode)
   
   See [AZURE_STORAGE_SETUP.md](AZURE_STORAGE_SETUP.md) for detailed instructions.

3. **Set up the database connection**
   
   For On-Premises mode, set the environment variable:
   ```
   ConStr=<your-encrypted-connection-string>
   ```

4. **Configure deployment mode**
   
   In `appsettings.json`, set the mode:
   ```json
   "SaaSOptions": {
     "Mode": "SaaS"  // or "OnPrem"
   }
   ```

### Running the Application

```bash
cd Fluxion_Lab
dotnet restore
dotnet build
dotnet run
```

The API will be available at `https://localhost:5001` (or the configured port).

### Swagger UI

Once running, access the Swagger documentation at:
```
https://localhost:5001/swagger
```

## Project Structure

```
Fluxion_Lab/
??? Classes/
?   ??? DependencyInjection/     # Service registration
?   ??? MiddleWare/       # Custom middleware
?   ??? DBOperations/      # Database helpers
??? Controllers/
?   ??? Authentication/      # Login and auth endpoints
?   ??? Masters/           # Master data management
?   ??? Transactions/            # Business transactions
?   ??? Reports/  # Reporting endpoints
?   ??? ...
??? Models/          # Data models and DTOs
??? Services/      # Business logic services
??? Program.cs    # Application entry point
```

## API Endpoints

### Authentication
- `POST /api/0102/getAuthenticated` - User login
- `GET /api/0102/getAppVersion` - Get application version

### Masters
- `/api/0101/*` - Master data CRUD operations

### Transactions
- `/api/0201/*` - Transaction processing

See Swagger UI for complete API documentation.

## Security

?? **Important Security Notes:**

- Never commit sensitive credentials to the repository
- Use User Secrets for local development
- Use Environment Variables or Azure Key Vault for production
- The Azure Storage connection string has been removed from source code
- See [AZURE_STORAGE_SETUP.md](AZURE_STORAGE_SETUP.md) for secure configuration

## Deployment

### On-Premises Deployment
1. Publish the application: `dotnet publish -c Release`
2. Set the `ConStr` environment variable
3. Configure IIS or host as a Windows Service

### SaaS Deployment (Azure)
1. Create an Azure App Service
2. Configure Azure Storage connection in App Settings
3. Set up Azure SQL Database
4. Deploy using Azure DevOps or GitHub Actions

## Contributing

This is a private project. For internal team members:

1. Create a feature branch
2. Make your changes
3. Submit a pull request for review

## License

Proprietary - All rights reserved

## Support

For support, contact the development team or create an issue in the repository.
