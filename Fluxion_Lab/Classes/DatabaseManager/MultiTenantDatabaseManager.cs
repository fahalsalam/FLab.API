namespace Fluxion_Lab.Classes.DatabaseManager
{
    public class MultiTenantDatabaseManager
    {
        private static MultiTenantDatabaseManager _instance;
        private static readonly object _lockObject = new object();

        public static MultiTenantDatabaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = new MultiTenantDatabaseManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private Dictionary<string, TenantDetails> connectionStrings;

        private MultiTenantDatabaseManager()
        {
            connectionStrings = new Dictionary<string, TenantDetails>();
        }

        public void AddTenant(string clientId, string userId, string token, string connectionString)
        {
            lock (_lockObject)
            {
                if (connectionStrings.ContainsKey(clientId))
                {
                    // Check if userId already exists for the clientId
                    if (connectionStrings.Values.Any(details => details.UserId == userId))
                    {
                        // Update TenantDetails for the existing clientId
                        connectionStrings[clientId] = new TenantDetails(userId, token, connectionString);
                    }
                }
                else
                {
                    // Add new TenantDetails for the clientId
                    connectionStrings.Add(clientId, new TenantDetails(userId, token, connectionString));
                }
            }
        }

        public string GetConnectionString(string clientId)
        {
            lock (_lockObject)
            {
                if (connectionStrings.TryGetValue(clientId, out TenantDetails tenantDetails))
                {
                    return tenantDetails.ConnectionString;
                }
                else
                {
                    throw new ArgumentException($"No Details found for client ID: {clientId}");
                }
            }
        }

        public bool IsTokenValid(string clientId, string userId, string token)
        {
            lock (_lockObject)
            {
                if (connectionStrings.TryGetValue(clientId, out TenantDetails tenantDetails) &&
                    tenantDetails.UserId == userId && tenantDetails.Token == token)
                {
                    return true;
                }
                else
                {
                    throw new ArgumentException("Session Timed Out");
                }
            }
        }

        public class TenantDetails
        {
            public string UserId { get; }
            public string Token { get; }
            public string ConnectionString { get; }

            public TenantDetails(string userId, string token, string connectionString)
            {
                UserId = userId;
                Token = token;
                ConnectionString = connectionString;
            }
        } 

    }
}
