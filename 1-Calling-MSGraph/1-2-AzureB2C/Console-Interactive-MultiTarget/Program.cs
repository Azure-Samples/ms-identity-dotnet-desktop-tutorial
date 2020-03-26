using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Console_Interactive_MultiTarget
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

            // Loading PublicClientApplicationOptions from the values set on appsettings.json
            appConfiguration = configuration
                .Get<PublicClientApplicationOptions>();

            string defaultUserFlow  = configuration.GetValue<string>("DefaultUserFlow");
            string domain = configuration.GetValue<string>("Domain");

            // Building the B2C authority, https://<b2cInstance>/tfp/<tenantDomain>/<defaultUserFlow>
            _authority = string.Concat(appConfiguration.Instance, domain, "/", defaultUserFlow);

            // Building a public client application
            var app = PublicClientApplicationBuilder.Create(appConfiguration.ClientId)
                                                    .WithB2CAuthority(_authority)
                                                    .WithRedirectUri(appConfiguration.RedirectUri)
                                                    .Build();

            string[] scopes = new[] { "https://graph.microsoft.com/user.read" };
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

            // Calling the /me endpoint of Microsoft Graph
            var me = await graphClient.Me.Request().GetAsync();

            // Printing the results
            DisplayGraphResult(result, me);
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

        private static void DisplayGraphResult(AuthenticationResult result, User me)
        {
            Console.Write(Environment.NewLine);
            Console.WriteLine($"Hello {result.Account.Username}");
            Console.Write(Environment.NewLine);
            Console.WriteLine("-------- GRAPH RESULT --------");
            Console.Write(Environment.NewLine);
            Console.WriteLine($"Id: {me.Id}");
            Console.WriteLine($"Display Name: {me.DisplayName}");
            Console.WriteLine($"Email: {me.Mail}");
            Console.Write(Environment.NewLine);
            Console.WriteLine("------------------------------");
            Console.Write(Environment.NewLine);
            Console.Write(Environment.NewLine);
        }
    }
}
