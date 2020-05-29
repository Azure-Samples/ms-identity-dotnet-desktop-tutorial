using Microsoft.Identity.Client;
using Microsoft.Graph;
using System.Linq;
using System.Net.Http.Headers;

namespace Console_Interactive_MultiTarget
{
    public sealed class SingletonGraphServiceClient
    {
        private SingletonGraphServiceClient()
        {
            GraphClient = GetSingletonGraphServiceClient();
        }
        private static SingletonGraphServiceClient instance = null;
        public static SingletonGraphServiceClient Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SingletonGraphServiceClient();
                }
                return instance;
            }
        }
        public IPublicClientApplication App { get; set; }
        public string[] Scopes { get; set; }
        public static GraphServiceClient GraphClient { get; set; }

        /// <summary>
        /// Method to acquire the access token and instantiate the graph service client.
        /// </summary>
        /// <returns>GraphServiceClient</returns>
        private GraphServiceClient GetSingletonGraphServiceClient()
        {
            GraphServiceClient graph = new GraphServiceClient(new DelegateAuthenticationProvider(
                                        async (requestMessage) =>
                                        {
                                            AuthenticationResult result;
                                            try
                                            {
                                                var accounts = await App.GetAccountsAsync();
                                                result = await App.AcquireTokenSilent(Scopes, accounts.FirstOrDefault())
                                                 .ExecuteAsync();
                                            }
                                            catch (MsalUiRequiredException ex)
                                            {
                                                result = await App.AcquireTokenInteractive(Scopes)
                                                 .WithClaims(ex.Claims)
                                                 .ExecuteAsync();
                                            }

                                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", result.AccessToken);

                                        }));
            return graph;
        }
    }
}
