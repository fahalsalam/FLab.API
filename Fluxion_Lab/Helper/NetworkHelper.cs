using System.Net;
using System.Net.NetworkInformation;

namespace Fluxion_Lab.Helper
{
    public static class NetworkHelper
    {
        public static bool IsInternetAvailable()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://www.google.com"))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
