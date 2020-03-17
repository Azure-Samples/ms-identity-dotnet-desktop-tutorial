using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Console_TokenCache
{
    class Program
    {
        private static PublicClientApplicationOptions appConfiguration = null;
        private static IConfiguration configuration;
        private static string _authority;

        static async Task Main(string[] args)
        {
            // Using appsettings.json as our configuration settings
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            configuration = builder.Build();

            appConfiguration = configuration
                .Get<PublicClientApplicationOptions>();

            _authority = string.Concat(appConfiguration.Instance, appConfiguration.TenantId);

            // Building a public client application
            var app = PublicClientApplicationBuilder.Create(appConfiguration.ClientId)
                                                    .WithAuthority(_authority)
                                                    .WithRedirectUri(appConfiguration.RedirectUri)
                                                    .Build();

            string[] scopes = new[] { "user.read" };
            AuthenticationResult result;

            try
            {
                var accounts = await app.GetAccountsAsync();

                // Try to acquire an access token from the cache. If an interaction is required, 
                // MsalUiRequiredException will be thrown.
                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                // Acquiring an access token interactively. MSAL will cache it so we can use AcquireTokenSilent
                // on future calls.
                result = await app.AcquireTokenInteractive(scopes)
                            .ExecuteAsync();
            }

            string graphApiUrl = configuration.GetValue<string>("GraphApiUrl");
            // Instantiating GraphServiceClient and using the access token acquired above.
            var graphClient = GetGraphServiceClient(result.AccessToken, graphApiUrl);

            // Calling the /me endpoint
            var me = await graphClient.Me.Request().GetAsync();

            // Printing the results
            Console.Write(Environment.NewLine);
            Console.WriteLine($"Hello {result.Account.Username}");
            Console.Write(Environment.NewLine);
            Console.WriteLine("-------- GRAPH RESULT --------");
            Console.Write(Environment.NewLine);
            Console.WriteLine($"Id: {me.Id}");
            Console.WriteLine($"Display Name: {me.DisplayName}");
            Console.WriteLine($"Email: {me.Mail}");
        }

        private static GraphServiceClient GetGraphServiceClient(string accessToken, string graphApiUrl)
        {
            GraphServiceClient graphServiceClient = new GraphServiceClient(graphApiUrl,
                                                                 new DelegateAuthenticationProvider(
                                                                     async (requestMessage) =>
                                                                     {
                                                                         await Task.Run(() =>
                                                                         {
                                                                             requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
                                                                         });
                                                                     }));

            return graphServiceClient;
        }
    }
}
